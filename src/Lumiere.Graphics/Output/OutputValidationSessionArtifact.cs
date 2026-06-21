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
    private const int CurrentSchemaVersion = 2;
    private const int MinimumSupportedSchemaVersion = 1;

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
            .Select(PrepareRecordForReleaseEvidence)
            .Aggregate(contract, (current, record) => current.ApplyValidationRecord(record));
    }

    public static OutputProfileContract ApplyAllTo(
        OutputProfileContract contract,
        IEnumerable<OutputValidationSessionArtifact> artifacts)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(artifacts);

        return artifacts.Aggregate(
            contract,
            (current, artifact) =>
            {
                ArgumentNullException.ThrowIfNull(artifact);
                return artifact.ApplyTo(current);
            });
    }

    public static OutputValidationSessionArtifact FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Validation session artifact JSON must be provided.", nameof(json));
        }

        var artifact = JsonSerializer.Deserialize<OutputValidationSessionArtifact>(json, JsonOptions)
            ?? throw new InvalidOperationException("Validation session artifact JSON is invalid or empty.");
        if (artifact.SchemaVersion is < MinimumSupportedSchemaVersion or > CurrentSchemaVersion)
        {
            throw new InvalidOperationException(
                $"Unsupported validation session artifact schema version {artifact.SchemaVersion}.");
        }

        return artifact;
    }

    private OutputProfileValidationRecord PrepareRecordForReleaseEvidence(OutputProfileValidationRecord record)
    {
        var missingFields = GetMissingManualEvidenceFields().ToArray();
        if (missingFields.Length == 0)
        {
            return record;
        }

        var detailSuffix = $"Validation session incomplete; missing {string.Join(", ", missingFields)}.";
        return record with
        {
            EvidenceSource = OutputValidationEvidenceSource.IncompleteManualSession,
            FormatContract = null,
            ViewerEvidence = record.ViewerEvidence
                .Select(evidence => evidence with { Detail = $"{evidence.Detail} {detailSuffix}" })
                .ToArray(),
        };
    }

    private IEnumerable<string> GetMissingManualEvidenceFields()
    {
        if (string.IsNullOrWhiteSpace(Date))
        {
            yield return "date";
        }

        if (string.IsNullOrWhiteSpace(Tester))
        {
            yield return "tester";
        }

        if (string.IsNullOrWhiteSpace(BuildCommit))
        {
            yield return "build/commit";
        }

        if (string.IsNullOrWhiteSpace(WindowsVersion))
        {
            yield return "Windows version";
        }

        if (string.IsNullOrWhiteSpace(Device))
        {
            yield return "device";
        }

        if (string.IsNullOrWhiteSpace(Gpu))
        {
            yield return "GPU";
        }

        if (string.IsNullOrWhiteSpace(DisplaySetup))
        {
            yield return "display setup";
        }

        if (string.IsNullOrWhiteSpace(HdrState))
        {
            yield return "HDR state";
        }

        if (IsMissing(DpiScales))
        {
            yield return "DPI scales";
        }

        if (IsMissing(EntryPointsTested))
        {
            yield return "entry points tested";
        }

        if (IsMissing(OutputTargetsTested))
        {
            yield return "output targets tested";
        }

        if (IsMissing(TargetAppsTested))
        {
            yield return "target apps tested";
        }

        if (IsMissing(ChecklistIdsCovered))
        {
            yield return "checklist IDs covered";
        }

        if (string.IsNullOrWhiteSpace(ResultSummary))
        {
            yield return "result summary";
        }

        if (IsMissing(EvidencePaths))
        {
            yield return "evidence paths";
        }
    }

    private static bool IsMissing(IEnumerable<string> values) =>
        !values.Any(value => !string.IsNullOrWhiteSpace(value));
}
