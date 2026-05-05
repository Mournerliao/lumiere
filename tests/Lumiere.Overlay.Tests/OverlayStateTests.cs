using Xunit;

namespace Lumiere.Overlay.Tests;

public sealed class OverlayStateTests
{
    [Fact]
    public void HdrReady_HasExpectedStatusAndLabel()
    {
        var overlayState = OverlayState.HdrReady("HDR preview is ready.", "FP16 scRGB swap chain is attached.");

        Assert.Equal(OverlayDisplayStatus.HdrReady, overlayState.Status);
        Assert.Equal("HDR-ready", overlayState.Label);
        Assert.False(overlayState.RequiresFailureTeardown);
    }

    [Fact]
    public void PreviewFailed_RequiresFailureTeardown()
    {
        var overlayState = OverlayState.PreviewFailed(
            "Preview failed",
            "SetSwapChain failed.");

        Assert.Equal(OverlayDisplayStatus.PreviewFailed, overlayState.Status);
        Assert.True(overlayState.RequiresFailureTeardown);
    }

    [Theory]
    [InlineData("Initializing preview")]
    [InlineData("Degraded preview")]
    [InlineData("Unsupported capture")]
    [InlineData("Preview stopped")]
    public void FactoryMethods_ProduceExpectedLabels(string expectedLabel)
    {
        var overlayState = expectedLabel switch
        {
            "Initializing preview" => OverlayState.Initializing("Message", "Detail"),
            "Degraded preview" => OverlayState.DegradedPreview("Message", "Detail"),
            "Unsupported capture" => OverlayState.UnsupportedCapture("Message", "Detail"),
            "Preview stopped" => OverlayState.Disposed("Message", "Detail"),
            _ => throw new InvalidOperationException(),
        };

        Assert.Equal(expectedLabel, overlayState.Label);
    }

    [Fact]
    public void FromStatus_UsesDistinctFailureAndReadyStyles()
    {
        var ready = OverlayStatusStyle.FromStatus(OverlayDisplayStatus.HdrReady);
        var degraded = OverlayStatusStyle.FromStatus(OverlayDisplayStatus.DegradedPreview);
        var unsupported = OverlayStatusStyle.FromStatus(OverlayDisplayStatus.UnsupportedCapture);
        var failed = OverlayStatusStyle.FromStatus(OverlayDisplayStatus.PreviewFailed);

        Assert.NotEqual(ready, degraded);
        Assert.NotEqual(ready, unsupported);
        Assert.NotEqual(ready, failed);
    }
}
