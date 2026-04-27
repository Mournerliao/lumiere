using Lumiere.Capture;
using Lumiere.Graphics.Hdr;
using Xunit;

namespace Lumiere.Graphics.Tests.Capture;

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
    public void NotStartedCaptureResultDoesNotExposeSessionResources()
    {
        var result = CaptureStartResult.NotStarted(
            PreviewReadinessStatus.Unsupported(
                PreviewReadinessStage.Capture,
                "Unsupported capture",
                "GraphicsCaptureSession.IsSupported returned false."));

        Assert.False(result.Started);
        Assert.Null(result.SessionResources);
        Assert.Equal(PreviewReadinessState.Unsupported, result.Readiness.State);
    }

    [Fact]
    public void StartSucceededCaptureResultExposesSessionResources()
    {
        var sessionResources = new CaptureSessionResources(() => { });
        var readiness = PreviewReadinessStatus.Initializing(
            PreviewReadinessStage.Capture,
            "Initializing preview",
            "Direct3D11CaptureFramePool started.");

        var result = CaptureStartResult.StartSucceeded(sessionResources, readiness);

        Assert.True(result.Started);
        Assert.Same(sessionResources, result.SessionResources);
        Assert.Same(readiness, result.Readiness);
    }
}
