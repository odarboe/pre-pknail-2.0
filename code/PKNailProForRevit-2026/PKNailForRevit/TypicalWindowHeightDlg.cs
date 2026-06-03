using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PKNailForRevit
{
    internal partial class TypicalWindowHeightDlg : DrivableForm
    {
        private List<WindowHeights> m_windowHeights;
        private bool m_isHandlingHeightChange;

        internal TypicalWindowHeightDlg(List <WindowHeights> windowHeights,
                                        DocumentSettings docSettings, UnitSystem unitSystem)
        {
            m_windowHeights = windowHeights;
            m_isHandlingHeightChange = false;
            
            InitializeComponent();
            m_OKBtn = CloseBtn;

            Distance.Units displayUnits = (unitSystem == UnitSystem.Imperial) ?
                Distance.Units.Imperial : Distance.Units.MetricMillimeter;
            Distance roundingIncrement = docSettings.FeatureDimensionRounding;
            HeadHeightDC.DisplayUnits = displayUnits;
            HeadHeightDC.TextChanged += new EventHandler(HeadHeightDC_TextChanged);
            HeadHeightDC.RoundingIncrement = roundingIncrement;
            WindowHeightDC.DisplayUnits = displayUnits;
            WindowHeightDC.TextChanged += new EventHandler(WindowHeightDC_TextChanged);
            WindowHeightDC.RoundingIncrement = roundingIncrement;
            SillHeightDC.DisplayUnits = displayUnits;
            SillHeightDC.TextChanged += new EventHandler(SillHeightDC_TextChanged);
            SillHeightDC.RoundingIncrement = roundingIncrement;
        }

        private void TypicalWindowHeightDlg_Load(object sender, EventArgs e)
        {
            m_isHandlingHeightChange = true;  // Prevent re-calculation of heights during initial population

            if (m_windowHeights.Count == 1)
            {
                WindowHeightDC.SetDistanceClean(m_windowHeights[0].Height);
                SillHeightDC.SetDistanceClean(m_windowHeights[0].SillHeight);
                HeadHeightDC.SetDistanceClean(m_windowHeights[0].HeadHeight);

                StackedHeightsChkBox.Checked = false;
            }
            else
            {
                populateStackedHeightsLB();

                WindowHeightDC.ClearDistance();
                SillHeightDC.ClearDistance();
                HeadHeightDC.ClearDistance();

                StackedHeightsChkBox.Checked = (m_windowHeights.Count > 1);
            }

            WindowHeightRB.Checked = true;

            setState();

            m_isHandlingHeightChange = false;
        }

        private void StackedHeightsChkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!StackedHeightsChkBox.Checked)
            {
                if (m_windowHeights.Count > 1)
                { m_windowHeights.RemoveRange(1, m_windowHeights.Count - 1); }
                StackedHeightsLB.Items.Clear();
                if (m_windowHeights.Count == 1)
                {
                    HeadHeightDC.SetDistanceClean(m_windowHeights[0].SillHeight + m_windowHeights[0].Height);
                    WindowHeightDC.SetDistanceClean(m_windowHeights[0].Height);
                    SillHeightDC.SetDistanceClean(m_windowHeights[0].SillHeight);
                }
            }
            else  // StackedHeightsChkBox.Checked
            {
                populateStackedHeightsLB();
            }

            setState();
        }

        private void AddStackedBtn_Click(object sender, EventArgs e)
        {
            bool isMatchingEntry = false;
            Distance sillHgt = SillHeightDC.DistanceValue;
            Distance windowHgt = WindowHeightDC.DistanceValue;
            foreach (WindowHeights wh in m_windowHeights)
            {
                if (wh.SillHeight == sillHgt && wh.Height == windowHgt)
                { isMatchingEntry = true; break; }
            }
            if (isMatchingEntry)  { return; }

            WindowHeights twh = new WindowHeights(WindowHeightDC.DistanceValue, SillHeightDC.DistanceValue);
            int insertIdx = -1;
            for (int i = 0; i < m_windowHeights.Count; i++)
            {
                if (m_windowHeights[i].HeadHeight > twh.HeadHeight)
                { insertIdx = i; break; }
            }
            if (insertIdx != -1)
            { m_windowHeights.Insert(insertIdx, twh); }
            else
            { m_windowHeights.Add(twh); }

            populateStackedHeightsLB();
            setState();
        }

        private void RemoveStackedBtn_Click(object sender, EventArgs e)
        {
            if (StackedHeightsLB.SelectedIndex != -1)
            {
                int sel = StackedHeightsLB.Items.Count - StackedHeightsLB.SelectedIndex - 1;
                m_windowHeights.RemoveAt(sel);
                StackedHeightsLB.Items.RemoveAt(StackedHeightsLB.SelectedIndex);
            }

            setState();
        }

        private void SillHeightDC_TextChanged(object sender, EventArgs e)
        { handleHeightChanged(); }

        private void SillHeightDC_Validated(object sender, EventArgs e)
        { handleHeightChanged(); }

        private void WindowHeightDC_TextChanged(object sender, EventArgs e)
        { handleHeightChanged(); }

        private void WindowHeightDC_Validated(object sender, EventArgs e)
        { handleHeightChanged(); }

        private void HeadHeightDC_TextChanged(object sender, EventArgs e)
        { handleHeightChanged(); }

        private void HeadHeightDC_Validated(object sender, EventArgs e)
        { handleHeightChanged(); }

        private void handleHeightChanged()
        {
            if (m_isHandlingHeightChange) { return; }
            m_isHandlingHeightChange = true;

            Distance SillHeight = SillHeightDC.DistanceValue;
            Distance WindowHeight = WindowHeightDC.DistanceValue;
            Distance HeadHeight = HeadHeightDC.DistanceValue;

            if (HeadHeightRB.Checked)
            {
                if (HeadHeight != Distance.NoValue && SillHeight != Distance.NoValue)
                { WindowHeight = HeadHeight - SillHeight; }
                else
                { WindowHeight = Distance.NoValue; }
                WindowHeightDC.SetDistanceClean(WindowHeight);
            }
            else   // WindowHeightRB.Checked
            {
                if (WindowHeight != Distance.NoValue && SillHeight != Distance.NoValue)
                { HeadHeight = WindowHeight + SillHeight; }
                else
                { HeadHeight = Distance.NoValue; }
                HeadHeightDC.SetDistanceClean(HeadHeight);
            }

            if (!StackedHeightsChkBox.Checked)
            {
                if (SillHeight != Distance.NoValue && WindowHeight != Distance.NoValue)
                {
                    if (m_windowHeights.Count == 0)
                    { m_windowHeights.Add(new WindowHeights(WindowHeight, SillHeight)); }
                    else
                    { m_windowHeights[0].Height = WindowHeight; m_windowHeights[0].SillHeight = SillHeight; }
                }
                else if (m_windowHeights.Count > 0)
                { m_windowHeights.Clear(); }
            }

            setState();

            m_isHandlingHeightChange = false;
        }

        private void populateStackedHeightsLB()
        {
            StackedHeightsLB.BeginUpdate();
            StackedHeightsLB.Items.Clear();

            // Populate the listbox in reverse order, so they visually "stack", lowest to highest
            for (int i = m_windowHeights.Count - 1; i >= 0; i--)
            {
                string lbItem = renderWindowHeights(m_windowHeights[i]);
                StackedHeightsLB.Items.Add(lbItem);
            }
            StackedHeightsLB.SelectedIndex = -1;
            StackedHeightsLB.EndUpdate();
        }

        private void setState()
        {
            bool isStackedWindows = StackedHeightsChkBox.Checked;
            StackedHeightsLB.Enabled = isStackedWindows;

            bool isAddStackedBtnEnabled = false;
            Distance sillHgt = SillHeightDC.DistanceValue;
            Distance windowHgt = WindowHeightDC.DistanceValue;
            if (isStackedWindows)
            {
                if (m_windowHeights.Count > 0)
                {
                    bool isMatchingEntry = false;
                    foreach (WindowHeights wh in m_windowHeights)
                    {
                        if (wh.SillHeight == sillHgt && wh.Height == windowHgt)
                        { isMatchingEntry = true; break; }
                    }
                    isAddStackedBtnEnabled = !isMatchingEntry
                                             && sillHgt != Distance.NoValue && windowHgt != Distance.NoValue;
                }
                else  // m_windowHeights.Count == 0
                {
                    if (sillHgt != Distance.NoValue && windowHgt != Distance.NoValue)
                    { isAddStackedBtnEnabled = true; }
                }
            }
            AddStackedBtn.Enabled = isAddStackedBtnEnabled;
            RemoveStackedBtn.Enabled = isStackedWindows && StackedHeightsLB.SelectedIndex != -1;
        }

        private void TypicalWindowHeightDlg_Paint(object sender, PaintEventArgs e)
        {
            Pen thePen = new Pen(Color.DarkGray, 1.0F);
            int offsetFromBottom = CloseBtn.Location.Y - (ClientSize.Height - (CloseBtn.Location.Y + CloseBtn.Size.Height));
            int offsetFromSides = ClientSize.Width - 12;
            e.Graphics.DrawLine(thePen, 12, offsetFromBottom - 1, offsetFromSides, offsetFromBottom - 1);
            thePen.Color = Color.White;
            e.Graphics.DrawLine(thePen, 12, offsetFromBottom, offsetFromSides, offsetFromBottom);
        }

        private void WindowHeightDiagramPnl_Paint(object sender, PaintEventArgs e)
        {
            Bitmap bmp = Properties.Resources.TypicalWindowHeightDiagram;
            bmp.MakeTransparent(System.Drawing.Color.White);
            e.Graphics.DrawImage(bmp, 0, 0);
        }

        private static string renderWindowHeights(WindowHeights twh)
        {
            string feet; string inches;
            twh.SillHeight.RenderImperial(out feet, out inches);
            string retval = "";
            if (feet.Length > 0) { retval += feet + @"' "; }
            if (inches.Length > 0) { retval += inches + @""" "; }
            retval += @"(SillHgt) | ";
            twh.Height.RenderImperial(out feet, out inches);
            if (feet.Length > 0) { retval += feet + @"' "; }
            if (inches.Length > 0) { retval += inches + @""" "; }
            retval += @"(Hgt)";
            return retval;
        }

        private void StackedHeightsLB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (StackedHeightsLB.SelectedIndex != -1)
            {
                WindowHeights wh = m_windowHeights[(m_windowHeights.Count - 1) - StackedHeightsLB.SelectedIndex];
                SillHeightDC.SetDistanceClean(wh.SillHeight);
                WindowHeightDC.SetDistanceClean(wh.Height);
                HeadHeightDC.SetDistanceClean(wh.SillHeight + wh.Height);
            }
            else
            {
                WindowHeightDC.SetDistanceClean(Distance.NoValue);
                SillHeightDC.SetDistanceClean(Distance.NoValue);
                HeadHeightDC.SetDistanceClean(Distance.NoValue);
            }

            setState();
        }

        private void StackedHeightsLB_EnabledChanged(object sender, EventArgs e)
        {
            StackedHeightsLB.BackColor = (StackedHeightsLB.Enabled) ? SystemColors.Window
                                                                    : StackedHeightsLB.Parent.BackColor;
        }

        private void WindowHeightRB_CheckedChanged(object sender, EventArgs e)
        {
            WindowHeightDC.Enabled = WindowHeightRB.Checked;
            HeadHeightDC.Enabled = !WindowHeightRB.Checked;
        }
    }
}
