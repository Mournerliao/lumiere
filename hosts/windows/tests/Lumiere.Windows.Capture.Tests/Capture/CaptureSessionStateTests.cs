using Lumiere.Windows.Capture;
using Lumiere.Windows.Graphics.Hdr;
using Windows.Graphics;
using Xunit;

namespace Lumiere.Windows.Capture.Tests;

public sealed class CaptureSessionStateTests
{
    [Fact]
    public void SelectedTargetAndStartedCaptureStayInitializingUntilPresentationIsReady()
    {
        var target = CreateTarget();
        var startResult = CaptureStartResult.StartSucceeded(
            new CaptureSessionResources(() => { }),
            EngineReadinessStatus.Initializing(
                EngineReadinessStage.Capture,
                "Initializing capture",
                "Direct3D11CaptureFramePool started."));

        var initializing = CaptureSessionState.FromStartResult(target, startResult);
        var capturing = CaptureSessionState.FromReadiness(
            target,
            EngineReadinessStatus.Ready(
                "HDR-ready",
                "Presented frame #1."));

        Assert.Equal(CaptureSessionStatus.Initializing, initializing.Status);
        Assert.True(initializing.HasNativeSession);
        Assert.Equal(CaptureSessionStatus.Capturing, capturing.Status);
        Assert.Same(target, capturing.Target);
        Assert.Equal(EngineReadinessState.Ready, capturing.Readiness.State);
    }

    [Fact]
    public void UnsupportedSelectionMapsToUnsupportedWithConciseUserReason()
    {
        var readiness = EngineReadinessStatus.Unsupported(
            EngineReadinessStage.Capture,
            "Unsupported capture",
            "GraphicsCaptureSession.IsSupported returned false.");
        var result = CaptureTargetSelectionResult.Unsupported(readiness);

        var state = CaptureSessionState.FromSelectionResult(result);

        Assert.Equal(CaptureSessionStatus.Unsupported, state.Status);
        Assert.Null(state.Target);
        Assert.Equal("Unsupported capture", state.UserFacingReason);
        Assert.Equal(EngineReadinessState.Unsupported, state.Readiness.State);
    }

    [Fact]
    public void DegradedReadinessMapsToDegradedAndRequiresUserAttention()
    {
        var target = CreateTarget();
        var readiness = EngineReadinessStatus.Degraded(
            EngineReadinessStage.Graphics,
            "Degraded capture",
            "Display capability probe did not establish active HDR.");

        var state = CaptureSessionState.FromReadiness(target, readiness);

        Assert.Equal(CaptureSessionStatus.Degraded, state.Status);
        Assert.True(state.Readiness.RequiresUserAttention);
        Assert.NotEqual(CaptureSessionStatus.Capturing, state.Status);
    }

    [Fact]
    public void FailurePreservesReadinessStageAndTechnicalDetail()
    {
        var target = CreateTarget();
        var readiness = EngineReadinessStatus.Failed(
            EngineReadinessStage.Interop,
            "Capture failed",
            "CreateDirect3DDevice failed with HRESULT 0x80004005.");
        var startResult = CaptureStartResult.NotStarted(readiness);

        var state = CaptureSessionState.FromStartResult(target, startResult);

        Assert.Equal(CaptureSessionStatus.Failed, state.Status);
        Assert.Equal(EngineReadinessStage.Interop, state.Readiness.Stage);
        Assert.Contains("0x80004005", state.TechnicalDetail);
    }

    [Fact]
    public void CancellationReturnsIdleAndDoesNotImplyNativeSessionResources()
    {
        var result = CaptureTargetSelectionResult.Canceled(
            EngineReadinessStatus.Initializing(
                EngineReadinessStage.Capture,
                "Choose a display or window to start the minimal HDR capture.",
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
            EngineReadinessStatus.Unsupported(
                EngineReadinessStage.Capture,
                "Unsupported capture",
                "GraphicsCaptureSession.IsSupported returned false."));

        var state = CaptureSessionState.FromStartResult(target, result);

        Assert.Equal(CaptureSessionStatus.Unsupported, state.Status);
        Assert.False(state.HasNativeSession);
        Assert.Equal(EngineReadinessState.Unsupported, state.Readiness.State);
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
        var readiness = EngineReadinessStatus.Unsupported(
            EngineReadinessStage.Graphics,
            "Unsupported capture",
            "Presentation support changed while capture resources were active.");

        var state = CaptureSessionState.FromReadiness(target, readiness);

        Assert.Equal(CaptureSessionStatus.Unsupported, state.Status);
        Assert.Same(target, state.Target);
        Assert.True(state.HasNativeSession);
        Assert.Equal(EngineReadinessStage.Graphics, state.Readiness.Stage);
    }

    private static CaptureTarget CreateTarget() =>
        CaptureTarget.CreateForTest(
            new SizeInt32 { Width = 1920, Height = 1080 },
            "Test Display");
}
