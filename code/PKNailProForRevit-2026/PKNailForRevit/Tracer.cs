using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Reflection;

namespace PKNailForRevit
{
    internal static class Tracer
    {
        internal static void WriteInfo(int numFrames, String msg)
        {
            write(numFrames, msg);
        }

        internal static void WriteInfo(String msg)
        {
            write(0, msg);
        }

        private static void write(int numFrames, String msg)
        {
            String log = System.DateTime.Now.ToLongTimeString();
            for (int i = numFrames; i >= 0; i--)
            {
                StackFrame callingSF = new StackFrame(i + 2, true);
                log += ": " + callingSF.GetMethod().Name + "(" + callingSF.GetFileLineNumber() + ")";
            }
            log += ": " + msg;
            Trace.TraceInformation(log);
        }
    }
}
