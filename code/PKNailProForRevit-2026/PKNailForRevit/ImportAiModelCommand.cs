#nullable enable

using System;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using PKNail.AiImport.DirectBuild;
using PKNail.AiImport.Model;
using PKNail.AiImport.Mapping;
using PKNailForRevit.AiImport;

namespace PKNailForRevit
{
    /// <summary>
    /// Epic 3 ribbon entry point — "Import AI Model (JSON)". Picks an AI-engine JSON file, validates and
    /// plans it (Revit-free), then drives a live <see cref="RevitDocument"/>:
    /// <list type="bullet">
    /// <item>files whose walls carry coordinates (contract v2/v2.1) go through Epic 13 <b>Direct Build</b>
    /// (<see cref="DirectBuildImporter"/>): native walls/openings exactly as the JSON describes them;</item>
    /// <item>v1 files (length + corner-angle chain, no coordinates) keep the survey-chain path
    /// (<see cref="AiModelImporter"/>).</item>
    /// </list>
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class ImportAiModelCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cmdData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = cmdData.Application.ActiveUIDocument;
            if (uiDoc == null || uiDoc.Document == null || uiDoc.Document.IsFamilyDocument)
            {
                message = @"Open a (non-family) Revit project before importing an AI model.";
                return Result.Failed;
            }

            string path;
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = @"Import AI Model (JSON)";
                ofd.Filter = @"AI engine results (*.json)|*.json|All files (*.*)|*.*";
                ofd.CheckFileExists = true;
                if (ofd.ShowDialog() != DialogResult.OK) { return Result.Cancelled; }
                path = ofd.FileName;
            }

            DetectedBuilding building;
            try
            {
                building = AiModelReader.Load(path);
            }
            catch (Exception ex)
            {
                message = @"Could not read the AI model JSON: " + ex.Message;
                return Result.Failed;
            }

            // Epic 13: any wall with coordinates ⇒ Direct Build. There, a bad wall/opening is skipped and
            // reported instead of blocking the file, so only file-level problems stop the import.
            bool directBuild = building.Levels.SelectMany(l => l.Walls).Any(w => w.HasEndpoints);
            ValidationReport validation = AiModelValidator.Validate(building, elementIssuesAreWarnings: directBuild);
            if (!validation.IsValid)
            {
                var sb = new StringBuilder();
                sb.AppendLine(@"The AI model has validation errors and cannot be imported:").AppendLine();
                foreach (ValidationIssue issue in validation.Errors)
                { sb.Append(@" - ").AppendLine(issue.ToString()); }
                MessageBox.Show(sb.ToString(), @"Import AI Model");
                message = @"AI model validation failed.";
                return Result.Failed;
            }

            // Revit-free planning: Direct Build resolves every final coordinate up front; the v1 path maps
            // onto the survey-chain parameters.
            DirectBuildPlan? plan = directBuild ? DirectBuildPlanner.Plan(building) : null;
            MappedBuilding? mapped = directBuild ? null : AiModelMapper.Map(building);

            // Concrete at the composition root: RevitDocument implements both the full IProjectModel (used
            // for EnsureDocumentParameters / HasPKNailWalls / DocumentSettings below) and the focused
            // IAiImportTarget the importer consumes.
            RevitDocument proj = new RevitDocument(cmdData.Application.Application, uiDoc.Document);

            // Re-import guard (Q5): don't silently append a second building onto existing PKNAIL content.
            if (proj.HasPKNailWalls())
            {
                DialogResult answer = MessageBox.Show(
                    @"This document already contains PKNAIL walls." + Environment.NewLine + Environment.NewLine +
                    @"Importing will ADD another building on top of the existing one. For a clean result, "
                    + @"import into an empty project instead." + Environment.NewLine + Environment.NewLine +
                    @"Append anyway?",
                    @"Import AI Model", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                if (answer != DialogResult.Yes) { return Result.Cancelled; }
            }

            // The engine's opening-hosting flow derives its context from the active floor-plan view's level,
            // so make a floor plan active before importing (imports are often launched from a 3D view). This
            // is a view change, not a document edit, so it must happen before any transaction is opened.
            // (Direct Build does not need it, but a plan view is also where the reviewer wants to land.)
            EnsureFloorPlanActive(uiDoc);

            // Ensure the PKNail project parameters exist (works on any document, not just a PKNail
            // template). Runs in its own transaction, before the import transaction group.
            string prepMsg = string.Empty;
            if (proj.EnsureDocumentParameters(ref prepMsg) != CommandResult.Succeeded)
            {
                MessageBox.Show(@"Could not prepare the document for PKNail import:" + Environment.NewLine + prepMsg,
                                @"Import AI Model");
                message = prepMsg;
                return Result.Failed;
            }

            // Epic 11e.3: a project with no PKNAIL door/window families (any stock Revit template) gets the
            // shipped ones, so the AI's door types (sliding, pocket, ...) can be built as such. Best-effort:
            // if the family files are missing the import uses the project's own families and says so.
            string familyNote;
            string familyMsg = string.Empty;
            bool familiesOk = proj.EnsureSurveyFamilies(out familyNote, ref familyMsg) == CommandResult.Succeeded;

            // The engine hosts openings using wall-face geometry derived from a default 3D view; a fresh
            // metric-template project may have none. Create one now so windows/doors can host (best-effort:
            // if this fails, walls still import and the openings report a clear skip reason). Direct Build
            // does not need it, but the 1.0 tools used on the imported walls afterwards (Activate Wall,
            // Capture) do, so it is created on both paths.
            string viewMsg = string.Empty;
            proj.EnsureDefault3DView(ref viewMsg);

            // Epic 10.3: default project metadata — name the PKNail project after the source file if it has
            // no name yet. Best-effort: metadata must never block the geometry import.
            try
            {
                DocumentSettings settings = proj.DocumentSettings;
                if (settings != null && string.IsNullOrEmpty(settings.ProjectName))
                {
                    settings.ProjectName = System.IO.Path.GetFileNameWithoutExtension(path);
                    proj.DocumentSettings = settings;
                }
            }
            catch { /* metadata is non-critical; proceed with the import regardless */ }

            // Per-element metering (Q4): record usage before each placed wall/opening via the app's license,
            // exactly like the 1.0 survey commands. In debug/bypass builds this is a no-op that returns true.
            Func<bool> recordUsage = TheApp.Instance.CreateImportUsageRecorder(proj);
            ImportReport report = directBuild
                ? new DirectBuildImporter().Import(proj, plan!, recordUsage)
                : new AiModelImporter().Import(proj, mapped!, recordUsage);

            if (!string.IsNullOrEmpty(familyNote)) { report.AddAdjustment(familyNote); }
            if (!familiesOk) { report.AddWarning(familyMsg + @"; the project's own door/window families were used."); }

            if (report.Committed)
            {
                // Epic 7: Revit warnings encountered during placement (e.g. "insert conflicts with joined
                // wall") were collected into the report, not shown as blocking dialogs — surface them here so
                // the reviewer sees exactly what happened without clicking through per-element prompts.
                string header = report.HasWarnings
                    ? $"Import committed with {report.Warnings.Count} warning(s) — review below."
                    : @"Import committed successfully.";
                MessageBox.Show(header + Environment.NewLine + Environment.NewLine + report.ToString(),
                                @"Import AI Model");
                return Result.Succeeded;
            }

            message = report.ToString();
            return Result.Failed;
        }

        // Make a floor-plan view active if one isn't already, so the engine can resolve its level context.
        // Prefers the lowest-level plan (where the imported walls are placed). No-op if a valid plan is active.
        private static void EnsureFloorPlanActive(UIDocument uiDoc)
        {
            if (uiDoc.ActiveView is ViewPlan cur && !cur.IsTemplate && cur.GenLevel != null) { return; }

            ViewPlan? plan = new FilteredElementCollector(uiDoc.Document)
                .OfClass(typeof(ViewPlan)).Cast<ViewPlan>()
                .Where(v => !v.IsTemplate && v.GenLevel != null)
                .OrderBy(v => v.GenLevel.Elevation)
                .FirstOrDefault();
            if (plan != null) { uiDoc.ActiveView = plan; }
        }
    }
}
