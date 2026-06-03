using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Autodesk.Revit.UI;

namespace PKNailForRevit
{
    internal class UpdateCheck
    {
        private const int cm_numDaysInUpdateCheckPeriod = 7;
        private const int cm_updateCheckTimeout = 8000;  // milliseconds

#if (!BYPASS_UPDATE_CHECK)
        private PKNailAppSettings m_appSettings;
        private UpdateCheckInfo m_updateCheckInfo;
#endif

        internal UpdateCheck(PKNailAppSettings appSettings)
        {
#if (!BYPASS_UPDATE_CHECK)
            m_appSettings = appSettings;
            m_updateCheckInfo = null;
#endif
        }

        internal bool CheckForNewVersion()
        { return CheckForNewVersion(false); }

        internal bool CheckForNewVersion(bool isForce)
        {
            bool retval = false;

#if (!BYPASS_UPDATE_CHECK)
            if (!isForce && !m_appSettings.IsPeriodicUpdateCheck)  { return retval; }

            DateTime today = DateTime.Now.Date;
            if (isForce || (int)Math.Round((today - m_appSettings.LastUpdateCheck.Date).TotalDays) >= cm_numDaysInUpdateCheckPeriod)
            {
                string url = ProductInfo.ProductEsdUrlRoot + @"/latestVersion.php";
                url += @"?product=" + ProductInfo.ProductFolderName;

                try
                {
                    string updateCheckInfoJson;
                    using (HttpClient httpClient = new HttpClient())
                    {
                        httpClient.Timeout = TimeSpan.FromMilliseconds(cm_updateCheckTimeout);
                        updateCheckInfoJson = httpClient.GetStringAsync(url).GetAwaiter().GetResult();
                    }

                    if (updateCheckInfoJson != @"")
                    {
                        m_updateCheckInfo = JsonSerializable.FromJson<UpdateCheckInfo>(updateCheckInfoJson);

                        string assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                        FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assemblyPath);

                        if (fvi.FileVersion != m_updateCheckInfo.Version
                            && (isForce || m_appSettings.SkipVersion != m_updateCheckInfo.Version))
                        { retval = true; }

                        m_appSettings.LastUpdateCheck = today;
                        m_appSettings.Save();
                    }
                }
                catch { }  // This is usually a no-no but the update check is such a low priority function that it is
                           // not even worth it to report or log an error
            }
#endif
            return retval;
        }

        internal void GetUserResponse()
        {
#if (!BYPASS_UPDATE_CHECK)
            TaskDialog td = new TaskDialog(@"Update Found");
            td.TitleAutoPrefix = false;
            td.MainInstruction =   @"An updated version of " + ProductInfo.ProductBaseName + @" (v"
                                 + ProductInfo.GetUserVersion(m_updateCheckInfo.Version)
                                 + @", released on " + m_updateCheckInfo.ReleaseDate + @"), is available";
            td.MainContent = @"What would you like to do?";
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, @"Download this update now");
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, @"Read what is new in this update");
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, @"Skip this update");
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink4, @"Remind me later");

            switch (td.Show())
            {
                case TaskDialogResult.CommandLink1:
                    using (TaskDialog dlDlg = new TaskDialog(@"Downloading Update"))
                    {
                        dlDlg.TitleAutoPrefix = false;
                        dlDlg.MainInstruction = @"IMPORTANT: You must exit completely from Revit before installing the update.";
                        dlDlg.CommonButtons = TaskDialogCommonButtons.Ok;
                        dlDlg.Show();
                    }
                    Process.Start(new ProcessStartInfo(m_updateCheckInfo.DownloadURL) { UseShellExecute = true });
                    break;
                case TaskDialogResult.CommandLink2:
                    Process.Start(new ProcessStartInfo(m_updateCheckInfo.ChangelogURL) { UseShellExecute = true });
                    td.Dispose();
                    GetUserResponse();  // Re-render the dialog to give the user a chance to download
                    break;
                case TaskDialogResult.CommandLink3:
                    m_appSettings.SkipVersion = m_updateCheckInfo.Version;
                    m_appSettings.Save();
                    break;
                case TaskDialogResult.CommandLink4:
                case TaskDialogResult.Close:
                case TaskDialogResult.Cancel:
                default:
                    break;
            }
#endif
        }

    }

    public class UpdateCheckInfo : JsonSerializable
    {
        public string Version;
        public string ReleaseDate;
        public string DownloadURL;
        public string ChangelogURL;
    }
}
