using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Diagnostics;
using Sounds = System.Media.SystemSounds;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Autodesk.Revit.Attributes;

namespace PKNailForRevit
{
    [Transaction(TransactionMode.Manual)]
    public sealed class PKNailForRevitApp : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        { return TheApp.Instance.OnStartup(application); }

        public Result OnShutdown(UIControlledApplication application)
        { return TheApp.Instance.OnShutdown(application); }
    }

    public sealed class KeypadCommandExternalEventHandler : IExternalEventHandler
    {
        public void Execute(UIApplication app)
        { TheApp.Instance.ExecuteKeypadCommand(app); }

        public string GetName()
        { return @"Rangefinder Event Handler"; }
    }

    internal sealed class TheApp
    {
        // This class is a singleton
        private static readonly TheApp instance = new TheApp();
        static TheApp() { }
        internal static TheApp Instance { get { return instance; } }
        private TheApp() { }
        
        private UIApplication m_revitUIApp = null;
        private PKLicense.PKLicense m_license = null;
        private RibbonPanel m_ribbonPanel;

        private enum appCommandState { None = 0, Executing = 1, Error = 2 }
        private appCommandState m_appCommandState;
        private KeypadCommandPhrase m_pendingKeypadCommandPhrase = null;
        private ExternalEvent m_keypadCommandEvent = null;
        private AutoResetEvent m_txnErrorSignal;
        private Thread m_txnMonitorThread;
        private volatile bool m_txnMonitorStop = false;
        private const int cm_splashLicenseTimeout = 2;  // seconds
        private const int cm_splashLoadingTimeout = 2;  // seconds

        private List<IMeasurementProvider> m_mps = new List<IMeasurementProvider>();
        private List<IKeypadCommandProvider> m_kcps = new List<IKeypadCommandProvider>();
        private List<IDialogNavigationProvider> m_dnps = new List<IDialogNavigationProvider>();
        private Disto m_disto = null;

        private PKNailAppSettings m_appSettings;
        private int m_autoSaveActionCount;

        private const string cm_cmdNameConnectRF = @"Connect";
        private const string cm_cmdNameDisconnectRF = @"Disconnect";
        private const string cm_cmdNameChangeKeymap = @"Change Key Map";
        private const string cm_cmdNameSurveyLevels = @"Capture Levels";
        private const string cm_cmdNameActivateWall = "Activate\nWall";
        private const string cm_cmdNameSetCoordinateSystem = "Set\nCoord Sys";
        private const string cm_cmdNameSurveyWall = @"Wall";
        private const string cm_cmdNameSurveyLine = @"Line";
        private const string cm_cmdNameSurveyDoor = @"Door";
        private const string cm_cmdNameSurveyWindow = @"Window";
        private const string cm_cmdNameSurveyHostedObject = @"Wall-Hosted Object";
        private const string cm_cmdNameCopyObjects = @"Copy Level-to-Level";
        private const string cm_cmdNameConvertUnknownWidthWalls = @"Convert Walls";
        private const string cm_cmdNameImportAiModel = "Import AI\nModel";
        private const string cm_cmdNameSettings = @"Settings";

        private string m_currentRFKeymapPage = @"";

        // Resource strings
        private const string cm_supportEMail = @"support@pointknown.com";
        private const string cm_salesEMail = @"sales@pointknown.com";
        private const string cm_supportPhone = @"(617) 575-2222";
        private const string cm_insufficientUsageMsg = @"Your usage balance is insufficient. You must reserve additional usage to continue.";
        private const string cm_assistanceMsg =   @"Contact PointKnown product support ("
                                                + cm_supportPhone + @" or " + cm_supportEMail
                                                + @") for further assistance.";

        public Result OnStartup(UIControlledApplication revitApp)
        {
            Result retval = Result.Succeeded;

            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            SplashForm.StartSplash(fvi.ProductName, fvi.ProductVersion, fvi.LegalCopyright, @"Checking License...");

            try
            {
                DateTime licenseCheckSplashEndTime = DateTime.Now + new TimeSpan(0, 0, cm_splashLicenseTimeout);

                CustomFailures.Register();
                m_appSettings = new PKNailAppSettings();
                populateRibbon(revitApp);

                using (TaskDialog licenseTD = new TaskDialog(licenseTDTitle))
                {
                    licenseTD.TitleAutoPrefix = false;
                    licenseTD.MainInstruction = @"";
                    getLicense(licenseTD);

                    if (DateTime.Now < licenseCheckSplashEndTime)
                    { Thread.Sleep(licenseCheckSplashEndTime - DateTime.Now); }

                    if (!licenseTD.MainInstruction.Equals(@""))
                    {
                        SplashForm.HideSplash();
                        showLicenseTaskDialog(licenseTD);
                    }
                }

                if (IsLicenseValid)
                {
                    SplashForm.UpdateStatus(@"Loading...");
                    SplashForm.ShowSplash();
                    DateTime loadingSplashEndTime = DateTime.Now + new TimeSpan(0, 0, cm_splashLoadingTimeout);

                    initializeApp();
                    setDocumentEventHandlers(revitApp);

                    UpdateCheck uc = new UpdateCheck(m_appSettings);
                    bool isNewVersionDetected = uc.CheckForNewVersion();

                    if (DateTime.Now < loadingSplashEndTime)
                    { Thread.Sleep(loadingSplashEndTime - DateTime.Now); }
                    SplashForm.HideSplash();

                    if (isNewVersionDetected) { uc.GetUserResponse(); }
                }
            }
            finally
            {
                SplashForm.StopSplash();
            }

            return retval;
        }

        public Result OnShutdown(UIControlledApplication revitApp)
        {
            finalizeApp();

            // Flush the output
            System.Diagnostics.Trace.Flush();

            return Result.Succeeded;
        }

        internal bool IsLicenseValid
        { get { return (m_license.IsValid); } }

        internal Result ConnectRFCommand_Txn(ExternalCommandData cmdData, ref string message)
        { return executeInTxn(connectRFCommand_Execute, cmdData, cm_cmdNameConnectRF, ref message); }

        private Result connectRFCommand_Execute(UIDocument uidoc, IProjectModel proj, ref string message)
        {
            // Check to see if we have a valid COM port name; if not, change the setting to automatically detect
            string portName = m_appSettings.RangefinderComPort;
            bool isAutoDetect = m_appSettings.IsAutoDetectComPort;
            int dummyPortNum;
            if (    !isAutoDetect
                && (   portName.Length < 4
                    || portName.Substring(0, 3) != @"COM"
                    || !int.TryParse(portName.Substring(3), out dummyPortNum)))
            {
                m_appSettings.IsAutoDetectComPort = true;
                m_appSettings.Save();
            }

            if (m_disto == null)
            { m_disto = new Disto(onDistoConnectionState, onDistoError, onDistoException); }

            IMeasurementProvider mp = m_disto;
            m_mps.Add(mp);
            mp.PushContext(onMeasurement);
            IKeypadCommandProvider kcp = m_disto;
            m_kcps.Add(kcp);
            kcp.PushContext(onKeypadCommand);
            IDialogNavigationProvider dnp = m_disto;
            m_dnps.Add(dnp);

            m_disto.Connect(m_appSettings.RangefinderComPort, m_appSettings.IsAutoDetectComPort, SynchronizationContext.Current);

            return Result.Succeeded;
        }

        internal Result DisconnectRFCommand_Txn(ExternalCommandData cmdData, ref string message)
        { return executeInTxn(disconnectRFCommand_Execute, cmdData, cm_cmdNameDisconnectRF, ref message);  }

        private Result disconnectRFCommand_Execute(UIDocument uidoc, IProjectModel proj, ref string message)
        {
            if (m_disto != null)
            {
                m_disto.Disconnect();
                IMeasurementProvider mp = (IMeasurementProvider)m_disto;
                mp.PopContext();
                m_mps.Remove(mp);
                IKeypadCommandProvider kcp = (IKeypadCommandProvider)m_disto;
                kcp.PopContext();
                m_kcps.Remove(kcp);
                m_dnps.Remove((IDialogNavigationProvider)m_disto);
            }

            return Result.Succeeded;
        }

        internal Result ChangeKeymapCommand_Txn(ExternalCommandData cmdData, ref string message)
        { return executeInTxn(changeKeymapCommand_Execute, cmdData, cm_cmdNameDisconnectRF, ref message);  }

        private Result changeKeymapCommand_Execute(UIDocument uidoc, IProjectModel proj, ref string message)
        {
            List<string> keymapPages = proj.GetRangefinderKeypadMapPages();

            int keymapIdx = keymapPages.IndexOf(m_currentRFKeymapPage);
            if (keymapIdx == -1)  { keymapIdx = 0; }
            PickOneDlg dlg = new PickOneDlg(keymapPages, keymapIdx);
            dlg.Text = @"Select Rangefinder Keypad Map";
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                m_currentRFKeymapPage = keymapPages[dlg.PickedIndex];
            }
            return Result.Succeeded;
        }

        internal Result LevelsDlgCommand_Txn(ExternalCommandData cmdData, ref string message)
        { return executeInTxn(levelsDlgCommand_Execute, cmdData, cm_cmdNameSurveyLevels, ref message); }

        private Result levelsDlgCommand_Execute(UIDocument uidoc, IProjectModel proj, ref string message)
        {
            LevelsData lvlsData = proj.LevelsData;
            if (lvlsData.Levels.Count < 1)
            {
                NumLvlDlg numLvlDlg = new NumLvlDlg();
                numLvlDlg.ShowDialog();
                if (numLvlDlg.DialogResult != System.Windows.Forms.DialogResult.OK)
                { return Result.Cancelled; }
                else
                {
                    int numLvls = numLvlDlg.NumLevels;
                    bool isDropCeilings = numLvlDlg.IsDropCeilings;
                    for (int lvlIdx = numLvls - 1; lvlIdx >= 0; lvlIdx--)
                    {
                        lvlsData.InsertLevel(lvlsData.Levels.Count, "Level " + lvlIdx.ToString("D2"));
                        lvlsData.Levels[lvlsData.Levels.Count - 1].IsDropCeiling = isDropCeilings;
                    }
                }
            }

            DocumentSettings docSettings = proj.DocumentSettings;
            LevelsDlg dlg = new LevelsDlg(lvlsData, docSettings, proj.UnitSystem);
            dlg.DriveAndShowDialog(m_mps, m_dnps);
            proj.LevelsData = dlg.Data;

            return Result.Succeeded;
        }

        internal Result ActivateWallCommand_Txn(ExternalCommandData cmdData, ref string message)
        { return executeInTxn(activateWallCommand_Execute, cmdData, cm_cmdNameActivateWall, ref message); }

        private Result activateWallCommand_Execute(UIDocument uidoc, IProjectModel proj, ref string message)
        {
            CommandResult cmdResult;
            CommandContext cmdContext;
            WallSurveyParams wsParams;

            cmdResult = proj.ActivateWallPrep(out cmdContext, out wsParams, ref message);
            if (cmdResult == CommandResult.Succeeded)
            {
                if (wsParams.IsSurveyRequired)
                {
                    if (wsParams.ReferenceOpeningParams != null)
                    {
                        TransitOpeningDlg dlg = new TransitOpeningDlg(cmdContext, wsParams.ReferenceOpeningParams);
                        dlg.DriveAndShowDialog(m_mps, m_dnps);
                        if (dlg.DialogResult == System.Windows.Forms.DialogResult.OK)
                        { wsParams.ReferenceOpeningParams = dlg.Params; }
                        else
                        { cmdResult = CommandResult.Cancelled; }
                    }
                    else
                    {
                        SurveyWallDlg dlg = new SurveyWallDlg(cmdContext, wsParams);
                        dlg.DriveAndShowDialog(m_mps, m_dnps);
                        if (dlg.DialogResult == System.Windows.Forms.DialogResult.OK)
                        { wsParams = dlg.Params; }
                        else
                        { cmdResult = CommandResult.Cancelled; }
                    }
                }
                if (cmdResult == CommandResult.Succeeded)
                {
                    if (proj.ActivateWall(cmdContext, wsParams, ref message) == CommandResult.Succeeded)
                    {
                        reZoom(uidoc, cmdContext.ZoomElements);
                        cmdResult = CommandResult.Succeeded;
                    }
                    else { cmdResult = CommandResult.Failed; }
                }
            }

            return toRevitCmdResult(cmdResult);
        }

        internal Result SetCoordinateSystemCommand_Txn(ExternalCommandData cmdData, ref string message)
        { return executeInTxn(setCoordinateSystemCommand_Execute, cmdData, cm_cmdNameActivateWall, ref message); }

        // Per-element usage metering for the AI import (Q4 — mirror 1.0: RecordUsage before each placed
        // wall/opening). Returns a delegate the importer invokes per element; false means the balance is
        // exhausted and the import should stop. In BYPASS_LICENSE_CHECK/debug builds RecordUsage returns
        // true (no metering), so this does not interfere with development imports.
        internal Func<bool> CreateImportUsageRecorder(IProjectModel proj)
        {
            if (m_license == null) { return () => true; }
            return () => m_license.RecordUsage(proj.DocumentSettings.ProjectName, m_appSettings.UserName);
        }

        private Result setCoordinateSystemCommand_Execute(UIDocument uidoc, IProjectModel proj, ref string message)
        { return toRevitCmdResult(proj.SetCoordinateSystemFromWall()); }

        internal Result SurveyWallCommand_Txn(ExternalCommandData cmdData, ref string message)
        { return executeInTxn(surveyWallCommand_Execute, cmdData, cm_cmdNameSurveyWall, ref message); }

        private Result surveyWallCommand_Execute(UIDocument uidoc, IProjectModel proj, SurveyDirection dir, ref string message)
        {
            CommandResult cmdResult;
            CommandContext cmdContext;
            WallSurveyParams wsParams;
            cmdResult = proj.SurveyWallPrep(dir, out cmdContext, out wsParams, ref message);
            if (cmdResult == CommandResult.Succeeded)
            {
                cmdResult = surveyWallCommand_Finish(cmdContext, uidoc, proj, wsParams, ref message);
            }
            return toRevitCmdResult(cmdResult);
        }

        private CommandResult surveyWallCommand_Finish(CommandContext cmdContext, UIDocument uidoc,
                                                       IProjectModel proj, WallSurveyParams wsParams, ref string message)
        {
            if (wsParams.IsFirstWall)
            {
                FirstWallDlg fwDlg = new FirstWallDlg(cmdContext, wsParams);
                fwDlg.DriveAndShowDialog(m_mps, m_dnps);
                if (fwDlg.DialogResult != System.Windows.Forms.DialogResult.OK)
                { return CommandResult.Cancelled; }
                else
                { wsParams = fwDlg.Params; }
            }

            if (wsParams.IsSurveyRequired)
            {
                SurveyWallDlg dlg = new SurveyWallDlg(cmdContext, wsParams);
                dlg.DriveAndShowDialog(m_mps, m_dnps);
                if (dlg.DialogResult != System.Windows.Forms.DialogResult.OK)
                { return CommandResult.Cancelled; }
                else
                { wsParams = dlg.Params; }
            }

            if (wsParams.IsCreateWall)
            {
                bool isSufficientUsage = m_license.RecordUsage(proj.DocumentSettings.ProjectName, m_appSettings.UserName);
                if (!isSufficientUsage)
                {
                    message = cm_insufficientUsageMsg;
                    return CommandResult.Failed;
                }
            }

            if (proj.SurveyWall(cmdContext, wsParams, ref message) == CommandResult.Succeeded)
            {
                reZoom(uidoc, cmdContext.ZoomElements);
                return CommandResult.Succeeded;
            }
            else { return CommandResult.Failed; }
        }

        private Result surveyWallCommand_Execute_LR(UIDocument uidoc, IProjectModel proj, ref string message)
        { return surveyWallCommand_Execute(uidoc, proj, SurveyDirection.LeftToRight, ref message); }

        private Result surveyWallCommand_Execute_RL(UIDocument uidoc, IProjectModel proj, ref string message)
        { return surveyWallCommand_Execute(uidoc, proj, SurveyDirection.RightToLeft, ref message); }

        private Result surveyWallCommand_Execute_ModelDir(UIDocument uidoc, IProjectModel proj, ref bool isAutoSave, ref string message)
        { return surveyWallCommand_Execute(uidoc, proj, SurveyDirection.FromModel, ref message); }

        internal Result SurveyWindowCommand_Txn(ExternalCommandData cmdData, ref string message)
        { return executeInTxn(surveyWindowCommand_Execute, cmdData, cm_cmdNameSurveyWindow, ref message); }

        private Result surveyWindowCommand_Execute(UIDocument uidoc, IProjectModel proj, SurveyDirection dir, ref string message)
        {
            CommandResult cmdResult;
            CommandContext cmdContext;
            WallHostedObjectSurveyParams whosParams;
            cmdResult = proj.SurveyWallHostedObjectPrep(dir, FamilyCategory.Windows, out cmdContext, out whosParams, ref message);
            if (cmdResult == CommandResult.Succeeded)
            {
                cmdResult = surveyWallHostedObjectCommand_Finish(cmdContext, proj, whosParams, ref message);
            }
            return toRevitCmdResult(cmdResult);
        }

        private Result surveyWindowCommand_Execute_LR(UIDocument uidoc, IProjectModel proj, ref string message)
        { return surveyWindowCommand_Execute(uidoc, proj, SurveyDirection.LeftToRight, ref message); }

        private Result surveyWindowCommand_Execute_RL(UIDocument uidoc, IProjectModel proj, ref string message)
        { return surveyWindowCommand_Execute(uidoc, proj, SurveyDirection.RightToLeft, ref message); }

        private Result surveyWindowCommand_Execute_ModelDir(UIDocument uidoc, IProjectModel proj, ref string message)
        { return surveyWindowCommand_Execute(uidoc, proj, SurveyDirection.FromModel, ref message); }

        internal Result SurveyWallHostedObjectCommand_Txn(ExternalCommandData cmdData, ref string message)
        { return executeInTxn(surveyWallHostedObjectCommand_Execute, cmdData, cm_cmdNameSurveyWindow, ref message); }

        private Result surveyWallHostedObjectCommand_Execute(UIDocument uidoc, IProjectModel proj, SurveyDirection dir, ref string message)
        {
            CommandResult cmdResult;

            string pickedFamilyCategory = null;
            List<string> familyCategories = proj.GetCustomFamilyCategories();
            PickOneDlg dlg = new PickOneDlg(familyCategories, 0);
            dlg.Text = @"Select a Family Category";
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                pickedFamilyCategory = familyCategories[dlg.PickedIndex];
                CommandContext cmdContext;
                WallHostedObjectSurveyParams whosParams;
                cmdResult = proj.SurveyWallHostedObjectPrep(dir, pickedFamilyCategory, out cmdContext, out whosParams, ref message);
                if (cmdResult == CommandResult.Succeeded)
                { cmdResult = surveyWallHostedObjectCommand_Finish(cmdContext, proj, whosParams, ref message); }
                return toRevitCmdResult(cmdResult);
            }
            else
            { return Result.Cancelled; }

        }

        private CommandResult surveyWallHostedObjectCommand_Finish(CommandContext cmdContext, IProjectModel proj,
                                                                   WallHostedObjectSurveyParams whosParams, ref string message)
        {
            SurveyWallHostedObjectForm dlg = null;
            if (whosParams.FamilyCategory.IsDoors())
            { dlg = new SurveyDoorDlg(cmdContext, whosParams); }
            else if (whosParams.FamilyCategory.IsWindows())
            { dlg = new SurveyWindowDlg(cmdContext, whosParams); }
            else
            { dlg = new SurveyHostedObjectDlg(cmdContext, whosParams); }

            dlg.DriveAndShowDialog(m_mps, m_dnps);
            if (dlg.DialogResult == System.Windows.Forms.DialogResult.OK)
            {
                bool isSufficientUsage = m_license.RecordUsage(proj.DocumentSettings.ProjectName, m_appSettings.UserName);
                if (!isSufficientUsage)
                {
                    message = cm_insufficientUsageMsg;
                    return CommandResult.Failed;
                }
                else
                {
                    return proj.SurveyWallHostedObject(cmdContext, dlg.Params, ref message);
                }
            }
            else
            { return CommandResult.Cancelled; }
        }

        internal Result SurveyDoorCommand_Txn(ExternalCommandData cmdData, ref string message)
        { return executeInTxn(surveyDoorCommand_Execute, cmdData, cm_cmdNameSurveyDoor, ref message); }

        private Result surveyDoorCommand_Execute(UIDocument uidoc, IProjectModel proj, SurveyDirection dir, ref string message)
        {
            CommandResult cmdResult;
            CommandContext cmdContext;
            WallHostedObjectSurveyParams whosParams;
            cmdResult = proj.SurveyWallHostedObjectPrep(dir, FamilyCategory.Doors, out cmdContext, out whosParams, ref message);
            if (cmdResult == CommandResult.Succeeded)
            { cmdResult = surveyWallHostedObjectCommand_Finish(cmdContext, proj, whosParams, ref message); }
            return toRevitCmdResult(cmdResult);
        }

        private Result surveyDoorCommand_Execute_LR(UIDocument uidoc, IProjectModel proj, ref string message)
        { return surveyDoorCommand_Execute(uidoc, proj, SurveyDirection.LeftToRight, ref message); }

        private Result surveyDoorCommand_Execute_RL(UIDocument uidoc, IProjectModel proj, ref string message)
        { return surveyDoorCommand_Execute(uidoc, proj, SurveyDirection.RightToLeft, ref message); }

        private Result surveyDoorCommand_Execute_ModelDir(UIDocument uidoc, IProjectModel proj, ref string message)
        { return surveyDoorCommand_Execute(uidoc, proj, SurveyDirection.FromModel, ref message); }

        private Result execRFKeymappedCmd(UIDocument uidoc, IProjectModel proj, KeypadCommandPhrase cmdPhrase, ref string message)
        {
            CommandResult cmdResult;
            CommandContext cmdContext;
            SurveyParams sParams;
            cmdResult = proj.SurveyRFCommandMappedObjectPrep(m_currentRFKeymapPage, cmdPhrase, out cmdContext, out sParams, ref message);
            if (cmdResult == CommandResult.Succeeded)
            {
                if (sParams.FamilyCategory.IsWalls())
                { cmdResult = surveyWallCommand_Finish(cmdContext, uidoc, proj, (WallSurveyParams)sParams, ref message); }
                else if (sParams.FamilyCategory.IsLines())
                { cmdResult = surveyLineCommand_Finish(cmdContext, uidoc, proj, (LineSurveyParams)sParams, ref message); }
                else
                { cmdResult = surveyWallHostedObjectCommand_Finish(cmdContext, proj, (WallHostedObjectSurveyParams)sParams, ref message); }
            }
            return toRevitCmdResult(cmdResult);
        }

        internal Result SurveyLineCommand_Txn(ExternalCommandData cmdData, ref string message)
        { return executeInTxn(surveyLineCommand_Execute, cmdData, cm_cmdNameSurveyLine, ref message); }

        private Result surveyLineCommand_Execute(UIDocument uidoc, IProjectModel proj, SurveyDirection dir, ref string message)
        {
            CommandResult cmdResult;

            CommandContext cmdContext;
            LineSurveyParams lsParams;
            cmdResult = proj.SurveyLinePrep(dir, false, out cmdContext, out lsParams, ref message);
            if (cmdResult == CommandResult.Succeeded)
            { cmdResult = surveyLineCommand_Finish(cmdContext, uidoc, proj, lsParams, ref message); }

            return toRevitCmdResult(cmdResult);
        }

        private CommandResult surveyLineCommand_Finish(CommandContext cmdContext, UIDocument uidoc,
                                                       IProjectModel proj, LineSurveyParams lsParams, ref string message)
        {
            if (lsParams.IsSurveyRequired)
            {
                SurveyLineDlg dlg = new SurveyLineDlg(cmdContext, lsParams);
                dlg.DriveAndShowDialog(m_mps, m_dnps);
                if (dlg.DialogResult != System.Windows.Forms.DialogResult.OK)
                { return CommandResult.Cancelled; }
                else
                { lsParams = dlg.Params; }
            }

            bool isSufficientUsage = m_license.RecordUsage(proj.DocumentSettings.ProjectName, m_appSettings.UserName);
            if (!isSufficientUsage)
            {
                message = cm_insufficientUsageMsg;
                return CommandResult.Failed;
            }

            if (proj.SurveyLine(cmdContext, lsParams, ref message) == CommandResult.Succeeded)
            {
                reZoom(uidoc, cmdContext.ZoomElements);
                return CommandResult.Succeeded;
            }
            else { return CommandResult.Failed; }
        }

        private Result surveyLineCommand_Execute_LR(UIDocument uidoc, IProjectModel proj, ref string message)
        { return surveyLineCommand_Execute(uidoc, proj, SurveyDirection.LeftToRight, ref message); }

        private Result surveyLineCommand_Execute_RL(UIDocument uidoc, IProjectModel proj, ref string message)
        { return surveyLineCommand_Execute(uidoc, proj, SurveyDirection.RightToLeft, ref message); }

        private Result surveyLineCommand_Execute_ModelDir(UIDocument uidoc, IProjectModel proj, ref string message)
        { return surveyLineCommand_Execute(uidoc, proj, SurveyDirection.FromModel, ref message); }

        internal Result CopyObjectsCommand_Txn(ExternalCommandData cmdData, ref string message)
        { return executeInTxn(copyObjectsCommand_Execute, cmdData, cm_cmdNameCopyObjects, ref message); }

        private Result copyObjectsCommand_Execute(UIDocument uidoc, IProjectModel proj, ref string message)
        {
            CommandResult cmdResult;
            List<string> selectionIDs = new List<string>();
            foreach (ElementId elId in uidoc.Selection.GetElementIds()) { selectionIDs.Add(elId.ToString()); }
            CopyObjectsData data;
            cmdResult = proj.CopyObjectsPrep(selectionIDs, out data, ref message);
            if (cmdResult == CommandResult.Succeeded)
            {
                CopyObjectsDlg dlg = new CopyObjectsDlg(data);
                dlg.ShowDialog();
                if (dlg.DialogResult == System.Windows.Forms.DialogResult.OK)
                { cmdResult = proj.CopyObjects(dlg.Data, ref message); }
            }
            return toRevitCmdResult(cmdResult);
        }

        internal Result ConvertUnknownWidthWallsCommand_Txn(ExternalCommandData cmdData, ref string message)
        { return executeInTxn(convertUnknownWidthWallsCommand_Execute, cmdData, cm_cmdNameCopyObjects, ref message); }

        private Result convertUnknownWidthWallsCommand_Execute(UIDocument uidoc, IProjectModel proj, ref string message)
        {
            CommandResult cmdResult;

            List<string> selectionIDs = new List<string>();
            foreach (ElementId elId in uidoc.Selection.GetElementIds()) { selectionIDs.Add(elId.ToString()); }
            ConvertUnknownWidthWallsData data;
            cmdResult = proj.ConvertUnknownWidthWallsPrep(selectionIDs, out data, ref message);
            if (cmdResult == CommandResult.Succeeded)
            {
                ConvertUnknownWidthWallsDlg dlg = new ConvertUnknownWidthWallsDlg(data);
                dlg.ShowDialog();
                if (dlg.DialogResult == System.Windows.Forms.DialogResult.OK)
                { cmdResult = proj.ConvertUnknownWidthWalls(dlg.Data, ref message); }
            }
            return toRevitCmdResult(cmdResult);
        }

        internal Result SettingsDlgCommand_Txn(ExternalCommandData cmdData, ref string message)
        { return executeInTxn(SettingsDlgCommand_Execute, cmdData, cm_cmdNameSettings, ref message); }

        private Result SettingsDlgCommand_Execute(UIDocument uidoc, IProjectModel proj, ref string message)
        {
            DocumentSettings docSettings = null;
            List<string> allFamilyCategories = null;
            UnitSystem projUnitSystem = UnitSystem.Imperial;  // dummy default
            if (proj != null)
            {
                docSettings = proj.DocumentSettings;
                allFamilyCategories = proj.GetBuiltinFamilyCategories();
                allFamilyCategories.AddRange(proj.GetCustomFamilyCategories());
                projUnitSystem = proj.UnitSystem;
            }

            bool isLicenseValidPreviously = IsLicenseValid;
            SettingsDlg dlg = new SettingsDlg(m_appSettings, docSettings, allFamilyCategories,
                                              m_license, projUnitSystem);
            dlg.ShowDialog();
            if (dlg.DialogResult == System.Windows.Forms.DialogResult.OK)
            {
                if (proj != null)  { proj.DocumentSettings = dlg.DocSettings; }
                if (dlg.AppSettings != null)  { dlg.AppSettings.Save(); }

                if (IsLicenseValid && !isLicenseValidPreviously)
                { initializeApp(); }
            }

            return Result.Succeeded;
        }

        private delegate Result ExecuteCmdWithDirection(UIDocument uidoc, IProjectModel proj, SurveyDirection dir, ref string message);

        private Result executeInTxn(ExecuteCmdWithDirection exec, ExternalCommandData cmdData, string txnName, ref string msg)
        {
            Result retval = Result.Succeeded;

            if (m_appCommandState != appCommandState.None)
            { Sounds.Hand.Play(); return Result.Failed; }

            if (cmdData.Application != null) { setAppContext(cmdData.Application); }

            IProjectModel proj = projectModel();

            if (proj != null)
            {
                if (proj.InitializeTransaction(ref msg) == CommandResult.Succeeded)
                {
                    m_appCommandState = appCommandState.Executing;
                    proj.SetTransactionName(txnName, ref msg);
                    if (proj.StartTransaction(ref msg) == CommandResult.Succeeded)
                    {
                        try
                        {
                            m_appCommandState = appCommandState.Executing;
                            retval = exec(cmdData.Application.ActiveUIDocument, proj, SurveyDirection.FromModel, ref msg);                            
                            if (retval == Result.Succeeded)
                            {
                                if (proj.CommitTransaction(ref msg) == CommandResult.Succeeded)
                                {
                                    autoSave(proj);
                                }
                                else
                                { retval = Result.Failed; }
                            }
                            else
                            {
                                proj.RollBackTransaction(ref msg);
                                if (retval == Result.Failed) { Sounds.Hand.Play(); }
                            }
                        }
                        catch (SoftException)
                        {
                            proj.RollBackTransaction(ref msg);
                            Sounds.Hand.Play();
                            retval = Result.Succeeded;
                        }
                        catch (HardException e)
                        {
                            msg = e.Message;
                            proj.RollBackTransaction(ref msg);
                            retval = Result.Failed;
                        }
                        catch
                        {
                            proj.RollBackTransaction(ref msg);
                            throw;  // TODO: Log the exception before re-throwing
                        }
                        finally
                        { m_appCommandState = appCommandState.None; }
                    }
                    else
                    { retval = Result.Failed; }
                }
                else   // proj.InitializeTransaction(ref msg) != CommandResult.Succeeded
                { retval = Result.Failed; }
            }
            else  // proj == null
            {
                try
                {
                    m_appCommandState = appCommandState.Executing;
                    retval = exec(cmdData.Application.ActiveUIDocument, proj, SurveyDirection.FromModel, ref msg);                      
                    if (retval == Result.Failed)
                    { Sounds.Hand.Play(); }
                }
                catch (SoftException)
                {
                    proj.RollBackTransaction(ref msg);
                    Sounds.Hand.Play();
                    retval = Result.Succeeded;
                }
                catch (HardException e)
                {
                    msg = e.Message;
                    proj.RollBackTransaction(ref msg);
                    retval = Result.Failed;
                }
                catch
                {
                    proj.RollBackTransaction(ref msg);
                    throw;  // TODO: Log the exception before re-throwing
                }
                finally
                { m_appCommandState = appCommandState.None; }
            }

            return retval;
        }

        private delegate Result ExecuteCmd(UIDocument uidoc, IProjectModel proj, ref string message);

        private Result executeInTxn(ExecuteCmd exec, ExternalCommandData cmdData, string txnName, ref string msg)
        {
            Result retval = Result.Succeeded;

            if (m_appCommandState != appCommandState.None)
            { Sounds.Hand.Play(); return Result.Failed; }

            if (cmdData.Application != null) { setAppContext(cmdData.Application); }

            IProjectModel proj = projectModel();

            if (proj != null)
            {
                if (proj.InitializeTransaction(ref msg) == CommandResult.Succeeded)
                {
                    m_appCommandState = appCommandState.Executing;
                    proj.SetTransactionName(txnName, ref msg);
                    if (proj.StartTransaction(ref msg) == CommandResult.Succeeded)
                    {
                        try
                        {
                            retval = exec(cmdData.Application.ActiveUIDocument, proj, ref msg);
                            if (retval == Result.Succeeded)
                            {
                                if (proj.CommitTransaction(ref msg) == CommandResult.Succeeded)
                                {
                                    autoSave(proj);
                                }
                                else
                                { retval = Result.Failed; }
                            }
                            else
                            {
                                proj.RollBackTransaction(ref msg);
                                if (retval == Result.Failed) { Sounds.Hand.Play(); }
                            }
                        }
                        catch (SoftException)
                        {
                            proj.RollBackTransaction(ref msg);
                            Sounds.Hand.Play();
                            retval = Result.Succeeded;
                        }
                        catch (HardException e)
                        {
                            msg = e.Message;
                            proj.RollBackTransaction(ref msg);
                            retval = Result.Failed;
                        }
                        catch
                        {
                            proj.RollBackTransaction(ref msg);
                            throw;  // TODO: Log the exception before re-throwing
                        }
                        finally
                        { m_appCommandState = appCommandState.None; }
                    }
                    else
                    { retval = Result.Failed; }
                }
                else   // proj.InitializeTransaction(ref msg) != CommandResult.Succeeded
                { retval = Result.Failed; }
            }
            else
            {
                try
                {
                    m_appCommandState = appCommandState.Executing;
                    retval = exec(cmdData.Application.ActiveUIDocument, proj, ref msg);
                    if (retval == Result.Failed)  { Sounds.Hand.Play(); }
                }
                catch (SoftException)
                {
                    proj.RollBackTransaction(ref msg);
                    Sounds.Hand.Play();
                    retval = Result.Succeeded;
                }
                catch (HardException e)
                {
                    msg = e.Message;
                    proj.RollBackTransaction(ref msg);
                    retval = Result.Failed;
                }
                catch
                {
                    proj.RollBackTransaction(ref msg);
                    throw;  // TODO: Log the exception before re-throwing
                }
                finally
                { m_appCommandState = appCommandState.None; }
            }

            return retval;
        }

        private static Result toRevitCmdResult(CommandResult cmdResult)
        {
            Result retval = Result.Cancelled;
            switch (cmdResult)
            {
                case CommandResult.Succeeded:
                    retval = Result.Succeeded;
                    break;
                case CommandResult.Failed:
                    retval = Result.Failed;
                    break;
                case CommandResult.Cancelled:
                    retval = Result.Cancelled;
                    break;
                case CommandResult.TransactionError:
                    retval = Result.Failed;
                    break;
            }
            return retval;
        }

        private void onMeasurement(object sender, MeasurementEventArgs e)
        { Sounds.Hand.Play(); }

        private void onKeypadCommand(object sender, KeypadCommandEventArgs e)
        {
            if (m_appCommandState != appCommandState.None)
            { Sounds.Hand.Play(); return; }
            m_pendingKeypadCommandPhrase = e.CommandPhrase;
            m_keypadCommandEvent.Raise();
        }

        private void onDistoError(object sender, Disto.ErrorEventArgs e)
        {
            System.Windows.Forms.MessageBox.Show("Rangefinder error: " + e.Error.ErrorMessage);
        }

        private void onDistoException(object sender, Disto.ExceptionEventArgs e)
        {
            System.Windows.Forms.MessageBox.Show("Rangefinder exception: " + e.Exception.Message);
        }

        private void onDistoConnectionState(object sender, Disto.ConnectionStateEventArgs e)
        {
            if (e.State == Disto.ConnectionState.Connected)
            {
                m_appSettings.RangefinderComPort = e.PortName;
                m_appSettings.Save();
            }
        }

        internal void ExecuteKeypadCommand(UIApplication app)
        {
            // This handler uses the rangefinder command phrase that was stored by the
            // rangefinder event handler.  If null, we have a synchronization problem
            if (m_pendingKeypadCommandPhrase == null)
            {
                System.Windows.Forms.MessageBox.Show(@"Rangefinder synchronization error");
                Sounds.Hand.Play();
                return;
            }

            string message = @"";
            UIDocument uidoc = m_revitUIApp.ActiveUIDocument;
            IProjectModel proj = projectModel();

            if (proj.InitializeTransaction(ref message) == CommandResult.Failed)
            { Sounds.Hand.Play(); return; }

            Result result = Result.Cancelled;
            try
            {
                m_appCommandState = appCommandState.Executing;

                if (proj.StartTransaction(ref message) == CommandResult.Succeeded)
                { result = execRFKeymappedCmd(uidoc, proj, m_pendingKeypadCommandPhrase, ref message); }
                else
                { result = Result.Failed; }

                if (result != Result.Succeeded)
                { proj.RollBackTransaction(ref message); }
                else
                {
                    if (proj.CommitTransaction(ref message) == CommandResult.Succeeded)
                    { autoSave(proj); }
                }
            }
            catch (SoftException)
            {
                proj.RollBackTransaction(ref message);
                result = Result.Failed;
            }
            catch (HardException he)
            {
                message += he.Message;
                proj.RollBackTransaction(ref message);
                result = Result.Failed;
            }
            catch (Exception ex)
            {
                message += ex.Message;
                proj.RollBackTransaction(ref message);
                result = Result.Failed;
            }
            finally
            {
                m_pendingKeypadCommandPhrase = null;
                m_appCommandState = appCommandState.None;
            }

            if (result == Result.Failed || message.Length > 0)
            { Sounds.Hand.Play(); }

            if (message.Length > 0)
            { System.Windows.Forms.MessageBox.Show(message); }
        }

        private void initializeApp()
        {
            //// Set rangefinder initial context
            //m_rf = TheRangefinder.Instance.Interface;
            //m_rf.OnRangefinderCommand += onRangefinderCommand;
            //m_rf.OnRangefinderMeasurement += onRangefinderMeasurement;
            //m_rf.OnRangefinderConnectionState += onRangefinderConnectionState;
            //m_rf.OnRangefinderException += onRangefinderException;
            //m_rf.OnRangefinderError += onRangefinderError;
            //m_rfConnectionState = RangefinderConnectionState.Disconnected;
            m_keypadCommandEvent = ExternalEvent.Create(new KeypadCommandExternalEventHandler());
            m_appCommandState = appCommandState.None;

            m_license.StartBackgroundPostUsageRecordsThread();

            m_txnErrorSignal = new AutoResetEvent(false);
            m_txnMonitorStop = false;
            m_txnMonitorThread = new Thread(new ThreadStart(transactionMonitorThreadWorker));
            m_txnMonitorThread.Name = @"txnMonitor";
            m_txnMonitorThread.IsBackground = true;
            m_txnMonitorThread.Start();

            m_autoSaveActionCount = 0;

        }

        private void finalizeApp()
        {
            if (m_disto != null)  { m_disto.Disconnect(); }
            //if (m_rf != null)
            //{
            //    m_rf.Disconnect();
            //    m_rf.OnRangefinderCommand -= onRangefinderCommand;
            //    m_rf.OnRangefinderMeasurement -= onRangefinderMeasurement;
            //    m_rf.OnRangefinderConnectionState -= onRangefinderConnectionState;
            //    m_rf.OnRangefinderException -= onRangefinderException;
            //    m_rf.OnRangefinderError -= onRangefinderError;
            //}
            if (m_keypadCommandEvent != null)
            {
                m_keypadCommandEvent.Dispose();
                m_keypadCommandEvent = null;
            }

            m_license.StopBackgroundPostUsageRecordsThread();

            // Stop the transaction-monitor thread gracefully. Thread.Abort() is unsupported on .NET 8
            // and throws PlatformNotSupportedException — which, escaping on shutdown, made Revit flag the
            // add-in as crashed and disable it (re-adding <SuppressedWarning> to the manifest).
            if (m_txnMonitorThread != null)
            {
                m_txnMonitorStop = true;
                try { m_txnErrorSignal?.Set(); } catch { }
                try { m_txnMonitorThread.Join(2000); } catch { }
                m_txnMonitorThread = null;
            }
        }

        private void setDocumentEventHandlers(UIControlledApplication revitApp)
        {
            revitApp.ControlledApplication.DocumentCreated += new EventHandler<DocumentCreatedEventArgs>(ControlledApplication_DocumentCreated);
            revitApp.ControlledApplication.DocumentOpened += new EventHandler<DocumentOpenedEventArgs>(controlledApplication_DocumentOpened);
            revitApp.ControlledApplication.DocumentClosing += new EventHandler<DocumentClosingEventArgs>(ControlledApplication_DocumentClosing);
        }

        private void reZoom(UIDocument uidoc, List<string> elementIDs)
        {
            if (elementIDs.Count > 0)
            {
                // Re-zoom the view
                ElementSet zoomElements = new ElementSet();
                Document doc = m_revitUIApp.ActiveUIDocument.Document;
                foreach (string s in elementIDs)
                {
                    Element el = doc.GetElement(new ElementId(int.Parse(s)));
                    zoomElements.Insert(el);
                }
                uidoc.ShowElements(zoomElements);
            }
        }

        private void getLicense(TaskDialog licenseTD)
        {
            m_license = new PKLicense.PKLicense();
            if (!m_license.IsValid)
            {
                if (!m_license.IsActivated)
                { renderActivateTD(licenseTD); }
                else if (m_license.IsExpired)
                { renderSubscriptionExpiredTD(licenseTD); }
                else if (m_license.IsInsufficientBalance)
                { renderInsufficientBalanceTD(licenseTD); }
                else
                { renderInvalidLicenseTD(licenseTD); }
            }
            else
            {
                if (m_license.IsExpirationWarning)
                { renderExpirationWarningTD(licenseTD); }
                else if (m_license.IsBalanceWarning)
                { renderBalanceWarningTD(licenseTD); }
            }
        }
        
        private void showLicenseTaskDialog(TaskDialog licenseTD)
        {
            TaskDialogResult r = licenseTD.Show();
            switch (r)
            {
                case TaskDialogResult.CommandLink1:   // Activate a license
                    using (ActivateLicenseOnlineDlg activateDlg = new ActivateLicenseOnlineDlg(m_license))
                    {
                        activateDlg.ShowDialog();
                        if (activateDlg.DialogResult == System.Windows.Forms.DialogResult.OK)
                        {
                            if (m_license.IsMetered)
                            {
                                if (m_license.IsTrial)
                                {
                                    if (m_license.ReserveUsage(m_license.LastUserID, activateDlg.Password, m_license.TrialUsageBalance))
                                    {
                                        using (TaskDialog trialTD = new TaskDialog(licenseTDTitle))
                                        {
                                            trialTD.TitleAutoPrefix = false;
                                            trialTD.MainInstruction = @"Your free trial of " + ProductInfo.ProductBaseName + @" has started";
                                            trialTD.MainContent = @"Your trial entitles you to " + m_license.ReserveBalance.ToString()
                                                + @" " + m_license.UsageUnitsStr + @" use of " + ProductInfo.ProductBaseName
                                                + @".  You will be prompted to purchase additional usage when your trial usage has been consumed.";
                                            trialTD.CommonButtons = TaskDialogCommonButtons.Ok;
                                            trialTD.Show();
                                        }
                                    }
                                    else
                                    {
                                        using (TaskDialog trialErrorTD = new TaskDialog(licenseTDTitle))
                                        {
                                            trialErrorTD.TitleAutoPrefix = false;
                                            trialErrorTD.MainInstruction = @"Your free trial of " + ProductInfo.ProductBaseName + @" is not available";
                                            trialErrorTD.MainContent = @"There was an error starting your trial. " + ProductInfo.ProductBaseName
                                                + " is disabled on this computer.  Error message: " + m_license.ErrorMessage
                                                + @". " + cm_assistanceMsg;
                                            trialErrorTD.CommonButtons = TaskDialogCommonButtons.Ok;
                                            trialErrorTD.Show();
                                        }
                                    }
                                }
                                else
                                {
                                    using (UsageBalanceTransferDlg xferDlg = new UsageBalanceTransferDlg(m_license, false, activateDlg.Password))
                                    {
                                        xferDlg.ShowDialog();
                                    }
                                }
                            }
                        }
                        else
                        {
                            (renderActivationFailedTD(licenseTDTitle)).Show();
                        }
                    }

                    break;

                case TaskDialogResult.CommandLink4:
                    using (UsageBalanceTransferDlg xferDlg = new UsageBalanceTransferDlg(m_license, false))
                    {
                        xferDlg.ShowDialog();
                    }
                    break;

                case TaskDialogResult.CommandLink2:
                case TaskDialogResult.CommandLink3:
                default:
                    break;
            }
        }

        private void renderActivateTD(TaskDialog td)
        {
            td.TitleAutoPrefix = false;
            td.MainInstruction = @"Welcome to " + ProductInfo.ProductBaseName + "!";
            string content = @"You must activate " + ProductInfo.ProductBaseName + @" before use.  ";
            content += @"Activation requires an Internet connection.  Would you like to activate " + ProductInfo.ProductBaseName + @" now?";
            td.MainContent = content;
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, @"Yes, activate " + ProductInfo.ProductBaseName + @" now");
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, @"No, continue without using " + ProductInfo.ProductBaseName);
        }

        private void renderSubscriptionExpiredTD(TaskDialog td)
        {
            td.MainInstruction = @"Your " + ProductInfo.ProductBaseName + @" license subscription has expired";
            td.MainContent = ProductInfo.ProductBaseName + @" is disabled on this computer.  ";
            td.MainContent += @"To renew your subscription, please contact your reseller or PointKnown Sales at ";
            td.MainContent += cm_supportPhone + @" or " + cm_salesEMail;
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, @"OK, continue without using " + ProductInfo.ProductBaseName);
        }

        private void renderInsufficientBalanceTD(TaskDialog td)
        {
            td.MainInstruction = @"Your " + ProductInfo.ProductBaseName + @" usage balance is insufficient";
            td.MainContent = @"Your balance is: " + m_license.ReserveBalance + @" " + licenseUsageUnits + @".  ";
            td.MainContent += @"Would you like to refill your usage balance on this computer?  This requires an Internet connection.";
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink4, @"Yes, refill my usage balance");
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, @"No, continue without using " + ProductInfo.ProductBaseName);
        }

        private void renderInvalidLicenseTD(TaskDialog td)
        {
            td.MainInstruction = @"Your " + ProductInfo.ProductBaseName + @" license is invalid";
            td.MainContent = ProductInfo.ProductBaseName + @" is disabled on this computer.  Details: " + m_license.ErrorMessage;
            td.MainContent += "\n" + cm_assistanceMsg;
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, @"OK, continue without using " + ProductInfo.ProductBaseName);
        }

        private void renderExpirationWarningTD(TaskDialog td)
        {
            td.MainInstruction += @"Your " + ProductInfo.ProductBaseName + @" license subscription expires in " + m_license.DaysToExpiration.ToString() + @" days";
            td.MainContent += @"To renew your subscription, please contact your reseller or PointKnown Sales at ";
            td.MainContent += cm_supportPhone + @" or " + cm_salesEMail;
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, @"OK, continue");
        }

        private void renderBalanceWarningTD(TaskDialog td)
        {
            td.MainInstruction = @"Your " + ProductInfo.ProductBaseName + @" usage balance is low";
            td.MainContent = @"Your balance is: " + m_license.ReserveBalance + @" " + licenseUsageUnits + @". ";
            td.MainContent += @"Please be advised that an Internet connection is required to refill your balance.  ";
            td.MainContent += @"Would you like to refill your usage balance now?";
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink4, @"Yes, refill my usage balance now");
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, @"No, I will refill later");
        }

        private TaskDialog renderActivationFailedTD(string licenseDialogTitle)
        {
            TaskDialog retval = new TaskDialog(licenseDialogTitle);
            retval.TitleAutoPrefix = false;
            retval.MainInstruction = @"Activation Failed";
            retval.MainContent = ProductInfo.ProductBaseName + @" is disabled on this computer." + "\n" + cm_assistanceMsg;
            return retval;
        }

        private static string licenseTDTitle  { get { return (ProductInfo.ProductBaseName + @" License Status"); } }
        private string licenseUsageUnits
        {
            get
            {
                if (m_license.IsMeteredDaily) { return @"Days"; }
                else if (m_license.IsMeteredHourly) { return @"Hours"; }
                else { return null; }
            }
        }

        private void populateRibbon(UIControlledApplication revitApp)
        {
            m_ribbonPanel = revitApp.CreateRibbonPanel(@"PKNail Pro");
            string assembly = Assembly.GetExecutingAssembly().Location;

            PushButton pb = (PushButton)m_ribbonPanel.AddItem(new PushButtonData(cm_cmdNameActivateWall, cm_cmdNameActivateWall,
                                                              assembly, @"PKNailForRevit.ActivateSelectedWallCommand"));
            pb.LargeImage = getImage(@"pack://application:,,,/PKNailForRevit;component/Resources/activateWall.png");
            pb.AvailabilityClassName = @"PKNailForRevit.CommandsAvailability";

            pb = (PushButton)m_ribbonPanel.AddItem(new PushButtonData(cm_cmdNameSetCoordinateSystem, cm_cmdNameSetCoordinateSystem,
                                                   assembly, @"PKNailForRevit.SetCoordinateSystemCommand"));
            pb.LargeImage = getImage(@"pack://application:,,,/PKNailForRevit;component/Resources/setCoordinateSys.png");
            pb.AvailabilityClassName = @"PKNailForRevit.CommandsAvailability";

            PulldownButton pdb = (PulldownButton)m_ribbonPanel.AddItem(new PulldownButtonData(@"Capture", @"Capture"));
            pdb.LargeImage = getImage(@"pack://application:,,,/PKNailForRevit;component/Resources/capture.png");

            pb = pdb.AddPushButton(new PushButtonData(cm_cmdNameSurveyWall, cm_cmdNameSurveyWall,
                                                      assembly, @"PKNailForRevit.SurveyWallCommand"));
            pb.LargeImage = getImage(@"pack://application:,,,/PKNailForRevit;component/Resources/captureWall.png");
            pb.AvailabilityClassName = @"PKNailForRevit.CommandsAvailability";

            pb = pdb.AddPushButton(new PushButtonData(cm_cmdNameSurveyLine, cm_cmdNameSurveyLine,
                                                      assembly, @"PKNailForRevit.SurveyLineDlgCommand"));
            pb.LargeImage = getImage(@"pack://application:,,,/PKNailForRevit;component/Resources/captureLine.png");
            pb.AvailabilityClassName = @"PKNailForRevit.CommandsAvailability";

            pb = pdb.AddPushButton(new PushButtonData(cm_cmdNameSurveyDoor, cm_cmdNameSurveyDoor,
                                                      assembly, @"PKNailForRevit.SurveyDoorDlgCommand"));
            pb.LargeImage = getImage(@"pack://application:,,,/PKNailForRevit;component/Resources/captureDoor.png");
            pb.AvailabilityClassName = @"PKNailForRevit.CommandsAvailability";

            pb = pdb.AddPushButton(new PushButtonData(cm_cmdNameSurveyWindow, cm_cmdNameSurveyWindow,
                                                      assembly, @"PKNailForRevit.SurveyWindowDlgCommand"));
            pb.LargeImage = getImage(@"pack://application:,,,/PKNailForRevit;component/Resources/captureWindow.png");
            pb.AvailabilityClassName = @"PKNailForRevit.CommandsAvailability";

            pb = pdb.AddPushButton(new PushButtonData(cm_cmdNameSurveyHostedObject, cm_cmdNameSurveyHostedObject,
                                                      assembly, @"PKNailForRevit.SurveyHostedObjectDlgCommand"));
            pb.LargeImage = getImage(@"pack://application:,,,/PKNailForRevit;component/Resources/captureWallHostedObject.png");
            pb.AvailabilityClassName = @"PKNailForRevit.CommandsAvailability";

            PushButtonData surveyLevelsBtn = new PushButtonData(cm_cmdNameSurveyLevels, cm_cmdNameSurveyLevels,
                                                                assembly, @"PKNailForRevit.LevelsDlgCommand");
            surveyLevelsBtn.AvailabilityClassName = @"PKNailForRevit.CommandsAvailability";
            PushButtonData copyObjectsBtn = new PushButtonData(cm_cmdNameCopyObjects, cm_cmdNameCopyObjects,
                                                               assembly, @"PKNailForRevit.CopyObjectsCommand");
            copyObjectsBtn.AvailabilityClassName = @"PKNailForRevit.CommandsAvailability";
            PushButtonData convertWallsBtn = new PushButtonData(cm_cmdNameConvertUnknownWidthWalls, cm_cmdNameConvertUnknownWidthWalls,
                                                                assembly, @"PKNailForRevit.ConvertUnknownWidthWallsCommand");
            convertWallsBtn.AvailabilityClassName = @"PKNailForRevit.CommandsAvailability";
            IList<RibbonItem> spb = m_ribbonPanel.AddStackedItems(surveyLevelsBtn, copyObjectsBtn, convertWallsBtn);
            ((PushButton)spb[0]).Image = getImage(@"pack://application:,,,/PKNailForRevit;component/Resources/captureLevels.png");
            ((PushButton)spb[1]).Image = getImage(@"pack://application:,,,/PKNailForRevit;component/Resources/copyLevelToLevel.png");
            ((PushButton)spb[2]).Image = getImage(@"pack://application:,,,/PKNailForRevit;component/Resources/convertWalls.png");
            
            pdb = (PulldownButton)m_ribbonPanel.AddItem(new PulldownButtonData(@"Rangefinder", @"Rangefinder"));
            pdb.LargeImage = getImage(@"pack://application:,,,/PKNailForRevit;component/Resources/rf.png");

            pb = pdb.AddPushButton(new PushButtonData(cm_cmdNameConnectRF, cm_cmdNameConnectRF,
                                                      assembly, @"PKNailForRevit.ConnectRFCommand"));
            pb.LargeImage = getImage(@"pack://application:,,,/PKNailForRevit;component/Resources/rf-connect.png");
            pb.AvailabilityClassName = @"PKNailForRevit.CommandsAvailability";

            pb = pdb.AddPushButton(new PushButtonData(cm_cmdNameDisconnectRF, cm_cmdNameDisconnectRF,
                                                      assembly, @"PKNailForRevit.DisconnectRFCommand"));
            pb.LargeImage = getImage(@"pack://application:,,,/PKNailForRevit;component/Resources/rf-disconnect.png");
            pb.AvailabilityClassName = @"PKNailForRevit.CommandsAvailability";

            pb = pdb.AddPushButton(new PushButtonData(cm_cmdNameChangeKeymap, cm_cmdNameChangeKeymap,
                                                      assembly, @"PKNailForRevit.ChangeKeymapCommand"));
            pb.LargeImage = getImage(@"pack://application:,,,/PKNailForRevit;component/Resources/changeKeypadMap.png");
            pb.AvailabilityClassName = @"PKNailForRevit.CommandsAvailability";

            // Epic 3: import an AI-engine JSON model. Text-only button (no bespoke icon resource) to keep
            // the ribbon registration low-risk; reuses CommandsAvailability (needs an open project doc).
            pb = (PushButton)m_ribbonPanel.AddItem(new PushButtonData(cm_cmdNameImportAiModel, cm_cmdNameImportAiModel,
                                                   assembly, @"PKNailForRevit.ImportAiModelCommand"));
            pb.AvailabilityClassName = @"PKNailForRevit.CommandsAvailability";

            pb = (PushButton)m_ribbonPanel.AddItem(new PushButtonData(cm_cmdNameSettings, cm_cmdNameSettings,
                                                   assembly, @"PKNailForRevit.SettingsDlgCommand"));
            pb.LargeImage = getImage(@"pack://application:,,,/PKNailForRevit;component/Resources/settings.png");
            pb.AvailabilityClassName = @"PKNailForRevit.SettingsAvailability";   // Note different availability class
        }

        private static System.Windows.Media.Imaging.BitmapImage getImage(string uri)
        {
            System.Windows.Media.Imaging.BitmapImage retval = new System.Windows.Media.Imaging.BitmapImage();
            retval.BeginInit();
            retval.UriSource = new Uri(uri);
            retval.EndInit();
            return retval;
        }

        private void ControlledApplication_DocumentCreated(object sender, DocumentCreatedEventArgs e)
        {
          try
          {
            if (IsLicenseValid && e.Status == RevitAPIEventStatus.Succeeded && !e.Document.IsFamilyDocument)
            {
                IProjectModel proj = new RevitDocument(e.Document.Application, e.Document);
                initializeProject(proj);

                if (m_appSettings.IsOpenProjectSettingsOnCreateDocument)
                {
                    string msg = @"";
                    if (proj.InitializeTransaction(ref msg) == CommandResult.Succeeded)
                    {
                        proj.SetTransactionName(@"Modify Document Settings", ref msg);
                        if (proj.StartTransaction(ref msg) == CommandResult.Succeeded)
                        {
                            DocumentSettings docSettings = proj.DocumentSettings;
                            NewProjectSettingsDlg dlg = new NewProjectSettingsDlg(m_appSettings, docSettings, proj.UnitSystem);
                            dlg.ShowDialog();
                            if (dlg.DialogResult == System.Windows.Forms.DialogResult.OK)
                            {
                                proj.DocumentSettings = docSettings;
                                m_appSettings.Save();
                                proj.CommitTransaction(ref msg);
                            }
                            else
                            {
                                proj.RollBackTransaction(ref msg);
                            }
                        }
                    }

                    if (msg.Length > 0) { System.Windows.Forms.MessageBox.Show(msg); }
                }
            }
          }
          catch (Exception ex)
          {
              // A document-event handler must never throw into Revit (Revit flags the add-in as crashed
              // and disables it). A new project that is not PKNail-initialized (no bound parameters) hits
              // this until the user runs an import; swallow it.
              System.Diagnostics.Trace.WriteLine(@"PKNail DocumentCreated handler: " + ex.Message);
          }
        }

        private void controlledApplication_DocumentOpened(object sender, DocumentOpenedEventArgs e)
        {
            if (IsLicenseValid && !e.Document.IsFamilyDocument)
            {
                IProjectModel proj = (IProjectModel)(new RevitDocument(e.Document.Application, e.Document));
                initializeProject(proj);
            }
        }

        private void ControlledApplication_DocumentClosing(object sender, DocumentClosingEventArgs e)
        {
            if (IsLicenseValid)
            {
                IProjectModel proj = (IProjectModel)(new RevitDocument(e.Document.Application, e.Document));
                string msg = @"";
                if (proj.Finalize(ref msg) == CommandResult.Failed)
                {
                    // TODO: open a TaskDialog (although what is there to say at this point?  Should we cancel the closing?)
                }
            }
        }

        private void initializeProject(IProjectModel proj)
        {
            string msg = @"";
            if (proj.InitializeTransaction(ref msg) == CommandResult.Succeeded)
            {
                proj.SetTransactionName(@"Initialize Document", ref msg);
                proj.StartTransaction(ref msg);
                try
                {
                    if (proj.Initialize(ref msg) != CommandResult.Failed)
                    {
                        // Set the startup page of the rangefinder keypad map if it hasn't been set already
                        if (m_currentRFKeymapPage == @"")
                        { m_currentRFKeymapPage = proj.GetRangefinderKeypadMapPages()[0]; }
                        
                        proj.CommitTransaction(ref msg);
                    }
                    else
                    {
                        proj.RollBackTransaction(ref msg);
                        // TODO: open a TaskDialog
                    }
                }
                catch
                { proj.RollBackTransaction(ref msg); }
            }
        }

        private void autoSave(IProjectModel proj)
        {
            if (m_appSettings.AutoSaveInterval == 0 || m_autoSaveActionCount == -1)  { return; }

            m_autoSaveActionCount++;
            if (m_autoSaveActionCount >= m_appSettings.AutoSaveInterval)
            {
                SaveError err;
                string msg = @"";
                if (proj.Save(out err, ref msg) == CommandResult.Succeeded)
                { m_autoSaveActionCount = 0; }
                else
                {
                    if (err == SaveError.InvalidOperation)
                    {
                        // Save is not going to work so disable silently
                        m_autoSaveActionCount = -1;
                    }
                    else
                    {
                        TaskDialog dlg = new TaskDialog(@"Automatic save failed");
                        string mainInstruction = @"";
                        string cmdLink1Text = @"";
                        switch (err)
                        {
                            case SaveError.NoPathSet:
                                mainInstruction = @"There is no file associated with this project yet.";
                                mainInstruction += @"  You must execute a ""Save"" or ""Save As"" command in Revit to save the project";
                                mainInstruction += @" to a file before PKNail Pro for Revit can automatically save the project during a session.";
                                cmdLink1Text = @"Keep autosave enabled; I will save the project into a new file now";
                                break;
                            case SaveError.ReadOnlyFile:
                                mainInstruction = @"The file associated with this project is read-only.  You must save the project to a writable";
                                mainInstruction += @" file before PKNail Pro for Revit can automatically save the project during a session.";
                                cmdLink1Text = @"Keep autosave enabled; I will save the project into a different file now";
                                break;
                            case SaveError.Other:
                                mainInstruction = @"The following error was encountered when PKNail Pro for Revit attempted to save the project: ";
                                mainInstruction += msg;
                                cmdLink1Text = @"Keep autosave enabled; I will resolve the error";
                                break;
                            case SaveError.InvalidOperation:   // This case was handled above
                            default:
                                break;
                        }
                        dlg.MainInstruction = mainInstruction;
                        dlg.MainContent = @"What do you want to do?";
                        dlg.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, cmdLink1Text);
                        dlg.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, @"Disable autosave for this session");
                        Sounds.Hand.Play();
                        switch (dlg.Show())
                        {
                            case TaskDialogResult.CommandLink1:
                                break;
                            case TaskDialogResult.CommandLink2:
                                m_autoSaveActionCount = -1;    // -1 means auto-save is disabled for this session
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }

        private void setAppContext(UIApplication uiapp)
        {
            m_revitUIApp = uiapp;
        }

        private IProjectModel projectModel()
        {
            IProjectModel retval = null;

            UIDocument uiDoc = (m_revitUIApp != null) ? m_revitUIApp.ActiveUIDocument : null;
            Document doc = (uiDoc != null) ? uiDoc.Document : null;
            if (doc != null)
            {
                retval = new RevitDocument(m_revitUIApp.Application, doc);
                retval.TransactionErrorSignal = m_txnErrorSignal;
            }

            return retval;
        }

        private void transactionMonitorThreadWorker()
        {
            while (!m_txnMonitorStop)
            {
                m_txnErrorSignal.WaitOne();
                if (m_txnMonitorStop) { break; }
                Sounds.Hand.Play();
            }
        }
    }

    public sealed class SettingsAvailability : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories)
        {
            return true;
        }
    }

    public sealed class CommandsAvailability : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories)
        {
            return (   applicationData.ActiveUIDocument != null
                    && !applicationData.ActiveUIDocument.Document.IsFamilyDocument
                    && TheApp.Instance.IsLicenseValid);
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class ConnectRFCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cmdData, ref string message, ElementSet elements)
        {
            return TheApp.Instance.ConnectRFCommand_Txn(cmdData, ref message);
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class DisconnectRFCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cmdData, ref string message, ElementSet elements)
        {
            return TheApp.Instance.DisconnectRFCommand_Txn(cmdData, ref message);
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class ChangeKeymapCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cmdData, ref string message, ElementSet elements)
        {
            return TheApp.Instance.ChangeKeymapCommand_Txn(cmdData, ref message);
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class LevelsDlgCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cmdData, ref string message, ElementSet elements)
        {
            return TheApp.Instance.LevelsDlgCommand_Txn(cmdData, ref message);
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class ActivateSelectedWallCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cmdData, ref string message, ElementSet elements)
        {
            return TheApp.Instance.ActivateWallCommand_Txn(cmdData, ref message);
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class SetCoordinateSystemCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cmdData, ref string message, ElementSet elements)
        {
            return TheApp.Instance.SetCoordinateSystemCommand_Txn(cmdData, ref message);
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class SurveyWallCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cmdData, ref string message, ElementSet elements)
        {
            return TheApp.Instance.SurveyWallCommand_Txn(cmdData, ref message);
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class SurveyWindowDlgCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cmdData, ref string message, ElementSet elements)
        {
            return TheApp.Instance.SurveyWindowCommand_Txn(cmdData, ref message);
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class SurveyDoorDlgCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cmdData, ref string message, ElementSet elements)
        {
            return TheApp.Instance.SurveyDoorCommand_Txn(cmdData, ref message);
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class SurveyHostedObjectDlgCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cmdData, ref string message, ElementSet elements)
        {
            return TheApp.Instance.SurveyWallHostedObjectCommand_Txn(cmdData, ref message);
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class SurveyLineDlgCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cmdData, ref string message, ElementSet elements)
        {
            return TheApp.Instance.SurveyLineCommand_Txn(cmdData, ref message);
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class CopyObjectsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cmdData, ref string message, ElementSet elements)
        {
            return TheApp.Instance.CopyObjectsCommand_Txn(cmdData, ref message);
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class ConvertUnknownWidthWallsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cmdData, ref string message, ElementSet elements)
        {
            return TheApp.Instance.ConvertUnknownWidthWallsCommand_Txn(cmdData, ref message);
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class SettingsDlgCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData cmdData, ref string message, ElementSet elements)
        {
            return TheApp.Instance.SettingsDlgCommand_Txn(cmdData, ref message);
        }
    }
}
