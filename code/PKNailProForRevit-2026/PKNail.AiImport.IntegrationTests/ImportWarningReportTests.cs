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
    /// Epic 7 (validation / human-in-the-loop): Revit warnings raised during placement (e.g. "insert
    /// conflicts with joined wall") must be COLLECTED into the import report — not surfaced as blocking
    /// modal dialogs. The importer opens a warning-capture scope for the whole placement and drains the
    /// collected warnings into <see cref="ImportReport.Warnings"/> on every exit path. The fake stands in
    /// for Revit's failure pipeline by returning canned warnings from <c>EndImportWarningCapture</c>.
    /// </summary>
    [TestFixture]
    public class ImportWarningReportTests
    {
        private static string V2Path =>
            Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "ai_engine_pknail_results.v2.example.json");

        private static MappedBuilding LoadMappedV2()
        {
            DetectedBuilding building = AiModelReader.Load(V2Path);
            return AiModelMapper.Map(building);
        }

        [Test]
        public void Import_OpensAndDrainsExactlyOneWarningCaptureScope()
        {
            var proj = new RecordingProjectModel();
            new AiModelImporter().Import(proj, LoadMappedV2());

            Assert.That(proj.BeginImportWarningCaptureCount, Is.EqualTo(1), "capture opened once");
            Assert.That(proj.EndImportWarningCaptureCount, Is.GreaterThanOrEqualTo(1), "capture drained");

            // The scope opens after StartTransaction and drains before CommitTransaction.
            int begin = proj.Calls.IndexOf("BeginImportWarningCapture");
            int end = proj.Calls.IndexOf("EndImportWarningCapture");
            int start = proj.Calls.IndexOf("StartTransaction");
            int commit = proj.Calls.IndexOf("CommitTransaction");
            Assert.That(begin, Is.GreaterThan(start), "capture begins after the group is open");
            Assert.That(end, Is.LessThan(commit), "warnings drained before commit");
        }

        [Test]
        public void Import_CollectedWarnings_LandInReport_WithoutStoppingTheImport()
        {
            var proj = new RecordingProjectModel();
            proj.WarningsToReturn.Add("Insert conflicts with joined Wall.");
            proj.WarningsToReturn.Add("Highlighted walls overlap.");

            ImportReport report = new AiModelImporter().Import(proj, LoadMappedV2());

            Assert.That(report.Committed, Is.True, "warnings must not roll the import back");
            Assert.That(report.HasWarnings, Is.True);
            Assert.That(report.Warnings.Count, Is.EqualTo(2));
            Assert.That(report.Warnings, Does.Contain("Insert conflicts with joined Wall."));
            Assert.That(report.HasFailures, Is.False, "a warning is not a failure");
            Assert.That(report.Skipped.Count, Is.EqualTo(0), "a warning is not a skip");
        }

        [Test]
        public void Report_ToString_ShowsWarningCountAndText()
        {
            var proj = new RecordingProjectModel();
            proj.WarningsToReturn.Add("Insert conflicts with joined Wall.");

            string text = new AiModelImporter().Import(proj, LoadMappedV2()).ToString();

            Assert.That(text, Does.Contain("warnings=1"));
            Assert.That(text, Does.Contain("~ Insert conflicts with joined Wall."));
        }

        [Test]
        public void Import_NoWarnings_ReportsZeroAndStillCommits()
        {
            var proj = new RecordingProjectModel();
            ImportReport report = new AiModelImporter().Import(proj, LoadMappedV2());

            Assert.That(report.Committed, Is.True);
            Assert.That(report.HasWarnings, Is.False);
            Assert.That(report.Warnings.Count, Is.EqualTo(0));
            Assert.That(report.ToString(), Does.Contain("warnings=0"));
        }

        [Test]
        public void Import_Warnings_AreDrainedEvenWhenTheImportIsRolledBack()
        {
            // Force the wall placement to fail so the import vetoes and rolls back; warnings collected up to
            // that point must still reach the report (drained on the rollback path, not lost).
            var proj = new RecordingProjectModel { FailAtStep = "CreateWallAbsolute", FailWith = CommandResult.Failed };
            proj.WarningsToReturn.Add("Highlighted walls overlap.");

            ImportReport report = new AiModelImporter().Import(proj, LoadMappedV2());

            Assert.That(report.Committed, Is.False);
            Assert.That(report.RolledBack, Is.True);
            Assert.That(report.Warnings, Does.Contain("Highlighted walls overlap."), "warnings survive a rollback");
        }
    }
}
