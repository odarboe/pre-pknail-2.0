using NUnit.Framework;

using PKNail.AiImport.DirectBuild;
using PKNail.AiImport.Model;

namespace PKNail.AiImport.Tests;

/// <summary>Epic 13 — contract v2.1 (additive): optional ids/heights/comments/type, nullable door orientation,
/// and the Direct Build validation policy (element problems are skipped, not file-blocking).</summary>
[TestFixture]
public class ContractV21Tests
{
    private const string V21Json = """
    {
      "SchemaVersion": "2.1", "Units": "meters", "AngleUnits": "radians",
      "IsScanFromInterior": true, "FirstWallCompassDirection": 0,
      "Levels": [{
        "Name": "Ground", "ElevationMeters": 0.6, "Floor2FloorMeters": 2.75, "Floor2CeilingMeters": 2.45,
        "HasDropCeiling": false, "TypicalWindowSillHeightMeters": 0.8, "TypicalWindowHeadHeightMeters": 2.0,
        "Walls": [{
          "Id": "W7", "StartMeters": {"X": 1, "Y": 2}, "EndMeters": {"X": 5, "Y": 2},
          "ThicknessMeters": 0.2, "HeightMeters": 2.6, "LocationLine": "Centerline", "LevelOffsetMeters": 0.05,
          "Comment": "thickness assumed (one face scanned); 87% scanned",
          "Openings": [
            {"Id": "W7-D1", "Kind": "Door", "OpeningType": "Sliding", "LocationFromStartMeters": 1.0,
             "WidthMeters": 0.8, "HeightMeters": 2.0, "SillHeightMeters": 0.0, "Comment": "confirmed"},
            {"Id": "W7-D2", "Kind": "Door", "LocationFromStartMeters": 3.0, "WidthMeters": 0.8,
             "HeightMeters": 2.0, "SillHeightMeters": 0.0, "HingeSideIndex": 1, "SwingDirectionIndex": 0}
          ]
        }]
      }]
    }
    """;

    [Test]
    public void V21_OptionalFields_Deserialize()
    {
        DetectedBuilding b = AiModelReader.Parse(V21Json);
        DetectedLevel lvl = b.Levels[0];
        DetectedWall w = lvl.Walls[0];

        Assert.That(lvl.Name, Is.EqualTo("Ground"));
        Assert.That(w.Id, Is.EqualTo("W7"));
        Assert.That(w.HeightMeters, Is.EqualTo(2.6));
        Assert.That(w.LocationLine, Is.EqualTo("Centerline"));
        Assert.That(w.LevelOffsetMeters, Is.EqualTo(0.05), "the v2 key LevelOffsetMeters is now read");
        Assert.That(w.Comment, Does.Contain("87% scanned"));
        Assert.That(w.Openings[0].Id, Is.EqualTo("W7-D1"));
        Assert.That(w.Openings[0].OpeningType, Is.EqualTo("Sliding"));
        Assert.That(w.Openings[0].Comment, Is.EqualTo("confirmed"));
    }

    [Test]
    public void MissingDoorOrientation_IsNull_NotSilentlyZero()
    {
        DetectedBuilding b = AiModelReader.Parse(V21Json);
        DetectedOpening missing = b.Levels[0].Walls[0].Openings[0];
        DetectedOpening given = b.Levels[0].Walls[0].Openings[1];

        Assert.That(missing.HingeSideIndex, Is.Null);
        Assert.That(missing.SwingDirectionIndex, Is.Null);
        Assert.That(given.HingeSideIndex, Is.EqualTo(1));
        Assert.That(given.SwingDirectionIndex, Is.EqualTo(0));
    }

    [Test]
    public void V21_IsASupportedVersion()
    {
        ValidationReport r = AiModelValidator.Validate(AiModelReader.Parse(V21Json));
        Assert.That(r.Warnings.Any(i => i.Path == "SchemaVersion"), Is.False, r.ToString());
        Assert.That(r.IsValid, Is.True, r.ToString());
    }

    [Test]
    public void LenientPolicy_TurnsElementErrorsIntoSkipWarnings()
    {
        DetectedBuilding b = AiModelReader.Parse(V21Json);
        b.Levels[0].Walls[0].EndMeters = new DetectedPoint2D { X = 1, Y = 2 };   // zero length
        b.Levels[0].Walls[0].Openings[1].HingeSideIndex = 5;                    // out of range

        ValidationReport strict = AiModelValidator.Validate(b);
        ValidationReport lenient = AiModelValidator.Validate(b, elementIssuesAreWarnings: true);

        Assert.That(strict.IsValid, Is.False);
        Assert.That(lenient.IsValid, Is.True, lenient.ToString());
        Assert.That(lenient.Warnings.Any(w => w.Path.EndsWith("LengthMeters") && w.Message.Contains("will be skipped")), Is.True);
        Assert.That(lenient.Warnings.Any(w => w.Path.EndsWith("HingeSideIndex") && w.Message.Contains("ignored")), Is.True);
    }

    [Test]
    public void LenientPolicy_StillBlocksFileLevelProblems()
    {
        DetectedBuilding b = AiModelReader.Parse(V21Json);
        b.Levels[0].Floor2CeilingMeters = 0.0;

        Assert.That(AiModelValidator.Validate(b, elementIssuesAreWarnings: true).IsValid, Is.False);
    }

    [Test]
    public void ExampleFileForTheAiTeam_IsClean_AndPlansWithNoChanges()
    {
        string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "ai_engine_pknail_results.v2.1.example.json");
        DetectedBuilding b = AiModelReader.Load(path);

        ValidationReport v = AiModelValidator.Validate(b, elementIssuesAreWarnings: true);
        Assert.That(v.IsValid, Is.True, v.ToString());
        Assert.That(v.Warnings.Select(w => w.Path), Is.EqualTo(new[] { "FirstWallCompassDirection" }), v.ToString());

        DirectBuildPlan plan = DirectBuildPlanner.Plan(b);
        Assert.That(plan.WallCount, Is.EqualTo(5));
        Assert.That(plan.OpeningCount, Is.EqualTo(5));
        Assert.That(plan.Adjustments, Is.Empty, "a clean example needs no tidying");
        Assert.That(plan.Warnings, Is.Empty);
        Assert.That(plan.Skipped, Is.Empty);

        // W5's explicit InteriorSide "Left" (walking +Y) is the west room, i.e. -X.
        PlannedWall w5 = plan.Levels[0].Walls.Single(w => w.Label == "W5");
        Assert.That(w5.InteriorNormal.X, Is.EqualTo(-1.0).Within(1e-9));
    }
}
