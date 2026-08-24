using Lumiere.Windows.Graphics.Presentation;

namespace Lumiere.Windows.Graphics.Output;

internal interface IOutputPngEncoder
{
    Task<OutputEncodedArtifact> EncodeArtifactAsync(
        CapturedFrameTexture texture,
        CropPixelRect? cropRegion,
        CancellationToken cancellationToken = default);
}

internal sealed record OutputEncodedArtifact(byte[] Bytes, string FileExtension)
{
    public const string Profile = "srgb-visual-match";

    public string NormalizedFileExtension =>
        string.IsNullOrWhiteSpace(FileExtension)
            ? "bin"
            : FileExtension.Trim().TrimStart('.').ToLowerInvariant();
}
