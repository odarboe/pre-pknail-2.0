using NUnit.Framework;
using PKNail.AiImport.Mapping;
using PKNail.AiImport.Model;

namespace PKNail.AiImport.Tests;

/// <summary>
/// v2 contract support: walls carry absolute endpoints (`StartMeters`/`EndMeters`) and the mapper
/// DERIVES length + interior corner angle from them. The key correctness check: the v2 example (the same
/// rectangular room as the known-good demo) must map to the SAME corner angles the demo used — all 90°
/// (`π/2`) — which we proved produces a clean room in Revit 2026.
/// </summary>
[TestFixture]
public class V2ContractTests
{
    private static string V2Path =>
        System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures",
                               "ai_engine_pknail_results.v2.example.json");

    private DetectedBuilding _building = null!;

    [SetUp]
    public void SetUp() => _building = AiModelReader.Load(V2Path);

    [Test]
    public void V2_Deserializes_WallsCarryEndpoints_AndSchemaVersion()
    {
        Assert.That(_building.SchemaVersion, Is.EqualTo("2.0"));
        Assert.That(_building.Levels, Has.Count.EqualTo(1));

        DetectedLevel lvl = _building.Levels[0];
        Assert.That(lvl.Walls, Has.Count.EqualTo(4));
        Assert.That(lvl.Walls.All(w => w.HasEndpoints), Is.True);
        Assert.That(lvl.Walls[0].StartMeters!.X, Is.EqualTo(0.0).Within(1e-9));
        Assert.That(lvl.Walls[0].EndMeters!.X, Is.EqualTo(5.05).Within(1e-9));

        // Floor-to-ceiling comes from the v2 key "Floor2CeilingMeters".
        Assert.That(lvl.Floor2CeilingMeters, Is.EqualTo(2.566).Within(1e-9));
    }

    [Test]
    public void V2_Maps_DerivesLengthsFromEndpoints()
    {
        MappedBuilding mapped = AiModelMapper.Map(_building);
        var lengths = mapped.Levels[0].Walls.Select(w => w.LengthMeters).ToArray();
        Assert.That(lengths, Is.EqualTo(new[] { 5.05, 4.0, 5.05, 4.0 }).Within(1e-6));
    }

    [Test]
    public void V2_Maps_DerivesRightAngleCorners_MatchingTheProvenDemo()
    {
        MappedBuilding mapped = AiModelMapper.Map(_building);
        IReadOnlyList<MappedWall> walls = mapped.Levels[0].Walls;

        Assert.That(walls[0].IsFirstWall, Is.True);
        Assert.That(walls[0].CornerAngleRad, Is.EqualTo(0.0).Within(1e-12));       // first wall ignored
        for (int i = 1; i < walls.Count; i++)
            Assert.That(walls[i].CornerAngleRad, Is.EqualTo(System.Math.PI / 2.0).Within(1e-9),
                $"wall {i} is a derived 90° inside corner");
    }

    [Test]
    public void V2_Maps_OpeningsUseKindAndLocationFromStart()
    {
        MappedBuilding mapped = AiModelMapper.Map(_building);

        MappedOpening window = mapped.Levels[0].Walls[2].Openings.Single();
        Assert.That(window.Kind, Is.EqualTo(OpeningKind.Window));
        Assert.That(window.LocationFromNearEndMeters, Is.EqualTo(2.525).Within(1e-9)); // from LocationFromStartMeters

        MappedOpening door = mapped.Levels[0].Walls[3].Openings.Single();
        Assert.That(door.Kind, Is.EqualTo(OpeningKind.Door));
        Assert.That(door.SwingDirection, Is.EqualTo(SwingDirectionKind.In)); // SwingDirectionIndex 1
    }

    [Test]
    public void V2_IsValid_NoErrors()
    {
        ValidationReport report = AiModelValidator.Validate(_building);
        Assert.That(report.IsValid, Is.True, report.ToString());
    }

    [Test]
    public void V2_WindingIsOrientationIndependent()
    {
        // Reverse the wall order's geometry (clockwise) — corners must still come out as 90° inside corners.
        DetectedLevel lvl = _building.Levels[0];
        foreach (DetectedWall w in lvl.Walls)
        {
            (w.StartMeters, w.EndMeters) = (w.EndMeters, w.StartMeters); // flip each wall
        }
        lvl.Walls.Reverse(); // reverse loop order → opposite winding

        MappedBuilding mapped = AiModelMapper.Map(_building);
        IReadOnlyList<MappedWall> walls = mapped.Levels[0].Walls;
        for (int i = 1; i < walls.Count; i++)
            Assert.That(walls[i].CornerAngleRad, Is.EqualTo(System.Math.PI / 2.0).Within(1e-9),
                $"wall {i} should still be a 90° corner regardless of winding");
    }
}
