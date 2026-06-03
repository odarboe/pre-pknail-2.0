using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace PKNailForRevit
{
    internal partial class SettingsAppCtl : UserControl
    {
        private PKNailAppSettings m_appSettings;

        internal SettingsAppCtl(PKNailAppSettings appSettings)
        {
            m_appSettings = appSettings;

            InitializeComponent();

            UserNameTB.Text = m_appSettings.UserName;
            COMPortTB.Text = m_appSettings.RangefinderComPort;
            if (m_appSettings.IsAutoDetectComPort)
            {
                AutoDetectCOMPortChkBx.Checked = true;
                COMPortTB.Enabled = false;
            }
            else
            {
                AutoDetectCOMPortChkBx.Checked = false;
                COMPortTB.Enabled = true;
            }
            AutoSaveTB.Text = m_appSettings.AutoSaveInterval.ToString();
            IsShowSettingsOnNewProjectChkBx.Checked = m_appSettings.IsOpenProjectSettingsOnCreateDocument;
        }

        private void UserNameTB_Validated(object sender, EventArgs e)
        { m_appSettings.UserName = UserNameTB.Text.Trim(); }

        private void COMPortTB_Validated(object sender, EventArgs e)
        { m_appSettings.RangefinderComPort = COMPortTB.Text; }

        private void AutoDetectCOMPortChkBx_CheckedChanged(object sender, EventArgs e)
        {
            if (AutoDetectCOMPortChkBx.Checked)
            {
                m_appSettings.IsAutoDetectComPort = true;
                COMPortTB.Enabled = false;
            }
            else
            {
                m_appSettings.IsAutoDetectComPort = false;
                COMPortTB.Enabled = true;
            }
        }

        private void AutoSaveTB_Validated(object sender, EventArgs e)
        {
            int valI;
            if (AutoSaveTB.Text == "")
            { m_appSettings.AutoSaveInterval = 0; }
            else if (Int32.TryParse(AutoSaveTB.Text, out valI))
            { m_appSettings.AutoSaveInterval = valI; }
        }

        private void IsShowSettingsOnNewProjectChkBx_CheckedChanged(object sender, EventArgs e)
        { m_appSettings.IsOpenProjectSettingsOnCreateDocument = IsShowSettingsOnNewProjectChkBx.Checked; }
    }
}
