using NUnit.Framework;

using PKNail.AiImport.Mapping;
using static PKNail.AiImport.Mapping.WallActivationGeometry;

namespace PKNail.AiImport.Tests;

/// <summary>
/// Epic 11b — unit coverage for the previously-untested absolute-wall activation geometry (review R12).
/// These are the two conventions that decide correct opening placement on a v2 absolute wall: which face
/// the scan surveyed, and which survey direction makes the face corner nearest the opening's near end the
/// engine's near corner. The Revit method <c>RevitDocument.ActivateWallById</c> now delegates both here.
/// </summary>
[TestFixture]
public class WallActivationGeometryTests
{
    // --- Decision 1: interior/exterior scan -> surveyed face --------------------------------------

    [Test]
    public void InteriorScan_SurveysInteriorFace_NotFlipped()
        => Assert.That(FaceOrientation(scanFromInterior: true), Is.EqualTo(SurveyOrientationKind.NotFlipped));

    [Test]
    public void ExteriorScan_SurveysExteriorFace_Flipped()
        => Assert.That(FaceOrientation(scanFromInterior: false), Is.EqualTo(SurveyOrientationKind.Flipped));

    // --- Decision 2: near-end -> survey direction ------------------------------------------------

    [Test]
    public void NearEnd_AtLeftCorner_ChoosesLeftToRight()
    {
        Direction dir = NearEndDirection(
            cornerLeft: new PlanPoint(0, 0), cornerRight: new PlanPoint(10, 0),
            desiredNear: new PlanPoint(0.2, 0));

        Assert.That(dir, Is.EqualTo(Direction.LeftToRight));
        Assert.That(NearSide(dir), Is.EqualTo(Side.Left));
    }

    [Test]
    public void NearEnd_AtRightCorner_ChoosesRightToLeft()
    {
        Direction dir = NearEndDirection(
            cornerLeft: new PlanPoint(0, 0), cornerRight: new PlanPoint(10, 0),
            desiredNear: new PlanPoint(9.8, 0));

        Assert.That(dir, Is.EqualTo(Direction.RightToLeft));
        Assert.That(NearSide(dir), Is.EqualTo(Side.Right));
    }

    [Test]
    public void EquidistantNearEnd_TieBreaksToLeftToRight()
        => Assert.That(NearEndDirection(new PlanPoint(0, 0), new PlanPoint(10, 0), new PlanPoint(5, 0)),
                       Is.EqualTo(Direction.LeftToRight));

    [Test]
    public void SwappingCorners_FlipsDirection()
    {
        var near = new PlanPoint(0, 0);
        Assert.That(NearEndDirection(new PlanPoint(0, 0), new PlanPoint(10, 0), near), Is.EqualTo(Direction.LeftToRight));
        Assert.That(NearEndDirection(new PlanPoint(10, 0), new PlanPoint(0, 0), near), Is.EqualTo(Direction.RightToLeft));
    }

    [Test]
    public void UsesFull2DDistance_NotJustX()
    {
        // The Right corner sits off-axis; the near point is exactly on it, so distance to Right is 0.
        Direction dir = NearEndDirection(
            cornerLeft: new PlanPoint(0, 0), cornerRight: new PlanPoint(3, 4),
            desiredNear: new PlanPoint(3, 4));

        Assert.That(dir, Is.EqualTo(Direction.RightToLeft), "must compare true 2D distance, not just X");
    }

    [Test]
    public void NearSide_MapsDirectionToSide()
    {
        Assert.That(NearSide(Direction.LeftToRight), Is.EqualTo(Side.Left));
        Assert.That(NearSide(Direction.RightToLeft), Is.EqualTo(Side.Right));
    }

    // --- The end-to-end convention: the chosen near side's corner IS the desired near end --------

    [TestCase(true)]   // near end = the Left corner (e.g. opening Location measured from the wall start)
    [TestCase(false)]  // near end = the Right corner
    public void ChosenNearSideCorner_IsTheDesiredNearEnd(bool nearAtLeftCorner)
    {
        var left = new PlanPoint(0, 0);
        var right = new PlanPoint(6.2, 0);
        var desiredNear = nearAtLeftCorner ? left : right;

        Direction dir = NearEndDirection(left, right, desiredNear);
        Side side = NearSide(dir);
        PlanPoint nearCorner = side == Side.Left ? left : right;

        Assert.That(nearCorner, Is.EqualTo(desiredNear),
            "the engine's near corner must coincide with the desired near end");
    }
}
