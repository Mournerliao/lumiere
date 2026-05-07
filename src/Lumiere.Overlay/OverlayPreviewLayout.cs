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

    public static OverlayPreviewLayout FitFrameToSurface(
        double frameWidth,
        double frameHeight,
        double availableWidth,
        double availableHeight)
    {
        var surfaceWidth = Math.Max(1, availableWidth);
        var surfaceHeight = Math.Max(1, availableHeight);
        if (frameWidth <= 0 || frameHeight <= 0)
        {
            return FillSurface(surfaceWidth, surfaceHeight);
        }

        var scale = Math.Min(surfaceWidth / frameWidth, surfaceHeight / frameHeight);
        var width = Math.Max(1, frameWidth * scale);
        var height = Math.Max(1, frameHeight * scale);
        return new OverlayPreviewLayout(new Rect(
            (surfaceWidth - width) / 2,
            (surfaceHeight - height) / 2,
            width,
            height));
    }
}
