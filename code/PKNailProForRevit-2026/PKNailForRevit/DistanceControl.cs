using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing;

namespace PKNailForRevit
{
    internal class DistanceControl : UserControl, IDistanceControl
    {
        #region Data Members
        // contained controls
        private FeetTextBox FeetTB;
        private InchesTextBox InchesTB;
        private MetricTextBox MetricTB;

        private Distance.Units m_displayUnits;
        private Distance m_notRoundedValue = Distance.NoValue;
        private Distance m_roundingIncrement = Distance.NoValue;
        private bool m_isDirty;
        private bool m_isValueChanging;
        private Color m_origBackgroundColor = Color.Empty;
        #endregion

        [Bindable(false)]
        [Browsable(false)]
        public Distance RoundingIncrement
        {
            get { return m_roundingIncrement; }
            set { m_roundingIncrement = value; }
        }

        [Bindable(false)]
        [Browsable(false)]
        public Distance DistanceValue
        { get { return distanceValueFromTB(); } }

        [Bindable(false)]
        [Browsable(false)]
        public Distance DistanceValueNotRounded
        { get { return (m_notRoundedValue.IsNoValue) ? DistanceValue : m_notRoundedValue; } }

        [Bindable(false)]
        [Browsable(false)]
        public bool IsDirty
        { get { return m_isDirty; } }

        [Bindable(false)]
        [Browsable(false)]
        public bool IsDistanceNoValue
        { get { return m_notRoundedValue.IsNoValue; } }

        public void SetDistanceClean(Distance d)
        { setDistanceValue(d, true, false); }

        public void SetDistanceDirty(Distance d)
        { setDistanceValue(d, false, false); }

        public void ClearDistance()
        {
            setDistanceValue(Distance.NoValue, true, false);
            RaiseTextChanged();
        }

        [Bindable(false)]
        [Browsable(false)]
        internal Distance.Units DisplayUnits
        {
            set
            {
                if (value != Distance.Units.Imperial
                    && value != Distance.Units.MetricMeter
                    && value != Distance.Units.MetricMillimeter)
                { throw new ArgumentException(@"Invalid argument in DistanceControl.DisplayUnits setter"); }

                m_displayUnits = value;

                if (m_displayUnits == Distance.Units.Imperial)
                {
                    FeetTB.Visible = true;
                    FeetTB.TabIndex = 0;
                    InchesTB.Visible = true;
                    InchesTB.TabIndex = 1;
                }
                else
                {
                    MetricTB.Visible = true;
                    MetricTB.TabIndex = 0;
                }
            }
        }

        // Synthetic TextChanged event that passes the TextChanged event from the child controls
        new internal event EventHandler TextChanged;

        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        internal bool IsCompletedIndicatorEnabled
        {
            set
            {
                if (m_displayUnits == Distance.Units.Imperial)
                {
                    FeetTB.IsCompletedIndicatorEnabled = value;
                    InchesTB.IsCompletedIndicatorEnabled = value;
                }
                else
                { MetricTB.IsCompletedIndicatorEnabled = value; }
            }
        }

        internal void SetTBBackgroundColor(Color c)
        {
            if (m_origBackgroundColor == Color.Empty)
            { m_origBackgroundColor = (m_displayUnits == Distance.Units.Imperial) ? FeetTB.BackColor : MetricTB.BackColor; }
            if (m_displayUnits == Distance.Units.Imperial)
            {
                FeetTB.BackColor = c;
                InchesTB.BackColor = c;
            }
            else
            { MetricTB.BackColor = c; }
        }

        internal void ClearTBBackgroundColor()
        {
            if (m_origBackgroundColor != Color.Empty)
            {
                if (m_displayUnits == Distance.Units.Imperial)
                {
                    FeetTB.BackColor = m_origBackgroundColor;
                    InchesTB.BackColor = m_origBackgroundColor;
                }
                else
                { MetricTB.BackColor = m_origBackgroundColor; }
            }
        }

        internal DistanceControl()
        {
            // Set to imperial so will load in designer
            // MUST be set to proper value before Load()
            m_displayUnits = Distance.Units.MetricMillimeter;

            m_isValueChanging = false;
            m_isDirty = false;
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            if (m_displayUnits == Distance.Units.Undefined)
            { throw new ApplicationException(@"DistanceControl.Load event fired without displayUnits set"); }

            if (m_displayUnits == Distance.Units.Imperial)
            {
                FeetTB.Visible = true;
                FeetTB.TabStop = true;
                FeetTB.TabIndex = 0;
                InchesTB.Visible = true;
                InchesTB.TabStop = false;
                InchesTB.TabIndex = 1;
            }
            else
            {
                MetricTB.Visible = true;
                MetricTB.TabStop = true;
                MetricTB.TabIndex = 0;
            }

            base.OnLoad(e);
        }

        private void InitializeComponent()
        {
            FeetTB = new FeetTextBox();
            InchesTB = new InchesTextBox();
            MetricTB = new MetricTextBox();

            SuspendLayout();

            // "this" control
            Name = "DistanceControl";
            Size = new Size(183, 31);
            Margin = new Padding(0);
            Validating += new CancelEventHandler(distanceControl_Validating);

            FeetTB.Name = "FeetTB";
            FeetTB.Location = new Point(0, 0);
            FeetTB.Size = new Size(48, 31);
            FeetTB.Text = "133";
            setCommonTBProperties(FeetTB);
            Controls.Add(FeetTB);

            InchesTB.Name = "InchesTB";
            InchesTB.Location = new Point(59, 0);
            InchesTB.Size = new Size(115, 31);
            InchesTB.TabStop = false;
            InchesTB.Text = "00 00/00";
            setCommonTBProperties(InchesTB);
            Controls.Add(InchesTB);

            MetricTB.Name = "MetricTB";
            MetricTB.Location = new Point(0, 0);
            MetricTB.Size = new Size(143, 31);
            MetricTB.Text = "99999.999";
            setCommonTBProperties(MetricTB);
            Controls.Add(MetricTB);

            ResumeLayout(false);
            PerformLayout();
        }

        private void setCommonTBProperties(DistanceTextBox tb)
        {
            tb.BorderStyle = BorderStyle.None;
            tb.Font = new Font("Microsoft Sans Serif", 20F, FontStyle.Bold);
            tb.Margin = new Padding(0);
            tb.TextAlign = HorizontalAlignment.Right;
            tb.Visible = false;
            tb.TextChanged += new EventHandler(tb_TextChanged);
        }

        private void setDistanceValue(Distance val, bool isClean, bool isSuppressTextChangedEvent)
        {
            m_isValueChanging = true;

            m_notRoundedValue = val;
            Distance valueToSet = (!val.IsNoValue) ? val.Round(m_roundingIncrement) : val;

            setIsCompletedIndicator(!val.IsNoValue);

            renderToTB(valueToSet);

            m_isDirty = !isClean;

            m_isValueChanging = false;
            if (!isSuppressTextChangedEvent)
            { RaiseTextChanged(); }
        }

        private void setIsCompletedIndicator(bool val)
        {
            if (m_displayUnits == Distance.Units.Imperial)
            {
                FeetTB.IsCompletedIndicator = val;
                InchesTB.IsCompletedIndicator = val;
            }
            else
            { MetricTB.IsCompletedIndicator = val; }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Brush brush = (this.Enabled) ? Brushes.Black : new SolidBrush(SystemColors.GrayText);
            if (m_displayUnits == Distance.Units.Imperial)
            {
                Pen pen = new Pen(brush, 2.0F);
                e.Graphics.DrawLine(pen, 50, 2, 50, 11);
                e.Graphics.DrawLine(pen, 176, 2, 176, 11);
                e.Graphics.DrawLine(pen, 179, 2, 179, 11);
            }
            else
            {
                String unitsDesignator = (m_displayUnits == Distance.Units.MetricMillimeter) ? @"mm" : @"m";
                Font f = new Font(FontFamily.GenericSansSerif, 14F);
                e.Graphics.DrawString(unitsDesignator, f, brush, new PointF(145, 7));
            }

            base.OnPaint(e);
        }

        private Distance distanceValueFromTB()
        {
            if (m_displayUnits == Distance.Units.Imperial)
            { return Distance.ParseImperial(FeetTB.Text, InchesTB.Text); }
            else if (m_displayUnits == Distance.Units.MetricMeter)
            { return Distance.ParseMetricMeters(MetricTB.Text); }
            else if (m_displayUnits == Distance.Units.MetricMillimeter)
            { return Distance.ParseMetricMillimeters(MetricTB.Text); }
            else { return Distance.NoValue; }
        }

        private void renderToTB(Distance valueToSet)
        {
            if (m_displayUnits == Distance.Units.Imperial)
            {
                string feet; string inches;
                valueToSet.RenderImperial(out feet, out inches);
                FeetTB.Text = feet; InchesTB.Text = inches;
            }
            else if (m_displayUnits == Distance.Units.MetricMeter)
            { MetricTB.Text = valueToSet.RenderMetricMeters(); }
            else if (m_displayUnits == Distance.Units.MetricMillimeter)
            { MetricTB.Text = valueToSet.RenderMetricMillimeters(); }
        }

        private void distanceControl_Validating(object sender, CancelEventArgs e)
        {
            if (m_isDirty)
            {
                Distance curValue = distanceValueFromTB();
                if (m_notRoundedValue.IsNoValue) { m_notRoundedValue = curValue; }

                Distance roundedValue = curValue.Round(m_roundingIncrement);
                if (roundedValue != curValue)
                {
                    m_isValueChanging = true;
                    renderToTB(roundedValue);
                    m_isValueChanging = false;
                }
            }
        }

        private void tb_TextChanged(object sender, EventArgs e)
        {
            if (!m_isValueChanging)
            {
                m_notRoundedValue = Distance.NoValue;
                m_isDirty = true;
                RaiseTextChanged();
            }
        }

        protected virtual void RaiseTextChanged()
        {
            EventHandler handler = TextChanged;
            if (handler != null)
            { handler(this, new EventArgs()); }
        }
    }
}