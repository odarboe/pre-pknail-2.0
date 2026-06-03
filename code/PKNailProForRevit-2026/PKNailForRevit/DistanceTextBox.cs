using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace PKNailForRevit
{
    internal class DistanceTextBox : TextBox
    {
        #region Data Members
        private bool m_isCompletedIndicator;
        private bool m_isCompletedIndicatorEnabled;
        #endregion

        [Browsable(false)]
        public bool IsCompletedIndicator
        {
            set
            {
                m_isCompletedIndicator = value;
                setBackgroundColor();
            }
        }

        [Browsable(false)]
        public bool IsCompletedIndicatorEnabled
        {
            set
            {
                m_isCompletedIndicatorEnabled = value;
                setBackgroundColor();
            }
        }

        protected override void OnCreateControl()
        {
            m_isCompletedIndicator = false;
            base.OnCreateControl();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            setBackgroundColor();
            base.OnEnabledChanged(e);
        }

        private void setBackgroundColor()
        {
            if (Enabled)
            {
                if (m_isCompletedIndicatorEnabled && !m_isCompletedIndicator)
                { BackColor = Color.Pink; }
                else
                { BackColor = SystemColors.Window; }
            }
        }
    }

    internal class FeetTextBox : DistanceTextBox
    {
        protected override void OnCreateControl()
        {
            if (Parent != null)
            { Name = Parent.Name + ":FeetTB"; }
            base.OnCreateControl();
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (e.KeyChar == '\'')
            {
                this.Parent.SelectNextControl(this, true, false, true, true);
                e.Handled = true;
            }
            else if (e.KeyChar == '\"')
            {
                string inchesTBName = this.Parent.Name + @":InchesTB";
                Control[] inchesCtl = this.Parent.Controls.Find(inchesTBName, true);
                inchesCtl[0].Text = this.Text;
                this.Text = "";
                this.TopLevelControl.SelectNextControl(this, true, true, true, true);
                e.Handled = true;
            }
            else if (   !char.IsDigit(e.KeyChar)
                     && e.KeyChar != ' '
                     && e.KeyChar != '.'
                     && e.KeyChar != '-'
                     && e.KeyChar != '/'
                     && e.KeyChar != '\b'      // backspace
                     && e.KeyChar != '\x7F')   // DEL
            {
                System.Media.SystemSounds.Beep.Play();
                e.Handled = true;
            }
            else { base.OnKeyPress(e); }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Right)
            {
                Tracer.WriteInfo("e.KeyCode: " + e.KeyCode.ToString() + "; selStart: " + this.SelectionStart.ToString() + "; selLen: " + this.SelectionLength.ToString());
                if ( this.SelectionStart == this.Text.Length && this.SelectionLength == 0)
                {
                    this.Parent.SelectNextControl(this, true, false, true, true);
                    e.Handled = true;
                }
            }
            else { base.OnKeyDown(e); }
        }
    }

    internal class InchesTextBox : DistanceTextBox
    {
        protected override void OnCreateControl()
        {
            if (Parent != null)
            { Name = Parent.Name + ":InchesTB"; }
            TabStop = false;
            base.OnCreateControl();
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (e.KeyChar == '\"')
            {
                this.TopLevelControl.SelectNextControl(this, true, true, true, true);
                e.Handled = true;
            }
            else if (   !char.IsDigit(e.KeyChar)
                     && e.KeyChar != ' '
                     && e.KeyChar != '.'
                     && e.KeyChar != '/'
                     && e.KeyChar != '-'
                     && e.KeyChar != '\b'
                     && e.KeyChar != '\x7F')
            {
                System.Media.SystemSounds.Beep.Play();
                e.Handled = true;
            }
            else
            { base.OnKeyPress(e); }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left
                && this.SelectionStart == 0
                && this.SelectionLength == 0)
            {
                this.Parent.SelectNextControl(this, false, false, true, true);
                e.Handled = true;
            }
            else { base.OnKeyDown(e); }
        }
    }

    internal class MetricTextBox : DistanceTextBox
    {
        protected override void OnCreateControl()
        {
            if (Parent != null)
            { Name = Parent.Name + ":MetricTB"; }
            TabStop = false;
            base.OnCreateControl();
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (!char.IsDigit(e.KeyChar)
                && e.KeyChar != ' '
                && e.KeyChar != '.'
                && e.KeyChar != '-'
                && e.KeyChar != '\b'
                && e.KeyChar != '\x7F')
            {
                System.Media.SystemSounds.Beep.Play();
                e.Handled = true;
            }
            else
            { base.OnKeyPress(e); }
        }
    }
}
