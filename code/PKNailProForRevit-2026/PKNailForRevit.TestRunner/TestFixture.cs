using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Diagnostics;

using NUnit.Framework;

using PKNailForRevit.TestCore;

namespace PKNailForRevit.TestRunner
{
    [TestFixture]
    public partial class PKNailForRevitTestFixture
    {
        private const string cm_rootDirPath = @"..\..\..\";
        private const string cm_testDirPath = @"..\..\tests\";
        private const string cm_revitAppPath = @"C:\Program Files\Autodesk\Revit 2017\Revit.exe";
        private const string cm_defaultProjFilePath = @"test.rvt";
        private const string cm_autoitAppName = @"autoit3.exe";
        private const string cm_dotCoverAppName = @"dotCover.exe";
        private const string cm_coverageConfigFileName = @"coverageConfig.xml";
        private const string cm_coverageResultsDir = @"coverageResults";

        private void runFunctionalTest(string testName, string revitProjectFile, string automationScriptFile, string refModelStateFile)
        {
            string rootDir = getRootDir();
            System.IO.DirectoryInfo rootDirInfo = new System.IO.DirectoryInfo(rootDir);

            // Determine the environment in which this test is running
            string isBuildServerEnvVar = Environment.GetEnvironmentVariable(@"PK_HOST_IS_BUILD_SERVER");
            bool isRunOnBuildServer = (isBuildServerEnvVar != null);
            string isIntegrationBuildEnvVar = Environment.GetEnvironmentVariable(@"PK_IS_INTEGRATION_BUILD");
            bool isIntegrationBuild = (isIntegrationBuildEnvVar != null);

            // Delete the model export file if present
            System.IO.FileInfo[] exportFileInfoArr = rootDirInfo.GetFiles(TestConfig.ExportModelFilename, System.IO.SearchOption.TopDirectoryOnly);
            if (exportFileInfoArr.Length > 0)
            {
                foreach (System.IO.FileInfo exportFileInfo in exportFileInfoArr)
                { exportFileInfo.Delete(); }
            }

            ModelState modelState = null;
            int numAttemptsLeft = (isIntegrationBuild) ? 3 : 1;
            bool isTestRunCompleted = false;
            while (!isTestRunCompleted)
            {
                // Start up Revit
                Process revitProcess = launchRevit(testName, revitProjectFile, isRunOnBuildServer);

                // Wait for the Revit main window to appear
                System.Threading.Thread.Sleep(10000);

                // Run the automation script
                Process scriptProcess = launchAutomationScript(automationScriptFile, isRunOnBuildServer);

                // Wait until the model file appears, indicating that the playback is finished
                int checkInterval = 2;  // seconds
                int numIntervalsToTimeOut = 150;   // = 5 minutes
                while (modelState == null && numIntervalsToTimeOut > 0)
                {
                    System.Threading.Thread.Sleep(checkInterval * 1000);
                    exportFileInfoArr = rootDirInfo.GetFiles(TestConfig.ExportModelFilename, System.IO.SearchOption.TopDirectoryOnly);
                    if (exportFileInfoArr.Length != 0)
                    {
                        System.Threading.Thread.Sleep(500);
                        modelState = RevitModelStateFactory.ImportRevitModelState(rootDir + TestConfig.ExportModelFilename);
                    }
                    numIntervalsToTimeOut--;
                }

                numAttemptsLeft--;
                if (modelState != null || numAttemptsLeft == 0) { isTestRunCompleted = true; }

                int deathWatchLimit = 5;  // seconds to wait for processes to die
                while ((!scriptProcess.HasExited || !revitProcess.HasExited) && deathWatchLimit > 0)
                { System.Threading.Thread.Sleep(1000); deathWatchLimit--; }

                if (!revitProcess.HasExited)  { revitProcess.Kill(); }
                if (!scriptProcess.HasExited) { scriptProcess.Kill(); }
            }

            if (modelState == null)
            { Assert.Fail("Timed out waiting for the application to export its model state"); }

            // Slurp up the reference model
            ModelState refModelState = RevitModelStateFactory.ImportRevitModelState(rootDir + refModelStateFile);

            // Compare the exported model state to the reference state
            string diffMsg;
            if (!modelState.Compare(refModelState, out diffMsg))
            { Assert.Fail(diffMsg); }
        }

        private Process launchAutomationScript(string automationScriptFilePath, bool isRunOnBuildServer)
        {
            string args = automationScriptFilePath;
            if (isRunOnBuildServer) { args += @" runOnBuildServer"; }
            ProcessStartInfo psi = new ProcessStartInfo(cm_autoitAppName, args);
            psi.UseShellExecute = false;
            psi.WorkingDirectory = getRootDir();
            return Process.Start(psi);
        }

        private Process launchRevit(string testName, string revitProjectFilePath, bool isRunOnBuildServer)
        {
            string rootDir = getRootDir();
            string args = ((isRunOnBuildServer) ? @"/viewer " : "") + revitProjectFilePath;

            ProcessStartInfo psi;
            psi = new ProcessStartInfo(cm_revitAppPath, args);
            psi.WorkingDirectory = rootDir;
            psi.UseShellExecute = false;

            return Process.Start(psi);
        }

        private string getRootDir()
        { return getAssemblyDir() + System.IO.Path.DirectorySeparatorChar + cm_rootDirPath; }

        private string getTestDir()
        { return getAssemblyDir() + System.IO.Path.DirectorySeparatorChar + cm_testDirPath; }

        private string getAssemblyDir()
        {
            Uri assemblyUri = new Uri(Assembly.GetExecutingAssembly().CodeBase);
            return System.IO.Path.GetDirectoryName(assemblyUri.AbsolutePath);
        }
    }
}
