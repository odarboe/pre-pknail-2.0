using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace PKNailForRevit
{
    public class Distance
    {
        public enum Units { Imperial, MetricMeter, MetricMillimeter, PKInternal, RevitInternal, Undefined }

        #region Data Members
        private double m_value;  // Internal storage is decimal feet (Imperial)
        #endregion

        #region Static Data Members
        private const double cm_feetPerMeter = 3.2808399;
        private const double cm_feetPerMillimeter = .0032808399;
        private const double cm_noValue = -10000001.0001;
        private const double cm_roundingTolerance = 0.0001;
        private const string cm_decExpStr = @"^\s*(?<minus>-?)\s*(?<dec>[\d\.]+)\s*$";
        private static Regex sm_decExp = new Regex(cm_decExpStr, RegexOptions.Compiled);
        private const string cm_fracExpStr = @"^\s*(?<minus>-?)\s*(?<int>\d*)\s*(?<fracNumer>\d+)/(?<fracDenom>\d+)\s*$";
        private static Regex sm_fracExp = new Regex(cm_fracExpStr, RegexOptions.Compiled);
        private const int cm_fracDenom = 32;   // Must be a power of 2
        private const double cm_fracTolerance = 0.05;
        #endregion

        public Distance() { m_value = cm_noValue; }
        public Distance(Distance from) { m_value = from.m_value; }
        public Distance(double val, Distance.Units unit)
        {
            switch (unit)
            {
                case Distance.Units.MetricMeter:
                    m_value = val * cm_feetPerMeter;
                    break;
                case Distance.Units.MetricMillimeter:
                    m_value = val * cm_feetPerMillimeter;
                    break;
                case Distance.Units.Imperial:
                case Distance.Units.PKInternal:
                case Distance.Units.RevitInternal:
                    m_value = val;
                    break;
            }
        }

        public static Json.ExporterFunc<Distance> JsonExporter = delegate(Distance d, Json.JsonWriter w)
        { w.Write(d.m_value); };

        public static Json.ImporterFunc<double, Distance> JsonImporter = delegate(double d)
        { return new Distance(d, Distance.Units.PKInternal); };
 
        public double ValueImperial { get { return m_value; } }
        public double ValueMetricMeters { get { return m_value / cm_feetPerMeter; } }
        public double ValueMetricMillimeters { get { return m_value / cm_feetPerMeter * 1000.0; } }
        public double ValuePKInternal { get { return m_value; } }
        public double ValueRevitInternal { get { return m_value; } }

        public bool IsNoValue { get { return (m_value == cm_noValue); } }
        public bool IsZero { get { return (Math.Abs(m_value) < cm_roundingTolerance); } }

        public static Distance NoValue = new Distance();
        public static Distance Zero = new Distance(0.0, Distance.Units.PKInternal);
        public static Distance VerySmall = new Distance(1.0 / (12.0 * 16.0), Distance.Units.PKInternal);  // 1/16"
        public static Distance VeryLarge = new Distance(2640.0, Distance.Units.PKInternal);  // 1/2 mile

        public static bool operator ==(Distance lhs, Distance rhs)
        {
            if (System.Object.ReferenceEquals(lhs, rhs)) { return true; }
            if ((object)lhs == null || (object)rhs == null) { return false; }
            return (Math.Abs(lhs.m_value - rhs.m_value) < cm_roundingTolerance);
        }

        public static bool operator !=(Distance lhs, Distance rhs)
        { return !(lhs == rhs); }

        public static bool operator <(Distance lhs, Distance rhs)
        { return (lhs != rhs && lhs.m_value < rhs.m_value); }

        public static bool operator <=(Distance lhs, Distance rhs)
        { return (lhs == rhs || lhs.m_value < rhs.m_value); }

        public static bool operator >(Distance lhs, Distance rhs)
        { return (lhs != rhs && lhs.m_value > rhs.m_value); }

        public static bool operator >=(Distance lhs, Distance rhs)
        { return (lhs == rhs || lhs.m_value > rhs.m_value); }

        public override bool Equals(Object obj)
        { return obj is Distance && this == (Distance)obj; }

        public override int GetHashCode()
        { return m_value.GetHashCode(); }

        public Distance Round(Distance roundingIncrement)
        {
            if (!roundingIncrement.IsNoValue)
            {
                double rounded = Math.Round(m_value / roundingIncrement.m_value) * roundingIncrement.m_value;
                return new Distance(rounded, Distance.Units.PKInternal);
            }
            else
            { return this; }
        }

        public static Distance operator +(Distance lhs, Distance rhs)
        { return new Distance(lhs.m_value + rhs.m_value, Distance.Units.PKInternal); }
        public static Distance operator -(Distance lhs, Distance rhs)
        { return new Distance(lhs.m_value - rhs.m_value, Distance.Units.PKInternal); }
        public static Distance operator -(Distance val)
        { return new Distance(-val.m_value, Distance.Units.PKInternal); }

        public static Distance operator *(Distance lhs, double rhs)
        { return new Distance(lhs.m_value * rhs, Distance.Units.PKInternal); }

        public static int Compare(Distance x, Distance y)
        {
            if (x == y) { return 0; }
            else if (x < y) { return -1; }
            else { return 1; }
        }

        public static Distance Abs(Distance d)
        { return new Distance(Math.Abs(d.m_value), Distance.Units.PKInternal); }

        public static Distance ParseImperial(string feetStr, string inchesStr)
        {
            double feetD; double inchesD;
            bool isValidFeetStr; bool isValidInchesStr; bool isValidFeetVal; bool isValidInchesVal;

            parseImperialWorker(feetStr, out isValidFeetStr, out isValidFeetVal, out feetD);
            parseImperialWorker(inchesStr, out isValidInchesStr, out isValidInchesVal, out inchesD);
            if (   (isValidFeetStr && isValidInchesStr)
                && (isValidFeetVal || isValidInchesVal))
            {
                if (isValidFeetVal && inchesD < 0.0)
                    { return Distance.NoValue; }

                double distD = feetD;
                if (feetD < 0.0)
                { distD += -inchesD / 12.0; }
                else
                { distD += inchesD / 12.0; }

                return new Distance(distD, Distance.Units.Imperial);
            }
            else
            { return Distance.NoValue; }
        }

        private static void parseImperialWorker(string str, out bool isValidStr, out bool isValidValue, out double result)
        {
            isValidStr = false; isValidValue = false; result = 0.0;

            if (str.Length == 0) { isValidStr = true; return; }

            Match fractionMatch = sm_fracExp.Match(str);
            if (fractionMatch.Success)
            {
                isValidStr = true;

                bool isNegative = (fractionMatch.Result("${minus}").Length > 0);
                string intStr = fractionMatch.Result("${int}");
                string fracNumStr = fractionMatch.Result("${fracNumer}");
                string fracDenStr = fractionMatch.Result("${fracDenom}");

                double intD;
                if (Double.TryParse(intStr, out intD))
                {
                    result += (result < 0.0 || isNegative) ? -intD : intD;
                    isValidValue = true;
                }
                double fracNumD;
                double fracDenD;
                if (Double.TryParse(fracNumStr, out fracNumD)
                    && Double.TryParse(fracDenStr, out fracDenD))
                {
                    if (fracDenD != 0.0)
                    {
                        result += (result < 0.0 || isNegative) ?
                                  -fracNumD / fracDenD
                                 : fracNumD / fracDenD;
                        isValidValue = true;
                    }
                    else if (fracNumD != 0.0)
                    { isValidStr = false; }
                }
            }
            else
            {
                Match decimalMatch = sm_decExp.Match(str);
                if (decimalMatch.Success)
                {
                    isValidStr = true;

                    double decD;
                    bool isNegative = (decimalMatch.Result("${minus}").Length > 0);
                    if (Double.TryParse(decimalMatch.Result("${dec}"), out decD))
                    {
                        result += (isNegative) ? -decD : decD;
                        isValidValue = true;
                    }
                }
            }
        }

        public void RenderImperial(out string feetStr, out string inchesStr)
        {
            feetStr = "";
            inchesStr = "";

            // Special cases
            if (IsNoValue) return;
            if (Math.Abs(m_value) < cm_roundingTolerance) { feetStr = "0"; inchesStr = "0"; return; }

            double feetD = m_value;
            Int32 feetI = (feetD >= 0) ? (int)(feetD + cm_roundingTolerance) : -(int)(-(feetD - cm_roundingTolerance));
            feetD -= feetI;

            // Render feet string
            if (Math.Abs(feetI) > 0) { feetStr = feetI.ToString("D"); }

            // Render inches string
            renderInches(feetD, ref feetI, out inchesStr, false);
            if (m_value < 0.0 && feetStr == "") { inchesStr = @"-" + inchesStr; }
        }

        public string RenderImperialAsInches()
        {
            int dummy = 0;
            string retval;
            renderInches(m_value, ref dummy, out retval, true);
            return retval;
        }

        public static string ShortDistanceUnitsLabel(UnitSystem unitSystem)
        {
            return (unitSystem == UnitSystem.Imperial) ? @"""" : @"mm";
        }

        public string RenderShortDistance(UnitSystem unitSystem)
        {
            if (IsNoValue)  { return @""; }
            return (unitSystem == UnitSystem.Imperial) ? RenderImperialAsInches() : RenderMetricMillimeters();
        }

        private static void renderInches(double feetD, ref int integerFeet, out string inchesStr, bool isRenderAsAllInches)
        {
            inchesStr = "";
            string fracStr = "";
            string decStr = "";

            // ...populate integer inches...
            double inchesD = Math.Abs(feetD * 12.0);  // Abs for now; add minus back in later if nec.
            int inchesI = (int)(inchesD + cm_roundingTolerance);
            inchesD -= (double)inchesI;

            // ...and fractional or decimal inches as appropriate
            double numerD = inchesD * cm_fracDenom;
            int numerI = (int)Math.Round(numerD);
            int denomI = cm_fracDenom;
            if (Math.Abs((Double)numerI - numerD) < cm_fracTolerance)
            {
                if (numerI == denomI)
                {
                    inchesI++;
                    if (!isRenderAsAllInches && inchesI == 12)
                    { integerFeet++; inchesI = 0; }
                }
                else if (numerI > 0)
                {
                    while (numerI % 2 == 0) { numerI /= 2; denomI /= 2; }  // Reduce the fraction
                    fracStr = numerI.ToString() + "/" + denomI.ToString();
                }
            }
            else
            {
                inchesD = Math.Round(inchesD, 3);
                if (inchesD == 1.0)
                { inchesI++; }
                else
                { decStr = inchesD.ToString(".###"); }
            }

            if (isRenderAsAllInches && feetD < 0.0) { inchesStr += "-"; }
            if (inchesI > 0) { inchesStr += inchesI.ToString(); }
            if (fracStr != "") { inchesStr += ((inchesStr != "") ? " " : "") + fracStr; }
            else if (decStr != "") { inchesStr += ((inchesStr != "") ? "" : "0") + decStr; }            
        }

        public static Distance ParseMetricMeters(string valStr)
        { return parseMetricWorker(valStr, Distance.Units.MetricMeter); }

        public static Distance ParseMetricMillimeters(string valStr)
        { return parseMetricWorker(valStr, Distance.Units.MetricMillimeter); }

        private static Distance parseMetricWorker(string valStr, Distance.Units units)
        {
            Distance retval = Distance.NoValue;

            Match decimalMatch = sm_decExp.Match(valStr);
            if (decimalMatch.Success)
            {
                double valD;
                bool isNegative = (decimalMatch.Result(@"${minus}").Length > 0);
                if (Double.TryParse(decimalMatch.Result(@"${dec}"), out valD))
                {
                    valD = (isNegative) ? -valD : valD;
                    retval = new Distance(valD, units);
                }
            }

            return retval;
        }

        public string RenderMetricMeters()
        { return renderMetricWorker(Distance.Units.MetricMeter); }

        public string RenderMetricMillimeters()
        { return renderMetricWorker(Distance.Units.MetricMillimeter); }

        private string renderMetricWorker(Distance.Units units)
        {
            if (IsNoValue)  { return @""; }
            else if (Math.Abs(m_value) < cm_roundingTolerance) { return @"0"; }
            else if (units == Distance.Units.MetricMeter)
            { return ValueMetricMeters.ToString(@"#.###"); }
            else if (units == Distance.Units.MetricMillimeter)
            { return ValueMetricMillimeters.ToString(@"F0"); }
            else
            { return @""; }
        }
    }
}
