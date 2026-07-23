#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

using PKNail.AiImport.Mapping;

namespace PKNailForRevit.AiImport
{
    /// <summary>
    /// Epic 3 bridge — drives an injected <see cref="IProjectModel"/> from a Revit-free
    /// <see cref="MappedBuilding"/>. This is the ONLY place the meters→<see cref="Distance"/> conversion,
    /// the enum→plugin-type translation and the Revit transaction orchestration live; the
    /// <c>PKNail.AiImport</c> library stays Revit-free.
    ///
    /// <para>Contract:</para>
    /// <list type="bullet">
    /// <item>The whole import runs inside ONE transaction group (Q12 — atomic). Any failure, a
    /// non-<see cref="CommandResult.Succeeded"/> result, an exception, or a <c>recordUsage()==false</c>
    /// metering veto rolls the entire import back and reports it.</item>
    /// <item><see cref="Import"/> never throws; everything is captured into the returned
    /// <see cref="ImportReport"/>.</item>
    /// <item>Only <see cref="IProjectModel"/> is touched — no <c>DrivableForm</c>/<c>Disto</c>/dialog types.</item>
    /// </list>
    /// </summary>
    public sealed class AiModelImporter
    {
        /// <summary>
        /// Imports <paramref name="model"/> into <paramref name="proj"/>.
        /// </summary>
        /// <param name="proj">The live project model (a <c>RevitDocument</c> in-product, or a fake in tests).</param>
        /// <param name="model">The normalized building produced by <c>AiModelMapper.Map</c>.</param>
        /// <param name="recordUsage">
        /// Optional per-element metering hook, invoked once BEFORE each wall and each opening is placed.
        /// Returning <c>false</c> vetoes that placement and stops the import; per Q12 the default behaviour
        /// is an atomic rollback of everything placed so far.
        /// </param>
        public ImportReport Import(IProjectModel proj, MappedBuilding model, Func<bool>? recordUsage = null)
        {
            if (proj == null) { throw new ArgumentNullException(nameof(proj)); }
            if (model == null) { throw new ArgumentNullException(nameof(model)); }

            var report = new ImportReport();
            string msg = string.Empty;
            bool inTransaction = false;

            try
            {
                if (!ok(proj.InitializeTransaction(ref msg), report, "InitializeTransaction", msg)) { return report; }
                if (!ok(proj.SetTransactionName(@"Import AI Model", ref msg), report, "SetTransactionName", msg)) { return report; }
                if (!ok(proj.StartTransaction(ref msg), report, "StartTransaction", msg)) { return report; }
                inTransaction = true;

                // Epic 7: collect Revit warnings raised during placement (e.g. "insert conflicts with joined
                // wall") into the report instead of letting them block the batch with modal dialogs.
                proj.BeginImportWarningCapture();

                // --- Levels -------------------------------------------------------------
                proj.LevelsData = BuildLevelsData(proj, model, report);
                report.LevelsAssigned = true;

                // --- Walls + openings, in document order --------------------------------
                // v2 contract: every wall carries absolute plan endpoints, so the walls are placed DIRECTLY
                // at their real coordinates (independent segments — a multi-room building, not one connected
                // survey chain). v1 (no endpoints) keeps the proven survey-chain path UNCHANGED.
                bool vetoed = HasAbsoluteGeometry(model)
                    ? ImportAbsolute(proj, model, report, recordUsage, ref msg)
                    : ImportSurveyChain(proj, model, report, recordUsage, ref msg);

                // Fold the collected Revit warnings into the report regardless of the outcome.
                DrainWarnings(proj, report);

                if (vetoed)
                {
                    // Q12 default: atomic rollback of the partial import.
                    Rollback(proj, report);
                    return report;
                }

                if (!ok(proj.CommitTransaction(ref msg), report, "CommitTransaction", msg))
                {
                    Rollback(proj, report);
                    return report;
                }
                report.Committed = true;
                return report;
            }
            catch (Exception ex)
            {
                // Include the originating frame so import failures are diagnosable from the report dialog.
                string where = ex.StackTrace == null ? "" : (" | at " + ex.StackTrace.Split('\n').FirstOrDefault()?.Trim());
                report.AddFailure("exception: " + ex.Message + where);
                if (inTransaction) { DrainWarnings(proj, report); Rollback(proj, report); }
                return report;
            }
        }

        // Move any warnings the project collected during placement into the report. EndImportWarningCapture
        // is idempotent, so calling this on both the normal and exception paths never double-counts.
        private static void DrainWarnings(IProjectModel proj, ImportReport report)
        {
            foreach (string w in proj.EndImportWarningCapture()) { report.AddWarning(w); }
        }

        /// <summary>True when the model's walls carry absolute geometry (v2). Requires at least one wall
        /// and every wall to have endpoints — mixed models fall back to the survey chain.</summary>
        private static bool HasAbsoluteGeometry(MappedBuilding model)
        {
            bool any = false;
            foreach (MappedLevel level in model.Levels)
            {
                foreach (MappedWall wall in level.Walls)
                {
                    any = true;
                    if (!wall.HasAbsoluteGeometry) { return false; }
                }
            }
            return any;
        }

        // The v1 survey-chain path, factored out unchanged from the original inline loop. Returns true if
        // the import was vetoed (a failure or metering stop) and the caller should roll back.
        private bool ImportSurveyChain(IProjectModel proj, MappedBuilding model, ImportReport report,
                                       Func<bool>? recordUsage, ref string msg)
        {
            bool vetoed = false;
            foreach (MappedLevel level in model.Levels)
            {
                for (int wi = 0; wi < level.Walls.Count; wi++)
                {
                    MappedWall wall = level.Walls[wi];

                    if (recordUsage != null && !recordUsage())
                    {
                        report.MeteringStopped = true;
                        report.SkipWall(wall.Index, "metering veto (recordUsage returned false)");
                        vetoed = true;
                        break;
                    }

                    if (!PlaceWall(proj, model, level, wall, report, ref msg)) { vetoed = true; break; }
                    report.WallsPlaced++;

                    for (int oi = 0; oi < wall.Openings.Count; oi++)
                    {
                        MappedOpening opening = wall.Openings[oi];

                        if (recordUsage != null && !recordUsage())
                        {
                            report.MeteringStopped = true;
                            report.SkipOpening(wall.Index, oi, "metering veto (recordUsage returned false)");
                            vetoed = true;
                            break;
                        }

                        // Openings are best-effort: a benign skip (e.g. missing window family) is
                        // recorded but does NOT abort the import. A hard engine error (e.g. a
                        // TransactionError) still rolls everything back to keep the model consistent.
                        // The survey chain auto-activates each new wall left-to-right, so host that way.
                        OpeningOutcome outcome = PlaceOpening(proj, wall, opening, SurveyDirection.LeftToRight, report, ref msg);
                        if (outcome == OpeningOutcome.Placed) { report.OpeningsPlaced++; }
                        else if (outcome == OpeningOutcome.NoFamily)
                        { report.SkipOpening(wall.Index, oi, "no PKNAIL survey family loaded for this opening category"); }
                        else if (outcome == OpeningOutcome.Failed) { vetoed = true; break; }
                    }

                    if (vetoed) { break; }
                }
                if (vetoed) { break; }
            }
            return vetoed;
        }

        // The v2 direct-absolute path: place each wall at its own re-based real coordinates, then host each
        // of its openings DIRECTLY on the just-placed wall (CreateOpeningOnWall). Openings are best-effort.
        private bool ImportAbsolute(IProjectModel proj, MappedBuilding model, ImportReport report,
                                    Func<bool>? recordUsage, ref string msg)
        {
            // Re-base: subtract the building-wide min X / min Y from every endpoint so the model sits near
            // the project origin instead of at its far-flung survey coordinates (e.g. Y ~ -100 m). The
            // offset is a pure function of the input (deterministic) and is recorded on the report so the
            // landing point relative to the original survey frame is documented (Epic 10.1).
            ComputeRebaseOffset(model, out double offsetX, out double offsetY);
            report.RebaseXMeters = offsetX;
            report.RebaseYMeters = offsetY;
            report.Rebased = (offsetX != 0.0 || offsetY != 0.0);

            bool vetoed = false;
            foreach (MappedLevel level in model.Levels)
            {
                for (int wi = 0; wi < level.Walls.Count; wi++)
                {
                    MappedWall wall = level.Walls[wi];

                    if (recordUsage != null && !recordUsage())
                    {
                        report.MeteringStopped = true;
                        report.SkipWall(wall.Index, "metering veto (recordUsage returned false)");
                        vetoed = true;
                        break;
                    }

                    // ImportAbsolute is only entered when HasAbsoluteGeometry(model) holds, so every wall's
                    // endpoints are non-null here.
                    MappedPoint2D start = wall.Start!;
                    MappedPoint2D end = wall.End!;
                    double sx = start.XMeters - offsetX;
                    double sy = start.YMeters - offsetY;
                    double ex = end.XMeters - offsetX;
                    double ey = end.YMeters - offsetY;

                    var thickness = new Distance(wall.ThicknessMeters, Distance.Units.MetricMeter);
                    var height = new Distance(level.Floor2CeilingMeters, Distance.Units.MetricMeter);

                    long wallId;
                    CommandResult result = proj.CreateWallAbsolute(sx, sy, ex, ey, thickness, height,
                                                                   /*levelIdx*/ 0, out wallId, ref msg);
                    if (result == CommandResult.Succeeded)
                    {
                        report.WallsPlaced++;
                    }
                    else
                    {
                        // A hard failure rolls the whole import back (walls are the core), matching the
                        // survey-chain behaviour where a failed SurveyWall aborts the import.
                        report.AddFailure($"CreateWallAbsolute(wall {wall.Index}) returned {result}"
                                          + (string.IsNullOrEmpty(msg) ? "" : (": " + msg)));
                        report.SkipWall(wall.Index, "CreateWallAbsolute failed: " + msg);
                        vetoed = true;
                        break;
                    }

                    // Openings host through the SAME proven 1.0 engine flow the survey-chain path uses:
                    // activate the just-placed wall as the engine's reference wall, then drive
                    // SurveyWallHostedObjectPrep -> SurveyWallHostedObject (family-def resolution + door
                    // hinge/swing + window facing). Best-effort: a single un-hostable opening rolls back only
                    // its own inner transaction (AbortPendingTransaction) and is recorded — never the whole
                    // import. A metering veto still stops + rolls back per Q12.
                    bool interiorFace = (model.Orientation == SurveyOrientationKind.Flipped);
                    for (int oi = 0; oi < wall.Openings.Count; oi++)
                    {
                        MappedOpening opening = wall.Openings[oi];

                        if (recordUsage != null && !recordUsage())
                        {
                            report.MeteringStopped = true;
                            report.SkipOpening(wall.Index, oi, "metering veto (recordUsage returned false)");
                            vetoed = true;
                            break;
                        }

                        // Reject an opening that cannot fit its host wall rather than placing it off the end.
                        if (!OpeningFitsWall(wall, opening, out string? boundsReason))
                        {
                            report.SkipOpening(wall.Index, oi, boundsReason);
                            continue;
                        }

                        // Make this wall the engine's active/reference wall (headless), honoring the scan's
                        // interior/exterior and the opening's near-end == wall-start convention.
                        if (proj.ActivateWallById(wallId, interiorFace, /*nearEndIsStart*/ true, ref msg)
                            != CommandResult.Succeeded)
                        {
                            report.SkipOpening(wall.Index, oi, "could not activate host wall: " + msg);
                            msg = string.Empty;
                            proj.AbortPendingTransaction();
                            continue;
                        }

                        try
                        {
                            // FromModel makes the stored survey direction (near end) authoritative.
                            OpeningOutcome outcome = PlaceOpening(proj, wall, opening, SurveyDirection.FromModel, report, ref msg);
                            if (outcome == OpeningOutcome.Placed)
                            {
                                report.OpeningsPlaced++;
                            }
                            else if (outcome == OpeningOutcome.NoFamily)
                            {
                                // No PKNAIL survey family loaded (e.g. a stock template). Fall back to direct
                                // hosting so the opening still appears; note that the AI's hinge/facing could
                                // not be applied (that needs a PKNAIL family — use the PKNAIL template).
                                proj.AbortPendingTransaction();
                                if (HostOpeningDirect(proj, wallId, opening, ref msg))
                                {
                                    report.OpeningsPlaced++;
                                    report.AddWarning($"wall[{wall.Index}].{opening.Kind}: hosted with a default family — "
                                        + "no PKNAIL survey family loaded, so the AI orientation (hinge/facing) was not applied");
                                }
                                else
                                {
                                    report.SkipOpening(wall.Index, oi,
                                        "no PKNAIL survey family and direct hosting failed: " + msg);
                                }
                            }
                            else { proj.AbortPendingTransaction(); }  // Skipped/Failed already recorded; keep going
                        }
                        catch (Exception hostEx)
                        {
                            // A hard engine throw (e.g. family cannot flip facing) leaves an open inner
                            // transaction; discard just that and continue with the next opening.
                            proj.AbortPendingTransaction();
                            report.SkipOpening(wall.Index, oi, "opening hosting failed: " + hostEx.Message);
                        }
                        msg = string.Empty;
                    }

                    if (vetoed) { break; }
                }
                if (vetoed) { break; }
            }
            return vetoed;
        }

        // Fallback direct hosting (no PKNAIL survey family available): place the opening straight onto the
        // wall via CreateOpeningOnWall. The AI's hinge/facing cannot be applied without a PKNAIL family, so
        // the caller records a warning. Returns true on success.
        private static bool HostOpeningDirect(IProjectModel proj, long wallId, MappedOpening opening, ref string msg)
        {
            long openId;
            CommandResult r = proj.CreateOpeningOnWall(
                wallId,
                opening.LocationFromNearEndMeters,
                new Distance(opening.WidthMeters, Distance.Units.MetricMeter),
                new Distance(opening.HeightMeters, Distance.Units.MetricMeter),
                new Distance(opening.SillHeightMeters, Distance.Units.MetricMeter),
                opening.Kind == OpeningKind.Door,
                out openId, ref msg);
            return r == CommandResult.Succeeded;
        }

        // #5: an opening must lie within its host wall's length (accounting for its half-width), or Revit
        // would place it off the end / fail. Uses the wall's derived centerline length and the near-end
        // location. Returns false with a reason when the opening cannot fit.
        private static bool OpeningFitsWall(MappedWall wall, MappedOpening opening, [NotNullWhen(false)] out string? reason)
        {
            reason = null;
            double wallLen = wall.LengthMeters;
            double loc = opening.LocationFromNearEndMeters;
            double halfW = Math.Max(0.0, opening.WidthMeters) / 2.0;
            const double tol = 1e-6;

            if (loc < -tol || loc > wallLen + tol)
            {
                reason = $"opening location {loc:0.###} m is outside the {wallLen:0.###} m host wall";
                return false;
            }
            if (loc - halfW < -tol || loc + halfW > wallLen + tol)
            {
                reason = $"opening (center {loc:0.###} m, width {opening.WidthMeters:0.###} m) extends past the {wallLen:0.###} m host wall";
                return false;
            }
            return true;
        }

        // Building-wide minimum X / Y across all absolute wall endpoints (the re-base origin).
        private static void ComputeRebaseOffset(MappedBuilding model, out double minX, out double minY)
        {
            minX = double.MaxValue;
            minY = double.MaxValue;
            foreach (MappedLevel level in model.Levels)
            {
                foreach (MappedWall wall in level.Walls)
                {
                    if (!wall.HasAbsoluteGeometry) { continue; }
                    minX = Math.Min(minX, Math.Min(wall.Start.XMeters, wall.End.XMeters));
                    minY = Math.Min(minY, Math.Min(wall.Start.YMeters, wall.End.YMeters));
                }
            }
            if (minX == double.MaxValue) { minX = 0.0; }
            if (minY == double.MaxValue) { minY = 0.0; }
        }

        private bool PlaceWall(IProjectModel proj, MappedBuilding model, MappedLevel level, MappedWall wall,
                               ImportReport report, ref string msg)
        {
            CommandContext ctx;
            WallSurveyParams wp;

            // Walls are surveyed left→right by convention for AI import.
            if (!ok(proj.SurveyWallPrep(SurveyDirection.LeftToRight, out ctx, out wp, ref msg),
                    report, $"SurveyWallPrep(wall {wall.Index})", msg))
            {
                report.SkipWall(wall.Index, "SurveyWallPrep failed: " + msg);
                return false;
            }

            // Overwrite ONLY the measured fields (Prep populates project context defaults).
            wp.Length = new Distance(wall.LengthMeters, Distance.Units.MetricMeter);
            wp.Width = new Distance(wall.ThicknessMeters, Distance.Units.MetricMeter);
            wp.CornerAngle = new Geometry2D.Radians(wall.CornerAngleRad);
            wp.LevelOffset = new Distance(wall.LevelOffsetMeters, Distance.Units.MetricMeter);

            // Wall height: the AI contract carries no per-wall height, so use the level's floor-to-ceiling
            // height as an unconnected wall height (-1 = not connected to a level above).
            wp.IsUnknownWidth = false;
            wp.Height = new Distance(level.Floor2CeilingMeters, Distance.Units.MetricMeter);
            wp.ConnectedLevelIdx = -1;

            if (wall.IsFirstWall)
            {
                wp.IsFirstWall = true;
                wp.SurveyorCompassDirection = (CompassDirection)(int)model.FirstWallCompass;

                // Orientation (interior vs exterior scan) is carried on the command context (Q-orientation).
                // ctx is guaranteed non-null after a successful SurveyWallPrep (checked via ok() above).
                ctx.SurveyOrientation = (model.Orientation == SurveyOrientationKind.Flipped)
                    ? SurveyOrientation.Flipped
                    : SurveyOrientation.NotFlipped;
            }

            if (!ok(proj.SurveyWall(ctx, wp, ref msg), report, $"SurveyWall(wall {wall.Index})", msg))
            {
                report.SkipWall(wall.Index, "SurveyWall failed: " + msg);
                return false;
            }

            return true;
        }

        private enum OpeningOutcome { Placed, Skipped, Failed, NoFamily }

        private OpeningOutcome PlaceOpening(IProjectModel proj, MappedWall wall, MappedOpening opening,
                                            SurveyDirection dir, ImportReport report, ref string msg)
        {
            bool isDoor = opening.Kind == OpeningKind.Door;
            string category = isDoor ? FamilyCategory.Doors : FamilyCategory.Windows;

            CommandContext ctx;
            WallHostedObjectSurveyParams hp;

            if (!ok(proj.SurveyWallHostedObjectPrep(dir, category, out ctx, out hp, ref msg),
                    report, $"SurveyWallHostedObjectPrep(wall {wall.Index})", msg))
            {
                report.SkipOpening(wall.Index, opening.Kind, "SurveyWallHostedObjectPrep failed: " + msg);
                return OpeningOutcome.Failed;
            }

            // Guard the family-def index to a valid slot; the AI v1 contract always emits 0 (Q3) but the
            // project family-def list length is decided by the document, so clamp defensively. A document
            // with no matching survey family (e.g. a stock template) is a benign skip, not a failure.
            int idx = opening.FamilyDefIdx;
            int count = (hp.ProjectSurveyFamilyDefs != null) ? hp.ProjectSurveyFamilyDefs.Count : 0;
            if (count == 0)
            {
                // No PKNAIL survey family for this category is loaded (e.g. a stock template). Let the caller
                // decide: the survey-chain path skips; the absolute path falls back to direct hosting so the
                // opening still appears. (Correct hinge/facing needs a PKNAIL family — see plan risk R4.)
                return OpeningOutcome.NoFamily;
            }
            if (idx < 0 || idx >= count) { idx = 0; }
            hp.SurveyFamilyDefIdx = idx;

            hp.Location = new Distance(opening.LocationFromNearEndMeters, Distance.Units.MetricMeter);

            // These setters mutate the selected SurveyFamilyDef (must be a valid index, guarded above).
            hp.SetWidth(new Distance(opening.WidthMeters, Distance.Units.MetricMeter));
            hp.SetHeight(new Distance(opening.HeightMeters, Distance.Units.MetricMeter));
            hp.SetSillHeight(new Distance(opening.SillHeightMeters, Distance.Units.MetricMeter));

            if (isDoor)
            {
                // Door: hinge side (lateral) + swing direction (facing).
                hp.SetLateralOrientation((int)opening.HingeSide);
                hp.SetFacingOrientation((int)opening.SwingDirection);
            }
            else
            {
                // Window: interior/exterior facing relation (facing). No lateral hinge for windows.
                hp.SetFacingOrientation((int)opening.Facing);
            }

            if (!ok(proj.SurveyWallHostedObject(ctx, hp, ref msg),
                    report, $"SurveyWallHostedObject(wall {wall.Index})", msg))
            {
                report.SkipOpening(wall.Index, opening.Kind, "SurveyWallHostedObject failed: " + msg);
                return OpeningOutcome.Failed;
            }

            return OpeningOutcome.Placed;
        }

        // Merge the imported level data INTO the document's existing Revit levels (mapped bottom-up).
        // We update existing LevelDataItems (which carry ElementId + ProjectElev) rather than creating
        // bare ones, so the levels stay linked to real Revit Level elements and walls can host on them.
        private LevelsData BuildLevelsData(IProjectModel proj, MappedBuilding model, ImportReport report)
        {
            LevelsData levelsData = proj.LevelsData; // existing doc levels (ElementId + ProjectElev populated)

            // Lowest elevation first, so DetectedLevel[0] (ground) maps to the lowest Revit level.
            List<LevelDataItem> existing = levelsData.Levels
                .OrderBy(l => l.ProjectElev.IsNoValue ? double.MaxValue : l.ProjectElev.ValueRevitInternal)
                .ToList();

            for (int i = 0; i < model.Levels.Count; i++)
            {
                if (i >= existing.Count)
                {
                    report.SkipLevel(i, "no matching Revit level in the document (add levels first)");
                    continue;
                }

                MappedLevel ml = model.Levels[i];
                LevelDataItem item = existing[i];

                item.Floor2FloorElev = new Distance(ml.Floor2FloorMeters, Distance.Units.MetricMeter);
                item.Floor2CeilingElev = new Distance(ml.Floor2CeilingMeters, Distance.Units.MetricMeter);
                item.IsDropCeiling = ml.HasDropCeiling;
                if (ml.HasDropCeiling)
                {
                    item.Floor2DropCeilingElev = new Distance(ml.DropCeilingHeightMeters, Distance.Units.MetricMeter);
                }

                // Typical window heights: WindowHeights(height, sillHeight); Head = sill + height.
                item.TypicalWindowHeights.Clear();
                if (ml.TypicalWindowHeadMeters > ml.TypicalWindowSillMeters)
                {
                    var sill = new Distance(ml.TypicalWindowSillMeters, Distance.Units.MetricMeter);
                    var height = new Distance(ml.TypicalWindowHeadMeters - ml.TypicalWindowSillMeters,
                                              Distance.Units.MetricMeter);
                    item.TypicalWindowHeights.Add(new WindowHeights(height, sill));
                }
            }

            return levelsData;
        }

        private void Rollback(IProjectModel proj, ImportReport report)
        {
            string msg = string.Empty;
            try
            {
                proj.RollBackTransaction(ref msg);
                report.RolledBack = true;
            }
            catch (Exception ex)
            {
                report.AddFailure("rollback failed: " + ex.Message);
            }
        }

        private static bool ok(CommandResult result, ImportReport report, string step, string msg)
        {
            if (result == CommandResult.Succeeded) { return true; }
            report.AddFailure($"{step} returned {result}" + (string.IsNullOrEmpty(msg) ? "" : (": " + msg)));
            return false;
        }
    }

    /// <summary>Outcome of an <see cref="AiModelImporter.Import"/> run. Pure data; never throws.</summary>
    public sealed class ImportReport
    {
        public int WallsPlaced { get; internal set; }
        public int OpeningsPlaced { get; internal set; }

        public bool LevelsAssigned { get; internal set; }
        public bool Committed { get; internal set; }
        public bool RolledBack { get; internal set; }

        /// <summary>True when <c>recordUsage</c> vetoed a placement and the import stopped early.</summary>
        public bool MeteringStopped { get; internal set; }

        /// <summary>Epic 10: the deterministic plan-coordinate offset (meters) subtracted from every absolute
        /// wall endpoint so the building lands near the project origin. Zero on the v1 survey-chain path.
        /// <see cref="Rebased"/> is true when a non-zero offset was applied.</summary>
        public double RebaseXMeters { get; internal set; }
        public double RebaseYMeters { get; internal set; }
        public bool Rebased { get; internal set; }

        private readonly List<string> m_skipped = new List<string>();
        private readonly List<string> m_failures = new List<string>();
        private readonly List<string> m_warnings = new List<string>();

        public IReadOnlyList<string> Skipped { get { return m_skipped; } }
        public IReadOnlyList<string> Failures { get { return m_failures; } }

        /// <summary>Revit warnings collected during placement (Epic 7) — e.g. "insert conflicts with joined
        /// wall". These did NOT stop the import; they are surfaced for the reviewer, not clicked through.</summary>
        public IReadOnlyList<string> Warnings { get { return m_warnings; } }

        public bool HasFailures { get { return m_failures.Count > 0; } }
        public bool HasWarnings { get { return m_warnings.Count > 0; } }

        internal void SkipLevel(int index, string reason)
        { m_skipped.Add($"level[{index}]: {reason}"); }

        internal void SkipWall(int index, string reason)
        { m_skipped.Add($"wall[{index}]: {reason}"); }

        internal void SkipOpening(int wallIndex, int openingIndex, string reason)
        { m_skipped.Add($"wall[{wallIndex}].opening[{openingIndex}]: {reason}"); }

        internal void SkipOpening(int wallIndex, OpeningKind kind, string reason)
        { m_skipped.Add($"wall[{wallIndex}].{kind}: {reason}"); }

        internal void AddFailure(string reason)
        { m_failures.Add(reason); }

        internal void AddWarning(string warning)
        { m_warnings.Add(warning); }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("AI import: ")
              .Append(Committed ? "COMMITTED" : (RolledBack ? "ROLLED BACK" : "incomplete"))
              .Append("; walls placed=").Append(WallsPlaced)
              .Append(", openings placed=").Append(OpeningsPlaced)
              .Append(", skipped=").Append(m_skipped.Count)
              .Append(", warnings=").Append(m_warnings.Count)
              .Append(", failures=").Append(m_failures.Count);
            if (MeteringStopped) { sb.Append(" (metering stopped)"); }
            if (Rebased)
            {
                sb.Append(Environment.NewLine)
                  .Append("  origin re-based to project 0,0 (subtracted ")
                  .Append(RebaseXMeters.ToString("0.###")).Append(", ").Append(RebaseYMeters.ToString("0.###"))
                  .Append(" m from survey coords)");
            }
            foreach (string f in m_failures) { sb.Append(Environment.NewLine).Append("  ! ").Append(f); }
            foreach (string s in m_skipped) { sb.Append(Environment.NewLine).Append("  - ").Append(s); }
            foreach (string w in m_warnings) { sb.Append(Environment.NewLine).Append("  ~ ").Append(w); }
            return sb.ToString();
        }
    }
}
