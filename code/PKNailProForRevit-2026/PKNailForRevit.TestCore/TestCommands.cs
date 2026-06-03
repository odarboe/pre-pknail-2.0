using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;

namespace PKNailForRevit.TestCore
{
    [Transaction(TransactionMode.Manual)]
    public sealed class PKNailTestApp : IExternalApplication
    {
        #region IExternalApplication Members
        public Result OnStartup(UIControlledApplication revitApp)
        {
            string assembly = Assembly.GetExecutingAssembly().Location;
            RibbonPanel pnl = revitApp.CreateRibbonPanel(@"PKNail Test");
            PushButtonData exportModelBtn = new PushButtonData(@"Export Model", @"Export Model",
                                                               assembly, @"PKNailForRevit.TestCore.ExportModelStateCommand");
            PushButtonData openViewBtn = new PushButtonData(@"Open View", @"Open View",
                                                               assembly, @"PKNailForRevit.TestCore.OpenViewCommand");
            pnl.AddStackedItems(exportModelBtn, openViewBtn);
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        { return Result.Succeeded; }
        #endregion
    }

    [Transaction(TransactionMode.Manual)]
    public class ExportModelStateCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cmdData, ref string message, ElementSet elements)
        {
            ModelState state;
            IProjectModel proj = new RevitDocument(cmdData.Application.Application,
                                                   cmdData.Application.ActiveUIDocument.Document);
            proj.InitializeTransaction(ref message);
            proj.SetTransactionName(@"Export Model State", ref message);
            proj.StartTransaction(ref message);
            proj.GetModelState(out state);
            string dir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            RevitModelStateFactory.ExportRevitModelState(state, dir + @"\..\..\..\" + TestConfig.ExportModelFilename);
            proj.CommitTransaction(ref message);
            return Result.Succeeded;
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class OpenViewCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cmdData, ref string message, ElementSet elements)
        {
            System.Windows.Forms.Form dlg = new System.Windows.Forms.Form();
            dlg.Text = @"Open View";
            dlg.ClientSize = new System.Drawing.Size(200, 100);
            System.Windows.Forms.TextBox tb = new System.Windows.Forms.TextBox();
            tb.Name = @"ViewNameTB";
            tb.Width = 160;
            dlg.Controls.Add(tb);
            tb.Location = new System.Drawing.Point(20, 20);
            dlg.ShowDialog();

            string viewName = tb.Text;

            UIDocument uidoc = cmdData.Application.ActiveUIDocument;
            FilteredElementCollector collector = new FilteredElementCollector(uidoc.Document);
            FilteredElementIterator iter = collector.OfClass(typeof(View)).GetElementIterator();
            iter.Reset();
            while (iter.MoveNext())
            {
                View view = iter.Current as View;
                if (   view != null && !view.IsTemplate
                    && view.ViewType == Autodesk.Revit.DB.ViewType.FloorPlan
                    && view.ViewName == viewName)
                { uidoc.ActiveView = view; }
            }
            return Result.Succeeded;
        }
    }
}
