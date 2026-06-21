using Lumiere.Graphics.Presentation;

namespace Lumiere.Graphics.Output;

public interface IOutputPngEncoder
{
    Task<byte[]> EncodePngAsync(
        CapturedFrameTexture texture,
        CropPixelRect? cropRegion,
        CancellationToken cancellationToken = default);

    async Task<OutputEncodedArtifact> EncodeArtifactAsync(
        CapturedFrameTexture texture,
        CropPixelRect? cropRegion,
        OutputProfileContract outputProfile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outputProfile);
        var pngBytes = await EncodePngAsync(texture, cropRegion, cancellationToken);
        return new OutputEncodedArtifact(
            pngBytes,
            "png",
            outputProfile);
    }
}

public sealed record OutputEncodedArtifact(
    byte[] Bytes,
    string FileExtension,
    OutputProfileContract Profile)
{
    public string NormalizedFileExtension =>
        string.IsNullOrWhiteSpace(FileExtension)
            ? "bin"
            : FileExtension.Trim().TrimStart('.').ToLowerInvariant();
}
