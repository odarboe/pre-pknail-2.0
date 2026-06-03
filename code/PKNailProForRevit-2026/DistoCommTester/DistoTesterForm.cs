using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

using PKNailForRevit;

namespace DistoCommTester
{
    public partial class DistoTesterForm : Form
    {
        public class TextBoxStreamWriter : TextWriter
        {
            DistoTesterForm m_form = null;

            public TextBoxStreamWriter(DistoTesterForm form)  { m_form = form; }

            public override void Write(char value)
            {
                base.Write(value);
                m_form.appendOutput(value.ToString());
            }

            public override Encoding Encoding
            {
                get { return System.Text.Encoding.UTF8; }
            }
        }

        private string m_outputText = @"";
        TextWriter m_outputWriter = null;

        public DistoTesterForm()
        {
            InitializeComponent();

            IRangefinder d = TheRangefinder.Instance.Interface;
            d.OnRangefinderCommand += new RangefinderCommandEventHandler(d_onRangefinderCommand);
            d.OnRangefinderConnectionState += new RangefinderConnectionStateEventHandler(d_onRangefinderConnectionState);
            d.OnRangefinderException += new RangefinderExceptionEventHandler(d_onRangefinderException);
            d.OnRangefinderMeasurement += new RangefinderMeasurementEventHandler(d_onRangefinderMeasurement);

            m_outputWriter = new TextBoxStreamWriter(this);
            TextWriterTraceListener listener = new TextWriterTraceListener(m_outputWriter);
            Trace.Listeners.Add(listener);
        }

        private void DistoTesterForm_Load(object sender, EventArgs e)
        {
            ProbeAllChkBx.Checked = true;
        }

        delegate void SetTextCallback(string text);

        private void appendOutput(string text)
        {
            if (OutputTB.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(appendOutput);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                lock (m_outputText)
                {
                    m_outputText += text;
                    OutputTB.Text = m_outputText;
                }
            }
        }

        private void ConnectBtn_Click(object sender, EventArgs e)
        {
            IRangefinder rf = TheRangefinder.Instance.Interface;
            rf.Connect(ComPortTB.Text, ProbeAllChkBx.Checked, System.Threading.SynchronizationContext.Current);
        }

        private void DisconnectBtn_Click(object sender, EventArgs e)
        {
            TheRangefinder.Instance.Interface.Disconnect();
            System.Threading.Thread.Sleep(3000);
            OutputTB.Text = m_outputText;
        }

        private void DistoTesterForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            TheRangefinder.Instance.Interface.Disconnect();
        }

        private void d_onRangefinderCommand(object sender, RangefinderCommandEventArgs e)
        {
            String msg = System.DateTime.Now.ToLongTimeString();
            msg += ": Command event:\r\n";

            foreach (RangefinderCommand cmd in e.CommandPhrase)
            {
                msg += @"{ ";
                switch (cmd.CursorKey)
                {
                    case RangefinderCommand.CursorKeyDef.Down:
                        msg += @"CursorDown";
                        break;
                    case RangefinderCommand.CursorKeyDef.Left:
                        msg += @"CursorLeft";
                        break;
                    case RangefinderCommand.CursorKeyDef.Right:
                        msg += @"CursorRight";
                        break;
                    case RangefinderCommand.CursorKeyDef.Up:
                        msg += @"CursorUp";
                        break;
                    case RangefinderCommand.CursorKeyDef.UpLeft:
                        msg += @"CursorUpLeft";
                        break;
                    case RangefinderCommand.CursorKeyDef.UpRight:
                        msg += @"CursorUpRight";
                        break;
                    case RangefinderCommand.CursorKeyDef.DownLeft:
                        msg += @"CursorDownLeft";
                        break;
                    case RangefinderCommand.CursorKeyDef.DownRight:
                        msg += @"CursorDownRight";
                        break;
                    case RangefinderCommand.CursorKeyDef.None:
                        msg += "None";
                        break;
                }
                msg += @"; numClicks=" + cmd.NumClicks.ToString() + @" }  ";
            }
            appendOutput(msg + "\r\n");
        }

        private void d_onRangefinderConnectionState(object sender, RangefinderConnectionStateEventArgs e)
        {
            String msg = System.DateTime.Now.ToLongTimeString();
            msg += ": Connection state event:\r\n" + "state=";
            switch (e.State)
            {
                case RangefinderConnectionState.Connected:
                    msg += "connected; opened port =" + e.OpenPortName;
                    ComPortTB.Text = e.OpenPortName;
                    break;
                case RangefinderConnectionState.ConnectFailed:
                    msg += "connectFailed";
                    break;
                case RangefinderConnectionState.ConnectionLost:
                    msg += "connectionLost";
                    break;
                case RangefinderConnectionState.Disconnected:
                    msg += "disconnected";
                    break;
            }
            msg += "; reconnect=" + e.IsReconnectPending.ToString();
            appendOutput(msg + "\r\n");
        }

        private void d_onRangefinderError(object sender, RangefinderErrorEventArgs e)
        {
            String msg = System.DateTime.Now.ToLongTimeString() + ": Error event:\r\n";
            switch (e.Error.ErrorType)
            {
                case RangefinderErrorType.None:
                    msg += "None";
                    break;
                case RangefinderErrorType.InvalidData:
                    msg += "InvalidData";
                    break;
                case RangefinderErrorType.DeviceError:
                    msg += "Rangefinder Error";
                    break;
                case RangefinderErrorType.Unknown:
                    msg += "Unknown";
                    break;
            }
            msg += "; msg=" + e.Error.ErrorMessage;
            appendOutput(msg + "\r\n");
        }

        private void d_onRangefinderException(object sender, RangefinderExceptionEventArgs e)
        {
            string msg = System.DateTime.Now.ToLongTimeString();
            msg += ": Exception event: " + e.Exception.Message + "\r\n";
            if (e.Exception.InnerException != null)
            {
                msg += "            Inner Exception: " + e.Exception.InnerException.Message + "\r\n";
                if (e.Exception.InnerException.InnerException != null)
                {
                    msg += "            Inner Exception (2): " + e.Exception.InnerException.InnerException.Message + "\r\n";
                }
            }
            appendOutput(msg);
        }

        private void d_onRangefinderMeasurement(object sender, RangefinderMeasurementEventArgs e)
        {
            appendOutput(System.DateTime.Now.ToLongTimeString() + ": Measurement event:\r\n" + e.Measurement.ValueImperial.ToString() + "\r\n");
        }
    }
}
