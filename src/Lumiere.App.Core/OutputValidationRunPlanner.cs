using Lumiere.Graphics.Output;

namespace Lumiere.App;

public sealed record OutputValidationRunPlan(
    IReadOnlyList<string> CoveredDisplayTopologies,
    IReadOnlyList<string> MissingDisplayTopologies,
    IReadOnlyList<string> MissingViewerTargets,
    IReadOnlyList<string> MissingEntryPoints,
    IReadOnlyList<string> MissingOutputTargets)
{
    public string? CreateNextWindowsRunRecommendation()
    {
        var topology = MissingDisplayTopologies.FirstOrDefault();
        var entryPoint = MissingEntryPoints.FirstOrDefault() ?? "Main panel";
        var outputTarget = MissingOutputTargets.FirstOrDefault() ?? "Folder";
        var viewers = MissingViewerTargets.Count == 0
            ? "the named HDR10 viewer set"
            : FormatEvidenceList(MissingViewerTargets, fallback: "the named HDR10 viewer set");

        if (topology is null
            && MissingViewerTargets.Count == 0
            && MissingEntryPoints.Count == 0
            && MissingOutputTargets.Count == 0)
        {
            return null;
        }

        var topologyClause = topology is null
            ? "a currently unblocked topology"
            : topology;
        return $"Next Windows run: use {entryPoint}, record the {topologyClause} topology, validate {outputTarget} output, and test HDR10 viewer evidence for {viewers}.";
    }

    private static string FormatEvidenceList(
        IEnumerable<string> values,
        string fallback,
        int maxItems = 3)
    {
        var distinctValues = values
            .Where(OutputValidationRunPlanner.IsRecordedEvidenceValue)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (distinctValues.Length == 0)
        {
            return fallback;
        }

        if (distinctValues.Length <= maxItems)
        {
            return string.Join(", ", distinctValues);
        }

        return $"{string.Join(", ", distinctValues.Take(maxItems))}, +{distinctValues.Length - maxItems} more";
    }
}

public static class OutputValidationRunPlanner
{
    private sealed record DisplayTopologyRequirement(
        string Label,
        Func<IReadOnlyList<OutputValidationSessionArtifact>, bool> IsCovered);

    private static readonly DisplayTopologyRequirement[] DisplayTopologyRequirements =
    [
        new(
            "Single HDR-capable display with Windows HDR enabled",
            artifacts => HasTopologyEvidence(artifacts, "single hdr-capable display with windows hdr enabled", "single hdr enabled", "single hdr-on")
                || HasSingleDisplayEvidence(artifacts, requiresHdrToken: true, "enabled", "active", "on")),
        new(
            "Single HDR-capable display with Windows HDR disabled",
            artifacts => HasTopologyEvidence(artifacts, "single hdr-capable display with windows hdr disabled", "single hdr disabled", "single hdr-off")
                || HasSingleDisplayEvidence(artifacts, requiresHdrToken: true, "disabled", "inactive", "off")),
        new(
            "Single SDR-only display",
            artifacts => HasTopologyEvidence(artifacts, "single sdr-only display", "single sdr")
                || HasSingleDisplayEvidence(artifacts, requiresHdrToken: false, "sdr-only", "sdr target")),
        new(
            "Mixed HDR + SDR multi-monitor desktop",
            artifacts => HasTopologyEvidence(artifacts, "mixed hdr + sdr", "mixed hdr/sdr", "hdr primary, sdr secondary", "sdr primary, hdr secondary")
                || artifacts.Any(artifact => IsMultiDisplayArtifact(artifact) && HasRecordedText(artifact.DisplaySetup, "hdr") && HasRecordedText(artifact.DisplaySetup, "sdr"))),
        new(
            "Multi-monitor same-DPI",
            artifacts => HasTopologyEvidence(artifacts, "multi-monitor same-dpi", "multi monitor same dpi", "same dpi")
                || artifacts.Any(artifact => IsMultiDisplayArtifact(artifact) && CountRecordedDpiScales(artifact) == 1)),
        new(
            "Multi-monitor mixed-DPI",
            artifacts => HasTopologyEvidence(artifacts, "multi-monitor mixed-dpi", "multi monitor mixed dpi", "mixed dpi")
                || artifacts.Any(artifact => IsMultiDisplayArtifact(artifact) && CountRecordedDpiScales(artifact) > 1)),
    ];

    private static readonly string[] RequiredCaptureEntryPoints =
    [
        "Main panel",
        "Tray menu",
        "Global hotkey",
    ];

    private static readonly string[] RequiredOutputTargets =
    [
        "Folder",
        "Clipboard",
        "Both",
    ];

    public static OutputValidationRunPlan Create(
        IReadOnlyList<OutputValidationSessionArtifact> artifacts,
        OutputProfileContract? viewerProfile = null)
    {
        ArgumentNullException.ThrowIfNull(artifacts);

        var profile = viewerProfile ?? OutputProfileContract.Hdr10Pq;
        return new OutputValidationRunPlan(
            CollectCoveredDisplayTopologies(artifacts),
            CollectMissingDisplayTopologies(artifacts),
            CollectMissingViewerTargets(profile, artifacts),
            CollectMissingRequiredValues(artifacts.SelectMany(artifact => artifact.EntryPointsTested), RequiredCaptureEntryPoints),
            CollectMissingRequiredValues(artifacts.SelectMany(artifact => artifact.OutputTargetsTested), RequiredOutputTargets));
    }

    public static bool IsRecordedEvidenceValue(string? value)
    {
        var trimmed = value?.Trim();
        return !string.IsNullOrWhiteSpace(trimmed)
            && !trimmed.Contains("REPLACE_WITH_", StringComparison.OrdinalIgnoreCase)
            && !trimmed.StartsWith("Template only", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> CollectCoveredDisplayTopologies(
        IReadOnlyList<OutputValidationSessionArtifact> artifacts) =>
        DisplayTopologyRequirements
            .Where(requirement => requirement.IsCovered(artifacts))
            .Select(requirement => requirement.Label)
            .ToArray();

    private static IReadOnlyList<string> CollectMissingDisplayTopologies(
        IReadOnlyList<OutputValidationSessionArtifact> artifacts) =>
        DisplayTopologyRequirements
            .Where(requirement => !requirement.IsCovered(artifacts))
            .Select(requirement => requirement.Label)
            .ToArray();

    private static IReadOnlyList<string> CollectMissingViewerTargets(
        OutputProfileContract profile,
        IReadOnlyList<OutputValidationSessionArtifact> artifacts)
    {
        var covered = artifacts
            .SelectMany(artifact => artifact.OutputProfileRecords
                .Where(record => record.ProfileKind == profile.Kind)
                .SelectMany(record => record.ViewerEvidence.Select(viewer => viewer.Name)))
            .Where(IsRecordedEvidenceValue)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return profile.ViewerEvidence
            .Select(viewer => viewer.Name)
            .Where(name => !covered.Contains(name))
            .ToArray();
    }

    private static IReadOnlyList<string> CollectMissingRequiredValues(
        IEnumerable<string> actualValues,
        IEnumerable<string> requiredValues)
    {
        var covered = actualValues
            .Where(IsRecordedEvidenceValue)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return requiredValues
            .Where(value => !covered.Contains(value))
            .ToArray();
    }

    private static bool HasTopologyEvidence(
        IReadOnlyList<OutputValidationSessionArtifact> artifacts,
        params string[] needles) =>
        artifacts.Any(artifact =>
            needles.Any(needle =>
                HasRecordedText(artifact.DisplaySetup, needle)
                || HasRecordedText(artifact.HdrState, needle)));

    private static bool HasSingleDisplayEvidence(
        IReadOnlyList<OutputValidationSessionArtifact> artifacts,
        bool requiresHdrToken,
        params string[] hdrStateNeedles) =>
        artifacts.Any(artifact =>
            IsSingleDisplayArtifact(artifact)
            && (!requiresHdrToken || HasRecordedText(artifact.DisplaySetup, "hdr"))
            && hdrStateNeedles.Any(needle =>
                HasRecordedText(artifact.DisplaySetup, needle)
                || HasRecordedText(artifact.HdrState, needle)));

    private static bool IsSingleDisplayArtifact(OutputValidationSessionArtifact artifact) =>
        HasRecordedText(artifact.DisplaySetup, "single")
        || HasRecordedText(artifact.DisplaySetup, "1 display")
        || HasRecordedText(artifact.DisplaySetup, "one display");

    private static bool IsMultiDisplayArtifact(OutputValidationSessionArtifact artifact) =>
        HasRecordedText(artifact.DisplaySetup, "multi")
        || HasRecordedText(artifact.DisplaySetup, "2 displays")
        || HasRecordedText(artifact.DisplaySetup, "two displays")
        || HasRecordedText(artifact.DisplaySetup, "secondary");

    private static int CountRecordedDpiScales(OutputValidationSessionArtifact artifact) =>
        artifact.DpiScales
            .Where(IsRecordedEvidenceValue)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

    private static bool HasRecordedText(string? value, string needle) =>
        IsRecordedEvidenceValue(value)
        && value!.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
}
