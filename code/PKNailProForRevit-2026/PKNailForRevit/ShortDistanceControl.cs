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
    internal class ShortDistanceContol : UserControl
    {
        #region Data Members
        // contained controls
        private InchesTextBox InchesTB;
        private MetricTextBox MetricTB;
        private Distance.Units m_displayUnits;
        #endregion

        internal ShortDistanceContol()
        {
            m_displayUnits = Distance.Units.Imperial;  // Defaulting so something is visible in designer
            InitializeComponent();
        }

        internal Distance.Units DisplayUnits
        {
            set
            {
                if (value != Distance.Units.Imperial && value != Distance.Units.MetricMillimeter)
                { throw new ArgumentException(@"Invalid argument in ShortDistanceControl.DisplayUnits setter"); }

                m_displayUnits = value;
                if (m_displayUnits == Distance.Units.Imperial)
                {
                    MetricTB.Visible = false;
                    InchesTB.Visible = true;
                    InchesTB.TabIndex = 0;
                }
                else
                {
                    InchesTB.Visible = false;
                    MetricTB.Visible = true;
                    MetricTB.TabIndex = 0;
                }
            }
        }

        internal Distance GetValue()
        {
            if (m_displayUnits == Distance.Units.Imperial)
            { return Distance.ParseImperial(@"", InchesTB.Text); }
            else
            { return Distance.ParseMetricMillimeters(MetricTB.Text); }
        }

        internal void SetValue(Distance d)
        {
            if (m_displayUnits == Distance.Units.Imperial)
            { InchesTB.Text = d.RenderImperialAsInches(); }
            else
            { MetricTB.Text = d.RenderMetricMillimeters(); }
        }

        protected override void OnLoad(EventArgs e)
        {
            if (m_displayUnits == Distance.Units.Undefined)
            { throw new ApplicationException(@"ShortDistanceControl.Load event fired without displayUnits set"); }

            if (m_displayUnits == Distance.Units.Imperial)
            {
                InchesTB.Visible = true;
                InchesTB.TabIndex = 0;
            }
            else
            {
                MetricTB.Visible = true;
                MetricTB.TabIndex = 0;
            }

            base.OnLoad(e);
        }

        private void InitializeComponent()
        {
            InchesTB = new InchesTextBox();
            MetricTB = new MetricTextBox();

            SuspendLayout();

            // "this" control
            Size = new Size(80, 22);
            Margin = new Padding(0);

            InchesTB.Name = "InchesTB";
            InchesTB.Text = "00 00/00";
            setCommonTBProperties(InchesTB);
            Controls.Add(InchesTB);

            MetricTB.Name = "MetricTB";
            MetricTB.Text = "999.9";
            setCommonTBProperties(MetricTB);
            Controls.Add(MetricTB);

            ResumeLayout(false);
            PerformLayout();
        }

        private void setCommonTBProperties(DistanceTextBox tb)
        {
            tb.BorderStyle = BorderStyle.Fixed3D;
            tb.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            tb.Location = new Point(0, 0);
            tb.Size = new Size(60, 22);
            tb.Margin = new Padding(0);
            tb.TextAlign = HorizontalAlignment.Right;
            tb.Visible = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Brush brush = (this.Enabled) ? Brushes.Black : new SolidBrush(SystemColors.GrayText);
            Pen pen = new Pen(brush, 1.0F);
            if (m_displayUnits == Distance.Units.Imperial)
            {
                e.Graphics.DrawLine(pen, 63, 3, 63, 10);
                e.Graphics.DrawLine(pen, 66, 3, 66, 10);
            }
            else
            {
                Font f = new Font(FontFamily.GenericSansSerif, 8.25F);
                e.Graphics.DrawString(@"mm", f, brush, new PointF(59.5F, 5.5F));
            }
            base.OnPaint(e);
        }
    }
}
