using Lumiere.Graphics.Presentation;

namespace Lumiere.Graphics.Output;

public sealed class CompositeOutputArtifactEncoder : IOutputPngEncoder
{
    private readonly IOutputPngEncoder compatibilityEncoder;
    private readonly IOutputPngEncoder hdr10Encoder;

    public CompositeOutputArtifactEncoder(
        IOutputPngEncoder compatibilityEncoder,
        IOutputPngEncoder hdr10Encoder)
    {
        this.compatibilityEncoder = compatibilityEncoder ?? throw new ArgumentNullException(nameof(compatibilityEncoder));
        this.hdr10Encoder = hdr10Encoder ?? throw new ArgumentNullException(nameof(hdr10Encoder));
    }

    public Task<byte[]> EncodePngAsync(
        CapturedFrameTexture texture,
        CropPixelRect? cropRegion,
        CancellationToken cancellationToken = default) =>
        compatibilityEncoder.EncodePngAsync(texture, cropRegion, cancellationToken);

    public Task<OutputEncodedArtifact> EncodeArtifactAsync(
        CapturedFrameTexture texture,
        CropPixelRect? cropRegion,
        OutputProfileContract outputProfile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outputProfile);
        return outputProfile.Kind switch
        {
            OutputProfileKind.Hdr10Pq => hdr10Encoder.EncodeArtifactAsync(
                texture,
                cropRegion,
                outputProfile,
                cancellationToken),
            _ => compatibilityEncoder.EncodeArtifactAsync(
                texture,
                cropRegion,
                outputProfile,
                cancellationToken),
        };
    }
}
