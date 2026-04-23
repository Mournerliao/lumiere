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
}
