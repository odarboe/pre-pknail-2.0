using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PKNailForRevit
{
    internal partial class AddLvlDlg : Form
    {
        #region Data Members
        private List<String> m_existingLvlNames;
        #endregion

        internal AddLvlDlg(List<string> existingLvlNames, string prompt)
        {
            m_existingLvlNames = existingLvlNames;
            InitializeComponent();
            PromptTB.Text = prompt;
            YesRB.Checked = true;
        }

        internal String NewLvlName { get { return NameTB.Text; } }

        private void NameTB_Validating(object sender, CancelEventArgs e)
        {
            NameEP.SetError(NameTB, "");
            e.Cancel = false;

            if (NameTB.Text.Trim() == "")
            {
                NameEP.SetError(NameTB, "You must name the new level");
                e.Cancel = true;
            }
            else
            {
                bool isMatch = false;
                for (int i = 0; i < m_existingLvlNames.Count; i++)
                { if (NameTB.Text == m_existingLvlNames[i]) { isMatch = true; break; } }
                if (isMatch)
                {
                    NameEP.SetError(NameTB, "The name of the new level must be unique");
                    e.Cancel = true;
                }
            }
        }

        private void YesRB_CheckedChanged(object sender, EventArgs e)
        {
            NameTB.Enabled = YesRB.Checked;
        }

        private void DoneBtn_Click(object sender, EventArgs e)
        {
            DialogResult = (YesRB.Checked) ? DialogResult.OK : DialogResult.Cancel;
        }
    }
}
