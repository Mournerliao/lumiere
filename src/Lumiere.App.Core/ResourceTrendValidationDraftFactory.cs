using Lumiere.Capture;
using Lumiere.Graphics.Hdr;
using Lumiere.Graphics.Output;

namespace Lumiere.App;

public static class ResourceTrendValidationDraftFactory
{
    public static string Create(
        ResourceTrendValidationDraftRequest request,
        string validationWorkspacePath,
        DateTimeOffset now,
        string template,
        ResourceTrendSummaryArtifact? latestSummary = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(validationWorkspacePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(template);

        var localNow = now.ToLocalTime();
        var date = localNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var buildLabel = NormalizeBuildVersion(request.BuildVersion);
        var outputDirectory = Path.Combine(validationWorkspacePath.Trim(), "resource-trends");
        var command = string.IsNullOrWhiteSpace(request.ResourceTrendCommand)
            ? ResourceTrendValidationCommandProjection.Create(
                Path.Combine(validationWorkspacePath.Trim(), FileOutputValidationArtifactSource.ResourceTrendScriptFileName),
                validationWorkspacePath,
                request.ProcessId)
            : request.ResourceTrendCommand.Trim();

        var summary = latestSummary;
        var csvPath = summary?.CsvPath
            ?? Path.Combine(outputDirectory, $"resource-trend-Lumiere.App-pid{request.ProcessId}-REPLACE_WITH_TIMESTAMP.csv");
        var summaryPath = summary?.SummaryPath
            ?? Path.Combine(outputDirectory, $"resource-trend-Lumiere.App-pid{request.ProcessId}-REPLACE_WITH_TIMESTAMP-summary.json");
        var durationSeconds = summary is null || summary.DurationSeconds <= 0
            ? "900"
            : summary.DurationSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var sampleIntervalSeconds = summary is null || summary.SampleIntervalSeconds <= 0
            ? "5"
            : summary.SampleIntervalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);

        return ApplyMetricRows(template, summary)
            .Replace("- Date:", $"- Date: {date}", StringComparison.Ordinal)
            .Replace("- Tester:", "- Tester: REPLACE_WITH_TESTER_NAME", StringComparison.Ordinal)
            .Replace("- Build / commit:", $"- Build / commit: {CreateBuildPlaceholder(buildLabel, request.BuildVersion)}", StringComparison.Ordinal)
            .Replace("- Windows version:", $"- Windows version: {CreateWindowsVersionPlaceholder()}", StringComparison.Ordinal)
            .Replace("- Device:", "- Device: REPLACE_WITH_DEVICE_MODEL", StringComparison.Ordinal)
            .Replace("- GPU:", $"- GPU: {CreateGpuPlaceholder(request.CurrentSessionHint)}", StringComparison.Ordinal)
            .Replace("- Display setup:", $"- Display setup: {CreateDisplaySetupPlaceholder(request.SessionState.Target, request.CurrentSessionHint)}", StringComparison.Ordinal)
            .Replace("- HDR state:", $"- HDR state: {CreateHdrStatePlaceholder(request.SessionState.Readiness)}", StringComparison.Ordinal)
            .Replace("- DPI scale(s):", $"- DPI scale(s): {CreateDpiScalePlaceholder(request.CurrentSessionHint)}", StringComparison.Ordinal)
            .Replace("- Lumiere process ID:", $"- Lumiere process ID: {CreateProcessIdPlaceholder(request.ProcessId, summary)}", StringComparison.Ordinal)
            .Replace("- Output configuration:", $"- Output configuration: {FormatOutputTarget(request.OutputTarget)}", StringComparison.Ordinal)
            .Replace("- Command:", $"- Command: {command ?? "REPLACE_WITH_RESOURCE_TREND_COMMAND"}", StringComparison.Ordinal)
            .Replace("- Duration seconds:", $"- Duration seconds: {durationSeconds}", StringComparison.Ordinal)
            .Replace("- Sample interval seconds:", $"- Sample interval seconds: {sampleIntervalSeconds}", StringComparison.Ordinal)
            .Replace("- Output directory:", $"- Output directory: {outputDirectory}", StringComparison.Ordinal)
            .Replace("- CSV path:", $"- CSV path: {csvPath}", StringComparison.Ordinal)
            .Replace("- Summary JSON path:", $"- Summary JSON path: {summaryPath}", StringComparison.Ordinal)
            .Replace("- GPU counter availability:", $"- GPU counter availability: {CreateGpuCounterAvailability(summary)}", StringComparison.Ordinal)
            .Replace("- `REL-STAB-01`:", "- `REL-STAB-01`: Repeated capture/output loop stability session", StringComparison.Ordinal)
            .Replace("- `REL-STAB-02`:", "- `REL-STAB-02`: Private bytes / handles / GPU trend sampler evidence", StringComparison.Ordinal)
            .Replace("- `REL-STAB-03`:", "- `REL-STAB-03`: Duplicate capture rejection during active session", StringComparison.Ordinal)
            .Replace("- `REL-STAB-04`:", "- `REL-STAB-04`: Slow or failing clipboard/file target recovery", StringComparison.Ordinal)
            .Replace("- Public gate `Long-run lifecycle evidence`:", $"- Public gate `Long-run lifecycle evidence`: {CreatePublicGatePlaceholder(summary)}", StringComparison.Ordinal)
            .Replace("- Session classification: PASS / PASS with limitation / FAIL / NOT RUN", $"- Session classification: {CreateSessionClassificationPlaceholder(summary)}", StringComparison.Ordinal)
            .Replace("- Release impact:", "- Release impact: REPLACE_WITH_RELEASE_IMPACT", StringComparison.Ordinal)
            .Replace("- Known limitations:", "- Known limitations: Draft created from current Lumiere session context. Replace with observed limitations after the run.", StringComparison.Ordinal)
            .Replace("- Warm-up or stabilization notes:", $"- Warm-up or stabilization notes: {CreateSummaryScopeNote(request.ProcessId, summary)}", StringComparison.Ordinal)
            .Replace("- Follow-up stories / issues:", "- Follow-up stories / issues: 12-3, 10-3, 11-3, 13-2", StringComparison.Ordinal);
    }

    private static string ApplyMetricRows(string template, ResourceTrendSummaryArtifact? summary)
    {
        if (summary is null)
        {
            return template;
        }

        return template
            .Replace(CreateMetricRow("Handles"), CreateMetricRow("Handles", summary.TryGetMetric("handles")), StringComparison.Ordinal)
            .Replace(CreateMetricRow("Private bytes"), CreateMetricRow("Private bytes", summary.TryGetMetric("privateBytes")), StringComparison.Ordinal)
            .Replace(CreateMetricRow("Threads"), CreateMetricRow("Threads", summary.TryGetMetric("threads")), StringComparison.Ordinal)
            .Replace(CreateMetricRow("Working set bytes"), CreateMetricRow("Working set bytes", summary.TryGetMetric("workingSetBytes")), StringComparison.Ordinal)
            .Replace(CreateMetricRow("Paged memory bytes"), CreateMetricRow("Paged memory bytes", summary.TryGetMetric("pagedMemoryBytes")), StringComparison.Ordinal)
            .Replace(CreateMetricRow("GPU dedicated usage bytes"), CreateMetricRow("GPU dedicated usage bytes", summary.TryGetMetric("gpuDedicatedUsageBytes")), StringComparison.Ordinal)
            .Replace(CreateMetricRow("GPU shared usage bytes"), CreateMetricRow("GPU shared usage bytes", summary.TryGetMetric("gpuSharedUsageBytes")), StringComparison.Ordinal)
            .Replace(CreateMetricRow("GPU total committed bytes"), CreateMetricRow("GPU total committed bytes", summary.TryGetMetric("gpuTotalCommittedBytes")), StringComparison.Ordinal);
    }

    private static string CreateMetricRow(string label) =>
        $"| {label} |  |  |  |  |  | PASS / PASS with limitation / FAIL / NOT RUN |  |";

    private static string CreateMetricRow(string label, ResourceTrendMetricSummary? metric)
    {
        if (metric is null)
        {
            return CreateMetricRow(label);
        }

        return string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"| {label} | {metric.Baseline} | {metric.Final} | {metric.Delta} | {metric.Min} | {metric.Max} | REPLACE_WITH_PASS_FAIL_LIMITATION | Imported from sampler summary. |");
    }

    private static string CreateGpuCounterAvailability(ResourceTrendSummaryArtifact? summary)
    {
        if (summary is null)
        {
            return "REPLACE_WITH_AVAILABLE_OR_LIMITATION";
        }

        return summary.HasGpuCounterSamples
            ? "GPU counters present in latest sampler summary"
            : "PASS with limitation candidate: latest sampler summary did not report non-zero GPU counters; confirm whether GPU counters were unavailable.";
    }

    private static string CreatePublicGatePlaceholder(ResourceTrendSummaryArtifact? summary) =>
        summary is null
            ? "REPLACE_WITH_PASS_FAIL_LIMITATION"
            : $"REPLACE_WITH_PASS_FAIL_LIMITATION after reviewing {summary.SampleCount} imported sampler samples";

    private static string CreateSessionClassificationPlaceholder(ResourceTrendSummaryArtifact? summary) =>
        summary is null
            ? "NOT RUN"
            : "REPLACE_WITH_PASS_FAIL_LIMITATION (sampler summary imported; human review required)";

    private static string NormalizeBuildVersion(string? buildVersion)
    {
        var trimmed = buildVersion?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return "Lumiere unknown build";
        }

        return trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase)
            ? $"Lumiere {trimmed}"
            : $"Lumiere v{trimmed}";
    }

    private static string CreateBuildPlaceholder(string buildLabel, string? buildVersion)
    {
        var commit = ExtractBuildCommit(buildVersion);
        return commit is null
            ? $"REPLACE_WITH_GIT_COMMIT (app version {buildLabel})"
            : $"{commit} (app version {buildLabel})";
    }

    private static string CreateWindowsVersionPlaceholder() =>
        $"REPLACE_WITH_WINDOWS_VERSION (current session: {Environment.OSVersion.VersionString})";

    private static string CreateGpuPlaceholder(OutputValidationCurrentSessionHint? currentSessionHint)
    {
        var gpu = currentSessionHint?.Gpu?.Trim();
        return string.IsNullOrWhiteSpace(gpu)
            ? "REPLACE_WITH_GPU_MODEL_AND_DRIVER"
            : $"REPLACE_WITH_GPU_MODEL_AND_DRIVER (current session: {gpu})";
    }

    private static string CreateDisplaySetupPlaceholder(CaptureTarget? target, OutputValidationCurrentSessionHint? currentSessionHint)
    {
        var currentSession = currentSessionHint?.DisplaySetup?.Trim();
        var baseValue = target is null
            ? "REPLACE_WITH_FULL_DISPLAY_SETUP"
            : $"REPLACE_WITH_FULL_DISPLAY_SETUP (active target: {target.DisplayName})";
        return string.IsNullOrWhiteSpace(currentSession)
            ? baseValue
            : $"{baseValue} (current session: {currentSession})";
    }

    private static string CreateHdrStatePlaceholder(PreviewReadinessStatus readiness) =>
        $"REPLACE_WITH_OBSERVED_WINDOWS_HDR_STATE (current session: {readiness.UserMessage})";

    private static string CreateDpiScalePlaceholder(OutputValidationCurrentSessionHint? currentSessionHint)
    {
        var values = currentSessionHint?.DpiScales
            ?.Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToArray();
        return values is { Length: > 0 }
            ? $"REPLACE_WITH_DPI_SCALE (current session: {string.Join(", ", values)})"
            : "REPLACE_WITH_DPI_SCALE";
    }

    private static string CreateProcessIdPlaceholder(int processId, ResourceTrendSummaryArtifact? summary)
    {
        var baseValue = processId > 0
            ? $"{processId} (current session)"
            : "REPLACE_WITH_LUMIERE_PROCESS_ID";
        if (summary is null)
        {
            return baseValue;
        }

        if (summary.MatchesProcessId(processId))
        {
            return $"{baseValue}; imported summary matches PID {summary.ProcessId}";
        }

        var importedPid = summary.ProcessId > 0
            ? summary.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : "missing";
        return $"{baseValue}; scope warning: imported summary PID {importedPid} does not match current PID {processId}";
    }

    private static string CreateSummaryScopeNote(int processId, ResourceTrendSummaryArtifact? summary)
    {
        if (summary is null)
        {
            return "REPLACE_WITH_WARMUP_OR_STABILIZATION_NOTES";
        }

        return summary.MatchesProcessId(processId)
            ? $"Imported sampler summary matches current PID {summary.ProcessId}; still verify the run covered the intended 50+ / 100+ cycle plan."
            : $"Scope warning: imported sampler summary PID {summary.ProcessId} does not match current PID {processId}. Verify this summary belongs to the intended validation run before counting it.";
    }

    private static string FormatOutputTarget(OutputTarget outputTarget) =>
        outputTarget switch
        {
            OutputTarget.Clipboard => "Clipboard",
            OutputTarget.Folder => "Folder",
            OutputTarget.Both => "Both",
            _ => "Folder",
        };

    private static string? ExtractBuildCommit(string? buildVersion)
    {
        var trimmed = buildVersion?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return null;
        }

        var plusIndex = trimmed.IndexOf('+');
        if (plusIndex >= 0 && plusIndex < trimmed.Length - 1)
        {
            return NormalizeCommitToken(trimmed[(plusIndex + 1)..]);
        }

        return null;
    }

    private static string? NormalizeCommitToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var filtered = new string(value.Trim().Where(Uri.IsHexDigit).ToArray());
        return filtered.Length >= 7
            ? filtered.ToLowerInvariant()
            : null;
    }
}
