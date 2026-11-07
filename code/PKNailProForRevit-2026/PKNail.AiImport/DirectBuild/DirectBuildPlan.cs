using PKNail.AiImport.Mapping;

namespace PKNail.AiImport.DirectBuild;

// Epic 13 — Direct Build. The Revit-free, fully-resolved description of what to draw: every wall at its
// final (re-based, cleaned) plan coordinates and every opening at its final centre along its host wall.
// The Revit bridge (DirectBuildImporter) only converts meters → Distance and calls the Revit API; it makes
// no geometric decisions. Lengths are METERS here, exactly like the Mapped* layer.

/// <summary>A plan point or plan direction, meters (or unitless for a direction).</summary>
public readonly record struct PlanPoint(double X, double Y)
{
    public static PlanPoint operator +(PlanPoint a, PlanPoint b) => new(a.X + b.X, a.Y + b.Y);
    public static PlanPoint operator -(PlanPoint a, PlanPoint b) => new(a.X - b.X, a.Y - b.Y);
    public static PlanPoint operator *(PlanPoint a, double k) => new(a.X * k, a.Y * k);
    public double Dot(PlanPoint b) => (X * b.X) + (Y * b.Y);
    public double Length => Math.Sqrt((X * X) + (Y * Y));
    public double DistanceTo(PlanPoint b) => (this - b).Length;
    public PlanPoint Normalized() { double l = Length; return l > 0 ? new(X / l, Y / l) : this; }
}

/// <summary>The complete build plan for one AI-engine file.</summary>
/// <param name="Levels">Levels lowest-first, each with the walls to draw on it.</param>
/// <param name="RebaseXMeters">Subtracted from every source X so the building lands near the project origin (Epic 10.1).</param>
/// <param name="RebaseYMeters">Subtracted from every source Y.</param>
/// <param name="RebaseZMeters">Subtracted from every level elevation so the lowest level sits at 0.</param>
/// <param name="Adjustments">Every geometric change made to the AI data (squared walls, closed gaps, shifted openings). Never silent.</param>
/// <param name="Warnings">Things the reviewer should check that were drawn anyway (overlaps, assumptions).</param>
/// <param name="Skipped">Elements that cannot be drawn, each with the reason.</param>
public sealed record DirectBuildPlan(
    IReadOnlyList<PlannedLevel> Levels,
    double RebaseXMeters,
    double RebaseYMeters,
    double RebaseZMeters,
    IReadOnlyList<string> Adjustments,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Skipped)
{
    public int WallCount => Levels.Sum(l => l.Walls.Count);
    public int OpeningCount => Levels.Sum(l => l.Walls.Sum(w => w.Openings.Count));
}

/// <summary>A level to match (by elevation) or create, plus the PKNAIL level data to store on it.</summary>
public sealed record PlannedLevel(
    int SourceIndex,
    string Name,
    double ElevationMeters,
    double Floor2FloorMeters,
    double Floor2CeilingMeters,
    bool HasDropCeiling,
    double DropCeilingHeightMeters,
    double TypicalWindowSillMeters,
    double TypicalWindowHeadMeters,
    IReadOnlyList<PlannedWall> Walls);

/// <summary>A straight wall drawn on its centerline from <see cref="Start"/> to <see cref="End"/>.</summary>
/// <param name="Label">The AI wall id, or <c>W{n}</c> (1-based source order). Written to the Revit Mark.</param>
/// <param name="InteriorNormal">Unit plan vector pointing to the wall's interior side: the AI's
/// <c>InteriorSide</c> when given, else toward the level's plan centroid (a heuristic for partitions).
/// Used to orient doors and windows.</param>
public sealed record PlannedWall(
    int SourceIndex,
    string Label,
    PlanPoint Start,
    PlanPoint End,
    double ThicknessMeters,
    double HeightMeters,
    double BaseOffsetMeters,
    PlanPoint InteriorNormal,
    string? Comment,
    IReadOnlyList<PlannedOpening> Openings)
{
    public double LengthMeters => Start.DistanceTo(End);
}

/// <summary>A door or window hosted on its wall, centred <see cref="CentreFromStartMeters"/> from the wall start.</summary>
/// <param name="Hinge">Door hinge side, or <c>null</c> when the AI did not provide it (Revit default kept).</param>
/// <param name="Swing">Door swing, or <c>null</c> when the AI did not provide it (Revit default kept).</param>
public sealed record PlannedOpening(
    int SourceIndex,
    string Label,
    OpeningKind Kind,
    double CentreFromStartMeters,
    double WidthMeters,
    double HeightMeters,
    double SillMeters,
    HingeSideKind? Hinge,
    SwingDirectionKind? Swing,
    string? OpeningType,
    string? Comment)
{
    /// <summary>The AI's <c>FamilyDefIdx</c> (0 when absent): index into the project's PKNAIL survey-family
    /// list. Used to choose the family when <see cref="OpeningType"/> does not name one.</summary>
    public int FamilyDefIdx { get; init; }
}
