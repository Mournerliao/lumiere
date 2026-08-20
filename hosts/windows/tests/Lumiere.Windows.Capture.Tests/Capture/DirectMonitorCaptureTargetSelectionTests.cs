using Lumiere.Windows.Capture;
using Lumiere.Windows.Graphics.Hdr;
using Lumiere.Windows.Interop;
using Xunit;

namespace Lumiere.Windows.Capture.Tests;

public sealed class DirectMonitorCaptureTargetSelectionTests
{
    [Fact]
    public async Task SelectsResolvedMonitorAndAddsStableDisplayIdentity()
    {
        var monitor = new MonitorHandle((nint)42, @"\\.\DISPLAY2", 100, 200, 1920, 1080);
        var service = new DirectMonitorCaptureTargetSelectionService(
            () => monitor,
            _ => CaptureTarget.CreateForTest(
                new Windows.Graphics.SizeInt32 { Width = 1920, Height = 1080 },
                "HDR display",
                CaptureTargetKind.Display),
            () => true);

        var result = await service.SelectTargetAsync();

        Assert.Equal(SelectionOutcome.Selected, result.Outcome);
        Assert.Equal(@"\\.\DISPLAY2", result.Target!.DisplayIdentity!.MonitorDisplayName);
        Assert.Equal(EngineReadinessState.Initializing, result.Readiness.State);
    }

    [Fact]
    public async Task ReportsUnsupportedWhenWgcIsUnavailable()
    {
        var service = new DirectMonitorCaptureTargetSelectionService(
            () => throw new InvalidOperationException("must not resolve"),
            _ => throw new InvalidOperationException("must not create"),
            () => false);

        var result = await service.SelectTargetAsync();

        Assert.Equal(SelectionOutcome.Unsupported, result.Outcome);
        Assert.Equal(EngineReadinessState.Unsupported, result.Readiness.State);
    }

    [Fact]
    public async Task MapsNativeInteropFailureWithoutOpeningUi()
    {
        var service = new DirectMonitorCaptureTargetSelectionService(
            () => throw new NativeInteropException(
                "MonitorFromPoint",
                "Interop",
                unchecked((int)0x80004001),
                "not implemented",
                "Capture is unavailable"),
            _ => throw new InvalidOperationException("must not create"),
            () => true);

        var result = await service.SelectTargetAsync();

        Assert.Equal(SelectionOutcome.Unsupported, result.Outcome);
        Assert.Null(result.Target);
    }
}
