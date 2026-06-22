using Lumiere.App;
using Xunit;

namespace Lumiere.Graphics.Tests.App;

public sealed class ResourceTrendValidationCommandProjectionTests
{
    [Fact]
    public void Create_ReturnsNullWhenInputsAreUnavailable()
    {
        Assert.Null(ResourceTrendValidationCommandProjection.Create(null, "C:\\Validation", 4242));
        Assert.Null(ResourceTrendValidationCommandProjection.Create("C:\\Validation\\collect-resource-trend-samples.ps1", null, 4242));
        Assert.Null(ResourceTrendValidationCommandProjection.Create("C:\\Validation\\collect-resource-trend-samples.ps1", "C:\\Validation", 0));
    }

    [Fact]
    public void Create_BuildsCurrentProcessSamplingCommand()
    {
        var command = ResourceTrendValidationCommandProjection.Create(
            "C:\\Validation\\collect-resource-trend-samples.ps1",
            "C:\\Validation",
            4242);

        Assert.Equal(
            "& \"C:\\Validation\\collect-resource-trend-samples.ps1\" -ProcessId 4242 -DurationSeconds 900 -SampleIntervalSeconds 5 -OutputDirectory \"C:\\Validation\\resource-trends\"",
            command);
    }

    [Fact]
    public void Create_UsesProvidedSamplingOverrides()
    {
        var command = ResourceTrendValidationCommandProjection.Create(
            "C:\\Validation\\collect-resource-trend-samples.ps1",
            "C:\\Validation",
            4242,
            durationSeconds: 1200,
            sampleIntervalSeconds: 10);

        Assert.Contains("-DurationSeconds 1200", command, System.StringComparison.Ordinal);
        Assert.Contains("-SampleIntervalSeconds 10", command, System.StringComparison.Ordinal);
    }
}
