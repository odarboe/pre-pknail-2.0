namespace PKNail.AiImport.Model;

/// <summary>
/// Semantic validation of a <see cref="DetectedBuilding"/> after deserialization. Hard problems that
/// would make placement impossible/ambiguous are <see cref="ValidationSeverity.Error"/>; suspicious but
/// tolerable contract issues are <see cref="ValidationSeverity.Warning"/>. <b>Never throws.</b>
/// </summary>
public static class AiModelValidator
{
    private const double Epsilon = 1e-6;
    private const double TwoPi = Math.PI * 2.0;

    public static ValidationReport Validate(DetectedBuilding building)
    {
        var report = new ValidationReport();

        if (building.SchemaVersion is null)
            report.Warn("SchemaVersion",
                "No SchemaVersion in the contract; assuming v1. Request a version field from the AI engine (Q10).");
        else if (building.SchemaVersion != AiImportLibrary.TargetContractVersion)
            report.Warn("SchemaVersion",
                $"Contract version '{building.SchemaVersion}' is not the supported '{AiImportLibrary.TargetContractVersion}'.");

        if (building.FirstWallCompassDirection is < 0 or > 4)
            report.Error("FirstWallCompassDirection", $"Out of range (expected 0-4): {building.FirstWallCompassDirection}.");
        else if (building.FirstWallCompassDirection == 0)
            report.Warn("FirstWallCompassDirection", "DontKnow (0): no real-world orientation provided by the scan.");

        if (building.Levels.Count == 0)
            report.Error("Levels", "Model has no levels.");

        for (int li = 0; li < building.Levels.Count; li++)
            ValidateLevel(building.Levels[li], $"Levels[{li}]", report);

        return report;
    }

    private static void ValidateLevel(DetectedLevel lvl, string path, ValidationReport r)
    {
        if (lvl.Floor2FloorMeters <= Epsilon)
            r.Error($"{path}.Floor2FloorMeters", $"Must be positive: {lvl.Floor2FloorMeters}.");
        if (lvl.Floor2CeilingMeters <= Epsilon)
            r.Error($"{path}.Floor2CeilingElev", $"Must be positive: {lvl.Floor2CeilingMeters}.");
        if (lvl.Floor2CeilingMeters > lvl.Floor2FloorMeters + Epsilon)
            r.Warn($"{path}.Floor2CeilingElev",
                $"Floor-to-ceiling ({lvl.Floor2CeilingMeters}) exceeds floor-to-floor ({lvl.Floor2FloorMeters}).");
        if (lvl.HasDropCeiling && lvl.DropCeilingHeightMeters <= Epsilon)
            r.Error($"{path}.DropCeilingHeightMeters", "HasDropCeiling is true but the height is not positive.");
        if (lvl.TypicalWindowHeadHeightMeters > Epsilon
            && lvl.TypicalWindowSillHeightMeters > Epsilon
            && lvl.TypicalWindowHeadHeightMeters <= lvl.TypicalWindowSillHeightMeters)
            r.Warn($"{path}.TypicalWindowHeadHeightMeters", "Typical window head height is not above the sill height.");

        if (lvl.Walls.Count == 0)
            r.Error($"{path}.Walls", "Level has no walls.");
        for (int wi = 0; wi < lvl.Walls.Count; wi++)
            ValidateWall(lvl.Walls[wi], $"{path}.Walls[{wi}]", isFirst: wi == 0, r);
    }

    private static void ValidateWall(DetectedWall w, string path, bool isFirst, ValidationReport r)
    {
        if (w.LengthMeters <= Epsilon)
            r.Error($"{path}.LengthMeters", $"Must be positive: {w.LengthMeters}.");
        if (w.ThicknessMeters <= Epsilon)
            r.Error($"{path}.ThicknessMeters", $"Must be positive: {w.ThicknessMeters}.");
        if (w.CornerAngleRad < -Epsilon || w.CornerAngleRad >= TwoPi + Epsilon)
            r.Error($"{path}.CornerAngleRad", $"Out of range [0, 2π): {w.CornerAngleRad}.");
        if (isFirst && Math.Abs(w.CornerAngleRad) > Epsilon)
            r.Warn($"{path}.CornerAngleRad",
                $"First wall has no predecessor; corner angle {w.CornerAngleRad} is ignored (Q2).");

        for (int oi = 0; oi < w.Openings.Count; oi++)
            ValidateOpening(w.Openings[oi], w, $"{path}.Openings[{oi}]", r);
    }

    private static void ValidateOpening(DetectedOpening o, DetectedWall host, string path, ValidationReport r)
    {
        if (o.WidthMeters <= Epsilon) r.Error($"{path}.WidthMeters", $"Must be positive: {o.WidthMeters}.");
        if (o.HeightMeters <= Epsilon) r.Error($"{path}.HeightMeters", $"Must be positive: {o.HeightMeters}.");
        if (o.SillHeightMeters < -Epsilon) r.Error($"{path}.SillHeightMeters", $"Must be >= 0: {o.SillHeightMeters}.");

        if (o.LocationFromNearEndMeters < -Epsilon || o.LocationFromNearEndMeters > host.LengthMeters + Epsilon)
            r.Error($"{path}.LocationFromNearEndMeters",
                $"Opening centre {o.LocationFromNearEndMeters} is outside the wall length {host.LengthMeters}.");
        else if (o.WidthMeters > Epsilon
                 && (o.LocationFromNearEndMeters - o.WidthMeters / 2.0 < -Epsilon
                     || o.LocationFromNearEndMeters + o.WidthMeters / 2.0 > host.LengthMeters + Epsilon))
            r.Warn($"{path}.LocationFromNearEndMeters", "Opening extents reach beyond the wall ends.");

        if (o.HingeSideIndex is < 0 or > 1) r.Error($"{path}.HingeSideIndex", $"Out of range (0-1): {o.HingeSideIndex}.");
        if (o.SwingDirectionIndex is < 0 or > 1) r.Error($"{path}.SwingDirectionIndex", $"Out of range (0-1): {o.SwingDirectionIndex}.");
        if (o.FacingOrientationIndex is < 0 or > 1) r.Error($"{path}.FacingOrientationIndex", $"Out of range (0-1): {o.FacingOrientationIndex}.");
        if (o.FamilyDefIdx < 0) r.Error($"{path}.FamilyDefIdx", $"Must be >= 0: {o.FamilyDefIdx}.");
    }
}
