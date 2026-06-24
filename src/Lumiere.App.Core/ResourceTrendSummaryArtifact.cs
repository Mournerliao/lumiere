using System.Text.Json;

namespace Lumiere.App;

public sealed record ResourceTrendSummaryArtifact(
    string SummaryPath,
    string CsvPath,
    int ProcessId,
    string ProcessName,
    int DurationSeconds,
    int SampleIntervalSeconds,
    int SampleCount,
    IReadOnlyDictionary<string, ResourceTrendMetricSummary> Metrics)
{
    public static ResourceTrendSummaryArtifact FromJson(string json, string summaryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        ArgumentException.ThrowIfNullOrWhiteSpace(summaryPath);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var metrics = new Dictionary<string, ResourceTrendMetricSummary>(StringComparer.OrdinalIgnoreCase);
        if (root.TryGetProperty("metrics", out var metricsElement)
            && metricsElement.ValueKind is JsonValueKind.Object)
        {
            foreach (var metric in metricsElement.EnumerateObject())
            {
                metrics[metric.Name] = ResourceTrendMetricSummary.FromJson(metric.Value);
            }
        }

        return new ResourceTrendSummaryArtifact(
            summaryPath,
            GetString(root, "csvPath") ?? "REPLACE_WITH_RESOURCE_TREND_CSV_PATH",
            GetInt(root, "processId"),
            GetString(root, "processName") ?? "Lumiere.App",
            GetInt(root, "durationSeconds"),
            GetInt(root, "sampleIntervalSeconds"),
            GetInt(root, "sampleCount"),
            metrics);
    }

    public ResourceTrendMetricSummary? TryGetMetric(string name) =>
        Metrics.TryGetValue(name, out var metric) ? metric : null;

    public bool HasGpuCounterSamples =>
        HasNonZeroMax("gpuDedicatedUsageBytes")
        || HasNonZeroMax("gpuSharedUsageBytes")
        || HasNonZeroMax("gpuTotalCommittedBytes");

    private bool HasNonZeroMax(string metricName) =>
        TryGetMetric(metricName)?.Max is > 0;

    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind is JsonValueKind.String
            ? value.GetString()
            : null;

    private static int GetInt(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var result)
            ? result
            : 0;
}

public sealed record ResourceTrendMetricSummary(
    long Baseline,
    long Final,
    long Delta,
    long Min,
    long Max)
{
    public static ResourceTrendMetricSummary FromJson(JsonElement element) =>
        new(
            GetInt64(element, "baseline"),
            GetInt64(element, "final"),
            GetInt64(element, "delta"),
            GetInt64(element, "min"),
            GetInt64(element, "max"));

    private static long GetInt64(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.TryGetInt64(out var result)
            ? result
            : 0;
}
