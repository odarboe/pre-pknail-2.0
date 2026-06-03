using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PKNailForRevit
{
    internal partial class CopyObjectsDlg : Form
    {
        private CopyObjectsData m_data;

        internal CopyObjectsDlg(CopyObjectsData data)
        {
            m_data = data;
            InitializeComponent();
        }

        internal CopyObjectsData Data { get { return m_data; } }

        private void CopyObjectsDlg_Load(object sender, EventArgs e)
        {
            CommandContext context = m_data.CommandContext;

            for (int i = context.ProjectLevelNames.Count - 1; i >= 0; i--)
            {
                if (i > context.CurrentLevelIdx)
                { UpperLvlCB.Items.Add(context.ProjectLevelNames[i]); }
                else if (i == context.CurrentLevelIdx)
                {
                    LowerLvlCB.Items.Add(@"(do not copy)");
                    UpperLvlCB.Items.Add(@"(do not copy)");
                }
                else
                { LowerLvlCB.Items.Add(context.ProjectLevelNames[i]); }
            }
            UpperLvlCB.SelectedIndex = UpperLvlCB.Items.Count - 1;
            LowerLvlCB.SelectedIndex = 0;

            CurLvlLbl.Text = @"From: " + context.ProjectLevelNames[context.CurrentLevelIdx];

            if (!m_data.IsObjectInSelection[(int)CopyObjectsType.Walls])
            {
                CopyWindowsSelRB.Checked = true;
                CopyWindowsSelRB.Enabled = false;
                CopyWindowsAllRB.Enabled = false;
                CopyWindowsNoneRB.Enabled = false;
                CopyDoorsSelRB.Checked = true;
                CopyDoorsSelRB.Enabled = false;
                CopyDoorsAllRB.Enabled = false;
                CopyDoorsNoneRB.Enabled = false;
                CopyObjsSelRB.Checked = true;
                CopyObjsSelRB.Enabled = false;
                CopyObjsAllRB.Enabled = false;
                CopyObjsNoneRB.Enabled = false;
            }
            else
            {
                if (m_data.IsObjectInSelection[(int)CopyObjectsType.Windows])
                { CopyWindowsSelRB.Checked = true; }
                else
                {
                    CopyWindowsSelRB.Enabled = false;
                    CopyWindowsAllRB.Checked = true;
                }

                if (m_data.IsObjectInSelection[(int)CopyObjectsType.Doors])
                { CopyDoorsSelRB.Checked = true; }
                else
                {
                    CopyDoorsSelRB.Enabled = false;
                    CopyDoorsAllRB.Checked = true;
                }

                if (m_data.IsObjectInSelection[(int)CopyObjectsType.OtherHostedObjects])
                { CopyObjsSelRB.Checked = true; }
                else
                {
                    CopyObjsSelRB.Enabled = false;
                    CopyObjsAllRB.Checked = true;
                }
            }
        }

        private void OKBtn_Click(object sender, EventArgs e)
        {
            m_data.UpperLevelIdx = m_data.CommandContext.ProjectLevelNames.Count - 1 - UpperLvlCB.SelectedIndex;
            m_data.LowerLevelIdx = m_data.CommandContext.CurrentLevelIdx - LowerLvlCB.SelectedIndex;

            if (CopyWindowsNoneRB.Checked)
            { m_data.IncludedObjects[(int)CopyObjectsType.Windows] = CopyObjectsIncludeScope.None; }
            else if (CopyWindowsSelRB.Checked)
            { m_data.IncludedObjects[(int)CopyObjectsType.Windows] = CopyObjectsIncludeScope.Selected; }
            else if (CopyWindowsAllRB.Checked)
            { m_data.IncludedObjects[(int)CopyObjectsType.Windows] = CopyObjectsIncludeScope.All; }

            if (CopyDoorsNoneRB.Checked)
            { m_data.IncludedObjects[(int)CopyObjectsType.Doors] = CopyObjectsIncludeScope.None; }
            else if (CopyDoorsSelRB.Checked)
            { m_data.IncludedObjects[(int)CopyObjectsType.Doors] = CopyObjectsIncludeScope.Selected; }
            else if (CopyDoorsAllRB.Checked)
            { m_data.IncludedObjects[(int)CopyObjectsType.Doors] = CopyObjectsIncludeScope.All; }

            if (CopyObjsNoneRB.Checked)
            { m_data.IncludedObjects[(int)CopyObjectsType.OtherHostedObjects] = CopyObjectsIncludeScope.None; }
            else if (CopyObjsSelRB.Checked)
            { m_data.IncludedObjects[(int)CopyObjectsType.OtherHostedObjects] = CopyObjectsIncludeScope.Selected; }
            else if (CopyObjsAllRB.Checked)
            { m_data.IncludedObjects[(int)CopyObjectsType.OtherHostedObjects] = CopyObjectsIncludeScope.All; }
        }

        private void UpperLvlCB_SelectedIndexChanged(object sender, EventArgs e)
        { setOKBtnState(); }

        private void LowerLvlCB_SelectedIndexChanged(object sender, EventArgs e)
        { setOKBtnState(); }

        private void setOKBtnState()
        {
            if (UpperLvlCB.SelectedIndex == UpperLvlCB.Items.Count - 1
                && LowerLvlCB.SelectedIndex == 0)
            { OKBtn.Enabled = false; }
            else
            { OKBtn.Enabled = true; }
        }
    }
}
