namespace PKNail.AiImport.Mapping;

/// <summary>
/// Tunables for the architectural-intent normalization in <see cref="AiModelMapper"/>.
/// Length rounding is NOT here — it uses Revit-side <c>DocumentSettings</c> and is applied in Epic 3.
/// </summary>
public sealed class MappingOptions
{
    /// <summary>
    /// A corner angle within this tolerance of a right-angle multiple (0, 90°, 180°, 270°) is snapped
    /// to it (brief §3 "snap near-orthogonal walls to 90°"). Default 5°.
    /// </summary>
    public double OrthogonalSnapToleranceRad { get; init; } = 5.0 * Math.PI / 180.0;

    /// <summary>When false, corner angles are only normalized to [0, 2π); no orthogonal snapping.</summary>
    public bool SnapToOrthogonal { get; init; } = true;

    public static MappingOptions Default { get; } = new();
}
