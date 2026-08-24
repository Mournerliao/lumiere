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

    private static WindowsDisplayCaptureEngine CreateEngine(
        CaptureTarget target,
        Func<Action<CapturedFrameTexture>, Action<EngineReadinessStatus>, CaptureStartResult> startCapture) =>
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
            new SuccessfulOutput());

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
}
