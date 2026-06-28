namespace PKNail.AiImport.Mapping;

// Revit-free enums that mirror the PKNailForRevit plugin enums *by integer value*, so the Epic 3
// bridge can translate with a trivial cast. Keep these in sync with IProjectModel.cs / SurveyFamilyDef.

/// <summary>Mirrors <c>SurveyOrientation</c> (NotFlipped=0, Flipped=1).</summary>
public enum SurveyOrientationKind { NotFlipped = 0, Flipped = 1 }

/// <summary>Mirrors <c>CompassDirection</c> (Undefined=0, N=1, E=2, S=3, W=4).</summary>
public enum CompassDirectionKind { Undefined = 0, North = 1, East = 2, South = 3, West = 4 }

/// <summary>Door vs window routing (from <c>DetectedOpening.IsLikelyDoor</c>).</summary>
public enum OpeningKind { Window = 0, Door = 1 }

/// <summary>Door only — mirrors <c>SurveyFamilyDef.HingeSide</c> (Left=0, Right=1).</summary>
public enum HingeSideKind { Left = 0, Right = 1 }

/// <summary>Door only — mirrors <c>SurveyFamilyDef.SwingDirection</c> (Out=0, In=1).</summary>
public enum SwingDirectionKind { Out = 0, In = 1 }

/// <summary>Window only — mirrors <c>SurveyFamilyDef.SurveyorRelation</c> (Interior=0, Exterior=1).</summary>
public enum FacingRelationKind { Interior = 0, Exterior = 1 }

/// <summary>
/// Normalized, Revit-free result of mapping a <c>DetectedBuilding</c>: semantics resolved and corner
/// angles snapped to architectural intent. <b>Lengths remain in meters</b> — the Epic 3 bridge performs
/// the authoritative meters→<c>Distance</c> (feet) conversion and applies <c>DocumentSettings</c> rounding.
/// </summary>
public sealed record MappedBuilding(
    SurveyOrientationKind Orientation,
    CompassDirectionKind FirstWallCompass,
    IReadOnlyList<MappedLevel> Levels);

public sealed record MappedLevel(
    double Floor2FloorMeters,
    double Floor2CeilingMeters,
    bool HasDropCeiling,
    double DropCeilingHeightMeters,
    double TypicalWindowSillMeters,
    double TypicalWindowHeadMeters,
    IReadOnlyList<MappedWall> Walls);

public sealed record MappedWall(
    int Index,
    bool IsFirstWall,
    double LengthMeters,
    double ThicknessMeters,
    double CornerAngleRad,
    bool CornerAngleSnapped,
    double LevelOffsetMeters,
    IReadOnlyList<MappedOpening> Openings);

public sealed record MappedOpening(
    double LocationFromNearEndMeters,
    double WidthMeters,
    double HeightMeters,
    double SillHeightMeters,
    OpeningKind Kind,
    HingeSideKind HingeSide,
    SwingDirectionKind SwingDirection,
    FacingRelationKind Facing,
    int FamilyDefIdx);
