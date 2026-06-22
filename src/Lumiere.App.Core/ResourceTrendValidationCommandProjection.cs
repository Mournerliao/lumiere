namespace Lumiere.App;

public static class ResourceTrendValidationCommandProjection
{
    public static string? Create(
        string? resourceTrendScriptPath,
        string? validationWorkspacePath,
        int processId,
        int durationSeconds = 900,
        int sampleIntervalSeconds = 5)
    {
        if (string.IsNullOrWhiteSpace(resourceTrendScriptPath)
            || string.IsNullOrWhiteSpace(validationWorkspacePath)
            || processId <= 0)
        {
            return null;
        }

        var outputDirectory = Path.Combine(validationWorkspacePath.Trim(), "resource-trends");
        return string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"& \"{resourceTrendScriptPath.Trim()}\" -ProcessId {processId} -DurationSeconds {durationSeconds} -SampleIntervalSeconds {sampleIntervalSeconds} -OutputDirectory \"{outputDirectory}\"");
    }
}
