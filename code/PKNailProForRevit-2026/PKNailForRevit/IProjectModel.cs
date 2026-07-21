using System;
using System.Collections.Generic;
using System.Text;

namespace PKNailForRevit
{
    public interface IProjectModel
    {
        CommandResult Initialize(ref string msg);
        CommandResult Finalize(ref string msg);

        // Ensures the PKNail shared/project parameters exist in the document, binding any that are
        // missing (so the add-in works on documents not created from a PKNail template).
        CommandResult EnsureDocumentParameters(ref string msg);

        CommandResult InitializeTransaction(ref string msg);
        CommandResult SetTransactionName(string name, ref string msg);
        CommandResult StartTransaction(ref string msg);
        CommandResult CommitTransaction(ref string msg);
        CommandResult RollBackTransaction(ref string msg);

        CommandResult SetCoordinateSystemFromWall();

        CommandResult ActivateWallPrep(out CommandContext cmdContext, out WallSurveyParams wsParams, ref string msg);
        CommandResult ActivateWall(CommandContext cmdContext, WallSurveyParams wsParams, ref string msg);

        CommandResult SurveyWallPrep(SurveyDirection dir, out CommandContext cmdContext,
                                     out WallSurveyParams wsParams, ref string msg);
        CommandResult SurveyWall(CommandContext cmdContext, WallSurveyParams wsParams, ref string msg);

        // Direct absolute wall placement for the v2 AI contract (independent walls at real plan
        // coordinates, NOT a connected survey chain). Coordinates are in METERS; the implementation
        // converts to Revit internal feet. Must run inside a transaction group already opened by the
        // caller (the importer) — the implementation opens/commits only its own inner transaction.
        CommandResult CreateWallAbsolute(double startXMeters, double startYMeters,
                                         double endXMeters, double endYMeters,
                                         Distance thickness, Distance height, int levelIdx,
                                         out long wallElementId, ref string msg);

        // Direct opening (window/door) hosting on an absolute-placed wall for the v2 AI contract. The
        // opening is placed at <paramref name="locationFromStartMeters"/> measured from the host wall's
        // start point, along its location line. Dimensions are Distances (feet internally). Must run inside
        // a transaction group already opened by the caller — the implementation opens/commits only its own
        // inner transaction (like CreateWallAbsolute).
        CommandResult CreateOpeningOnWall(long wallElementId, double locationFromStartMeters,
                                          Distance width, Distance height, Distance sillHeight,
                                          bool isDoor, out long openingElementId, ref string msg);

        CommandResult SurveyLinePrep(SurveyDirection dir, bool isParallel, out CommandContext cmdContext,
                                     out LineSurveyParams lsParams, ref string msg);
        CommandResult SurveyLine(CommandContext cmdContext, LineSurveyParams lsParams, ref string msg);

        CommandResult SurveyWallHostedObjectPrep(SurveyDirection dir, string familyCategory,
                                                 out CommandContext cmdContext,
                                                 out WallHostedObjectSurveyParams whoParams,
                                                 ref string msg);
        CommandResult SurveyRFCommandMappedObjectPrep(string rfCmdPage, KeypadCommandPhrase rfCmdPhrase,
                                                      out CommandContext cmdContext,
                                                      out SurveyParams sParams,
                                                      ref string msg);
        CommandResult SurveyWallHostedObject(CommandContext cmdContext, WallHostedObjectSurveyParams whosParams, ref string msg);

        CommandResult CopyObjectsPrep(List<string> selectionIDs, out CopyObjectsData data, ref string msg);
        CommandResult CopyObjects(CopyObjectsData data, ref string msg);

        CommandResult ConvertUnknownWidthWallsPrep(List<string> selectionIDs, out ConvertUnknownWidthWallsData data, ref string msg);
        CommandResult ConvertUnknownWidthWalls(ConvertUnknownWidthWallsData data, ref string msg);

        void GetModelState(out ModelState state);

        DocumentSettings DocumentSettings { get; set; }
        LevelsData LevelsData { get; set; }

        CommandResult Save(out SaveError err, ref string msg);

        UnitSystem UnitSystem { get; }

        List<string> GetRangefinderKeypadMapPages();

        List<string> GetBuiltinFamilyCategories();
        List<string> GetCustomFamilyCategories();

        System.Threading.AutoResetEvent TransactionErrorSignal { set; }
    }

    public enum CommandResult { Succeeded, Failed, Cancelled, TransactionError }

    public enum SaveError { None, NoPathSet, ReadOnlyFile, InvalidOperation, Other }

    public abstract class ModelState : JsonSerializable
    {
        public abstract bool Compare(ModelState other, out string diffMsg);
    }

    public static class FamilyCategory
    {
        public static string Walls = @"Walls";
        public static string WallsAlt = @"Wall";
        public static string Doors = @"Doors";
        public static string DoorsAlt = @"Door";
        public static string Windows = @"Windows";
        public static string WindowsAlt = @"Window";
        public static string Lines = @"Lines";
        public static string LinesAlt = @"Line";

        public static bool IsWalls(this string value)
        { return isMatchBuiltinFamilyCategory(value, Walls); }

        public static bool IsDoors(this string value)
        { return isMatchBuiltinFamilyCategory(value, Doors); }

        public static bool IsWindows(this string value)
        { return isMatchBuiltinFamilyCategory(value, Windows); }

        public static bool IsLines(this string value)
        { return isMatchBuiltinFamilyCategory(value, Lines); }

        public static bool IsBuiltinFamilyCategory(this string value)
        { return (IsWalls(value) || IsDoors(value) || IsWindows(value) || IsLines(value)); }

        public static string ToBuiltinFamilyCategory(this string value)
        {
            if (IsWalls(value)) { return Walls; }
            else if (IsDoors(value)) { return Doors; }
            else if (IsWindows(value)) { return Windows; }
            else if (IsLines(value)) { return Lines; }
            else { return value; }
        }

        public static List<string> GetBuiltinFamilyCategories()
        {
            List<string> retval = new List<string>();
            retval.Add(Walls);
            retval.Add(Lines);
            retval.Add(Doors);
            retval.Add(Windows);
            return retval;
        }

        private static bool isMatchBuiltinFamilyCategory(string value, string matchingBuiltinFamilyCategory)
        {
            return (   value == matchingBuiltinFamilyCategory
                    || value == (matchingBuiltinFamilyCategory.Substring(0, matchingBuiltinFamilyCategory.Length - 1)));
        }
    }

    public enum UnitSystem  { Imperial, Metric };

    public class CommandContext
    {
        public List<string> ProjectLevelNames;
        public int CurrentLevelIdx;
        public SurveyDirection SurveyDirection;
        public SurveyOrientation SurveyOrientation;
        public UnitSystem DisplayUnitSystem;
        public DocumentSettings ModelSettings;
        public List<string> ZoomElements;

        public CommandContext()
        {
            ProjectLevelNames = new List<string>();
            ZoomElements = new List<string>();
        }
    }

    public class SurveyParams
    {
        public string FamilyCategory;
        public SurveyParams(string familyCategory)
        { FamilyCategory = familyCategory; }
    }

    public class ReferenceOpeningParams
    {
        public string ReferenceOpeningID;
        public Distance CornerToOpening;
        public Distance ContinuationCornerToOpening;
        public Distance WallExtent;
        public Distance ContinuationWallExtent;
        public Distance LevelOffset;

        public ReferenceOpeningParams()
        {
            ReferenceOpeningID = @"";
            CornerToOpening = Distance.NoValue;
            ContinuationCornerToOpening = Distance.NoValue;
            WallExtent = Distance.NoValue;
            ContinuationWallExtent = Distance.NoValue;
            LevelOffset = Distance.NoValue;
        }
    }

    public class WallSurveyParams : SurveyParams
    {
        public bool IsSurveyRequired;
        public bool IsCreateWall;
        public bool IsFirstWall;
        public bool IsCloseWall;
        public Distance Length;
        public Distance Width;
        public Distance Height;
        public Distance LevelOffset;
        public bool IsUnknownWidth;
        public List<Geometry2D.Radians> CornerAngleList;
        public List<Distance> MaxLengthList;
        public List<Distance> ProjectWallWidthList;
        public int ConnectedLevelIdx;
        public List<Distance> ProjectLevelVisibleHeightList;
        public Geometry2D.Radians CornerAngle;
        public Distance CloseLength;
        public Geometry2D.Radians CloseCornerAngle;
        public Distance ReferenceFaceLength;
        public ReferenceOpeningParams ReferenceOpeningParams;

        // This field is used by the first wall dialog
        public CompassDirection SurveyorCompassDirection;

        public WallSurveyParams() : base(PKNailForRevit.FamilyCategory.Walls)
        {
            Length = Distance.NoValue;
            Width = Distance.NoValue;
            Height = Distance.NoValue;
            LevelOffset = Distance.NoValue;
            CornerAngleList = new List<Geometry2D.Radians>();
            MaxLengthList = new List<Distance>();
            ProjectWallWidthList = new List<Distance>();
            ProjectLevelVisibleHeightList = new List<Distance>();
            CornerAngle = Geometry2D.Radians.Zero;
            CloseLength = Distance.NoValue;
            CloseCornerAngle = Geometry2D.Radians.Zero;
            ReferenceFaceLength = Distance.NoValue;
            ReferenceOpeningParams = null;
        }
    }

    public class SurveyFamilyDef
    {
        [Flags]
        public enum FixedDimensions
        {
            None = 0,
            Width = 1,
            Height = 2,
            Depth = 4,
            SillHeight = 8
        }

        public enum HingeSide { Left = 0, Right = 1 }
        public enum SwingDirection { Out = 0, In = 1 }
        public enum SlideDirection { RightToLeft = 0, LeftToRight = 1 };
        public enum SurveyorRelation { Interior = 0, Exterior = 1 }

        public enum LateralOrientationType { None = 0, Hinged = 1, Sliding = 2 };
        public enum FacingOrientationType { None = 0, Swinging = 1, Facing = 2 };

        public string Name;

        public Distance Width;
        public Distance Height;
        public Distance Depth;
        public Distance SillHeight;

        public FixedDimensions FixedDimensionsVal;

        public LateralOrientationType LateralOrientationTypeVal;
        public FacingOrientationType FacingOrientationTypeVal;

        public int LateralOrientation;
        public int FacingOrientation;

        public bool IsMeasureLocationCentered;

        public SurveyFamilyDef()
        {
            Width = Distance.NoValue;
            Height = Distance.NoValue;
            Depth = Distance.NoValue;
            SillHeight = Distance.NoValue;
            FixedDimensionsVal = FixedDimensions.None;
            LateralOrientationTypeVal = LateralOrientationType.None;
            FacingOrientationTypeVal = FacingOrientationType.None;
            LateralOrientation = 0;
            FacingOrientation = 0;
            IsMeasureLocationCentered = false;
        }
    }

    public class WallHostedObjectSurveyParams : SurveyParams
    {
        public Distance Location;
        public Distance HostWallHeight;
        public Distance HostWallLength;
        public Distance TypicalHeight;
        public Distance TypicalSillHeight;
        public bool IsTypicalWindowHeights;
        public bool IsStackedWindows;

        public List<SurveyFamilyDef> ProjectSurveyFamilyDefs;
        public int SurveyFamilyDefIdx;

        public WallHostedObjectSurveyParams() : base(@"")
        {
            construct();
        }

        public WallHostedObjectSurveyParams(string familyCategory) : base(familyCategory)
        {
            construct();
        }

        internal WallHostedObjectSurveyParams(WallHostedObjectSurveyParams from) : base(from.FamilyCategory)
        {
            Location = from.Location;
            HostWallHeight = from.HostWallHeight;
            TypicalHeight = from.TypicalHeight;
            TypicalSillHeight = from.TypicalSillHeight;
            IsTypicalWindowHeights = from.IsTypicalWindowHeights;
            IsStackedWindows = from.IsStackedWindows;

            ProjectSurveyFamilyDefs = new List<SurveyFamilyDef>(from.ProjectSurveyFamilyDefs);
            SurveyFamilyDefIdx = from.SurveyFamilyDefIdx;
        }

        internal Distance GetWidth()
        {
            SurveyFamilyDef def = getSelectedFamilyDef();
            return ((def != null) ? def.Width : Distance.NoValue); 
        }

        internal void SetWidth(Distance w)
        {
            SurveyFamilyDef def = getSelectedFamilyDef();
            if (def != null)
            { def.Width = w; }
            else
            { throw new HardException(@"No valid SurveyFamilyTypeDef"); }
        }

        internal Distance GetHeight()
        {
            SurveyFamilyDef def = getSelectedFamilyDef();
            return ((def != null) ? def.Height : Distance.NoValue);
        }

        internal void SetHeight(Distance h)
        {
            SurveyFamilyDef def = getSelectedFamilyDef();
            if (def != null)
            { def.Height = h; }
            else
            { throw new HardException(@"No valid SurveyFamilyTypeDef"); }
        }

        internal Distance GetDepth()
        {
            SurveyFamilyDef def = getSelectedFamilyDef();
            return ((def != null) ? def.Depth : Distance.NoValue);
        }

        internal void SetDepth(Distance d)
        {
            SurveyFamilyDef def = getSelectedFamilyDef();
            if (def != null)
            { def.Depth = d; }
            else
            { throw new HardException(@"No valid SurveyFamilyTypeDef"); }
        }

        internal Distance GetSillHeight()
        {
            SurveyFamilyDef def = getSelectedFamilyDef();
            return ((def != null) ? def.SillHeight : Distance.NoValue);
        }

        internal void SetSillHeight(Distance h)
        {
            SurveyFamilyDef def = getSelectedFamilyDef();
            if (def != null)
            { def.SillHeight = h; }
            else
            { throw new HardException(@"No valid SurveyFamilyTypeDef"); }
        }

        internal SurveyFamilyDef.FixedDimensions GetFixedDimensions()
        {
            SurveyFamilyDef def = getSelectedFamilyDef();
            return ((def != null) ? def.FixedDimensionsVal : SurveyFamilyDef.FixedDimensions.None);
        }

        internal bool GetIsMeasureLocationCentered()
        {
            SurveyFamilyDef def = getSelectedFamilyDef();
            return ((def != null) ? def.IsMeasureLocationCentered : false);
        }

        internal SurveyFamilyDef.LateralOrientationType GetLateralOrientationTypeVal()
        {
            SurveyFamilyDef def = getSelectedFamilyDef();
            return ((def != null) ? def.LateralOrientationTypeVal : SurveyFamilyDef.LateralOrientationType.None);
        }

        internal int GetLateralOrientation()
        {
            SurveyFamilyDef def = getSelectedFamilyDef();
            return ((def != null) ? def.LateralOrientation : 0);
        }

        internal void SetLateralOrientation(int lo)
        {
            SurveyFamilyDef def = getSelectedFamilyDef();
            if (def != null)
            { def.LateralOrientation = lo; }
            else
            { throw new HardException(@"No valid SurveyFamilyTypeDef"); }
        }

        internal SurveyFamilyDef.FacingOrientationType GetFacingOrientationTypeVal()
        {
            SurveyFamilyDef def = getSelectedFamilyDef();
            return ((def != null) ? def.FacingOrientationTypeVal : SurveyFamilyDef.FacingOrientationType.None);
        }

        internal int GetFacingOrientation()
        {
            SurveyFamilyDef def = getSelectedFamilyDef();
            return ((def != null) ? def.FacingOrientation : 0);
        }

        internal void SetFacingOrientation(int fo)
        {
            SurveyFamilyDef def = getSelectedFamilyDef();
            if (def != null)
            { def.FacingOrientation = fo; }
            else
            { throw new HardException(@"No valid SurveyFamilyTypeDef"); }
        }

        private SurveyFamilyDef getSelectedFamilyDef()
        {
            if (SurveyFamilyDefIdx >= 0 && SurveyFamilyDefIdx < ProjectSurveyFamilyDefs.Count)
            { return ProjectSurveyFamilyDefs[SurveyFamilyDefIdx]; }
            else
            { return null; }
        }

        private void construct()
        {
            Location = Distance.NoValue;
            HostWallHeight = Distance.NoValue;
            HostWallLength = Distance.NoValue;
            TypicalHeight = Distance.NoValue;
            TypicalSillHeight = Distance.NoValue;
            IsTypicalWindowHeights = false;
            IsStackedWindows = false;

            ProjectSurveyFamilyDefs = new List<SurveyFamilyDef>();
            SurveyFamilyDefIdx = 0;  // Default is first type in order            
        }
    }

    public enum CopyObjectsIncludeScope { Undefined = -1, None = 0, Selected = 1, All = 2 };
    public enum CopyObjectsType { Walls = 0, Doors = 1, Windows = 2, OtherHostedObjects = 3 };

    public class CopyObjectsData
    {
        public CommandContext CommandContext;

        public List<string> Selection;
        public int LowerLevelIdx;
        public int UpperLevelIdx;
        public CopyObjectsIncludeScope[] IncludedObjects;
        public bool[] IsObjectInSelection;

        public CopyObjectsData(CommandContext context)
        {
            CommandContext = context;
            Selection = new List<string>();
            int objTypeCount = Enum.GetValues(typeof(CopyObjectsType)).Length;
            IncludedObjects = new CopyObjectsIncludeScope[objTypeCount];
            for (int i = 0; i < objTypeCount; i++) { IncludedObjects[i] = CopyObjectsIncludeScope.Undefined; }
            IsObjectInSelection = new bool[objTypeCount];
            for (int i = 0; i < objTypeCount; i++) { IsObjectInSelection[i] = false; }
        }
    }

    public enum ConvertUnknownWidthWallsScope  { Undefined = -1, Selected = 0, AllOnCurrentLevel = 1, All = 2 };

    public class ConvertUnknownWidthWallsData
    {
        public CommandContext CommandContext;
        public List<string> Selection;
        public ConvertUnknownWidthWallsScope Scope;

        public ConvertUnknownWidthWallsData(CommandContext context)
        {
            CommandContext = context;
            Selection = new List<string>();
            Scope = ConvertUnknownWidthWallsScope.Undefined;
        }
    }

    public class LineSurveyParams : SurveyParams
    {
        public bool IsSurveyRequired;
        public bool IsParallel;
        public bool IsReferenceFaceWall;
        public List<string> LineStyles;
        public int LineStyleIdx;
        public Distance Location;
        public Distance Length;
        public Geometry2D.Radians CornerAngle;
        public List<Geometry2D.Radians> CornerAngleList;
        public List<Distance> MaxLengthList;
        public Distance ReferenceFaceLength;

        public LineSurveyParams() : base(PKNailForRevit.FamilyCategory.Lines)
        {
            IsParallel = false;
            IsReferenceFaceWall = true;
            LineStyles = new List<string>();
            LineStyleIdx = 0;
            Location = Distance.NoValue;
            Length = Distance.NoValue;
            CornerAngleList = new List<Geometry2D.Radians>();
            MaxLengthList = new List<Distance>();
            ReferenceFaceLength = Distance.NoValue;
        }
    }

    public enum CompassDirection { Undefined = 0, North = 1, East = 2, South = 3, West = 4 };

    public class SurveySide
    {
        public static SurveySide Left = new SurveySide(0);
        public static SurveySide Right = new SurveySide(1);

        private int m_val;
        private SurveySide(int val) { m_val = val; }

        public SurveySide Other
        {
            get
            {
                if (m_val == 0) { return new SurveySide(1); }
                else { return new SurveySide(0); }
            }
        }

        public static bool operator ==(SurveySide lhs, SurveySide rhs)
        {
            if (System.Object.ReferenceEquals(lhs, rhs)) { return true; }
            if ((object)lhs == null || (object)rhs == null) { return false; }
            return (lhs.m_val == rhs.m_val);
        }

        public static bool operator !=(SurveySide lhs, SurveySide rhs)
        { return !(lhs == rhs); }

        public int Index { get { return m_val; } }

        public static SurveySide FromIndex(int i)
        { return new SurveySide(i); }

        public override bool Equals(Object obj)
        { return obj is Distance && this == (SurveySide)obj; }
        public override int GetHashCode()
        { return m_val.GetHashCode(); }
    };

    public class SurveyDirection
    {
        public static SurveyDirection LeftToRight = new SurveyDirection(0);
        public static SurveyDirection RightToLeft = new SurveyDirection(1);
        public static SurveyDirection FromModel = new SurveyDirection(2);

        private int m_val;

        private SurveyDirection() { }
        private SurveyDirection(int val) { m_val = val; }

        public static Json.ExporterFunc<SurveyDirection> JsonExporter = delegate(SurveyDirection d, Json.JsonWriter w)
        { w.Write(d.m_val); };
        public static Json.ImporterFunc<int, SurveyDirection> JsonImporter = delegate(int i)
        { return new SurveyDirection(i); };

        public static bool operator ==(SurveyDirection lhs, SurveyDirection rhs)
        {
            if (System.Object.ReferenceEquals(lhs, rhs)) { return true; }
            if ((object)lhs == null || (object)rhs == null) { return false; }
            return (lhs.m_val == rhs.m_val);
        }

        public static bool operator !=(SurveyDirection lhs, SurveyDirection rhs)
        { return !(lhs == rhs); }

        public int Index { get { return m_val; } }

        public static SurveyDirection FromIndex(int i)
        {
            SurveyDirection retval;
            switch (i)
            {
                case 0:
                case 1:
                case 2:
                    retval = new SurveyDirection(i); break;
                default:
                    throw new ArgumentOutOfRangeException("idx");
            }
            return retval;
        }

        public SurveyDirection Opposite
        {
            get
            {
                if (m_val > 1) { throw new ArgumentException(@"No ""Opposite"" property for SurveyDirection.FromModel"); }
                return (m_val == 0) ? SurveyDirection.RightToLeft : SurveyDirection.LeftToRight;
            }
        }

        public SurveySide NearSide
        {
            get
            {
                if (m_val > 1) { throw new ArgumentException(@"No ""NearSide"" property for SurveyDirection.FromModel"); }
                return (m_val == 0) ? SurveySide.Left : SurveySide.Right;
            }
        }

        public SurveySide FarSide
        {
            get
            {
                if (m_val > 1) { throw new ArgumentException(@"No ""FarSide"" property for SurveyDirection.FromModel"); }
                return (m_val == 0) ? SurveySide.Right : SurveySide.Left;
            }
        }

        public override bool Equals(Object obj)
        { return obj is SurveyDirection && this == (SurveyDirection)obj; }
        public override int GetHashCode()
        { return m_val.GetHashCode(); }
    }

    public class SurveyOrientation
    {
        public static SurveyOrientation NotFlipped = new SurveyOrientation(0);
        public static SurveyOrientation Flipped = new SurveyOrientation(1);

        public SurveyOrientation(SurveyOrientation from) { m_val = from.m_val; }

        private int m_val;

        private SurveyOrientation() { }
        private SurveyOrientation(int val) { m_val = val; }
        
        public static Json.ExporterFunc<SurveyOrientation> JsonExporter = delegate(SurveyOrientation d, Json.JsonWriter w)
        { w.Write(d.m_val); };
        public static Json.ImporterFunc<int, SurveyOrientation> JsonImporter = delegate(int i)
        { return new SurveyOrientation(i); };

        public static bool operator ==(SurveyOrientation lhs, SurveyOrientation rhs)
        {
            if (System.Object.ReferenceEquals(lhs, rhs)) { return true; }
            if ((object)lhs == null || (object)rhs == null) { return false; }
            return (lhs.m_val == rhs.m_val);
        }

        public static bool operator !=(SurveyOrientation lhs, SurveyOrientation rhs)
        { return !(lhs == rhs); }

        public int Index { get { return m_val; } }

        public SurveyOrientation Other  { get { return (m_val == 0) ? Flipped : NotFlipped; } }

        public static SurveyOrientation FromIndex(int i)
        {
            SurveyOrientation retval;
            switch (i)
            {
                case 0:
                case 1:
                    retval = new SurveyOrientation(i); break;
                default: throw new ArgumentOutOfRangeException("idx");
            }
            return retval;
        }

        public void Flip() { m_val = (m_val == 0) ? 1 : 0; }

        public override bool Equals(Object obj)
        { return obj is SurveyOrientation && this == (SurveyOrientation)obj; }
        public override int GetHashCode()
        { return m_val.GetHashCode(); }
    }

    public class SoftException : Exception
    {
        public SoftException() { }
        public SoftException(string s) : base(s) { }
        public SoftException(string s, Exception inner) : base(s, inner) { }
    }

    public class HardException : Exception
    {
        public HardException() { }
        public HardException(string s) : base(s) { }
        public HardException(string s, Exception inner) : base(s, inner) { }
    }
}
