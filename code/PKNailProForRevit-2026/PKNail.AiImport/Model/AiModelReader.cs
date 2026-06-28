using System.Text.Json;

namespace PKNail.AiImport.Model;

/// <summary>
/// Deserializes the AI-engine JSON (<c>ai_engine_pknail_results.json</c>) into a
/// <see cref="DetectedBuilding"/>. Structural/parse failures throw <see cref="AiModelFormatException"/>;
/// <b>semantic</b> problems (bad dimensions, out-of-range enums) are NOT thrown here — run
/// <see cref="AiModelValidator"/> on the result for those.
/// </summary>
public static class AiModelReader
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
    };

    /// <summary>Parse JSON text into a <see cref="DetectedBuilding"/>.</summary>
    /// <exception cref="AiModelFormatException">JSON is empty, null, or structurally invalid.</exception>
    public static DetectedBuilding Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new AiModelFormatException("AI model JSON is empty.");

        try
        {
            DetectedBuilding? building = JsonSerializer.Deserialize<DetectedBuilding>(json, ReadOptions);
            if (building is null)
                throw new AiModelFormatException("AI model JSON deserialized to null.");
            return building;
        }
        catch (JsonException ex)
        {
            throw new AiModelFormatException($"AI model JSON is not valid: {ex.Message}", ex);
        }
    }

    /// <summary>Read and parse a JSON file.</summary>
    public static DetectedBuilding Load(string path) => Parse(File.ReadAllText(path));

    /// <summary>Serialize a <see cref="DetectedBuilding"/> back to indented JSON (round-trip / debugging).</summary>
    public static string Serialize(DetectedBuilding building) => JsonSerializer.Serialize(building, WriteOptions);
}

/// <summary>Thrown when the AI-engine JSON cannot be parsed into the <c>Detected*</c> model.</summary>
public sealed class AiModelFormatException : Exception
{
    public AiModelFormatException(string message) : base(message) { }
    public AiModelFormatException(string message, Exception inner) : base(message, inner) { }
}
