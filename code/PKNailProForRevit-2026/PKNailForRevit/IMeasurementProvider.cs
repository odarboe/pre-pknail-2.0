using System;
using System.Collections.Generic;
using System.Text;

namespace PKNailForRevit
{
    public class MeasurementResult
    {
        internal MeasurementResult(Distance d) { m_measurement = d; }
        internal MeasurementResult(string err) { m_errormsg = err; }

        public Distance Measurement { get { return m_measurement; } }
        internal string ErrorMessage { get { return m_errormsg; } }

        protected Distance m_measurement = null;
        protected string m_errormsg = null;
    }

    public class MeasurementEventArgs : EventArgs
    {
        protected MeasurementResult m_result;
        internal MeasurementEventArgs(Distance d)  { m_result = new MeasurementResult(d); }
        internal MeasurementEventArgs(string err) { m_result = new MeasurementResult(err); }
        public MeasurementResult Result  { get { return m_result; } }
    }

    public delegate void MeasurementEventHandler(object sender, MeasurementEventArgs e);

    public interface IMeasurementProvider
    {
        bool IsSyncSupported { get; }
        bool IsAsyncSupported { get; }

        // only if IsAsyncSupported
        void PushContext(MeasurementEventHandler handler);
        void PopContext();

        // only if IsSyncSupported
        void GetMeasurement(out MeasurementResult result);
    }
}
