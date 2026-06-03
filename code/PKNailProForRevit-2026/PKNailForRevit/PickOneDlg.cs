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
    internal partial class PickOneDlg : DrivableForm
    {
        private int m_pickedIdx;
        private List<string> m_choices;

        internal int PickedIndex { get { return m_pickedIdx; } }

        internal PickOneDlg(List<string> choices, int pickedIdx)
        {
            m_choices = choices;
            m_pickedIdx = pickedIdx;
            InitializeComponent();
            m_OKBtn = OKBtn;
        }

        private void PickOneDlg_Load(object sender, EventArgs e)
        {
            foreach (string s in m_choices)
            { ChooseLB.Items.Add(s); }
            ChooseLB.SelectedIndex = m_pickedIdx;
        }

        private void OKBtn_Click(object sender, EventArgs e)
        {
            m_pickedIdx = ChooseLB.SelectedIndex;
        }
    }
}
