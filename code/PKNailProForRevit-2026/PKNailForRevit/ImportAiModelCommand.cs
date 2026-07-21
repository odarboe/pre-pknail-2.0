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

            // Per-element metering hook lives in PKLicense usage tracking. Default to "allowed" here; the
            // real usage/consumption gate is wired through TheApp's licensing flow in product builds.
            ImportReport report = new AiModelImporter().Import(proj, mapped, recordUsage: null);

            if (report.Committed)
            {
                MessageBox.Show(report.ToString(), @"Import AI Model");
                return Result.Succeeded;
            }

            message = report.ToString();
            return Result.Failed;
        }
    }
}
