namespace PKNail.AiImport;

/// <summary>
/// Assembly marker + metadata for the Revit-free PKNAIL 2.0 AI-engine import layer.
/// <para>
/// This library deserializes <c>ai_engine_pknail_results.json</c> into <c>Detected*</c> DTOs
/// (Epic 1) and maps them onto PKNAIL parameter objects (Epic 2). It has NO dependency on
/// Revit, WinForms, or the PKNailForRevit plugin, so it is unit-testable in isolation.
/// The Revit-touching <c>AiModelImporter</c> bridge lives in the PKNailForRevit project.
/// </para>
/// </summary>
public static class AiImportLibrary
{
    /// <summary>
    /// The AI-engine JSON contract version this library targets. The frozen v1 sample is
    /// <c>docs/ai_engine_pknail_results.json</c>. See <c>docs/PKNAIL_2.0_JiraPlan.md</c> (Q10):
    /// a <c>SchemaVersion</c> field will be validated against this once the AI engine emits it.
    /// </summary>
    public const string TargetContractVersion = "1.0";

    /// <summary>
    /// Every contract version this library reads: the frozen v1 sample, the v2 absolute-endpoint
    /// contract and the additive v2.1 (Epic 13: optional ids/heights/comments, no loop-order rule).
    /// </summary>
    public static readonly IReadOnlyList<string> SupportedContractVersions = new[] { "1.0", "2.0", "2.1" };
}
