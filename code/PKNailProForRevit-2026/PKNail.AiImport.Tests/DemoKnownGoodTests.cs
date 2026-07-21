using NUnit.Framework;
using PKNail.AiImport.Mapping;
using PKNail.AiImport.Model;

namespace PKNail.AiImport.Tests;

/// <summary>
/// Regression for the known-good demo (`ai_engine_pknail_results.demo.json`): a clean 5x4 m room with
/// correct interior corner angles. This was the input that produced a clean closed room end-to-end in
/// Revit 2026, so it locks in the Revit-free half of that result (deserialize → validate → map).
/// </summary>
[TestFixture]
public class DemoKnownGoodTests
{
    private DetectedBuilding _building = null!;

    [SetUp]
    public void SetUp() => _building = AiModelReader.Load(TestFixtures.DemoPath);

    [Test]
    public void Demo_DeserializesToOneLevel_FourWalls_OneWindow()
    {
        Assert.That(_building.Levels, Has.Count.EqualTo(1));
        Assert.That(_building.Levels[0].Walls, Has.Count.EqualTo(4));

        var lengths = _building.Levels[0].Walls.Select(w => w.LengthMeters).ToArray();
        Assert.That(lengths, Is.EqualTo(new[] { 5.0, 4.0, 5.0, 4.0 }).Within(1e-9));

        int openings = _building.Levels[0].Walls.Sum(w => w.Openings.Count);
        Assert.That(openings, Is.EqualTo(1));
    }

    [Test]
    public void Demo_IsValid_NoErrors()
    {
        ValidationReport report = AiModelValidator.Validate(_building);
        Assert.That(report.IsValid, Is.True, report.ToString());
    }

    [Test]
    public void Demo_Maps_FirstWallAngleIgnored_RemainingCornersAreRightAngles()
    {
        MappedBuilding mapped = AiModelMapper.Map(_building);
        IReadOnlyList<MappedWall> walls = mapped.Levels[0].Walls;

        Assert.That(walls, Has.Count.EqualTo(4));
        Assert.That(walls[0].IsFirstWall, Is.True);
        Assert.That(walls[0].CornerAngleRad, Is.EqualTo(0.0).Within(1e-12));      // first wall: ignored
        for (int i = 1; i < walls.Count; i++)
            Assert.That(walls[i].CornerAngleRad, Is.EqualTo(System.Math.PI / 2.0).Within(1e-9), $"wall {i} is a 90° corner");
    }

    [Test]
    public void Demo_Maps_WindowOnThirdWall_WithConvertedDims()
    {
        MappedBuilding mapped = AiModelMapper.Map(_building);
        MappedOpening o = mapped.Levels[0].Walls[2].Openings.Single();

        Assert.That(o.Kind, Is.EqualTo(OpeningKind.Window));
        Assert.That(o.WidthMeters, Is.EqualTo(1.0).Within(1e-9));
        Assert.That(o.HeightMeters, Is.EqualTo(1.4).Within(1e-9));
        Assert.That(o.SillHeightMeters, Is.EqualTo(0.9).Within(1e-9));
    }
}
