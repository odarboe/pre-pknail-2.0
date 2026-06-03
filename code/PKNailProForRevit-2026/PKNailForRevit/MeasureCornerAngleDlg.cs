using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PKNailForRevit
{
    internal partial class MeasureCornerAngleDlg : DrivableForm
    {
        protected Distance m_refWallLength;
        protected Distance m_surveyWallLength;
        protected Distance m_diagLength;
        protected bool m_isRefWall;       // vs line
        protected bool m_isSurveyWall;    // vs line

        internal MeasureCornerAngleDlg(Distance refWallLength, Distance surveyWallLength,
                                       Distance roundingIncrement, UnitSystem unitSystem,
                                       bool isRefWall, bool isSurveyWall)
        {
            m_refWallLength = refWallLength;
            m_surveyWallLength = surveyWallLength;
            m_isRefWall = isRefWall;
            m_isSurveyWall = isSurveyWall;
            InitializeComponent();
            Distance.Units displayUnits = (unitSystem == UnitSystem.Imperial) ?
                Distance.Units.Imperial : Distance.Units.MetricMillimeter;
            RefDC.DisplayUnits = displayUnits;
            RefDC.TextChanged += new EventHandler(RefDC_TextChanged);
            RefDC.RoundingIncrement = roundingIncrement;
            SurveyDC.DisplayUnits = displayUnits;
            SurveyDC.TextChanged += new EventHandler(SurveyDC_TextChanged);
            SurveyDC.RoundingIncrement = roundingIncrement;
            DiagDC.DisplayUnits = displayUnits;
            DiagDC.TextChanged += new EventHandler(DiagDC_TextChanged);
            DiagDC.RoundingIncrement = roundingIncrement;
            m_OKBtn = OKBtn;
        }

        internal Distance RefWallLength { get { return m_refWallLength; } }
        internal Distance SurveyWallLength { get { return m_surveyWallLength; } }
        internal Distance DiagonalLength { get { return m_diagLength; } }

        private void MeasureCornerAngleDlg_Load(object sender, EventArgs e)
        {
            RefDC.SetDistanceClean(m_refWallLength);
            SurveyDC.SetDistanceClean(m_surveyWallLength);
            DiagDC.SetDistanceClean(Distance.NoValue);

            RefDC.Enabled = false;
            RefDCLbl.Text = @"Length on Reference ";
            RefDCLbl.Text += m_isRefWall ? @"Wall" : @"Line";
            RefChooseCheck.Enabled = m_isRefWall;

            SurveyDC.Enabled = m_surveyWallLength.IsNoValue;
            SurveyDCLbl.Text = @"Length on Survey ";
            SurveyDCLbl.Text += m_isSurveyWall ? @"Wall" : @"Line";
            SurveyChooseCheck.Enabled = !m_surveyWallLength.IsNoValue;
            SurveyChooseCheck.Checked = m_surveyWallLength.IsNoValue;

            setState();
        }

        private void RefDC_TextChanged(object sender, EventArgs e)
        { setState(); }

        private void SurveyDC_TextChanged(object sender, EventArgs e)
        { setState(); }

        private void DiagDC_TextChanged(object sender, EventArgs e)
        { setState(); }

        private void RefChooseCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (RefChooseCheck.Checked)
            { RefDC.Enabled = true; }
            else
            {
                RefDC.SetDistanceClean(m_refWallLength);
                RefDC.Enabled = false;
            }
        }

        private void SurveyChooseCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (SurveyChooseCheck.Checked)
            { SurveyDC.Enabled = true; }
            else
            {
                SurveyDC.SetDistanceClean(m_surveyWallLength);
                SurveyDC.Enabled = false;
            }
        }

        private void OKBtn_Click(object sender, EventArgs e)
        {
            if (RefDC.IsDirty) { m_refWallLength = RefDC.DistanceValueNotRounded; }
            if (SurveyDC.IsDirty) { m_surveyWallLength = SurveyDC.DistanceValueNotRounded; }
            m_diagLength = DiagDC.DistanceValueNotRounded;

            DialogResult = DialogResult.OK;
            this.Close();
        }

        private void CancelBtn_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void setState()
        {
            OKBtn.Enabled = (!RefDC.IsDistanceNoValue && !SurveyDC.IsDistanceNoValue && !DiagDC.IsDistanceNoValue);
        }
    }
}
