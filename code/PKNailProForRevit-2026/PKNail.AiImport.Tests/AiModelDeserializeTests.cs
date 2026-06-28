using NUnit.Framework;
using PKNail.AiImport.Model;

namespace PKNail.AiImport.Tests;

/// <summary>Epic 1 — deserialization of the frozen JSON contract v1 into the Detected* model.</summary>
[TestFixture]
public class AiModelDeserializeTests
{
    private DetectedBuilding _building = null!;

    [SetUp]
    public void SetUp() => _building = AiModelReader.Load(TestFixtures.FrozenContractPath);

    [Test]
    public void Building_TopLevelFields()
    {
        Assert.That(_building.IsScanFromInterior, Is.True);
        Assert.That(_building.FirstWallCompassDirection, Is.EqualTo(0)); // DontKnow
        Assert.That(_building.SchemaVersion, Is.Null);                   // v1 carries no version (Q10)
        Assert.That(_building.Levels, Has.Count.EqualTo(1));
    }

    [Test]
    public void Level_DimensionsAndTypicalHeights()
    {
        DetectedLevel lvl = _building.Levels[0];
        Assert.Multiple(() =>
        {
            Assert.That(lvl.Floor2FloorMeters, Is.EqualTo(2.8664869165231077).Within(1e-9));
            Assert.That(lvl.Floor2CeilingMeters, Is.EqualTo(2.566486916523108).Within(1e-9)); // mapped from "Floor2CeilingElev"
            Assert.That(lvl.HasDropCeiling, Is.False);
            Assert.That(lvl.TypicalWindowSillHeightMeters, Is.EqualTo(0.9).Within(1e-9));
            Assert.That(lvl.TypicalWindowHeadHeightMeters, Is.EqualTo(2.1).Within(1e-9));
            Assert.That(lvl.Walls, Has.Count.EqualTo(9));
        });
    }

    [Test]
    public void Walls_LengthsAndThickness()
    {
        var lengths = _building.Levels[0].Walls.Select(w => w.LengthMeters).ToArray();
        Assert.That(lengths[0], Is.EqualTo(5.05).Within(1e-6));
        Assert.That(lengths[1], Is.EqualTo(6.0).Within(1e-6));
        Assert.That(lengths[2], Is.EqualTo(5.25).Within(1e-6));
        Assert.That(lengths[5], Is.EqualTo(8.35).Within(1e-6));
        Assert.That(_building.Levels[0].Walls.All(w => Math.Abs(w.ThicknessMeters - 0.2) < 1e-6), Is.True,
            "All sample walls are 0.2 m thick.");
    }

    [Test]
    public void FirstWall_CarriesTheUnreliableCornerAngle()
    {
        // 4.712 rad = 270°; meaningless on the first wall — preserved as-is by the DTO, handled in mapping (Q2).
        Assert.That(_building.Levels[0].Walls[0].CornerAngleRad, Is.EqualTo(4.71238898038469).Within(1e-9));
    }

    [Test]
    public void Opening_OnThirdWall_IsTheWindow()
    {
        DetectedWall wallWithOpening = _building.Levels[0].Walls[2]; // length ~5.25 m
        Assert.That(wallWithOpening.Openings, Has.Count.EqualTo(1));

        DetectedOpening o = wallWithOpening.Openings[0];
        Assert.Multiple(() =>
        {
            Assert.That(o.LocationFromNearEndMeters, Is.EqualTo(4.0).Within(1e-6));
            Assert.That(o.WidthMeters, Is.EqualTo(1.0).Within(1e-6));
            Assert.That(o.HeightMeters, Is.EqualTo(1.4).Within(1e-6));
            Assert.That(o.SillHeightMeters, Is.EqualTo(0.4).Within(1e-6));
            Assert.That(o.IsLikelyDoor, Is.False);  // window
            Assert.That(o.FamilyDefIdx, Is.EqualTo(0));
        });
    }

    [Test]
    public void OtherWalls_HaveNoOpenings()
    {
        int openingCount = _building.Levels[0].Walls.Sum(w => w.Openings.Count);
        Assert.That(openingCount, Is.EqualTo(1), "Sample has exactly one opening across all walls.");
    }

    [Test]
    public void RoundTrip_PreservesStructureAndValues()
    {
        string json = AiModelReader.Serialize(_building);
        DetectedBuilding again = AiModelReader.Parse(json);

        Assert.That(again.Levels, Has.Count.EqualTo(1));
        Assert.That(again.Levels[0].Walls, Has.Count.EqualTo(9));
        Assert.That(again.Levels[0].Floor2CeilingMeters,
            Is.EqualTo(_building.Levels[0].Floor2CeilingMeters).Within(1e-12));
        Assert.That(again.Levels[0].Walls[2].Openings[0].WidthMeters, Is.EqualTo(1.0).Within(1e-9));
    }

    [Test]
    public void Parse_EmptyOrMalformed_Throws()
    {
        Assert.Throws<AiModelFormatException>(() => AiModelReader.Parse(""));
        Assert.Throws<AiModelFormatException>(() => AiModelReader.Parse("{ not json"));
    }
}
