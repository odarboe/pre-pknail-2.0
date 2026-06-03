using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PKNailForRevit
{
    internal partial class SurveyDoorDlg : SurveyWallHostedObjectForm
    {
        internal SurveyDoorDlg(CommandContext cmdContext, WallHostedObjectSurveyParams whosParams)
            : base(cmdContext, whosParams)
        {
            InitializeComponent();
            bindControls(LocationDC, WidthDC, WidthLbl, HeightDC, HeightLbl,
                         null, null, SillHeightDC, SillHeightLbl, CenterChkBx, TypeCB, OKBtn);
        }

        protected override void typeCB_SelectedIndexChanged(object sender, EventArgs args)
        {
            base.typeCB_SelectedIndexChanged(sender, args);

            if (m_typeCB.SelectedIndex != -1)
            {
                // Set up lateral orientation combo box
                SurveyFamilyDef.LateralOrientationType lateralOrientationType = m_whosParams.GetLateralOrientationTypeVal();
                if (lateralOrientationType == SurveyFamilyDef.LateralOrientationType.Hinged)
                {
                    LateralOrientationLbl.Visible = true;
                    LateralOrientationLbl.Text = @"Hinge Side";
                    LateralOrientationCB.Items.Clear();
                    LateralOrientationCB.Items.AddRange(new Object[] { "Left", "Right" });
                    LateralOrientationCB.SelectedIndex = (m_whosParams.GetLateralOrientation() == (int)SurveyFamilyDef.HingeSide.Left) ? 0 : 1;
                    LateralOrientationCB.Enabled = true;
                }
                else if (lateralOrientationType == SurveyFamilyDef.LateralOrientationType.Sliding)
                {
                    LateralOrientationLbl.Visible = true;
                    LateralOrientationLbl.Text = @"Slide Direction";
                    LateralOrientationCB.Items.Clear();
                    LateralOrientationCB.Items.AddRange(new Object[] { "Right to Left", "Left to Right" });
                    LateralOrientationCB.SelectedIndex = (m_whosParams.GetLateralOrientation() == (int)SurveyFamilyDef.SlideDirection.RightToLeft) ? 0 : 1;
                    LateralOrientationCB.Enabled = true;
                }
                else  // lateralOrientationType == None
                {
                    LateralOrientationLbl.Visible = false;
                    LateralOrientationCB.Items.Clear();
                    LateralOrientationCB.Enabled = false;
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
            else
            {
                LateralOrientationLbl.Visible = false;
                LateralOrientationCB.Items.Clear();
                LateralOrientationCB.Enabled = false;
                FacingOrientationLbl.Visible = false;
                FacingOrientationCB.Items.Clear();
                FacingOrientationCB.Enabled = false;
            }
        }

        protected override void OKBtn_Click(object sender, EventArgs args)
        {
            base.OKBtn_Click(sender, args);

            if (m_whosParams.GetLateralOrientationTypeVal() != SurveyFamilyDef.LateralOrientationType.None)
            { m_whosParams.SetLateralOrientation(LateralOrientationCB.SelectedIndex); }
            if (m_whosParams.GetFacingOrientationTypeVal() != SurveyFamilyDef.FacingOrientationType.None)
            { m_whosParams.SetFacingOrientation(FacingOrientationCB.SelectedIndex); }
        }
    }
}
