using Lumiere.Windows.Graphics.Presentation;

namespace Lumiere.Windows.Graphics.Output;

internal sealed record RegionPreviewArtifact(
    byte[] Bytes,
    int Width,
    int Height,
    long RenderMilliseconds = 0,
    long EncodeMilliseconds = 0);

internal interface IRegionPreviewEncoder
{
    Task<RegionPreviewArtifact> EncodePreviewAsync(
        CapturedFrameTexture texture,
        int outputWidth,
        int outputHeight,
        SrgbVisualMatchConversionContext context,
        CancellationToken cancellationToken = default);
}

internal sealed class SrgbRegionPreviewEncoder : IRegionPreviewEncoder
{
    private readonly ICapturedFrameTextureReadback readback;

    public SrgbRegionPreviewEncoder(ICapturedFrameTextureReadback readback)
    {
        this.readback = readback ?? throw new ArgumentNullException(nameof(readback));
    }

    public async Task<RegionPreviewArtifact> EncodePreviewAsync(
        CapturedFrameTexture texture,
        int outputWidth,
        int outputHeight,
        SrgbVisualMatchConversionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(texture);
        cancellationToken.ThrowIfCancellationRequested();
        var renderStartedAt = System.Diagnostics.Stopwatch.GetTimestamp();
        var source = readback.ReadRgba16Float(texture, cropRegion: null);
        var image = SrgbVisualMatchPixelConverter.ConvertRgba16FloatToBgra8(
            source,
            outputWidth,
            outputHeight,
            context);
        var renderMilliseconds = (long)Math.Round(
            System.Diagnostics.Stopwatch.GetElapsedTime(renderStartedAt).TotalMilliseconds);
        cancellationToken.ThrowIfCancellationRequested();
        var encodeStartedAt = System.Diagnostics.Stopwatch.GetTimestamp();
        var bytes = await SrgbVisualMatchPngEncoder.EncodeAsPngAsync(image);
        var encodeMilliseconds = (long)Math.Round(
            System.Diagnostics.Stopwatch.GetElapsedTime(encodeStartedAt).TotalMilliseconds);
        return new RegionPreviewArtifact(
            bytes,
            image.Width,
            image.Height,
            renderMilliseconds,
            encodeMilliseconds);
    }
}
