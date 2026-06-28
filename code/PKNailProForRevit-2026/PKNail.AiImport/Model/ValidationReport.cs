namespace PKNail.AiImport.Model;

public enum ValidationSeverity
{
    /// <summary>Suspicious but tolerable contract issue; import can still proceed.</summary>
    Warning,

    /// <summary>Hard problem that would make placement impossible/ambiguous; import should halt or skip.</summary>
    Error,
}

/// <summary>A single validation finding with a JSON-ish path (e.g. <c>Levels[0].Walls[2].WidthMeters</c>).</summary>
public sealed record ValidationIssue(ValidationSeverity Severity, string Path, string Message)
{
    public override string ToString() => $"[{Severity}] {Path}: {Message}";
}

/// <summary>
/// The structured result of validating a <see cref="DetectedBuilding"/>. Never throws — the caller
/// decides what to do with <see cref="Errors"/> vs <see cref="Warnings"/> (Jira plan: "flag/skip, don't guess").
/// </summary>
public sealed class ValidationReport
{
    private readonly List<ValidationIssue> _issues = new();

    public IReadOnlyList<ValidationIssue> Issues => _issues;
    public IEnumerable<ValidationIssue> Errors => _issues.Where(i => i.Severity == ValidationSeverity.Error);
    public IEnumerable<ValidationIssue> Warnings => _issues.Where(i => i.Severity == ValidationSeverity.Warning);

    /// <summary>True when there are no <see cref="ValidationSeverity.Error"/> issues.</summary>
    public bool IsValid => !_issues.Any(i => i.Severity == ValidationSeverity.Error);

    public int ErrorCount => _issues.Count(i => i.Severity == ValidationSeverity.Error);
    public int WarningCount => _issues.Count(i => i.Severity == ValidationSeverity.Warning);

    internal void Error(string path, string message) => _issues.Add(new ValidationIssue(ValidationSeverity.Error, path, message));
    internal void Warn(string path, string message) => _issues.Add(new ValidationIssue(ValidationSeverity.Warning, path, message));

    public override string ToString() =>
        _issues.Count == 0 ? "valid (no issues)" : string.Join(Environment.NewLine, _issues);
}
