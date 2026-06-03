using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PKNailForRevit
{
    internal partial class SurveyWindowDlg : SurveyWallHostedObjectForm
    {
        #region Data Members
        private Distance m_savedHeight;
        private Distance m_savedSillHeight;
        #endregion

        internal SurveyWindowDlg(CommandContext cmdContext, WallHostedObjectSurveyParams whosParams)
            : base(cmdContext, whosParams)
        {
            m_savedHeight = Distance.NoValue;
            m_savedSillHeight = Distance.NoValue;
            InitializeComponent();
            bindControls(LocationDC, WidthDC, WidthLbl, HeightDC, HeightLbl,
                         null, null, SillHeightDC, SillHeightLbl, CenterChkBx, TypeCB, OKBtn);
        }

        internal SurveyWindowDlg() : base() { }

        protected override void form_Load(object sender, EventArgs e)
        {
            base.form_Load(sender, e);

            if (m_whosParams.IsTypicalWindowHeights)
            {
                HeightDC.SetDistanceClean(m_whosParams.TypicalHeight);
                SillHeightDC.SetDistanceClean(m_whosParams.TypicalSillHeight);
                TypicalHeightsChkBx.Checked = true;
                StackWindowsChkBx.Enabled = m_whosParams.IsStackedWindows;
                StackWindowsChkBx.Checked = false;
            }
            else
            {
                TypicalHeightsChkBx.Enabled = false;
                StackWindowsChkBx.Enabled = false;
                StackWindowsChkBx.Checked = false;
                HeightDC.SetDistanceClean(m_whosParams.GetHeight());
                SillHeightDC.SetDistanceClean(m_whosParams.GetSillHeight());
            }
        }

        protected override void typeCB_SelectedIndexChanged(object sender, EventArgs args)
        {
            base.typeCB_SelectedIndexChanged(sender, args);

            if (TypeCB.SelectedIndex != -1)
            {
                if (!TypicalHeightsChkBx.Checked)
                {
                    HeightDC.SetDistanceClean(m_whosParams.GetHeight());
                    SillHeightDC.SetDistanceClean(m_whosParams.GetSillHeight());
                }

                // Set up facing orientation combo box
                SurveyFamilyDef.FacingOrientationType facingOrientationType = m_whosParams.GetFacingOrientationTypeVal();
                if (facingOrientationType == SurveyFamilyDef.FacingOrientationType.Swinging)
                {
                    FacingOrientationLbl.Visible = true;
                    FacingOrientationLbl.Text = @"Swing Direction";
                    FacingOrientationCB.Items.Clear();
                    FacingOrientationCB.Items.AddRange(new Object[] { "Out", "In" });
                    FacingOrientationCB.SelectedIndex = (m_whosParams.GetFacingOrientation() == (int)SurveyFamilyDef.SwingDirection.Out) ? 0 : 1;
                    FacingOrientationCB.Enabled = true;
                }
                else if (facingOrientationType == SurveyFamilyDef.FacingOrientationType.Facing)
                {
                    FacingOrientationLbl.Visible = true;
                    FacingOrientationLbl.Text = @"Side Facing";
                    FacingOrientationCB.Items.Clear();
                    FacingOrientationCB.Items.AddRange(new Object[] { "Interior", "Exterior" });
                    FacingOrientationCB.SelectedIndex = (m_whosParams.GetFacingOrientation() == (int)SurveyFamilyDef.SurveyorRelation.Interior) ? 0 : 1;
                    FacingOrientationCB.Enabled = true;
                }
                else  // facingOrientationType == None
                {
                    FacingOrientationLbl.Visible = false;
                    FacingOrientationCB.Items.Clear();
                    FacingOrientationCB.Enabled = false;
                }
            }

            setState();
        }

        private void TypicalHeightsChkBx_CheckedChanged(object sender, EventArgs e)
        {
            if (TypicalHeightsChkBx.Checked)
            {
                if (HeightDC.IsDirty)  { m_savedHeight = HeightDC.DistanceValueNotRounded; }
                HeightDC.TextChanged -= dc_TextChanged;
                HeightDC.SetDistanceClean(m_whosParams.TypicalHeight);
                HeightDC.TextChanged += dc_TextChanged;
                if (SillHeightDC.IsDirty) { m_savedSillHeight = SillHeightDC.DistanceValueNotRounded; }
                SillHeightDC.TextChanged -= dc_TextChanged;
                SillHeightDC.SetDistanceClean(m_whosParams.TypicalSillHeight);
                SillHeightDC.TextChanged += dc_TextChanged;
                StackWindowsChkBx.Enabled = m_whosParams.IsStackedWindows;
            }
            else
            {
                StackWindowsChkBx.Checked = false;
                StackWindowsChkBx.Enabled = false;
                if (!HeightDC.IsDirty && !SillHeightDC.IsDirty)
                {
                    if (m_savedHeight.IsNoValue)
                    { HeightDC.SetDistanceClean(m_whosParams.GetHeight()); }
                    else
                    { HeightDC.SetDistanceClean(m_savedHeight); }
                    if (m_savedSillHeight.IsNoValue)
                    { SillHeightDC.SetDistanceClean(m_whosParams.GetSillHeight()); }
                    else
                    { SillHeightDC.SetDistanceClean(m_savedSillHeight); }
                }
            }
        }

        protected override void OKBtn_Click(object sender, EventArgs args)
        {
            base.OKBtn_Click(sender, args);

            m_whosParams.IsStackedWindows = StackWindowsChkBx.Checked;
            m_whosParams.SetFacingOrientation(FacingOrientationCB.SelectedIndex);
        }

        protected override void setState()
        {
            TypicalHeightsChkBx.CheckedChanged -= TypicalHeightsChkBx_CheckedChanged;
            TypicalHeightsChkBx.Checked = (   TypicalHeightsChkBx.Enabled
                                           && !SillHeightDC.DistanceValueNotRounded.IsNoValue && SillHeightDC.DistanceValue == m_whosParams.TypicalSillHeight
                                           && !HeightDC.DistanceValueNotRounded.IsNoValue && HeightDC.DistanceValue == m_whosParams.TypicalHeight );
            TypicalHeightsChkBx.CheckedChanged += TypicalHeightsChkBx_CheckedChanged;
            base.setState();
        }
    }
}
