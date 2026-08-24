using System.Runtime.InteropServices.WindowsRuntime;
using Lumiere.Windows.Graphics.Presentation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Lumiere.Windows.Graphics.Output;

internal sealed class SrgbVisualMatchPngEncoder : IOutputPngEncoder
{
    private readonly ISrgbVisualMatchConverter converter;

    public SrgbVisualMatchPngEncoder(ISrgbVisualMatchConverter converter)
    {
        this.converter = converter ?? throw new ArgumentNullException(nameof(converter));
    }

    public async Task<OutputEncodedArtifact> EncodeArtifactAsync(
        CapturedFrameTexture texture,
        CropPixelRect? cropRegion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(texture);
        cancellationToken.ThrowIfCancellationRequested();
        var image = converter.Convert(texture, cropRegion);
        cancellationToken.ThrowIfCancellationRequested();
        return new OutputEncodedArtifact(await EncodeAsPngAsync(image), "png");
    }

    private static async Task<byte[]> EncodeAsPngAsync(SrgbVisualMatchImage image)
    {
        using var stream = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Straight,
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
