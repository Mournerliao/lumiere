using Lumiere.Overlay.Crop;
using Windows.Foundation;
using Xunit;

namespace Lumiere.Overlay.Tests;

public sealed class OverlayConfirmTests
{
    [Fact]
    public void TryCreate_MapsValidActiveCropToConfirmedSelection()
    {
        var selection = new CropSelection(
            CropSelectionPhase.Active,
            CropGeometry.FromEdges(10, 20, 50, 60, new Rect(0, 0, 100, 100)),
            null);
        var state = OverlayState.HdrReady("HDR preview is ready.", "FP16 scRGB preview.");

        var result = ConfirmedCaptureSelection.TryCreate(
            selection,
            new Rect(0, 0, 100, 100),
            new CaptureFrameSize(1000, 500),
            state,
            out var confirmed);

        Assert.True(result);
        Assert.Equal(new Rect(10, 20, 40, 40), confirmed.DipRegion);
        Assert.Equal(new CropPixelRect(100, 100, 400, 200), confirmed.PixelRegion);
        Assert.Equal(new CaptureFrameSize(1000, 500), confirmed.FrameSize);
        Assert.Equal(OverlayDisplayStatus.HdrReady, confirmed.Status);
        Assert.Equal("HDR preview is ready.", confirmed.StatusText);
    }

    [Fact]
    public void TryCreate_RejectsMissingCrop()
    {
        var result = ConfirmedCaptureSelection.TryCreate(
            CropSelection.Empty,
            new Rect(0, 0, 100, 100),
            new CaptureFrameSize(1000, 500),
            OverlayState.HdrReady("Ready"),
            out _);

        Assert.False(result);
    }

    [Fact]
    public void TryCreate_PreservesDegradedStatus()
    {
        var selection = new CropSelection(
            CropSelectionPhase.Active,
            CropGeometry.FromEdges(0, 0, 100, 50, new Rect(0, 0, 100, 100)),
            null);
        var state = OverlayState.DegradedPreview("Preview fidelity is degraded.", "Presentation fell back.");

        var result = ConfirmedCaptureSelection.TryCreate(
            selection,
            new Rect(0, 0, 100, 100),
            new CaptureFrameSize(200, 100),
            state,
            out var confirmed);

        Assert.True(result);
        Assert.Equal(OverlayDisplayStatus.DegradedPreview, confirmed.Status);
        Assert.Contains("degraded", confirmed.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(OverlayDisplayStatus.UnsupportedCapture)]
    [InlineData(OverlayDisplayStatus.PreviewFailed)]
    [InlineData(OverlayDisplayStatus.Closing)]
    [InlineData(OverlayDisplayStatus.Disposed)]
    public void TryCreate_RejectsUnavailableStates(OverlayDisplayStatus status)
    {
        var selection = new CropSelection(
            CropSelectionPhase.Active,
            CropGeometry.FromEdges(10, 10, 30, 30, new Rect(0, 0, 100, 100)),
            null);
        var state = new OverlayState(status, "Label", "Message", "Detail", OverlayFailureAction.KeepOpenWithFailure);

        var result = ConfirmedCaptureSelection.TryCreate(
            selection,
            new Rect(0, 0, 100, 100),
            new CaptureFrameSize(1000, 500),
            state,
            out _);

        Assert.False(result);
    }
}
