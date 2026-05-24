using Lumiere.Graphics.Output;
using Lumiere.Graphics.Presentation;
using Xunit;

namespace Lumiere.Graphics.Tests.Output;

public sealed class FolderOutputServiceTests
{
    [Fact]
    public async Task ExecuteOutputAsync_FolderDisabledSkipsWithoutWriting()
    {
        var encoder = new TestPngEncoder();
        var writes = 0;
        var service = CreateService(encoder, directoryExists: _ => true, write: (_, _, _) =>
        {
            writes++;
            return Task.CompletedTask;
        });

        var result = await service.ExecuteOutputAsync(CreateRequest(OutputTarget.Clipboard, "C:\\Captures"));

        Assert.False(result.IsSuccess);
        Assert.Equal(OutputOutcome.Skipped, result.FolderOutcome);
        Assert.Equal(0, encoder.Calls);
        Assert.Equal(0, writes);
    }

    [Theory]
    [InlineData(null, "Save path is not configured")]
    [InlineData("   ", "Save path is not configured")]
    public async Task ExecuteOutputAsync_MissingSavePathFails(string? savePath, string expectedMessage)
    {
        var service = CreateService(new TestPngEncoder(), directoryExists: _ => true);

        var result = await service.ExecuteOutputAsync(CreateRequest(OutputTarget.Folder, savePath));

        Assert.False(result.IsSuccess);
        Assert.Equal(OutputOutcome.Failed, result.FolderOutcome);
        Assert.Equal(expectedMessage, result.UserMessage);
    }

    [Fact]
    public async Task ExecuteOutputAsync_MissingDirectoryFails()
    {
        var service = CreateService(new TestPngEncoder(), directoryExists: _ => false);

        var result = await service.ExecuteOutputAsync(CreateRequest(OutputTarget.Folder, "C:\\Missing"));

        Assert.False(result.IsSuccess);
        Assert.Equal(OutputOutcome.Failed, result.FolderOutcome);
        Assert.Equal("Save folder is not available", result.UserMessage);
    }

    [Fact]
    public async Task ExecuteOutputAsync_SuccessWritesArtifactAndReportsPath()
    {
        var encoder = new TestPngEncoder();
        var writtenPaths = new List<string>();
        var service = CreateService(encoder, directoryExists: _ => true, write: (path, bytes, _) =>
        {
            writtenPaths.Add(path);
            Assert.Equal([1, 2, 3], bytes);
            return Task.CompletedTask;
        });

        var result = await service.ExecuteOutputAsync(CreateRequest(OutputTarget.Folder, "C:\\Captures"));

        Assert.True(result.IsSuccess);
        Assert.Equal(OutputOutcome.Success, result.FolderOutcome);
        Assert.Equal("C:\\Captures\\Lumiere-20260523-090807-123.png", result.Targets.Single().ArtifactPath);
        Assert.Equal(result.Targets.Single().ArtifactPath, writtenPaths.Single());
        Assert.Equal(1, encoder.Calls);
    }

    [Fact]
    public async Task ExecuteOutputAsync_WriteFailureReturnsRecoverableFailure()
    {
        var service = CreateService(
            new TestPngEncoder(),
            directoryExists: _ => true,
            write: (_, _, _) => throw new UnauthorizedAccessException("Denied"));

        var result = await service.ExecuteOutputAsync(CreateRequest(OutputTarget.Folder, "C:\\Captures"));

        Assert.False(result.IsSuccess);
        Assert.Equal(OutputOutcome.Failed, result.FolderOutcome);
        Assert.Equal("Failed to save file", result.UserMessage);
        Assert.Contains("Denied", result.TechnicalDetail);
    }

    private static FolderOutputService CreateService(
        TestPngEncoder encoder,
        Func<string, bool> directoryExists,
        Func<string, byte[], CancellationToken, Task>? write = null)
    {
        var pathPolicy = new OutputFolderPathPolicy(() => new DateTimeOffset(2026, 5, 23, 9, 8, 7, 123, TimeSpan.Zero));
        return new FolderOutputService(
            encoder,
            pathPolicy,
            directoryExists,
            _ => false,
            write ?? ((_, _, _) => Task.CompletedTask));
    }

    private static OutputRequest CreateRequest(OutputTarget target, string? savePath) =>
        new()
        {
            Texture = new CapturedFrameTexture(null, 16, 16, "Test frame"),
            Policy = OutputPolicy.FromSettings(
                target,
                copyAsImage: true,
                savePath,
                timestampNaming: true,
                afterCaptureBehavior: null),
        };

    private sealed class TestPngEncoder : IOutputPngEncoder
    {
        public int Calls { get; private set; }

        public Task<byte[]> EncodePngAsync(
            CapturedFrameTexture texture,
            CropPixelRect? cropRegion,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult<byte[]>([1, 2, 3]);
        }
    }
}
