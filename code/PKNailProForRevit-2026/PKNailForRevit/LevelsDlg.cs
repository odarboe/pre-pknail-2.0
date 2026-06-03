using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PKNailForRevit
{
    internal partial class LevelsDlg : DrivableForm
    {
        #region Data Members
        private LevelsData m_data;
        private DocumentSettings m_docSettings;
        private UnitSystem m_unitSystem;
        private undoStack m_undoStack;
        private bool m_isLevelsLocked;  // NOTE: this is a placeholder for Revit dimensioning logic
        private bool m_isSettingState;
        private TextBox m_editTB;
        private IButtonControl m_savedCancelBtn;
        #endregion

        internal LevelsDlg(LevelsData data, DocumentSettings docSettings, UnitSystem unitSystem)
        {
            m_data = data;
            m_docSettings = docSettings;
            m_unitSystem = unitSystem;

            m_isLevelsLocked = false;
            m_isSettingState = false;
            m_undoStack = new undoStack();

            m_editTB = new TextBox();
            m_editTB.KeyDown += new KeyEventHandler(m_editTB_KeyDown);
            m_editTB.Enter += new EventHandler(m_editTB_Enter);
            m_editTB.Leave += new System.EventHandler(m_editTB_Leave);
            m_editTB.Hide();
            Controls.Add(m_editTB);
            m_savedCancelBtn = null;

            InitializeComponent();
            m_OKBtn = CloseBtn;

            Distance.Units displayUnits = (unitSystem == UnitSystem.Imperial) ?
                Distance.Units.Imperial : Distance.Units.MetricMillimeter;
            Distance roundingIncrement = docSettings.LocationRounding;
            Floor2FloorDC.DisplayUnits = displayUnits;
            Floor2FloorDC.RoundingIncrement = roundingIncrement;
            Floor2CeilingDC.DisplayUnits = displayUnits;
            Floor2CeilingDC.RoundingIncrement = roundingIncrement;
            Floor2DropCeilingDC.DisplayUnits = displayUnits;
            Floor2DropCeilingDC.RoundingIncrement = roundingIncrement;
            LevelElevDC.DisplayUnits = displayUnits;
            LevelElevDC.RoundingIncrement = roundingIncrement;

            // This is here because I could not get this property to be browsable/editable
            // in the visual designer
            Floor2FloorDC.IsCompletedIndicatorEnabled = true;
            Floor2CeilingDC.IsCompletedIndicatorEnabled = true;
            Floor2DropCeilingDC.IsCompletedIndicatorEnabled = true;
            LevelElevDC.IsCompletedIndicatorEnabled = true;
        }

        internal LevelsData Data  { get { return m_data; } }

        private void LevelsDlg_Load(object sender, EventArgs e)
        {
            // Populate the levels listbox
            LevelsLB.BeginUpdate();
            foreach (LevelDataItem item in m_data.Levels)
            {
                LevelsLB.Items.Add(item.Name);
            }
            LevelsLB.SelectedIndex = -1;
            LevelsLB.EndUpdate();
            m_editTB.Font = new Font(LevelsLB.Font.FontFamily, 9.5F);

            // Select lowest level initially
            LevelsLB.SelectedIndex = LevelsLB.Items.Count - 1;
        }

        private void setDialogState()
        {
            if (m_isSettingState) { return; }
            m_isSettingState = true;

            //
            // Set ProjectElevation for each level from Floor2Floor values
            //
            m_data.RecalculateProjectElevations();


            // 
            // Set the data state (values) of the controls
            //
            int lvlIdx = LevelsLB.SelectedIndex;
            if (lvlIdx != -1)
            {
                LevelDataItem item = m_data.Levels[lvlIdx];
                this.Text = item.Name + " - Levels Wizard";
                LevelRefCB.Checked = item.IsRefElev;
                LevelElevDC.SetDistanceClean(item.ProjectElev);
                Floor2FloorDC.SetDistanceClean(item.Floor2FloorElev);
                Floor2CeilingDC.SetDistanceClean(item.Floor2CeilingElev);
                Floor2DCNoneCB.Checked = !item.IsDropCeiling;
                Floor2DropCeilingDC.SetDistanceClean(item.Floor2DropCeilingElev);
            }
            else
            {
                this.Text = "Levels Wizard";
                LevelRefCB.Checked = false;
                LevelElevDC.ClearDistance();
                Floor2FloorDC.ClearDistance();
                Floor2DCNoneCB.Checked = false;
                Floor2CeilingDC.ClearDistance();
                Floor2DropCeilingDC.ClearDistance();
            }

            //
            // Set the UI state of the controls
            //
            LevelsLB.Invalidate(true);

            if (m_isLevelsLocked == false)
            {
                UndoBtn.Enabled = (m_undoStack.Count > 0);
                MoveUpBtn.Enabled = (lvlIdx != -1);
                MoveDownBtn.Enabled = (lvlIdx != -1);
                LevelRefCB.Enabled = (lvlIdx != -1 && !LevelRefCB.Checked);

                LevelElevLbl.Enabled = LevelRefCB.Checked;
                LevelElevDC.Enabled = LevelRefCB.Checked;

                Floor2FloorLbl.Enabled = true;
                Floor2FloorDC.Enabled = true;

                Floor2CeilingLbl.Enabled = true;
                Floor2CeilingDC.Enabled = true;

                Floor2DCNoneCB.Enabled = true;
                Floor2DropCeilingLbl.Enabled = !Floor2DCNoneCB.Checked;
                Floor2DropCeilingDC.Enabled = !Floor2DCNoneCB.Checked;
            }
            else
            {
                UndoBtn.Enabled = false;
                MoveUpBtn.Enabled = false;
                MoveDownBtn.Enabled = false;
                LevelRefCB.Enabled = false;

                LevelElevLbl.Enabled = false;
                LevelElevDC.Enabled = false;

                Floor2FloorLbl.Enabled = false;
                Floor2FloorDC.Enabled = false;

                Floor2CeilingLbl.Enabled = false;
                Floor2CeilingDC.Enabled = false;

                Floor2DCNoneCB.Enabled = false;
                Floor2DropCeilingLbl.Enabled = false;
                Floor2DropCeilingDC.Enabled = false;
            }

            m_isSettingState = false;
        }

        private void LevelsLB_SelectedIndexChanged(object sender, EventArgs e)
        {
            setDialogState();
            Floor2FloorDC.Focus();
        }

        private void Floor2FloorDC_Validating(object sender, CancelEventArgs e)
        {
            if (Floor2FloorDC.DistanceValue.IsNoValue
                && LevelsLB.SelectedIndex != 0 && LevelsLB.SelectedIndex != -1)
            {
                F2FEP.SetError(Floor2FloorDC, @"Floor to Floor elevation must be defined for this level");
                e.Cancel = true;
            }
        }

        private void Floor2FloorDC_Validated(object sender, EventArgs e)
        {
            F2FEP.SetError(Floor2FloorDC, @"");

            if (!Floor2FloorDC.IsDirty) return;
            int sel = LevelsLB.SelectedIndex; if (sel == -1) return;

            Distance f2f = Floor2FloorDC.DistanceValue;
            if (!f2f.IsNoValue)
            {
                if (sel == 0)
                {
                    // Currently at the highest level
                    List<string> lvlNames = new List<string>();
                    foreach (LevelDataItem item in m_data.Levels)
                    { lvlNames.Add(item.Name); }
                    AddLvlDlg dlg = new AddLvlDlg(lvlNames, @"You are already at the highest level.  Do you want to add a new level above?");
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        pushUndoState();
                        m_data.Levels[sel].Floor2FloorElev = f2f;
                        m_data.InsertLevel(sel, dlg.NewLvlName);
                        LevelsLB.Items.Insert(sel, m_data.Levels[sel].Name);
                    }
                }
                else
                {
                    pushUndoState();
                    m_data.Levels[sel].Floor2FloorElev = f2f;
                }
            }
            else if (sel == 0)
            {
                pushUndoState();
                m_data.Levels[sel].Floor2FloorElev = f2f;
            }

            setDialogState();
        }

        private void Floor2CeilingDC_Validating(object sender, CancelEventArgs e)
        {
            Distance f2c = Floor2CeilingDC.DistanceValue;
            Distance f2f = Floor2FloorDC.DistanceValue;
            if (!f2c.IsNoValue && !f2f.IsNoValue && (f2c > f2f))
            {
                F2CEP.SetError(Floor2CeilingDC, @"Floor to ceiling height may not exceed floor to floor height");
                e.Cancel = true;
            }
        }

        private void Floor2CeilingDC_Validated(object sender, EventArgs e)
        {
            F2CEP.SetError(Floor2CeilingDC, @"");

            if (!Floor2CeilingDC.IsDirty) return;
            int sel = LevelsLB.SelectedIndex; if (sel == -1) return;

            pushUndoState();
            m_data.Levels[sel].Floor2CeilingElev = Floor2CeilingDC.DistanceValue;
            setDialogState();
        }

        private void Floor2DropCeilingDC_Validating(object sender, CancelEventArgs e)
        {
            Distance f2dc = Floor2DropCeilingDC.DistanceValue;
            Distance f2c = Floor2CeilingDC.DistanceValue;
            Distance f2f = Floor2FloorDC.DistanceValue;
            if (!f2dc.IsNoValue)
            {
                if (!f2c.IsNoValue && (f2dc > f2c))
                {
                    F2DCEP.SetError(Floor2DropCeilingDC, @"Floor to drop ceiling height may not exceed floor to ceiling height");
                    e.Cancel = true;
                }
                else if (!f2f.IsNoValue && (f2dc > f2f))
                {
                    F2DCEP.SetError(Floor2DropCeilingDC, @"Floor to drop ceiling height may not exceed floor to floor height");
                    e.Cancel = true;
                }
            }
        }

        private void Floor2DropCeilingDC_Validated(object sender, EventArgs e)
        {
            F2DCEP.SetError(Floor2DropCeilingDC, @"");

            if (!Floor2DropCeilingDC.IsDirty) return;
            int sel = LevelsLB.SelectedIndex; if (sel == -1) return;

            pushUndoState();
            m_data.Levels[sel].Floor2DropCeilingElev = Floor2DropCeilingDC.DistanceValue;
            setDialogState();
        }

        private void LevelElevDC_Validated(object sender, EventArgs e)
        {
            if (!LevelElevDC.IsDirty) return;
            int sel = LevelsLB.SelectedIndex; if (sel == -1) return;

            pushUndoState();
            m_data.Levels[sel].ProjectElev = LevelElevDC.DistanceValue;
            setDialogState();
        }

        private void Floor2DCNoneCB_CheckedChanged(object sender, EventArgs e)
        {
            int sel = LevelsLB.SelectedIndex; if (sel == -1) return;

            pushUndoState();
            m_data.Levels[sel].IsDropCeiling = !Floor2DCNoneCB.Checked;
            if (Floor2DCNoneCB.Checked)
            { m_data.Levels[sel].Floor2DropCeilingElev = Distance.NoValue; }
            else
            { Floor2DropCeilingDC.Focus(); }
            setDialogState();
        }

        private void LevelRefCB_CheckedChanged(object sender, EventArgs e)
        {
            int sel = LevelsLB.SelectedIndex; if (sel == -1) return;

            pushUndoState();

            LevelDataList levels = m_data.Levels;
            if (LevelRefCB.Checked == true)
            {
                // Find existing reference level and clear the reference flag
                for (int lvlIdx = 0; lvlIdx < levels.Count; lvlIdx++)
                {
                    if (levels[lvlIdx].IsRefElev) { levels[lvlIdx].IsRefElev = false; break; }
                }

                levels[sel].IsRefElev = true;
            }
            else
            { levels[sel].IsRefElev = false; }

            setDialogState();
        }

        private void MoveUpBtn_Click(object sender, EventArgs e)
        {
            int sel = LevelsLB.SelectedIndex; if (sel == -1) { return; }

            bool isUndoPushed = false;
            int newSel;
            if (sel > 0)
            {
                newSel = sel - 1;
                if (CopyCB.Checked == true) { CopyLevel(newSel, newSel + 1, ref isUndoPushed); }
            }
            else
            {
                newSel = sel;

                List<string> lvlNames = new List<string>();
                foreach (LevelDataItem item in m_data.Levels)
                { lvlNames.Add(item.Name); }
                AddLvlDlg dlg = new AddLvlDlg(lvlNames, @"You are already at the highest level.  Do you want to add a new level above?");
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    pushUndoState(); isUndoPushed = true;
                    m_data.InsertLevel(newSel, dlg.NewLvlName);
                    LevelsLB.Items.Insert(newSel, m_data.Levels[newSel].Name);
                    if (CopyCB.Checked == true) { CopyLevel(newSel, newSel + 1, ref isUndoPushed); }
                }
            }

            LevelsLB.SelectedIndex = newSel;
        }

        private void MoveDownBtn_Click(object sender, EventArgs e)
        {
            int sel = LevelsLB.SelectedIndex; if (sel == -1) { return; }

            bool isUndoPushed = false;
            int newSel;
            if (sel < m_data.Levels.Count - 1)
            {
                newSel = sel + 1;
                if (CopyCB.Checked == true)
                { CopyLevel(newSel, newSel - 1, ref isUndoPushed); }
            }
            else
            {
                newSel = sel;

                List<string> lvlNames = new List<string>();
                foreach (LevelDataItem item in m_data.Levels)
                { lvlNames.Add(item.Name); }
                AddLvlDlg dlg = new AddLvlDlg(lvlNames, @"You are already at the lowest level.  Do you want to add a new level below?");
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    pushUndoState(); isUndoPushed = true;
                    newSel = sel + 1;
                    m_data.InsertLevel(newSel, dlg.NewLvlName);
                    LevelsLB.Items.Insert(newSel, m_data.Levels[newSel].Name);
                    if (CopyCB.Checked == true) { CopyLevel(newSel, newSel - 1, ref isUndoPushed); }
                }
            }

            LevelsLB.SelectedIndex = newSel;
        }

        private void CopyLevel(int toLvl, int fromLvl, ref bool isUndoPushed)
        {
            if (!m_data.Levels[toLvl].IsEmpty)
            {
                if (MessageBox.Show("The next level already contains measurements.  Are you sure you want to overwrite these with the previous level's measurements?",
                                    "Confirm Copy", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation)
                    == DialogResult.No)
                { return; }
            }
            if (!isUndoPushed) { pushUndoState(); isUndoPushed = true; }
            m_data.Levels[toLvl].CopyElevations(m_data.Levels[fromLvl]);
        }

        private void LevelDiagramPanel_Paint(object sender, PaintEventArgs e)
        {
            Bitmap bmp = Properties.Resources.LvlDiagram;
            bmp.MakeTransparent(System.Drawing.Color.White);
            e.Graphics.DrawImage(bmp, 0, 0);
        }

        private void MoveUpBtn_Paint(object sender, PaintEventArgs e)
        {
            Bitmap bmp = Properties.Resources.MoveUpBtn;
            bmp.MakeTransparent(System.Drawing.Color.White);
            e.Graphics.DrawImage(bmp, (MoveUpBtn.Width - bmp.Width) / 2, (MoveUpBtn.Height - bmp.Height) / 2);
        }

        private void MoveDownBtn_Paint(object sender, PaintEventArgs e)
        {
            Bitmap bmp = Properties.Resources.MoveDownBtn;
            bmp.MakeTransparent(System.Drawing.Color.White);
            e.Graphics.DrawImage(bmp, (MoveDownBtn.Width - bmp.Width) / 2, (MoveDownBtn.Height - bmp.Height) / 2);
        }

        private void LevelsLB_DoubleClick(object sender, EventArgs e)
        {
            int tbMargin = 0;
            Rectangle itemRect = LevelsLB.GetItemRectangle(LevelsLB.SelectedIndex);
            m_editTB.Location = new Point(itemRect.X + LevelsLB.Location.X - tbMargin, itemRect.Y + LevelsLB.Location.Y - tbMargin);
            m_editTB.Size = new Size(itemRect.Width + (2 * tbMargin), itemRect.Height = (2 * tbMargin));
            m_editTB.Text = LevelsLB.Items[LevelsLB.SelectedIndex].ToString();
            m_editTB.Tag = LevelsLB.SelectedIndex;  // Hold onto this as the selection may change
            m_editTB.Show();
            m_editTB.Focus();
        }

        private void WindowHeightBtn_Click(object sender, EventArgs e)
        {
            int sel = LevelsLB.SelectedIndex; if (sel == -1) { return; }

            TypicalWindowHeightDlg dlg = new TypicalWindowHeightDlg(m_data.Levels[sel].TypicalWindowHeights,
                                                                    m_docSettings, m_unitSystem);
            dlg.DriveAndShowDialog(this, m_mps, m_dnps);

            setDialogState();
        }

        private void m_editTB_KeyDown(object sender, KeyEventArgs e)
        {
           switch (e.KeyCode)
            {
                case Keys.Escape:
                    editTB_cancel();
                    e.Handled = true;
                    break;
                case Keys.Enter:
                    editTB_accept();
                    e.Handled = true;
                    break;
                default:
                    e.Handled = false;
                    break;
            }
        }

        void m_editTB_Enter(object sender, EventArgs e)
        {
            m_savedCancelBtn = this.CancelButton;
            this.CancelButton = null;
        }

        private void m_editTB_Leave(object sender, EventArgs e)
        {
            editTB_accept();
            this.CancelButton = m_savedCancelBtn;
        }

        private void editTB_accept()
        {
            Int32 sel = (Int32)m_editTB.Tag;
            pushUndoState();
            m_data.Levels[sel].Name = m_editTB.Text;
            LevelsLB.Items[sel] = m_editTB.Text;
            m_editTB.Hide();
            setDialogState();
            Floor2FloorDC.Focus();
        }

        private void editTB_cancel()
        { m_editTB.Hide(); }

        private void LevelsLB_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index > m_data.Levels.Count - 1) { return; }

            LevelDataItem item = m_data.Levels[e.Index];
            Brush textBrush;
            System.Drawing.Color backColor;
            bool isItemSelected = ((e.State & DrawItemState.Selected) == DrawItemState.Selected);

            if (item.IsComplete && isItemSelected)
            { textBrush = Brushes.White;  backColor = SystemColors.Highlight; }
            else if (item.IsComplete && !isItemSelected)
            { textBrush = Brushes.Black; backColor = System.Drawing.Color.White; }
            else if (!item.IsComplete && isItemSelected)
            { textBrush = Brushes.White; backColor = System.Drawing.Color.Red; }
            else  // !isComplete && !isItemSelected
            { textBrush = Brushes.Red; backColor = System.Drawing.Color.White; }

            // NOTE: this is the only way I found to override the background color
            if (isItemSelected)
            {
                e = new DrawItemEventArgs(e.Graphics, e.Font, e.Bounds, e.Index,
                                              e.State ^ DrawItemState.Selected,
                                              e.ForeColor, backColor);
            }
            e.DrawBackground();

            e.Graphics.DrawString(LevelsLB.Items[e.Index].ToString(), e.Font, textBrush,
                                  e.Bounds, StringFormat.GenericDefault);
        }

        private void LevelsDlg_Paint(object sender, PaintEventArgs e)
        {
            Pen thePen = new Pen(Color.DarkGray, 1.0F);
            int offsetFromBottom = CloseBtn.Location.Y - (ClientSize.Height - (CloseBtn.Location.Y + CloseBtn.Size.Height));
            int offsetFromSides = ClientSize.Width - 12;
            e.Graphics.DrawLine(thePen, 12, offsetFromBottom - 1, offsetFromSides, offsetFromBottom - 1);
            thePen.Color = Color.White;
            e.Graphics.DrawLine(thePen, 12, offsetFromBottom, offsetFromSides, offsetFromBottom);
        }

        private void UndoBtn_Click(object sender, EventArgs e)
        {
            if (m_undoStack.Count > 0)
            {
                popUndoStack();
                setDialogState();
            }
        }

        private void pushUndoState()
        {
            if (!m_isSettingState) { m_undoStack.Push(new LevelsData(m_data)); }
        }

        private void popUndoStack()
        {
            LevelsData undoData = m_undoStack.Pop();
            if (undoData != null) { m_data = undoData; }
        }
    }

    class undoStack : List<LevelsData>
    {
        private static int sm_undoStackDepth = 10;

        public void Push(LevelsData data)
        {
            if (Count == sm_undoStackDepth) { RemoveAt(Count - 1); }
            Insert(0, data);
        }

        public LevelsData Pop()
        {
            LevelsData retval = null;
            if (Count > 0)
            {
                retval = this[0];
                RemoveAt(0);
            }
            return retval;
        }
    }
}
