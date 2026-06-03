using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PKNailForRevit
{
    internal partial class NumLvlDlg : Form
    {
        internal NumLvlDlg()
        {
            InitializeComponent();
        }

        internal int NumLevels
        {
            get
            {
                int retval;
                Int32.TryParse(NumLvlsTB.Text, out retval);
                return retval;
            }
        }

        internal bool IsDropCeilings { get { return YesRB.Checked; } }

        private void NumLevelsDlg_Load(object sender, EventArgs e)
        {
        }

        private void OKBtn_Click(object sender, EventArgs e)
        {
            int numLvls;
            Int32.TryParse(NumLvlsTB.Text, out numLvls);
            if (numLvls < 1)
            {
                MessageBox.Show("Please specify the number of floors/levels, which must be 1 or more",
                                "Levels", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (!YesRB.Checked && !NoRB.Checked)
            {
                MessageBox.Show("Please specify whether the building contains drop ceilings",
                                "Levels", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else { DialogResult = DialogResult.OK; Close(); }
        }

        private void NumLevelsDlg_Shown(object sender, EventArgs e)
        {
            YesRB.Checked = false;
            NoRB.Checked = false;
            NumLvlsTB.Focus();
        }
    }
}
