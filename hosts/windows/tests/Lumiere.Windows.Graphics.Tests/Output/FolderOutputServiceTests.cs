using Lumiere.Windows.Graphics.Output;
using Xunit;

namespace Lumiere.Windows.Graphics.Tests.Output;

public sealed class FolderOutputServiceTests
{
    [Fact]
    public async Task WritesTheSingleVisualMatchPngArtifact()
    {
        byte[]? writtenBytes = null;
        var service = new FolderOutputService(
            new OutputFolderPathPolicy(() => DateTimeOffset.UnixEpoch),
            _ => true,
            _ => false,
            (_, bytes, _) =>
            {
                writtenBytes = bytes;
                return Task.CompletedTask;
            });
        var result = await service.DeliverAsync(
            new OutputRequest
            {
                Texture = new(null, 2, 2, "test"),
                Delivery = OutputTarget.Folder,
                SaveDirectory = "C:\\captures",
            },
            new OutputEncodedArtifact([1, 2, 3], "png"),
            CancellationToken.None);

        Assert.Equal(OutputOutcome.Success, result.Outcome);
        Assert.Equal(new byte[] { 1, 2, 3 }, writtenBytes);
        var artifactPath = Assert.IsType<string>(result.ArtifactPath);
        Assert.EndsWith(".png", artifactPath, StringComparison.OrdinalIgnoreCase);
    }
}
