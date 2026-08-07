using NUnit.Framework;
using PKNail.AiImport.Mapping;
using PKNail.AiImport.Model;

namespace PKNail.AiImport.Tests;

/// <summary>
/// Guards the near-orthogonal drift warning.
/// <para>
/// Phase 1 targets orthogonal interiors, but the v2 absolute path places <c>Start</c>/<c>End</c>
/// verbatim — the mapper's orthogonal snapping only ever applied to the v1 <c>CornerAngleRad</c> chain.
/// So a wall the AI engine exports as <i>almost</i> square stays almost square in Revit, corners miss,
/// and joins fail. Nothing downstream corrects it, which is exactly why the validator has to say so.
/// </para>
/// <para>
/// The two <c>final_combined_pknail_v4.*</c> fixtures are the real regression pair: v4.0 was the AI
/// engine's drifting export, v4.1 the corrected axis-aligned one.
/// </para>
/// </summary>
[TestFixture]
public class OrthogonalDriftTests
{
    private static string FixturePath(string name) =>
        Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", name);

    private static DetectedPoint2D P(double x, double y) => new() { X = x, Y = y };

    private static IEnumerable<ValidationIssue> DriftWarnings(ValidationReport report) =>
        report.Warnings.Where(w => w.Message.Contains("off square"));

    // ---------- the geometry helper ----------

    [Test]
    public void OrthogonalDeviation_IsZero_ForAxisAlignedWalls()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WallGeometry.OrthogonalDeviation(P(0, 0), P(5, 0)), Is.EqualTo(0.0).Within(1e-12), "east");
            Assert.That(WallGeometry.OrthogonalDeviation(P(0, 0), P(0, 5)), Is.EqualTo(0.0).Within(1e-12), "north");
            Assert.That(WallGeometry.OrthogonalDeviation(P(0, 0), P(-5, 0)), Is.EqualTo(0.0).Within(1e-12), "west");
            Assert.That(WallGeometry.OrthogonalDeviation(P(0, 0), P(0, -5)), Is.EqualTo(0.0).Within(1e-12), "south");
        });
    }

    [Test]
    public void OrthogonalDeviation_IsDirectionAgnostic()
    {
        // Reversing a wall must not change how square it is.
        double forward = WallGeometry.OrthogonalDeviation(P(0, 0), P(5.0, 0.01));
        double reverse = WallGeometry.OrthogonalDeviation(P(5.0, 0.01), P(0, 0));
        Assert.That(reverse, Is.EqualTo(forward).Within(1e-12));
    }

    [Test]
    public void OrthogonalDeviation_PeaksAt45Degrees()
    {
        double deg = WallGeometry.OrthogonalDeviation(P(0, 0), P(5, 5)) * 180.0 / Math.PI;
        Assert.That(deg, Is.EqualTo(45.0).Within(1e-9));
    }

    [Test]
    public void OrthogonalDeviation_IsZero_ForCoincidentPoints()
    {
        // Degenerate walls are a length error, not a skew error — don't double-report them.
        Assert.That(WallGeometry.OrthogonalDeviation(P(3, 3), P(3, 3)), Is.EqualTo(0.0));
    }

    // ---------- the validator rule ----------

    private static DetectedBuilding BuildingWithWall(DetectedPoint2D start, DetectedPoint2D end) =>
        new()
        {
            SchemaVersion = "2.0",
            FirstWallCompassDirection = 1,
            Levels =
            {
                new DetectedLevel
                {
                    Floor2FloorMeters = 2.8,
                    Floor2CeilingMeters = 2.5,
                    Walls = { new DetectedWall { StartMeters = start, EndMeters = end, ThicknessMeters = 0.1 } },
                },
            },
        };

    [Test]
    public void Validator_Warns_OnNearSquareDrift_AndQuantifiesTheOffset()
    {
        // 5 m wall, 10 mm out of square -> ~0.115°, far end ~10 mm off axis.
        ValidationReport report = AiModelValidator.Validate(BuildingWithWall(P(0, 0), P(5.0, 0.01)));

        Assert.That(report.IsValid, Is.True, "drift is a warning, never an error — it must not block import");

        ValidationIssue[] drift = DriftWarnings(report).ToArray();
        Assert.That(drift, Has.Length.EqualTo(1));
        Assert.That(drift[0].Message, Does.Contain("0.115").And.Contain("mm"));
    }

    [Test]
    public void Validator_Silent_OnPerfectlyAxisAlignedWalls()
    {
        ValidationReport report = AiModelValidator.Validate(BuildingWithWall(P(0, 0), P(5.0, 0.0)));
        Assert.That(DriftWarnings(report), Is.Empty);
    }

    [Test]
    public void Validator_Silent_OnDeliberatelyAngledWalls()
    {
        // A 45° wall is a design choice, not drift. Warning here would be noise on every diagonal.
        ValidationReport report = AiModelValidator.Validate(BuildingWithWall(P(0, 0), P(5.0, 5.0)));
        Assert.That(DriftWarnings(report), Is.Empty);
    }

    [Test]
    public void Validator_Silent_OnFloatingPointNoise()
    {
        // Sub-0.01° is export round-off, not skew — below the noise floor.
        ValidationReport report = AiModelValidator.Validate(BuildingWithWall(P(0, 0), P(5.0, 1e-7)));
        Assert.That(DriftWarnings(report), Is.Empty);
    }

    [Test]
    public void Validator_Silent_OnV1Walls_WhichHaveNoEndpoints()
    {
        // The v1 chain has no absolute geometry to judge, and its angles ARE snapped by the mapper.
        DetectedBuilding v1 = AiModelReader.Load(TestFixtures.DemoPath);
        Assert.That(v1.Levels[0].Walls.Any(w => w.HasEndpoints), Is.False, "demo fixture should be v1");
        Assert.That(DriftWarnings(AiModelValidator.Validate(v1)), Is.Empty);
    }

    // ---------- the real AI-engine regression pair ----------

    [Test]
    public void RealExport_v40_HasDrift_AndIsReported()
    {
        DetectedBuilding building = AiModelReader.Load(FixturePath("final_combined_pknail_v4.0_bug.json"));
        ValidationReport report = AiModelValidator.Validate(building);

        // Still importable — we report the skew, we don't block on it.
        Assert.That(report.IsValid, Is.True);
        Assert.That(DriftWarnings(report), Is.Not.Empty,
            "v4.0 is the known drifting export; the guard exists to catch exactly this");
    }

    [Test]
    public void RealExport_v41_IsClean()
    {
        DetectedBuilding building = AiModelReader.Load(FixturePath("final_combined_pknail_v4.1.json"));
        ValidationReport report = AiModelValidator.Validate(building);

        Assert.That(report.IsValid, Is.True);
        Assert.That(DriftWarnings(report), Is.Empty,
            "v4.1 is the corrected axis-aligned export and must not trip the guard");
    }

    [Test]
    public void RealExport_BothVersions_MapWithoutThrowing()
    {
        // The newest engine exports were previously untested; make sure they at least round-trip.
        foreach (string name in new[] { "final_combined_pknail_v4.0_bug.json", "final_combined_pknail_v4.1.json" })
        {
            DetectedBuilding building = AiModelReader.Load(FixturePath(name));
            MappedBuilding mapped = AiModelMapper.Map(building);
            Assert.That(mapped.Levels[0].Walls, Is.Not.Empty, name);
            Assert.That(mapped.Levels[0].Walls.All(w => w.HasAbsoluteGeometry), Is.True, name);
        }
    }
}
