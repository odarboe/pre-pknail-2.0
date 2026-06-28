using NUnit.Framework;

namespace PKNail.AiImport.Tests;

/// <summary>
/// Epic 9a smoke tests: prove the net8.0 test gate runs, the Revit-free PKNail.AiImport library
/// is referenceable, and the frozen JSON contract fixture is wired in for Epic 1+ tests.
/// These are infrastructure checks — domain tests (DTO deserialization, mapping) arrive in Epics 1–2.
/// </summary>
[TestFixture]
public class FoundationSmokeTests
{
    [Test]
    public void TestHarness_Runs()
    {
        Assert.That(2 + 2, Is.EqualTo(4));
    }

    [Test]
    public void AiImportLibrary_IsReferenceable()
    {
        Assert.That(AiImportLibrary.TargetContractVersion, Is.EqualTo("1.0"));
    }

    [Test]
    public void FrozenContractFixture_IsPresentAndNonEmpty()
    {
        string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "ai_engine_pknail_results.json");
        Assert.That(File.Exists(path), Is.True, $"Frozen JSON contract fixture missing at: {path}");

        string json = File.ReadAllText(path);
        Assert.That(json, Does.Contain("\"Levels\""), "Fixture does not look like the AI-engine contract.");
        Assert.That(json, Does.Contain("\"LengthMeters\""), "Fixture is missing expected wall fields.");
    }
}
