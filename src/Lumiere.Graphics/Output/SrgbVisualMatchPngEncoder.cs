using System.Runtime.InteropServices.WindowsRuntime;
using Lumiere.Graphics.Presentation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Lumiere.Graphics.Output;

public sealed class SrgbVisualMatchPngEncoder : IOutputPngEncoder
{
    private readonly ISrgbVisualMatchConverter converter;

    public SrgbVisualMatchPngEncoder(ISrgbVisualMatchConverter converter)
    {
        this.converter = converter ?? throw new ArgumentNullException(nameof(converter));
    }

    public async Task<byte[]> EncodePngAsync(
        CapturedFrameTexture texture,
        CropPixelRect? cropRegion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(texture);
        cancellationToken.ThrowIfCancellationRequested();

        var image = converter.Convert(texture, cropRegion);

        cancellationToken.ThrowIfCancellationRequested();

        return await EncodeAsPngAsync(image);
    }

    public async Task<OutputEncodedArtifact> EncodeArtifactAsync(
        CapturedFrameTexture texture,
        CropPixelRect? cropRegion,
        OutputProfileContract outputProfile,
        CancellationToken cancellationToken = default,
        OutputArtifactCache? artifactCache = null)
    {
        ArgumentNullException.ThrowIfNull(outputProfile);
        if (outputProfile.Kind is not OutputProfileKind.SrgbCompatibilityPng)
        {
            throw new InvalidOperationException(
                $"The sRGB Visual Match PNG encoder cannot create {outputProfile.Label} artifacts.");
        }

        ArgumentNullException.ThrowIfNull(texture);

        var cacheKey = OutputArtifactCacheKey.Create(
            outputProfile.Kind,
            cropRegion,
            texture.Width,
            texture.Height);
        return artifactCache is null
            ? await CreateArtifactAsync(texture, cropRegion, outputProfile, cancellationToken)
            : await artifactCache.GetOrCreateAsync(
                cacheKey,
                () => CreateArtifactAsync(texture, cropRegion, outputProfile, cancellationToken));
    }

    private async Task<OutputEncodedArtifact> CreateArtifactAsync(
        CapturedFrameTexture texture,
        CropPixelRect? cropRegion,
        OutputProfileContract outputProfile,
        CancellationToken cancellationToken)
    {
        var pngBytes = await EncodePngAsync(texture, cropRegion, cancellationToken);
        return new OutputEncodedArtifact(
            pngBytes,
            "png",
            outputProfile);
    }

    private static async Task<byte[]> EncodeAsPngAsync(SrgbVisualMatchImage image)
    {
        using var stream = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);

        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            (uint)image.Width,
            (uint)image.Height,
            96.0,
            96.0,
            image.Bgra8PixelData);

        await encoder.FlushAsync();

        stream.Seek(0);
        var bytes = new byte[checked((int)stream.Size)];
        await stream.ReadAsync(bytes.AsBuffer(), (uint)bytes.Length, InputStreamOptions.None);

        return bytes;
    }
}
