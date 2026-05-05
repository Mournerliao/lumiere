using Windows.Foundation;

namespace Lumiere.Overlay.Crop;

public static class CropCoordinateMapper
{
    public static CropPixelRect MapToCapturePixels(
        Rect crop,
        Rect previewBounds,
        CaptureFrameSize frameSize)
    {
        if (previewBounds.Width <= 0 || previewBounds.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(previewBounds));
        }

        if (frameSize.Width <= 0 || frameSize.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frameSize));
        }

        var scaleX = frameSize.Width / previewBounds.Width;
        var scaleY = frameSize.Height / previewBounds.Height;

        var left = (crop.X - previewBounds.X) * scaleX;
        var top = (crop.Y - previewBounds.Y) * scaleY;
        var right = (crop.X + crop.Width - previewBounds.X) * scaleX;
        var bottom = (crop.Y + crop.Height - previewBounds.Y) * scaleY;

        var pixelLeft = Clamp((int)Math.Floor(left), 0, frameSize.Width);
        var pixelTop = Clamp((int)Math.Floor(top), 0, frameSize.Height);
        var pixelRight = Clamp((int)Math.Ceiling(right), 0, frameSize.Width);
        var pixelBottom = Clamp((int)Math.Ceiling(bottom), 0, frameSize.Height);

        return new CropPixelRect(
            pixelLeft,
            pixelTop,
            Math.Max(0, pixelRight - pixelLeft),
            Math.Max(0, pixelBottom - pixelTop));
    }

    private static int Clamp(int value, int minimum, int maximum) =>
        Math.Min(Math.Max(value, minimum), maximum);
}
