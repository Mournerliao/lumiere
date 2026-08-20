using Lumiere.Windows.Graphics.Presentation;

namespace Lumiere.Windows.Graphics.Output;

public interface ISrgbVisualMatchConverter
{
    SrgbVisualMatchImage Convert(
        CapturedFrameTexture texture,
        CropPixelRect? cropRegion);
}

public sealed class SrgbVisualMatchConverter : ISrgbVisualMatchConverter
{
    private readonly ICapturedFrameTextureReadback readback;

    public SrgbVisualMatchConverter(ICapturedFrameTextureReadback readback)
    {
        this.readback = readback ?? throw new ArgumentNullException(nameof(readback));
    }

    public SrgbVisualMatchImage Convert(
        CapturedFrameTexture texture,
        CropPixelRect? cropRegion)
    {
        var frameReadback = readback.ReadRgba16Float(texture, cropRegion);
        return SrgbVisualMatchPixelConverter.ConvertRgba16FloatToBgra8(frameReadback);
    }
}
