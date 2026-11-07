using NUnit.Framework;

using PKNail.AiImport.DirectBuild;
using PKNail.AiImport.Mapping;

namespace PKNail.AiImport.Tests;

/// <summary>Epic 13 — Revit-free flip rules applied to doors/windows placed by Direct Build.</summary>
[TestFixture]
public class OpeningOrientationTests
{
    private static readonly PlanPoint Interior = new(0, 1);   // the wall's interior side is +Y
    private static readonly PlanPoint TowardInterior = new(0, 1);
    private static readonly PlanPoint TowardExterior = new(0, -1);

    [Test]
    public void Window_FacingInterior_IsFlippedToFaceExterior()
    {
        Assert.That(OpeningOrientation.WindowNeedsFlipFacing(TowardInterior, Interior), Is.True);
        Assert.That(OpeningOrientation.WindowNeedsFlipFacing(TowardExterior, Interior), Is.False);
    }

    [Test]
    public void Door_WithoutSwing_KeepsRevitDefault()
    {
        Assert.That(OpeningOrientation.DoorNeedsFlipFacing(TowardInterior, Interior, null), Is.False);
        Assert.That(OpeningOrientation.DoorNeedsFlipFacing(TowardExterior, Interior, null), Is.False);
    }

    [Test]
    public void Door_Swing_FollowsTheFacingConvention()
    {
        // With DoorSwingsTowardFacing = true, a door facing the interior swings in.
        Assume.That(OpeningOrientation.DoorSwingsTowardFacing, Is.True);
        Assert.That(OpeningOrientation.DoorNeedsFlipFacing(TowardInterior, Interior, SwingDirectionKind.In), Is.False);
        Assert.That(OpeningOrientation.DoorNeedsFlipFacing(TowardInterior, Interior, SwingDirectionKind.Out), Is.True);
        Assert.That(OpeningOrientation.DoorNeedsFlipFacing(TowardExterior, Interior, SwingDirectionKind.In), Is.True);
        Assert.That(OpeningOrientation.DoorNeedsFlipFacing(TowardExterior, Interior, SwingDirectionKind.Out), Is.False);
    }

    [Test]
    public void Door_Hinge_IsJudgedFromTheInteriorSide()
    {
        // Viewer on +Y looking toward -Y: their right is -X. Hand (latch→hinge) = +X ⇒ hinge on +X ⇒ Left.
        Assume.That(OpeningOrientation.HandPointsAwayFromHinge, Is.False);
        var hand = new PlanPoint(1, 0);
        Assert.That(OpeningOrientation.DoorNeedsFlipHand(hand, Interior, HingeSideKind.Left), Is.False);
        Assert.That(OpeningOrientation.DoorNeedsFlipHand(hand, Interior, HingeSideKind.Right), Is.True);
        Assert.That(OpeningOrientation.DoorNeedsFlipHand(hand * -1.0, Interior, HingeSideKind.Right), Is.False);
    }

    // The three hinged doors of 1_v2_ai_pknail_v2_doortypes.json (Revit check 2026-11-06). Revit hosts a
    // door with its hand along the wall direction; each case is the hand flip that puts the hinge on the
    // jamb drawn in expected_plan.png. With the old convention all three came out mirrored.
    [TestCase(1.0, 0.0, 0.0, -1.0, HingeSideKind.Right, false, TestName = "Wall 9 door 0: runs +X, interior south, hinge on the east jamb")]
    [TestCase(0.0, 1.0, 1.0, 0.0, HingeSideKind.Right, false, TestName = "Wall 6 door 1: runs +Y, interior east, hinge on the north jamb")]
    [TestCase(0.0, 1.0, 1.0, 0.0, HingeSideKind.Left, true, TestName = "Wall 6 door 2: runs +Y, interior east, hinge on the south jamb")]
    public void Door_Hinge_RevitCheckDoors(double wallDirX, double wallDirY, double interiorX, double interiorY,
                                           HingeSideKind hinge, bool expectFlipHand)
    {
        var hand = new PlanPoint(wallDirX, wallDirY);
        var interior = new PlanPoint(interiorX, interiorY);
        Assert.That(OpeningOrientation.DoorNeedsFlipHand(hand, interior, hinge), Is.EqualTo(expectFlipHand));
    }

    [Test]
    public void Door_WithoutHinge_KeepsRevitDefault()
    {
        Assert.That(OpeningOrientation.DoorNeedsFlipHand(new PlanPoint(1, 0), Interior, null), Is.False);
    }
}
