using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

using NUnit.Framework;
using NUnit.Framework.Constraints;

using PKNailForRevit;

namespace PKNailForRevit.UnitTests
{
    [TestFixture]
    public class DistoTest
    {
        private static Disto sm_disto = null;

        [Test]
        [NUnit.Framework.Category(@"NotFullyAutomated")]
        public void CtorsTest()
        {
            sm_disto = new Disto(d_OnDistoConnectionState, d_OnDistoError, d_OnDistoException);
            Assert.IsTrue(true);
        }

        [Test]
        [NUnit.Framework.Category(@"NotFullyAutomated")]
        public void GetMeasurementTest()
        {
            if (sm_disto == null)
            { sm_disto = new Disto(d_OnDistoConnectionState, d_OnDistoError, d_OnDistoException); }

            ((IKeypadCommandProvider)sm_disto).PushContext(d_OnKeypadCommand);
            ((IMeasurementProvider)sm_disto).PushContext(d_OnMeasurement);

            try
            {
                Form theForm = new Form();
                theForm.FormClosing += new FormClosingEventHandler(theForm_FormClosing);
                theForm.Size = new System.Drawing.Size(100, 150);

                Button btn = new Button();
                btn.Size = new System.Drawing.Size(70, 33);
                btn.Text = @"Connect";
                btn.Location = new System.Drawing.Point(15, 15);
                btn.Click += new EventHandler(connectBtn_Click);
                theForm.Controls.Add(btn);

                btn = new Button();
                btn.Size = new System.Drawing.Size(70, 33);
                btn.Text = @"Disconnect";
                btn.Location = new System.Drawing.Point(15, 70);
                btn.Click += new EventHandler(disconnectBtn_Click);
                theForm.Controls.Add(btn);
                
                theForm.ShowDialog();

                Console.WriteLine(System.DateTime.Now.ToLongTimeString() + @"Bye");
            }
            catch (Exception e)
            {
                Console.Out.WriteLine(@"Exception in GetMeasurementTest(): " + e.Message);
            }

            ((IKeypadCommandProvider)sm_disto).PopContext();
            ((IMeasurementProvider)sm_disto).PopContext();

            Assert.IsTrue(true);
        }

        void disconnectBtn_Click(object sender, EventArgs e)
        {
            Console.Out.WriteLine(System.DateTime.Now.ToLongTimeString() + @": Disconnecting...");
            sm_disto.Disconnect();
        }

        void connectBtn_Click(object sender, EventArgs e)
        {
            Console.Out.WriteLine(System.DateTime.Now.ToLongTimeString() + @": Connecting...");
            sm_disto.Connect(@"", true, System.Threading.SynchronizationContext.Current);
        }

        void theForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            sm_disto.Disconnect();
        }

        void d_OnKeypadCommand(object sender, KeypadCommandEventArgs e)
        {
            String msg = System.DateTime.Now.ToLongTimeString();
            msg += @": Command event: ";

            foreach (KeypadCommand cmd in e.CommandPhrase)
            {
                msg += @"{ ";
                switch (cmd.CursorKey)
                {
                    case KeypadCommand.KeyDef.Down:
                        msg += @"CursorDown";
                        break;
                    case KeypadCommand.KeyDef.Left:
                        msg += @"CursorLeft";
                        break;
                    case KeypadCommand.KeyDef.Right:
                        msg += @"CursorRight";
                        break;
                    case KeypadCommand.KeyDef.Up:
                        msg += @"CursorUp";
                        break;
                    case KeypadCommand.KeyDef.UpLeft:
                        msg += @"CursorUpLeft";
                        break;
                    case KeypadCommand.KeyDef.UpRight:
                        msg += @"CursorUpRight";
                        break;
                    case KeypadCommand.KeyDef.DownLeft:
                        msg += @"CursorDownLeft";
                        break;
                    case KeypadCommand.KeyDef.DownRight:
                        msg += @"CursorDownRight";
                        break;
                    case KeypadCommand.KeyDef.None:
                        msg += @"None";
                        break;
                }
                msg += @", numClicks=" + cmd.NumClicks.ToString() + @" }  ";
            }
            Console.Out.WriteLine(msg);
        }

        void d_OnDistoConnectionState(object sender, Disto.ConnectionStateEventArgs e)
        {
            String msg = System.DateTime.Now.ToLongTimeString();
            msg += @": Connection state event: state=";
            switch (e.State)
            {
                case Disto.ConnectionState.Connected:
                    msg += @"connected";
                    break;
                case Disto.ConnectionState.ConnectFailed:
                    msg += @"connectFailed";
                    break;
                case Disto.ConnectionState.ConnectionLost:
                    msg += @"connectionLost";
                    break;
                case Disto.ConnectionState.Disconnected:
                    msg += "disconnected";
                    break;
            }
            msg += @"; reconnect=" + e.IsReconnectPending.ToString();
            Console.Out.WriteLine(msg);
        }

        void d_OnDistoError(object sender, Disto.ErrorEventArgs e)
        {
            String msg = System.DateTime.Now.ToLongTimeString() + @": Error event: ";
            switch (e.Error.ErrorType)
            {
                case Disto.ErrorType.None:
                    msg += @"None";
                    break;
                case Disto.ErrorType.InvalidData:
                    msg += @"InvalidData";
                    break;
                case Disto.ErrorType.DeviceError:
                    msg += @"Rangefinder Error";
                    break;
                case Disto.ErrorType.Unknown:
                    msg += "Unknown";
                    break;
            }
            msg += @"; msg=" + e.Error.ErrorMessage;
            Console.Out.WriteLine(msg);
        }

        void d_OnDistoException(object sender, Disto.ExceptionEventArgs e)
        {
            Console.Out.WriteLine(System.DateTime.Now.ToLongTimeString() + @": Exception event: " + e.Exception.Message + "\n");
        }

        void d_OnMeasurement(object sender, MeasurementEventArgs e)
        {
            Console.Out.WriteLine(System.DateTime.Now.ToLongTimeString() + @": Measurement event: "
                                  + e.Result.Measurement.ValueImperial.ToString() + "\n");
        }
    }
}
