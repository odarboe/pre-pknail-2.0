using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace PKNailForRevit
{

    internal partial class SplashForm : Form
    {
        // Data members
        private delegate void CloseSplashDelegate();
        private CloseSplashDelegate m_closeDelegate;
        private delegate void UpdateStatusDelegate(string msg);
        private UpdateStatusDelegate m_updateStatusDelegate;
        private delegate void HideSplashDelegate();
        private HideSplashDelegate m_hideSplashDelegate;
        private delegate void ShowSplashDelegate();
        private ShowSplashDelegate m_showSplashDelegate;
        private static SplashForm sm_instance = null;

        private SplashForm(string productName, string productVersion, string copyright, string initialStatus)
        {
            InitializeComponent();
            m_closeDelegate = new CloseSplashDelegate(internalCloseSplash);
            m_updateStatusDelegate = new UpdateStatusDelegate(internalUpdateStatus);
            m_hideSplashDelegate = new HideSplashDelegate(internalHideSplash);
            m_showSplashDelegate = new ShowSplashDelegate(internalShowSplash);
            VersionLbl.BackColor = Color.Transparent;
            VersionLbl.Text = @"Version " + productVersion;
            CopyrightLbl.BackColor = Color.Transparent;
            CopyrightLbl.Text = copyright;
            RightsLbl.BackColor = Color.Transparent;
            StatusLbl.BackColor = Color.Transparent;
            StatusLbl.Text = initialStatus;
        }

        private SplashForm() { }

        //start a new thread to display splash window
        internal static void StartSplash(string productName, string productVersion, string copyright, string initialStatus)
        {
#if (!BYPASS_SPLASH)
            sm_instance = new SplashForm(productName, productVersion, copyright, initialStatus);
            (new Thread(splashThreadFunc)).Start();
#endif
        }

        internal static bool IsStarted  { get { return (sm_instance != null); } }

        internal static void UpdateStatus(string msg)
        { if (sm_instance != null) { sm_instance.Invoke(sm_instance.m_updateStatusDelegate, new object[] { msg }); } }

        internal static void HideSplash()
        { if (sm_instance != null) { sm_instance.Invoke(sm_instance.m_hideSplashDelegate); } }

        internal static void ShowSplash()
        { if (sm_instance != null) { sm_instance.Invoke(sm_instance.m_showSplashDelegate); } }

        internal static void StopSplash()
        { if (sm_instance != null) { sm_instance.Invoke(sm_instance.m_closeDelegate); } }

        // this is called by the new thread to show the splash screen
        private static void splashThreadFunc()
        {
            if (sm_instance != null)
            {
                sm_instance.TopMost = true;
                sm_instance.Show();
                Application.Run(sm_instance);
            }
        }

        private void internalUpdateStatus(string msg)
        { StatusLbl.Text = msg; }

        private void internalCloseSplash()
        { Close(); Dispose(); }

        private void internalHideSplash()
        { Hide(); }

        private void internalShowSplash()
        { TopMost = true; Show(); }
    }
}
