using Lumiere.Graphics.Output;
using Lumiere.Graphics.Presentation;
using Xunit;

namespace Lumiere.Graphics.Tests.Output;

public sealed class Hdr10JxrOutputEncoderTests
{
    [Fact]
    public void CanEncode_RequiresCompleteHdr10PreservedContract()
    {
        var profile = OutputProfileContract.Hdr10Pq with
        {
            FidelityMode = OutputFidelityMode.HdrPreserved,
            FormatContract = CompleteHdr10Contract,
        };

        Assert.True(Hdr10JxrOutputEncoder.CanEncode(profile));
    }

    [Fact]
    public void CanEncode_RejectsCompatibilityProfile()
    {
        Assert.False(Hdr10JxrOutputEncoder.CanEncode(OutputProfileContract.SrgbCompatibilityPng));
    }

    [Fact]
    public void CanEncode_RejectsIncompleteHdr10Contract()
    {
        var profile = OutputProfileContract.Hdr10Pq with
        {
            FidelityMode = OutputFidelityMode.HdrPreserved,
        };

        Assert.False(Hdr10JxrOutputEncoder.CanEncode(profile));
    }

    [Fact]
    public async Task EncodeArtifactAsync_FailsUntilWindowsHdrJxrEncodingIsImplemented()
    {
        var readback = new TestReadback();
        var encoder = new Hdr10JxrOutputEncoder(readback);
        var profile = OutputProfileContract.Hdr10Pq with
        {
            FidelityMode = OutputFidelityMode.HdrPreserved,
            FormatContract = CompleteHdr10Contract,
        };
        using var texture = new CapturedFrameTexture(null, 16, 16, "Test frame");

        var exception = await Assert.ThrowsAsync<OutputArtifactEncodingException>(() =>
            encoder.EncodeArtifactAsync(texture, cropRegion: null, profile));

        Assert.Contains("not implemented", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("metadata", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validation", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, readback.Calls);
        Assert.Same(texture, readback.Texture);
    }

    [Fact]
    public async Task EncodeArtifactAsync_RejectsSrgbCompatibilityProfile()
    {
        var readback = new TestReadback();
        var encoder = new Hdr10JxrOutputEncoder(readback);
        using var texture = new CapturedFrameTexture(null, 16, 16, "Test frame");

        var exception = await Assert.ThrowsAsync<OutputArtifactEncodingException>(() =>
            encoder.EncodeArtifactAsync(texture, cropRegion: null, OutputProfileContract.SrgbCompatibilityPng));

        Assert.Contains("cannot create sRGB", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, readback.Calls);
    }

    [Fact]
    public async Task EncodePngAsync_RejectsCompatibilityPngPath()
    {
        var encoder = new Hdr10JxrOutputEncoder(new TestReadback());
        using var texture = new CapturedFrameTexture(null, 16, 16, "Test frame");

        var exception = await Assert.ThrowsAsync<OutputArtifactEncodingException>(() =>
            encoder.EncodePngAsync(texture, cropRegion: null));

        Assert.Contains("cannot produce sRGB PNG", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CapturedFrameReadback_RequiresRgba16FloatByteLength()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new CapturedFrameReadback(2, 2, OutputPixelFormat.R16G16B16A16Float, [1, 2, 3]));

        Assert.Contains("32 bytes", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static OutputFormatContract CompleteHdr10Contract { get; } =
        new(
            OutputPixelFormat.R16G16B16A16Float,
            OutputPixelFormat.R16G16B16A16Float,
            OutputTransferFunction.PqSt2084,
            OutputColorPrimaries.Bt2020,
            OutputConversionPolicy.PreserveHdrWithDefinedToneMapping,
            OutputMetadataPolicy.AttachHdr10StaticMetadata,
            OutputTargetAppAssumption.RequiresHdrViewerValidation);

    private sealed class TestReadback : ICapturedFrameTextureReadback
    {
        public int Calls { get; private set; }

        public CapturedFrameTexture? Texture { get; private set; }

        public CropPixelRect? CropRegion { get; private set; }

        public CapturedFrameReadback ReadRgba16Float(
            CapturedFrameTexture texture,
            CropPixelRect? cropRegion)
        {
            Calls++;
            Texture = texture;
            CropRegion = cropRegion;
            return new CapturedFrameReadback(
                texture.Width,
                texture.Height,
                OutputPixelFormat.R16G16B16A16Float,
                new byte[texture.Width * texture.Height * CapturedFrameReadback.BytesPerPixel]);
        }
    }
}
