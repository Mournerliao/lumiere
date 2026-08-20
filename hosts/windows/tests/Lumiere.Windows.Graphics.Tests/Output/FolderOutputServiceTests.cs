using Lumiere.Windows.Graphics.Output;
using Lumiere.Windows.Graphics.Presentation;
using Xunit;

namespace Lumiere.Windows.Graphics.Tests.Output;

public sealed class FolderOutputServiceTests
{
    [Fact]
    public async Task WritesTheSingleVisualMatchPngArtifact()
    {
        byte[]? writtenBytes = null;
        var service = new FolderOutputService(
            new FakeEncoder(),
            new OutputFolderPathPolicy(() => DateTimeOffset.UnixEpoch),
            _ => true,
            _ => false,
            (_, bytes, _) =>
            {
                writtenBytes = bytes;
                return Task.CompletedTask;
            });
        using var texture = new CapturedFrameTexture(null, 2, 2, "test");

        var result = await service.ExecuteOutputAsync(new OutputRequest
        {
            Texture = texture,
            Delivery = OutputTarget.Folder,
            SaveDirectory = "C:\\captures",
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(new byte[] { 1, 2, 3 }, writtenBytes);
        var artifactPath = Assert.IsType<string>(result.Targets.Single().ArtifactPath);
        Assert.EndsWith(".png", artifactPath, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeEncoder : IOutputPngEncoder
    {
        public Task<byte[]> EncodePngAsync(
            CapturedFrameTexture texture,
            CropPixelRect? cropRegion,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<byte[]>([1, 2, 3]);
    }
}
