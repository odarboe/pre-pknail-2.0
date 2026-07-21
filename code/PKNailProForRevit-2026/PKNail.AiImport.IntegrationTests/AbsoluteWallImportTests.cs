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
    /// chain). The importer must drive <c>CreateWallAbsolute</c> (never <c>SurveyWall</c>), re-base the
    /// coordinates near the project origin, host each opening directly on its wall via
    /// <c>CreateOpeningOnWall</c>, and commit once.
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
        public void RealV2_Import_ThreeWindowsHostedDirectlyOnWalls_Committed()
        {
            var proj = new RecordingProjectModel();
            ImportReport result = new AiModelImporter().Import(proj, LoadMappedRealV2());

            // 3 windows (walls 2, 6, 7) each hosted directly on their wall via CreateOpeningOnWall.
            Assert.That(result.OpeningsPlaced, Is.EqualTo(3), result.ToString());
            Assert.That(result.Skipped.Count, Is.EqualTo(0), "no openings skipped");
            Assert.That(proj.CreateOpeningOnWallCalls.Count, Is.EqualTo(3));
            Assert.That(proj.Calls.Count(c => c == "CreateOpeningOnWall"), Is.EqualTo(3));

            // The survey-chain opening path must never be touched on the absolute path.
            Assert.That(proj.SurveyHostedObjectCalls.Count, Is.EqualTo(0));

            // Committed once, no rollback.
            Assert.That(result.Committed, Is.True);
            Assert.That(proj.CommitTransactionCount, Is.EqualTo(1));
            Assert.That(proj.RollBackTransactionCount, Is.EqualTo(0));

            // All 3 are windows (isDoor == false).
            Assert.That(proj.CreateOpeningOnWallCalls.All(o => o.IsDoor == false), Is.True, "all 3 are windows");
        }

        [Test]
        public void RealV2_Import_OpeningsHostedOnCorrectWallIds_WithCorrectLocations()
        {
            var proj = new RecordingProjectModel();
            new AiModelImporter().Import(proj, LoadMappedRealV2());

            // The walls carrying openings are model indices 2, 6 and 7. CreateWallAbsoluteCalls is in wall
            // order, so its [i] entry is the returned element id for wall i.
            long wall2Id = proj.CreateWallAbsoluteCalls[2].ReturnedId;
            long wall6Id = proj.CreateWallAbsoluteCalls[6].ReturnedId;
            long wall7Id = proj.CreateWallAbsoluteCalls[7].ReturnedId;

            var onWall2 = proj.CreateOpeningOnWallCalls.Single(o => o.WallElementId == wall2Id);
            var onWall6 = proj.CreateOpeningOnWallCalls.Single(o => o.WallElementId == wall6Id);
            var onWall7 = proj.CreateOpeningOnWallCalls.Single(o => o.WallElementId == wall7Id);

            // Location is measured along the host wall from its start; 3.8 m on wall 2.
            Assert.That(onWall2.LocationFromStartMeters, Is.EqualTo(3.8).Within(1e-4));
            Assert.That(onWall2.Width.ValueMetricMeters, Is.EqualTo(1.0).Within(1e-4));
            Assert.That(onWall2.Height.ValueMetricMeters, Is.EqualTo(1.4).Within(1e-4));
            Assert.That(onWall2.SillHeight.ValueMetricMeters, Is.EqualTo(0.4).Within(1e-4));

            Assert.That(onWall6.LocationFromStartMeters, Is.EqualTo(2.4).Within(1e-4));
            Assert.That(onWall7.LocationFromStartMeters, Is.EqualTo(2.3).Within(1e-4));

            // No opening should reference a wall that has none.
            Assert.That(proj.CreateOpeningOnWallCalls.Select(o => o.WallElementId),
                        Is.EquivalentTo(new[] { wall2Id, wall6Id, wall7Id }));
        }

        [Test]
        public void Synthetic_AbsoluteWall_WithDoor_HostsDoorViaCreateOpeningOnWall()
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
            Assert.That(proj.CreateOpeningOnWallCalls.Count, Is.EqualTo(1));

            RecordingProjectModel.OpeningCall call = proj.CreateOpeningOnWallCalls[0];
            Assert.That(call.IsDoor, Is.True, "the door must route with isDoor: true");
            Assert.That(call.WallElementId, Is.EqualTo(proj.CreateWallAbsoluteCalls[0].ReturnedId));
            Assert.That(call.LocationFromStartMeters, Is.EqualTo(1.0).Within(1e-6));
            Assert.That(call.Width.ValueMetricMeters, Is.EqualTo(0.9).Within(1e-4));
            Assert.That(call.Height.ValueMetricMeters, Is.EqualTo(2.1).Within(1e-4));
        }
    }
}
