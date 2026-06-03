using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using NUnit.Framework;

namespace PKNailForRevit.UnitTests
{
    [TestFixture]
    public class DrivableFormTest
    {
        private IRangefinder m_rf;
        private DrivableFormTestForm m_testForm = null;

        //[Test]
        //public void TestWithoutRangefinder()
        //{
        //    try
        //    {
        //        m_testForm = new DrivableFormTestForm(true);

        //        Thread worker = new Thread(new ParameterizedThreadStart(doWork));
        //        worker.IsBackground = true;
        //        worker.Start(SynchronizationContext.Current);
        //        m_testForm.ShowDialog();
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.Out.WriteLine(System.DateTime.Now.ToLongTimeString() + ": TestWithoutRangefinder() threw exception: " + ex.Message);
        //        throw ex;
        //    }
        //    finally
        //    {
        //        if (m_testForm != null) { m_testForm.Dispose(); }
        //    }
        //}

        //private void doWork(object syncObj)
        //{
        //    SynchronizationContext sync = syncObj as SynchronizationContext;

        //    SendOrPostCallback measurementCB = new SendOrPostCallback(injectMeasurement);
        //    SendOrPostCallback navCmdCB = new SendOrPostCallback(injectRelativeNavigation);

        //    // Each element of the array has: feet, inches, numerator, denominator
        //    Double[,] injectDist =
        //    {
        //      {0.0, 1.0, 1.0, 2.0},
        //      {1.0, 2.0, 3.0, 4.0},
        //      {2.0, 2.0, 3.0, 8.0},
        //      {3.0, 3.0, 15.0, 16.0},
        //      {4.0, 4.0, 31.0, 32.0}
        //    };

        //    for (int i = 0; i <= injectDist.GetUpperBound(0); i++)
        //    {
        //        Thread.Sleep(5000);
        //        Distance d = new Distance(injectDist[i, 0] + (injectDist[i, 1] + (injectDist[i, 2] / injectDist[i, 3])) / 12.0,
        //                                  Distance.Units.Imperial);
        //        sync.Post(measurementCB, d);
        //    }
        //}

        //private void injectMeasurement(Object distObj)
        //{
        //    Distance d = distObj as Distance;
        //    m_testForm.HandleMeasurementEvent(this, new DrivableForm.MeasurementEventArgs(d));
        //}

        //private void injectRelativeNavigation(Object cmdObj)
        //{
        //    DrivableForm.NavigationRelative nav = (DrivableForm.NavigationRelative)cmdObj;
        //    m_testForm.NavigationRelativeEventHandler(this, new DrivableForm.NavigationRelativeEventArgs(nav));
        //}

        [Test]
        [NUnit.Framework.Category(@"NotFullyAutomated")]
        public void TestWithRangefinder()
        {
            m_rf = TheRangefinder.Instance.Interface;
            m_rf.OnRangefinderConnectionState += new RangefinderConnectionStateEventHandler(rf_OnRangefinderConnectionState);
            m_rf.OnRangefinderError += new RangefinderErrorEventHandler(rf_OnRangefinderError);
            m_rf.OnRangefinderException += new RangefinderExceptionEventHandler(rf_OnRangefinderException);

            try
            {
                m_testForm = new DrivableFormTestForm();
                m_rf.Connect(@"", true, SynchronizationContext.Current);
                DialogResult result = m_testForm.DriveAndShowDialog(m_rf);
                Console.Out.WriteLine(System.DateTime.Now.ToLongTimeString() + @": TestWithRangefinder(): DialogResult=" + result.ToString());
                m_rf.Disconnect();
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(System.DateTime.Now.ToLongTimeString() + @": TestWithRangefinder() threw exception: " + ex.Message);
            }
            finally
            {
                if (m_testForm != null) { m_testForm.Dispose(); }
            }
        }

        void rf_OnRangefinderException(object sender, RangefinderExceptionEventArgs e)
        {
            Console.Out.WriteLine(System.DateTime.Now.ToLongTimeString() + @": Exception event: " + e.Exception.Message + "\n");
        }

        void rf_OnRangefinderError(object sender, RangefinderErrorEventArgs e)
        {
            String msg = System.DateTime.Now.ToLongTimeString() + @": Error event: ";
            switch (e.Error.ErrorType)
            {
                case RangefinderErrorType.None:
                    msg += @"None";
                    break;
                case RangefinderErrorType.InvalidData:
                    msg += @"InvalidData";
                    break;
                case RangefinderErrorType.DeviceError:
                    msg += @"Rangefinder Error";
                    break;
                case RangefinderErrorType.Unknown:
                    msg += @"Unknown";
                    break;
            }
            msg += @"; msg=" + e.Error.ErrorMessage;
            Console.Out.WriteLine(msg);
        }

        void rf_OnRangefinderConnectionState(object sender, RangefinderConnectionStateEventArgs e)
        {
            String msg = System.DateTime.Now.ToLongTimeString();
            msg += @": Connection state event: state=";
            switch (e.State)
            {
                case RangefinderConnectionState.Connected:
                    msg += @"connected";
                    break;
                case RangefinderConnectionState.ConnectFailed:
                    msg += @"connectFailed";
                    break;
                case RangefinderConnectionState.ConnectionLost:
                    msg += "connectionLost";
                    break;
                case RangefinderConnectionState.Disconnected:
                    msg += @"disconnected";
                    break;
            }
            msg += @"; reconnect=" + e.IsReconnectPending.ToString();
            Console.Out.WriteLine(msg);

            if (e.State == RangefinderConnectionState.ConnectionLost)
            {
                Thread.Sleep(30000);
                m_rf.Connect(@"", true, SynchronizationContext.Current);
            }
        }
    }
}
