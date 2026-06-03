using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace PKNailForRevit
{
    internal partial class SettingsProjectRoundingCtl : UserControl
    {
        private DocumentSettings m_docSettings;

        internal SettingsProjectRoundingCtl(DocumentSettings docSettings, UnitSystem us)
        {
            m_docSettings = docSettings;

            InitializeComponent();

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
    }
}
