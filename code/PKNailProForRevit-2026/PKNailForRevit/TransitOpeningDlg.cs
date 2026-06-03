using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PKNailForRevit
{
    internal partial class TransitOpeningDlg : DrivableForm
    {
        private ReferenceOpeningParams m_roParams;
        private SurveyDirection m_dir;

        internal TransitOpeningDlg(CommandContext cmdContext, ReferenceOpeningParams roParams)
        {
            m_roParams = roParams;
            m_dir = cmdContext.SurveyDirection;
            InitializeComponent();
            m_OKBtn = OKBtn;

            Distance.Units displayUnits = (cmdContext.DisplayUnitSystem == UnitSystem.Imperial) ?
                Distance.Units.Imperial : Distance.Units.MetricMillimeter;
            Distance locRounding = cmdContext.ModelSettings.LocationRounding;
            WallRefDC.DisplayUnits = displayUnits;
            WallRefDC.RoundingIncrement = locRounding;
            WallRefDC.TextChanged += new EventHandler(WallRefDC_TextChanged);
            OpeningRefDC.RoundingIncrement = locRounding;
            OpeningRefDC.DisplayUnits = displayUnits;
            OpeningRefDC.TextChanged += new EventHandler(OpeningRefDC_TextChanged);
            LevelOffsetDC.DisplayUnits = displayUnits;
            LevelOffsetDC.RoundingIncrement = locRounding;
        }

        internal ReferenceOpeningParams Params { get { return m_roParams; } }

        private void TransitOpeningDlg_Load(object sender, EventArgs e)
        {
            int leftAlign = DiagramPnl.Location.X;
            int rightAlign = leftAlign + DiagramPnl.Size.Width;
            if (m_dir == SurveyDirection.LeftToRight)
            {
                CornerToOpeningLbl.Text = @"Left Corner To Opening";
            }
            else
            {
                int toMove = rightAlign - OpeningRefDC.Size.Width - OpeningRefDC.Location.X;
                OpeningRefDC.Location = new Point(OpeningRefDC.Location.X + toMove, OpeningRefDC.Location.Y);
                CornerToOpeningLbl.Location = new Point(CornerToOpeningLbl.Location.X + toMove,
                                                        CornerToOpeningLbl.Location.Y);
                CornerToOpeningLbl.Text = @"Right Corner To Opening";
            }

            WallRefDC.SetDistanceClean(m_roParams.WallExtent);
            OpeningRefDC.SetDistanceClean(m_roParams.CornerToOpening);
            LevelOffsetDC.SetDistanceClean(m_roParams.LevelOffset);

            setState();
        }

        private void DiagramPnl_Paint(object sender, PaintEventArgs e)
        {
            Bitmap bmp;
            if (m_dir == SurveyDirection.LeftToRight)
            {
                if (OpeningRefDC.ContainsFocus)
                { bmp = Properties.Resources.TransitOpeningLeftCornerFocusDiagram; }
                else if (WallRefDC.ContainsFocus)
                { bmp = Properties.Resources.TransitOpeningLeftOverallFocusDiagram; }
                else
                { bmp = Properties.Resources.TransitOpeningLeftNoFocusDiagram; }
            }
            else
            {
                if (OpeningRefDC.ContainsFocus)
                { bmp = Properties.Resources.TransitOpeningRightCornerFocusDiagram; }
                else if (WallRefDC.ContainsFocus)
                { bmp = Properties.Resources.TransitOpeningRightOverallFocusDiagram; }
                else
                { bmp = Properties.Resources.TransitOpeningRightNoFocusDiagram; }
            }
            bmp.MakeTransparent(System.Drawing.Color.White);
            e.Graphics.DrawImage(bmp, 0, 0);
        }

        private void WallRefDC_TextChanged(object sender, EventArgs e)
        { setState(); }

        private void OpeningRefDC_TextChanged(object sender, EventArgs e)
        { setState(); }

        private void LevelOffsetDC_Validated(object sender, EventArgs e)
        { setState(); }

        private void setState()
        {
            if (LevelOffsetDC.DistanceValue.IsNoValue || LevelOffsetDC.DistanceValue.IsZero)
            { LevelOffsetDC.ClearTBBackgroundColor(); }
            else
            { LevelOffsetDC.SetTBBackgroundColor(Color.Orange); }

            OKBtn.Enabled = (!WallRefDC.DistanceValue.IsNoValue && !OpeningRefDC.DistanceValue.IsNoValue);
        }

        private void OKBtn_Click(object sender, EventArgs e)
        {
            if (WallRefDC.IsDirty && !WallRefDC.DistanceValue.IsNoValue) { m_roParams.WallExtent = WallRefDC.DistanceValue; }
            if (OpeningRefDC.IsDirty && !OpeningRefDC.DistanceValue.IsNoValue) { m_roParams.CornerToOpening = OpeningRefDC.DistanceValue; }
            if (LevelOffsetDC.IsDirty && !LevelOffsetDC.DistanceValue.IsNoValue) { m_roParams.LevelOffset = LevelOffsetDC.DistanceValue; }
        }

        private void OpeningRefDC_Enter(object sender, EventArgs e)
        { DiagramPnl.Invalidate(); }

        private void OpeningRefDC_Leave(object sender, EventArgs e)
        { DiagramPnl.Invalidate(); }

        private void WallRefDC_Enter(object sender, EventArgs e)
        { DiagramPnl.Invalidate(); }

        private void WallRefDC_Leave(object sender, EventArgs e)
        { DiagramPnl.Invalidate(); }
    }
}
