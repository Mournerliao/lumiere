using Windows.Foundation;

namespace Lumiere.Overlay;

public sealed record OverlayPreviewLayout(Rect PreviewBounds)
{
    public static OverlayPreviewLayout FillSurface(double availableWidth, double availableHeight)
    {
        var width = Math.Max(1, availableWidth);
        var height = Math.Max(1, availableHeight);
        return new OverlayPreviewLayout(new Rect(0, 0, width, height));
    }
}
