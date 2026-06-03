using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PKNailForRevit
{
    internal partial class SettingsRFKeypadMapCtl : UserControl
    {
        private DocumentSettings m_docSettings = null;
        private List<string> m_familyCategories = null;
        private ComboBox[] m_cursorKeyCBs = { null, null, null, null, null, null, null, null };
        private int[] m_cursorKeyCBIndices = { 0, 1, 3, 4, 5, 7 };
        private TextBox m_editTB;
        private IButtonControl m_savedCancelBtn;
        private IButtonControl m_savedAcceptBtn;
        private int m_savedPagesLBSelIndex;
        private bool m_isCursorKeyCBsSubscribed;

        internal SettingsRFKeypadMapCtl(DocumentSettings docSettings, List<string> familyCategories)
        {
            m_docSettings = docSettings;
            m_familyCategories = familyCategories;
            m_familyCategories.Remove(FamilyCategory.Walls);  // Walls is always mapped to the right and left keys in all maps
            InitializeComponent();

            // Set up the textbox we will use for entering the names of new map pages
            m_editTB = new TextBox();
            m_editTB.KeyDown += new KeyEventHandler(m_editTB_KeyDown);
            m_editTB.Enter += new EventHandler(m_editTB_Enter);
            m_editTB.Leave += new EventHandler(m_editTB_Leave);
            m_editTB.Hide();
            Controls.Add(m_editTB);
            m_savedCancelBtn = null;
            m_savedAcceptBtn = null;
            m_isCursorKeyCBsSubscribed = false;
        }

        private void SettingsRFKeypadMapCtl_Load(object sender, EventArgs e)
        {
            // Set the index of the combo box array to match the integer value of the RangefinderCommand.CursorKeyDef enum
            cursorKeyCBs_mapToCursorKeyDefs();

            foreach (string page in m_docSettings.RFKeypadMaps.GetAllPages())
            { PagesLB.Items.Add(page); }

            // Populate the combox boxes (the first item is blank or "none")
            foreach (int idx in m_cursorKeyCBIndices)
            {
                m_cursorKeyCBs[idx].Items.Add(@"");
                foreach (string familyCategory in m_familyCategories)
                { m_cursorKeyCBs[idx].Items.Add(familyCategory); }
            }

            if (PagesLB.Items.Count > 0) { PagesLB.SelectedIndex = 0; }

            foreach (int idx in m_cursorKeyCBIndices)
            { m_cursorKeyCBs[idx].SelectedIndexChanged += cursorKeyCB_SelectedIndexChanged; }
            m_isCursorKeyCBsSubscribed = true;
        }

        private void cursorKeyCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!m_isCursorKeyCBsSubscribed) { return; }

            ComboBox cb = (ComboBox)sender;
            int keyIdx = (int)(cb.Tag);
            string familyCategory = (string)cb.SelectedItem;
            KeypadCommand rfCmd = new KeypadCommand((KeypadCommand.KeyDef)keyIdx, 1);
            string page  = (string)PagesLB.SelectedItem;
            if (familyCategory == @"")
            { m_docSettings.RFKeypadMaps.DeleteCommandinPage(page, rfCmd); }
            else
            { m_docSettings.RFKeypadMaps.AddCommandInPage(page, rfCmd, familyCategory); }
        }

        private void PagesLB_SelectedIndexChanged(object sender, EventArgs e)
        {
            m_isCursorKeyCBsSubscribed = false;

            foreach (int idx in m_cursorKeyCBIndices)
            { if (m_cursorKeyCBs[idx].Items.Count > 0) { m_cursorKeyCBs[idx].SelectedIndex = 0; } }

            if (PagesLB.SelectedIndex != -1)
            {
                string page = (string)PagesLB.Items[PagesLB.SelectedIndex];
                Dictionary<KeypadCommand, string> map = m_docSettings.RFKeypadMaps.GetMapAtPage(page);
                foreach (KeypadCommand rfCmd in map.Keys)
                { m_cursorKeyCBs[(int)rfCmd.CursorKey].SelectedItem = map[rfCmd]; }

                bool isCBEnabled = !(page == RevitDocument.RangefinderKeypadMaps.StandardMapName);
                foreach (int idx in m_cursorKeyCBIndices) { m_cursorKeyCBs[idx].Enabled = isCBEnabled; }
            }
            else
            {
                foreach (int idx in m_cursorKeyCBIndices)  { m_cursorKeyCBs[idx].Enabled = false; }
            }

            m_isCursorKeyCBsSubscribed = true;

            DeleteBtn.Enabled = (PagesLB.SelectedIndex > 0);
        }

        private void NewBtn_Click(object sender, EventArgs e)
        {
            m_savedPagesLBSelIndex = PagesLB.SelectedIndex;
            PagesLB.SelectedIndex = -1;
            PagesLB.Items.Add(@"(new map)");
            int tbMargin = 0;
            Rectangle itemRect = PagesLB.GetItemRectangle(PagesLB.Items.Count - 1);
            m_editTB.Location = new Point(itemRect.X + PagesLB.Location.X - tbMargin, itemRect.Y + PagesLB.Location.Y - tbMargin);
            m_editTB.Size = new Size(itemRect.Width + (2 * tbMargin), itemRect.Height + (2 * tbMargin));
            m_editTB.Text = PagesLB.Items[PagesLB.Items.Count - 1].ToString();
            m_editTB.BringToFront();
            m_editTB.Show();
            m_editTB.Focus();
            
        }

        private void DeleteBtn_Click(object sender, EventArgs e)
        {
            if (PagesLB.SelectedIndex > 0)
            {
                string page = (string)PagesLB.Items[PagesLB.SelectedIndex];
                m_docSettings.RFKeypadMaps.DeletePage(page);
                PagesLB.Items.RemoveAt(PagesLB.SelectedIndex);
            }
        }

        private void m_editTB_KeyDown(object sender, KeyEventArgs e)
        {
           switch (e.KeyCode)
            {
                case Keys.Escape:
                    editTB_cancel();
                    editTB_leaveEditMode();
                    e.Handled = true;
                    break;
                case Keys.Enter:
                    editTB_accept();
                    editTB_leaveEditMode();
                    e.Handled = true;
                    break;
                default:
                    e.Handled = false;
                    break;
            }
        }

        void m_editTB_Enter(object sender, EventArgs e)
        {
            editTB_enterEditMode();
        }

        private void m_editTB_Leave(object sender, EventArgs e)
        {
            editTB_accept();
            editTB_leaveEditMode();
        }

        private void editTB_accept()
        {
            PagesLB.Items[PagesLB.Items.Count - 1] = m_editTB.Text;
            m_docSettings.RFKeypadMaps.AddPage(m_editTB.Text);
            PagesLB.SelectedIndex = PagesLB.Items.Count - 1;
        }

        private void editTB_cancel()
        {
            PagesLB.Items.RemoveAt(PagesLB.Items.Count - 1);
            PagesLB.SelectedIndex = m_savedPagesLBSelIndex;
        }

        private void editTB_enterEditMode()
        {
            m_savedCancelBtn = ParentForm.CancelButton;
            ParentForm.CancelButton = null;
            m_savedAcceptBtn = ParentForm.AcceptButton;
            ParentForm.AcceptButton = null;
            NewBtn.Enabled = false;
            DeleteBtn.Enabled = false;
        }

        private void editTB_leaveEditMode()
        {
            m_editTB.Leave -= m_editTB_Leave;
            m_editTB.Hide();
            m_editTB.Leave += m_editTB_Leave;
            ParentForm.CancelButton = m_savedCancelBtn;
            ParentForm.AcceptButton = m_savedAcceptBtn;
            NewBtn.Enabled = true;
            DeleteBtn.Enabled = (PagesLB.SelectedIndex > 0);
            
        }

        private void cursorKeyCBs_mapToCursorKeyDefs()
        {
            // Set the index of the combo box array to match the integer value of the RangefinderCommand.CursorKeyDef enum
            UpTB.Tag = 0;
            m_cursorKeyCBs[0] = UpTB;
            UpRightTB.Tag = 1;
            m_cursorKeyCBs[1] = UpRightTB;
            DownRightTB.Tag = 3;
            m_cursorKeyCBs[3] = DownRightTB;
            DownTB.Tag = 4;
            m_cursorKeyCBs[4] = DownTB;
            DownLeftTB.Tag = 5;
            m_cursorKeyCBs[5] = DownLeftTB;
            UpLeftTB.Tag = 7;
            m_cursorKeyCBs[7] = UpLeftTB;
        }

        //private void cursorKeyCBs_subscribeToSelectionChangeEvent(bool isSubscribe)
        //{
        //    foreach (int i in m_cursorKeyCBIndices)
        //    {
        //        if (isSubscribe)
        //        { m_cursorKeyCBs[i].SelectedIndexChanged += cursorKeyCB_SelectedIndexChanged; }
        //        else
        //        { m_cursorKeyCBs[i].SelectedIndexChanged -= cursorKeyCB_SelectedIndexChanged; }
        //    }
        //}
    }
}
