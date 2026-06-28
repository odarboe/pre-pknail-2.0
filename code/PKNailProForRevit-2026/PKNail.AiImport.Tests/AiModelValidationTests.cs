using NUnit.Framework;
using PKNail.AiImport.Model;

namespace PKNail.AiImport.Tests;

/// <summary>Epic 1 — semantic validation of the Detected* model (errors vs warnings, never throws).</summary>
[TestFixture]
public class AiModelValidationTests
{
    private static DetectedBuilding ValidSingleWallBuilding() => new()
    {
        SchemaVersion = "1.0",
        IsScanFromInterior = true,
        FirstWallCompassDirection = 1, // North
        Levels =
        {
            new DetectedLevel
            {
                Floor2FloorMeters = 2.9,
                Floor2CeilingMeters = 2.6,
                TypicalWindowSillHeightMeters = 0.9,
                TypicalWindowHeadHeightMeters = 2.1,
                Walls = { new DetectedWall { LengthMeters = 5.0, ThicknessMeters = 0.2, CornerAngleRad = 0.0 } },
            },
        },
    };

    [Test]
    public void FrozenContract_IsValid_WithExpectedWarnings()
    {
        DetectedBuilding building = AiModelReader.Load(TestFixtures.FrozenContractPath);
        ValidationReport report = AiModelValidator.Validate(building);

        Assert.That(report.IsValid, Is.True, report.ToString());      // no errors
        Assert.That(report.Warnings, Is.Not.Empty);                   // but it does warn

        // The known v1 quirks surface as warnings (not errors):
        Assert.That(report.Warnings.Any(w => w.Path == "SchemaVersion"), Is.True);
        Assert.That(report.Warnings.Any(w => w.Path == "FirstWallCompassDirection"), Is.True); // DontKnow
        Assert.That(report.Warnings.Any(w => w.Path.Contains("Walls[0].CornerAngleRad")), Is.True); // first-wall angle
    }

    [Test]
    public void CleanBuilding_HasNoIssues()
    {
        ValidationReport report = AiModelValidator.Validate(ValidSingleWallBuilding());
        Assert.That(report.IsValid, Is.True, report.ToString());
        Assert.That(report.WarningCount, Is.EqualTo(0), report.ToString());
    }

    [Test]
    public void NegativeOpeningWidth_IsError()
    {
        DetectedBuilding b = ValidSingleWallBuilding();
        b.Levels[0].Walls[0].Openings.Add(new DetectedOpening
        {
            LocationFromNearEndMeters = 2.5, WidthMeters = -1.0, HeightMeters = 1.4, SillHeightMeters = 0.4,
        });

        ValidationReport report = AiModelValidator.Validate(b);
        Assert.That(report.IsValid, Is.False);
        Assert.That(report.Errors.Any(e => e.Path.EndsWith("WidthMeters")), Is.True, report.ToString());
    }

    [Test]
    public void OutOfRangeEnumIndex_IsError()
    {
        DetectedBuilding b = ValidSingleWallBuilding();
        b.Levels[0].Walls[0].Openings.Add(new DetectedOpening
        {
            LocationFromNearEndMeters = 2.5, WidthMeters = 1.0, HeightMeters = 2.0, SillHeightMeters = 0.0,
            HingeSideIndex = 5, // invalid
        });

        ValidationReport report = AiModelValidator.Validate(b);
        Assert.That(report.Errors.Any(e => e.Path.EndsWith("HingeSideIndex")), Is.True, report.ToString());
    }

    [Test]
    public void OpeningBeyondWallLength_IsError()
    {
        DetectedBuilding b = ValidSingleWallBuilding();
        b.Levels[0].Walls[0].Openings.Add(new DetectedOpening
        {
            LocationFromNearEndMeters = 99.0, WidthMeters = 1.0, HeightMeters = 2.0, SillHeightMeters = 0.0,
        });

        ValidationReport report = AiModelValidator.Validate(b);
        Assert.That(report.Errors.Any(e => e.Path.EndsWith("LocationFromNearEndMeters")), Is.True, report.ToString());
    }

    [Test]
    public void NoLevels_IsError()
    {
        var b = new DetectedBuilding();
        ValidationReport report = AiModelValidator.Validate(b);
        Assert.That(report.Errors.Any(e => e.Path == "Levels"), Is.True, report.ToString());
    }

    [Test]
    public void DoctoredJsonText_ParsesButValidatorFlagsIt()
    {
        // Parse succeeds (structurally valid JSON); the semantic problem is caught by the validator, not the reader.
        string doctored = TestFixtures.FrozenContractJson.Replace("\"WidthMeters\": 1.0", "\"WidthMeters\": -1.0");
        DetectedBuilding b = AiModelReader.Parse(doctored);

        ValidationReport report = AiModelValidator.Validate(b);
        Assert.That(report.IsValid, Is.False);
        Assert.That(report.Errors.Any(e => e.Path.EndsWith("WidthMeters")), Is.True, report.ToString());
    }
}
