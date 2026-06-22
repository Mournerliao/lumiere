using System.Text.Json;
using System.Text.Json.Serialization;

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
    private const int CurrentSchemaVersion = 4;
    private const int MinimumSupportedSchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public TargetAwareHdrValidationEvidence? TargetHdrEvidence { get; init; }

    public IReadOnlyList<OutputValidationTargetAppVersionRecord> TargetAppVersions { get; init; } =
        [];

    public bool CoversOutputTarget(OutputTarget target) => TargetsCover(OutputTargetsTested, target);

    public bool CoversProfileOutputTarget(OutputProfileKind profileKind, OutputTarget target) =>
        OutputProfileRecords
            .Where(record => record.ProfileKind == profileKind)
            .Any(record => RecordCoversOutputTarget(record, target));

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public OutputProfileContract ApplyTo(OutputProfileContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return OutputProfileRecords
            .Where(record => record.ProfileKind == contract.Kind)
            .Select(PrepareRecordForReleaseEvidence)
            .Aggregate(contract, (current, record) => current.ApplyValidationRecord(record));
    }

    public OutputProfileContract ApplyTo(OutputProfileContract contract, OutputTarget target)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return OutputProfileRecords
            .Where(record => record.ProfileKind == contract.Kind)
            .Where(record => RecordCoversOutputTarget(record, target))
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

    public static OutputProfileContract ApplyAllTo(
        OutputProfileContract contract,
        IEnumerable<OutputValidationSessionArtifact> artifacts,
        OutputTarget target)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(artifacts);

        return artifacts.Aggregate(
            contract,
            (current, artifact) =>
            {
                ArgumentNullException.ThrowIfNull(artifact);
                return artifact.ApplyTo(current, target);
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
                .Select(evidence => evidence with
                {
                    ArtifactHandlingStatus = DowngradeIncompleteManualStatus(evidence.ArtifactHandlingStatus),
                    VisualMatchStatus = DowngradeIncompleteManualStatus(evidence.VisualMatchStatus),
                    HdrPreservationStatus = DowngradeIncompleteManualStatus(evidence.HdrPreservationStatus),
                    Hdr10MetadataStatus = DowngradeIncompleteManualStatus(evidence.Hdr10MetadataStatus),
                    Detail = $"{evidence.Detail} {detailSuffix}",
                })
                .ToArray(),
        };
    }

    private static OutputCompatibilityEvidenceStatus DowngradeIncompleteManualStatus(
        OutputCompatibilityEvidenceStatus status) =>
        status is OutputCompatibilityEvidenceStatus.Fail or OutputCompatibilityEvidenceStatus.NotApplicable
            ? status
            : OutputCompatibilityEvidenceStatus.Limited;

    private IEnumerable<string> GetMissingManualEvidenceFields()
    {
        if (OutputValidationManualEvidenceFields.IsMissing(Date))
        {
            yield return "date";
        }

        if (OutputValidationManualEvidenceFields.IsMissing(Tester))
        {
            yield return "tester";
        }

        if (OutputValidationManualEvidenceFields.IsMissing(BuildCommit))
        {
            yield return "build/commit";
        }

        if (OutputValidationManualEvidenceFields.IsMissing(WindowsVersion))
        {
            yield return "Windows version";
        }

        if (OutputValidationManualEvidenceFields.IsMissing(Device))
        {
            yield return "device";
        }

        if (OutputValidationManualEvidenceFields.IsMissing(Gpu))
        {
            yield return "GPU";
        }

        if (OutputValidationManualEvidenceFields.IsMissing(DisplaySetup))
        {
            yield return "display setup";
        }

        if (OutputValidationManualEvidenceFields.IsMissing(HdrState))
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

        if (OutputValidationManualEvidenceFields.IsMissing(ResultSummary))
        {
            yield return "result summary";
        }

        if (IsMissing(EvidencePaths))
        {
            yield return "evidence paths";
        }

        if (TargetHdrEvidence is null)
        {
            yield return "target-aware HDR evidence";
        }
        else
        {
            foreach (var field in TargetHdrEvidence.GetMissingFields())
            {
                yield return $"target-aware HDR evidence {field}";
            }
        }
    }

    private static bool IsMissing(IEnumerable<string> values) =>
        !values.Any(value => !OutputValidationManualEvidenceFields.IsMissing(value));

    private bool RecordCoversOutputTarget(OutputProfileValidationRecord record, OutputTarget target)
    {
        ArgumentNullException.ThrowIfNull(record);

        return record.OutputTargetsCovered.Count == 0
            ? CoversOutputTarget(target)
            : TargetsCover(record.OutputTargetsCovered, target);
    }

    private static bool TargetsCover(IEnumerable<string> values, OutputTarget target) =>
        values.Any(value =>
            TryParseOutputTarget(value, out var parsed)
            && (parsed == target
                || parsed == OutputTarget.Both && target is OutputTarget.Clipboard or OutputTarget.Folder));

    private static bool TryParseOutputTarget(string? value, out OutputTarget target)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            target = default;
            return false;
        }

        var normalized = value.Trim();
        if (normalized.Equals("Clipboard", StringComparison.OrdinalIgnoreCase))
        {
            target = OutputTarget.Clipboard;
            return true;
        }

        if (normalized.Equals("Folder", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("File", StringComparison.OrdinalIgnoreCase))
        {
            target = OutputTarget.Folder;
            return true;
        }

        if (normalized.Equals("Both", StringComparison.OrdinalIgnoreCase))
        {
            target = OutputTarget.Both;
            return true;
        }

        target = default;
        return false;
    }
}

public sealed record OutputValidationTargetAppVersionRecord(
    string Name,
    string Version);

public sealed record TargetAwareHdrValidationEvidence(
    string TargetDisplayName,
    int? Left,
    int? Top,
    int Width,
    int Height,
    string MatchKind,
    string HdrState,
    string? ColorSpace,
    string Detail)
{
    public IEnumerable<string> GetMissingFields()
    {
        if (OutputValidationManualEvidenceFields.IsMissing(TargetDisplayName))
        {
            yield return "target display";
        }

        if (Width <= 0 || Height <= 0)
        {
            yield return "target bounds";
        }

        if (OutputValidationManualEvidenceFields.IsMissing(MatchKind))
        {
            yield return "match kind";
        }

        if (OutputValidationManualEvidenceFields.IsMissing(HdrState))
        {
            yield return "HDR state";
        }

        if (OutputValidationManualEvidenceFields.IsMissing(Detail))
        {
            yield return "detail";
        }
    }

}

internal static class OutputValidationManualEvidenceFields
{
    public static bool IsMissing(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed)
            || trimmed.Contains("REPLACE_WITH_", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("Template only", StringComparison.OrdinalIgnoreCase);
    }
}
