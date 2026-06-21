using Lumiere.Graphics.Presentation;

namespace Lumiere.Graphics.Output;

public sealed class Hdr10JxrOutputEncoder : IOutputPngEncoder
{
    public const string FileExtension = "jxr";

    private readonly ICapturedFrameTextureReadback readback;
    private readonly IHdr10JxrCodec codec;

    public Hdr10JxrOutputEncoder(
        ICapturedFrameTextureReadback readback,
        IHdr10JxrCodec codec)
    {
        this.readback = readback ?? throw new ArgumentNullException(nameof(readback));
        this.codec = codec ?? throw new ArgumentNullException(nameof(codec));
    }

    public Task<byte[]> EncodePngAsync(
        CapturedFrameTexture texture,
        CropPixelRect? cropRegion,
        CancellationToken cancellationToken = default) =>
        throw new OutputArtifactEncodingException(
            "HDR10 JXR encoder cannot produce sRGB PNG compatibility artifacts.");

    public async Task<OutputEncodedArtifact> EncodeArtifactAsync(
        CapturedFrameTexture texture,
        CropPixelRect? cropRegion,
        OutputProfileContract outputProfile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(texture);
        ArgumentNullException.ThrowIfNull(outputProfile);

        if (!CanEncode(outputProfile))
        {
            throw new OutputArtifactEncodingException(
                $"HDR10 JXR encoder cannot create {outputProfile.Label} artifacts.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var source = readback.ReadRgba16Float(texture, cropRegion);
        if (source.PixelFormat is not OutputPixelFormat.R16G16B16A16Float)
        {
            throw new OutputArtifactEncodingException(
                "HDR10 JXR encoding requires R16G16B16A16 float source readback.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var encodedBytes = await codec.EncodeAsync(
            new Hdr10JxrCodecInput(source, outputProfile),
            cancellationToken);

        return new OutputEncodedArtifact(
            encodedBytes,
            FileExtension,
            outputProfile);
    }

    public static bool CanEncode(OutputProfileContract outputProfile)
    {
        ArgumentNullException.ThrowIfNull(outputProfile);
        return outputProfile.Kind is OutputProfileKind.Hdr10Pq
            && outputProfile.FidelityMode is OutputFidelityMode.HdrPreserved
            && outputProfile.FormatContract is
            {
                SourcePixelFormat: OutputPixelFormat.R16G16B16A16Float,
                DestinationPixelFormat: OutputPixelFormat.R16G16B16A16Float,
                TransferFunction: OutputTransferFunction.PqSt2084,
                ColorPrimaries: OutputColorPrimaries.Bt2020,
                ConversionPolicy: OutputConversionPolicy.PreserveHdrWithDefinedToneMapping,
                MetadataPolicy: OutputMetadataPolicy.AttachHdr10StaticMetadata,
                TargetAppAssumption: OutputTargetAppAssumption.RequiresHdrViewerValidation,
            };
    }
}
