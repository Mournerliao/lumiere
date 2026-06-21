namespace Lumiere.Graphics.Output;

public interface IHdr10JxrCodec
{
    Hdr10JxrCodecReadiness Readiness { get; }

    Task<byte[]> EncodeAsync(
        Hdr10JxrCodecInput input,
        CancellationToken cancellationToken = default);
}

public sealed record Hdr10JxrCodecReadiness(
    bool HasNativeWicJpegXrEncoder,
    bool AcceptsRgba16FloatSource,
    bool WritesHdr10Metadata,
    bool HasWindowsManualViewerValidation,
    IReadOnlyList<string> Blockers)
{
    public static Hdr10JxrCodecReadiness PendingNativeWicImplementation { get; } =
        new(
            HasNativeWicJpegXrEncoder: false,
            AcceptsRgba16FloatSource: true,
            WritesHdr10Metadata: false,
            HasWindowsManualViewerValidation: false,
            Blockers:
            [
                "Native Windows WIC JPEG XR codec integration is not implemented.",
                "HDR10 static metadata write policy is not implemented.",
                "Windows manual viewer validation for the emitted JXR artifact has not passed.",
            ]);

    public bool IsReady =>
        HasNativeWicJpegXrEncoder
        && AcceptsRgba16FloatSource
        && WritesHdr10Metadata
        && HasWindowsManualViewerValidation
        && Blockers.Count == 0;

    public string FormatBlockers() =>
        IsReady
            ? "HDR10 JXR codec readiness passed."
            : string.Join(" ", Blockers);
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
    public Hdr10JxrCodecReadiness Readiness => Hdr10JxrCodecReadiness.PendingNativeWicImplementation;

    public Task<byte[]> EncodeAsync(
        Hdr10JxrCodecInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        throw new OutputArtifactEncodingException(
            $"HDR10 JXR encoding is not implemented yet. {Readiness.FormatBlockers()}");
    }
}
