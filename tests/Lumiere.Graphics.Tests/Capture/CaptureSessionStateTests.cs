using Lumiere.Capture;
using Lumiere.Graphics.Hdr;
using Windows.Graphics;
using Xunit;

namespace Lumiere.Graphics.Tests.Capture;

public sealed class CaptureSessionStateTests
{
    [Fact]
    public void SelectedTargetAndStartedCaptureStayInitializingUntilPresentationIsReady()
    {
        var target = CreateTarget();
        var startResult = CaptureStartResult.StartSucceeded(
            new CaptureSessionResources(() => { }),
            PreviewReadinessStatus.Initializing(
                PreviewReadinessStage.Capture,
                "Initializing preview",
                "Direct3D11CaptureFramePool started."));

        var initializing = CaptureSessionState.FromStartResult(target, startResult);
        var capturing = CaptureSessionState.FromReadiness(
            target,
            PreviewReadinessStatus.Ready(
                "HDR-ready",
                "Presented frame #1."));

        Assert.Equal(CaptureSessionStatus.Initializing, initializing.Status);
        Assert.True(initializing.HasNativeSession);
        Assert.Equal(CaptureSessionStatus.Capturing, capturing.Status);
        Assert.Same(target, capturing.Target);
        Assert.Equal(PreviewReadinessState.Ready, capturing.Readiness.State);
    }

    [Fact]
    public void UnsupportedSelectionMapsToUnsupportedWithConciseUserReason()
    {
        var readiness = PreviewReadinessStatus.Unsupported(
            PreviewReadinessStage.Capture,
            "Unsupported capture",
            "GraphicsCaptureSession.IsSupported returned false.");
        var result = CaptureTargetSelectionResult.Unsupported(readiness);

        var state = CaptureSessionState.FromSelectionResult(result);

        Assert.Equal(CaptureSessionStatus.Unsupported, state.Status);
        Assert.Null(state.Target);
        Assert.Equal("Unsupported capture", state.UserFacingReason);
        Assert.Equal(PreviewReadinessState.Unsupported, state.Readiness.State);
    }

    [Fact]
    public void DegradedReadinessMapsToDegradedAndRequiresUserAttention()
    {
        var target = CreateTarget();
        var readiness = PreviewReadinessStatus.Degraded(
            PreviewReadinessStage.Presentation,
            "Degraded preview",
            "Swap chain color-space validation did not prove scRGB HDR.");

        var state = CaptureSessionState.FromReadiness(target, readiness);

        Assert.Equal(CaptureSessionStatus.Degraded, state.Status);
        Assert.True(state.Readiness.RequiresUserAttention);
        Assert.NotEqual(CaptureSessionStatus.Capturing, state.Status);
    }

    [Fact]
    public void FailurePreservesReadinessStageAndTechnicalDetail()
    {
        var target = CreateTarget();
        var readiness = PreviewReadinessStatus.Failed(
            PreviewReadinessStage.Interop,
            "Preview failed",
            "CreateDirect3DDevice failed with HRESULT 0x80004005.");
        var startResult = CaptureStartResult.NotStarted(readiness);

        var state = CaptureSessionState.FromStartResult(target, startResult);

        Assert.Equal(CaptureSessionStatus.Failed, state.Status);
        Assert.Equal(PreviewReadinessStage.Interop, state.Readiness.Stage);
        Assert.Contains("0x80004005", state.TechnicalDetail);
    }

    [Fact]
    public void CancellationReturnsIdleAndDoesNotImplyNativeSessionResources()
    {
        var result = CaptureTargetSelectionResult.Canceled(
            PreviewReadinessStatus.Initializing(
                PreviewReadinessStage.Capture,
                "Choose a display or window to start the minimal HDR preview.",
                "GraphicsCapturePicker was canceled."));

        var state = CaptureSessionState.FromSelectionResult(result);

        Assert.Equal(CaptureSessionStatus.Idle, state.Status);
        Assert.Null(state.Target);
        Assert.False(state.HasNativeSession);
    }

    [Fact]
    public void UnsupportedStartResultMapsToUnsupportedWithoutNativeSession()
    {
        var target = CreateTarget();
        var result = CaptureStartResult.NotStarted(
            PreviewReadinessStatus.Unsupported(
                PreviewReadinessStage.Capture,
                "Unsupported capture",
                "GraphicsCaptureSession.IsSupported returned false."));

        var state = CaptureSessionState.FromStartResult(target, result);

        Assert.Equal(CaptureSessionStatus.Unsupported, state.Status);
        Assert.False(state.HasNativeSession);
        Assert.Equal(PreviewReadinessState.Unsupported, state.Readiness.State);
    }

    [Fact]
    public void DisposedStateDoesNotExposeNativeSession()
    {
        var state = CaptureSessionState.Disposed();

        Assert.Equal(CaptureSessionStatus.Disposed, state.Status);
        Assert.Null(state.Target);
        Assert.False(state.HasNativeSession);
        Assert.Contains("disposed", state.TechnicalDetail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnsupportedReadinessDuringActiveSessionPreservesTargetAndNativeSession()
    {
        var target = CreateTarget();
        var readiness = PreviewReadinessStatus.Unsupported(
            PreviewReadinessStage.Presentation,
            "Unsupported capture",
            "Presentation support changed while capture resources were active.");

        var state = CaptureSessionState.FromReadiness(target, readiness);

        Assert.Equal(CaptureSessionStatus.Unsupported, state.Status);
        Assert.Same(target, state.Target);
        Assert.True(state.HasNativeSession);
        Assert.Equal(PreviewReadinessStage.Presentation, state.Readiness.Stage);
    }

    private static CaptureTarget CreateTarget() =>
        CaptureTarget.CreateForTest(
            new SizeInt32 { Width = 1920, Height = 1080 },
            "Test Display");
}
