using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.IO;

namespace PKNailForRevit
{
    /// <summary>
    /// Lowest level Com driver handling all Win32 API calls and processing send and receive in terms of
    /// individual bytes. Used as a base class for higher level drivers.
    /// </summary>
    public abstract class CommBase : IDisposable
    {
        protected string m_openPortName;
        private IntPtr m_hPort;
        private IntPtr m_ptrUWO = IntPtr.Zero;
        private Thread m_rxThread = null;
        private bool m_online = false;
        private bool m_auto = false;
        private bool m_checkSends = true;
        private Exception m_rxException = null;
        private bool m_rxExceptionReported = false;
        private int m_writeCount = 0;
        private ManualResetEvent m_writeEvent = new ManualResetEvent(false);
        private ManualResetEvent m_startEvent = new ManualResetEvent(false);
        private int m_stateRTS = 2;
        private int m_stateDTR = 2;
        private int m_stateBRK = 2;
        private bool[] m_empty = new bool[1];
        private bool m_dataQueued = false;

        /// <summary>
        /// Parity settings
        /// </summary>
        internal enum Parity
        {
            /// <summary>
            /// Characters do not have a parity bit.
            /// </summary>
            none = 0,
            /// <summary>
            /// If there are an odd number of 1s in the data bits, the parity bit is 1.
            /// </summary>
            odd = 1,
            /// <summary>
            /// If there are an even number of 1s in the data bits, the parity bit is 1.
            /// </summary>
            even = 2,
            /// <summary>
            /// The parity bit is always 1.
            /// </summary>
            mark = 3,
            /// <summary>
            /// The parity bit is always 0.
            /// </summary>
            space = 4
        };

        /// <summary>
        /// Stop bit settings
        /// </summary>
        internal enum StopBits
        {
            /// <summary>
            /// Line is asserted for 1 bit duration at end of each character
            /// </summary>
            one = 0,
            /// <summary>
            /// Line is asserted for 1.5 bit duration at end of each character
            /// </summary>
            onePointFive = 1,
            /// <summary>
            /// Line is asserted for 2 bit duration at end of each character
            /// </summary>
            two = 2
        };

        /// <summary>
        /// Uses for RTS or DTR pins
        /// </summary>
        internal enum HSOutput
        {
            /// <summary>
            /// Pin is asserted when this station is able to receive data.
            /// </summary>
            handshake = 2,
            /// <summary>
            /// Pin is asserted when this station is transmitting data (RTS on NT, 2000 or XP only).
            /// </summary>
            gate = 3,
            /// <summary>
            /// Pin is asserted when this station is online (port is open).
            /// </summary>
            online = 1,
            /// <summary>
            /// Pin is never asserted.
            /// </summary>
            none = 0
        };

        /// <summary>
        /// Standard handshake methods
        /// </summary>
        internal enum Handshake
        {
            /// <summary>
            /// No handshaking
            /// </summary>
            none,
            /// <summary>
            /// Software handshaking using Xon / Xoff
            /// </summary>
            XonXoff,
            /// <summary>
            /// Hardware handshaking using CTS / RTS
            /// </summary>
            CtsRts,
            /// <summary>
            /// Hardware handshaking using DSR / DTR
            /// </summary>
            DsrDtr
        }

        /// <summary>
        /// Set the public fields to supply settings to CommBase.
        /// </summary>
        internal class CommBaseSettings
        {
            /// <summary>
            /// Port Name (default: "COM1:")
            /// </summary>
            internal string LastPortConnected = @"";
            /// <summary>
            /// Switch to set whether all COM ports are probed (default: true)
            /// </summary>
            internal bool IsProbePorts = true;
            /// <summary>
            /// Baud Rate (default: 2400) unsupported rates will throw "Bad settings"
            /// </summary>
            internal int BaudRate = 2400;
            /// <summary>
            /// The parity checking scheme (default: none)
            /// </summary>
            internal Parity Parity = Parity.none;
            /// <summary>
            /// Number of databits 1..8 (default: 8) unsupported values will throw "Bad settings"
            /// </summary>
            internal int DataBits = 8;
            /// <summary>
            /// Number of stop bits (default: one)
            /// </summary>
            internal StopBits StopBits = StopBits.one;
            /// <summary>
            /// If true, transmission is halted unless CTS is asserted by the remote station (default: false)
            /// </summary>
            internal bool TxFlowCTS = false;
            /// <summary>
            /// If true, transmission is halted unless DSR is asserted by the remote station (default: false)
            /// </summary>
            internal bool TxFlowDSR = false;
            /// <summary>
            /// If true, transmission is halted when Xoff is received and restarted when Xon is received (default: false)
            /// </summary>
            internal bool TxFlowX = false;
            /// <summary>
            /// If false, transmission is suspended when this station has sent Xoff to the remote station (default: true)
            /// Set false if the remote station treats any character as an Xon.
            /// </summary>
            internal bool TxWhenRxXoff = true;
            /// <summary>
            /// If true, received characters are ignored unless DSR is asserted by the remote station (default: false)
            /// </summary>
            internal bool RxGateDSR = false;
            /// <summary>
            /// If true, Xon and Xoff characters are sent to control the data flow from the remote station (default: false)
            /// </summary>
            internal bool RxFlowX = false;
            /// <summary>
            /// Specifies the use to which the RTS output is put (default: none)
            /// </summary>
            internal HSOutput UseRTS = HSOutput.none;
            /// <summary>
            /// Specidies the use to which the DTR output is put (default: none)
            /// </summary>
            internal HSOutput UseDTR = HSOutput.none;
            /// <summary>
            /// The character used to signal Xon for X flow control (default: DC1)
            /// </summary>
            internal ASCII XonChar = ASCII.DC1;
            /// <summary>
            /// The character used to signal Xoff for X flow control (default: DC3)
            /// </summary>
            internal ASCII XoffChar = ASCII.DC3;
            /// <summary>
            /// The number of free bytes in the reception queue at which flow is disabled
            /// (Default: 0 = Set to 1/10th of actual rxQueue size)
            /// </summary>
            internal int RxHighWater = 0;
            /// <summary>
            /// The number of bytes in the reception queue at which flow is re-enabled
            /// (Default: 0 = Set to 1/10th of actual rxQueue size)
            /// </summary>
            internal int RxLowWater = 0;
            /// <summary>
            /// Multiplier. Max time for Send in ms = (Multiplier * Characters) + Constant
            /// (default: 0 = No timeout)
            /// </summary>
            internal uint SendTimeoutMultiplier = 0;
            /// <summary>
            /// Constant.  Max time for Send in ms = (Multiplier * Characters) + Constant (default: 0)
            /// </summary>
            internal uint SendTimeoutConstant = 0;
            /// <summary>
            /// Requested size for receive queue (default: 0 = use operating system default)
            /// </summary>
            internal int RxQueue = 0;
            /// <summary>
            /// Requested size for transmit queue (default: 0 = use operating system default)
            /// </summary>
            internal int TxQueue = 0;
            /// <summary>
            /// If true, the port will automatically re-open on next send if it was previously closed due
            /// to an error (default: false)
            /// </summary>
            internal bool IsAutoReopen = false;

            /// <summary>
            /// If true, subsequent Send commands wait for completion of earlier ones enabling the results
            /// to be checked. If false, errors, including timeouts, may not be detected, but performance
            /// may be better.
            /// </summary>
            internal bool IsCheckAllSends = true;

            /// <summary>
            /// Pre-configures settings for most modern devices: 8 databits, 1 stop bit, no parity and
            /// one of the common handshake protocols. Change individual settings later if necessary.
            /// </summary>
            /// <param name="Port">The port to use (i.e. "COM1:")</param>
            /// <param name="Baud">The baud rate</param>
            /// <param name="Hs">The handshake protocol</param>
            internal void SetStandard(string lastPortConnected, bool isProbePorts, int br, Handshake Hs)
            {
                LastPortConnected = lastPortConnected;
                IsProbePorts = isProbePorts;
                BaudRate = br;
                DataBits = 8; StopBits = StopBits.one; Parity = Parity.none;
                switch (Hs)
                {
                    case Handshake.none:
                        TxFlowCTS = false; TxFlowDSR = false; TxFlowX = false;
                        RxFlowX = false; UseRTS = HSOutput.online; UseDTR = HSOutput.online;
                        TxWhenRxXoff = true; RxGateDSR = false;
                        break;
                    case Handshake.XonXoff:
                        TxFlowCTS = false; TxFlowDSR = false; TxFlowX = true;
                        RxFlowX = true; UseRTS = HSOutput.online; UseDTR = HSOutput.online;
                        TxWhenRxXoff = true; RxGateDSR = false;
                        XonChar = ASCII.DC1; XoffChar = ASCII.DC3;
                        break;
                    case Handshake.CtsRts:
                        TxFlowCTS = true; TxFlowDSR = false; TxFlowX = false;
                        RxFlowX = false; UseRTS = HSOutput.handshake; UseDTR = HSOutput.online;
                        TxWhenRxXoff = true; RxGateDSR = false;
                        break;
                    case Handshake.DsrDtr:
                        TxFlowCTS = false; TxFlowDSR = true; TxFlowX = false;
                        RxFlowX = false; UseRTS = HSOutput.online; UseDTR = HSOutput.handshake;
                        TxWhenRxXoff = true; RxGateDSR = false;
                        break;
                }
            }
        }

        /// <summary>
        /// Byte type with enumeration constants for ASCII control codes.
        /// </summary>
        internal enum ASCII : byte
        {
            NULL = 0x00, SOH = 0x01, STX = 0x02, ETX = 0x03, EOT = 0x04, ENQ = 0x05, ACK = 0x06, BELL = 0x07,
            BS = 0x08, HT = 0x09, LF = 0x0A, VT = 0x0B, FF = 0x0C, CR = 0x0D, SO = 0x0E, SI = 0x0F, DC1 = 0x11,
            DC2 = 0x12, DC3 = 0x13, DC4 = 0x14, NAK = 0x15, SYN = 0x16, ETB = 0x17, CAN = 0x18, EM = 0x19,
            SUB = 0x1A, ESC = 0x1B, FS = 0x1C, GS = 0x1D, RS = 0x1E, US = 0x1F, SP = 0x20, DEL = 0x7F
        }

        /// <summary>
        /// Returns the alternative name of a com port i.e. \\.\COM1 for COM1:
        /// Some systems require this form for double or more digit com port numbers.
        /// </summary>
        /// <param name="s">Name in form COM1 or COM1:</param>
        /// <returns>Name in form \\.\COM1</returns>
        private static string AltName(string s)
        {
            string r = s.Trim();
            if (s.EndsWith(":")) s = s.Substring(0, s.Length - 1);
            if (s.StartsWith(@"\")) return s;
            return @"\\.\" + s;
        }

        /// <summary>
        /// Availability status of a port
        /// </summary>
        internal enum PortStatus
        {
            /// <summary>
            /// Port exists but is unavailable (may be open to another program)
            /// </summary>
            unavailable = 0,
            /// <summary>
            /// Available for use
            /// </summary>
            available = 1,
            /// <summary>
            /// Port does not exist
            /// </summary>
            absent = -1
        }

        /// <summary>
        /// Tests the availability of a named comm port.
        /// </summary>
        /// <param name="s">Name of port</param>
        /// <returns>Availability of port</returns>
        internal PortStatus IsPortAvailable(string s)
        {
            IntPtr h;

            h = Win32Com.CreateFile(s, Win32Com.GENERIC_READ | Win32Com.GENERIC_WRITE, 0, IntPtr.Zero,
                Win32Com.OPEN_EXISTING, Win32Com.FILE_FLAG_OVERLAPPED, IntPtr.Zero);
            if (h == (IntPtr)Win32Com.INVALID_HANDLE_VALUE)
            {
                if (Marshal.GetLastWin32Error() == Win32Com.ERROR_ACCESS_DENIED)
                {
                    return PortStatus.unavailable;
                }
                else
                {
                    h = Win32Com.CreateFile(AltName(s), Win32Com.GENERIC_READ | Win32Com.GENERIC_WRITE, 0, IntPtr.Zero,
                        Win32Com.OPEN_EXISTING, Win32Com.FILE_FLAG_OVERLAPPED, IntPtr.Zero);
                    if (h == (IntPtr)Win32Com.INVALID_HANDLE_VALUE)
                    {
                        if (Marshal.GetLastWin32Error() == Win32Com.ERROR_ACCESS_DENIED)
                        {
                            return PortStatus.unavailable;
                        }
                        else
                        {
                            return PortStatus.absent;
                        }
                    }
                }
            }
            Win32Com.CloseHandle(h);
            return PortStatus.available;
        }

        /// <summary>
        /// Opens the com port and configures it with the required settings
        /// </summary>
        /// <returns>false if the port could not be opened</returns>
        internal bool Open()
        {
            if (m_online) return false;

            CommBaseSettings settings = CommSettings();

            string portToOpen = settings.LastPortConnected;
            openPort(portToOpen, settings);
            if (!m_online)
            {
                if (settings.IsProbePorts)
                {
                    List<string> lowNumberPorts = new List<string>();
                    foreach (string port in System.IO.Ports.SerialPort.GetPortNames())
                    {
                        if (int.Parse(port.Substring(3)) < 5)
                        {
                            // Don't try to open low-numbered ports unless correct port not found in higher numbers
                            lowNumberPorts.Add(port);
                            continue;
                        }
                        portToOpen = port;
                        openPort(portToOpen, settings);
                        if (m_online)  { break; }
                    }
                    if (!m_online && lowNumberPorts.Count > 0)
                    {
                        foreach (string port in lowNumberPorts)
                        {
                            portToOpen = port;
                            openPort(portToOpen, settings);
                            if (m_online) { break; }
                        }
                    }
                }
            }

            if (m_online) { m_openPortName = portToOpen; }

            return m_online;
        }

        private void openPort(string portName, CommBaseSettings settings)
        {
            m_online = false;

            if (portName == null || portName == @"")  { return; }

            Tracer.WriteInfo(@"attempting to open port " + portName);

            m_hPort = Win32Com.CreateFile(portName, Win32Com.GENERIC_READ | Win32Com.GENERIC_WRITE, 0, IntPtr.Zero,
                Win32Com.OPEN_EXISTING, Win32Com.FILE_FLAG_OVERLAPPED, IntPtr.Zero);
            if (m_hPort == (IntPtr)Win32Com.INVALID_HANDLE_VALUE)
            {
                int err = Marshal.GetLastWin32Error();
                if (err != Win32Com.ERROR_ACCESS_DENIED)
                {
                    m_hPort = Win32Com.CreateFile(AltName(portName), Win32Com.GENERIC_READ | Win32Com.GENERIC_WRITE, 0, IntPtr.Zero,
                        Win32Com.OPEN_EXISTING, Win32Com.FILE_FLAG_OVERLAPPED, IntPtr.Zero);
                    if (m_hPort == (IntPtr)Win32Com.INVALID_HANDLE_VALUE)
                    {
                        err = Marshal.GetLastWin32Error();
                        if (err != Win32Com.ERROR_ACCESS_DENIED)
                        { Tracer.WriteInfo(@"open port failed, err=" + err); }
                        return;
                    }
                }
                else
                {
                    Tracer.WriteInfo(@"open port failed, err=" + err);
                    return;
                }
            }

            m_online = true;
            Tracer.WriteInfo(@"port opened");

            Win32Com.COMMTIMEOUTS CommTimeouts = new Win32Com.COMMTIMEOUTS();
            CommTimeouts.ReadIntervalTimeout = Win32Com.MAXDWORD;
            CommTimeouts.ReadTotalTimeoutConstant = 0;
            CommTimeouts.ReadTotalTimeoutMultiplier = 0;

            if (settings.SendTimeoutMultiplier == 0)
            {
                if (System.Environment.OSVersion.Platform == System.PlatformID.Win32NT)
                { CommTimeouts.WriteTotalTimeoutMultiplier = 0; }
                else
                { CommTimeouts.WriteTotalTimeoutMultiplier = 10000; }
            }
            else
            { CommTimeouts.WriteTotalTimeoutMultiplier = settings.SendTimeoutMultiplier; }
            CommTimeouts.WriteTotalTimeoutConstant = settings.SendTimeoutConstant;

            Win32Com.DCB dcb = new Win32Com.DCB();
            if (!Win32Com.GetCommState(m_hPort, ref dcb))
            {
                int err = Marshal.GetLastWin32Error();
                throwException("Error in call to Win32Com.GetCommState(): err=" + err.ToString());
            }
            dcb.BaudRate = settings.BaudRate;
            dcb.Parity = (byte)settings.Parity;
            dcb.ByteSize = (byte)settings.DataBits;
            dcb.StopBits = (byte)settings.StopBits;

            if (!Win32Com.SetCommState(m_hPort, ref dcb))
            {
                int err = Marshal.GetLastWin32Error();
                throwException("Error in call to Win32Com.SetCommState(): err=" + err.ToString());
            }
            if (!Win32Com.SetCommTimeouts(m_hPort, ref CommTimeouts))
            {
                int err = Marshal.GetLastWin32Error();
                throwException("Error in call to Win32Com.SetCommTimeouts(): err=" + err.ToString());
            }

            m_stateBRK = 0;
            if (settings.UseDTR == HSOutput.none) m_stateDTR = 0;
            if (settings.UseDTR == HSOutput.online) m_stateDTR = 1;
            if (settings.UseRTS == HSOutput.none) m_stateRTS = 0;
            if (settings.UseRTS == HSOutput.online) m_stateRTS = 1;

            Win32Com.OVERLAPPED wo = new Win32Com.OVERLAPPED();
            m_checkSends = settings.IsCheckAllSends;
            wo.Offset = 0;
            wo.OffsetHigh = 0;
#pragma warning disable 0618
            if (m_checkSends)
                wo.hEvent = m_writeEvent.Handle;
            else
                wo.hEvent = IntPtr.Zero;
#pragma warning restore 0618

            m_ptrUWO = Marshal.AllocHGlobal(Marshal.SizeOf(wo));

            Marshal.StructureToPtr(wo, m_ptrUWO, true);
            m_writeCount = 0;
            m_empty[0] = true;
            m_dataQueued = false;

            m_rxException = null;
            m_rxExceptionReported = false;
            m_rxThread = new Thread(new ThreadStart(this.receiveThread));
            m_rxThread.Name = "CommBaseRx";
            m_rxThread.Priority = ThreadPriority.AboveNormal;
            m_rxThread.IsBackground = true;
            m_rxThread.Start();

            m_startEvent.WaitOne(500, false);

            m_auto = false;
            if (AfterOpen())
            {
                Tracer.WriteInfo(@"AfterOpen() returned true, port is good");
                m_auto = settings.IsAutoReopen;
            }
            else
            {
                Tracer.WriteInfo(@"AfterOpen() returned false, closing port");
                Close();
            }
        }

        /// <summary>
        /// Closes the com port.
        /// </summary>
        internal void Close()
        {
            if (m_online)
            {
                m_auto = false;
                BeforeClose(false);
                InternalClose();
                m_rxException = null;
            }
        }

        private void InternalClose()
        {
            Win32Com.CancelIo(m_hPort);
            if (m_rxThread != null)
            {
                // Thread.Abort() is unsupported on .NET 8. CancelIo() above (and CloseHandle below) unblock
                // the receive thread's pending read so it exits on its own; just wait briefly for it.
                try { m_rxThread.Join(500); } catch { }
                m_rxThread = null;
            }
            Win32Com.CloseHandle(m_hPort);
            if (m_ptrUWO != IntPtr.Zero) Marshal.FreeHGlobal(m_ptrUWO);
            m_stateRTS = 2;
            m_stateDTR = 2;
            m_stateBRK = 2;
            m_online = false;
        }

        /// <summary>
        /// For IDisposable
        /// </summary>
        public void Dispose() { Close(); }

        /// <summary>
        /// Destructor (just in case)
        /// </summary>
        ~CommBase() { Close(); }

        /// <summary>
        /// True if online.
        /// </summary>
        internal bool Online { get { if (!m_online) return false; else return checkOnline(); } }

        /// <summary>
        /// Block until all bytes in the queue have been transmitted.
        /// </summary>
        internal void Flush()
        {
            checkOnline();
            checkResult();
        }

        /// <summary>
        /// Use this to throw exceptions in derived classes. Correctly handles threading issues
        /// and closes the port if necessary.
        /// </summary>
        /// <param name="reason">Description of fault</param>
        protected void throwException(string reason)
        {
            if (Thread.CurrentThread == m_rxThread)
            {
                throw new CommPortException(reason);
            }
            else
            {
                if (m_online)
                {
                    BeforeClose(true);
                    InternalClose();
                }
                if (m_rxException == null)
                {
                    throw new CommPortException(reason);
                }
                else
                {
                    throw new CommPortException(m_rxException);
                }
            }
        }

        /// <summary>
        /// Queues bytes for transmission. 
        /// </summary>
        /// <param name="tosend">Array of bytes to be sent</param>
        protected void send(byte[] tosend)
        {
            uint sent = 0;
            checkOnline();
            checkResult();
            m_writeCount = tosend.GetLength(0);
            if (Win32Com.WriteFile(m_hPort, tosend, (uint)m_writeCount, out sent, m_ptrUWO))
            {
                m_writeCount -= (int)sent;
            }
            else
            {
                if (Marshal.GetLastWin32Error() != Win32Com.ERROR_IO_PENDING) throwException("Send failed");
                m_dataQueued = true;
            }
        }

        /// <summary>
        /// Queues a single byte for transmission.
        /// </summary>
        /// <param name="tosend">Byte to be sent</param>
        protected void send(byte tosend)
        {
            byte[] b = new byte[1];
            b[0] = tosend;
            send(b);
        }

        private void checkResult()
        {
            uint sent = 0;

            if (m_writeCount > 0)
            {
                if (Win32Com.GetOverlappedResult(m_hPort, m_ptrUWO, out sent, m_checkSends))
                {
                    if (m_checkSends)
                    {
                        m_writeCount -= (int)sent;
                        if (m_writeCount != 0) throwException("Send Timeout");
                        m_writeCount = 0;
                    }
                }
                else
                {
                    if (Marshal.GetLastWin32Error() != Win32Com.ERROR_IO_INCOMPLETE) throwException("Write Error");
                }
            }
        }

        /// <summary>
        /// Sends a protocol byte immediately ahead of any queued bytes.
        /// </summary>
        /// <param name="tosend">Byte to send</param>
        protected void sendImmediate(byte tosend)
        {
            checkOnline();
            if (!Win32Com.TransmitCommChar(m_hPort, tosend)) throwException("Transmission failure");
        }

        /// <summary>
        /// Delay processing.
        /// </summary>
        /// <param name="milliseconds">Milliseconds to delay by</param>
        protected void sleep(int milliseconds)
        {
            Thread.Sleep(milliseconds);
        }

        /// <summary>
        /// Represents the status of the modem control input signals.
        /// </summary>
        protected struct ModemStatus
        {
            private uint status;
            internal ModemStatus(uint val) { status = val; }
            /// <summary>
            /// Condition of the Clear To Send signal.
            /// </summary>
            internal bool cts { get { return ((status & Win32Com.MS_CTS_ON) != 0); } }
            /// <summary>
            /// Condition of the Data Set Ready signal.
            /// </summary>
            internal bool dsr { get { return ((status & Win32Com.MS_DSR_ON) != 0); } }
            /// <summary>
            /// Condition of the Receive Line Status Detection signal.
            /// </summary>
            internal bool rlsd { get { return ((status & Win32Com.MS_RLSD_ON) != 0); } }
            /// <summary>
            /// Condition of the Ring Detection signal.
            /// </summary>
            internal bool ring { get { return ((status & Win32Com.MS_RING_ON) != 0); } }

            public override string ToString()
            {
                return "cts=" + cts.ToString() + "; dsr=" + dsr.ToString()
                       + "; rlsd=" + rlsd.ToString() + "; ring=" + ring.ToString();
            }
        }

        /// <summary>
        /// Gets the status of the modem control input signals.
        /// </summary>
        /// <returns>Modem status object</returns>
        protected ModemStatus getModemStatus()
        {
            uint f;

            checkOnline();
            if (!Win32Com.GetCommModemStatus(m_hPort, out f)) throwException("Unexpected failure");
            return new ModemStatus(f);
        }

        /// <summary>
        /// Represents the current condition of the port queues.
        /// </summary>
        private struct QueueStatus
        {
            private uint status;
            private uint inQueue;
            private uint outQueue;
            private uint inQueueSize;
            private uint outQueueSize;

            internal QueueStatus(uint stat, uint inQ, uint outQ, uint inQs, uint outQs)
            { status = stat; inQueue = inQ; outQueue = outQ; inQueueSize = inQs; outQueueSize = outQs; }
            /// <summary>
            /// Output is blocked by CTS handshaking.
            /// </summary>
            internal bool ctsHold { get { return ((status & Win32Com.COMSTAT.fCtsHold) != 0); } }
            /// <summary>
            /// Output is blocked by DRS handshaking.
            /// </summary>
            internal bool dsrHold { get { return ((status & Win32Com.COMSTAT.fDsrHold) != 0); } }
            /// <summary>
            /// Output is blocked by RLSD handshaking.
            /// </summary>
            internal bool rlsdHold { get { return ((status & Win32Com.COMSTAT.fRlsdHold) != 0); } }
            /// <summary>
            /// Output is blocked because software handshaking is enabled and XOFF was received.
            /// </summary>
            internal bool xoffHold { get { return ((status & Win32Com.COMSTAT.fXoffHold) != 0); } }
            /// <summary>
            /// Output was blocked because XOFF was sent and this station is not yet ready to receive.
            /// </summary>
            internal bool xoffSent { get { return ((status & Win32Com.COMSTAT.fXoffSent) != 0); } }

            /// <summary>
            /// There is a character waiting for transmission in the immediate buffer.
            /// </summary>
            internal bool immediateWaiting { get { return ((status & Win32Com.COMSTAT.fTxim) != 0); } }

            /// <summary>
            /// Number of bytes waiting in the input queue.
            /// </summary>
            internal long InQueue { get { return (long)inQueue; } }
            /// <summary>
            /// Number of bytes waiting for transmission.
            /// </summary>
            internal long OutQueue { get { return (long)outQueue; } }
            /// <summary>
            /// Total size of input queue (0 means information unavailable)
            /// </summary>
            internal long InQueueSize { get { return (long)inQueueSize; } }
            /// <summary>
            /// Total size of output queue (0 means information unavailable)
            /// </summary>
            internal long OutQueueSize { get { return (long)outQueueSize; } }

            public override string ToString()
            {
                StringBuilder m = new StringBuilder("The reception queue is ", 60);
                if (inQueueSize == 0)
                {
                    m.Append("of unknown size and ");
                }
                else
                {
                    m.Append(inQueueSize.ToString() + " bytes long and ");
                }
                if (inQueue == 0)
                {
                    m.Append("is empty.");
                }
                else if (inQueue == 1)
                {
                    m.Append("contains 1 byte.");
                }
                else
                {
                    m.Append("contains ");
                    m.Append(inQueue.ToString());
                    m.Append(" bytes.");
                }
                m.Append(" The transmission queue is ");
                if (outQueueSize == 0)
                {
                    m.Append("of unknown size and ");
                }
                else
                {
                    m.Append(outQueueSize.ToString() + " bytes long and ");
                }
                if (outQueue == 0)
                {
                    m.Append("is empty");
                }
                else if (outQueue == 1)
                {
                    m.Append("contains 1 byte. It is ");
                }
                else
                {
                    m.Append("contains ");
                    m.Append(outQueue.ToString());
                    m.Append(" bytes. It is ");
                }
                if (outQueue > 0)
                {
                    if (ctsHold || dsrHold || rlsdHold || xoffHold || xoffSent)
                    {
                        m.Append("holding on");
                        if (ctsHold) m.Append(" CTS");
                        if (dsrHold) m.Append(" DSR");
                        if (rlsdHold) m.Append(" RLSD");
                        if (xoffHold) m.Append(" Rx XOff");
                        if (xoffSent) m.Append(" Tx XOff");
                    }
                    else
                    {
                        m.Append("pumping data");
                    }
                }
                m.Append(". The immediate buffer is ");
                if (immediateWaiting)
                    m.Append("full.");
                else
                    m.Append("empty.");
                return m.ToString();
            }
        }

        /// <summary>
        /// Get the status of the queues
        /// </summary>
        /// <returns>Queue status object</returns>
        //protected QueueStatus getQueueStatus()
        //{
        //    Win32Com.COMSTAT cs;
        //    Win32Com.COMMPROP cp;
        //    uint er;

        //    checkOnline();
        //    if (!Win32Com.ClearCommError(m_hPort, out er, out cs)) throwException("Unexpected failure");
        //    if (!Win32Com.GetCommProperties(m_hPort, out cp)) throwException("Unexpected failure");
        //    return new QueueStatus(cs.Flags, cs.cbInQue, cs.cbOutQue, cp.dwCurrentRxQueue, cp.dwCurrentTxQueue);
        //}

        /// <summary>
        /// Test if the line is congested (data being queued for send faster than it is being dequeued)
        /// This detects if baud rate is too slow or if handshaking is not allowing enough transmission
        /// time. It should be called at reasonably long fixed intervals. If data has been sent during
        /// the interval, congestion is reported if the queue was never empty during the interval.
        /// </summary>
        /// <returns>True if congested</returns>
        protected bool isCongested()
        {
            bool e;
            if (!m_dataQueued) return false;
            lock (m_empty) { e = m_empty[0]; m_empty[0] = false; }
            m_dataQueued = false;
            return !e;
        }

        /// <summary>
        /// True if the RTS pin is controllable via the RTS property
        /// </summary>
        protected bool rtsAvailable { get { return (m_stateRTS < 2); } }

        /// <summary>
        /// Set the state of the RTS modem control output
        /// </summary>
        protected bool rts
        {
            set
            {
                if (m_stateRTS > 1) return;
                checkOnline();
                if (value)
                {
                    if (Win32Com.EscapeCommFunction(m_hPort, Win32Com.SETRTS))
                        m_stateRTS = 1;
                    else
                        throwException("Unexpected Failure");
                }
                else
                {
                    if (Win32Com.EscapeCommFunction(m_hPort, Win32Com.CLRRTS))
                        m_stateRTS = 0;
                    else
                        throwException("Unexpected Failure");
                }
            }
            get
            {
                return (m_stateRTS == 1);
            }
        }

        /// <summary>
        /// True if the DTR pin is controllable via the DTR property
        /// </summary>
        protected bool dtrAvailable { get { return (m_stateDTR < 2); } }

        /// <summary>
        /// The state of the DTR modem control output
        /// </summary>
        protected bool dtr
        {
            set
            {
                if (m_stateDTR > 1) return;
                checkOnline();  
              if (value)
                {
                    if (Win32Com.EscapeCommFunction(m_hPort, Win32Com.SETDTR))
                        m_stateDTR = 1;
                    else
                        throwException("Unexpected Failure");
                }
                else
                {
                    if (Win32Com.EscapeCommFunction(m_hPort, Win32Com.CLRDTR))
                        m_stateDTR = 0;
                    else
                        throwException("Unexpected Failure");
                }
            }
            get
            {
                return (m_stateDTR == 1);
            }
        }

        /// <summary>
        /// Assert or remove a break condition from the transmission line
        /// </summary>
        protected bool Break
        {
            set
            {
                if (m_stateBRK > 1) return;
                checkOnline();
                if (value)
                {
                    if (Win32Com.EscapeCommFunction(m_hPort, Win32Com.SETBREAK))
                        m_stateBRK = 0;
                    else
                        throwException("Unexpected Failure");
                }
                else
                {
                    if (Win32Com.EscapeCommFunction(m_hPort, Win32Com.CLRBREAK))
                        m_stateBRK = 0;
                    else
                        throwException("Unexpected Failure");
                }
            }
            get
            {
                return (m_stateBRK == 1);
            }
        }

        /// <summary>
        /// Override this to provide settings. (NB this is called during Open method)
        /// </summary>
        /// <returns>CommBaseSettings, or derived object with required settings initialised</returns>
        internal virtual CommBaseSettings CommSettings() { return new CommBaseSettings(); }

        /// <summary>
        /// Override this to provide processing after the port is openned (i.e. to configure remote
        /// device or just check presence).
        /// </summary>
        /// <returns>false to close the port again</returns>
        protected virtual bool AfterOpen() { return true; }

        /// <summary>
        /// Override this to provide processing prior to port closure.
        /// </summary>
        /// <param name="error">True if closing due to an error</param>
        protected virtual void BeforeClose(bool error) { }

        /// <summary>
        /// Override this to process received bytes.
        /// </summary>
        /// <param name="ch">The byte that was received</param>
        protected virtual void OnRxChar(byte ch) { }

        /// <summary>
        /// Override this to take action when transmission is complete (i.e. all bytes have actually
        /// been sent, not just queued).
        /// </summary>
        protected virtual void OnTxDone() { }

        /// <summary>
        /// Override this to take action when a break condition is detected on the input line.
        /// </summary>
        protected virtual void OnBreak() { }

        /// <summary>
        /// Override this to take action when one or more modem status inputs change state
        /// </summary>
        /// <param name="mask">The status inputs that have changed state</param>
        /// <param name="state">The state of the status inputs</param>
        protected virtual void OnStatusChange(ModemStatus mask, ModemStatus state) { }

        /// <summary>
        /// Override this to take action when the reception thread closes due to an exception being thrown.
        /// </summary>
        /// <param name="e">The exception which was thrown</param>
        protected virtual void OnRxException(Exception e) { }

        private void receiveThread()
        {
            byte[] buf = new Byte[1];
            uint gotbytes;
            bool starting;

            starting = true;
            AutoResetEvent sg = new AutoResetEvent(false);
            Win32Com.OVERLAPPED ov = new Win32Com.OVERLAPPED();

            IntPtr unmanagedOv;
            IntPtr uMask;
            uint eventMask = 0;
            unmanagedOv = Marshal.AllocHGlobal(Marshal.SizeOf(ov));
            uMask = Marshal.AllocHGlobal(Marshal.SizeOf(eventMask));

            ov.Offset = 0; ov.OffsetHigh = 0;
#pragma warning disable 0618
            ov.hEvent = sg.Handle;
#pragma warning restore 0618
            Marshal.StructureToPtr(ov, unmanagedOv, true);

            try
            {
                while (true)
                {
                    if (!Win32Com.SetCommMask(m_hPort, Win32Com.EV_RXCHAR | Win32Com.EV_TXEMPTY | Win32Com.EV_CTS | Win32Com.EV_DSR
                        | Win32Com.EV_BREAK | Win32Com.EV_RLSD | Win32Com.EV_RING | Win32Com.EV_ERR))
                    {
                        throw new CommPortException("IO Error [001]");
                    }
                    Marshal.WriteInt32(uMask, 0);
                    if (starting) { m_startEvent.Set(); starting = false; }
                    if (!Win32Com.WaitCommEvent(m_hPort, uMask, unmanagedOv))
                    {
                        if (Marshal.GetLastWin32Error() == Win32Com.ERROR_IO_PENDING)
                        {
                            sg.WaitOne();
                        }
                        else
                        {
                            throw new CommPortException("IO Error [002]");
                        }
                    }
                    eventMask = (uint)Marshal.ReadInt32(uMask);
                    if ((eventMask & Win32Com.EV_ERR) != 0)
                    {
                        UInt32 errs;
                        if (Win32Com.ClearCommError(m_hPort, out errs, IntPtr.Zero))
                        {
                            //BREAK condition has an error flag and and an event flag. Not sure if both
                            //are always raised, so if CE_BREAK is only error flag ignore it and set the EV_BREAK
                            //flag for normal handling. Also made more robust by handling case were no recognised
                            //error was present in the flags.
                            int ec = 0;
                            StringBuilder s = new StringBuilder("UART Error: ", 40);
                            if ((errs & Win32Com.CE_FRAME) != 0) { s = s.Append("Framing,"); ec++; }
                            if ((errs & Win32Com.CE_IOE) != 0) { s = s.Append("IO,"); ec++; }
                            if ((errs & Win32Com.CE_OVERRUN) != 0) { s = s.Append("Overrun,"); ec++; }
                            if ((errs & Win32Com.CE_RXOVER) != 0) { s = s.Append("Receive Cverflow,"); ec++; }
                            if ((errs & Win32Com.CE_RXPARITY) != 0) { s = s.Append("Parity,"); ec++; }
                            if ((errs & Win32Com.CE_TXFULL) != 0) { s = s.Append("Transmit Overflow,"); ec++; }
                            if (ec > 0)
                            {
                                s.Length = s.Length - 1;
                                throw new CommPortException(s.ToString());
                            }
                            else
                            {
                                if (errs == Win32Com.CE_BREAK)
                                {
                                    eventMask |= Win32Com.EV_BREAK;
                                }
                                else
                                {
                                    throw new CommPortException("IO Error [003]");
                                }
                            }
                        }
                        else
                        {
                            throw new CommPortException("IO Error [003]");
                        }
                    }
                    if ((eventMask & Win32Com.EV_RXCHAR) != 0)
                    {
                        do
                        {
                            gotbytes = 0;
                            if (!Win32Com.ReadFile(m_hPort, buf, 1, out gotbytes, unmanagedOv))
                            {
                                int x = Marshal.GetLastWin32Error();
                                throw new CommPortException("IO Error [004]");
                            }
                            if (gotbytes == 1) OnRxChar(buf[0]);
                        } while (gotbytes > 0);
                    }
                    if ((eventMask & Win32Com.EV_TXEMPTY) != 0)
                    {
                        lock (m_empty) m_empty[0] = true;
                        OnTxDone();
                    }
                    if ((eventMask & Win32Com.EV_BREAK) != 0) OnBreak();

                    uint i = 0;
                    if ((eventMask & Win32Com.EV_CTS) != 0) i |= Win32Com.MS_CTS_ON;
                    if ((eventMask & Win32Com.EV_DSR) != 0) i |= Win32Com.MS_DSR_ON;
                    if ((eventMask & Win32Com.EV_RLSD) != 0) i |= Win32Com.MS_RLSD_ON;
                    if ((eventMask & Win32Com.EV_RING) != 0) i |= Win32Com.MS_RING_ON;
                    if (i != 0)
                    {
                        uint f;
                        if (!Win32Com.GetCommModemStatus(m_hPort, out f)) throw new CommPortException("IO Error [005]");
                        OnStatusChange(new ModemStatus(i), new ModemStatus(f));
                    }
                }
            }
            catch (Exception e)
            {
                Win32Com.CancelIo(m_hPort);
                if (uMask != IntPtr.Zero) Marshal.FreeHGlobal(uMask);
                if (unmanagedOv != IntPtr.Zero) Marshal.FreeHGlobal(unmanagedOv);

                if (!(e is ThreadAbortException))
                {
                    m_rxException = e;
                    OnRxException(e);
                }
            }
        }

        private bool checkOnline()
        {
            if ((m_rxException != null) && (!m_rxExceptionReported))
            {
                m_rxExceptionReported = true;
                throwException("rx");
            }
            if (m_online)
            {
                if (m_hPort != (System.IntPtr)Win32Com.INVALID_HANDLE_VALUE) return true;
                throwException("Offline");
                return false;
            }
            else
            {
                if (m_auto)
                { if (Open()) return true; }
                throwException("Offline");
                return false;
            }
        }
    }

    /// <summary>
    /// Overlays CommBase to provide line or packet oriented communications to derived classes. Strings
    /// are sent and received and the Transact method is added which transmits a string and then blocks until
    /// a reply string has been received (subject to a timeout).
    /// </summary>
    public abstract class CommLine : CommBase
    {
        private byte[] m_rxBuffer;
        private uint m_rxBufferP = 0;
        private ASCII m_rxTerm;
        private ASCII[] m_txTerm;
        private ASCII[] m_rxFilter;
        private string m_rxString = "";
        private ManualResetEvent m_transFlag = new ManualResetEvent(true);
        private uint m_transTimeout;

        /// <summary>
        /// Extends CommBaseSettings to add the settings used by CommLine.
        /// </summary>
        internal class CommLineSettings : CommBase.CommBaseSettings
        {
            /// <summary>
            /// Maximum size of received string (default: 256)
            /// </summary>
            internal int rxStringBufferSize = 256;
            /// <summary>
            /// ASCII code that terminates a received string (default: CR)
            /// </summary>
            internal ASCII rxTerminator = ASCII.CR;
            /// <summary>
            /// ASCII codes that will be ignored in received string (default: null)
            /// </summary>
            internal ASCII[] rxFilter;
            /// <summary>
            /// Maximum time (ms) for the Transact method to complete (default: 500)
            /// </summary>
            internal int transactTimeout = 1000;
            /// <summary>
            /// ASCII codes transmitted after each Send string (default: null)
            /// </summary>
            internal ASCII[] txTerminator;
        }

        /// <summary>
        /// Queue the ASCII representation of a string and then the set terminator bytes for sending.
        /// </summary>
        /// <param name="toSend">String to be sent.</param>
        protected void send(string toSend)
        {
            uint l = (uint)Encoding.ASCII.GetByteCount(toSend);
            if (m_txTerm != null) l += (uint)m_txTerm.GetLength(0);
            byte[] b = new byte[l];
            byte[] s = Encoding.ASCII.GetBytes(toSend);
            int i;
            for (i = 0; (i <= s.GetUpperBound(0)); i++) b[i] = s[i];
            if (m_txTerm != null) for (int j = 0; (j <= m_txTerm.GetUpperBound(0)); j++, i++) b[i] = (byte)m_txTerm[j];
            send(b);
        }

        /// <summary>
        /// Transmits the ASCII representation of a string followed by the set terminator bytes and then
        /// awaits a response string.
        /// </summary>
        /// <param name="toSend">The string to be sent.</param>
        /// <returns>The response string.</returns>
        protected string transact(string toSend)
        {
            send(toSend);
            m_transFlag.Reset();
            if (!m_transFlag.WaitOne((int)m_transTimeout, false)) throwException("Timeout");
            string s;
            lock (m_rxString) { s = m_rxString; }
            return s;
        }

        /// <summary>
        /// If a derived class overrides ComSettings(), it must call this prior to returning the settings to
        /// the base class.
        /// </summary>
        /// <param name="s">Class containing the appropriate settings.</param>
        internal void Setup(CommLineSettings s)
        {
            m_rxBuffer = new byte[s.rxStringBufferSize];
            m_rxTerm = s.rxTerminator;
            m_rxFilter = s.rxFilter;
            m_transTimeout = (uint)s.transactTimeout;
            m_txTerm = s.txTerminator;
        }

        /// <summary>
        /// Override this to process unsolicited input lines (not a result of Transact).
        /// </summary>
        /// <param name="s">String containing the received ASCII text.</param>
        protected virtual void OnRxLine(string s) { }

        protected override void OnRxChar(byte ch)
        {
            ASCII ca = (ASCII)ch;
            if ((ca == m_rxTerm) || (m_rxBufferP > m_rxBuffer.GetUpperBound(0)))
            {
                lock (m_rxString) { m_rxString = Encoding.ASCII.GetString(m_rxBuffer, 0, (int)m_rxBufferP); }
                m_rxBufferP = 0;
                if (m_transFlag.WaitOne(0, false))
                {
                    OnRxLine(m_rxString);
                }
                else
                {
                    m_transFlag.Set();
                }
            }
            else
            {
                bool wr = true;
                if (m_rxFilter != null)
                {
                    for (int i = 0; i <= m_rxFilter.GetUpperBound(0); i++) if (m_rxFilter[i] == ca) wr = false;
                }
                if (wr)
                {
                    m_rxBuffer[m_rxBufferP] = ch;
                    m_rxBufferP++;
                }
            }
        }
    }

    /// <summary>
    /// Exception used for all errors.
    /// </summary>
    internal class CommPortException : ApplicationException
    {
        /// <summary>
        /// Constructor for raising direct exceptions
        /// </summary>
        /// <param name="desc">Description of error</param>
        internal CommPortException(string desc) : base(desc) { }

        /// <summary>
        /// Constructor for re-raising exceptions from receive thread
        /// </summary>
        /// <param name="e">Inner exception raised on receive thread</param>
        internal CommPortException(Exception e) : base("Receive Thread Exception", e) { }
    }

    internal class Win32Com
    {

        /// <summary>
        /// Opening Testing and Closing the Port Handle.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr CreateFile(String lpFileName, UInt32 dwDesiredAccess, UInt32 dwShareMode,
            IntPtr lpSecurityAttributes, UInt32 dwCreationDisposition, UInt32 dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        //Constants for errors:
        internal const UInt32 ERROR_FILE_NOT_FOUND = 2;
        internal const UInt32 ERROR_INVALID_NAME = 123;
        internal const UInt32 ERROR_ACCESS_DENIED = 5;
        internal const UInt32 ERROR_IO_PENDING = 997;
        internal const UInt32 ERROR_IO_INCOMPLETE = 996;

        //Constants for return value:
        internal const Int32 INVALID_HANDLE_VALUE = -1;

        //Constants for dwFlagsAndAttributes:
        internal const UInt32 FILE_FLAG_OVERLAPPED = 0x40000000;

        //Constants for dwCreationDisposition:
        internal const UInt32 OPEN_EXISTING = 3;

        //Constants for dwDesiredAccess:
        internal const UInt32 GENERIC_READ = 0x80000000;
        internal const UInt32 GENERIC_WRITE = 0x40000000;

        [DllImport("kernel32.dll")]
        internal static extern Boolean CloseHandle(IntPtr hObject);

        /// <summary>
        /// Manipulating the communications settings.
        /// </summary>

        [DllImport("kernel32.dll")]
        internal static extern Boolean GetCommState(IntPtr hFile, ref DCB lpDCB);

        [DllImport("kernel32.dll")]
        internal static extern Boolean GetCommTimeouts(IntPtr hFile, out COMMTIMEOUTS lpCommTimeouts);

        [DllImport("kernel32.dll")]
        internal static extern Boolean BuildCommDCBAndTimeouts(String lpDef, ref DCB lpDCB, ref COMMTIMEOUTS lpCommTimeouts);

        [DllImport("kernel32.dll")]
        internal static extern Boolean SetCommState(IntPtr hFile, [In] ref DCB lpDCB);

        [DllImport("kernel32.dll")]
        internal static extern Boolean SetCommTimeouts(IntPtr hFile, [In] ref COMMTIMEOUTS lpCommTimeouts);

        [DllImport("kernel32.dll")]
        internal static extern Boolean SetupComm(IntPtr hFile, UInt32 dwInQueue, UInt32 dwOutQueue);

        [StructLayout(LayoutKind.Sequential)]
        internal struct COMMTIMEOUTS
        {
            internal UInt32 ReadIntervalTimeout;
            internal UInt32 ReadTotalTimeoutMultiplier;
            internal UInt32 ReadTotalTimeoutConstant;
            internal UInt32 WriteTotalTimeoutMultiplier;
            internal UInt32 WriteTotalTimeoutConstant;
        }
        internal const UInt32 MAXDWORD = 0xffffffff;

        [StructLayout(LayoutKind.Sequential)]
        internal struct DCB
        {
            internal Int32 DCBlength;
            internal Int32 BaudRate;
            internal Int32 PackedValues;
            internal Int16 wReserved;
            internal Int16 XonLim;
            internal Int16 XoffLim;
            internal Byte ByteSize;
            internal Byte Parity;
            internal Byte StopBits;
            internal Byte XonChar;
            internal Byte XoffChar;
            internal Byte ErrorChar;
            internal Byte EofChar;
            internal Byte EvtChar;
            internal Int16 wReserved1;

            internal void init(bool parity, bool outCTS, bool outDSR, int dtr, bool inDSR, bool txc, bool xOut,
                bool xIn, int rts)
            {
                DCBlength = 28; PackedValues = 0x4001;
                if (parity) PackedValues |= 0x0002;
                if (outCTS) PackedValues |= 0x0004;
                if (outDSR) PackedValues |= 0x0008;
                PackedValues |= ((dtr & 0x0003) << 4);
                if (inDSR) PackedValues |= 0x0040;
                if (txc) PackedValues |= 0x0080;
                if (xOut) PackedValues |= 0x0100;
                if (xIn) PackedValues |= 0x0200;
                PackedValues |= ((rts & 0x0003) << 12);

            }
        }

        /// <summary>
        /// Reading and writing.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern Boolean WriteFile(IntPtr fFile, Byte[] lpBuffer, UInt32 nNumberOfBytesToWrite,
            out UInt32 lpNumberOfBytesWritten, IntPtr lpOverlapped);

        [StructLayout(LayoutKind.Sequential)]
        internal struct OVERLAPPED
        {
            internal UIntPtr Internal;
            internal UIntPtr InternalHigh;
            internal UInt32 Offset;
            internal UInt32 OffsetHigh;
            internal IntPtr hEvent;
        }

        [DllImport("kernel32.dll")]
        internal static extern Boolean SetCommMask(IntPtr hFile, UInt32 dwEvtMask);

        // Constants for dwEvtMask:
        internal const UInt32 EV_RXCHAR = 0x0001;
        internal const UInt32 EV_RXFLAG = 0x0002;
        internal const UInt32 EV_TXEMPTY = 0x0004;
        internal const UInt32 EV_CTS = 0x0008;
        internal const UInt32 EV_DSR = 0x0010;
        internal const UInt32 EV_RLSD = 0x0020;
        internal const UInt32 EV_BREAK = 0x0040;
        internal const UInt32 EV_ERR = 0x0080;
        internal const UInt32 EV_RING = 0x0100;
        internal const UInt32 EV_PERR = 0x0200;
        internal const UInt32 EV_RX80FULL = 0x0400;
        internal const UInt32 EV_EVENT1 = 0x0800;
        internal const UInt32 EV_EVENT2 = 0x1000;

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern Boolean WaitCommEvent(IntPtr hFile, IntPtr lpEvtMask, IntPtr lpOverlapped);

        [DllImport("kernel32.dll")]
        internal static extern Boolean CancelIo(IntPtr hFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern Boolean ReadFile(IntPtr hFile, [Out] Byte[] lpBuffer, UInt32 nNumberOfBytesToRead,
            out UInt32 nNumberOfBytesRead, IntPtr lpOverlapped);

        [DllImport("kernel32.dll")]
        internal static extern Boolean TransmitCommChar(IntPtr hFile, Byte cChar);

        /// <summary>
        /// Control port functions.
        /// </summary>
        [DllImport("kernel32.dll")]
        internal static extern Boolean EscapeCommFunction(IntPtr hFile, UInt32 dwFunc);

        // Constants for dwFunc:
        internal const UInt32 SETXOFF = 1;
        internal const UInt32 SETXON = 2;
        internal const UInt32 SETRTS = 3;
        internal const UInt32 CLRRTS = 4;
        internal const UInt32 SETDTR = 5;
        internal const UInt32 CLRDTR = 6;
        internal const UInt32 RESETDEV = 7;
        internal const UInt32 SETBREAK = 8;
        internal const UInt32 CLRBREAK = 9;

        [DllImport("kernel32.dll")]
        internal static extern Boolean GetCommModemStatus(IntPtr hFile, out UInt32 lpModemStat);

        // Constants for lpModemStat:
        internal const UInt32 MS_CTS_ON = 0x0010;
        internal const UInt32 MS_DSR_ON = 0x0020;
        internal const UInt32 MS_RING_ON = 0x0040;
        internal const UInt32 MS_RLSD_ON = 0x0080;

        /// <summary>
        /// Status Functions.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern Boolean GetOverlappedResult(IntPtr hFile, IntPtr lpOverlapped,
            out UInt32 nNumberOfBytesTransferred, Boolean bWait);

        [DllImport("kernel32.dll")]
        internal static extern Boolean ClearCommError(IntPtr hFile, out UInt32 lpErrors, IntPtr lpStat);
        [DllImport("kernel32.dll")]
        internal static extern Boolean ClearCommError(IntPtr hFile, out UInt32 lpErrors, out COMSTAT cs);

        //Constants for lpErrors:
        internal const UInt32 CE_RXOVER = 0x0001;
        internal const UInt32 CE_OVERRUN = 0x0002;
        internal const UInt32 CE_RXPARITY = 0x0004;
        internal const UInt32 CE_FRAME = 0x0008;
        internal const UInt32 CE_BREAK = 0x0010;
        internal const UInt32 CE_TXFULL = 0x0100;
        internal const UInt32 CE_PTO = 0x0200;
        internal const UInt32 CE_IOE = 0x0400;
        internal const UInt32 CE_DNS = 0x0800;
        internal const UInt32 CE_OOP = 0x1000;
        internal const UInt32 CE_MODE = 0x8000;

        [StructLayout(LayoutKind.Sequential)]
        internal struct COMSTAT
        {
            internal const uint fCtsHold = 0x1;
            internal const uint fDsrHold = 0x2;
            internal const uint fRlsdHold = 0x4;
            internal const uint fXoffHold = 0x8;
            internal const uint fXoffSent = 0x10;
            internal const uint fEof = 0x20;
            internal const uint fTxim = 0x40;
            internal UInt32 Flags;
            internal UInt32 cbInQue;
            internal UInt32 cbOutQue;
        }
        [DllImport("kernel32.dll")]
        internal static extern Boolean GetCommProperties(IntPtr hFile, out COMMPROP cp);

        [StructLayout(LayoutKind.Sequential)]
        internal struct COMMPROP
        {
            internal UInt16 wPacketLength;
            internal UInt16 wPacketVersion;
            internal UInt32 dwServiceMask;
            internal UInt32 dwReserved1;
            internal UInt32 dwMaxTxQueue;
            internal UInt32 dwMaxRxQueue;
            internal UInt32 dwMaxBaud;
            internal UInt32 dwProvSubType;
            internal UInt32 dwProvCapabilities;
            internal UInt32 dwSettableParams;
            internal UInt32 dwSettableBaud;
            internal UInt16 wSettableData;
            internal UInt16 wSettableStopParity;
            internal UInt32 dwCurrentTxQueue;
            internal UInt32 dwCurrentRxQueue;
            internal UInt32 dwProvSpec1;
            internal UInt32 dwProvSpec2;
            internal Byte wcProvChar;
        }
    }
}
