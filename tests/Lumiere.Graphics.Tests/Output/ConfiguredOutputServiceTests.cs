using Lumiere.Graphics.Output;
using Lumiere.Graphics.Presentation;
using Xunit;

namespace Lumiere.Graphics.Tests.Output;

public sealed class ConfiguredOutputServiceTests
{
    [Fact]
    public async Task ExecuteOutputAsync_BothTargetAttemptsClipboardAndFolder()
    {
        var clipboard = new TestOutputService(OutputTargetResult.Success(OutputTarget.Clipboard, "Copied"));
        var folder = new TestOutputService(OutputTargetResult.Success(OutputTarget.Folder, "Saved", artifactPath: "C:\\Captures\\a.png"));
        var service = new ConfiguredOutputService(clipboard, folder);

        var result = await service.ExecuteOutputAsync(CreateRequest(OutputTarget.Both));

        Assert.True(result.IsSuccess);
        Assert.Equal(OutputOutcome.Success, result.ClipboardOutcome);
        Assert.Equal(OutputOutcome.Success, result.FolderOutcome);
        Assert.Equal("Output complete", result.UserMessage);
        Assert.Equal(1, clipboard.Calls);
        Assert.Equal(1, folder.Calls);
    }

    [Fact]
    public async Task ExecuteOutputAsync_PartialSuccessPreservesFailedTarget()
    {
        var clipboard = new TestOutputService(OutputTargetResult.Success(OutputTarget.Clipboard, "Copied"));
        var folder = new TestOutputService(OutputTargetResult.Failed(OutputTarget.Folder, "Failed to save", "Denied"));
        var service = new ConfiguredOutputService(clipboard, folder);

        var result = await service.ExecuteOutputAsync(CreateRequest(OutputTarget.Both));

        Assert.True(result.IsSuccess);
        Assert.Equal(OutputOutcome.Success, result.ClipboardOutcome);
        Assert.Equal(OutputOutcome.Failed, result.FolderOutcome);
        Assert.Equal("Output partially complete", result.UserMessage);
        Assert.Contains("Denied", result.TechnicalDetail);
    }

    [Fact]
    public async Task ExecuteOutputAsync_ServiceExceptionBecomesFailedTargetAndOtherTargetStillRuns()
    {
        var clipboard = new ThrowingOutputService(new InvalidOperationException("Clipboard unavailable"));
        var folder = new TestOutputService(OutputTargetResult.Success(OutputTarget.Folder, "Saved"));
        var service = new ConfiguredOutputService(clipboard, folder);

        var result = await service.ExecuteOutputAsync(CreateRequest(OutputTarget.Both));

        Assert.True(result.IsSuccess);
        Assert.Equal(OutputOutcome.Failed, result.ClipboardOutcome);
        Assert.Equal(OutputOutcome.Success, result.FolderOutcome);
        Assert.Contains("Clipboard unavailable", result.TechnicalDetail);
        Assert.Equal(1, folder.Calls);
    }

    [Fact]
    public async Task ExecuteOutputAsync_TargetTimeoutBecomesFailedTargetAndOtherTargetStillRuns()
    {
        var clipboard = new NeverCompletingOutputService();
        var folder = new TestOutputService(OutputTargetResult.Success(OutputTarget.Folder, "Saved"));
        var service = new ConfiguredOutputService(clipboard, folder, TimeSpan.FromMilliseconds(10));

        var result = await service.ExecuteOutputAsync(CreateRequest(OutputTarget.Both));

        Assert.True(result.IsSuccess);
        Assert.Equal(OutputOutcome.Failed, result.ClipboardOutcome);
        Assert.Equal(OutputOutcome.Success, result.FolderOutcome);
        Assert.Contains("timed out", result.Targets.Single(target => target.Target == OutputTarget.Clipboard).UserMessage);
        Assert.Equal(1, folder.Calls);
    }

    [Theory]
    [InlineData(OutputTarget.Clipboard, 1, 0)]
    [InlineData(OutputTarget.Folder, 0, 1)]
    public async Task ExecuteOutputAsync_SingleTargetCallsOnlyConfiguredService(
        OutputTarget target,
        int expectedClipboardCalls,
        int expectedFolderCalls)
    {
        var clipboard = new TestOutputService(OutputTargetResult.Success(OutputTarget.Clipboard, "Copied"));
        var folder = new TestOutputService(OutputTargetResult.Success(OutputTarget.Folder, "Saved"));
        var service = new ConfiguredOutputService(clipboard, folder);

        await service.ExecuteOutputAsync(CreateRequest(target));

        Assert.Equal(expectedClipboardCalls, clipboard.Calls);
        Assert.Equal(expectedFolderCalls, folder.Calls);
    }

    private static OutputRequest CreateRequest(OutputTarget target) =>
        new()
        {
            Texture = new CapturedFrameTexture(null, 16, 16, "Test frame"),
            Policy = OutputPolicy.FromSettings(
                target,
                copyAsImage: true,
                savePath: "C:\\Captures",
                timestampNaming: true,
                afterCaptureBehavior: null),
        };

    private sealed class TestOutputService : IOutputService
    {
        private readonly OutputTargetResult result;

        public TestOutputService(OutputTargetResult result)
        {
            this.result = result;
        }

        public int Calls { get; private set; }

        public Task<OutputResult> ExecuteOutputAsync(OutputRequest request, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(OutputResult.FromTargets(result));
        }
    }

    private sealed class ThrowingOutputService : IOutputService
    {
        private readonly Exception exception;

        public ThrowingOutputService(Exception exception)
        {
            this.exception = exception;
        }

        public Task<OutputResult> ExecuteOutputAsync(OutputRequest request, CancellationToken cancellationToken = default) =>
            throw exception;
    }

    private sealed class NeverCompletingOutputService : IOutputService
    {
        public async Task<OutputResult> ExecuteOutputAsync(OutputRequest request, CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new UnreachableException();
        }
    }

    private sealed class UnreachableException : Exception;
}
