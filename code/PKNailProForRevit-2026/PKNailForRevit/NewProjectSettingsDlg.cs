using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PKNailForRevit
{
    internal partial class NewProjectSettingsDlg : Form
    {
        private PKNailAppSettings m_appSettings = null;
        private DocumentSettings m_docSettings = null;

        internal NewProjectSettingsDlg(PKNailAppSettings appSettings, DocumentSettings docSettings, UnitSystem us)
        {
            m_appSettings = appSettings;
            m_docSettings = docSettings;

            InitializeComponent();

            ProjectNameTB.Text = (m_docSettings.ProjectName.Length > 0) ?
                m_docSettings.ProjectName : m_appSettings.LastProjectName;
            UserNameTB.Text = m_appSettings.UserName;

            Distance.Units displayUnits = (us == UnitSystem.Imperial) ?
                Distance.Units.Imperial : Distance.Units.MetricMillimeter;
            LocationRoundingSDC.DisplayUnits = displayUnits;
            FeatureRoundingSDC.DisplayUnits = displayUnits;
            WallWidthSDC.DisplayUnits = displayUnits;
            GeometryAdjustmentSDC.DisplayUnits = displayUnits;

            LocationRoundingSDC.SetValue(m_docSettings.LocationRounding);
            FeatureRoundingSDC.SetValue(m_docSettings.FeatureDimensionRounding);
            WallWidthSDC.SetValue(m_docSettings.WallWidthRounding);
            GeometryAdjustmentSDC.SetValue(m_docSettings.MeasurementTolerance);
        }

        private void LocationRoundingSDC_Validated(object sender, EventArgs e)
        { m_docSettings.LocationRounding = LocationRoundingSDC.GetValue(); }

        private void WallWidthSDC_Validated(object sender, EventArgs e)
        { m_docSettings.WallWidthRounding = WallWidthSDC.GetValue(); }

        private void FeatureRoundingSDC_Validated(object sender, EventArgs e)
        { m_docSettings.FeatureDimensionRounding = FeatureRoundingSDC.GetValue(); }

        private void GeometryAdjustmentSDC_Validated(object sender, EventArgs e)
        { m_docSettings.MeasurementTolerance = GeometryAdjustmentSDC.GetValue(); }

        private void DoneBtn_Click(object sender, EventArgs e)
        {
            m_docSettings.ProjectName = ProjectNameTB.Text.Trim();
            m_appSettings.LastProjectName = m_docSettings.ProjectName;
            m_appSettings.UserName = UserNameTB.Text.Trim();
            if (DontShowChkBx.Checked)  { m_appSettings.IsOpenProjectSettingsOnCreateDocument = false; }

            DialogResult = System.Windows.Forms.DialogResult.OK;
            Close();
        }
    }
}
