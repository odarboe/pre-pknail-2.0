using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Threading;
using System.Timers;
using System.Text.RegularExpressions;

namespace PKNailForRevit
{
    public class Disto : CommLine, IMeasurementProvider, IKeypadCommandProvider, IDialogNavigationProvider
    {
        public enum ConnectionState { Disconnected, Connected, ConnectFailed, ConnectionLost }
        public class ConnectionStateEventArgs : EventArgs
        {
            protected ConnectionState m_state;
            protected string m_portName;
            protected bool m_isReconnectPending;

            internal ConnectionStateEventArgs(ConnectionState state, string portName, bool isReconnectPending)
            {
                m_state = state;
                m_portName = portName;
                m_isReconnectPending = isReconnectPending;
            }
            public ConnectionState State { get { return m_state; } }
            public string PortName { get { return m_portName; } }
            public bool IsReconnectPending { get { return m_isReconnectPending; } }
        }
        public delegate void ConnectionStateEventHandler(object sender, ConnectionStateEventArgs e);
        private event ConnectionStateEventHandler m_onConnectionState;

        public enum ErrorType { None, InvalidData, DeviceError, SerialPortError, Unknown }
        public class Error
        {
            protected ErrorType m_eType;
            protected String m_eMsg;

            internal Error(ErrorType eType, string eMsg)
            {
                m_eType = eType;
                m_eMsg = eMsg;
            }
            public ErrorType ErrorType { get { return m_eType; } }
            public String ErrorMessage { get { return m_eMsg; } }
        }
        public class ErrorEventArgs : EventArgs
        {
            protected Error m_error;
            internal ErrorEventArgs(Error error) { m_error = error; }
            public Error Error { get { return m_error; } }
        }
        public delegate void ErrorEventHandler(object sender, ErrorEventArgs e);
        private event ErrorEventHandler m_onError;

        public class ExceptionEventArgs : EventArgs
        {
            protected Exception m_exception;
            internal ExceptionEventArgs(Exception e) { m_exception = e; }
            public Exception Exception { get { return m_exception; } }
        }
        public delegate void ExceptionEventHandler(object sender, ExceptionEventArgs e);
        private event ExceptionEventHandler m_onException;

        private event MeasurementEventHandler OnMeasurement;
        private Stack<Delegate> m_measurementEventHandlerStack = new Stack<Delegate>();

        private event KeypadCommandEventHandler OnKeypadCommand;
        private event DialogNavigationEventHandler OnDialogNavigation;
        private Stack<Delegate> m_keypadEventHandlerStack = new Stack<Delegate>();

        private distoKeypadContext m_dialogNavigationKeypadContext = new distoKeypadContext(2, 1, 500.0, 1000.0);
        private distoKeypadContext m_commandKeypadContext = new distoKeypadContext(1, 2, 500.0, 1000.0);
        private distoKeypadContext m_currentKeypadContext = null;
        private KeypadCommand m_pendingCommand;
        private KeypadCommandPhrase m_pendingPhrase;
        private System.Timers.Timer m_multiClickTimer;
        private System.Timers.Timer m_phraseTimer;

        private Object m_lockThis;
        private bool m_isProbePorts;
        private String m_lastPortConnected;
        private state m_state;
        private AutoResetEvent m_signal;
        private SynchronizationContext m_syncContext;
        private String m_distoData;

        private enum state { Disconnected, ConnectPending, Connected, DisconnectRequested, ReconnectRequested, SerialPortError }

        private SendOrPostCallback m_onDistoCommandDelegate;
        private SendOrPostCallback m_onDistoConnectionStateDelegate;
        private SendOrPostCallback m_onDistoErrorDelegate;
        private SendOrPostCallback m_onDistoExceptionDelegate;
        private SendOrPostCallback m_onDistoMeasurementDelegate;

        private delegate void WorkerEventHandler(AsyncOperation asyncOp);

        public Disto(ConnectionStateEventHandler stateHandler, ErrorEventHandler errorHandler, ExceptionEventHandler exceptionHandler)
        {
            m_onConnectionState += stateHandler;
            m_onError += errorHandler;
            m_onException += exceptionHandler;

            InitializeDelegates();

            m_multiClickTimer = new System.Timers.Timer();
            m_multiClickTimer.AutoReset = false;
            m_multiClickTimer.Elapsed += new ElapsedEventHandler(m_multiClickTimer_Elapsed);

            m_phraseTimer = new System.Timers.Timer();
            m_phraseTimer.AutoReset = false;
            m_phraseTimer.Elapsed += new ElapsedEventHandler(m_phraseTimer_Elapsed);

            m_lockThis = new Object();
            m_state = state.Disconnected;
            m_signal = new AutoResetEvent(false);
        }

        // IMeasurementProvider implementations

        bool IMeasurementProvider.IsSyncSupported  { get { return false; } }
        bool IMeasurementProvider.IsAsyncSupported  { get { return true; } }

        void IMeasurementProvider.PushContext(MeasurementEventHandler handler)
        {
            if (OnMeasurement != null)
            {
                Delegate[] handlers = OnMeasurement.GetInvocationList();
                if (handlers.Length > 1)
                { throw new HardException("More than one measurement event handler (shouldn't happen)"); }
                if (handlers.Length == 1)
                {
                    OnMeasurement -= (MeasurementEventHandler)handlers[0];
                    m_measurementEventHandlerStack.Push(handlers[0]);
                }
            }

            OnMeasurement += handler;
        }

        void IMeasurementProvider.PopContext()
        {
            Delegate[] handlers = OnMeasurement.GetInvocationList();
            if (handlers.Length > 1)
            { throw new HardException("More than one measurement event handler (shouldn't happen)"); }
            if (handlers.Length == 1)
            { OnMeasurement -= (MeasurementEventHandler)handlers[0]; }

            if (m_measurementEventHandlerStack.Count > 0)
            { OnMeasurement += (MeasurementEventHandler)(m_measurementEventHandlerStack.Pop()); }
        }

        void IMeasurementProvider.GetMeasurement(out MeasurementResult result)
        {
            throw new NotImplementedException();
        }

        // IKeypadCommandProvider interface implementations

        void IKeypadCommandProvider.PushContext(KeypadCommandEventHandler handler)
        {
            pushHandler();
            OnKeypadCommand += handler;
            setKeypadContext();
        }

        void IKeypadCommandProvider.PopContext()
        {
            Delegate[] handlers = OnKeypadCommand.GetInvocationList();
            if (handlers.Length > 1)
            { throw new HardException("More than one keypad event handler for disto (shouldn't happen)"); }
            if (handlers.Length == 1)
            { OnKeypadCommand -= (KeypadCommandEventHandler)handlers[0]; }

            popHandler();
            setKeypadContext();
        }

        void IDialogNavigationProvider.PushContext(DialogNavigationEventHandler handler)
        {
            pushHandler();
            OnDialogNavigation += handler;
            setKeypadContext();
        }

        void IDialogNavigationProvider.PopContext()
        {
            Delegate[] handlers = OnDialogNavigation.GetInvocationList();
            if (handlers.Length > 1)
            { throw new HardException("More than one keypad event handler for disto (shouldn't happen)"); }
            if (handlers.Length == 1)
            { OnDialogNavigation -= (DialogNavigationEventHandler)handlers[0]; }

            popHandler();
            setKeypadContext();
        }

        void IDialogNavigationProvider.AddControlTag(string tag)
        {
            throw new NotImplementedException();
        }

        void IDialogNavigationProvider.AddSubControlTag(string tag)
        {
            throw new NotImplementedException();
        }

        public void Connect(string lastPortConnected, bool isProbePorts, SynchronizationContext syncContext)
        {
            lock (m_lockThis)
            {
                m_syncContext = syncContext;
                m_isProbePorts = isProbePorts;
                m_lastPortConnected = lastPortConnected;

                if (m_state == state.Disconnected)
                {
                    Thread distoWorkerThread = new Thread(new ThreadStart(distoReadWorker));
                    distoWorkerThread.Name = "distoReadWorker";
                    distoWorkerThread.IsBackground = true;
                    distoWorkerThread.Start();
                }
                else if (m_state == state.ConnectPending)
                {
                    // Short-circuit the reconnect waiting period
                    m_signal.Set();
                }
            }
        }

        public void Disconnect()
        {
            lock (m_lockThis)
            {
                if (m_state != state.Disconnected)
                {
                    m_state = state.DisconnectRequested;
                    m_signal.Set();
                }
            }
        }

        private class distoKeypadContext
        {
            internal int MaxMultiClickLength;
            internal int MaxPhraseLength;
            internal double MultiClickDelay;  // milliseconds
            internal double PhraseDelay;  // milliseconds

            internal distoKeypadContext(int maxMultiClickLength, int maxPhraseLength, double multiClickDelay, double phraseDelay)
            {
                MaxMultiClickLength = maxMultiClickLength;
                MaxPhraseLength = maxPhraseLength;
                MultiClickDelay = multiClickDelay;
                PhraseDelay = phraseDelay;
            }
        }

        private void pushHandler()
        {
            if (OnDialogNavigation != null)
            {
                Delegate[] handlers = OnDialogNavigation.GetInvocationList();
                if (handlers.Length > 1)
                { throw new HardException("More than one keypad event handler for disto (shouldn't happen)"); }
                if (handlers.Length == 1)
                {
                    OnDialogNavigation -= (DialogNavigationEventHandler)handlers[0];
                    m_keypadEventHandlerStack.Push(handlers[0]);
                }
            }

            else if (OnKeypadCommand != null)
            {
                Delegate[] handlers = OnKeypadCommand.GetInvocationList();
                if (handlers.Length > 1)
                { throw new HardException("More than one keypad event handler for disto (shouldn't happen)"); }
                if (handlers.Length == 1)
                {
                    OnKeypadCommand -= (KeypadCommandEventHandler)handlers[0];
                    m_keypadEventHandlerStack.Push(handlers[0]);
                }
            }
        }

        private void popHandler()
        {
            if (m_keypadEventHandlerStack.Count > 0)
            {
                Delegate handler = m_keypadEventHandlerStack.Pop();
                if (handler is DialogNavigationEventHandler)
                { OnDialogNavigation += (DialogNavigationEventHandler)handler; }
                else
                { OnKeypadCommand += (KeypadCommandEventHandler)handler; }
            }
        }

        private void setKeypadContext()
        {
            if (OnDialogNavigation != null)
            { m_currentKeypadContext = m_dialogNavigationKeypadContext; }
            else if (OnKeypadCommand != null)
            { m_currentKeypadContext = m_commandKeypadContext; }
            else
            { m_currentKeypadContext = null; }

            if (m_currentKeypadContext != null)
            {
                m_multiClickTimer.Interval = m_currentKeypadContext.MultiClickDelay;
                m_phraseTimer.Interval = m_currentKeypadContext.PhraseDelay;
            }
        }

        private void distoReadWorker()
        {
            bool isConnected;

            while (true)
            {
                try
                {
                    #region Open port
                    //
                    // Attempt to open the serial port
                    //
                    do
                    {
                        isConnected = false;

                        lock (m_lockThis)
                        {
                            isConnected = Open();

                            if (isConnected)
                            {
                                m_state = state.Connected;
                                ConnectionStateEventArgs cEventArgs
                                    = new ConnectionStateEventArgs(ConnectionState.Connected, m_openPortName, false);
                                m_syncContext.Post(this.m_onDistoConnectionStateDelegate, cEventArgs);
                                m_distoData = "";
                                m_pendingCommand = null;
                                m_pendingPhrase = null;
                            }
                            else
                            {
                                m_state = state.ConnectPending;
                            }
                        }

                        if (!isConnected)
                        {
                            m_signal.WaitOne(5000, false);
                            lock (m_lockThis)
                            {
                                if (m_state == state.DisconnectRequested) { break; }
                            }
                        }
                    }
                    while (!isConnected);
                    #endregion

                    #region Get Data
                    // 
                    // Connected; process signals from the serial port callback functions
                    //
                    while (isConnected)
                    {
                        m_signal.WaitOne();
                        lock (m_lockThis)
                        {
                            if (m_state == state.Connected)
                            { processDistoMsg(); }
                            else
                            { break; }
                        }
                    }
                    #endregion

                    #region Disconnect or reconnect
                    //
                    // Done processing signals from Disto callbacks; disconnect or reconnect as indicated by state
                    //
                    lock (m_lockThis)
                    {
                        if (m_state == state.DisconnectRequested)
                        {
                            break;
                        }

                        else if (m_state == state.ReconnectRequested)
                        {
                            ConnectionStateEventArgs e
                                = new ConnectionStateEventArgs(ConnectionState.ConnectionLost, @"", true);
                            m_syncContext.Post(this.m_onDistoConnectionStateDelegate, e);
                            if (Online) { Close(); }
                        }

                        else if (m_state == state.SerialPortError)
                        {
                            ErrorEventArgs errEventArgs
                                = new ErrorEventArgs(new Error(ErrorType.SerialPortError, m_distoData));
                            m_syncContext.Post(this.m_onDistoErrorDelegate, errEventArgs);
                            break;
                        }
                    }
                    #endregion
                }
                catch (Exception e)
                {
                    ExceptionEventArgs exEventArgs = new ExceptionEventArgs(e);
                    m_syncContext.Post(this.m_onDistoExceptionDelegate, exEventArgs);
                    break;
                }
            }

            if (Online) { Close(); }
            ConnectionStateEventArgs csEventArgs
                = new ConnectionStateEventArgs(ConnectionState.Disconnected, @"", false);
            m_syncContext.Post(this.m_onDistoConnectionStateDelegate, csEventArgs);
            m_state = state.Disconnected;
        }

        protected override void OnStatusChange(ModemStatus mask, ModemStatus status)
        {
            if ((mask.cts && !status.cts) || (mask.dsr && !status.dsr))
            {
                lock (m_lockThis)
                {
                    m_state = state.ReconnectRequested;
                    m_signal.Set();
                }
            }
            base.OnStatusChange(mask, status);
        }

        protected override void OnRxLine(string s)
        {
            lock (m_lockThis)
            {
                m_distoData += s;
                m_signal.Set();
            }
            base.OnRxLine(s);
        }

        protected void processDistoMsg()
        {
            distoParser.DistoMessage distoMsg = null;
            while (m_distoData.Length > 0 && distoParser.TryParse(ref m_distoData, out distoMsg))
            {
                switch (distoMsg.MessageType)
                {
                    case distoParser.MessageType.OK:
                        break;

                    case distoParser.MessageType.Keypress:
                        send("cfm");
                        if (m_currentKeypadContext != null)
                        { handleKeypress(distoMsg); }
                        break;

                    case distoParser.MessageType.Distance:
                        send("cfm");
                        MeasurementEventArgs mArgs = new MeasurementEventArgs(distoMsg.Distance);
                        m_syncContext.Post(this.m_onDistoMeasurementDelegate, mArgs);
                        break;

                    case distoParser.MessageType.Error:
                        ErrorEventArgs eArgs = new ErrorEventArgs(distoMsg.Error);
                        m_syncContext.Post(this.m_onDistoErrorDelegate, eArgs);
                        break;
                }
            }
        }

        private void handleKeypress(distoParser.DistoMessage distoMsg)
        {
            bool isWithinMultiClickDelay = m_multiClickTimer.Enabled;
            m_multiClickTimer.Stop();
            m_phraseTimer.Stop();

            if (m_pendingCommand != null)
            {
                if (distoMsg.CursorKey == m_pendingCommand.CursorKey)
                {
                    if (isWithinMultiClickDelay)
                    {
                        m_pendingCommand.NumClicks++;
                    }
                    else
                    {
                        addPendingCommandToPhrase();
                        sendCommandPhrase();
                        m_pendingCommand = new KeypadCommand(distoMsg.CursorKey, 1);
                    }
                }
                else  // distoMsg.CursorKey != m_pendingCommand.CursorKey
                {
                    addPendingCommandToPhrase();
                    m_pendingCommand = new KeypadCommand(distoMsg.CursorKey, 1);
                }
            }
            else
            {
                m_pendingCommand = new KeypadCommand(distoMsg.CursorKey, 1);
            }

            if (m_pendingCommand.NumClicks >= m_currentKeypadContext.MaxMultiClickLength)
            {
                addPendingCommandToPhrase();
                if (m_pendingPhrase.Count >= m_currentKeypadContext.MaxPhraseLength)
                {
                    sendCommandPhrase();
                }
                else
                {
                    m_phraseTimer.Start();
                }
            }
            else
            {
                m_multiClickTimer.Start();
                m_phraseTimer.Start();
            }
        }

        private void m_multiClickTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            addPendingCommandToPhrase();
            if (m_pendingPhrase != null && m_pendingPhrase.Count >= m_currentKeypadContext.MaxPhraseLength)
            {
                m_phraseTimer.Stop();
                sendCommandPhrase();
            }
        }

        private void m_phraseTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            addPendingCommandToPhrase();
            sendCommandPhrase();
        }

        private void addPendingCommandToPhrase()
        {
            if (m_pendingCommand != null)
            {
                if (m_pendingPhrase == null)  { m_pendingPhrase = new KeypadCommandPhrase(); }
                m_pendingPhrase.Add(m_pendingCommand);
            }
            m_pendingCommand = null;
        }

        private void sendCommandPhrase()
        {
            if (m_pendingPhrase != null)
            {
                KeypadCommandEventArgs cArgs = new KeypadCommandEventArgs(m_pendingPhrase);
                m_syncContext.Post(this.m_onDistoCommandDelegate, cArgs);
                m_pendingPhrase = null;
                m_pendingCommand = null;
            }
        }

        private void distoCommand(object state)
        {
            KeypadCommandEventArgs e = state as KeypadCommandEventArgs;
            raiseDistoCommand(e);
        }
        protected virtual void raiseDistoCommand(KeypadCommandEventArgs e)
        {
            if (OnKeypadCommand != null)
            { OnKeypadCommand(this, e); }

            if (OnDialogNavigation != null)
            {
                KeypadCommand cmd = e.CommandPhrase[0];
                DialogNavigationAction action = actionFromCommand(cmd);
                if (action != DialogNavigationAction.Unknown)
                {
                    DialogNavigationEventArgs dne = new DialogNavigationEventArgs(action);
                    OnDialogNavigation(this, dne);
                }
            }
        }

        private DialogNavigationAction actionFromCommand(KeypadCommand cmd)
        {
            DialogNavigationAction retval = DialogNavigationAction.Unknown;

            if (cmd.NumClicks == 1)
            {
                switch (cmd.CursorKey)
                {
                    case KeypadCommand.KeyDef.Down:
                        retval = DialogNavigationAction.TabDown;
                        break;

                    case KeypadCommand.KeyDef.Left:
                        retval = DialogNavigationAction.Deselect;
                        break;

                    case KeypadCommand.KeyDef.Right:
                        retval = DialogNavigationAction.SelectOrToggle;
                        break;

                    case KeypadCommand.KeyDef.Up:
                        retval = DialogNavigationAction.TabUp;
                        break;
                }
            }
            else if (cmd.NumClicks == 2)
            {
                if (cmd.CursorKey == KeypadCommand.KeyDef.Right)
                { retval = DialogNavigationAction.OK; }
                else if (cmd.CursorKey == KeypadCommand.KeyDef.Left)
                { retval = DialogNavigationAction.Cancel; }
            }

            return retval;
        }

        private void distoConnectionState(object state)
        {
            ConnectionStateEventArgs e = state as ConnectionStateEventArgs;
            raiseDistoConnectionState(e);
        }
        protected virtual void raiseDistoConnectionState(ConnectionStateEventArgs e)
        {
            ConnectionStateEventHandler handler = m_onConnectionState;
            if (handler != null)
            { m_onConnectionState(this, e); }
        }

        private void distoError(object state)
        {
            ErrorEventArgs e = state as ErrorEventArgs;
            raiseDistoError(e);
        }
        protected virtual void raiseDistoError(ErrorEventArgs e)
        {
            ErrorEventHandler handler = m_onError;
            if (handler != null)
            { m_onError(this, e); }
        }

        private void distoException(object state)
        {
            ExceptionEventArgs e = state as ExceptionEventArgs;
            raiseDistoException(e);
        }
        protected virtual void raiseDistoException(ExceptionEventArgs e)
        {
            ExceptionEventHandler handler = m_onException;
            if (handler != null)
            { m_onException(this, e); }
        }

        private void distoMeasurement(object state)
        {
            MeasurementEventArgs e = state as MeasurementEventArgs;
            raiseDistoMeasurement(e);
        }
        protected virtual void raiseDistoMeasurement(MeasurementEventArgs e)
        {
            MeasurementEventHandler handler = OnMeasurement;
            if (handler != null)
            { OnMeasurement(this, e); }
        }

        protected virtual void InitializeDelegates()
        {
            m_onDistoCommandDelegate = new SendOrPostCallback(distoCommand);
            m_onDistoConnectionStateDelegate = new SendOrPostCallback(distoConnectionState);
            m_onDistoErrorDelegate = new SendOrPostCallback(distoError);
            m_onDistoExceptionDelegate = new SendOrPostCallback(distoException);
            m_onDistoMeasurementDelegate = new SendOrPostCallback(distoMeasurement);
        }

        internal override CommBaseSettings CommSettings()
        {
            CommLineSettings retval = new CommLineSettings();
            retval.SetStandard(m_lastPortConnected, m_isProbePorts, 9600, Handshake.none);
            retval.rxFilter = new ASCII[2] { ASCII.LF, ASCII.SOH };
            retval.rxTerminator = ASCII.CR;
            retval.txTerminator = new ASCII[2] { ASCII.CR, ASCII.LF };
            Setup(retval);
            return retval;
        }

        protected override bool AfterOpen()
        {
            bool retval = false;

            ModemStatus status = getModemStatus();
            if (status.cts && status.dsr)
            {
                System.Threading.Thread.Sleep(50);  // Makes the port more reliable
                String resp = "";
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        resp = transact("a");
                        if (resp != "" && resp.Substring(0, 1) == "?")
                        { retval = true; break; }
                    }
                    catch (Exception e)
                    {
                        if (e.Message == "Timeout") { resp = ""; }
                        else { throwException(e.Message); }
                    }
                }
            }

            return retval;
        }

        private static class distoParser
        {
            private static Regex sm_termExp = new Regex(@"\r\n", RegexOptions.Compiled);
            private static Regex sm_OKExp = new Regex(@"\?", RegexOptions.Compiled);
            private static Regex sm_errExp = new Regex(@"@E(?<code>\d{3})",
                                                       RegexOptions.Compiled);
            private static Regex sm_distExp = new Regex(@"31\.\.(?<units>\d{2})(?<sign>[\+-])(?<value>\d{8}) ",
                                                        RegexOptions.Compiled);
            private static Regex sm_cmdExp = new Regex(@"5000\.\.\+(?<key>\d{8}) ",
                                                        RegexOptions.Compiled);
            private static Dictionary<int, KeypadCommand.KeyDef> sm_cmdKeyMap = new Dictionary<int, KeypadCommand.KeyDef>()
            {
                { 1, KeypadCommand.KeyDef.Down },
                { 2, KeypadCommand.KeyDef.Up },
                { 3, KeypadCommand.KeyDef.Left },
                { 4, KeypadCommand.KeyDef.UpLeft },
                { 6, KeypadCommand.KeyDef.Right },
                { 8, KeypadCommand.KeyDef.UpRight },
                { 10, KeypadCommand.KeyDef.DownLeft },
                { 15, KeypadCommand.KeyDef.DownRight },
            };

            private static Dictionary<int, String> sm_distoErrorMap = new Dictionary<int, String>()
            {
                { 203, "Wrong command or wrong parameter" },
                { 204, "Dimension error" },
                { 205, "Timeout measure module response" },
                { 220, "Error on serial communication" },
                { 224, "Error communication I2C (HSG sensor)" },
                { 225, "Error communication slave controller (R8C)" },
                { 240, "Bluetooth data transmission not confirmed within expected time" },
                { 241, "Bluetooth confirm [cfm] command received but not expected" },
                { 242, "Bluetooth module error (no communication)" },
                { 251, "Battery voltage too low" },
                { 255, "Signal too low or distance out of range" },
                { 256, "Signal too high" },
                { 257, "Too much background light" },
                { 284, "Laser error" },
                { 287, "EEPROM HMI Checksum error" },
                { 288, "APD can't be adjusted to reference signal" },
                { 290, "Reference signal out of range" },
                { 294, "Display overflow" }
            };

            internal enum MessageType { OK, Error, Distance, Keypress }

            internal class DistoMessage
            {
                internal DistoMessage()
                {
                    m_msgType = MessageType.OK;
                }
                internal DistoMessage(Distance d)
                {
                    m_msgType = MessageType.Distance;
                    m_distance = d;
                }
                internal DistoMessage(KeypadCommand.KeyDef key)
                {
                    m_msgType = MessageType.Keypress;
                    m_key = key;
                }
                internal DistoMessage(Error e)
                {
                    m_msgType = MessageType.Error;
                    m_error = e;
                }
                internal MessageType MessageType { get { return m_msgType; } }
                internal Distance Distance { get { return m_distance; } }
                internal KeypadCommand.KeyDef CursorKey { get { return m_key; } }
                internal Error Error { get { return m_error; } }

                private MessageType m_msgType;
                private Distance m_distance = null;
                private KeypadCommand.KeyDef m_key = KeypadCommand.KeyDef.None;
                private Error m_error = null;
            }

            internal static bool TryParse(ref string data, out DistoMessage result)
            {
                bool retval = false;
                result = null;

                retval = (   tryParseOK(ref data, out result)
                          || tryParseDistance(ref data, out result)
                          || tryParseCommand(ref data, out result)
                          || tryParseError(ref data, out result));

                Match termMatch = sm_termExp.Match(data);
                if (termMatch.Success)
                {
                    if (!retval || termMatch.Index != 0)
                    {
                        result = new DistoMessage(new Error(ErrorType.InvalidData,
                                                  data.Substring(0, termMatch.Index)));
                    }
                    if (data.Length == 2) { data = ""; } else { data = data.Substring(2); }
                }
                return retval;
            }

            private static bool tryParseOK(ref string data, out DistoMessage result)
            {
                bool retval = false;
                result = null;
                Match OKMatch = sm_OKExp.Match(data);
                if (OKMatch.Success)
                {
                    result = new DistoMessage();
                    data = data.Substring(OKMatch.Length);
                }
                return retval;
            }

            private static bool tryParseDistance(ref string data, out DistoMessage result) 
            {
                bool retval = false;
                result = null;
                Match distMatch = sm_distExp.Match(data);
                if (distMatch.Success)
                {
                    int units;
                    if (Int32.TryParse(distMatch.Result("${units}"), out units))
                    {
                        String valueStr = distMatch.Result("${value}");
                        Double signD = (distMatch.Result("${sign}") == "-") ? -1.0 : 1.0;
                        Int32 valueI1; Int32 valueI2; Int32 valueI3;
                        switch (units)
                        {
                            case 0:  // decimal meters, 3 sig. digits
                                if (Int32.TryParse(valueStr, out valueI1))
                                {
                                    result = new DistoMessage(new Distance((((double)valueI1) / 1000.0) * signD, Distance.Units.MetricMeter));
                                    retval = true;
                                }
                                break;
                            case 1:  // decimal ft, 2 sig. digits
                                if (Int32.TryParse(valueStr, out valueI1))
                                {
                                    result = new DistoMessage(new Distance((((double)valueI1) / 100.0) * signD, Distance.Units.Imperial));
                                    retval = true;
                                }
                                break;
                            case 2:  // decimal in, 1 sig. digit
                                if (Int32.TryParse(valueStr, out valueI1))
                                {
                                    result = new DistoMessage(new Distance((((double)valueI1) / 120.0) * signD, Distance.Units.Imperial));
                                    retval = true;
                                }
                                break;
                            case 3:  // integer ft + xx/32 in
                                if (Int32.TryParse(valueStr.Substring(0, 6), out valueI1)
                                    && Int32.TryParse(valueStr.Substring(6, 2), out valueI2))
                                {
                                    result = new DistoMessage(new Distance(((double)valueI1 + ((double)valueI2 / 384.0)) * signD,
                                                          Distance.Units.Imperial));
                                    retval = true;
                                }
                                break;
                            case 4:  // integer mm
                                if (Int32.TryParse(valueStr, out valueI1))
                                {
                                    result = new DistoMessage(new Distance(((double)valueI1) * signD, Distance.Units.MetricMillimeter));
                                    retval = true;
                                }
                                break;
                            case 5:  // decimal meters, 2 sig. digits
                                if (Int32.TryParse(valueStr, out valueI1))
                                {
                                    result = new DistoMessage(new Distance((((double)valueI1) / 100.0) * signD, Distance.Units.MetricMeter));
                                    retval = true;
                                }
                                break;
                            case 6:  // decimal meters, 4 sig. digits
                                if (Int32.TryParse(valueStr, out valueI1))
                                {
                                    result = new DistoMessage(new Distance((((double)valueI1) / 10000.0) * signD, Distance.Units.MetricMeter));
                                    retval = true;
                                }
                                break;
                            case 7:  // decimal yd, 3 sig. digits
                                if (Int32.TryParse(valueStr, out valueI1))
                                {
                                    result = new DistoMessage(new Distance((((double)valueI1) * 0.003) * signD,
                                                          Distance.Units.Imperial));
                                    retval = true;
                                }
                                break;
                            case 8:  // integer ft + integer in + xx/32 in
                                if (Int32.TryParse(valueStr.Substring(0, 4), out valueI1)
                                    && Int32.TryParse(valueStr.Substring(4, 2), out valueI2)
                                    && Int32.TryParse(valueStr.Substring(6, 2), out valueI3))
                                {
                                    result = new DistoMessage(new Distance(((double)valueI1 + ((double)valueI2 / 12.0)
                                                            + ((double)valueI3 / 192.0)) * signD,
                                                          Distance.Units.Imperial));
                                    retval = true;
                                }
                                break;
                            case 9:
                            default:
                                if (Int32.TryParse(valueStr.Substring(0, 4), out valueI1)
                                    && Int32.TryParse(valueStr.Substring(4, 2), out valueI2)
                                    && Int32.TryParse(valueStr.Substring(6, 2), out valueI3))
                                {
                                    result = new DistoMessage(new Distance(((double)valueI1 + ((double)valueI2 / 12.0)
                                                            + ((double)valueI3 / 384.0)) * signD,
                                                          Distance.Units.Imperial));
                                    retval = true;
                                }
                                break;
                        }
                    }

                    if (retval)
                    {
                        data = data.Substring(distMatch.Length);
                    }
                }
                return retval;
            }

            private static bool tryParseCommand(ref string data, out DistoMessage result)
            {
                bool retval = false;
                result = null;

                Match cmdMatch = sm_cmdExp.Match(data);
                if (cmdMatch.Success)
                {
                    Int32 keyCode;
                    KeypadCommand.KeyDef key;
                    if (Int32.TryParse(cmdMatch.Result("${key}"), out keyCode)
                        && sm_cmdKeyMap.TryGetValue(keyCode, out key))
                    {
                        result = new DistoMessage(key);
                        retval = true;
                        data = data.Substring(cmdMatch.Length);
                    }
                }
                return retval;
            }

            private static bool tryParseError(ref string data, out DistoMessage result)
            {
                bool retval = false;
                result = null;
                Match errMatch = sm_errExp.Match(data);
                if (errMatch.Success)
                {
                    String errMsg = "Disto error";
                    Int32 errCode;
                    String msg;
                    if (Int32.TryParse(errMatch.Result("${code}"), out errCode)
                        && sm_distoErrorMap.TryGetValue(errCode, out msg))
                    {
                        errMsg += " (" + errCode.ToString() + "): " + msg;
                    }
                    else { errMsg += ": Unknown error"; }
                    result = new DistoMessage(new Error(ErrorType.DeviceError, errMsg));
                    retval = true;
                    data = data.Substring(errMatch.Length);
                }
                return retval;
            }
        }
    }
}
