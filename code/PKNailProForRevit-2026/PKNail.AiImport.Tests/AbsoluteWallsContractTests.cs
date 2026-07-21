using NUnit.Framework;
using PKNail.AiImport.Mapping;
using PKNail.AiImport.Model;

namespace PKNail.AiImport.Tests;

/// <summary>
/// Real v2 building (`new_final_combined_pknail.json`): 9 independent absolute walls forming a
/// multi-room layout (NOT a connected survey polyline). The mapper must carry each wall's absolute
/// endpoints (meters) through to the <see cref="MappedWall"/> so the Epic 3 bridge can place them
/// directly at their real coordinates.
/// </summary>
[TestFixture]
public class AbsoluteWallsContractTests
{
    private static string RealV2Path =>
        System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures",
                               "new_final_combined_pknail.json");

    private DetectedBuilding _building = null!;

    [SetUp]
    public void SetUp() => _building = AiModelReader.Load(RealV2Path);

    [Test]
    public void RealV2_IsValid_NoErrors()
    {
        ValidationReport report = AiModelValidator.Validate(_building);
        Assert.That(report.IsValid, Is.True, report.ToString());
    }

    [Test]
    public void RealV2_Maps_OneLevel_NineAbsoluteWalls_WithEndpoints()
    {
        MappedBuilding mapped = AiModelMapper.Map(_building);

        Assert.That(mapped.Levels, Has.Count.EqualTo(1));
        IReadOnlyList<MappedWall> walls = mapped.Levels[0].Walls;
        Assert.That(walls, Has.Count.EqualTo(9));

        Assert.That(walls.All(w => w.HasAbsoluteGeometry), Is.True,
            "every real v2 wall carries absolute endpoints");

        // Wall 0: Start (5.5689, -97.741) -> End (10.569, -97.741).
        Assert.That(walls[0].Start!.XMeters, Is.EqualTo(5.5689).Within(1e-3));
        Assert.That(walls[0].Start!.YMeters, Is.EqualTo(-97.741).Within(1e-3));
        Assert.That(walls[0].End!.XMeters, Is.EqualTo(10.569).Within(1e-3));
        Assert.That(walls[0].End!.YMeters, Is.EqualTo(-97.741).Within(1e-3));

        // Length is still derived (5.0 m for wall 0) so the v1 fields remain populated.
        Assert.That(walls[0].LengthMeters, Is.EqualTo(5.0).Within(1e-3));
    }

    [Test]
    public void RealV2_Maps_ThreeWindowsTotal_AcrossWalls()
    {
        MappedBuilding mapped = AiModelMapper.Map(_building);
        var openings = mapped.Levels[0].Walls.SelectMany(w => w.Openings).ToList();

        Assert.That(openings, Has.Count.EqualTo(3), "3 windows total across the 9 walls");
        Assert.That(openings.All(o => o.Kind == OpeningKind.Window), Is.True);
    }
}
