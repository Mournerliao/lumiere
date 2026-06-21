using Lumiere.Graphics.Output;
using Lumiere.Graphics.Presentation;
using Lumiere.Infrastructure.Interop;
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
        var encoder = new Hdr10JxrOutputEncoder(readback, new PendingHdr10JxrCodec());
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
        Assert.Contains("WIC JPEG XR", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, readback.Calls);
        Assert.Same(texture, readback.Texture);
    }

    [Fact]
    public async Task EncodeArtifactAsync_ReturnsJxrArtifactWhenCodecSucceeds()
    {
        var readback = new TestReadback();
        var codec = new TestCodec([4, 5, 6]);
        var encoder = new Hdr10JxrOutputEncoder(readback, codec);
        var profile = OutputProfileContract.Hdr10Pq with
        {
            FidelityMode = OutputFidelityMode.HdrPreserved,
            FormatContract = CompleteHdr10Contract,
        };
        using var texture = new CapturedFrameTexture(null, 16, 16, "Test frame");
        var cropRegion = new CropPixelRect(2, 4, 8, 6);

        var artifact = await encoder.EncodeArtifactAsync(texture, cropRegion, profile);

        Assert.Equal([4, 5, 6], artifact.Bytes);
        Assert.Equal("jxr", artifact.NormalizedFileExtension);
        Assert.Equal(OutputProfileKind.Hdr10Pq, artifact.Profile.Kind);
        Assert.Equal(1, readback.Calls);
        Assert.Equal(cropRegion, readback.CropRegion);
        Assert.NotNull(codec.Input);
        Assert.Equal(OutputProfileKind.Hdr10Pq, codec.Input.OutputProfile.Kind);
        Assert.Equal(OutputPixelFormat.R16G16B16A16Float, codec.Input.Source.PixelFormat);
        Assert.True(codec.Input.StaticMetadataPolicy.IsComplete);
        Assert.Equal(Hdr10StaticMetadataSource.Bt2020PqReference, codec.Input.StaticMetadataPolicy.Source);
    }

    [Fact]
    public async Task EncodeArtifactAsync_RejectsSrgbCompatibilityProfile()
    {
        var readback = new TestReadback();
        var encoder = new Hdr10JxrOutputEncoder(readback, new PendingHdr10JxrCodec());
        using var texture = new CapturedFrameTexture(null, 16, 16, "Test frame");

        var exception = await Assert.ThrowsAsync<OutputArtifactEncodingException>(() =>
            encoder.EncodeArtifactAsync(texture, cropRegion: null, OutputProfileContract.SrgbCompatibilityPng));

        Assert.Contains("cannot create sRGB", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, readback.Calls);
    }

    [Fact]
    public async Task EncodePngAsync_RejectsCompatibilityPngPath()
    {
        var encoder = new Hdr10JxrOutputEncoder(new TestReadback(), new PendingHdr10JxrCodec());
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

    [Fact]
    public void Hdr10JxrCodecInput_RejectsIncompleteProfile()
    {
        var source = new CapturedFrameReadback(
            1,
            1,
            OutputPixelFormat.R16G16B16A16Float,
            new byte[CapturedFrameReadback.BytesPerPixel]);

        var exception = Assert.Throws<ArgumentException>(() =>
            new Hdr10JxrCodecInput(source, OutputProfileContract.Hdr10Pq));

        Assert.Contains("complete HDR10-preserved", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CanEncode_RejectsHdr10ContractWithoutAuditableStaticMetadata()
    {
        var profile = OutputProfileContract.Hdr10Pq with
        {
            FidelityMode = OutputFidelityMode.HdrPreserved,
            FormatContract = CompleteHdr10Contract with
            {
                Hdr10StaticMetadataPolicy = Hdr10StaticMetadataPolicy.Undefined,
            },
        };

        Assert.False(profile.HasCompleteFormatContract);
        Assert.False(Hdr10JxrOutputEncoder.CanEncode(profile));
    }

    [Fact]
    public void PendingHdr10JxrCodec_ReadinessRecordsImplementationAndValidationBlockers()
    {
        var readiness = new PendingHdr10JxrCodec().Readiness;

        Assert.False(readiness.IsReady);
        Assert.False(readiness.HasNativeWicJpegXrEncoder);
        Assert.True(readiness.AcceptsRgba16FloatSource);
        Assert.False(readiness.WritesHdr10Metadata);
        Assert.False(readiness.HasWindowsManualViewerValidation);
        Assert.Contains(readiness.Blockers, blocker => blocker.Contains("WIC JPEG XR", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(readiness.Blockers, blocker => blocker.Contains("metadata", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(readiness.Blockers, blocker => blocker.Contains("viewer validation", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void WicHdr10JxrCodec_ReadinessReflectsNativeEncoderButKeepsMetadataAndValidationBlocked()
    {
        var nativeEncoder = new TestWicJpegXrEncoder(
            new WicJpegXrEncoderReadiness(
                HasWindowsWicFactory: true,
                HasJpegXrContainerEncoder: true,
                AcceptsRgbaHalfPixelFormat: true,
                Blockers: []),
            [9, 8, 7]);
        var codec = new WicHdr10JxrCodec(nativeEncoder);

        var readiness = codec.Readiness;

        Assert.False(readiness.IsReady);
        Assert.True(readiness.HasNativeWicJpegXrEncoder);
        Assert.True(readiness.AcceptsRgba16FloatSource);
        Assert.False(readiness.WritesHdr10Metadata);
        Assert.Contains(readiness.Blockers, blocker => blocker.Contains("metadata writer", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(readiness.Blockers, blocker => blocker.Contains("manual viewer validation", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task WicHdr10JxrCodec_MapsRgbaHalfReadbackIntoNativeJpegXrRequest()
    {
        var sourceBytes = Enumerable.Range(0, CapturedFrameReadback.BytesPerPixel * 2)
            .Select(value => (byte)value)
            .ToArray();
        var source = new CapturedFrameReadback(
            2,
            1,
            OutputPixelFormat.R16G16B16A16Float,
            sourceBytes);
        var profile = OutputProfileContract.Hdr10Pq with
        {
            FidelityMode = OutputFidelityMode.HdrPreserved,
            FormatContract = CompleteHdr10Contract,
        };
        var nativeEncoder = new TestWicJpegXrEncoder(
            WicJpegXrEncoderReadiness.Unknown with
            {
                HasWindowsWicFactory = true,
                HasJpegXrContainerEncoder = true,
                AcceptsRgbaHalfPixelFormat = true,
                Blockers = [],
            },
            [1, 2, 3]);
        var codec = new WicHdr10JxrCodec(nativeEncoder);

        var encoded = await codec.EncodeAsync(new Hdr10JxrCodecInput(source, profile));

        Assert.Equal([1, 2, 3], encoded);
        Assert.NotNull(nativeEncoder.Request);
        Assert.Equal(2, nativeEncoder.Request.Width);
        Assert.Equal(1, nativeEncoder.Request.Height);
        Assert.Equal(16, nativeEncoder.Request.StrideBytes);
        Assert.Same(sourceBytes, nativeEncoder.Request.RgbaHalfPixels);
        Assert.Contains(
            nativeEncoder.Request.Metadata,
            entry => entry.QueryPath == "/xmp/Lumiere:Hdr10MetadataSource"
                && entry.Value == Hdr10StaticMetadataSource.Bt2020PqReference.ToString());
        Assert.Contains(
            nativeEncoder.Request.Metadata,
            entry => entry.QueryPath == "/xmp/Lumiere:MaxContentLightLevelNits"
                && entry.Value == "1000");
        Assert.Contains(
            nativeEncoder.Request.Metadata,
            entry => entry.QueryPath == "/xmp/Lumiere:MetadataPolicyDetail"
                && entry.Value.Contains("policy input", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void WicJpegXrMetadataEntry_RequiresQueryPathAndValue()
    {
        Assert.Throws<ArgumentException>(() => new WicJpegXrMetadataEntry("", "value"));
        Assert.Throws<ArgumentException>(() => new WicJpegXrMetadataEntry("/xmp/Lumiere:Test", ""));
    }

    private static OutputFormatContract CompleteHdr10Contract { get; } =
        new(
            OutputPixelFormat.R16G16B16A16Float,
            OutputPixelFormat.R16G16B16A16Float,
            OutputTransferFunction.PqSt2084,
            OutputColorPrimaries.Bt2020,
            OutputConversionPolicy.PreserveHdrWithDefinedToneMapping,
            OutputMetadataPolicy.AttachHdr10StaticMetadata,
            OutputTargetAppAssumption.RequiresHdrViewerValidation,
            Hdr10StaticMetadataPolicy.Bt2020PqReference1000Nit);

    private sealed class TestCodec : IHdr10JxrCodec
    {
        private readonly byte[] bytes;

        public TestCodec(byte[] bytes)
        {
            this.bytes = bytes;
        }

        public Hdr10JxrCodecInput? Input { get; private set; }

        public Hdr10JxrCodecReadiness Readiness { get; } =
            new(
                HasNativeWicJpegXrEncoder: true,
                AcceptsRgba16FloatSource: true,
                WritesHdr10Metadata: true,
                Hdr10StaticMetadataPolicy: Hdr10StaticMetadataPolicy.Bt2020PqReference1000Nit,
                HasWindowsManualViewerValidation: true,
                Blockers: []);

        public Task<byte[]> EncodeAsync(
            Hdr10JxrCodecInput input,
            CancellationToken cancellationToken = default)
        {
            Input = input;
            return Task.FromResult(bytes);
        }
    }

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

    private sealed class TestWicJpegXrEncoder : IWicJpegXrEncoder
    {
        private readonly byte[] encodedBytes;

        public TestWicJpegXrEncoder(
            WicJpegXrEncoderReadiness readiness,
            byte[] encodedBytes)
        {
            Readiness = readiness;
            this.encodedBytes = encodedBytes;
        }

        public WicJpegXrEncoderReadiness Readiness { get; }

        public WicJpegXrEncodeRequest? Request { get; private set; }

        public byte[] EncodeRgbaHalf(WicJpegXrEncodeRequest request)
        {
            Request = request;
            return encodedBytes;
        }
    }
}
