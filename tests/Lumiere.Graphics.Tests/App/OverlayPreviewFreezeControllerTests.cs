using Lumiere.App;
using Lumiere.Capture;
using Xunit;

namespace Lumiere.Graphics.Tests.App;

public sealed class OverlayPreviewFreezeControllerTests
{
    [Fact]
    public void RegionCapture_FreezesAfterFirstStableFrame()
    {
        var controller = new OverlayPreviewFreezeController(CaptureCommandMode.Region);

        var disposition = controller.OnFramePresented(requiresRecreation: false);

        Assert.Equal(OverlayPreviewFrameDisposition.FreezeAfterPresent, disposition);
        Assert.True(controller.IsFrozen);
        Assert.False(controller.AcceptsCallbacks);
    }

    [Fact]
    public void RegionCapture_DoesNotFreezeBeforeFrameRecreationStabilizes()
    {
        var controller = new OverlayPreviewFreezeController(CaptureCommandMode.Region);

        var firstDisposition = controller.OnFramePresented(requiresRecreation: true);
        var secondDisposition = controller.OnFramePresented(requiresRecreation: false);

        Assert.Equal(OverlayPreviewFrameDisposition.Continue, firstDisposition);
        Assert.Equal(OverlayPreviewFrameDisposition.FreezeAfterPresent, secondDisposition);
        Assert.True(controller.IsFrozen);
    }

    [Fact]
    public void RegionCapture_IgnoresFramesAfterFreeze()
    {
        var controller = new OverlayPreviewFreezeController(CaptureCommandMode.Region);
        controller.OnFramePresented(requiresRecreation: false);

        var disposition = controller.OnFramePresented(requiresRecreation: false);

        Assert.Equal(OverlayPreviewFrameDisposition.Ignore, disposition);
    }

    [Fact]
    public void FullscreenCapture_RemainsLiveAcrossFrames()
    {
        var controller = new OverlayPreviewFreezeController(CaptureCommandMode.Fullscreen);

        var firstDisposition = controller.OnFramePresented(requiresRecreation: false);
        var secondDisposition = controller.OnFramePresented(requiresRecreation: false);

        Assert.Equal(OverlayPreviewFrameDisposition.Continue, firstDisposition);
        Assert.Equal(OverlayPreviewFrameDisposition.Continue, secondDisposition);
        Assert.False(controller.IsFrozen);
        Assert.True(controller.AcceptsCallbacks);
    }
}
