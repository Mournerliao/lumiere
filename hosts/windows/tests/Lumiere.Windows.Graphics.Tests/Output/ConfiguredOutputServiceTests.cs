using Lumiere.Windows.Graphics.Output;
using Lumiere.Windows.Graphics.Presentation;
using Xunit;

namespace Lumiere.Windows.Graphics.Tests.Output;

public sealed class ConfiguredOutputServiceTests
{
    [Fact]
    public async Task BothTargetsReceiveOneEncodedArtifact()
    {
        var encoder = new RecordingEncoder();
        var clipboard = new RecordingOutputTarget(OutputTarget.Clipboard);
        var folder = new RecordingOutputTarget(OutputTarget.Folder);
        var service = new ConfiguredOutputService(encoder, clipboard, folder);
        using var texture = new CapturedFrameTexture(null, 10, 10, "test");

        var result = await service.ExecuteOutputAsync(new OutputRequest
        {
            Texture = texture,
            Delivery = OutputTarget.Both,
            SaveDirectory = "C:\\captures",
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(1, encoder.EncodeCount);
        Assert.Same(clipboard.Artifact, folder.Artifact);
    }

    [Fact]
    public async Task EncodingFailurePropagatesBeforeDelivery()
    {
        var clipboard = new RecordingOutputTarget(OutputTarget.Clipboard);
        var folder = new RecordingOutputTarget(OutputTarget.Folder);
        var service = new ConfiguredOutputService(new ThrowingEncoder(), clipboard, folder);
        using var texture = new CapturedFrameTexture(null, 10, 10, "test");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteOutputAsync(
            new OutputRequest
            {
                Texture = texture,
                Delivery = OutputTarget.Both,
                SaveDirectory = "C:\\captures",
            }));

        Assert.Null(clipboard.Artifact);
        Assert.Null(folder.Artifact);
    }

    private sealed class RecordingEncoder : IOutputPngEncoder
    {
        public int EncodeCount { get; private set; }

        public Task<OutputEncodedArtifact> EncodeArtifactAsync(
            CapturedFrameTexture texture,
            CropPixelRect? cropRegion,
            SrgbVisualMatchConversionContext context,
            CancellationToken cancellationToken = default)
        {
            EncodeCount++;
            return Task.FromResult(new OutputEncodedArtifact([1, 2, 3], "png"));
        }
    }

    private sealed class ThrowingEncoder : IOutputPngEncoder
    {
        public Task<OutputEncodedArtifact> EncodeArtifactAsync(
            CapturedFrameTexture texture,
            CropPixelRect? cropRegion,
            SrgbVisualMatchConversionContext context,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("encoding failed");
    }

    private sealed class RecordingOutputTarget(OutputTarget target) : IOutputTargetAdapter
    {
        public OutputTarget Target => target;

        public OutputEncodedArtifact? Artifact { get; private set; }

        public Task<OutputTargetResult> DeliverAsync(
            OutputRequest request,
            OutputEncodedArtifact artifact,
            CancellationToken cancellationToken)
        {
            Artifact = artifact;
            return Task.FromResult(OutputTargetResult.Success(target, "complete"));
        }
    }
}
