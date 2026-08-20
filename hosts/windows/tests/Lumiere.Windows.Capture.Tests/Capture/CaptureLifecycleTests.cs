using Lumiere.Windows.Capture;
using Lumiere.Windows.Graphics.Hdr;
using Xunit;

namespace Lumiere.Windows.Capture.Tests;

public sealed class CaptureLifecycleTests
{
    [Fact]
    public void DisposalCoordinatorUnsubscribesBeforeReleasingCaptureResources()
    {
        var calls = new List<string>();

        CaptureSessionDisposalCoordinator.DisposeOnce(
            () => calls.Add("unsubscribe"),
            () => calls.Add("stop-session"),
            () => calls.Add("dispose-frame-pool"),
            () => calls.Add("dispose-device"));

        Assert.Equal(
            new[]
            {
                "unsubscribe",
                "stop-session",
                "dispose-frame-pool",
                "dispose-device",
            },
            calls);
    }

    [Fact]
    public void DisposalCoordinatorReturnsCompletedTeardownResult()
    {
        var result = CaptureSessionDisposalCoordinator.DisposeOnce(
            () => { },
            () => { },
            () => { },
            () => { });

        Assert.True(result.FrameHandlerUnsubscribed);
        Assert.True(result.SessionStopped);
        Assert.True(result.FramePoolDisposed);
        Assert.True(result.DeviceDisposed);
        Assert.True(result.Completed);
    }

    [Fact]
    public void CaptureSessionResourcesDisposesOnlyOnce()
    {
        var disposeCount = 0;
        var resources = new CaptureSessionResources(() => disposeCount++);

        resources.Dispose();
        resources.Dispose();

        Assert.Equal(1, disposeCount);
    }

    [Fact]
    public void CaptureSessionResourcesDisposesOnlyOnceAcrossConcurrentCallers()
    {
        var disposeCount = 0;
        var resources = new CaptureSessionResources(() => Interlocked.Increment(ref disposeCount));

        Parallel.For(0, 32, _ => resources.Dispose());

        Assert.Equal(1, disposeCount);
    }

    [Fact]
    public void CaptureSessionResourcesRetainsDisposalResult()
    {
        var resources = new CaptureSessionResources(
            () => new CaptureSessionDisposalResult(
                FrameHandlerUnsubscribed: true,
                SessionStopped: true,
                FramePoolDisposed: true,
                DeviceDisposed: true));

        resources.Dispose();

        Assert.NotNull(resources.DisposalResult);
        Assert.True(resources.DisposalResult.Completed);
    }

    [Fact]
    public void NotStartedCaptureResultDoesNotExposeSessionResources()
    {
        var result = CaptureStartResult.NotStarted(
            EngineReadinessStatus.Unsupported(
                EngineReadinessStage.Capture,
                "Unsupported capture",
                "GraphicsCaptureSession.IsSupported returned false."));

        Assert.False(result.Started);
        Assert.Null(result.SessionResources);
        Assert.Equal(EngineReadinessState.Unsupported, result.Readiness.State);
    }

    [Fact]
    public void StartSucceededCaptureResultExposesSessionResources()
    {
        var sessionResources = new CaptureSessionResources(() => { });
        var readiness = EngineReadinessStatus.Initializing(
            EngineReadinessStage.Capture,
            "Initializing capture",
            "Direct3D11CaptureFramePool started.");

        var result = CaptureStartResult.StartSucceeded(sessionResources, readiness);

        Assert.True(result.Started);
        Assert.Same(sessionResources, result.SessionResources);
        Assert.Same(readiness, result.Readiness);
    }

}
