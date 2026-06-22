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
        string template)
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

        return template
            .Replace("- Date:", $"- Date: {date}", StringComparison.Ordinal)
            .Replace("- Tester:", "- Tester: REPLACE_WITH_TESTER_NAME", StringComparison.Ordinal)
            .Replace("- Build / commit:", $"- Build / commit: {CreateBuildPlaceholder(buildLabel, request.BuildVersion)}", StringComparison.Ordinal)
            .Replace("- Windows version:", $"- Windows version: {CreateWindowsVersionPlaceholder()}", StringComparison.Ordinal)
            .Replace("- Device:", "- Device: REPLACE_WITH_DEVICE_MODEL", StringComparison.Ordinal)
            .Replace("- GPU:", $"- GPU: {CreateGpuPlaceholder(request.CurrentSessionHint)}", StringComparison.Ordinal)
            .Replace("- Display setup:", $"- Display setup: {CreateDisplaySetupPlaceholder(request.SessionState.Target, request.CurrentSessionHint)}", StringComparison.Ordinal)
            .Replace("- HDR state:", $"- HDR state: {CreateHdrStatePlaceholder(request.SessionState.Readiness)}", StringComparison.Ordinal)
            .Replace("- DPI scale(s):", $"- DPI scale(s): {CreateDpiScalePlaceholder(request.CurrentSessionHint)}", StringComparison.Ordinal)
            .Replace("- Lumiere process ID:", $"- Lumiere process ID: {CreateProcessIdPlaceholder(request.ProcessId)}", StringComparison.Ordinal)
            .Replace("- Output configuration:", $"- Output configuration: {FormatOutputTarget(request.OutputTarget)}", StringComparison.Ordinal)
            .Replace("- Command:", $"- Command: {command ?? "REPLACE_WITH_RESOURCE_TREND_COMMAND"}", StringComparison.Ordinal)
            .Replace("- Duration seconds:", "- Duration seconds: 900", StringComparison.Ordinal)
            .Replace("- Sample interval seconds:", "- Sample interval seconds: 5", StringComparison.Ordinal)
            .Replace("- Output directory:", $"- Output directory: {outputDirectory}", StringComparison.Ordinal)
            .Replace("- CSV path:", $"- CSV path: {Path.Combine(outputDirectory, $"resource-trend-Lumiere.App-pid{request.ProcessId}-REPLACE_WITH_TIMESTAMP.csv")}", StringComparison.Ordinal)
            .Replace("- Summary JSON path:", $"- Summary JSON path: {Path.Combine(outputDirectory, $"resource-trend-Lumiere.App-pid{request.ProcessId}-REPLACE_WITH_TIMESTAMP-summary.json")}", StringComparison.Ordinal)
            .Replace("- GPU counter availability:", "- GPU counter availability: REPLACE_WITH_AVAILABLE_OR_LIMITATION", StringComparison.Ordinal)
            .Replace("- `REL-STAB-01`:", "- `REL-STAB-01`: Repeated capture/output loop stability session", StringComparison.Ordinal)
            .Replace("- `REL-STAB-02`:", "- `REL-STAB-02`: Private bytes / handles / GPU trend sampler evidence", StringComparison.Ordinal)
            .Replace("- `REL-STAB-03`:", "- `REL-STAB-03`: Duplicate capture rejection during active session", StringComparison.Ordinal)
            .Replace("- `REL-STAB-04`:", "- `REL-STAB-04`: Slow or failing clipboard/file target recovery", StringComparison.Ordinal)
            .Replace("- Public gate `Long-run lifecycle evidence`:", "- Public gate `Long-run lifecycle evidence`: REPLACE_WITH_PASS_FAIL_LIMITATION", StringComparison.Ordinal)
            .Replace("- Session classification: PASS / PASS with limitation / FAIL / NOT RUN", "- Session classification: NOT RUN", StringComparison.Ordinal)
            .Replace("- Release impact:", "- Release impact: REPLACE_WITH_RELEASE_IMPACT", StringComparison.Ordinal)
            .Replace("- Known limitations:", "- Known limitations: Draft created from current Lumiere session context. Replace with observed limitations after the run.", StringComparison.Ordinal)
            .Replace("- Follow-up stories / issues:", "- Follow-up stories / issues: 12-3, 10-3, 11-3, 13-2", StringComparison.Ordinal);
    }

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

    private static string CreateProcessIdPlaceholder(int processId) =>
        processId > 0
            ? $"{processId} (current session)"
            : "REPLACE_WITH_LUMIERE_PROCESS_ID";

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
