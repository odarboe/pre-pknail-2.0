using System;
using System.Collections.Generic;
using System.Text;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;

namespace PKNailForRevit
{
    public class RevitModelState : ModelState
    {
        public RevitWallModelStateList Walls;
        public RevitDetailCurveModelStateList DetailCurves;
        public RevitFamilyInstanceModelStateList FamilyInstances;

        public RevitModelState()
        {
            Walls = new RevitWallModelStateList();
            DetailCurves = new RevitDetailCurveModelStateList();
            FamilyInstances = new RevitFamilyInstanceModelStateList();
        }

        public RevitModelState(Document doc)
        {
            Walls = new RevitWallModelStateList(doc);
            DetailCurves = new RevitDetailCurveModelStateList(doc);
            FamilyInstances = new RevitFamilyInstanceModelStateList(doc);
        }

        public override bool Compare(ModelState refMS, out string diffMsg)
        {
            diffMsg = "";

            RevitModelState refRMS = refMS as RevitModelState;
            if (refRMS == null) { diffMsg = @"Other state is not a Revit model state"; return false; }

            return (   Walls.Compare(refRMS.Walls, ref diffMsg)
                    && DetailCurves.Compare(refRMS.DetailCurves, ref diffMsg)
                    && FamilyInstances.Compare(refRMS.FamilyInstances, ref diffMsg) );
        }

    }

    public static class RevitModelStateFactory
    {
        public static ModelState ImportRevitModelState(string filePath)
        {
            string modelJson;
            using (System.IO.StreamReader mSR = System.IO.File.OpenText(filePath))
            { modelJson = mSR.ReadToEnd(); }
            return JsonSerializable.FromJson<RevitModelState>(modelJson);
        }

        public static void ExportRevitModelState(ModelState modelState, string filePath)
        {
            string modelJson = modelState.ToPrettyJson();
            using (System.IO.StreamWriter sr = System.IO.File.CreateText(filePath))
            { sr.Write(modelJson); }
        }
    }

    public class RevitWallModelStateList : RevitObjectModelStateList<RevitWallModelState>
    {
        public RevitWallModelStateList() { }   // public for json serialization

        internal RevitWallModelStateList(Document doc)
        {
            FilteredElementCollector coll = new FilteredElementCollector(doc).OfClass(typeof(Wall));
            foreach (Element el in coll.ToElements())
            { Add(new RevitWallModelState(el as Wall)); }
        }

    }

    public class RevitDetailCurveModelStateList : RevitObjectModelStateList<RevitDetailCurveModelState>
    {
        public RevitDetailCurveModelStateList() { }   // public for json serialization

        internal RevitDetailCurveModelStateList(Document doc)
        {
            FilteredElementCollector coll = new FilteredElementCollector(doc).OfClass(typeof(CurveElement));
            foreach (Element el in coll.ToElements())
            {
                DetailCurve c = el as DetailCurve;
                if (c != null && c.GeometryCurve.IsBound) { Add( new RevitDetailCurveModelState(el as DetailCurve)); }
            }
        }
    }

    public class RevitFamilyInstanceModelStateList : RevitObjectModelStateList<RevitFamilyInstanceModelState>
    {
        public RevitFamilyInstanceModelStateList() { }   // public for json serialization

        internal RevitFamilyInstanceModelStateList(Document doc)
        {
            FilteredElementCollector coll = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance));
            foreach (Element el in coll.ToElements())
            { Add(new RevitFamilyInstanceModelState(doc, el as FamilyInstance)); }
        }
    }

    public class RevitObjectModelStateList<T> : List<T> where T : RevitObjectModelState
    {
        internal bool Compare(RevitObjectModelStateList<T> refStateList, ref string diffMsg)
        {
            string objectTypeName = (Count > 0) ? this[0].ObjectTypeName : @"object";

            if (Count != refStateList.Count)
            {
                diffMsg = objectTypeName + @" count does not match: expected " + Count + ", got " + refStateList.Count;
                return false;
            }

            for (int i = 0; i < Count; i++)
            {
                diffMsg = objectTypeName + @"[" + i.ToString() + @"]";

                T thisState = this[i];

                // Find the matching object state in the other list
                T matchingRefState = null;
                foreach (T otherState in refStateList)
                { if (thisState.isMatching(otherState)) { matchingRefState = otherState; break; } }
                if (matchingRefState == null)
                {
                    diffMsg += @": No matching " + objectTypeName + @" in reference model";
                    return false;
                }

                if (!thisState.Compare(matchingRefState, ref diffMsg))
                { return false; }
            }

            return true;
        }
    }

    public abstract class RevitObjectModelState
    {
        internal abstract string ObjectTypeName { get; }
        internal abstract bool isMatching(RevitObjectModelState refState);

        internal bool Compare(RevitObjectModelState refState, ref string diffMsg)
        {
            return compareObjects(this, refState, ref diffMsg);
        }

        private static bool compareObjects(object obj, object refObj, ref string diffMsg)
        {
            if (obj == null || refObj == null)
            {
                if (obj != null || refObj != null)
                {
                    diffMsg += @": one but not both objects are null";
                    return false;
                }
                else { return true; }   // OK for both objects to be null 
            }

            Type t = obj.GetType();
            if (!t.Equals(refObj.GetType()))
            {
                diffMsg += ": types do not match";
                return false;
            }

            System.Reflection.FieldInfo[] fields = t.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

            foreach (System.Reflection.FieldInfo field in fields)
            {
                Type ft = field.FieldType;
                string fieldName = "." + field.Name;

                // Ignore the field if directed by an attribute
                bool isIgnoreField = false;
                object[] attrObjs = field.GetCustomAttributes(typeof(ModelStateCompareAttribute), false);
                foreach (object attrObj in attrObjs)
                {
                    if (((ModelStateCompareAttribute)attrObj).Directive == ModelStateCompareAttribute.DirectiveEnum.Ignore)
                    { isIgnoreField = true; break; }
                }
                if (isIgnoreField)  { continue; }

                if (ft == typeof(int))
                {
                    int objVal = (int)field.GetValue(obj); int refObjVal = (int)field.GetValue(refObj);
                    if (objVal != refObjVal)
                    {
                        diffMsg += fieldName + diffValMsg(objVal.ToString(), refObjVal.ToString());
                        return false;
                    }
                }
                else if (ft == typeof(double))
                {
                    double objVal = (double)field.GetValue(obj); double refObjVal = (double)field.GetValue(refObj);
                    if (Math.Abs(objVal - refObjVal) > 0.0001)
                    {
                        diffMsg += fieldName + diffValMsg(objVal.ToString(), refObjVal.ToString());
                        return false;
                    }
                }
                else if (ft == typeof(string))
                {
                    string objVal = (string)field.GetValue(obj); string refObjVal = (string)field.GetValue(refObj);
                    if (objVal != refObjVal)
                    {
                        diffMsg += fieldName + diffValMsg(objVal, refObjVal);
                        return false;
                    }
                }
                else if (ft == typeof(bool))
                {
                    bool objVal = (bool)field.GetValue(obj); bool refObjVal = (bool)field.GetValue(refObj);
                    if (objVal != refObjVal)
                    {
                        diffMsg += fieldName + diffValMsg(objVal.ToString(), refObjVal.ToString());
                        return false;
                    }
                }
                else
                {
                    Type iListInterface = ft.GetInterface("IList");
                    if (iListInterface != null)
                    {
                        System.Reflection.PropertyInfo countProperty = ft.GetInterface("ICollection").GetProperty("Count");
                        Type[] singleIntParameter = { typeof(System.Int32) };
                        System.Reflection.PropertyInfo itemProperty = iListInterface.GetProperty("Item", singleIntParameter);
                        int objListCount = (int)countProperty.GetValue(field.GetValue(obj), null);
                        int refObjListCount = (int)countProperty.GetValue(field.GetValue(refObj), null);
                        if (objListCount != refObjListCount)
                        {
                            diffMsg += fieldName + @": list count" + diffValMsg(objListCount.ToString(), refObjListCount.ToString());
                            return false;
                        }
                        for (int listIdx = 0; listIdx < objListCount; listIdx++)
                        {
                            object[] idxParam = new object[] { listIdx };
                            object objListItem = itemProperty.GetValue(field.GetValue(obj), idxParam);
                            object refObjListItem = itemProperty.GetValue(field.GetValue(refObj), idxParam);
                            string arrayDiffMsg = fieldName + "[" + listIdx + "]";
                            if (!compareObjects(objListItem, refObjListItem, ref arrayDiffMsg))
                            {
                                diffMsg += arrayDiffMsg;
                                return false;
                            }
                        }
                    }

                    else if (ft.IsClass)
                    {
                        string fieldDiffMsg = fieldName;
                        if (!compareObjects(field.GetValue(obj), field.GetValue(refObj), ref fieldDiffMsg))
                        {
                            diffMsg += fieldDiffMsg;
                            return false;
                        }
                    }

                    else
                    {
                        diffMsg += fieldName + @": """ + ft.ToString() + @""" is not supported by compareObjects()";
                        return false;
                    }
                }
            }

            return true;
        }

        private static string diffValMsg(string thisVal, string otherVal)
        { return @": expected """ + thisVal + @""", got " + otherVal + @""""; }
    }

    public class RevitWallModelState : RevitObjectModelState
    {
        // These fields make up the "key" to determine a unique instance
        public string TypeName;
        public double[] LocationEndPt0;
        public double[] LocationEndPt1;

        public double Width;
        public string LevelName;
        public double LevelElevation;
        public bool IsFlipped;

        // From parameters
        public double WALL_USER_HEIGHT_PARAM;
        public double WALL_TOP_OFFSET;
        public int WALL_TOP_IS_ATTACHED;
        public string WALL_HEIGHT_TYPE;
        public string WALL_BASE_CONSTRAINT;
        public int WALL_BOTTOM_IS_ATTACHED;
        public double WALL_BASE_OFFSET;
        public int WALL_KEY_REF_PARAM;
        public WallState WallState;

        public RevitWallModelState()
        {
            LocationEndPt0 = new double[3];
            LocationEndPt1 = new double[3];
            WallState = null;
        }

        internal RevitWallModelState(Wall w)
        {
            WallType wt = w.WallType;
            TypeName = wt.Name;
            Width = wt.Width;

            Level lvl = RevitSurveyWall.BaseLevel(w);
            if (lvl != null)
            {
                LevelName = lvl.Name;
                LevelElevation = lvl.Elevation;
            }

            LocationEndPt0 = new double[3];
            LocationEndPt1 = new double[3];
            Curve locCurve = ((LocationCurve)w.Location).Curve;
            XYZ endPt0 = locCurve.GetEndPoint(0);
            XYZ endPt1 = locCurve.GetEndPoint(1);
            LocationEndPt0[0] = endPt0.X; LocationEndPt0[1] = endPt0.Y; LocationEndPt0[2] = endPt0.Z;
            LocationEndPt1[0] = endPt1.X; LocationEndPt1[1] = endPt1.Y; LocationEndPt1[2] = endPt1.Z;
            IsFlipped = w.Flipped;

            WALL_USER_HEIGHT_PARAM = w.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM).AsDouble();
            WALL_TOP_OFFSET = w.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET).AsDouble();
            WALL_TOP_IS_ATTACHED = w.get_Parameter(BuiltInParameter.WALL_TOP_IS_ATTACHED).AsInteger();
            Element el = null;
            ElementId elId = w.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).AsElementId();
            try { el = w.Document.GetElement(elId); }
            catch { el = null; }
            if (el != null) { WALL_HEIGHT_TYPE = el.Name; }
            elId = w.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT).AsElementId();
            try { el = w.Document.GetElement(elId); }
            catch { el = null; }
            if (el != null) { WALL_BASE_CONSTRAINT = el.Name; }
            WALL_BOTTOM_IS_ATTACHED = w.get_Parameter(BuiltInParameter.WALL_BOTTOM_IS_ATTACHED).AsInteger();
            WALL_BASE_OFFSET = w.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).AsDouble();
            WALL_KEY_REF_PARAM = w.get_Parameter(BuiltInParameter.WALL_KEY_REF_PARAM).AsInteger();

            string wallStateJson = null;
            Parameter p = PKNailParameter.GetNamedParameter(w, @"WallState");
            if (p != null)  { wallStateJson = p.AsString(); }
            if (wallStateJson != null && wallStateJson != "")
            { WallState = (WallState)Json.JsonMapper.ToObject(typeof(WallState), wallStateJson); }
            else { WallState = null; }
        }

        internal override string ObjectTypeName { get { return @"Wall"; } }

        internal override bool isMatching(RevitObjectModelState refState)
        {
            if (refState is RevitWallModelState)
            {
                RevitWallModelState refStateAsWall = refState as RevitWallModelState;
                if (   TypeName == refStateAsWall.TypeName
                    && Math.Abs(LocationEndPt0[0] - refStateAsWall.LocationEndPt0[0]) < 0.0001
                    && Math.Abs(LocationEndPt0[1] - refStateAsWall.LocationEndPt0[1]) < 0.0001
                    && Math.Abs(LocationEndPt0[2] - refStateAsWall.LocationEndPt0[2]) < 0.0001
                    && Math.Abs(LocationEndPt1[0] - refStateAsWall.LocationEndPt1[0]) < 0.0001
                    && Math.Abs(LocationEndPt1[1] - refStateAsWall.LocationEndPt1[1]) < 0.0001
                    && Math.Abs(LocationEndPt1[2] - refStateAsWall.LocationEndPt1[2]) < 0.0001)
                { return true; }
            }
            return false;
        }
    }

    public class RevitDetailCurveModelState : RevitObjectModelState
    {
        public string LineStyleName;
        public string LevelName;
        public double LevelElevation;
        public double[] LocationEndPt0;
        public double[] LocationEndPt1;

        public RevitDetailCurveModelState()
        {
            LineStyleName = @"";
            LevelName = @"";
            LevelElevation = 0.0;
            LocationEndPt0 = new double[3];
            LocationEndPt1 = new double[3];
            for (int j = 0; j < 3; j++)
            {
                LocationEndPt0[j] = 0.0;
                LocationEndPt1[j] = 0.0;
            }
        }

        internal RevitDetailCurveModelState(DetailCurve line)
        {
            LineStyleName = line.LineStyle.Name;

            Level lvl = line.Document.GetElement(line.LevelId) as Level;
            if (lvl != null)
            {
                LevelName = lvl.Name;
                LevelElevation = lvl.Elevation;
            }

            LocationEndPt0 = new double[3];
            LocationEndPt1 = new double[3];
            Curve locCurve = ((LocationCurve)line.Location).Curve;
            XYZ endPt0 = locCurve.GetEndPoint(0);
            XYZ endPt1 = locCurve.GetEndPoint(1);
            LocationEndPt0[0] = endPt0.X; LocationEndPt0[1] = endPt0.Y; LocationEndPt0[2] = endPt0.Z;
            LocationEndPt1[0] = endPt1.X; LocationEndPt1[1] = endPt1.Y; LocationEndPt1[2] = endPt1.Z;
        }

        internal override string ObjectTypeName { get { return @"DetailCurve"; } }

        internal override bool isMatching(RevitObjectModelState other)
        {
            if (other is RevitDetailCurveModelState)
            {
                RevitDetailCurveModelState otherAsDetailCurve = other as RevitDetailCurveModelState;
                if (   LineStyleName == otherAsDetailCurve.LineStyleName
                    && Math.Abs(LocationEndPt0[0] - otherAsDetailCurve.LocationEndPt0[0]) < 0.0001
                    && Math.Abs(LocationEndPt0[1] - otherAsDetailCurve.LocationEndPt0[1]) < 0.0001
                    && Math.Abs(LocationEndPt0[2] - otherAsDetailCurve.LocationEndPt0[2]) < 0.0001
                    && Math.Abs(LocationEndPt1[0] - otherAsDetailCurve.LocationEndPt1[0]) < 0.0001
                    && Math.Abs(LocationEndPt1[1] - otherAsDetailCurve.LocationEndPt1[1]) < 0.0001
                    && Math.Abs(LocationEndPt1[2] - otherAsDetailCurve.LocationEndPt1[2]) < 0.0001)
                { return true; }
            }
            return false;
        }
    }

    public class RevitFamilyInstanceModelState : RevitObjectModelState
    {
        public string TypeName;
        public string LevelName;
        public double LevelElevation;
        public double[] Location;
        public double[] LocationCurveSecondEndpoint;
        public double Rotation;
        public bool IsFacingFlipped;
        public bool IsHandFlipped;
        public double Width;
        public double Height;
        public double SillHeight;

        public RevitFamilyInstanceModelState()
        {
            TypeName = @"";
            LevelName = @"";
            LevelElevation = 0.0;
            Location = new double[3];
            LocationCurveSecondEndpoint = new double[3];
            for (int j = 0; j < 3; j++)
            {
                Location[j] = 0.0;
                LocationCurveSecondEndpoint[j] = 0.0;
            }
            Rotation = 0.0;
            IsFacingFlipped = false;
            IsHandFlipped = false;
            Width = 0.0;
            Height = 0.0;
            SillHeight = 0.0;
        }

        internal RevitFamilyInstanceModelState(Document doc, FamilyInstance inst)
        {
            FamilySymbol sym = doc.GetElement(inst.GetTypeId()) as FamilySymbol;
            TypeName = sym.Category.Name + @":" + sym.Family.Name + @":" + sym.Name;
            Level lvl = doc.GetElement(inst.LevelId) as Level;
            if (lvl != null)
            {
                LevelName = lvl.Name;
                LevelElevation = lvl.Elevation;
            }

            Location = new double[3];
            LocationCurveSecondEndpoint = new double[3];
            if (inst.Location is LocationPoint)
            {
                LocationPoint locPt = (LocationPoint)inst.Location;
                for (int j = 0; j < 3; j++)
                {
                    Location[j] = locPt.Point[j];
                    LocationCurveSecondEndpoint[j] = 0.0;
                }
                Rotation = locPt.Rotation;
                while (Rotation >= ((Math.PI * 2.0) - 0.000001)) { Rotation -= Math.PI * 2.0; }
            }
            else if (inst.Location is LocationCurve)
            {
                Curve locCurve = (inst.Location as LocationCurve).Curve;
                XYZ locEndPt = locCurve.GetEndPoint(0);
                for (int j = 0; j < 3; j++)
                { Location[j] = locEndPt[j]; }
                locEndPt = locCurve.GetEndPoint(1);
                for (int j = 0; j < 3; j++)
                { LocationCurveSecondEndpoint[j] = locEndPt[j]; }
            }
            IsFacingFlipped = inst.FacingFlipped;
            IsHandFlipped = inst.HandFlipped;

            Parameter p = sym.LookupParameter(@"Width");
            Width = (p != null) ? p.AsDouble() : 0.0;
            p = sym.LookupParameter(@"Height");
            Height = (p != null) ? p.AsDouble() : 0.0;
            p = inst.LookupParameter(@"Sill Height");
            if (p != null)
            {
                SillHeight = p.AsDouble();
            }
            else
            {
                p = inst.LookupParameter(@"Elevation");
                SillHeight = (p != null) ? p.AsDouble() : 0.0;
            }
        }

        internal override string ObjectTypeName { get { return @"Family Instance"; } }

        internal override bool isMatching(RevitObjectModelState refState)
        {
            if (refState is RevitFamilyInstanceModelState)
            {
                RevitFamilyInstanceModelState refStateAsInst = refState as RevitFamilyInstanceModelState;
                if (TypeName == refStateAsInst.TypeName)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        if (   Math.Abs(Location[j] - refStateAsInst.Location[j]) > 0.0001
                            || Math.Abs(LocationCurveSecondEndpoint[j] - refStateAsInst.LocationCurveSecondEndpoint[j]) > 0.0001)
                        { return false; }
                    }

                }
                else { return false; }

                return true;
            }
            else { return false; }
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class ModelStateCompareAttribute : Attribute
    {
        public enum DirectiveEnum { None, Ignore };
        
        public ModelStateCompareAttribute(DirectiveEnum directive)
        { m_directive = directive; }

        public DirectiveEnum Directive  { get { return m_directive; } set { m_directive = value; } }

        private DirectiveEnum m_directive;
    }
}
