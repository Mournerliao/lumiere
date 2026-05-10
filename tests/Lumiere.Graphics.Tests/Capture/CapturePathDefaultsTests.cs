using Lumiere.Capture;
using Lumiere.Graphics.Hdr;
using Lumiere.Infrastructure.Interop;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Xunit;

namespace Lumiere.Graphics.Tests.Capture;

public sealed class CapturePathDefaultsTests
{
    [Fact]
    public void CreateDirectOnlyProducesServiceWithNoFallbackPicker()
    {
        var service = DirectMonitorCaptureTargetSelectionService.CreateDirectOnly(
            () => new MonitorHandle(new IntPtr(12345), "DISPLAY1"),
            monitor => CaptureTarget.CreateForTest(
                new SizeInt32 { Width = 1920, Height = 1080 },
                monitor.DisplayName,
                CaptureTargetKind.Display),
            () => true);

        Assert.False(service.HasFallbackPicker);
    }

    [Fact]
    public async Task CreateDirectOnlyServiceSelectsDirectMonitorTarget()
    {
        var service = DirectMonitorCaptureTargetSelectionService.CreateDirectOnly(
            () => new MonitorHandle(new IntPtr(12345), "DISPLAY1"),
            monitor => CaptureTarget.CreateForTest(
                new SizeInt32 { Width = 1920, Height = 1080 },
                monitor.DisplayName,
                CaptureTargetKind.Display),
            () => true);

        var result = await service.SelectDirectMonitorTargetAsync();

        Assert.Equal(SelectionOutcome.Selected, result.Outcome);
        Assert.NotNull(result.Target);
        Assert.Equal(CaptureTargetKind.Display, result.Target.Kind);
    }

    [Fact]
    public void CaptureCommandFullscreenIsAcceptedWhenSessionIdle()
    {
        var state = CaptureSessionState.Idle();
        var command = CaptureCommand.Fullscreen();

        var canAccept = CaptureService.CanAcceptCommand(state, command, out var rejectionReason);

        Assert.True(canAccept);
        Assert.Null(rejectionReason);
    }

    [Fact]
    public void CaptureCommandRegionIsAcceptedWhenSessionIdle()
    {
        var state = CaptureSessionState.Idle();
        var command = CaptureCommand.Region();

        var canAccept = CaptureService.CanAcceptCommand(state, command, out var rejectionReason);

        Assert.True(canAccept);
        Assert.Null(rejectionReason);
    }

    [Fact]
    public void CaptureCommandFullscreenProducesCorrectMode()
    {
        var command = CaptureCommand.Fullscreen();

        Assert.Equal(CaptureCommandMode.Fullscreen, command.Mode);
        Assert.Null(command.Target);
    }

    [Fact]
    public void CaptureCommandRegionProducesCorrectMode()
    {
        var command = CaptureCommand.Region();

        Assert.Equal(CaptureCommandMode.Region, command.Mode);
        Assert.Null(command.Target);
    }

    [Fact]
    public async Task CreateDirectOnlyServiceRejectsFallbackPickerPath()
    {
        var service = DirectMonitorCaptureTargetSelectionService.CreateDirectOnly(
            () => new MonitorHandle(new IntPtr(12345), "DISPLAY1"),
            monitor => CaptureTarget.CreateForTest(
                new SizeInt32 { Width = 1920, Height = 1080 },
                monitor.DisplayName,
                CaptureTargetKind.Display),
            () => true);

        var result = await service.SelectWithFallbackPickerAsync();

        Assert.Equal(SelectionOutcome.Failed, result.Outcome);
        Assert.Contains("no fallback picker was configured", result.Readiness.TechnicalDetail);
    }
}
