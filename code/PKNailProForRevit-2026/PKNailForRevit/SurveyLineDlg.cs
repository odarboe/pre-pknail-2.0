using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PKNailForRevit
{
    internal partial class SurveyLineDlg : DrivableForm
    {
        private CommandContext m_cmdContext;
        private LineSurveyParams m_lsParams;
        private List<Geometry2D.Radians> m_cornerAngles = new List<Geometry2D.Radians>();

        internal SurveyLineDlg(CommandContext cmdContext, LineSurveyParams lsParams)
        {
            m_cmdContext = cmdContext;
            m_lsParams = lsParams;
            InitializeComponent();
            m_OKBtn = OKBtn;

            Distance.Units displayUnits = (m_cmdContext.DisplayUnitSystem == UnitSystem.Imperial) ?
                Distance.Units.Imperial : Distance.Units.MetricMillimeter;
            LocationDC.DisplayUnits = displayUnits;
            LocationDC.TextChanged += new EventHandler(LocationDC_TextChanged);
            LocationDC.RoundingIncrement = m_cmdContext.ModelSettings.LocationRounding;
            LengthDC.DisplayUnits = displayUnits;
            LengthDC.TextChanged += new EventHandler(LengthDC_TextChanged);
            LengthDC.RoundingIncrement = m_cmdContext.ModelSettings.LocationRounding;
        }

        internal LineSurveyParams Params { get { return m_lsParams; } }

        private void surveyLineDlg_Load(object sender, EventArgs e)
        {
            LocationDC.SetDistanceClean(m_lsParams.Location);
            LengthDC.SetDistanceClean(m_lsParams.Length);

            foreach (string s in m_lsParams.LineStyles)  { TypeCB.Items.Add(s); }
            TypeCB.SelectedIndex = m_lsParams.LineStyleIdx;

            initCornerAngleCB();
            if (CornerAngleCB.Items.Count > 0) { CornerAngleCB.SelectedIndex = 0; }

            if (m_lsParams.IsParallel)
            {
                LengthLbl.Enabled = false;
                LengthDC.Enabled = false;
                CornerAngleLbl.Enabled = false;
                CornerAngleCB.Enabled = false;
            }

            if (!m_lsParams.IsReferenceFaceWall)
            {
                LocationLbl.Enabled = false;
                LocationDC.Enabled = false;
                CenterChkBx.Enabled = false;
            }

            setState();
        }

        private void LocationDC_TextChanged(object sender, EventArgs e)
        { setState(); }

        private void CenterChkBx_CheckedChanged(object sender, EventArgs e)
        {
            if (CenterChkBx.Checked)
            {
                LocationDC.SetDistanceDirty(new Distance(m_lsParams.ReferenceFaceLength.ValueRevitInternal / 2.0, Distance.Units.RevitInternal));
                LocationDC.Enabled = false;
            }
            else
            {
                LocationDC.ClearDistance();
                LocationDC.Enabled = true;
            }
            setState();
        }

        private void CornerAngleCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = CornerAngleCB.SelectedIndex;
            if (idx == -1)
            {
                // Ensure that there is a selected item
                CornerAngleCB.SelectedIndexChanged -= CornerAngleCB_SelectedIndexChanged;
                idx = 0;
                CornerAngleCB.SelectedIndex = idx;
                CornerAngleCB.SelectedIndexChanged += CornerAngleCB_SelectedIndexChanged;
            }

            if (!LengthDC.IsDirty)
            { LengthDC.SetDistanceClean(m_lsParams.MaxLengthList[idx]); }
            setState();
        }

        private void CornerAngleMeasureBtn_Click(object sender, EventArgs e)
        {
            Distance refLength = Distance.NoValue;
            if (m_lsParams.IsReferenceFaceWall)
            { refLength = LocationDC.DistanceValue; }
            else
            { refLength = m_lsParams.ReferenceFaceLength; }

            // Only give the dialog a default length along the survey wall if it was measured
            // (not if it is the default for the default corner angle)
            Distance defaultSurveyLength = LengthDC.IsDirty ? LengthDC.DistanceValue : Distance.NoValue;

            MeasureCornerAngleDlg dlg = new MeasureCornerAngleDlg(refLength, defaultSurveyLength,
                                                                  m_cmdContext.ModelSettings.LocationRounding,
                                                                  m_cmdContext.DisplayUnitSystem,
                                                                  m_lsParams.IsReferenceFaceWall, false);

            dlg.DriveAndShowDialog(this, m_mps, m_dnps);

            if (dlg.DialogResult == DialogResult.OK
                && !dlg.RefWallLength.IsNoValue
                && !dlg.SurveyWallLength.IsNoValue
                && !dlg.DiagonalLength.IsNoValue)
            {
                Geometry2D.Radians ca
                    = Geometry2D.Radians.FromScaleneTriangle(dlg.RefWallLength.ValueRevitInternal,
                                                             dlg.SurveyWallLength.ValueRevitInternal,
                                                             dlg.DiagonalLength.ValueRevitInternal);
                CornerAngleCB.Items.Insert(0, ca.AsDegrees().ToString(@"#.##"));
                m_lsParams.MaxLengthList.Insert(0, dlg.SurveyWallLength);
                m_cornerAngles.Insert(0, ca);
                CornerAngleCB.SelectedIndex = 0;

                LengthDC.SetDistanceClean(dlg.SurveyWallLength);
            }

            setState();
        }

        private void LengthDC_TextChanged(object sender, EventArgs e)
        { setState(); }

        private void OKBtn_Click(object sender, EventArgs e)
        {
            m_lsParams.Location = (CenterChkBx.Checked) ? LocationDC.DistanceValueNotRounded : LocationDC.DistanceValue;

            if (TypeCB.SelectedIndex != -1)  { m_lsParams.LineStyleIdx = TypeCB.SelectedIndex; }

            if (   LengthDC.IsDirty
                || CornerAngleCB.SelectedIndex == -1 || CornerAngleCB.SelectedIndex >= m_lsParams.MaxLengthList.Count)
            { m_lsParams.Length = LengthDC.DistanceValue; }
            else
            { m_lsParams.Length = m_lsParams.MaxLengthList[CornerAngleCB.SelectedIndex]; }

            m_lsParams.CornerAngle = m_cornerAngles[CornerAngleCB.SelectedIndex];
        }

        private void setState()
        {
            bool isLocationDCValidValue = (!LocationDC.DistanceValue.IsNoValue || !LocationDC.Enabled);
            bool isLengthDCValidValue = (!LengthDC.DistanceValue.IsNoValue || !LengthDC.Enabled);
            CornerAngleMeasureBtn.Enabled = isLocationDCValidValue;
            OKBtn.Enabled = (isLocationDCValidValue && isLengthDCValidValue);
        }

        private void initCornerAngleCB()
        {
            foreach (Geometry2D.Radians cornerAngle in m_lsParams.CornerAngleList)
            {
                m_cornerAngles.Add(cornerAngle);
                CornerAngleCB.Items.Add(cornerAngle.AsDegrees().ToString(@"F0"));
            }
        }
    }
}
