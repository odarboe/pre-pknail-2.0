using System.Text.Json.Serialization;

namespace PKNail.AiImport.Model;

/// <summary>
/// Root DTO mirroring <c>ai_engine_pknail_results.json</c> (the AI-engine output, contract v1).
/// <para>
/// Units are the RAW values as delivered: lengths in <b>meters</b>, angles in <b>radians</b>.
/// This layer does NO Revit work and NO unit conversion — converting meters → <c>Distance</c> (feet)
/// and snapping happens in the Epic 2 mapping layer. See <c>docs/PKNAIL_2.0_JiraPlan.md</c> §3.
/// </para>
/// </summary>
public sealed class DetectedBuilding
{
    /// <summary>Contract version. Absent in v1 (Q10) — <c>null</c> means "unversioned v1".</summary>
    public string? SchemaVersion { get; set; }

    /// <summary>True ⇒ scan taken from inside ⇒ <c>SurveyOrientation.Flipped</c> in the mapping layer.</summary>
    public bool IsScanFromInterior { get; set; }

    /// <summary>Compass of the first wall face: 0=DontKnow/Undefined, 1=N, 2=E, 3=S, 4=W.</summary>
    public int FirstWallCompassDirection { get; set; }

    public List<DetectedLevel> Levels { get; set; } = new();
}

public sealed class DetectedLevel
{
    /// <summary>Structural storey height, meters.</summary>
    public double Floor2FloorMeters { get; set; }

    private double _floor2CeilingMeters;

    /// <summary>
    /// Finished floor-to-ceiling height, <b>meters</b>. v1 spells the JSON key `Floor2CeilingElev`
    /// (no `Meters` suffix); v2 uses `Floor2CeilingMeters`. Both keys write this same value.
    /// </summary>
    [JsonPropertyName("Floor2CeilingElev")]
    public double Floor2CeilingMeters { get => _floor2CeilingMeters; set => _floor2CeilingMeters = value; }

    /// <summary>v2 alias for <see cref="Floor2CeilingMeters"/> (JSON key `Floor2CeilingMeters`).</summary>
    [JsonPropertyName("Floor2CeilingMeters")]
    public double Floor2CeilingMetersV2 { get => _floor2CeilingMeters; set => _floor2CeilingMeters = value; }

    /// <summary>v2 only: base elevation of this level (meters). Informational; the plugin re-bases.</summary>
    public double? ElevationMeters { get; set; }

    /// <summary>v2 only: whether the wall loop closes back to its start.</summary>
    public bool? IsClosed { get; set; }

    public bool HasDropCeiling { get; set; }

    /// <summary>Floor-to-drop-ceiling height, meters; only meaningful when <see cref="HasDropCeiling"/>.</summary>
    public double DropCeilingHeightMeters { get; set; }

    public double TypicalWindowSillHeightMeters { get; set; }
    public double TypicalWindowHeadHeightMeters { get; set; }

    public List<DetectedWall> Walls { get; set; } = new();
}

/// <summary>A 2D plan point in meters (v2 wall endpoints).</summary>
public sealed class DetectedPoint2D
{
    public double X { get; set; }
    public double Y { get; set; }
}

public sealed class DetectedWall
{
    /// <summary>Wall face length near-end → far-end, meters. v1 input; in v2 it is derived from endpoints.</summary>
    public double LengthMeters { get; set; }

    /// <summary>Wall thickness near-face → far-face, meters.</summary>
    public double ThicknessMeters { get; set; }

    /// <summary>
    /// Corner angle relative to the previous wall, radians. <b>Unreliable in v1</b> (Jira plan §5 item 1 /
    /// Appendix A). In v2 this is ignored — the mapper derives the corner angle from the endpoints.
    /// </summary>
    public double CornerAngleRad { get; set; }

    /// <summary>v2 only: absolute wall start point (meters), per <see cref="DetectedWall"/> LocationLine.</summary>
    public DetectedPoint2D? StartMeters { get; set; }

    /// <summary>v2 only: absolute wall end point (meters).</summary>
    public DetectedPoint2D? EndMeters { get; set; }

    /// <summary>True when this wall carries absolute endpoints (v2 contract).</summary>
    [JsonIgnore]
    public bool HasEndpoints => StartMeters != null && EndMeters != null;

    /// <summary>Elevation delta of this face above the floor datum, <b>meters</b> (JSON key "LevelOffset", no suffix).</summary>
    [JsonPropertyName("LevelOffset")]
    public double LevelOffsetMeters { get; set; }

    public List<DetectedOpening> Openings { get; set; } = new();
}

public sealed class DetectedOpening
{
    /// <summary>v1: distance from the wall near-end to the opening <b>centre</b>, meters.</summary>
    public double LocationFromNearEndMeters { get; set; }

    /// <summary>v2: distance from the wall <c>Start</c> to the opening centre, meters.</summary>
    public double? LocationFromStartMeters { get; set; }

    /// <summary>Resolved distance along the wall to the opening centre (v2 wins, else v1).</summary>
    [JsonIgnore]
    public double LocationMeters => LocationFromStartMeters ?? LocationFromNearEndMeters;

    public double WidthMeters { get; set; }
    public double HeightMeters { get; set; }

    /// <summary>Height of the opening bottom above finished floor, meters.</summary>
    public double SillHeightMeters { get; set; }

    /// <summary>v1: <c>true</c> ⇒ door, <c>false</c> ⇒ window.</summary>
    public bool IsLikelyDoor { get; set; }

    /// <summary>v2: explicit <c>"Door"</c> / <c>"Window"</c> (takes precedence over <see cref="IsLikelyDoor"/>).</summary>
    public string? Kind { get; set; }

    /// <summary>Resolved door/window decision (v2 <see cref="Kind"/> wins, else v1 <see cref="IsLikelyDoor"/>).</summary>
    [JsonIgnore]
    public bool ResolvedIsDoor =>
        Kind != null ? Kind.Equals("Door", System.StringComparison.OrdinalIgnoreCase) : IsLikelyDoor;

    /// <summary>Door only — HingeSide: 0=Left, 1=Right.</summary>
    public int HingeSideIndex { get; set; }

    /// <summary>Door only — SwingDirection: 0=Out, 1=In.</summary>
    public int SwingDirectionIndex { get; set; }

    /// <summary>Window only — SurveyorRelation: 0=Interior, 1=Exterior.</summary>
    public int FacingOrientationIndex { get; set; }

    /// <summary>Index into the project's family-def list. v1 always emits 0 (Jira plan §5 item 4 / Q3).</summary>
    public int FamilyDefIdx { get; set; }
}
