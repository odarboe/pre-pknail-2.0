namespace PKNail.AiImport.Mapping;

/// <summary>
/// Revit-free geometry decisions for activating an absolute-placed wall as the engine's reference wall
/// (Epic 11b). The Revit side reads the two face corners from the wall solid; this type encodes the two
/// CONVENTIONS that determine correct opening placement, so they are unit-testable without Revit:
/// <list type="number">
/// <item>which physical wall face the scan surveyed (interior vs exterior), and</item>
/// <item>which survey direction makes the face corner nearest the desired near end the engine's
/// "near side" — the corner from which an opening's <c>Location</c> is measured.</item>
/// </list>
/// The matching Revit wrapper is <c>RevitDocument.ActivateWallById</c>; keeping the decisions here means
/// that method contains no untested geometry logic (it only reads Revit geometry and translates enums).
/// </summary>
public static class WallActivationGeometry
{
    /// <summary>A unit-agnostic plan point. Only relative distances between points are compared, so the
    /// unit (feet or meters) is irrelevant as long as all points share it.</summary>
    public readonly record struct PlanPoint(double X, double Y);

    /// <summary>Which end of the surveyed face is the engine's near corner.</summary>
    public enum Side { Left, Right }

    /// <summary>Survey direction along the face. LeftToRight ⇒ near side is Left; RightToLeft ⇒ Right.</summary>
    public enum Direction { LeftToRight, RightToLeft }

    /// <summary>
    /// The wall face the scan surveyed. An <b>interior</b> scan surveys the interior face — which the
    /// engine represents as <see cref="SurveyOrientationKind.NotFlipped"/>; an <b>exterior</b> scan
    /// surveys the exterior face (<see cref="SurveyOrientationKind.Flipped"/>). Mirrors
    /// <c>Utilities.SurveyOrientationFromFace</c> on the Revit side.
    /// </summary>
    public static SurveyOrientationKind FaceOrientation(bool scanFromInterior)
        => scanFromInterior ? SurveyOrientationKind.NotFlipped : SurveyOrientationKind.Flipped;

    /// <summary>
    /// Choose the survey direction so the face corner nearest <paramref name="desiredNear"/> becomes the
    /// engine's near corner. Convention-independent: it compares the two supplied face corners (read from
    /// the wall solid on the Revit side) to the desired near end. A tie resolves to
    /// <see cref="Direction.LeftToRight"/> (the Left corner), matching the engine's <c>&lt;=</c> tie-break.
    /// </summary>
    public static Direction NearEndDirection(PlanPoint cornerLeft, PlanPoint cornerRight, PlanPoint desiredNear)
        => DistanceSq(cornerLeft, desiredNear) <= DistanceSq(cornerRight, desiredNear)
            ? Direction.LeftToRight
            : Direction.RightToLeft;

    /// <summary>The near side implied by a survey direction.</summary>
    public static Side NearSide(Direction direction)
        => direction == Direction.LeftToRight ? Side.Left : Side.Right;

    private static double DistanceSq(PlanPoint a, PlanPoint b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        return (dx * dx) + (dy * dy);
    }
}
