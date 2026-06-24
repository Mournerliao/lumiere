using Lumiere.Graphics.Output;

namespace Lumiere.App;

public static class ScenarioValidationDraftFactory
{
    public static string Create(
        string template,
        OutputValidationSessionArtifact artifact,
        string outputValidationArtifactFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(template);
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputValidationArtifactFileName);

        return template
            .Replace("- Date:", $"- Date: {artifact.Date}", StringComparison.Ordinal)
            .Replace("- Tester:", $"- Tester: {artifact.Tester}", StringComparison.Ordinal)
            .Replace("- Build / commit:", $"- Build / commit: {artifact.BuildCommit}", StringComparison.Ordinal)
            .Replace("- Windows version:", $"- Windows version: {artifact.WindowsVersion}", StringComparison.Ordinal)
            .Replace("- Device:", $"- Device: {artifact.Device}", StringComparison.Ordinal)
            .Replace("- GPU:", $"- GPU: {artifact.Gpu}", StringComparison.Ordinal)
            .Replace("- Display setup:", $"- Display setup: {artifact.DisplaySetup}", StringComparison.Ordinal)
            .Replace("- HDR state:", $"- HDR state: {artifact.HdrState}", StringComparison.Ordinal)
            .Replace("- DPI scale(s):", $"- DPI scale(s): {JoinValues(artifact.DpiScales)}", StringComparison.Ordinal)
            .Replace("- Target apps tested:", $"- Target apps tested: {JoinValues(artifact.TargetAppsTested)}", StringComparison.Ordinal)
            .Replace("- Entry points tested:", $"- Entry points tested: {JoinValues(artifact.EntryPointsTested)}", StringComparison.Ordinal)
            .Replace("- Output targets tested:", $"- Output targets tested: {JoinValues(artifact.OutputTargetsTested)}", StringComparison.Ordinal)
            .Replace("- `REL-CAP-*`:", $"- `REL-CAP-*`: {JoinChecklistIds(artifact.ChecklistIdsCovered, "REL-CAP-")}", StringComparison.Ordinal)
            .Replace("- `REL-OUT-*`:", $"- `REL-OUT-*`: {JoinChecklistIds(artifact.ChecklistIdsCovered, "REL-OUT-")}", StringComparison.Ordinal)
            .Replace("- `REL-HDR-*`:", $"- `REL-HDR-*`: {JoinChecklistIds(artifact.ChecklistIdsCovered, "REL-HDR-")}", StringComparison.Ordinal)
            .Replace("- `REL-A11Y-*`:", $"- `REL-A11Y-*`: {JoinChecklistIds(artifact.ChecklistIdsCovered, "REL-A11Y-")}", StringComparison.Ordinal)
            .Replace("- `REL-SET-*`:", $"- `REL-SET-*`: {JoinChecklistIds(artifact.ChecklistIdsCovered, "REL-SET-")}", StringComparison.Ordinal)
            .Replace("- Additional notes:", $"- Additional notes: Linked output validation JSON: ..\\{outputValidationArtifactFileName}", StringComparison.Ordinal)
            .Replace("- Known limitations:", $"- Known limitations: {JoinValues(artifact.KnownLimitations)}", StringComparison.Ordinal)
            .Replace("- Follow-up stories / issues:", $"- Follow-up stories / issues: {JoinValues(artifact.FollowUpIssuesOrStories)}", StringComparison.Ordinal);
    }

    private static string JoinValues(IEnumerable<string> values)
    {
        var normalized = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToArray();
        return normalized.Length == 0
            ? "REPLACE_WITH_OBSERVED_VALUE"
            : string.Join(", ", normalized);
    }

    private static string JoinChecklistIds(IEnumerable<string> values, string prefix)
    {
        var matching = values
            .Where(value => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        return matching.Length == 0
            ? "NOT RUN"
            : string.Join(", ", matching);
    }
}
