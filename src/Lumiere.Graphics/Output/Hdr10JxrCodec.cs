namespace Lumiere.Graphics.Output;

public interface IHdr10JxrCodec
{
    Task<byte[]> EncodeAsync(
        Hdr10JxrCodecInput input,
        CancellationToken cancellationToken = default);
}

public sealed record Hdr10JxrCodecInput
{
    public Hdr10JxrCodecInput(
        CapturedFrameReadback source,
        OutputProfileContract outputProfile)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(outputProfile);

        if (source.PixelFormat is not OutputPixelFormat.R16G16B16A16Float)
        {
            throw new ArgumentException(
                "HDR10 JXR codec input requires R16G16B16A16 float source data.",
                nameof(source));
        }

        if (!Hdr10JxrOutputEncoder.CanEncode(outputProfile))
        {
            throw new ArgumentException(
                "HDR10 JXR codec input requires a complete HDR10-preserved output profile.",
                nameof(outputProfile));
        }

        Source = source;
        OutputProfile = outputProfile;
    }

    public CapturedFrameReadback Source { get; }

    public OutputProfileContract OutputProfile { get; }
}

public sealed class PendingHdr10JxrCodec : IHdr10JxrCodec
{
    public Task<byte[]> EncodeAsync(
        Hdr10JxrCodecInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        throw new OutputArtifactEncodingException(
            "HDR10 JXR encoding is not implemented yet; native Windows WIC JPEG XR codec integration, HDR metadata, and viewer validation are required before this profile can be enabled.");
    }
}
