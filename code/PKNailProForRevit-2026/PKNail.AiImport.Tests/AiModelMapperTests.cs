using NUnit.Framework;
using PKNail.AiImport.Mapping;
using PKNail.AiImport.Model;

namespace PKNail.AiImport.Tests;

/// <summary>Epic 2 — Detected* → MappedBuilding (semantics + intent snapping, Revit-free, meters preserved).</summary>
[TestFixture]
public class AiModelMapperTests
{
    private MappedBuilding _mapped = null!;

    [SetUp]
    public void SetUp() =>
        _mapped = AiModelMapper.Map(AiModelReader.Load(TestFixtures.FrozenContractPath));

    [Test]
    public void Orientation_FromInteriorScan_IsFlipped()
    {
        // IsScanFromInterior == true ⇒ Flipped
        Assert.That(_mapped.Orientation, Is.EqualTo(SurveyOrientationKind.Flipped));
    }

    [Test]
    public void FirstWallCompass_DontKnow_IsUndefined()
    {
        Assert.That(_mapped.FirstWallCompass, Is.EqualTo(CompassDirectionKind.Undefined));
    }

    [Test]
    public void Structure_LevelsAndWallsPreserved_InOrder()
    {
        Assert.That(_mapped.Levels, Has.Count.EqualTo(1));
        IReadOnlyList<MappedWall> walls = _mapped.Levels[0].Walls;
        Assert.That(walls, Has.Count.EqualTo(9));
        for (int i = 0; i < walls.Count; i++)
            Assert.That(walls[i].Index, Is.EqualTo(i));
        Assert.That(walls[0].IsFirstWall, Is.True);
        Assert.That(walls.Skip(1).All(w => !w.IsFirstWall), Is.True);
    }

    [Test]
    public void FirstWall_CornerAngle_IsIgnored_SetToZero()
    {
        // JSON first-wall angle is 4.712 (270°); mapping drops it to 0 (Q2).
        Assert.That(_mapped.Levels[0].Walls[0].CornerAngleRad, Is.EqualTo(0.0).Within(1e-12));
    }

    [Test]
    public void Lengths_StayInMeters_Unconverted()
    {
        // No unit conversion in Epic 2 — meters preserved exactly (Epic 3 converts via Distance).
        Assert.That(_mapped.Levels[0].Walls[0].LengthMeters, Is.EqualTo(5.05).Within(1e-9));
        Assert.That(_mapped.Levels[0].Walls[0].ThicknessMeters, Is.EqualTo(0.2).Within(1e-9));
    }

    [Test]
    public void RightAngleWall_StaysNinety_AndIsMarkedSnapped()
    {
        MappedWall w = _mapped.Levels[0].Walls[5]; // CornerAngleRad 1.5707963 in the sample
        Assert.That(w.CornerAngleRad, Is.EqualTo(Math.PI / 2.0).Within(1e-9));
    }

    [Test]
    public void FloatingPointNoiseAngle_SnapsToZero()
    {
        MappedWall w = _mapped.Levels[0].Walls[6]; // CornerAngleRad 2.22e-16 in the sample
        Assert.That(w.CornerAngleRad, Is.EqualTo(0.0).Within(1e-12));
        Assert.That(w.CornerAngleSnapped, Is.True);
    }

    [Test]
    public void Opening_OnThirdWall_IsWindow_DimsPreserved()
    {
        MappedOpening o = _mapped.Levels[0].Walls[2].Openings.Single();
        Assert.Multiple(() =>
        {
            Assert.That(o.Kind, Is.EqualTo(OpeningKind.Window)); // IsLikelyDoor == false
            Assert.That(o.WidthMeters, Is.EqualTo(1.0).Within(1e-9));
            Assert.That(o.HeightMeters, Is.EqualTo(1.4).Within(1e-9));
            Assert.That(o.SillHeightMeters, Is.EqualTo(0.4).Within(1e-9));
            Assert.That(o.LocationFromNearEndMeters, Is.EqualTo(4.0).Within(1e-9));
            Assert.That(o.FamilyDefIdx, Is.EqualTo(0));
            Assert.That(o.Facing, Is.EqualTo(FacingRelationKind.Interior)); // index 0
        });
    }

    // --- focused unit tests for the snapping + semantic helpers (synthetic inputs) ---

    [Test]
    public void Snap_NearRightAngle_SnapsToHalfPi()
    {
        double near90 = 89.0 * Math.PI / 180.0;
        double result = AiModelMapper.SnapCornerAngle(near90, MappingOptions.Default, out bool snapped);
        Assert.That(result, Is.EqualTo(Math.PI / 2.0).Within(1e-12));
        Assert.That(snapped, Is.True);
    }

    [Test]
    public void Snap_FortyFiveDegrees_DoesNotSnap()
    {
        double deg45 = 45.0 * Math.PI / 180.0;
        double result = AiModelMapper.SnapCornerAngle(deg45, MappingOptions.Default, out bool snapped);
        Assert.That(result, Is.EqualTo(deg45).Within(1e-12));
        Assert.That(snapped, Is.False);
    }

    [Test]
    public void Snap_NormalizesNegativeAndWraps()
    {
        double result = AiModelMapper.SnapCornerAngle(-Math.PI / 2.0, MappingOptions.Default, out _);
        Assert.That(result, Is.EqualTo(3.0 * Math.PI / 2.0).Within(1e-12)); // -90° → 270°
    }

    [Test]
    public void ExteriorScan_MapsToNotFlipped()
    {
        var b = new DetectedBuilding { IsScanFromInterior = false, FirstWallCompassDirection = 2 };
        MappedBuilding m = AiModelMapper.Map(b);
        Assert.That(m.Orientation, Is.EqualTo(SurveyOrientationKind.NotFlipped));
        Assert.That(m.FirstWallCompass, Is.EqualTo(CompassDirectionKind.East));
    }

    [Test]
    public void DoorOpening_RoutesToDoor_WithHingeAndSwing()
    {
        var b = new DetectedBuilding
        {
            Levels =
            {
                new DetectedLevel
                {
                    Floor2FloorMeters = 2.9, Floor2CeilingMeters = 2.6,
                    Walls =
                    {
                        new DetectedWall
                        {
                            LengthMeters = 4, ThicknessMeters = 0.2,
                            Openings =
                            {
                                new DetectedOpening
                                {
                                    LocationFromNearEndMeters = 2, WidthMeters = 0.9, HeightMeters = 2.1,
                                    SillHeightMeters = 0, IsLikelyDoor = true,
                                    HingeSideIndex = 1, SwingDirectionIndex = 1,
                                },
                            },
                        },
                    },
                },
            },
        };

        MappedOpening o = AiModelMapper.Map(b).Levels[0].Walls[0].Openings.Single();
        Assert.Multiple(() =>
        {
            Assert.That(o.Kind, Is.EqualTo(OpeningKind.Door));
            Assert.That(o.HingeSide, Is.EqualTo(HingeSideKind.Right));
            Assert.That(o.SwingDirection, Is.EqualTo(SwingDirectionKind.In));
        });
    }
}
