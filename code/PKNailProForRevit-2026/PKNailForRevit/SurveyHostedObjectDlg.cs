using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PKNailForRevit
{
    internal partial class SurveyHostedObjectDlg : SurveyWallHostedObjectForm
    {
        internal SurveyHostedObjectDlg(CommandContext cmdContext, WallHostedObjectSurveyParams whosParams)
            : base(cmdContext, whosParams)
        {
            InitializeComponent();
            bindControls(LocationDC, WidthDC, WidthLbl, HeightDC, HeightLbl,
                         DepthDC, DepthLbl, SillHeightDC, SillHeightLbl,
                         CenterChkBx, TypeCB, OKBtn);
        }
    }
}
