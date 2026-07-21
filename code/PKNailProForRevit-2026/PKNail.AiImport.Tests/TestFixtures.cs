using NUnit.Framework;

namespace PKNail.AiImport.Tests;

/// <summary>Shared helpers for locating the frozen JSON contract fixture copied to the test output.</summary>
internal static class TestFixtures
{
    public static string FrozenContractPath =>
        Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "ai_engine_pknail_results.json");

    public static string FrozenContractJson => File.ReadAllText(FrozenContractPath);

    /// <summary>Known-good demo (clean 5x4 m room with correct interior angles) verified end-to-end in Revit 2026.</summary>
    public static string DemoPath =>
        Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "ai_engine_pknail_results.demo.json");
}
