using Lumiere.Windows.Capture;
using Lumiere.Windows.Graphics.Hdr;
using Lumiere.Windows.Graphics.Output;
using Lumiere.Windows.Graphics.Presentation;
using Vortice.DXGI;
using Windows.Graphics;
using Xunit;

namespace Lumiere.Windows.Capture.Tests;

public sealed class WindowsDisplayCaptureEngineTests
{
    [Fact]
    public async Task CaptureDisplayAsync_OwnsFrameDeliveryAndSessionTeardown()
    {
        var sessionDisposed = false;
        var target = CreateTarget();
        await using var engine = CreateEngine(
            target,
            (onFrame, _) =>
            {
                onFrame(new CapturedFrameTexture(null, 2, 2, "test frame"));
                return CaptureStartResult.StartSucceeded(
                    new CaptureSessionResources(() => sessionDisposed = true),
                    EngineReadinessStatus.Initializing("Capture started"));
            });

        var result = await engine.CaptureDisplayAsync(
            new WindowsCaptureRequest("request-1", OutputTarget.Folder, "C:\\captures"));

        Assert.Equal(WindowsCaptureOutcome.Delivered, result.Outcome);
        Assert.True(result.HasDeliveredArtifact);
        Assert.True(sessionDisposed);
    }

    [Fact]
    public async Task CaptureDisplayAsync_CancellationDisposesTheActiveSession()
    {
        var sessionStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sessionDisposed = false;
        var target = CreateTarget();
        await using var engine = CreateEngine(
            target,
            (_, _) =>
            {
                sessionStarted.SetResult();
                return CaptureStartResult.StartSucceeded(
                    new CaptureSessionResources(() => sessionDisposed = true),
                    EngineReadinessStatus.Initializing("Capture started"));
            });
        using var cancellation = new CancellationTokenSource();

        var capture = engine.CaptureDisplayAsync(
            new WindowsCaptureRequest("request-2", OutputTarget.Folder, "C:\\captures"),
            cancellation.Token);
        await sessionStarted.Task;
        await cancellation.CancelAsync();
        var result = await capture;

        Assert.Equal(WindowsCaptureOutcome.Cancelled, result.Outcome);
        Assert.True(sessionDisposed);
    }

    [Fact]
    public async Task CaptureRegionAsync_UsesIssuedTargetAndPixelAlignedCrop()
    {
        var target = CreateTarget();
        var output = new RecordingOutput();
        await using var engine = CreateEngine(
            target,
            (onFrame, _) =>
            {
                onFrame(new CapturedFrameTexture(null, 2, 2, "test frame"));
                return CaptureStartResult.StartSucceeded(
                    new CaptureSessionResources(() => { }),
                    EngineReadinessStatus.Initializing("Capture started"));
            },
            output);
        var targetSnapshot = WindowsTargetCapability.CreateForTest(
            WindowsTargetHdrState.Inactive,
            new WindowsTargetLogicalSize(1, 1),
            target);

        var result = await engine.CaptureRegionAsync(
            new WindowsCaptureRequest("request-region", OutputTarget.Folder, "C:\\captures"),
            targetSnapshot,
            new WindowsRegionGeometry(0.25, 0.25, 0.5, 0.5));

        Assert.Equal(WindowsCaptureOutcome.Delivered, result.Outcome);
        Assert.Equal(new CropPixelRect(0, 0, 2, 2), output.Request?.CropRegion);
    }

    [Fact]
    public async Task CaptureDisplayAsync_MapsTargetResolutionFailureToUnavailable()
    {
        await using var engine = new WindowsDisplayCaptureEngine(
            command => CaptureCommandResult.Accepted(command),
            _ => { },
            _ => Task.FromResult(CaptureTargetSelectionResult.Failed(
                EngineReadinessStatus.Failed(
                    EngineReadinessStage.Interop,
                    "Capture target selection failed",
                    "GetCursorPos returned access denied."))),
            _ => throw new InvalidOperationException("must not probe"),
            (_, _, _) => throw new InvalidOperationException("must not capture"),
            new SuccessfulOutput());

        var result = await engine.CaptureDisplayAsync(
            new WindowsCaptureRequest("request-unavailable", OutputTarget.Folder, "C:\\captures"));

        Assert.Equal(WindowsCaptureOutcome.Unavailable, result.Outcome);
    }

    private static WindowsDisplayCaptureEngine CreateEngine(
        CaptureTarget target,
        Func<Action<CapturedFrameTexture>, Action<EngineReadinessStatus>, CaptureStartResult> startCapture,
        IOutputService? output = null) =>
        new(
            command => CaptureCommandResult.Accepted(command),
            _ => { },
            _ => Task.FromResult(CaptureTargetSelectionResult.Selected(
                target,
                EngineReadinessStatus.Initializing("Target selected"))),
            _ => new HdrDisplayCapability(
                HdrDisplayState.Inactive,
                ColorSpaceType.RgbFullG22NoneP709,
                "test display"),
            (_, onFrame, onFailure) => startCapture(onFrame, onFailure),
            output ?? new SuccessfulOutput());

    private static CaptureTarget CreateTarget() =>
        CaptureTarget.CreateForTest(
            new SizeInt32 { Width = 2, Height = 2 },
            "test display",
            CaptureTargetKind.Display,
            new DisplayOutputIdentity("test display", 0, 0, 2, 2));

    private sealed class SuccessfulOutput : IOutputService
    {
        public Task<OutputResult> ExecuteOutputAsync(
            OutputRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(OutputResult.FromTargets(
                OutputTargetResult.Success(
                    OutputTarget.Folder,
                    "Saved to folder",
                    artifactPath: "C:\\captures\\test.png")));
    }

    private sealed class RecordingOutput : IOutputService
    {
        public OutputRequest? Request { get; private set; }

        public Task<OutputResult> ExecuteOutputAsync(
            OutputRequest request,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult(OutputResult.FromTargets(
                OutputTargetResult.Success(
                    OutputTarget.Folder,
                    "Saved to folder",
                    artifactPath: "C:\\captures\\test.png")));
        }
    }
}
