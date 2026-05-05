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
    public void DisposalCoordinatorReturnsCompletedTeardownEvidence()
    {
        var evidence = CaptureSessionDisposalCoordinator.DisposeOnce(
            () => { },
            () => { },
            () => { },
            () => { });

        Assert.True(evidence.FrameHandlerUnsubscribed);
        Assert.True(evidence.SessionStopped);
        Assert.True(evidence.FramePoolDisposed);
        Assert.True(evidence.DeviceDisposed);
        Assert.True(evidence.Completed);
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
    public void CaptureSessionResourcesRetainsDisposalEvidence()
    {
        var resources = new CaptureSessionResources(
            () => new CaptureSessionDisposalEvidence(
                FrameHandlerUnsubscribed: true,
                SessionStopped: true,
                FramePoolDisposed: true,
                DeviceDisposed: true));

        resources.Dispose();

        Assert.NotNull(resources.DisposalEvidence);
        Assert.True(resources.DisposalEvidence.Completed);
    }

    [Fact]
    public void PreviewRecreationRequestRunsOnlyForQueuedGeneration()
    {
        var request = CapturePreviewRecreationRequest.Create(
            CreateTarget(2560, 1440),
            CaptureFrameSizeChange.Evaluate(1920, 1080, 2560, 1440),
            generation: 7);

        Assert.True(request.MatchesGeneration(7));
        Assert.False(request.MatchesGeneration(8));
    }

    [Fact]
    public void PreviewRecreationRequestRequiresFrameSizeMismatch()
    {
        var sizeChange = CaptureFrameSizeChange.Evaluate(1920, 1080, 1920, 1080);

        var exception = Assert.Throws<ArgumentException>(() =>
            CapturePreviewRecreationRequest.Create(CreateTarget(1920, 1080), sizeChange, generation: 1));

        Assert.Equal("sizeChange", exception.ParamName);
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

    private static CaptureTarget CreateTarget(int width, int height) =>
        CaptureTarget.CreateForTest(
            new Windows.Graphics.SizeInt32 { Width = width, Height = height },
            "Lifecycle target");
}
