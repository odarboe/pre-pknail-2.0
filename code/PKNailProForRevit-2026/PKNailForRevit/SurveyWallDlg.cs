using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PKNailForRevit
{
    internal partial class SurveyWallDlg : DrivableForm
    {
        #region Data Members
        private CommandContext m_cmdContext;
        private WallSurveyParams m_wsParams;
        private List<Geometry2D.Radians> m_cornerAngles = new List<Geometry2D.Radians>();
        private List<Distance> m_defaultLengths = new List<Distance>();
        private bool m_isCornerAngleCBDirty;
        #endregion

        internal SurveyWallDlg(CommandContext cmdContext, WallSurveyParams wsParams)
        {
            m_cmdContext = cmdContext;
            m_wsParams = wsParams;
            InitializeComponent();
            m_OKBtn = OKBtn;

            Distance.Units displayUnits = (m_cmdContext.DisplayUnitSystem == UnitSystem.Imperial) ?
                Distance.Units.Imperial : Distance.Units.MetricMillimeter;
            LengthDC.DisplayUnits = displayUnits;
            LengthDC.TextChanged += new EventHandler(LengthDC_TextChanged);
            LengthDC.RoundingIncrement = m_cmdContext.ModelSettings.LocationRounding;
            WidthDC.DisplayUnits = displayUnits;
            WidthDC.TextChanged += new EventHandler(WidthDC_TextChanged);
            WidthDC.RoundingIncrement = m_cmdContext.ModelSettings.WallWidthRounding;
            HeightDC.DisplayUnits = displayUnits;
            HeightDC.TextChanged += new EventHandler(HeightDC_TextChanged);
            HeightDC.RoundingIncrement = m_cmdContext.ModelSettings.LocationRounding;
            UnknownWidthCheck.KeyPress += new KeyPressEventHandler(UnknownWidthCheck_KeyPress);
            LevelOffsetDC.DisplayUnits = displayUnits;
            LevelOffsetDC.RoundingIncrement = m_cmdContext.ModelSettings.LocationRounding;
        }

        internal WallSurveyParams Params { get { return m_wsParams; } }

        private void surveyWallDlg_Load(object sender, EventArgs e)
        {
            ConnectedLevelCB.Items.Add(@"<measure>");
            for (int i = m_cmdContext.CurrentLevelIdx + 1; i < m_cmdContext.ProjectLevelNames.Count; i++)
            { ConnectedLevelCB.Items.Add(m_cmdContext.ProjectLevelNames[i]); }
            if (m_wsParams.ConnectedLevelIdx != -1)
            { ConnectedLevelCB.SelectedIndex = m_wsParams.ConnectedLevelIdx - m_cmdContext.CurrentLevelIdx; }
            else
            { ConnectedLevelCB.SelectedIndex = 0; }

            LengthDC.SetDistanceClean(m_wsParams.Length);

            int idxToSelect = -1;
            WidthCB.Items.Add(@"<measure>");
            for (int i = 0; i < m_wsParams.ProjectWallWidthList.Count; i++)
            {
                Distance width = m_wsParams.ProjectWallWidthList[i];
                WidthCB.Items.Add(wallWidthCBLabel(width, m_cmdContext.DisplayUnitSystem));
                if (width == m_wsParams.Width)  { idxToSelect = i + 1; }
            }
            WidthCB.SelectedIndex = (idxToSelect != -1) ? idxToSelect : 0;
            WidthDC.SetDistanceClean(m_wsParams.Width);

            HeightDC.SetDistanceClean(m_wsParams.Height);
            LevelOffsetDC.SetDistanceClean(m_wsParams.LevelOffset);

            CloseCheck.Enabled = m_wsParams.IsCloseWall;

            if (!m_wsParams.IsFirstWall)
            {
                initCornerAngleCB();
                if (CornerAngleCB.Items.Count > 0) { CornerAngleCB.SelectedIndex = 0; }

                UnknownWidthCheck.Checked = m_wsParams.IsUnknownWidth;
            }

            if (!m_wsParams.IsCreateWall)
            { UnknownWidthCheck.Enabled = false; }

            setState();
        }

        private void initCornerAngleCB()
        {
            for (int i = 0; i < m_wsParams.CornerAngleList.Count; i++)
            {
                Distance defaultDistance = (i < m_wsParams.MaxLengthList.Count) ? m_wsParams.MaxLengthList[i] : Distance.NoValue;
                insertNewCornerAngle(i, m_wsParams.CornerAngleList[i], defaultDistance);
            }
            m_isCornerAngleCBDirty = false;
        }

        private void CloseCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (CloseCheck.Checked == true)
            {
                m_wsParams.Length = LengthDC.DistanceValue;
                LengthDC.SetDistanceClean(m_wsParams.CloseLength);
                CornerAngleCB.Text = m_wsParams.CloseCornerAngle.AsDegrees().ToString(@"#.##");
            }
            else
            {
                LengthDC.SetDistanceClean(m_wsParams.Length);
                CornerAngleCB.SelectedIndex = 0;
            }

            setState();
        }

        private void CornerAngleMeasureBtn_Click(object sender, EventArgs e)
        {
            // Only give the dialog a default length along the survey wall if it was measured
            // (not if it is the default for the default corner angle)
            Distance defaultSurveyWallLength = (LengthDC.IsDirty) ? LengthDC.DistanceValue : Distance.NoValue;

            MeasureCornerAngleDlg dlg = new MeasureCornerAngleDlg(m_wsParams.ReferenceFaceLength,
                                                                  defaultSurveyWallLength,
                                                                  m_cmdContext.ModelSettings.LocationRounding,
                                                                  m_cmdContext.DisplayUnitSystem,
                                                                  true, true);
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
                insertNewCornerAngle(0, ca, dlg.SurveyWallLength);
                CornerAngleCB.SelectedIndex = 0;
            }

            setState();
        }

        private void OKBtn_Click(object sender, EventArgs e)
        {
            if (!CloseCheck.Checked)
            {
                if (LengthDC.IsDirty || CornerAngleCB.SelectedIndex == -1 || !m_wsParams.IsCreateWall)
                { m_wsParams.Length = LengthDC.DistanceValue; }
                else
                { m_wsParams.Length = m_defaultLengths[CornerAngleCB.SelectedIndex]; }

                setCornerAngle();
            }
            else
            {
                m_wsParams.Length = m_wsParams.CloseLength;
                m_wsParams.CornerAngle = m_wsParams.CloseCornerAngle;
            }

            m_wsParams.Width = WidthDC.DistanceValue;

            m_wsParams.Height = HeightDC.DistanceValue;
            if (ConnectedLevelCB.SelectedIndex != 0)
            { m_wsParams.ConnectedLevelIdx = ConnectedLevelCB.SelectedIndex + m_cmdContext.CurrentLevelIdx; }
            else
            { m_wsParams.ConnectedLevelIdx = -1; }

            m_wsParams.LevelOffset = LevelOffsetDC.DistanceValue;
        }

        private void setCornerAngle()
        {
            if (CornerAngleCB.SelectedIndex == -1)
            {
                double ca;
                if (Double.TryParse(CornerAngleCB.Text, out ca))  // TODO: handle parse errors
                { m_wsParams.CornerAngle = Geometry2D.Radians.FromDegrees(ca); }
            }
            else
            {
                m_wsParams.CornerAngle = m_cornerAngles[CornerAngleCB.SelectedIndex];
            }
        }

        private void insertNewCornerAngle(int idx, Geometry2D.Radians angle, Distance defaultLength)
        {
            m_cornerAngles.Insert(idx, angle);
            m_defaultLengths.Insert(idx, defaultLength);
            CornerAngleCB.Items.Insert(idx, angle.AsDegrees().ToString(@"#.##"));
        }

        private void LengthDC_TextChanged(object sender, EventArgs e)
        { setState(); }

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

            if (m_wsParams.IsCreateWall && !LengthDC.IsDirty)
            { LengthDC.SetDistanceClean(m_defaultLengths[idx]); }
            setState();
        }

        private void WidthDC_TextChanged(object sender, EventArgs e)
        { setState(); }

        private void HeightDC_TextChanged(object sender, EventArgs e)
        {
            if (HeightDC.IsDirty)
            { ConnectedLevelCB.SelectedIndex = 0; }
            setState();
        }

        private void UnknownWidthCheck_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '+')
            { UnknownWidthCheck.Checked = true; }
            else if (e.KeyChar == '-')
            { UnknownWidthCheck.Checked = false; }
        }

        private void WidthCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (WidthCB.SelectedIndex != 0)
            { WidthDC.SetDistanceClean(m_wsParams.ProjectWallWidthList[WidthCB.SelectedIndex - 1]); }
            else
            { WidthDC.ClearDistance(); }
        }

        private void UnknownWidthCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (UnknownWidthCheck.Checked == true)
            {
                m_wsParams.IsUnknownWidth = true;
                WidthDC.SetDistanceClean(Distance.NoValue);
            }
            else
            {
                m_wsParams.IsUnknownWidth = false;
                WidthDC.SetDistanceClean(m_wsParams.Width);
            }

            syncWidthCB();
            setState();
        }

        private void syncWidthCB()
        {
            WidthCB.SelectedIndexChanged -= WidthCB_SelectedIndexChanged;

            Distance width = WidthDC.DistanceValue;
            if (!width.IsNoValue)
            {
                string labelMatch = wallWidthCBLabel(WidthDC.DistanceValue, m_cmdContext.DisplayUnitSystem);
                int i;
                for (i = 1; i < WidthCB.Items.Count; i++)
                {
                    if ((string)WidthCB.Items[i] == labelMatch)
                    { WidthCB.SelectedIndex = i; break; }
                }
                if (i == WidthCB.Items.Count) { WidthCB.SelectedIndex = 0; }
            }
            else { WidthCB.SelectedIndex = 0; }

            WidthCB.SelectedIndexChanged += WidthCB_SelectedIndexChanged;
        }

        private void ConnectedLevelCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ConnectedLevelCB.SelectedIndex != 0)
            { HeightDC.SetDistanceClean(m_wsParams.ProjectLevelVisibleHeightList[ConnectedLevelCB.SelectedIndex + m_cmdContext.CurrentLevelIdx - 1]); }
            else
            { HeightDC.ClearDistance(); }
        }

        private void LevelOffsetDC_Validated(object sender, EventArgs e)
        { setState(); }

        private void setState()
        {
            bool isCreateWall = m_wsParams.IsCreateWall;
            bool isFirstWall = m_wsParams.IsFirstWall;

            Distance LengthD = LengthDC.DistanceValue;

            LengthDC.Enabled = !CloseCheck.Checked;
            LengthLbl.Enabled = !CloseCheck.Checked;

            CornerAngleCB.Enabled = (!CloseCheck.Checked && isCreateWall && !isFirstWall);
            CornerAngleLbl.Enabled = (!CloseCheck.Checked && isCreateWall && !isFirstWall);
            CornerAngleMeasureBtn.Enabled = (!CloseCheck.Checked && isCreateWall && !isFirstWall);

            WidthDC.Enabled = !UnknownWidthCheck.Checked && isCreateWall;
            WidthCB.Enabled = !UnknownWidthCheck.Checked && isCreateWall;
            WidthLbl.Enabled = !UnknownWidthCheck.Checked && isCreateWall;

            if (LevelOffsetDC.DistanceValue.IsNoValue || LevelOffsetDC.DistanceValue.IsZero)
            { LevelOffsetDC.ClearTBBackgroundColor(); }
            else
            { LevelOffsetDC.SetTBBackgroundColor(Color.Orange); }

            OKBtn.Enabled = (   !LengthD.IsNoValue
                             && (!WidthDC.DistanceValue.IsNoValue || !WidthDC.Enabled || UnknownWidthCheck.Checked)
                             && !HeightDC.DistanceValue.IsNoValue);
        }

        private string wallWidthCBLabel(Distance d, UnitSystem unitSystem)
        {
            return d.RenderShortDistance(unitSystem) + Distance.ShortDistanceUnitsLabel(unitSystem);
        }

        private void CornerAngleCB_TextUpdate(object sender, EventArgs e)
        {
            m_isCornerAngleCBDirty = true;
        }

        private void CornerAngleCB_Validating(object sender, CancelEventArgs e)
        {
            if (m_isCornerAngleCBDirty)
            {
                double caD;
                if (Double.TryParse(CornerAngleCB.Text, out caD))
                {
                    Geometry2D.Radians ca = Geometry2D.Radians.FromDegrees(caD);
                    int newSelIdx = -1;

                    // See if the user typed the value of an existing corner angle
                    for (int i = 0; i < m_cornerAngles.Count; i++)
                    { if (m_cornerAngles[i] == ca) { newSelIdx = i; } }
                    if (newSelIdx == -1)
                    {
                        newSelIdx = 0;
                        insertNewCornerAngle(newSelIdx, ca, Distance.NoValue);
                    }

                    CornerAngleCB.SelectedIndex = newSelIdx;
                }
                else
                { e.Cancel = true; }   // TODO: add an error provider
            }
            m_isCornerAngleCBDirty = false;
        }
    }
}
