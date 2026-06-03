using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.Revit.DB;

using G2D = PKNailForRevit.Geometry2D;

namespace PKNailForRevit
{
    public class WallState : JsonSerializable
    {
        public WallState()
        {
            construct_empty();
        }

        public WallState(WallState from)
        {
            construct_empty();
            for (int i = 0; i < 2; i++)
            {
                m_IsFaceSurveyed[i] = from.m_IsFaceSurveyed[i];
                m_LevelOffset[i] = from.m_LevelOffset[i];
            }
            for (int i = 0; i < 4; i++)
            {
                m_RefPtOffset[i] = from.m_RefPtOffset[i];
            }
        }

        public bool IsFaceSurveyed(SurveyOrientation so)
        { return m_IsFaceSurveyed[so.Index]; }

        public void SetIsFaceSurveyed(SurveyOrientation so)
        { m_IsFaceSurveyed[so.Index] = true; }

        public void ResetIsFaceSurveyed(SurveyOrientation so)
        { m_IsFaceSurveyed[so.Index] = false; }

        public Distance LevelOffset(SurveyOrientation so)
        { return m_LevelOffset[so.Index]; }

        public void SetLevelOffset(SurveyOrientation so, Distance offset)
        { m_LevelOffset[so.Index] = (offset.IsZero) ? Distance.NoValue : offset; }

        public Distance RefPtOffset(SurveySide side, SurveyOrientation so)
        { return m_RefPtOffset[(2 * side.Index) + so.Index]; }

        public void SetRefPtOffset(SurveySide side, SurveyOrientation so, Distance offset)
        { m_RefPtOffset[(2 * side.Index) + so.Index] = offset; }

        public void ClearRefPtOffsets()
        { for (int i = 0; i < 4; i++) { m_RefPtOffset[i] = null; } }

        public void ApplyToWall(Document doc, UnitSystem unitSystem, Wall w)
        {
            for (int soIdx = 0; soIdx < 2; soIdx++)
            {
                if (m_LevelOffset[soIdx] != Distance.NoValue)
                {
                    if (m_LevelOffsetDetailIdValue[soIdx] != -1)
                    {
                        Distance curLevelOffset = levelOffsetDetail.GetOffset(doc, m_LevelOffsetDetailIdValue[soIdx]);
                        if (curLevelOffset != m_LevelOffset[soIdx])
                        {
                            ElementId elId = new ElementId(m_LevelOffsetDetailIdValue[soIdx]);
                            Element el = doc.GetElement(elId);
                            if (el != null)  { doc.Delete(elId); }
                            m_LevelOffsetDetailIdValue[soIdx] = levelOffsetDetail.CreateInstance(doc, w,
                                                                        SurveyOrientation.FromIndex(soIdx), unitSystem, m_LevelOffset[soIdx]);
                        }
                    }
                    else
                    {
                        m_LevelOffsetDetailIdValue[soIdx] = levelOffsetDetail.CreateInstance(doc, w,
                                                                    SurveyOrientation.FromIndex(soIdx), unitSystem, m_LevelOffset[soIdx]);
                    }
                }
                else if (m_LevelOffsetDetailIdValue[soIdx] != -1)
                {
                    ElementId elId = new ElementId(m_LevelOffsetDetailIdValue[soIdx]);
                    Element el = doc.GetElement(elId);
                    if (el != null)  { doc.Delete(elId); }
                    m_LevelOffsetDetailIdValue[soIdx] = -1;
                }
            }
        }

        public void RemoveFromWall(Document doc, Wall w)
        {
            for (int soIdx = 0; soIdx < 2; soIdx++)
            {
                if (m_LevelOffsetDetailIdValue[soIdx] != -1)
                {
                    ElementId elId = new ElementId(m_LevelOffsetDetailIdValue[soIdx]);
                    Element el = doc.GetElement(elId);
                    if (el != null)  { doc.Delete(elId); }
                    m_LevelOffsetDetailIdValue[soIdx] = -1;
                }
            }
        }

        private void construct_empty()
        {
            m_IsFaceSurveyed = new bool[2] { false, false };
            m_LevelOffset = new Distance[2] { Distance.NoValue, Distance.NoValue };
            m_LevelOffsetDetailIdValue = new long[2] { -1L, -1L };
            m_RefPtOffset = new Distance[4] { null, null, null, null };
        }

        // TODO: Write json import/export functions so these can be private, not public
        public bool[] m_IsFaceSurveyed;
        public Distance[] m_LevelOffset;

        // We ignore this field because the element ID can vary across test file revisions
        [ModelStateCompare(ModelStateCompareAttribute.DirectiveEnum.Ignore)]
        public long[] m_LevelOffsetDetailIdValue;

        public Distance[] m_RefPtOffset;

        private static class levelOffsetDetail
        {
            private const string cm_symbolName = @"PKNail Level Offset Detail";
            private const string cm_labelParamName = @"PKNail Level Offset Label";
            private const string cm_distanceParamName = @"PKNail Level Offset Distance";
            private const string cm_labelPrefix = @"Z: ";

            internal static long CreateInstance(Document doc, Wall w, SurveyOrientation so, UnitSystem unitSystem, Distance levelOffset)
            {
                G2D.Vector edge = RevitSurveyWall.WallEdge(w, so, true);
                Curve locCurve = Utilities.ToRevitLine(edge);
                Level lvl = RevitSurveyWall.BaseLevel(w);
                FamilySymbol sym = getSymbol(doc);
                if (!sym.IsActive) { sym.Activate(); }
                FamilyInstance inst = doc.Create.NewFamilyInstance(locCurve, sym, lvl, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

                Parameter distParam = inst.LookupParameter(cm_distanceParamName);
                if (distParam == null)  { throw new HardException(@"No parameter """ + cm_distanceParamName + @""" in family """ + cm_symbolName + @""""); }
                distParam.Set(levelOffset.ValueRevitInternal);

                Parameter lblParam = inst.LookupParameter(cm_labelParamName);
                if (lblParam == null)  { throw new HardException(@"No parameter """ + cm_labelParamName + @""" in family """ + cm_symbolName + @""""); }
                Distance oneFoot = new Distance(1.0, Distance.Units.Imperial);
                if (edge.Length >= oneFoot.ValueRevitInternal)  // 1 foot
                { lblParam.Set(renderLabel(levelOffset, unitSystem)); }
                else
                { lblParam.Set(@"."); }

                return inst.Id.Value;
            }

            internal static Distance GetOffset(Document doc, long instIDValue)
            {
                Distance retval = Distance.NoValue;

                FamilyInstance inst = doc.GetElement(new ElementId(instIDValue)) as FamilyInstance;
                if (inst != null)
                {
                    Parameter distParam = inst.LookupParameter(cm_distanceParamName);
                    if (distParam == null) { throw new HardException(@"No parameter """ + cm_distanceParamName + @""" in family """ + cm_symbolName + @""""); }
                    double dist = distParam.AsDouble();
                    retval = new Distance(dist, Distance.Units.RevitInternal);
                }

                return retval;
            }

            private static FamilySymbol getSymbol(Document doc)
            {
                FamilySymbol retval = null;

                FilteredElementCollector coll = new FilteredElementCollector(doc);
                coll.WhereElementIsElementType().OfCategory(BuiltInCategory.OST_DetailComponents);
                FilteredElementIterator iter = coll.GetElementIterator();
                iter.Reset();
                while (iter.MoveNext())
                { if (iter.Current.Name == cm_symbolName) { retval = iter.Current as FamilySymbol; break; } }
                if (retval == null)  { throw new HardException(@"Family """ + cm_symbolName + @""" is missing"); }

                return retval;
            }

            private static string renderLabel(Distance dist, UnitSystem unitSystem)
            {
                string retval;

                if (unitSystem == UnitSystem.Imperial)
                {
                    string feet; string inches;
                    dist.RenderImperial(out feet, out inches);
                    retval = cm_labelPrefix;
                    if (dist >= Distance.Zero) { retval += @"+"; }
                    if (feet != @"")
                    { retval += feet; }
                    else
                    {
                        if (dist < Distance.Zero) { retval += @"-"; }
                        retval += @"0";
                    }
                    retval += @"' - ";
                    if (inches != @"")
                    {
                        if (inches[0] == '-')
                        { retval += inches.Substring(1); }
                        else
                        { retval += inches; }
                    }
                    else
                    { retval += @"0"; }
                    retval += @"""";
                }
                else   // unitSystem == UnitSystem.Metric
                {
                    retval = dist.RenderMetricMillimeters() + Distance.ShortDistanceUnitsLabel(unitSystem);
                }

                return retval;
            }
        }
    }
}
