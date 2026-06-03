using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace PKNailForRevit
{
    internal partial class SettingsProjectGeneralCtl : UserControl
    {
        private PKNailAppSettings m_appSettings;
        private DocumentSettings m_docSettings;

        internal SettingsProjectGeneralCtl(PKNailAppSettings appSettings, DocumentSettings docSettings)
        {
            m_appSettings = appSettings;
            m_docSettings = docSettings;
            InitializeComponent();
        }

        private void SettingsProjectGeneralCtl_Load(object sender, EventArgs e)
        {
            IsStructuralChkBx.Checked = m_docSettings.IsStructuralWalls;
            ProjectNameTB.Text = m_docSettings.ProjectName;
        }

        private void IsStructuralChkBx_CheckedChanged(object sender, EventArgs e)
        { m_docSettings.IsStructuralWalls = IsStructuralChkBx.Checked; }

        private void ProjectNameTB_Leave(object sender, EventArgs e)
        {
            m_docSettings.ProjectName = ProjectNameTB.Text.Trim();
            m_appSettings.LastProjectName = m_docSettings.ProjectName;
        }
    }
}
