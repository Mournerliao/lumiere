using Lumiere.Infrastructure.Interop;

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
    Hdr10StaticMetadataPolicy Hdr10StaticMetadataPolicy,
    bool HasWindowsManualViewerValidation,
    IReadOnlyList<string> Blockers)
{
    public static Hdr10JxrCodecReadiness PendingNativeWicImplementation { get; } =
        new(
            HasNativeWicJpegXrEncoder: false,
            AcceptsRgba16FloatSource: true,
            WritesHdr10Metadata: false,
            Hdr10StaticMetadataPolicy: Hdr10StaticMetadataPolicy.Undefined,
            HasWindowsManualViewerValidation: false,
            Blockers:
            [
                "Native Windows WIC JPEG XR codec integration is not implemented.",
                "HDR10 static metadata write policy is not implemented or auditable.",
                "Windows manual viewer validation for the emitted JXR artifact has not passed.",
            ]);

    public bool IsReady =>
        HasNativeWicJpegXrEncoder
        && AcceptsRgba16FloatSource
        && WritesHdr10Metadata
        && Hdr10StaticMetadataPolicy.IsComplete
        && HasWindowsManualViewerValidation
        && Blockers.Count == 0;

    public string FormatBlockers() =>
        IsReady
            ? "HDR10 JXR codec readiness passed."
            : string.Join(" ", Blockers);

    public Hdr10JxrCodecReadiness WithNativeWicReadiness(WicJpegXrEncoderReadiness wicReadiness)
    {
        ArgumentNullException.ThrowIfNull(wicReadiness);

        var blockers = new List<string>();
        if (!wicReadiness.IsReady)
        {
            blockers.AddRange(wicReadiness.Blockers);
        }

        if (!WritesHdr10Metadata)
        {
            blockers.Add("HDR10 static metadata writer is not implemented for the JPEG XR container.");
        }

        if (!Hdr10StaticMetadataPolicy.IsComplete)
        {
            blockers.Add("HDR10 static metadata policy is not complete and auditable.");
        }

        if (!HasWindowsManualViewerValidation)
        {
            blockers.Add("Windows manual viewer validation for the emitted JXR artifact has not passed.");
        }

        return this with
        {
            HasNativeWicJpegXrEncoder = wicReadiness.HasJpegXrContainerEncoder,
            AcceptsRgba16FloatSource = wicReadiness.AcceptsRgbaHalfPixelFormat,
            Blockers = blockers,
        };
    }
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

    public Hdr10StaticMetadataPolicy StaticMetadataPolicy =>
        OutputProfile.FormatContract.Hdr10StaticMetadataPolicy
        ?? throw new InvalidOperationException(
            "HDR10 JXR codec input requires an auditable HDR10 static metadata policy.");
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

public sealed class WicHdr10JxrCodec : IHdr10JxrCodec
{
    private readonly IWicJpegXrEncoder encoder;

    public WicHdr10JxrCodec(IWicJpegXrEncoder encoder)
    {
        this.encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
    }

    public Hdr10JxrCodecReadiness Readiness =>
        Hdr10JxrCodecReadiness.PendingNativeWicImplementation
            .WithNativeWicReadiness(encoder.Readiness);

    public Task<byte[]> EncodeAsync(
        Hdr10JxrCodecInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var source = input.Source;
            var encoded = encoder.EncodeRgbaHalf(
                new WicJpegXrEncodeRequest(
                    source.Width,
                    source.Height,
                    checked(source.Width * WicJpegXrEncodeRequest.RgbaHalfBytesPerPixel),
                    source.PixelData));
            return Task.FromResult(encoded);
        }
        catch (NativeInteropException exception)
        {
            throw new OutputArtifactEncodingException(
                $"HDR10 JXR WIC encoding failed. {exception.Message}");
        }
    }
}
