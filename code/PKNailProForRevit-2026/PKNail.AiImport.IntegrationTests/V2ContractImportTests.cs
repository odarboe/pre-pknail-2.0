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
    /// End-to-end (fake-driven) check that the v2 contract — absolute wall endpoints — imports through the
    /// DIRECT-absolute placement path: each wall is placed at its real (re-based) plan coordinates via
    /// <c>CreateWallAbsolute</c> rather than chained through the survey engine. The v2 example is a
    /// connected 5.05 x 4.0 m room; 4 walls placed at absolute coords, its 2 openings hosted directly on
    /// their walls via <c>CreateOpeningOnWall</c>, one committed group.
    /// </summary>
    [TestFixture]
    public class V2ContractImportTests
    {
        private static string V2Path =>
            Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "ai_engine_pknail_results.v2.example.json");

        private static MappedBuilding LoadMappedV2()
        {
            DetectedBuilding building = AiModelReader.Load(V2Path);
            Assert.That(AiModelValidator.Validate(building).IsValid, Is.True, "v2 example should validate.");
            return AiModelMapper.Map(building);
        }

        [Test]
        public void V2_Import_PlacesFourWallsDirectly_Committed()
        {
            var proj = new RecordingProjectModel();
            ImportReport result = new AiModelImporter().Import(proj, LoadMappedV2());

            Assert.That(result.Committed, Is.True, result.ToString());
            Assert.That(result.WallsPlaced, Is.EqualTo(4));
            Assert.That(proj.CreateWallAbsoluteCalls.Count, Is.EqualTo(4), "direct absolute placement, not survey chain");
            Assert.That(proj.SurveyWallCalls.Count, Is.EqualTo(0));
            Assert.That(result.OpeningsPlaced, Is.EqualTo(2), "openings hosted on their walls via the engine flow");
            // Openings host through the proven 1.0 engine flow: activate the wall, then SurveyWallHostedObject.
            Assert.That(proj.ActivateWallByIdCalls.Count, Is.EqualTo(2), "each host wall activated first");
            Assert.That(proj.SurveyHostedObjectCalls.Count, Is.EqualTo(2), "hosted via SurveyWallHostedObject");
            Assert.That(proj.CreateOpeningOnWallCalls.Count, Is.EqualTo(0), "the simplified path is not used");
            Assert.That(proj.CommitTransactionCount, Is.EqualTo(1));
            Assert.That(proj.RollBackTransactionCount, Is.EqualTo(0));
        }

        [Test]
        public void V2_Import_PlacesWallsAtAbsoluteCoordinates()
        {
            var proj = new RecordingProjectModel();
            new AiModelImporter().Import(proj, LoadMappedV2());

            // Room already sits at the origin (min X/Y = 0), so re-basing is a no-op.
            // Wall 0 spans (0,0)->(5.05,0).
            RecordingProjectModel.AbsoluteWallCall w0 = proj.CreateWallAbsoluteCalls[0];
            Assert.That(w0.StartXMeters, Is.EqualTo(0.0).Within(1e-4));
            Assert.That(w0.StartYMeters, Is.EqualTo(0.0).Within(1e-4));
            Assert.That(w0.EndXMeters, Is.EqualTo(5.05).Within(1e-4));
            Assert.That(w0.EndYMeters, Is.EqualTo(0.0).Within(1e-4));
            Assert.That(w0.Thickness.ValueMetricMeters, Is.EqualTo(0.2).Within(1e-4));
        }

        [Test]
        public void V2_Import_WindowAndDoorHostedViaEngine_OnCorrectWalls()
        {
            var proj = new RecordingProjectModel();
            ImportReport result = new AiModelImporter().Import(proj, LoadMappedV2());

            Assert.That(result.OpeningsPlaced, Is.EqualTo(2), "window on wall 2 + door on wall 3, both hosted");
            Assert.That(result.Skipped.Count, Is.EqualTo(0), "no openings skipped");
            Assert.That(proj.SurveyHostedObjectCalls.Count, Is.EqualTo(2));
            Assert.That(proj.CreateOpeningOnWallCalls.Count, Is.EqualTo(0));

            // Wall 2 = Window, wall 3 = Door. CreateWallAbsoluteCalls[i] is the returned id for wall i;
            // openings host in wall order, so activation 0 -> wall 2, activation 1 -> wall 3.
            long wall2Id = proj.CreateWallAbsoluteCalls[2].ReturnedId;
            long wall3Id = proj.CreateWallAbsoluteCalls[3].ReturnedId;
            Assert.That(proj.ActivateWallByIdCalls.Select(a => a.WallId), Is.EqualTo(new[] { wall2Id, wall3Id }));

            WallHostedObjectSurveyParams window = proj.SurveyHostedObjectCalls[0];
            WallHostedObjectSurveyParams door = proj.SurveyHostedObjectCalls[1];

            Assert.That(window.FamilyCategory.IsWindows(), Is.True, "wall 2 opening is a window");
            Assert.That(door.FamilyCategory.IsDoors(), Is.True, "wall 3 opening is a door");
            Assert.That(window.Location.ValueMetricMeters, Is.EqualTo(2.525).Within(1e-4));
            Assert.That(door.Location.ValueMetricMeters, Is.EqualTo(1.2).Within(1e-4));
        }
    }
}
