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
    /// Epic 9b — drives the Epic 3 <see cref="AiModelImporter"/> against a recording fake
    /// <see cref="IProjectModel"/> using the frozen JSON contract. No Revit runtime; no Disto/DrivableForm.
    /// </summary>
    [TestFixture]
    public class AiModelImporterTests
    {
        private const double FeetPerMeter = 3.2808399;

        private static string FrozenContractPath =>
            Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "ai_engine_pknail_results.json");

        private static MappedBuilding LoadMappedSample()
        {
            DetectedBuilding building = AiModelReader.Load(FrozenContractPath);
            ValidationReport report = AiModelValidator.Validate(building);
            Assert.That(report.IsValid, Is.True, "Frozen fixture should validate without errors.");
            return AiModelMapper.Map(building);
        }

        [Test]
        public void Import_Sample_DrivesNineWallsThenOneOpening_InOrder()
        {
            var proj = new RecordingProjectModel();
            ImportReport result = new AiModelImporter().Import(proj, LoadMappedSample());

            Assert.That(result.Committed, Is.True, result.ToString());
            Assert.That(proj.SurveyWallCalls.Count, Is.EqualTo(9), "Sample has 9 walls.");
            Assert.That(proj.SurveyHostedObjectCalls.Count, Is.EqualTo(1), "Sample has 1 opening (a window).");

            // 9 walls + 1 opening drive 10 placement calls total, in document order. The single window
            // hosts on wall index 2 (the 3rd wall), so it is correctly emitted immediately after the 3rd
            // SurveyWall — i.e. openings interleave with their host wall, not appended at the very end.
            var driverCalls = proj.Calls
                .Where(c => c == "SurveyWall" || c == "SurveyWallHostedObject")
                .ToList();
            Assert.That(driverCalls.Count, Is.EqualTo(10));
            Assert.That(driverCalls.Count(c => c == "SurveyWall"), Is.EqualTo(9));
            Assert.That(driverCalls.Count(c => c == "SurveyWallHostedObject"), Is.EqualTo(1));

            // The opening immediately follows its host wall (after exactly 3 walls).
            int openingIdx = driverCalls.IndexOf("SurveyWallHostedObject");
            Assert.That(openingIdx, Is.EqualTo(3), "window hosts on wall index 2, placed right after the 3rd wall");
            Assert.That(driverCalls.Take(3), Is.All.EqualTo("SurveyWall"));

            Assert.That(result.WallsPlaced, Is.EqualTo(9));
            Assert.That(result.OpeningsPlaced, Is.EqualTo(1));
        }

        [Test]
        public void Import_Window_DimsArriveConvertedToFeet()
        {
            var proj = new RecordingProjectModel();
            new AiModelImporter().Import(proj, LoadMappedSample());

            WallHostedObjectSurveyParams hp = proj.SurveyHostedObjectCalls.Single();
            SurveyFamilyDef def = hp.ProjectSurveyFamilyDefs[hp.SurveyFamilyDefIdx];

            // Window in the fixture: width 1.0 m, height 1.4 m, sill 0.4 m, location 4.0 m.
            Assert.That(def.Width.ValueMetricMeters, Is.EqualTo(1.0).Within(1e-4), "width 1.0 m");
            Assert.That(def.Width.ValueImperial, Is.EqualTo(FeetPerMeter).Within(1e-4), "width ~3.2808399 ft");
            Assert.That(def.Height.ValueMetricMeters, Is.EqualTo(1.4).Within(1e-4), "height 1.4 m");
            Assert.That(def.SillHeight.ValueMetricMeters, Is.EqualTo(0.4).Within(1e-4), "sill 0.4 m");
            Assert.That(hp.Location.ValueMetricMeters, Is.EqualTo(4.0).Within(1e-4), "location 4.0 m");
        }

        [Test]
        public void Import_Wall_DimsArriveConvertedToFeet()
        {
            var proj = new RecordingProjectModel();
            new AiModelImporter().Import(proj, LoadMappedSample());

            // First wall in the fixture: length 5.05 m, thickness 0.2 m.
            WallSurveyParams w0 = proj.SurveyWallCalls[0];
            Assert.That(w0.Length.ValueMetricMeters, Is.EqualTo(5.05).Within(1e-4));
            Assert.That(w0.Width.ValueMetricMeters, Is.EqualTo(0.2).Within(1e-4));
            Assert.That(w0.IsFirstWall, Is.True);
        }

        [Test]
        public void Import_AssignsLevelsData_WithFloor2CeilingNear2566mm()
        {
            var proj = new RecordingProjectModel();
            new AiModelImporter().Import(proj, LoadMappedSample());

            Assert.That(proj.AssignedLevelsData, Is.Not.Null);
            LevelsData levels = proj.AssignedLevelsData!;
            Assert.That(levels.Levels.Count, Is.EqualTo(1));

            LevelDataItem level = levels.Levels[0];
            Assert.That(level.Floor2CeilingElev.ValueMetricMeters, Is.EqualTo(2.566).Within(1e-3),
                "Floor-to-ceiling ~2.566 m");
            Assert.That(level.Floor2FloorElev.ValueMetricMeters, Is.EqualTo(2.866).Within(1e-3));

            // Typical window heights: head 2.1 m, sill 0.9 m => height 1.2 m.
            Assert.That(level.TypicalWindowHeights.Count, Is.EqualTo(1));
            WindowHeights wh = level.TypicalWindowHeights[0];
            Assert.That(wh.SillHeight.ValueMetricMeters, Is.EqualTo(0.9).Within(1e-3));
            Assert.That(wh.Height.ValueMetricMeters, Is.EqualTo(1.2).Within(1e-3));
            Assert.That(wh.HeadHeight.ValueMetricMeters, Is.EqualTo(2.1).Within(1e-3));
        }

        [Test]
        public void Import_Sample_CommitsExactlyOneTransactionGroup()
        {
            var proj = new RecordingProjectModel();
            new AiModelImporter().Import(proj, LoadMappedSample());

            Assert.That(proj.InitializeTransactionCount, Is.EqualTo(1));
            Assert.That(proj.StartTransactionCount, Is.EqualTo(1));
            Assert.That(proj.CommitTransactionCount, Is.EqualTo(1));
            Assert.That(proj.RollBackTransactionCount, Is.EqualTo(0));
            Assert.That(proj.TransactionName, Is.EqualTo("Import AI Model"));
        }

        [Test]
        public void Import_ForcedFailure_RollsBackAndDoesNotCommit()
        {
            var proj = new RecordingProjectModel
            {
                FailAtStep = "SurveyWall",        // fail on the first SurveyWall placement
                FailWith = CommandResult.Failed,
            };

            ImportReport result = new AiModelImporter().Import(proj, LoadMappedSample());

            Assert.That(result.Committed, Is.False);
            Assert.That(result.RolledBack, Is.True);
            Assert.That(proj.CommitTransactionCount, Is.EqualTo(0), "must not commit on failure");
            Assert.That(proj.RollBackTransactionCount, Is.EqualTo(1), "must roll back exactly once");
            Assert.That(result.HasFailures, Is.True);
        }

        [Test]
        public void Import_TransactionError_RollsBack()
        {
            var proj = new RecordingProjectModel
            {
                FailAtStep = "SurveyWallHostedObject",
                FailWith = CommandResult.TransactionError,
            };

            ImportReport result = new AiModelImporter().Import(proj, LoadMappedSample());

            Assert.That(result.Committed, Is.False);
            Assert.That(result.RolledBack, Is.True);
            Assert.That(proj.RollBackTransactionCount, Is.EqualTo(1));
        }

        [Test]
        public void Import_RecordUsageReturnsFalseAfterN_StopsAndReportsPartial()
        {
            var proj = new RecordingProjectModel();

            int budget = 3; // allow 3 placements, then veto.
            Func<bool> recordUsage = () => budget-- > 0;

            ImportReport result = new AiModelImporter().Import(proj, LoadMappedSample(), recordUsage);

            Assert.That(result.MeteringStopped, Is.True, "metering should have stopped the import");
            Assert.That(result.Committed, Is.False);
            Assert.That(result.RolledBack, Is.True, "default Q12 behaviour is atomic rollback");
            Assert.That(proj.SurveyWallCalls.Count, Is.EqualTo(3), "only the first 3 walls were placed");
            Assert.That(proj.CommitTransactionCount, Is.EqualTo(0));
            Assert.That(result.Skipped, Is.Not.Empty);
        }

        [Test]
        public void Import_OnlyTouchesIProjectModel_NoDistoOrDrivableForm()
        {
            // The importer must drive nothing but IProjectModel. We assert that the concrete Revit/UI types
            // are never instantiated during import by confirming the fake (which constructs none of them)
            // fully satisfies the importer. If the importer reached for DrivableForm/Disto it could not
            // compile against the injected IProjectModel and this test project would not build.
            var proj = new RecordingProjectModel();
            ImportReport result = new AiModelImporter().Import(proj, LoadMappedSample());

            Assert.That(result.Committed, Is.True);

            // Sanity: the importer never invoked any non-import driver surface (lines, RF, copy, etc.).
            string[] forbidden = { "SurveyLine", "SurveyLinePrep", "CopyObjects", "ConvertUnknownWidthWalls",
                                   "SurveyRFCommandMappedObjectPrep", "ActivateWall" };
            foreach (string f in forbidden)
            {
                Assert.That(proj.Calls, Has.None.EqualTo(f), $"importer should not call {f}");
            }
        }
    }
}
