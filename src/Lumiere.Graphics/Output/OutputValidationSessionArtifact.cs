using System.Text.Json;

namespace Lumiere.Graphics.Output;

/// <summary>
/// Durable JSON payload for a Windows output validation session.
/// </summary>
public sealed record OutputValidationSessionArtifact(
    string Date,
    string Tester,
    string BuildCommit,
    string WindowsVersion,
    string Device,
    string Gpu,
    string DisplaySetup,
    string HdrState,
    IReadOnlyList<string> DpiScales,
    IReadOnlyList<string> EntryPointsTested,
    IReadOnlyList<string> OutputTargetsTested,
    IReadOnlyList<string> TargetAppsTested,
    IReadOnlyList<string> ChecklistIdsCovered,
    string ResultSummary,
    IReadOnlyList<string> EvidencePaths,
    IReadOnlyList<string> KnownLimitations,
    IReadOnlyList<string> FollowUpIssuesOrStories,
    IReadOnlyList<OutputProfileValidationRecord> OutputProfileRecords)
{
    private const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public OutputProfileContract ApplyTo(OutputProfileContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return OutputProfileRecords
            .Where(record => record.ProfileKind == contract.Kind)
            .Aggregate(contract, (current, record) => current.ApplyValidationRecord(record));
    }

    public static OutputValidationSessionArtifact FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Validation session artifact JSON must be provided.", nameof(json));
        }

        var artifact = JsonSerializer.Deserialize<OutputValidationSessionArtifact>(json, JsonOptions)
            ?? throw new InvalidOperationException("Validation session artifact JSON is invalid or empty.");
        if (artifact.SchemaVersion != CurrentSchemaVersion)
        {
            throw new InvalidOperationException(
                $"Unsupported validation session artifact schema version {artifact.SchemaVersion}.");
        }

        return artifact;
    }
}
