using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PKNailForRevit
{
    internal partial class FirstWallDlg : DrivableForm
    {
        private CommandContext m_cmdContext;
        private WallSurveyParams m_wsParams;

        internal FirstWallDlg(CommandContext cmdContext, WallSurveyParams wsParams)
        {
            m_cmdContext = cmdContext;
            m_wsParams = wsParams;
            InitializeComponent();
            m_OKBtn = OKBtn;
        }

        public WallSurveyParams Params { get { return m_wsParams; } }

        private void FirstWallDlg_Load(object sender, EventArgs e)
        {
            LabelTB.Text = "You are capturing the first wall in this project.  Please answer the following questions to orient this project properly:";
            LabelTB.Select(0, 0);
            PerspectiveCB.Items.AddRange(new Object[] { "Outside", "Inside" });
            PerspectiveCB.SelectedIndex = 0;   // Outside
            DirectionCB.Items.AddRange(new Object[] { "Don't know", "North", "East", "South", "West" });
            DirectionCB.SelectedIndex = 0;   // Don't know
        }

        private void OKBtn_Click(object sender, EventArgs e)
        {
            if (PerspectiveCB.SelectedIndex == 0)
            { m_cmdContext.SurveyOrientation = SurveyOrientation.Flipped; }
            m_wsParams.SurveyorCompassDirection = (CompassDirection)DirectionCB.SelectedIndex;
        }
    }
}
