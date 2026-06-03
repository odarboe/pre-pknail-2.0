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
    internal partial class ConvertUnknownWidthWallsDlg : Form
    {

        private ConvertUnknownWidthWallsData m_data;

        internal ConvertUnknownWidthWallsData Data  { get { return m_data; } }

        internal ConvertUnknownWidthWallsDlg(ConvertUnknownWidthWallsData data)
        {
            m_data = data;
            InitializeComponent();
        }

        private void ConvertUnknownWidthWallsDlg_Load(object sender, EventArgs e)
        {
            if (m_data.Selection.Count == 0)
            {
                SelectedRB.Checked = false;
                SelectedRB.Enabled = false;
                AllCurLvlRB.Checked = true;
            }
            else
            {
                SelectedRB.Enabled = true;
                SelectedRB.Checked = true;
            }
        }

        private void OKBtn_Click(object sender, EventArgs e)
        {
            if (SelectedRB.Checked)  { m_data.Scope = ConvertUnknownWidthWallsScope.Selected; }
            else if (AllCurLvlRB.Checked)  { m_data.Scope = ConvertUnknownWidthWallsScope.AllOnCurrentLevel; }
            else if (AllProjectRB.Checked)  { m_data.Scope = ConvertUnknownWidthWallsScope.All; }
        }
    }
}
