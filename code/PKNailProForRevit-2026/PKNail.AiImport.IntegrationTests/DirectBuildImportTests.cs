using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using NUnit.Framework;

using PKNailForRevit;
using PKNailForRevit.AiImport;
using PKNail.AiImport.DirectBuild;
using PKNail.AiImport.Mapping;
using PKNail.AiImport.Model;

namespace PKNail.AiImport.IntegrationTests
{
    /// <summary>
    /// Epic 13 — end-to-end (fake-driven) Direct Build: JSON → validate (lenient) → plan → DirectBuildImporter
    /// → <see cref="IAiImportTarget"/>. Proves the real October exports import whole, in one undo step, with
    /// per-element metering, without the survey engine, and that failures follow Q12.
    /// </summary>
    [TestFixture]
    public class DirectBuildImportTests
    {
        private static string OctoberDir => Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "October5");

        public static IEnumerable<string> OctoberFiles() =>
            Directory.GetFiles(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "October5"), "*.json")
                     .Select(f => Path.GetFileName(f)!)
                     .OrderBy(n => n);

        private static (DetectedBuilding Building, DirectBuildPlan Plan) Load(string file)
        {
            DetectedBuilding building = AiModelReader.Load(Path.Combine(OctoberDir, file));
            ValidationReport v = AiModelValidator.Validate(building, elementIssuesAreWarnings: true);
            Assert.That(v.IsValid, Is.True, v.ToString());
            return (building, DirectBuildPlanner.Plan(building));
        }

        private static DetectedWall Wall(double sx, double sy, double ex, double ey, params DetectedOpening[] openings) => new DetectedWall
        {
            StartMeters = new DetectedPoint2D { X = sx, Y = sy },
            EndMeters = new DetectedPoint2D { X = ex, Y = ey },
            ThicknessMeters = 0.115,
            Openings = openings.ToList(),
        };

        private static DetectedOpening Window(double centre) => new DetectedOpening
        { LocationFromStartMeters = centre, WidthMeters = 0.9, HeightMeters = 1.2, SillHeightMeters = 0.8, Kind = "Window" };

        private static DetectedLevel Level(double elevation, string? name, params DetectedWall[] walls) => new DetectedLevel
        {
            Name = name, ElevationMeters = elevation, Floor2FloorMeters = 2.75, Floor2CeilingMeters = 2.45,
            TypicalWindowSillHeightMeters = 0.8, TypicalWindowHeadHeightMeters = 2.0, Walls = walls.ToList(),
        };

        // --- The real October exports ----------------------------------------------------------------

        [TestCaseSource(nameof(OctoberFiles))]
        public void OctoberExport_ImportsEveryWallAndOpening_InOneUndoStep(string file)
        {
            (DetectedBuilding building, DirectBuildPlan plan) = Load(file);
            var proj = new RecordingProjectModel();

            ImportReport report = new DirectBuildImporter().Import(proj, plan);

            Assert.That(report.Committed, Is.True, report.ToString());
            Assert.That(report.HasFailures, Is.False, report.ToString());
            Assert.That(report.WallsPlaced, Is.EqualTo(building.Levels.Sum(l => l.Walls.Count)));
            Assert.That(report.OpeningsPlaced, Is.EqualTo(building.Levels.Sum(l => l.Walls.Sum(w => w.Openings.Count))));
            Assert.That(report.Mode, Is.EqualTo("Direct Build"));

            Assert.That(proj.InitializeTransactionCount, Is.EqualTo(1));
            Assert.That(proj.CommitTransactionCount, Is.EqualTo(1));
            Assert.That(proj.RollBackTransactionCount, Is.EqualTo(0));
            Assert.That(proj.TransactionName, Is.EqualTo(DirectBuildImporter.TransactionName));
        }

        [TestCaseSource(nameof(OctoberFiles))]
        public void OctoberExport_NeverTouchesTheSurveyEngine(string file)
        {
            var proj = new RecordingProjectModel();
            new DirectBuildImporter().Import(proj, Load(file).Plan);

            string[] engineCalls = { "SurveyWallPrep", "SurveyWall", "SurveyWallHostedObjectPrep", "SurveyWallHostedObject",
                                     "ActivateWallById", "CreateWallAbsolute", "CreateOpeningOnWall" };
            Assert.That(proj.Calls.Intersect(engineCalls), Is.Empty);
        }

        [Test]
        public void Orleans_WallsLandOnTheExistingGroundLevel_AtThePlannedCoordinates()
        {
            DirectBuildPlan plan = Load("Orleans_ai_pknail_v2.json").Plan;
            var proj = new RecordingProjectModel();
            new DirectBuildImporter().Import(proj, plan);

            Assert.That(proj.CreatedLevelNames, Is.Empty, "the scan level (2.6 m in the survey frame) re-bases onto the existing level at 0");
            List<PlannedWall> planned = plan.Levels[0].Walls.ToList();
            for (int i = 0; i < planned.Count; i++)
            {
                DirectWallSpec spec = proj.CreateWallDirectCalls[i].Spec;
                Assert.That(spec.LevelElementId, Is.EqualTo(1), "seeded Level 1 has element id 1");
                Assert.That(spec.StartX.ValueMetricMeters, Is.EqualTo(planned[i].Start.X).Within(1e-9));
                Assert.That(spec.EndY.ValueMetricMeters, Is.EqualTo(planned[i].End.Y).Within(1e-9));
                Assert.That(spec.Thickness.ValueMetricMeters, Is.EqualTo(planned[i].ThicknessMeters).Within(1e-9));
                Assert.That(spec.Height.ValueMetricMeters, Is.EqualTo(2.45).Within(1e-9));
                Assert.That(spec.Mark, Is.EqualTo($"W{i + 1}"));
            }
        }

        [Test]
        public void Openings_AreHostedOnTheirOwnWall_AtTheirCentre()
        {
            DirectBuildPlan plan = Load("Dover_ai_pknail_v2.json").Plan;
            var proj = new RecordingProjectModel();
            new DirectBuildImporter().Import(proj, plan);

            int k = 0;
            for (int wi = 0; wi < plan.Levels[0].Walls.Count; wi++)
            {
                PlannedWall w = plan.Levels[0].Walls[wi];
                long wallId = proj.CreateWallDirectCalls[wi].Id;
                foreach (PlannedOpening o in w.Openings)
                {
                    (long hostId, DirectOpeningSpec spec) = proj.CreateOpeningDirectCalls[k++];
                    Assert.That(hostId, Is.EqualTo(wallId), o.Label);
                    Assert.That(spec.LocationFromStart.ValueMetricMeters, Is.EqualTo(o.CentreFromStartMeters).Within(1e-9));
                    Assert.That(spec.Width.ValueMetricMeters, Is.EqualTo(o.WidthMeters).Within(1e-9));
                    Assert.That(spec.IsDoor, Is.EqualTo(o.Kind == OpeningKind.Door));
                    Assert.That(spec.InteriorX * spec.InteriorX + spec.InteriorY * spec.InteriorY, Is.EqualTo(1.0).Within(1e-9));
                }
            }
            Assert.That(k, Is.EqualTo(plan.OpeningCount));
        }

        // --- Metering (Q4) and failure policy (Q12) -------------------------------------------------

        [Test]
        public void Metering_IsRecordedOncePerWallAndPerOpening()
        {
            DirectBuildPlan plan = Load("Carlisle_ai_pknail_v2.json").Plan;
            int calls = 0;
            ImportReport report = new DirectBuildImporter().Import(new RecordingProjectModel(), plan, () => { calls++; return true; });

            Assert.That(report.Committed, Is.True, report.ToString());
            Assert.That(calls, Is.EqualTo(plan.WallCount + plan.OpeningCount));
        }

        [Test]
        public void MeteringVeto_StopsAndRollsBackEverything()
        {
            DirectBuildPlan plan = Load("Carlisle_ai_pknail_v2.json").Plan;
            var proj = new RecordingProjectModel();
            int calls = 0;
            ImportReport report = new DirectBuildImporter().Import(proj, plan, () => ++calls <= 5);

            Assert.That(report.MeteringStopped, Is.True);
            Assert.That(report.RolledBack, Is.True);
            Assert.That(report.Committed, Is.False);
            Assert.That(proj.RollBackTransactionCount, Is.EqualTo(1));
            Assert.That(proj.CommitTransactionCount, Is.EqualTo(0));
        }

        [Test]
        public void WallRefusedByRevit_RollsBackTheWholeImport()
        {
            var proj = new RecordingProjectModel { FailAtStep = "CreateWallDirect" };
            ImportReport report = new DirectBuildImporter().Import(proj, Load("Concord_ai_pknail_v2.json").Plan);

            Assert.That(report.RolledBack, Is.True);
            Assert.That(report.Failures.Single(), Does.Contain("CreateWallDirect(W1)"));
            Assert.That(report.WallsPlaced, Is.EqualTo(0));
        }

        [Test]
        public void ExceptionWhileDrawing_IsCaught_AndRolledBack()
        {
            var proj = new RecordingProjectModel { ThrowAtStep = "CreateWallDirect" };
            ImportReport report = new DirectBuildImporter().Import(proj, Load("Concord_ai_pknail_v2.json").Plan);

            Assert.That(report.RolledBack, Is.True);
            Assert.That(report.Failures.Single(), Does.StartWith("exception: forced throw"));
        }

        [Test]
        public void OpeningRevitCannotHost_IsSkipped_TheImportStillCommits()
        {
            DirectBuildPlan plan = Load("Danvers_ai_pknail_v2.json").Plan;
            var proj = new RecordingProjectModel { FailAtStep = "CreateOpeningDirect" };
            ImportReport report = new DirectBuildImporter().Import(proj, plan);

            Assert.That(report.Committed, Is.True, report.ToString());
            Assert.That(report.WallsPlaced, Is.EqualTo(plan.WallCount));
            Assert.That(report.OpeningsPlaced, Is.EqualTo(0));
            Assert.That(report.Skipped.Count, Is.EqualTo(plan.OpeningCount));
            Assert.That(proj.AbortPendingTransactionCount, Is.EqualTo(plan.OpeningCount));
        }

        [Test]
        public void OpeningNote_AndMissingDoorOrientation_AreReportedAsWarnings()
        {
            DirectBuildPlan plan = Load("Orleans_ai_pknail_v2.json").Plan;
            var proj = new RecordingProjectModel { OpeningNoteToReturn = "fixed-size family" };
            ImportReport report = new DirectBuildImporter().Import(proj, plan);

            int doors = plan.Levels.SelectMany(l => l.Walls).SelectMany(w => w.Openings).Count(o => o.Kind == OpeningKind.Door);
            Assert.That(report.Warnings.Count(w => w.EndsWith(": fixed-size family")), Is.EqualTo(plan.OpeningCount));
            Assert.That(report.Warnings.Count(w => w.Contains("door swing and hinge side not provided")), Is.EqualTo(doors));
        }

        // --- Levels, skips and adjustments in the report ------------------------------------------

        [Test]
        public void SecondLevel_IsCreatedThroughLevelsData_AndItsWallsUseTheNewLevel()
        {
            var building = new DetectedBuilding
            {
                SchemaVersion = "2.1",
                Levels = { Level(0.6, "Ground", Wall(0, 0, 4, 0)), Level(3.35, "Level 1", Wall(0, 0, 4, 0, Window(2.0))) },
            };
            var proj = new RecordingProjectModel();
            ImportReport report = new DirectBuildImporter().Import(proj, DirectBuildPlanner.Plan(building));

            Assert.That(report.Committed, Is.True, report.ToString());
            // "Level 1" already exists (the seeded level at 0), so the new one is disambiguated.
            Assert.That(proj.CreatedLevelNames, Is.EqualTo(new[] { "Level 1 (AI)" }));
            Assert.That(proj.CreateWallDirectCalls[0].Spec.LevelElementId, Is.EqualTo(1));
            Assert.That(proj.CreateWallDirectCalls[1].Spec.LevelElementId, Is.EqualTo(9001));

            LevelDataItem upper = proj.AssignedLevelsData!.Levels.Single(l => l.Name == "Level 1 (AI)");
            Assert.That(upper.ProjectElev.ValueMetricMeters, Is.EqualTo(2.75).Within(1e-9));
            Assert.That(upper.Floor2CeilingElev.ValueMetricMeters, Is.EqualTo(2.45).Within(1e-9));
            Assert.That(report.Adjustments, Has.Some.Contains("Level 'Level 1 (AI)' created at 2.75 m"));
        }

        [Test]
        public void PlanSkipsAdjustmentsAndWarnings_AllReachTheReport()
        {
            var building = new DetectedBuilding
            {
                Levels = { Level(0, null,
                    Wall(0, 0, 10, 0), Wall(5, 0.06, 5, 4),     // gap closed → adjustment
                    Wall(1, 1, 1, 1),                           // zero length → skipped
                    Wall(6.5, 0.02, 9.5, 0.02)) },              // overlaps W1, clear of W2's junction → warning
            };
            DirectBuildPlan plan = DirectBuildPlanner.Plan(building);
            ImportReport report = new DirectBuildImporter().Import(new RecordingProjectModel(), plan);

            Assert.That(report.Committed, Is.True, report.ToString());
            Assert.That(report.WallsPlaced, Is.EqualTo(3));
            Assert.That(report.Adjustments, Has.Some.Contains("W2 start: extended 60 mm"));
            Assert.That(report.Skipped, Has.Some.Contains("W3").And.Contains("zero-length"));
            Assert.That(report.Warnings, Has.Some.Contains("W1 and W4 overlap"));
            Assert.That(report.ToString(), Does.StartWith("AI import (Direct Build): COMMITTED"));
        }

        [Test]
        public void ExampleFile_DoorOrientationTypeAndIds_ReachRevit()
        {
            string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "ai_engine_pknail_results.v2.1.example.json");
            var proj = new RecordingProjectModel();
            ImportReport report = new DirectBuildImporter().Import(proj, DirectBuildPlanner.Plan(AiModelReader.Load(path)));

            Assert.That(report.Committed, Is.True, report.ToString());
            Assert.That(report.Warnings, Is.Empty, "every door in the example carries its orientation");
            Assert.That(proj.CreateWallDirectCalls.Select(c => c.Spec.Mark), Is.EqualTo(new[] { "W1", "W2", "W3", "W4", "W5" }));

            DirectOpeningSpec front = proj.CreateOpeningDirectCalls.Single(c => c.Spec.Mark == "W2-D1").Spec;
            Assert.That(front.IsDoor, Is.True);
            Assert.That(front.Hinge, Is.EqualTo(HingeSideKind.Left));
            Assert.That(front.Swing, Is.EqualTo(SwingDirectionKind.In));
            Assert.That(front.Comment, Is.EqualTo("AI type: Hinged; front door; seen from both sides"));
            Assert.That(front.InteriorX, Is.EqualTo(-1.0).Within(1e-9), "W2 runs +Y with InteriorSide Left ⇒ interior is -X");

            DirectOpeningSpec window = proj.CreateOpeningDirectCalls.Single(c => c.Spec.Mark == "W3-N1").Spec;
            Assert.That(window.Comment, Is.EqualTo("AI type: Sliding"));
            Assert.That(window.Sill.ValueMetricMeters, Is.EqualTo(1.1).Within(1e-9));

            // Epic 12c: the type also reaches Revit as the family selector.
            Assert.That(front.OpeningType, Is.EqualTo("Hinged"));
            Assert.That(window.OpeningType, Is.EqualTo("Sliding"));
        }

        // The 2026-11-06 Revit check: every door was built from the stock swing-door family because the
        // family index never reached Revit. It must now arrive with each opening.
        [Test]
        public void RevitCheckFile_FamilyIndexAndHanding_ReachRevit()
        {
            string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "October31", "1_v2_ai_pknail_v2_doortypes.json");
            DetectedBuilding building = AiModelReader.Load(path);
            Assert.That(AiModelValidator.Validate(building, elementIssuesAreWarnings: true).IsValid, Is.True);
            var proj = new RecordingProjectModel();
            ImportReport report = new DirectBuildImporter().Import(proj, DirectBuildPlanner.Plan(building));

            Assert.That(report.Committed, Is.True, report.ToString());
            Assert.That(report.WallsPlaced, Is.EqualTo(14));
            Assert.That(report.OpeningsPlaced, Is.EqualTo(13));
            Assert.That(report.Skipped, Is.Empty, report.ToString());

            List<DirectOpeningSpec> doors = proj.CreateOpeningDirectCalls.Select(c => c.Spec).Where(s => s.IsDoor).ToList();
            List<DirectOpeningSpec> windows = proj.CreateOpeningDirectCalls.Select(c => c.Spec).Where(s => !s.IsDoor).ToList();
            Assert.That(doors.Select(d => d.FamilyDefIdx), Is.EqualTo(new[] { 4, 4, 4, 0, 0, 4, 0 }), "walls 3, 5, 6 (x3), 7, 9");
            Assert.That(windows.Select(w => w.FamilyDefIdx), Is.All.EqualTo(0));
            Assert.That(doors.Select(d => d.OpeningType), Is.All.Null, "a v2.0 file has no OpeningType");

            // Wall 6 door 1: hinged, right hinge, swings in (east). Wall 9 door 0: right hinge, swings out.
            DirectOpeningSpec w6d1 = doors.Single(d => d.Mark == "W7-D2");
            Assert.That(w6d1.Hinge, Is.EqualTo(HingeSideKind.Right));
            Assert.That(w6d1.Swing, Is.EqualTo(SwingDirectionKind.In));
            Assert.That(w6d1.LocationFromStart.ValueMetricMeters, Is.EqualTo(3.351).Within(1e-6));
            DirectOpeningSpec w9d0 = doors.Single(d => d.Mark == "W10-D1");
            Assert.That(w9d0.Hinge, Is.EqualTo(HingeSideKind.Right));
            Assert.That(w9d0.Swing, Is.EqualTo(SwingDirectionKind.Out));
            Assert.That(w9d0.InteriorY, Is.EqualTo(-1.0).Within(1e-9));
        }
    }
}
