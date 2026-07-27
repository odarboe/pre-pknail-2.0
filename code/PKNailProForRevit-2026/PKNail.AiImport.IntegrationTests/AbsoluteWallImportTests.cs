using System;
using System.IO;
using System.Linq;

using NUnit.Framework;

using PKNailForRevit;
using PKNailForRevit.AiImport;
using PKNail.AiImport.Model;
using PKNail.AiImport.Mapping;

namespace PKNail.AiImport.IntegrationTests
{
    /// <summary>
    /// End-to-end (fake-driven) check of the v2 DIRECT-absolute placement path using the real building
    /// `new_final_combined_pknail.json` — 9 independent absolute walls (multi-room, NOT a connected survey
    /// chain). Walls are placed via <c>CreateWallAbsolute</c> (never <c>SurveyWall</c>) and re-based near the
    /// origin. Openings host through the SAME proven 1.0 engine flow the survey-chain path uses: each wall is
    /// activated headlessly (<c>ActivateWallById</c>) and the opening placed via
    /// <c>SurveyWallHostedObjectPrep</c>/<c>SurveyWallHostedObject</c> (family-def resolution + door
    /// hinge/swing + window facing), NOT the simplified <c>CreateOpeningOnWall</c>.
    /// </summary>
    [TestFixture]
    public class AbsoluteWallImportTests
    {
        private static string RealV2Path =>
            Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "new_final_combined_pknail.json");

        private static MappedBuilding LoadMappedRealV2()
        {
            DetectedBuilding building = AiModelReader.Load(RealV2Path);
            Assert.That(AiModelValidator.Validate(building).IsValid, Is.True, "real v2 building should validate.");
            return AiModelMapper.Map(building);
        }

        [Test]
        public void RealV2_Import_PlacesNineWallsDirectly_NotSurveyChain_Committed()
        {
            var proj = new RecordingProjectModel();
            ImportReport result = new AiModelImporter().Import(proj, LoadMappedRealV2());

            Assert.That(result.Committed, Is.True, result.ToString());
            Assert.That(result.WallsPlaced, Is.EqualTo(9));

            Assert.That(proj.CreateWallAbsoluteCalls.Count, Is.EqualTo(9), "9 direct absolute placements");
            Assert.That(proj.SurveyWallCalls.Count, Is.EqualTo(0), "must NOT use the survey chain");
            Assert.That(proj.Calls, Has.None.EqualTo("SurveyWall"));

            Assert.That(proj.CommitTransactionCount, Is.EqualTo(1));
            Assert.That(proj.RollBackTransactionCount, Is.EqualTo(0));
        }

        [Test]
        public void RealV2_Import_CoordinatesAreRebasedToOrigin()
        {
            var proj = new RecordingProjectModel();
            new AiModelImporter().Import(proj, LoadMappedRealV2());

            var xs = proj.CreateWallAbsoluteCalls
                .SelectMany(c => new[] { c.StartXMeters, c.EndXMeters });
            var ys = proj.CreateWallAbsoluteCalls
                .SelectMany(c => new[] { c.StartYMeters, c.EndYMeters });

            Assert.That(xs.Min(), Is.EqualTo(0.0).Within(1e-6), "min X re-based to ~0");
            Assert.That(ys.Min(), Is.EqualTo(0.0).Within(1e-6), "min Y re-based to ~0");
            Assert.That(ys.Max(), Is.GreaterThan(0.0), "the far-flung negative Y coords are shifted positive");
        }

        [Test]
        public void RealV2_Import_FirstWallRebasedEndpointsAndThickness()
        {
            var proj = new RecordingProjectModel();
            new AiModelImporter().Import(proj, LoadMappedRealV2());

            // Building min is X=4.4189, Y=-106.09114. Wall 0 raw: (5.5689,-97.74114)->(10.5689,-97.74114).
            RecordingProjectModel.AbsoluteWallCall w0 = proj.CreateWallAbsoluteCalls[0];
            Assert.That(w0.StartXMeters, Is.EqualTo(5.5689 - 4.4189).Within(1e-3));
            Assert.That(w0.StartYMeters, Is.EqualTo(-97.74114 - (-106.09114)).Within(1e-3));
            Assert.That(w0.EndXMeters, Is.EqualTo(10.5689 - 4.4189).Within(1e-3));
            Assert.That(w0.EndYMeters, Is.EqualTo(-97.74114 - (-106.09114)).Within(1e-3));
            Assert.That(w0.Thickness.ValueMetricMeters, Is.EqualTo(0.2).Within(1e-4));
            // Height = level floor-to-ceiling (~2.566 m).
            Assert.That(w0.Height.ValueMetricMeters, Is.EqualTo(2.566).Within(1e-3));
        }

        [Test]
        public void RealV2_Import_ThreeWindowsHostedViaEngineFlow_Committed()
        {
            var proj = new RecordingProjectModel();
            ImportReport result = new AiModelImporter().Import(proj, LoadMappedRealV2());

            // 3 windows (walls 2, 6, 7) each hosted through the 1.0 engine flow (activate + SurveyWallHostedObject).
            Assert.That(result.OpeningsPlaced, Is.EqualTo(3), result.ToString());
            Assert.That(result.Skipped.Count, Is.EqualTo(0), "no openings skipped");

            Assert.That(proj.SurveyHostedObjectCalls.Count, Is.EqualTo(3), "hosted via the engine, not CreateOpeningOnWall");
            Assert.That(proj.ActivateWallByIdCalls.Count, Is.EqualTo(3), "each host wall activated first");
            Assert.That(proj.CreateOpeningOnWallCalls.Count, Is.EqualTo(0), "the simplified path is not used");

            Assert.That(result.Committed, Is.True);
            Assert.That(proj.CommitTransactionCount, Is.EqualTo(1));
            Assert.That(proj.RollBackTransactionCount, Is.EqualTo(0));

            // All 3 route to the Windows category.
            Assert.That(proj.SurveyHostedObjectCalls.All(hp => hp.FamilyCategory.IsWindows()), Is.True, "all 3 are windows");
            // Every activation measures from the wall start (the JSON's near-end convention).
            Assert.That(proj.ActivateWallByIdCalls.All(a => a.NearEndIsStart), Is.True);
        }

        [Test]
        public void RealV2_Import_OpeningsActivateCorrectWalls_WithCorrectLocations()
        {
            var proj = new RecordingProjectModel();
            new AiModelImporter().Import(proj, LoadMappedRealV2());

            // The walls carrying openings are model indices 2, 6 and 7. CreateWallAbsoluteCalls is in wall
            // order, so its [i] entry is the returned element id for wall i. Openings are hosted in wall order,
            // so ActivateWallByIdCalls[0..2] target walls 2, 6, 7 respectively.
            long wall2Id = proj.CreateWallAbsoluteCalls[2].ReturnedId;
            long wall6Id = proj.CreateWallAbsoluteCalls[6].ReturnedId;
            long wall7Id = proj.CreateWallAbsoluteCalls[7].ReturnedId;

            Assert.That(proj.ActivateWallByIdCalls.Select(a => a.WallId),
                        Is.EqualTo(new[] { wall2Id, wall6Id, wall7Id }));

            // Location is measured along the host wall from its start; 3.8 m on wall 2 (first opening).
            WallHostedObjectSurveyParams hp2 = proj.SurveyHostedObjectCalls[0];
            SurveyFamilyDef def2 = hp2.ProjectSurveyFamilyDefs[hp2.SurveyFamilyDefIdx];
            Assert.That(hp2.Location.ValueMetricMeters, Is.EqualTo(3.8).Within(1e-4));
            Assert.That(def2.Width.ValueMetricMeters, Is.EqualTo(1.0).Within(1e-4));
            Assert.That(def2.Height.ValueMetricMeters, Is.EqualTo(1.4).Within(1e-4));
            Assert.That(def2.SillHeight.ValueMetricMeters, Is.EqualTo(0.4).Within(1e-4));

            Assert.That(proj.SurveyHostedObjectCalls[1].Location.ValueMetricMeters, Is.EqualTo(2.4).Within(1e-4));
            Assert.That(proj.SurveyHostedObjectCalls[2].Location.ValueMetricMeters, Is.EqualTo(2.3).Within(1e-4));
        }

        [Test]
        public void Synthetic_AbsoluteWall_WithDoor_HostsViaEngine_WithHingeAndSwing()
        {
            // One absolute wall (2,0)->(5,0) carrying a single DOOR opening 1.0 m along it.
            var door = new MappedOpening(
                LocationFromNearEndMeters: 1.0,
                WidthMeters: 0.9,
                HeightMeters: 2.1,
                SillHeightMeters: 0.0,
                Kind: OpeningKind.Door,
                HingeSide: HingeSideKind.Left,
                SwingDirection: SwingDirectionKind.Out,
                Facing: FacingRelationKind.Interior,
                FamilyDefIdx: 0);

            var wall = new MappedWall(
                Index: 0, IsFirstWall: true, LengthMeters: 3.0, ThicknessMeters: 0.2,
                CornerAngleRad: 0.0, CornerAngleSnapped: false, LevelOffsetMeters: 0.0,
                Openings: new[] { door },
                Start: new MappedPoint2D(2.0, 0.0), End: new MappedPoint2D(5.0, 0.0));

            var level = new MappedLevel(
                Floor2FloorMeters: 3.0, Floor2CeilingMeters: 2.6, HasDropCeiling: false,
                DropCeilingHeightMeters: 0.0, TypicalWindowSillMeters: 0.9, TypicalWindowHeadMeters: 2.1,
                Walls: new[] { wall });

            var building = new MappedBuilding(
                SurveyOrientationKind.NotFlipped, CompassDirectionKind.North, new[] { level });

            var proj = new RecordingProjectModel();
            ImportReport result = new AiModelImporter().Import(proj, building);

            Assert.That(result.Committed, Is.True, result.ToString());
            Assert.That(proj.CreateWallAbsoluteCalls.Count, Is.EqualTo(1));
            Assert.That(result.OpeningsPlaced, Is.EqualTo(1));

            // Hosted through the engine, not CreateOpeningOnWall.
            Assert.That(proj.CreateOpeningOnWallCalls.Count, Is.EqualTo(0));
            Assert.That(proj.SurveyHostedObjectCalls.Count, Is.EqualTo(1));

            // Activated the right wall; exterior scan (NotFlipped -> interiorFace false); near end = start.
            RecordingProjectModel.ActivateCall act = proj.ActivateWallByIdCalls.Single();
            Assert.That(act.WallId, Is.EqualTo(proj.CreateWallAbsoluteCalls[0].ReturnedId));
            Assert.That(act.InteriorFace, Is.False, "NotFlipped scan surveys the exterior face");
            Assert.That(act.NearEndIsStart, Is.True);

            // Door routed to the Doors category with hinge (lateral) + swing (facing) applied.
            WallHostedObjectSurveyParams hp = proj.SurveyHostedObjectCalls.Single();
            Assert.That(hp.FamilyCategory.IsDoors(), Is.True, "the door routes to the Doors category");
            SurveyFamilyDef def = hp.ProjectSurveyFamilyDefs[hp.SurveyFamilyDefIdx];
            Assert.That(def.LateralOrientation, Is.EqualTo((int)HingeSideKind.Left), "hinge side = Left");
            Assert.That(def.FacingOrientation, Is.EqualTo((int)SwingDirectionKind.Out), "swing = Out");
            Assert.That(hp.Location.ValueMetricMeters, Is.EqualTo(1.0).Within(1e-6));
            Assert.That(def.Width.ValueMetricMeters, Is.EqualTo(0.9).Within(1e-4));
            Assert.That(def.Height.ValueMetricMeters, Is.EqualTo(2.1).Within(1e-4));
        }

        // Build a single absolute wall (0,0)->(lengthM,0) carrying one window at the given location/width.
        private static MappedBuilding OneWallWithWindow(double lengthM, double locationM, double widthM)
        {
            var window = new MappedOpening(
                LocationFromNearEndMeters: locationM, WidthMeters: widthM, HeightMeters: 1.4,
                SillHeightMeters: 0.4, Kind: OpeningKind.Window, HingeSide: HingeSideKind.Left,
                SwingDirection: SwingDirectionKind.Out, Facing: FacingRelationKind.Interior, FamilyDefIdx: 0);
            var wall = new MappedWall(
                Index: 0, IsFirstWall: true, LengthMeters: lengthM, ThicknessMeters: 0.2,
                CornerAngleRad: 0.0, CornerAngleSnapped: false, LevelOffsetMeters: 0.0,
                Openings: new[] { window },
                Start: new MappedPoint2D(0.0, 0.0), End: new MappedPoint2D(lengthM, 0.0));
            var level = new MappedLevel(
                Floor2FloorMeters: 3.0, Floor2CeilingMeters: 2.6, HasDropCeiling: false,
                DropCeilingHeightMeters: 0.0, TypicalWindowSillMeters: 0.9, TypicalWindowHeadMeters: 2.1,
                Walls: new[] { wall });
            return new MappedBuilding(SurveyOrientationKind.Flipped, CompassDirectionKind.North, new[] { level });
        }

        [Test]
        public void Absolute_OpeningPastWallEnd_IsSkipped_NotPlaced()
        {
            // #5: an opening 5.0 m along a 3.0 m wall cannot fit -> skipped before activation, import still commits.
            var proj = new RecordingProjectModel();
            ImportReport result = new AiModelImporter().Import(proj, OneWallWithWindow(3.0, 5.0, 1.0));

            Assert.That(result.Committed, Is.True, result.ToString());
            Assert.That(result.WallsPlaced, Is.EqualTo(1));
            Assert.That(result.OpeningsPlaced, Is.EqualTo(0));
            Assert.That(result.Skipped.Count, Is.EqualTo(1));
            Assert.That(result.Skipped[0], Does.Contain("outside").Or.Contain("extends past"));
            Assert.That(proj.ActivateWallByIdCalls.Count, Is.EqualTo(0), "out-of-bounds is rejected before activation");
            Assert.That(proj.SurveyHostedObjectCalls.Count, Is.EqualTo(0));
        }

        [Test]
        public void Absolute_NoPKNailFamily_FallsBackToDirectHosting_WithWarning()
        {
            // On a stock template (no PKNAIL survey families), the engine flow declines; the absolute path
            // falls back to direct hosting so the 3 windows still appear, each with a warning that the AI
            // orientation could not be applied.
            var proj = new RecordingProjectModel { ProvideSurveyFamilyDefs = false };
            ImportReport result = new AiModelImporter().Import(proj, LoadMappedRealV2());

            Assert.That(result.Committed, Is.True, result.ToString());
            Assert.That(result.OpeningsPlaced, Is.EqualTo(3), "windows still placed via the fallback");
            Assert.That(result.Skipped.Count, Is.EqualTo(0), "fallback succeeded, so nothing skipped");
            Assert.That(proj.CreateOpeningOnWallCalls.Count, Is.EqualTo(3), "fell back to direct hosting");
            Assert.That(result.Warnings.Count, Is.EqualTo(3), "each fallback notes orientation was not applied");
            Assert.That(result.Warnings, Has.All.Contain("no PKNAIL survey family"));
        }

        [Test]
        public void Absolute_OpeningHostingFails_IsSkipped_ImportStillCommits()
        {
            // Best-effort on the absolute path: a hosting failure rolls back only that opening's inner
            // transaction (AbortPendingTransaction) and is recorded — the wall and the import survive.
            var proj = new RecordingProjectModel
            {
                FailAtStep = "SurveyWallHostedObject",
                FailWith = CommandResult.TransactionError,
            };

            ImportReport result = new AiModelImporter().Import(proj, OneWallWithWindow(4.0, 2.0, 1.0));

            Assert.That(result.Committed, Is.True, "one bad opening must NOT roll back the whole absolute import");
            Assert.That(result.WallsPlaced, Is.EqualTo(1));
            Assert.That(result.OpeningsPlaced, Is.EqualTo(0));
            Assert.That(result.Skipped.Count, Is.EqualTo(1));
            Assert.That(proj.AbortPendingTransactionCount, Is.GreaterThanOrEqualTo(1), "the failed opening's txn is discarded");
            Assert.That(proj.RollBackTransactionCount, Is.EqualTo(0), "the group is not rolled back");
            Assert.That(proj.CommitTransactionCount, Is.EqualTo(1));
        }

        [Test]
        public void Absolute_WallPlacementFails_RollsBackWholeImport()
        {
            // A hard WALL failure on the primary v2 path is atomic: the whole import rolls back (walls are
            // the core; unlike best-effort openings). Parallels the v1 forced-failure test for the v2 path.
            var proj = new RecordingProjectModel { FailAtStep = "CreateWallAbsolute", FailWith = CommandResult.Failed };

            ImportReport result = new AiModelImporter().Import(proj, LoadMappedRealV2());

            Assert.That(result.Committed, Is.False);
            Assert.That(result.RolledBack, Is.True);
            Assert.That(result.HasFailures, Is.True);
            Assert.That(proj.CommitTransactionCount, Is.EqualTo(0));
            Assert.That(proj.RollBackTransactionCount, Is.EqualTo(1));
            Assert.That(result.WallsPlaced, Is.EqualTo(0), "the first wall failed, so none were placed");
        }

        [Test]
        public void Absolute_MeteringVetoStopsAndRollsBack()
        {
            // Per Q12, a per-element metering veto stops the import and rolls the whole group back on the
            // primary v2 path too. Allow 3 placements, then veto.
            var proj = new RecordingProjectModel();
            int budget = 3;
            Func<bool> recordUsage = () => budget-- > 0;

            ImportReport result = new AiModelImporter().Import(proj, LoadMappedRealV2(), recordUsage);

            Assert.That(result.MeteringStopped, Is.True);
            Assert.That(result.Committed, Is.False);
            Assert.That(result.RolledBack, Is.True);
            Assert.That(proj.CreateWallAbsoluteCalls.Count, Is.EqualTo(3), "only 3 placements were allowed");
            Assert.That(proj.CommitTransactionCount, Is.EqualTo(0));
        }
    }
}
