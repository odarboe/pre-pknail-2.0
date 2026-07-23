using System;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using PKNail.AiImport.Model;
using PKNail.AiImport.Mapping;
using PKNailForRevit.AiImport;

namespace PKNailForRevit
{
    /// <summary>
    /// Epic 3 ribbon entry point — "Import AI Model (JSON)". Picks a frozen-contract JSON file, validates
    /// it, maps it (both Revit-free), then drives a live <see cref="RevitDocument"/> through
    /// <see cref="AiModelImporter"/>. All Revit work happens here; the model/mapping/import-bridge layers
    /// are unit/integration testable without Revit.
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

            ValidationReport validation = AiModelValidator.Validate(building);
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

            MappedBuilding mapped = AiModelMapper.Map(building);

            IProjectModel proj = new RevitDocument(cmdData.Application.Application, uiDoc.Document);

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

            // The engine hosts openings using wall-face geometry derived from a default 3D view; a fresh
            // metric-template project may have none. Create one now so windows/doors can host (best-effort:
            // if this fails, walls still import and the openings report a clear skip reason).
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
            ImportReport report = new AiModelImporter().Import(proj, mapped, recordUsage);

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

            ViewPlan plan = new FilteredElementCollector(uiDoc.Document)
                .OfClass(typeof(ViewPlan)).Cast<ViewPlan>()
                .Where(v => !v.IsTemplate && v.GenLevel != null)
                .OrderBy(v => v.GenLevel.Elevation)
                .FirstOrDefault();
            if (plan != null) { uiDoc.ActiveView = plan; }
        }
    }
}
