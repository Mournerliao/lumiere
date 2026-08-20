using Lumiere.Windows.Capture;
using Lumiere.Windows.Graphics.Devices;
using Lumiere.Windows.Graphics.Hdr;
using Windows.Graphics;
using Xunit;

namespace Lumiere.Windows.Capture.Tests;

public sealed class CaptureCommandCoordinatorTests
{
    [Fact]
    public async Task ExecuteAsync_DelegatesToCaptureServiceAndReturnsResult()
    {
        var deviceProvider = new GraphicsDeviceProvider();
        using var deviceResources = deviceProvider.CreateDevice();
        var captureService = new CaptureService(deviceResources, CaptureBorderOptions.RequireSystemBorder());
        var coordinator = new CaptureCommandCoordinator(captureService);
        var command = CaptureCommand.Fullscreen();

        var result = await coordinator.ExecuteAsync(command);

        Assert.NotNull(result);
        Assert.True(result.IsAccepted);
        Assert.Same(command, result.Command);
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesRejectionWhenSessionActive()
    {
        var deviceProvider = new GraphicsDeviceProvider();
        using var deviceResources = deviceProvider.CreateDevice();
        var captureService = new CaptureService(deviceResources, CaptureBorderOptions.RequireSystemBorder());
        var coordinator = new CaptureCommandCoordinator(captureService);

        // First command to reserve the session
        var firstCommand = CaptureCommand.Fullscreen();
        var firstResult = await coordinator.ExecuteAsync(firstCommand);
        Assert.True(firstResult.IsAccepted);

        // Second command should be rejected because session is now SelectingTarget
        var secondCommand = CaptureCommand.Region();
        var secondResult = await coordinator.ExecuteAsync(secondCommand);

        Assert.False(secondResult.IsAccepted);
        Assert.True(secondResult.IsRejectedSessionActive);
        Assert.Same(secondCommand, secondResult.Command);
    }

    [Fact]
    public async Task ExecuteAsync_RoutesFullscreenMode()
    {
        var deviceProvider = new GraphicsDeviceProvider();
        using var deviceResources = deviceProvider.CreateDevice();
        var captureService = new CaptureService(deviceResources, CaptureBorderOptions.RequireSystemBorder());
        var coordinator = new CaptureCommandCoordinator(captureService);
        var command = CaptureCommand.Fullscreen();

        var result = await coordinator.ExecuteAsync(command);

        Assert.True(result.IsAccepted);
        Assert.Equal(CaptureCommandMode.Fullscreen, result.Command.Mode);
    }

    [Fact]
    public async Task ExecuteAsync_RoutesRegionMode()
    {
        var deviceProvider = new GraphicsDeviceProvider();
        using var deviceResources = deviceProvider.CreateDevice();
        var captureService = new CaptureService(deviceResources, CaptureBorderOptions.RequireSystemBorder());
        var coordinator = new CaptureCommandCoordinator(captureService);
        var command = CaptureCommand.Region();

        var result = await coordinator.ExecuteAsync(command);

        Assert.True(result.IsAccepted);
        Assert.Equal(CaptureCommandMode.Region, result.Command.Mode);
    }

    [Fact]
    public void Constructor_ThrowsOnNullCaptureService()
    {
        Assert.Throws<ArgumentNullException>(() => new CaptureCommandCoordinator(null!));
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsOnNullCommand()
    {
        var deviceProvider = new GraphicsDeviceProvider();
        using var deviceResources = deviceProvider.CreateDevice();
        var captureService = new CaptureService(deviceResources, CaptureBorderOptions.RequireSystemBorder());
        var coordinator = new CaptureCommandCoordinator(captureService);

        await Assert.ThrowsAsync<ArgumentNullException>(() => coordinator.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecuteAsync_UpdatesSessionStateToSelectingTarget()
    {
        var deviceProvider = new GraphicsDeviceProvider();
        using var deviceResources = deviceProvider.CreateDevice();
        var captureService = new CaptureService(deviceResources, CaptureBorderOptions.RequireSystemBorder());
        var coordinator = new CaptureCommandCoordinator(captureService);
        var command = CaptureCommand.Fullscreen();

        Assert.Equal(CaptureSessionStatus.Idle, captureService.CurrentSessionState.Status);

        var result = await coordinator.ExecuteAsync(command);

        Assert.True(result.IsAccepted);
        Assert.Equal(CaptureSessionStatus.SelectingTarget, captureService.CurrentSessionState.Status);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsCompletedTask()
    {
        var deviceProvider = new GraphicsDeviceProvider();
        using var deviceResources = deviceProvider.CreateDevice();
        var captureService = new CaptureService(deviceResources, CaptureBorderOptions.RequireSystemBorder());
        var coordinator = new CaptureCommandCoordinator(captureService);
        var command = CaptureCommand.Fullscreen();

        var task = coordinator.ExecuteAsync(command);

        Assert.True(task.IsCompleted);
    }
}
