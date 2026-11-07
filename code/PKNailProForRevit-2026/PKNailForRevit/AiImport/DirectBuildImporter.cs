#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

using PKNail.AiImport.DirectBuild;
using PKNail.AiImport.Mapping;

namespace PKNailForRevit.AiImport
{
    /// <summary>
    /// Epic 13 — the Direct Build bridge. Drives an <see cref="IAiImportTarget"/> from a Revit-free
    /// <see cref="DirectBuildPlan"/>: matches or creates the levels, then draws every wall and hosts every
    /// opening exactly where the plan says. It is the default import for v2/v2.1 files (absolute wall
    /// endpoints); v1 files keep the survey-chain path in <see cref="AiModelImporter"/>.
    ///
    /// <para>The PKNAIL 2.0 rules are kept:</para>
    /// <list type="bullet">
    /// <item>The AI JSON is the only input; meters become <see cref="Distance"/> here and nowhere else.</item>
    /// <item>One transaction group = one undo entry. A wall Revit refuses, an exception, or a metering veto
    /// rolls the whole import back (Q12). Elements the planner could not draw were already skipped and
    /// reported before the transaction started, so bad data never forces a rollback.</item>
    /// <item>Metering is per element (Q4): <c>recordUsage</c> runs before each wall and each opening.</item>
    /// <item>Openings are best-effort: one that Revit cannot host is skipped and reported; the rest continue.</item>
    /// </list>
    /// </summary>
    public sealed class DirectBuildImporter
    {
        /// <summary>Two levels within this distance (meters) are the same level.</summary>
        internal const double LevelMatchToleranceMeters = 0.005;

        /// <summary>The transaction-group (undo) name of a Direct Build import.</summary>
        public const string TransactionName = @"Import AI Model (Direct Build)";

        public ImportReport Import(IAiImportTarget proj, DirectBuildPlan plan, Func<bool>? recordUsage = null)
        {
            if (proj == null) { throw new ArgumentNullException(nameof(proj)); }
            if (plan == null) { throw new ArgumentNullException(nameof(plan)); }

            var report = new ImportReport { Mode = @"Direct Build" };
            report.RebaseXMeters = plan.RebaseXMeters;
            report.RebaseYMeters = plan.RebaseYMeters;
            report.Rebased = plan.RebaseXMeters != 0.0 || plan.RebaseYMeters != 0.0;
            foreach (string a in plan.Adjustments) { report.AddAdjustment(a); }
            foreach (string w in plan.Warnings) { report.AddWarning(w); }
            foreach (string s in plan.Skipped) { report.AddSkipped(s); }

            string msg = string.Empty;
            bool inTransaction = false;
            try
            {
                if (!ok(proj.InitializeTransaction(ref msg), report, "InitializeTransaction", msg)) { return report; }
                if (!ok(proj.SetTransactionName(TransactionName, ref msg), report, "SetTransactionName", msg)) { return report; }
                if (!ok(proj.StartTransaction(ref msg), report, "StartTransaction", msg)) { return report; }
                inTransaction = true;
                proj.BeginImportWarningCapture();

                long[] levelIds = ResolveLevels(proj, plan, report);
                report.LevelsAssigned = true;

                bool vetoed = DrawWalls(proj, plan, levelIds, report, recordUsage);
                DrainWarnings(proj, report);

                if (vetoed) { Rollback(proj, report); return report; }
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
                string where = ex.StackTrace == null ? "" : (" | at " + ex.StackTrace.Split('\n').FirstOrDefault()?.Trim());
                report.AddFailure("exception: " + ex.Message + where);
                if (inTransaction) { DrainWarnings(proj, report); Rollback(proj, report); }
                return report;
            }
        }

        // Returns true when the import was vetoed (wall failure or metering stop) and must roll back.
        private static bool DrawWalls(IAiImportTarget proj, DirectBuildPlan plan, long[] levelIds,
                                      ImportReport report, Func<bool>? recordUsage)
        {
            for (int li = 0; li < plan.Levels.Count; li++)
            {
                PlannedLevel level = plan.Levels[li];
                foreach (PlannedWall wall in level.Walls)
                {
                    if (recordUsage != null && !recordUsage())
                    {
                        report.MeteringStopped = true;
                        report.AddSkipped($"{wall.Label}: metering veto (recordUsage returned false)");
                        return true;
                    }

                    var spec = new DirectWallSpec
                    {
                        StartX = M(wall.Start.X),
                        StartY = M(wall.Start.Y),
                        EndX = M(wall.End.X),
                        EndY = M(wall.End.Y),
                        Thickness = M(wall.ThicknessMeters),
                        Height = M(wall.HeightMeters),
                        BaseOffset = M(wall.BaseOffsetMeters),
                        LevelElementId = levelIds[li],
                        Mark = wall.Label,
                        Comment = wall.Comment,
                    };

                    string msg = string.Empty;
                    if (proj.CreateWallDirect(spec, out long wallId, ref msg) != CommandResult.Succeeded)
                    {
                        // Walls are the core of the model: a wall Revit refuses rolls everything back (Q12).
                        report.AddFailure($"CreateWallDirect({wall.Label}) failed" + (string.IsNullOrEmpty(msg) ? "" : (": " + msg)));
                        return true;
                    }
                    report.WallsPlaced++;

                    foreach (PlannedOpening opening in wall.Openings)
                    {
                        if (recordUsage != null && !recordUsage())
                        {
                            report.MeteringStopped = true;
                            report.AddSkipped($"{opening.Label}: metering veto (recordUsage returned false)");
                            return true;
                        }
                        PlaceOpening(proj, wall, wallId, opening, report);
                    }
                }
            }
            return false;
        }

        private static void PlaceOpening(IAiImportTarget proj, PlannedWall wall, long wallId,
                                         PlannedOpening opening, ImportReport report)
        {
            var spec = new DirectOpeningSpec
            {
                IsDoor = opening.Kind == OpeningKind.Door,
                LocationFromStart = M(opening.CentreFromStartMeters),
                Width = M(opening.WidthMeters),
                Height = M(opening.HeightMeters),
                Sill = M(opening.SillMeters),
                Hinge = opening.Hinge,
                Swing = opening.Swing,
                OpeningType = opening.OpeningType,
                FamilyDefIdx = opening.FamilyDefIdx,
                InteriorX = wall.InteriorNormal.X,
                InteriorY = wall.InteriorNormal.Y,
                Mark = opening.Label,
                Comment = OpeningComment(opening),
            };

            string msg = string.Empty;
            CommandResult r;
            string note;
            try
            {
                r = proj.CreateOpeningDirect(wallId, spec, out _, out note, ref msg);
            }
            catch (Exception ex)
            {
                r = CommandResult.Failed;
                note = string.Empty;
                msg += ex.Message;
            }

            if (r == CommandResult.Succeeded)
            {
                report.OpeningsPlaced++;
                if (!string.IsNullOrEmpty(note)) { report.AddWarning($"{opening.Label}: {note}"); }
                if (opening.Kind == OpeningKind.Door && (opening.Swing == null || opening.Hinge == null))
                {
                    var missing = new List<string>();
                    if (opening.Swing == null) { missing.Add("swing"); }
                    if (opening.Hinge == null) { missing.Add("hinge side"); }
                    report.AddWarning($"{opening.Label}: door {string.Join(" and ", missing)} not provided by the AI; Revit default kept.");
                }
            }
            else
            {
                // Openings are best-effort: discard just this one (never the import) and say why.
                proj.AbortPendingTransaction();
                report.AddSkipped($"{opening.Label}: could not be hosted on {wall.Label}" + (string.IsNullOrEmpty(msg) ? "" : (": " + msg)));
            }
        }

        // Match each planned level to an existing PKNAIL/Revit level by elevation, or add a new LevelDataItem
        // (empty ElementId) which the LevelsData setter turns into a real Revit level + floor plan — the same
        // mechanism the 1.0 Levels dialog uses. Returns the Revit element id per planned level (-1 = unknown,
        // in which case walls go on the lowest level and a warning is recorded).
        private static long[] ResolveLevels(IAiImportTarget proj, DirectBuildPlan plan, ImportReport report)
        {
            LevelsData levelsData = proj.LevelsData;
            var names = new HashSet<string>(levelsData.Levels.Select(l => l.Name ?? ""), StringComparer.OrdinalIgnoreCase);
            var used = new HashSet<LevelDataItem>();

            foreach (PlannedLevel pl in plan.Levels)
            {
                LevelDataItem? item = FindByElevation(levelsData, pl.ElevationMeters, used);
                if (item == null)
                {
                    string name = UniqueName(pl.Name, names);
                    names.Add(name);
                    item = new LevelDataItem(name, "", M(pl.ElevationMeters));
                    levelsData.Levels.Add(item);
                    report.AddAdjustment($"Level '{name}' created at {pl.ElevationMeters:0.###} m.");
                }
                used.Add(item);
                ApplyLevelData(item, pl);
            }

            proj.LevelsData = levelsData;   // creates the missing Revit levels and stores the PKNAIL level data

            // Read back so newly created levels carry their Revit element ids.
            LevelsData stored = proj.LevelsData;
            var ids = new long[plan.Levels.Count];
            var claimed = new HashSet<LevelDataItem>();
            for (int i = 0; i < plan.Levels.Count; i++)
            {
                LevelDataItem? item = FindByElevation(stored, plan.Levels[i].ElevationMeters, claimed);
                if (item != null) { claimed.Add(item); }
                ids[i] = (item != null && long.TryParse(item.ElementId, out long id)) ? id : -1;
                if (ids[i] < 0)
                {
                    report.AddWarning($"Level '{plan.Levels[i].Name}' at {plan.Levels[i].ElevationMeters:0.###} m could not be "
                                      + "resolved to a Revit level; its walls were placed on the lowest level.");
                }
            }
            return ids;
        }

        private static LevelDataItem? FindByElevation(LevelsData data, double elevationMeters, HashSet<LevelDataItem> exclude) =>
            data.Levels
                .Where(l => !exclude.Contains(l) && !l.ProjectElev.IsNoValue
                            && Math.Abs(l.ProjectElev.ValueMetricMeters - elevationMeters) <= LevelMatchToleranceMeters)
                .OrderBy(l => Math.Abs(l.ProjectElev.ValueMetricMeters - elevationMeters))
                .FirstOrDefault();

        private static string UniqueName(string wanted, HashSet<string> taken)
        {
            if (!taken.Contains(wanted)) { return wanted; }
            string ai = wanted + " (AI)";
            if (!taken.Contains(ai)) { return ai; }
            for (int k = 2; ; k++)
            {
                string candidate = $"{wanted} (AI {k})";
                if (!taken.Contains(candidate)) { return candidate; }
            }
        }

        private static void ApplyLevelData(LevelDataItem item, PlannedLevel pl)
        {
            if (pl.Floor2FloorMeters > 0.0) { item.Floor2FloorElev = M(pl.Floor2FloorMeters); }
            if (pl.Floor2CeilingMeters > 0.0) { item.Floor2CeilingElev = M(pl.Floor2CeilingMeters); }
            item.IsDropCeiling = pl.HasDropCeiling;
            if (pl.HasDropCeiling) { item.Floor2DropCeilingElev = M(pl.DropCeilingHeightMeters); }

            item.TypicalWindowHeights.Clear();
            if (pl.TypicalWindowHeadMeters > pl.TypicalWindowSillMeters)
            {
                item.TypicalWindowHeights.Add(new WindowHeights(M(pl.TypicalWindowHeadMeters - pl.TypicalWindowSillMeters),
                                                                M(pl.TypicalWindowSillMeters)));
            }
        }

        private static Distance M(double meters) => new Distance(meters, Distance.Units.MetricMeter);

        // The AI's OpeningType also stays on the element (Comments) so a reviewer can compare it with the
        // family that was chosen from it.
        internal static string? OpeningComment(PlannedOpening o)
        {
            string? type = o.OpeningType == null ? null : "AI type: " + o.OpeningType;
            if (type == null) { return o.Comment; }
            return o.Comment == null ? type : type + "; " + o.Comment;
        }

        private static void DrainWarnings(IAiImportTarget proj, ImportReport report)
        {
            foreach (string w in proj.EndImportWarningCapture()) { report.AddWarning(w); }
        }

        private static void Rollback(IAiImportTarget proj, ImportReport report)
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
}
