namespace PKNail.AiImport.DirectBuild;

/// <summary>Tolerances for <see cref="WallNetworkCleaner"/>. Defaults were tuned on the October 2026 AI exports.</summary>
public sealed class CleanupOptions
{
    public static CleanupOptions Default { get; } = new();

    /// <summary>
    /// A wall within this many degrees of the X or Y axis is squared up to it. Matches the validator's
    /// drift band (2°): anything more skewed is treated as a deliberately angled wall and left alone.
    /// </summary>
    public double SquareUpToleranceDeg { get; init; } = 2.0;

    /// <summary>
    /// A loose wall end is moved (along its own wall, never sideways) by at most this much to meet the
    /// centerline of a neighbouring wall at a corner or T-junction. Larger gaps are left as they are.
    /// </summary>
    public double JoinToleranceMeters { get; init; } = 0.10;

    /// <summary>Set false to draw the AI geometry verbatim (the cleanup still reports overlaps).</summary>
    public bool Enabled { get; init; } = true;
}

/// <summary>A wall the cleaner may adjust. Plan coordinates in meters.</summary>
public sealed class CleanWall
{
    public CleanWall(string label, PlanPoint start, PlanPoint end, double thicknessMeters)
    {
        Label = label;
        Start = start;
        End = end;
        ThicknessMeters = thicknessMeters;
    }

    public string Label { get; }
    public PlanPoint Start { get; set; }
    public PlanPoint End { get; set; }
    public double ThicknessMeters { get; }
    public double Length => Start.DistanceTo(End);

    internal PlanPoint EndAt(int which) => which == 0 ? Start : End;
    internal void SetEnd(int which, PlanPoint p) { if (which == 0) Start = p; else End = p; }
}

/// <summary>
/// Epic 13 — tidies the AI engine's independent wall segments so Revit can join them, without ever
/// deleting a wall or moving one sideways:
/// <list type="number">
/// <item><b>Square up</b> walls that are within <see cref="CleanupOptions.SquareUpToleranceDeg"/> of an
/// axis (both ends share the averaged coordinate, so the wall stays where it was).</item>
/// <item><b>Close small gaps</b>: a loose end that stops short of, or overshoots, a neighbouring wall's
/// centerline by at most <see cref="CleanupOptions.JoinToleranceMeters"/> is extended or trimmed along its
/// own direction onto it. This closes L-corners and T-junctions.</item>
/// <item><b>Report overlaps</b>: two walls lying on the same line and sharing length are flagged as a
/// possible duplicate. Both are kept; deleting one is the reviewer's call.</item>
/// </list>
/// Every change is returned as a human-readable line for the import report. Revit-free and deterministic.
/// </summary>
public static class WallNetworkCleaner
{
    private const double CoincidentMeters = 0.001;     // ends closer than 1 mm already meet
    private const double ReportMoveMeters = 0.0005;    // moves under 0.5 mm are float noise, not reported
    private const double MinWallMeters = 0.05;         // never shorten a wall below 5 cm
    private static readonly double MinJoinAngleSin = Math.Sin(10.0 * Math.PI / 180.0);
    private static readonly double ParallelSin = Math.Sin(1.0 * Math.PI / 180.0);

    /// <summary>Clean <paramref name="walls"/> in place.</summary>
    /// <param name="adjustments">Receives one line per geometric change.</param>
    /// <param name="warnings">Receives one line per thing the reviewer should check (overlaps).</param>
    public static void Clean(IList<CleanWall> walls, CleanupOptions options,
                             ICollection<string> adjustments, ICollection<string> warnings)
    {
        ArgumentNullException.ThrowIfNull(walls);
        ArgumentNullException.ThrowIfNull(options);

        if (options.Enabled)
        {
            foreach (CleanWall w in walls) SquareUp(w, options, adjustments);
            for (int i = 0; i < walls.Count; i++)
                for (int which = 0; which < 2; which++)
                    CloseGap(walls, i, which, options, adjustments);
        }
        ReportOverlaps(walls, warnings);
    }

    /// <summary>Deviation of a wall's bearing from the nearest axis, degrees, in [0, 45].</summary>
    public static double DeviationFromAxisDeg(PlanPoint a, PlanPoint b)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y;
        if (dx == 0.0 && dy == 0.0) return 0.0;
        double deg = Math.Abs(Math.Atan2(dy, dx) * 180.0 / Math.PI) % 90.0;
        return Math.Min(deg, 90.0 - deg);
    }

    private static void SquareUp(CleanWall w, CleanupOptions o, ICollection<string> adjustments)
    {
        double dev = DeviationFromAxisDeg(w.Start, w.End);
        if (dev == 0.0 || dev > o.SquareUpToleranceDeg) return;

        PlanPoint s = w.Start, e = w.End;
        bool horizontal = Math.Abs(e.X - s.X) >= Math.Abs(e.Y - s.Y);
        if (horizontal)
        {
            double y = (s.Y + e.Y) / 2.0;
            w.Start = new PlanPoint(s.X, y);
            w.End = new PlanPoint(e.X, y);
        }
        else
        {
            double x = (s.X + e.X) / 2.0;
            w.Start = new PlanPoint(x, s.Y);
            w.End = new PlanPoint(x, e.Y);
        }

        double moved = Math.Max(s.DistanceTo(w.Start), e.DistanceTo(w.End));
        if (moved >= ReportMoveMeters)
            adjustments.Add($"{w.Label}: squared up ({dev:0.###}° off {(horizontal ? "the X axis" : "the Y axis")}); "
                            + $"each end moved {moved * 1000.0:0.#} mm.");
    }

    private static void CloseGap(IList<CleanWall> walls, int i, int which, CleanupOptions o,
                                 ICollection<string> adjustments)
    {
        CleanWall w = walls[i];
        PlanPoint p = w.EndAt(which);
        PlanPoint q = w.EndAt(1 - which);
        if (IsAlreadyJoined(walls, i, p)) return;

        PlanPoint dir = (p - q).Normalized();   // pointing outward through this end
        double bestShift = double.MaxValue;
        int bestJ = -1;

        for (int j = 0; j < walls.Count; j++)
        {
            if (j == i) continue;
            CleanWall other = walls[j];
            double lenJ = other.Length;
            if (lenJ <= 0.0) continue;
            PlanPoint dj = (other.End - other.Start) * (1.0 / lenJ);

            double cross = (dir.X * dj.Y) - (dir.Y * dj.X);
            if (Math.Abs(cross) < MinJoinAngleSin) continue; // (near) parallel: no corner to close

            // Solve p + s·dir = other.Start + u·dj.
            PlanPoint r = other.Start - p;
            double s = ((r.X * dj.Y) - (r.Y * dj.X)) / cross;
            double u = ((r.X * dir.Y) - (r.Y * dir.X)) / cross;

            if (Math.Abs(s) > o.JoinToleranceMeters) continue;
            if (u < -o.JoinToleranceMeters || u > lenJ + o.JoinToleranceMeters) continue;
            if (w.Length + s < MinWallMeters) continue;
            if (Math.Abs(s) < Math.Abs(bestShift)) { bestShift = s; bestJ = j; }
        }

        if (bestJ < 0) return;
        w.SetEnd(which, p + (dir * bestShift));
        if (Math.Abs(bestShift) >= ReportMoveMeters)
            adjustments.Add($"{w.Label} {(which == 0 ? "start" : "end")}: "
                            + $"{(bestShift > 0 ? "extended" : "trimmed")} {Math.Abs(bestShift) * 1000.0:0.#} mm "
                            + $"to meet {walls[bestJ].Label}.");
    }

    // An end already meets the network if it touches another wall's end or lies on another wall's centerline.
    private static bool IsAlreadyJoined(IList<CleanWall> walls, int i, PlanPoint p)
    {
        for (int j = 0; j < walls.Count; j++)
        {
            if (j == i) continue;
            CleanWall other = walls[j];
            if (p.DistanceTo(other.Start) <= CoincidentMeters || p.DistanceTo(other.End) <= CoincidentMeters)
                return true;

            double len = other.Length;
            if (len <= 0.0) continue;
            PlanPoint d = (other.End - other.Start) * (1.0 / len);
            PlanPoint r = p - other.Start;
            double along = r.Dot(d);
            double perp = Math.Abs((r.X * d.Y) - (r.Y * d.X));
            if (perp <= CoincidentMeters && along >= -CoincidentMeters && along <= len + CoincidentMeters)
                return true;
        }
        return false;
    }

    private static void ReportOverlaps(IList<CleanWall> walls, ICollection<string> warnings)
    {
        for (int i = 0; i < walls.Count; i++)
        {
            CleanWall a = walls[i];
            double lenA = a.Length;
            if (lenA <= 0.0) continue;
            PlanPoint da = (a.End - a.Start) * (1.0 / lenA);

            for (int j = i + 1; j < walls.Count; j++)
            {
                CleanWall b = walls[j];
                double lenB = b.Length;
                if (lenB <= 0.0) continue;
                PlanPoint db = (b.End - b.Start) * (1.0 / lenB);
                if (Math.Abs((da.X * db.Y) - (da.Y * db.X)) > ParallelSin) continue;

                PlanPoint r = b.Start - a.Start;
                double offset = Math.Abs((r.X * da.Y) - (r.Y * da.X));
                if (offset >= (a.ThicknessMeters + b.ThicknessMeters) / 2.0) continue;

                double p1 = (b.Start - a.Start).Dot(da), p2 = (b.End - a.Start).Dot(da);
                double overlap = Math.Min(lenA, Math.Max(p1, p2)) - Math.Max(0.0, Math.Min(p1, p2));
                if (overlap > MinWallMeters)
                    warnings.Add($"{a.Label} and {b.Label} overlap for {overlap:0.##} m on the same line "
                                 + $"({offset * 1000.0:0} mm apart) — possibly a duplicate wall. Both were drawn; "
                                 + "delete one in Revit if it is a duplicate.");
            }
        }
    }
}
