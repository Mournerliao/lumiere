using Lumiere.Graphics.Output;
using Lumiere.Graphics.Presentation;
using Lumiere.Infrastructure.Interop;
using Xunit;

namespace Lumiere.Graphics.Tests.Output;

public sealed class AfterCaptureOutputServiceTests
{
    [Theory]
    [InlineData("Open", ArtifactShellActionKind.Open, AfterCaptureOutcome.Success)]
    [InlineData("Reveal", ArtifactShellActionKind.Reveal, AfterCaptureOutcome.Success)]
    public async Task ExecuteOutputAsync_RunsSupportedActionForFolderArtifact(
        string afterCaptureBehavior,
        ArtifactShellActionKind expectedShellAction,
        AfterCaptureOutcome expectedOutcome)
    {
        var artifactPath = "C:\\Captures\\Lumiere.png";
        var inner = new TestOutputService(OutputTargetResult.Success(
            OutputTarget.Folder,
            "Saved",
            artifactPath: artifactPath));
        var shell = new TestArtifactShellAction(ArtifactShellActionResult.Success());
        var service = new AfterCaptureOutputService(inner, shell);

        var result = await service.ExecuteOutputAsync(CreateRequest(OutputTarget.Folder, afterCaptureBehavior));

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedOutcome, result.AfterCapture?.Outcome);
        Assert.Equal(artifactPath, result.AfterCapture?.ArtifactPath);
        Assert.Equal(expectedShellAction, shell.Actions.Single());
        Assert.Equal(artifactPath, shell.Paths.Single());
    }

    [Fact]
    public async Task ExecuteOutputAsync_ClipboardOnlySkipsAfterCaptureAction()
    {
        var inner = new TestOutputService(OutputTargetResult.Success(OutputTarget.Clipboard, "Copied"));
        var shell = new TestArtifactShellAction(ArtifactShellActionResult.Success());
        var service = new AfterCaptureOutputService(inner, shell);

        var result = await service.ExecuteOutputAsync(CreateRequest(OutputTarget.Clipboard, "Open"));

        Assert.True(result.IsSuccess);
        Assert.Equal(AfterCaptureOutcome.Skipped, result.AfterCapture?.Outcome);
        Assert.Empty(shell.Actions);
    }

    [Fact]
    public async Task ExecuteOutputAsync_ActionFailureDoesNotChangeOutputSuccess()
    {
        var inner = new TestOutputService(OutputTargetResult.Success(
            OutputTarget.Folder,
            "Saved",
            artifactPath: "C:\\Captures\\Lumiere.png"));
        var shell = new TestArtifactShellAction(ArtifactShellActionResult.Failed("Explorer unavailable"));
        var service = new AfterCaptureOutputService(inner, shell);

        var result = await service.ExecuteOutputAsync(CreateRequest(OutputTarget.Folder, "Reveal"));

        Assert.True(result.IsSuccess);
        Assert.Equal(OutputOutcome.Success, result.FolderOutcome);
        Assert.Equal(AfterCaptureOutcome.Failed, result.AfterCapture?.Outcome);
        Assert.Contains("Explorer unavailable", result.AfterCapture?.TechnicalDetail);
    }

    [Fact]
    public async Task ExecuteOutputAsync_NoneRecordsNotRequested()
    {
        var inner = new TestOutputService(OutputTargetResult.Success(
            OutputTarget.Folder,
            "Saved",
            artifactPath: "C:\\Captures\\Lumiere.png"));
        var shell = new TestArtifactShellAction(ArtifactShellActionResult.Success());
        var service = new AfterCaptureOutputService(inner, shell);

        var result = await service.ExecuteOutputAsync(CreateRequest(OutputTarget.Folder, "None"));

        Assert.True(result.IsSuccess);
        Assert.Equal(AfterCaptureOutcome.NotRequested, result.AfterCapture?.Outcome);
        Assert.Empty(shell.Actions);
    }

    private static OutputRequest CreateRequest(OutputTarget target, string afterCaptureBehavior) =>
        new()
        {
            Texture = new CapturedFrameTexture(null, 16, 16, "Test frame"),
            Policy = OutputPolicy.FromSettings(
                target,
                copyAsImage: true,
                savePath: "C:\\Captures",
                timestampNaming: true,
                afterCaptureBehavior: afterCaptureBehavior),
        };

    private sealed class TestOutputService : IOutputService
    {
        private readonly OutputTargetResult result;

        public TestOutputService(OutputTargetResult result)
        {
            this.result = result;
        }

        public Task<OutputResult> ExecuteOutputAsync(OutputRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(OutputResult.FromTargets(result));
    }

    private sealed class TestArtifactShellAction : IArtifactShellAction
    {
        private readonly ArtifactShellActionResult result;

        public TestArtifactShellAction(ArtifactShellActionResult result)
        {
            this.result = result;
        }

        public List<ArtifactShellActionKind> Actions { get; } = [];

        public List<string> Paths { get; } = [];

        public Task<ArtifactShellActionResult> ExecuteAsync(
            string artifactPath,
            ArtifactShellActionKind action,
            CancellationToken cancellationToken = default)
        {
            Paths.Add(artifactPath);
            Actions.Add(action);
            return Task.FromResult(result);
        }
    }
}
