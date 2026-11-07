using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.Revit;
using Autodesk.Revit.DB;

namespace PKNailForRevit
{
    internal class PKNailParameter
    {
        private static string sm_prefix = @"PKNail ";

        protected string m_name;
        protected ForgeTypeId m_type;

        // Predefined Type parameters
        internal static PKNailParameter FamilyCategory = new PKNailParameter(@"Family Category", SpecTypeId.String.Text);
        internal static PKNailParameter Family = new PKNailParameter(@"Family", SpecTypeId.String.Text);
        internal static PKNailParameter FamilyOrder = new PKNailParameter(@"Family Order", SpecTypeId.Int.Integer);
        internal static PKNailParameter FamilyLateralOrientation = new PKNailParameter(@"Family Lateral Orientation", SpecTypeId.String.Text);
        internal static PKNailParameter FamilyFacingOrientation = new PKNailParameter(@"Family Facing Orientation", SpecTypeId.String.Text);
        internal static PKNailParameter WallTypeUnknownWidth = new PKNailParameter(@"Wall Type Unknown Width", SpecTypeId.Boolean.YesNo);
        internal static PKNailParameter FixedDimensions = new PKNailParameter(@"Family Fixed Dimensions", SpecTypeId.String.Text);
        internal static PKNailParameter FamilyMeasureLocationCentered = new PKNailParameter(@"Family Measure Location Centered", SpecTypeId.Boolean.YesNo);
        internal static PKNailParameter FamilyInstanceElevation = new PKNailParameter(@"Family Instance Elevation", SpecTypeId.String.Text);

        internal void SetDouble(Element boundElement, double val)
        {
            if (!(m_type == SpecTypeId.Number || m_type == SpecTypeId.Length))
            { throw new HardException(@"Parameter types do not match"); }
            getParameter(boundElement).Set(val);
        }

        internal double GetDouble(Element boundElement)
        {
            if (!(m_type == SpecTypeId.Number || m_type == SpecTypeId.Length))
            { throw new HardException(@"Parameter types do not match"); }
            Parameter p = tryGetParameter(boundElement);
            return ((p != null) ? getParameter(boundElement).AsDouble() : 0.0);
        }

        internal void SetString(Element boundElement, string val)
        {
            if (m_type != SpecTypeId.String.Text) { throw new HardException(@"Parameter types do not match"); }
            getParameter(boundElement).Set(val);
        }

        internal string GetString(Element boundElement)
        {
            if (m_type != SpecTypeId.String.Text) { throw new HardException(@"Parameter types do not match"); }
            Parameter p = tryGetParameter(boundElement);
            if (p != null)
            {
                string s = getParameter(boundElement).AsString();
                return (s == null) ? @"" : s;
            }
            else
            { return @""; }
        }

        internal void SetInteger(Element boundElement, int val)
        {
            if (!(m_type == SpecTypeId.Int.Integer || m_type == SpecTypeId.Boolean.YesNo))
            { throw new HardException(@"Parameter types do not match"); }
            getParameter(boundElement).Set(val);
        }

        internal int GetInteger(Element boundElement)
        {
            if (!(m_type == SpecTypeId.Int.Integer || m_type == SpecTypeId.Boolean.YesNo))
            { throw new HardException(@"Parameter types do not match"); }
            Parameter p = tryGetParameter(boundElement);
            return ((p != null) ? getParameter(boundElement).AsInteger() : 0);
        }

        internal void SetDistance(Element boundElement, Distance val)
        {
            if (!(m_type == SpecTypeId.Number || m_type == SpecTypeId.Length))
            { throw new HardException(@"Parameter types do not match"); }
            Parameter p = getParameter(boundElement);
            if (!val.IsNoValue)
            { p.Set(val.ValueRevitInternal); }
            else
            { p.SetValueString(@""); }
        }

        internal Distance GetDistance(Element boundElement)
        {
            if (!(m_type == SpecTypeId.Number || m_type == SpecTypeId.Length))
            { throw new HardException(@"Parameter types do not match"); }
            Parameter p = tryGetParameter(boundElement);
            if (p != null && p.AsValueString() != @"")
            { return new Distance(p.AsDouble(), Distance.Units.RevitInternal); }
            else
            { return Distance.NoValue; }
        }

        internal Parameter GetParameterObject(Element boundElement)
        {
            return getParameterbyName(boundElement, m_name);
        }

        // Every parameter on the element with this name. Normally one; see getParameterbyName for how a
        // document ends up with two.
        internal IList<Parameter> GetParameterObjects(Element boundElement)
        {
            return boundElement.GetParameters(sm_prefix + m_name);
        }

        internal static Parameter GetNamedParameter(Element boundElement, string paramName)
        {
            return getParameterbyName(boundElement, paramName);
        }

        protected PKNailParameter(string name, ForgeTypeId type)
        {
            m_name = name; m_type = type;
        }

        protected Parameter getParameter(Element boundElement)
        {
            Parameter retval = getParameterbyName(boundElement, m_name);
            if (retval == null) { throw new HardException(@"Parameter """ + sm_prefix + @" " + m_name + @""" is undefined"); }
            return retval;
        }

        protected Parameter tryGetParameter(Element boundElement)
        {
            return (getParameterbyName(boundElement, m_name));
        }

        protected static Parameter getParameterbyName(Element boundElement, string paramName)
        {
            string fullParamName = sm_prefix + paramName;
            Parameter retval = boundElement.LookupParameter(fullParamName);

            // An element can carry two parameters with this name: a PKNail template or family defines its own
            // (not shared) and the shared parameter of the same name was bound to the project as well.
            // LookupParameter then returns either one. If it picked an empty one while its namesake holds
            // the data, use the namesake; otherwise keep what LookupParameter returned.
            if (retval != null && !hasValue(retval))
            {
                foreach (Parameter p in boundElement.GetParameters(fullParamName))
                { if (hasValue(p)) { return p; } }
            }

            return retval;
        }

        private static bool hasValue(Parameter p)
        {
            switch (p.StorageType)
            {
                case StorageType.String: return !string.IsNullOrEmpty(p.AsString());
                case StorageType.Integer: return p.AsInteger() != 0;
                case StorageType.Double: return p.HasValue && p.AsDouble() != 0.0;
                case StorageType.ElementId: return p.AsElementId() != ElementId.InvalidElementId;
                default: return false;
            }
        }
    }
}
