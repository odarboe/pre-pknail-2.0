using PKNail.AiImport.Mapping;
using PKNail.AiImport.Model;

namespace PKNail.AiImport.DirectBuild;

/// <summary>Options for <see cref="DirectBuildPlanner.Plan"/>.</summary>
public sealed class DirectBuildOptions
{
    public static DirectBuildOptions Default { get; } = new();

    public CleanupOptions Cleanup { get; init; } = CleanupOptions.Default;
}

/// <summary>
/// Epic 13 — turns a v2/v2.1 <see cref="DetectedBuilding"/> (independent walls with absolute endpoints)
/// into a <see cref="DirectBuildPlan"/>: levels re-based to elevation 0, walls re-based near the origin and
/// tidied by <see cref="WallNetworkCleaner"/>, openings re-anchored to the tidied walls and kept inside them.
/// <para>
/// Unlike the survey-chain mapping, this needs no loop order, no corner angles and no PKNAIL survey
/// families. A bad element is skipped and reported; it never blocks the rest of the file. Run
/// <c>AiModelValidator.Validate(building, elementIssuesAreWarnings: true)</c> first for file-level errors.
/// </para>
/// </summary>
public static class DirectBuildPlanner
{
    private const double Epsilon = 1e-6;
    private const double ReportMoveMeters = 0.0005;

    public static DirectBuildPlan Plan(DetectedBuilding building, DirectBuildOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(building);
        options ??= DirectBuildOptions.Default;

        var adjustments = new List<string>();
        var warnings = new List<string>();
        var skipped = new List<string>();

        // --- Level elevations: explicit ElevationMeters, else stacked Floor2Floor heights. Lowest → 0. ---
        int n = building.Levels.Count;
        var elevations = new double[n];
        double stacked = 0.0;
        for (int i = 0; i < n; i++)
        {
            DetectedLevel lvl = building.Levels[i];
            elevations[i] = lvl.ElevationMeters ?? stacked;
            stacked = elevations[i] + Math.Max(0.0, lvl.Floor2FloorMeters);
        }
        double rebaseZ = n > 0 ? elevations.Min() : 0.0;

        // --- Plan re-base: building-wide min X / min Y over drawable walls (deterministic, Epic 10.1). ---
        double minX = double.MaxValue, minY = double.MaxValue;
        foreach (DetectedLevel lvl in building.Levels)
            foreach (DetectedWall w in lvl.Walls)
            {
                if (!IsDrawable(w)) continue;
                minX = Math.Min(minX, Math.Min(w.StartMeters!.X, w.EndMeters!.X));
                minY = Math.Min(minY, Math.Min(w.StartMeters!.Y, w.EndMeters!.Y));
            }
        if (minX == double.MaxValue) { minX = 0.0; minY = 0.0; }
        var rebase = new PlanPoint(minX, minY);

        var levels = new List<PlannedLevel>(n);
        for (int li = 0; li < n; li++)
        {
            DetectedLevel lvl = building.Levels[li];
            string levelTag = n > 1 ? $"Level {li + 1} " : "";
            List<PlannedWall> walls = PlanWalls(lvl, levelTag, rebase, options, adjustments, warnings, skipped);

            levels.Add(new PlannedLevel(
                li,
                string.IsNullOrWhiteSpace(lvl.Name) ? $"Level {li + 1}" : lvl.Name!.Trim(),
                elevations[li] - rebaseZ,
                lvl.Floor2FloorMeters,
                lvl.Floor2CeilingMeters,
                lvl.HasDropCeiling,
                lvl.HasDropCeiling ? lvl.DropCeilingHeightMeters : 0.0,
                lvl.TypicalWindowSillHeightMeters,
                lvl.TypicalWindowHeadHeightMeters,
                walls));
        }

        levels.Sort((a, b) => a.ElevationMeters.CompareTo(b.ElevationMeters));
        return new DirectBuildPlan(levels, minX, minY, rebaseZ, adjustments, warnings, skipped);
    }

    private static bool IsDrawable(DetectedWall w) =>
        w.HasEndpoints
        && WallGeometry.Length(w.StartMeters!, w.EndMeters!) > Epsilon
        && w.ThicknessMeters > Epsilon;

    private static List<PlannedWall> PlanWalls(DetectedLevel lvl, string levelTag, PlanPoint rebase,
                                               DirectBuildOptions options, List<string> adjustments,
                                               List<string> warnings, List<string> skipped)
    {
        // 1. Collect drawable walls (re-based) and remember their original geometry for opening re-anchoring.
        var sources = new List<(int Index, DetectedWall Wall, PlanPoint Start0, PlanPoint End0)>();
        var work = new List<CleanWall>();
        for (int wi = 0; wi < lvl.Walls.Count; wi++)
        {
            DetectedWall w = lvl.Walls[wi];
            string label = WallLabel(w, wi);
            if (!w.HasEndpoints)
            {
                skipped.Add($"{levelTag}{label}: no StartMeters/EndMeters — Direct Build needs wall coordinates.");
                continue;
            }
            if (WallGeometry.Length(w.StartMeters!, w.EndMeters!) <= Epsilon)
            {
                skipped.Add($"{levelTag}{label}: start and end points are the same (zero-length wall).");
                continue;
            }
            if (w.ThicknessMeters <= Epsilon)
            {
                skipped.Add($"{levelTag}{label}: thickness {w.ThicknessMeters} m is not positive.");
                continue;
            }

            var s0 = new PlanPoint(w.StartMeters!.X, w.StartMeters!.Y) - rebase;
            var e0 = new PlanPoint(w.EndMeters!.X, w.EndMeters!.Y) - rebase;
            sources.Add((wi, w, s0, e0));
            work.Add(new CleanWall(levelTag + label, s0, e0, w.ThicknessMeters));
        }

        // 2. Tidy the network (square up, close small gaps, flag overlaps).
        WallNetworkCleaner.Clean(work, options.Cleanup, adjustments, warnings);

        // 3. Interior side = toward the plan centroid of this level's walls.
        PlanPoint centroid = new(0, 0);
        foreach (CleanWall cw in work) centroid += (cw.Start + cw.End) * 0.5;
        if (work.Count > 0) centroid *= 1.0 / work.Count;

        var planned = new List<PlannedWall>(work.Count);
        for (int k = 0; k < work.Count; k++)
        {
            (int index, DetectedWall w, PlanPoint s0, PlanPoint e0) = sources[k];
            CleanWall cw = work[k];
            string label = WallLabel(w, index);

            double height = lvl.Floor2CeilingMeters;
            if (w.HeightMeters is double h)
            {
                if (h > Epsilon) height = h;
                else warnings.Add($"{cw.Label}: HeightMeters {h} is not positive; used the level ceiling height {height:0.###} m.");
            }

            if (!string.IsNullOrWhiteSpace(w.LocationLine)
                && !w.LocationLine!.Trim().Equals("Centerline", StringComparison.OrdinalIgnoreCase))
                warnings.Add($"{cw.Label}: LocationLine '{w.LocationLine}' is not supported yet; the points were drawn as the wall centerline.");

            // Interior side: the AI's explicit InteriorSide when given, else the side facing the plan centroid.
            PlanPoint dir = (cw.End - cw.Start).Normalized();
            PlanPoint left = new(-dir.Y, dir.X);
            PlanPoint normal;
            string? side = w.InteriorSide?.Trim();
            if (string.Equals(side, "Left", StringComparison.OrdinalIgnoreCase)) normal = left;
            else if (string.Equals(side, "Right", StringComparison.OrdinalIgnoreCase)) normal = left * -1.0;
            else
            {
                if (!string.IsNullOrEmpty(side))
                    warnings.Add($"{cw.Label}: InteriorSide '{w.InteriorSide}' is not Left or Right; the side facing the middle of the plan was used.");
                PlanPoint mid = (cw.Start + cw.End) * 0.5;
                normal = (centroid - mid).Dot(left) >= 0 ? left : left * -1.0;
            }

            List<PlannedOpening> openings = PlanOpenings(w, label, cw, s0, e0, levelTag, adjustments, warnings, skipped);

            planned.Add(new PlannedWall(index, label, cw.Start, cw.End, w.ThicknessMeters, height,
                                        w.LevelOffsetMeters, normal, Clean(w.Comment), openings));
        }
        return planned;
    }

    private static List<PlannedOpening> PlanOpenings(DetectedWall w, string wallLabel, CleanWall cw, PlanPoint s0, PlanPoint e0,
                                                     string levelTag, List<string> adjustments,
                                                     List<string> warnings, List<string> skipped)
    {
        var result = new List<PlannedOpening>(w.Openings.Count);
        PlanPoint u0 = (e0 - s0).Normalized();
        PlanPoint u1 = (cw.End - cw.Start).Normalized();
        double len = cw.Length;
        int doors = 0, windows = 0;

        for (int oi = 0; oi < w.Openings.Count; oi++)
        {
            DetectedOpening o = w.Openings[oi];
            bool isDoor = o.ResolvedIsDoor;
            int ordinal = isDoor ? ++doors : ++windows;
            string label = !string.IsNullOrWhiteSpace(o.Id)
                ? o.Id!.Trim()
                : $"{wallLabel}-{(isDoor ? "D" : "N")}{ordinal}";
            string where = $"{levelTag}{label}";

            if (o.WidthMeters <= Epsilon || o.HeightMeters <= Epsilon)
            {
                skipped.Add($"{where}: width {o.WidthMeters} m / height {o.HeightMeters} m must both be positive.");
                continue;
            }
            if (o.WidthMeters > len + Epsilon)
            {
                skipped.Add($"{where}: {o.WidthMeters:0.###} m wide, but its wall {cw.Label} is only {len:0.###} m long.");
                continue;
            }

            // Re-anchor: the opening stays at the same plan position even if the cleaner moved the wall start.
            PlanPoint centreXY = s0 + (u0 * o.LocationMeters);
            double centre = (centreXY - cw.Start).Dot(u1);
            if (centre < -Epsilon || centre > len + Epsilon)
            {
                skipped.Add($"{where}: centre {centre:0.###} m is outside its {len:0.###} m wall {cw.Label}.");
                continue;
            }

            // Keep the whole opening inside the wall; a small overhang is AI noise, so slide it in and say so.
            double half = o.WidthMeters / 2.0, shift = 0.0;
            if (centre - half < 0.0) shift = half - centre;
            else if (centre + half > len) shift = len - (centre + half);
            if (Math.Abs(shift) >= ReportMoveMeters)
                adjustments.Add($"{where}: moved {Math.Abs(shift) * 1000.0:0.#} mm along {cw.Label} so it fits inside the wall.");
            centre += shift;

            double sill = o.SillHeightMeters;
            if (sill < 0.0)
            {
                adjustments.Add($"{where}: sill height {sill:0.###} m raised to 0.");
                sill = 0.0;
            }

            HingeSideKind? hinge = o.HingeSideIndex switch { 0 => HingeSideKind.Left, 1 => HingeSideKind.Right, _ => null };
            SwingDirectionKind? swing = o.SwingDirectionIndex switch { 0 => SwingDirectionKind.Out, 1 => SwingDirectionKind.In, _ => null };

            result.Add(new PlannedOpening(oi, label, isDoor ? OpeningKind.Door : OpeningKind.Window, centre,
                                          o.WidthMeters, o.HeightMeters, sill,
                                          isDoor ? hinge : null, isDoor ? swing : null,
                                          Clean(o.OpeningType), Clean(o.Comment))
                       { FamilyDefIdx = o.FamilyDefIdx });
        }

        // Flag openings that overlap on the same wall (the AI's own data; both are still placed).
        var ordered = result.OrderBy(p => p.CentreFromStartMeters).ToList();
        for (int i = 1; i < ordered.Count; i++)
        {
            PlannedOpening a = ordered[i - 1], b = ordered[i];
            double gap = (b.CentreFromStartMeters - (b.WidthMeters / 2.0)) - (a.CentreFromStartMeters + (a.WidthMeters / 2.0));
            if (gap < -0.001)
                warnings.Add($"{levelTag}{a.Label} and {b.Label} overlap by {-gap * 1000.0:0} mm on {cw.Label}; both were placed.");
        }
        return result;
    }

    // The AI id when present, else W{n} in 1-based source order.
    private static string WallLabel(DetectedWall w, int index) =>
        !string.IsNullOrWhiteSpace(w.Id) ? w.Id!.Trim() : $"W{index + 1}";

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
