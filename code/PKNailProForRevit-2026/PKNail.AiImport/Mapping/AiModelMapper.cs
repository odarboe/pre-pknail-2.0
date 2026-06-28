using PKNail.AiImport.Model;

namespace PKNail.AiImport.Mapping;

/// <summary>
/// Epic 2 — Revit-free mapping from the <c>Detected*</c> contract DTOs to the normalized
/// <see cref="MappedBuilding"/>: resolves orientation / compass / door-vs-window / family index, snaps
/// corner angles to architectural intent, and preserves wall order. <b>No unit conversion</b> (lengths
/// stay in meters) and <b>no Revit dependency</b>. Run <see cref="AiModelValidator"/> first.
/// </summary>
public static class AiModelMapper
{
    private const double HalfPi = Math.PI / 2.0;
    private const double TwoPi = Math.PI * 2.0;
    private const double Epsilon = 1e-9;

    public static MappedBuilding Map(DetectedBuilding building, MappingOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(building);
        options ??= MappingOptions.Default;

        SurveyOrientationKind orientation = building.IsScanFromInterior
            ? SurveyOrientationKind.Flipped
            : SurveyOrientationKind.NotFlipped;

        CompassDirectionKind compass = building.FirstWallCompassDirection is >= 0 and <= 4
            ? (CompassDirectionKind)building.FirstWallCompassDirection
            : CompassDirectionKind.Undefined;

        var levels = new List<MappedLevel>(building.Levels.Count);
        foreach (DetectedLevel lvl in building.Levels)
            levels.Add(MapLevel(lvl, options));

        return new MappedBuilding(orientation, compass, levels);
    }

    private static MappedLevel MapLevel(DetectedLevel lvl, MappingOptions options)
    {
        var walls = new List<MappedWall>(lvl.Walls.Count);
        for (int i = 0; i < lvl.Walls.Count; i++)
            walls.Add(MapWall(lvl.Walls[i], i, options));

        return new MappedLevel(
            lvl.Floor2FloorMeters,
            lvl.Floor2CeilingMeters,
            lvl.HasDropCeiling,
            lvl.HasDropCeiling ? lvl.DropCeilingHeightMeters : 0.0,
            lvl.TypicalWindowSillHeightMeters,
            lvl.TypicalWindowHeadHeightMeters,
            walls);
    }

    private static MappedWall MapWall(DetectedWall w, int index, MappingOptions options)
    {
        bool isFirst = index == 0;

        // The first wall has no predecessor, so its corner angle is meaningless and ignored (Q2).
        double angle;
        bool snapped;
        if (isFirst)
        {
            angle = 0.0;
            snapped = false;
        }
        else
        {
            angle = SnapCornerAngle(w.CornerAngleRad, options, out snapped);
        }

        var openings = new List<MappedOpening>(w.Openings.Count);
        foreach (DetectedOpening o in w.Openings)
            openings.Add(MapOpening(o));

        return new MappedWall(index, isFirst, w.LengthMeters, w.ThicknessMeters, angle, snapped, w.LevelOffsetMeters, openings);
    }

    private static MappedOpening MapOpening(DetectedOpening o) => new(
        o.LocationFromNearEndMeters,
        o.WidthMeters,
        o.HeightMeters,
        o.SillHeightMeters,
        o.IsLikelyDoor ? OpeningKind.Door : OpeningKind.Window,
        o.HingeSideIndex == 1 ? HingeSideKind.Right : HingeSideKind.Left,
        o.SwingDirectionIndex == 1 ? SwingDirectionKind.In : SwingDirectionKind.Out,
        o.FacingOrientationIndex == 1 ? FacingRelationKind.Exterior : FacingRelationKind.Interior,
        o.FamilyDefIdx);

    /// <summary>
    /// Normalize an angle to [0, 2π) and, when <see cref="MappingOptions.SnapToOrthogonal"/> is set,
    /// snap it to the nearest multiple of 90° if within tolerance. Floating-point noise (e.g. 2.22e-16)
    /// collapses to 0 because 0 is an orthogonal multiple.
    /// </summary>
    internal static double SnapCornerAngle(double radians, MappingOptions options, out bool snapped)
    {
        double normalized = radians % TwoPi;
        if (normalized < 0) normalized += TwoPi;

        if (!options.SnapToOrthogonal)
        {
            snapped = false;
            return normalized;
        }

        double nearest = Math.Round(normalized / HalfPi) * HalfPi;
        if (nearest >= TwoPi - Epsilon) nearest = 0.0; // wrap 360° → 0°

        double delta = Math.Abs(normalized - nearest);
        if (delta <= options.OrthogonalSnapToleranceRad)
        {
            // "snapped" = the value was moved at all (any nonzero correction, incl. collapsing noise to 0).
            snapped = delta > 0.0;
            return nearest;
        }

        snapped = false;
        return normalized;
    }
}
