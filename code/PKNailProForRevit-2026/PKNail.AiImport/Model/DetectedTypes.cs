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

    /// <summary>
    /// Finished floor-to-ceiling height, <b>meters</b> — despite the JSON key lacking the
    /// <c>Meters</c> suffix every other field has (contract inconsistency, Jira plan §5 item 3).
    /// </summary>
    [JsonPropertyName("Floor2CeilingElev")]
    public double Floor2CeilingMeters { get; set; }

    public bool HasDropCeiling { get; set; }

    /// <summary>Floor-to-drop-ceiling height, meters; only meaningful when <see cref="HasDropCeiling"/>.</summary>
    public double DropCeilingHeightMeters { get; set; }

    public double TypicalWindowSillHeightMeters { get; set; }
    public double TypicalWindowHeadHeightMeters { get; set; }

    public List<DetectedWall> Walls { get; set; } = new();
}

public sealed class DetectedWall
{
    /// <summary>Wall face length near-end → far-end, meters.</summary>
    public double LengthMeters { get; set; }

    /// <summary>Wall thickness near-face → far-face, meters.</summary>
    public double ThicknessMeters { get; set; }

    /// <summary>
    /// Corner angle relative to the previous wall, radians. <b>Unreliable in v1</b> (Jira plan §5 item 1 /
    /// Appendix A): mostly 0, one 90°, one 270°, and meaningless on the first wall. The mapping layer
    /// snaps/handles it per Q2.
    /// </summary>
    public double CornerAngleRad { get; set; }

    /// <summary>Elevation delta of this face above the floor datum, <b>meters</b> (JSON key "LevelOffset", no suffix).</summary>
    [JsonPropertyName("LevelOffset")]
    public double LevelOffsetMeters { get; set; }

    public List<DetectedOpening> Openings { get; set; } = new();
}

public sealed class DetectedOpening
{
    /// <summary>Distance from the wall near-end to the opening <b>centre</b>, meters.</summary>
    public double LocationFromNearEndMeters { get; set; }

    public double WidthMeters { get; set; }
    public double HeightMeters { get; set; }

    /// <summary>Height of the opening bottom above finished floor, meters.</summary>
    public double SillHeightMeters { get; set; }

    /// <summary><c>true</c> ⇒ door, <c>false</c> ⇒ window (routing in the mapping/bridge layer).</summary>
    public bool IsLikelyDoor { get; set; }

    /// <summary>Door only — HingeSide: 0=Left, 1=Right.</summary>
    public int HingeSideIndex { get; set; }

    /// <summary>Door only — SwingDirection: 0=Out, 1=In.</summary>
    public int SwingDirectionIndex { get; set; }

    /// <summary>Window only — SurveyorRelation: 0=Interior, 1=Exterior.</summary>
    public int FacingOrientationIndex { get; set; }

    /// <summary>Index into the project's family-def list. v1 always emits 0 (Jira plan §5 item 4 / Q3).</summary>
    public int FamilyDefIdx { get; set; }
}
