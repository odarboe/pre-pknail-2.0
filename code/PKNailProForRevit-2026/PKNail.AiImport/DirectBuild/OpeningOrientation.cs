using PKNail.AiImport.Mapping;

namespace PKNail.AiImport.DirectBuild;

/// <summary>
/// Epic 13 — Revit-free flip decisions for a door/window that Direct Build has just placed. The Revit
/// bridge reads the instance's <c>FacingOrientation</c> / <c>HandOrientation</c>, asks this class whether
/// to call <c>flipFacing()</c> / <c>flipHand()</c>, and applies the answer. Keeping the rules here makes
/// them unit-testable and puts the two family conventions in ONE place.
/// <para>
/// Definitions used by Direct Build:
/// <list type="bullet">
/// <item><b>Interior side</b> of a wall: <see cref="PlannedWall.InteriorNormal"/> (toward the plan centroid).</item>
/// <item><b>Windows</b> face the exterior.</item>
/// <item><b>Swing In</b> = the door leaf opens into the interior side; <b>Out</b> = into the exterior side.</item>
/// <item><b>Hinge Left/Right</b> = as seen by a person standing on the interior side, facing the wall.</item>
/// </list>
/// When the AI did not provide swing or hinge, the Revit default is kept (no flip).
/// </para>
/// </summary>
public static class OpeningOrientation
{
    /// <summary>
    /// Family convention: in Revit's standard door families the leaf swings toward the side the
    /// instance's <c>FacingOrientation</c> points to. <b>Verify on the first manual test</b> (Epic 13
    /// manual step 6); if doors come out mirrored, flip this constant — nothing else changes.
    /// </summary>
    public const bool DoorSwingsTowardFacing = true;

    /// <summary>
    /// Family convention: <c>true</c> if a door's <c>HandOrientation</c> points from the hinge toward the
    /// latch, <c>false</c> if it points from the latch toward the hinge.
    /// <para>
    /// <b>false</b> since the 2026-11-06 Revit check (Q19): with <c>true</c>, all three hinged doors of
    /// <c>1_v2_ai_pknail_v2_doortypes.json</c> came out with the hinge on the wrong jamb while the swing
    /// side was right. It also matches the 1.0 engine, whose hand-flip rule
    /// (<c>wallHostedInstanceDefinition.construct</c>) puts the hinge on the side the hand points to.
    /// </para>
    /// </summary>
    public const bool HandPointsAwayFromHinge = false;

    /// <summary>
    /// Window rule: the window should face the exterior, i.e. its facing vector should point away from
    /// <paramref name="interior"/>. Returns true when a <c>flipFacing()</c> is needed.
    /// </summary>
    public static bool WindowNeedsFlipFacing(PlanPoint facing, PlanPoint interior) => facing.Dot(interior) > 0.0;

    /// <summary>
    /// Door swing rule. Returns true when a <c>flipFacing()</c> is needed to make the door swing as
    /// requested; false when <paramref name="swing"/> is null (keep the Revit default).
    /// </summary>
    public static bool DoorNeedsFlipFacing(PlanPoint facing, PlanPoint interior, SwingDirectionKind? swing)
    {
        if (swing is null) return false;
        bool facingIsInterior = facing.Dot(interior) > 0.0;
        bool swingsToInterior = DoorSwingsTowardFacing ? facingIsInterior : !facingIsInterior;
        bool wantInterior = swing == SwingDirectionKind.In;
        return swingsToInterior != wantInterior;
    }

    /// <summary>
    /// Door hinge rule, evaluated AFTER any facing flip (read <c>HandOrientation</c> again first). Returns
    /// true when a <c>flipHand()</c> is needed; false when <paramref name="hinge"/> is null.
    /// </summary>
    public static bool DoorNeedsFlipHand(PlanPoint hand, PlanPoint interior, HingeSideKind? hinge)
    {
        if (hinge is null) return false;
        // A viewer on the interior side looks along -interior; their right-hand direction is
        // (-interior) rotated 90° clockwise = (-interior.Y, interior.X).
        var viewerRight = new PlanPoint(-interior.Y, interior.X);
        PlanPoint hingeDir = HandPointsAwayFromHinge ? hand * -1.0 : hand;
        bool hingeIsRight = hingeDir.Dot(viewerRight) > 0.0;
        bool wantRight = hinge == HingeSideKind.Right;
        return hingeIsRight != wantRight;
    }
}
