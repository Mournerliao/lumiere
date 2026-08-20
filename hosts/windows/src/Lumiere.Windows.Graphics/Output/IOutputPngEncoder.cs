using Lumiere.Windows.Graphics.Presentation;

namespace Lumiere.Windows.Graphics.Output;

public interface IOutputPngEncoder
{
    Task<byte[]> EncodePngAsync(
        CapturedFrameTexture texture,
        CropPixelRect? cropRegion,
        CancellationToken cancellationToken = default);

    async Task<OutputEncodedArtifact> EncodeArtifactAsync(
        CapturedFrameTexture texture,
        CropPixelRect? cropRegion,
        CancellationToken cancellationToken = default,
        OutputArtifactCache? artifactCache = null)
    {
        ArgumentNullException.ThrowIfNull(texture);
        var key = OutputArtifactCacheKey.Create(cropRegion, texture.Width, texture.Height);

        async Task<OutputEncodedArtifact> EncodeAsync() =>
            new(await EncodePngAsync(texture, cropRegion, cancellationToken), "png");

        return artifactCache is null
            ? await EncodeAsync()
            : await artifactCache.GetOrCreateAsync(key, EncodeAsync);
    }
}

public sealed record OutputEncodedArtifact(byte[] Bytes, string FileExtension)
{
    public const string Profile = "srgb-visual-match";

    public string NormalizedFileExtension =>
        string.IsNullOrWhiteSpace(FileExtension)
            ? "bin"
            : FileExtension.Trim().TrimStart('.').ToLowerInvariant();
}
