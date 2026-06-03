using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PKNailForRevit
{
    internal partial class SettingsUpdatesCtl : UserControl
    {
        private PKNailAppSettings m_appSettings = null;

        internal SettingsUpdatesCtl(PKNailAppSettings appSettings)
        {
            m_appSettings = appSettings;
            InitializeComponent();
            PeriodicUpdateCheckCB.Checked = (m_appSettings.IsPeriodicUpdateCheck);
        }

        private void PeriodicUpdateCheckCB_CheckedChanged(object sender, EventArgs e)
        {
            m_appSettings.IsPeriodicUpdateCheck = PeriodicUpdateCheckCB.Checked;
        }

        private void CheckNowLinkLbl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            UpdateCheck uc = new UpdateCheck(m_appSettings);
            if (uc.CheckForNewVersion(true))
            {
                uc.GetUserResponse();
            }
            else
            {
                MessageBox.Show(@"Your installation of " + ProductInfo.ProductName + @" is up to date", @"Check For Updates");
            }
        }
    }
}
