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
    /// Regression for the known-good demo (`ai_engine_pknail_results.demo.json`) — the clean 5x4 m room
    /// that imported successfully end-to-end in Revit 2026. It is a v1-format input (length + corner angle,
    /// NO absolute endpoints), so it stays on the proven SURVEY-CHAIN path. Drives the full bridge against
    /// the recording fake to lock in: 4 walls placed in order with right-angle corners, the window placed,
    /// and one committed transaction group.
    /// </summary>
    [TestFixture]
    public class DemoKnownGoodImportTests
    {
        private static string DemoPath =>
            Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "ai_engine_pknail_results.demo.json");

        private static MappedBuilding LoadMappedDemo()
        {
            DetectedBuilding building = AiModelReader.Load(DemoPath);
            Assert.That(AiModelValidator.Validate(building).IsValid, Is.True, "Demo fixture should validate.");
            return AiModelMapper.Map(building);
        }

        [Test]
        public void Demo_Import_PlacesFourWallsAndOneWindow_Committed()
        {
            var proj = new RecordingProjectModel();
            ImportReport result = new AiModelImporter().Import(proj, LoadMappedDemo());

            Assert.That(result.Committed, Is.True, result.ToString());
            Assert.That(result.WallsPlaced, Is.EqualTo(4));
            Assert.That(result.OpeningsPlaced, Is.EqualTo(1));     // the fake supplies a family def
            Assert.That(proj.SurveyWallCalls.Count, Is.EqualTo(4));
            Assert.That(proj.CreateWallAbsoluteCalls.Count, Is.EqualTo(0), "v1 demo stays on the survey chain");
            Assert.That(proj.SurveyHostedObjectCalls.Count, Is.EqualTo(1));
            Assert.That(proj.CommitTransactionCount, Is.EqualTo(1));
            Assert.That(proj.RollBackTransactionCount, Is.EqualTo(0));
        }

        [Test]
        public void Demo_Import_FeedsRightAngleCornersToTheEngine()
        {
            var proj = new RecordingProjectModel();
            new AiModelImporter().Import(proj, LoadMappedDemo());

            // First wall has no corner; walls 1..3 are 90° inside corners (the engine convention).
            Assert.That((double)proj.SurveyWallCalls[0].CornerAngle, Is.EqualTo(0.0).Within(1e-12));
            for (int i = 1; i < 4; i++)
                Assert.That((double)proj.SurveyWallCalls[i].CornerAngle, Is.EqualTo(Math.PI / 2.0).Within(1e-9),
                    $"wall {i} should be fed a 90° corner");
        }

        [Test]
        public void Demo_Import_FirstWallLengthConvertedToFeet()
        {
            var proj = new RecordingProjectModel();
            new AiModelImporter().Import(proj, LoadMappedDemo());

            // Wall 0 length 5.0 m round-trips through Distance.
            Assert.That(proj.SurveyWallCalls[0].Length.ValueMetricMeters, Is.EqualTo(5.0).Within(1e-4));
            Assert.That(proj.SurveyWallCalls[0].Width.ValueMetricMeters, Is.EqualTo(0.2).Within(1e-4));
        }
    }
}
