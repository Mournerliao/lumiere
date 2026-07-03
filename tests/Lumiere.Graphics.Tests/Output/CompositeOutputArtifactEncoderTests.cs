using Lumiere.Graphics.Output;
using Lumiere.Graphics.Presentation;
using Xunit;

namespace Lumiere.Graphics.Tests.Output;

public sealed class CompositeOutputArtifactEncoderTests
{
    [Fact]
    public async Task EncodeArtifactAsync_RoutesCompatibilityProfileToCompatibilityEncoder()
    {
        var compatibility = new TestEncoder(OutputProfileKind.SrgbCompatibilityPng, "png");
        var hdr10 = new TestEncoder(OutputProfileKind.Hdr10Pq, "jxr");
        var encoder = new CompositeOutputArtifactEncoder(compatibility, hdr10);
        using var texture = new CapturedFrameTexture(null, 16, 16, "Test frame");

        var artifact = await encoder.EncodeArtifactAsync(
            texture,
            cropRegion: null,
            OutputProfileContract.SrgbCompatibilityPng);

        Assert.Equal("png", artifact.NormalizedFileExtension);
        Assert.Equal(1, compatibility.ArtifactCalls);
        Assert.Equal(0, hdr10.ArtifactCalls);
    }

    [Fact]
    public async Task EncodeArtifactAsync_RoutesHdr10ProfileToHdr10Encoder()
    {
        var compatibility = new TestEncoder(OutputProfileKind.SrgbCompatibilityPng, "png");
        var hdr10 = new TestEncoder(OutputProfileKind.Hdr10Pq, "jxr");
        var encoder = new CompositeOutputArtifactEncoder(compatibility, hdr10);
        using var texture = new CapturedFrameTexture(null, 16, 16, "Test frame");

        var artifact = await encoder.EncodeArtifactAsync(
            texture,
            cropRegion: null,
            OutputProfileContract.Hdr10Pq);

        Assert.Equal("jxr", artifact.NormalizedFileExtension);
        Assert.Equal(0, compatibility.ArtifactCalls);
        Assert.Equal(1, hdr10.ArtifactCalls);
    }

    [Fact]
    public async Task EncodePngAsync_AlwaysUsesCompatibilityEncoder()
    {
        var compatibility = new TestEncoder(OutputProfileKind.SrgbCompatibilityPng, "png");
        var hdr10 = new TestEncoder(OutputProfileKind.Hdr10Pq, "jxr");
        var encoder = new CompositeOutputArtifactEncoder(compatibility, hdr10);
        using var texture = new CapturedFrameTexture(null, 16, 16, "Test frame");

        var bytes = await encoder.EncodePngAsync(texture, cropRegion: null);

        Assert.Equal([1, 2, 3], bytes);
        Assert.Equal(1, compatibility.PngCalls);
        Assert.Equal(0, hdr10.PngCalls);
    }

    private sealed class TestEncoder : IOutputPngEncoder
    {
        private readonly OutputProfileKind kind;
        private readonly string extension;

        public TestEncoder(OutputProfileKind kind, string extension)
        {
            this.kind = kind;
            this.extension = extension;
        }

        public int ArtifactCalls { get; private set; }

        public int PngCalls { get; private set; }

        public Task<byte[]> EncodePngAsync(
            CapturedFrameTexture texture,
            CropPixelRect? cropRegion,
            CancellationToken cancellationToken = default)
        {
            PngCalls++;
            return Task.FromResult<byte[]>([1, 2, 3]);
        }

        public Task<OutputEncodedArtifact> EncodeArtifactAsync(
            CapturedFrameTexture texture,
            CropPixelRect? cropRegion,
            OutputProfileContract outputProfile,
            CancellationToken cancellationToken = default,
            OutputArtifactCache? artifactCache = null)
        {
            ArtifactCalls++;
            return Task.FromResult(new OutputEncodedArtifact(
                [1, 2, 3],
                extension,
                outputProfile with { Kind = kind }));
        }
    }
}
