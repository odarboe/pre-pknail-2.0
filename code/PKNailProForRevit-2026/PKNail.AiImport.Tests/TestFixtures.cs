using NUnit.Framework;

namespace PKNail.AiImport.Tests;

/// <summary>Shared helpers for locating the frozen JSON contract fixture copied to the test output.</summary>
internal static class TestFixtures
{
    public static string FrozenContractPath =>
        Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "ai_engine_pknail_results.json");

    public static string FrozenContractJson => File.ReadAllText(FrozenContractPath);
}
