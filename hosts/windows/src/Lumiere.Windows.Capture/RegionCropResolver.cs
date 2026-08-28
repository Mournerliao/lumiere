using Lumiere.Windows.Graphics.Output;

namespace Lumiere.Windows.Capture;

internal static class RegionCropResolver
{
    public static CropPixelRect Resolve(
        WindowsRegionGeometry geometry,
        WindowsTargetCapability target,
        CaptureTarget captureTarget)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(captureTarget);

        if (!target.SupportsRegionCapture
            || target.LogicalSize is not { } logicalSize
            || target.PixelWidth is not { } pixelWidth
            || target.PixelHeight is not { } pixelHeight
            || captureTarget.Size.Width != pixelWidth
            || captureTarget.Size.Height != pixelHeight)
        {
            throw new InvalidOperationException("The region target changed after it was issued.");
        }

        const double tolerance = 0.000001;
        if (geometry.X + geometry.Width > logicalSize.Width + tolerance
            || geometry.Y + geometry.Height > logicalSize.Height + tolerance)
        {
            throw new ArgumentOutOfRangeException(nameof(geometry), "Region geometry exceeds the issued target.");
        }

        var scaleX = pixelWidth / logicalSize.Width;
        var scaleY = pixelHeight / logicalSize.Height;
        var left = Math.Clamp((int)Math.Floor(geometry.X * scaleX), 0, pixelWidth);
        var top = Math.Clamp((int)Math.Floor(geometry.Y * scaleY), 0, pixelHeight);
        var right = Math.Clamp(
            (int)Math.Ceiling((geometry.X + geometry.Width) * scaleX),
            left + 1,
            pixelWidth);
        var bottom = Math.Clamp(
            (int)Math.Ceiling((geometry.Y + geometry.Height) * scaleY),
            top + 1,
            pixelHeight);
        return new CropPixelRect(left, top, right - left, bottom - top);
    }
}
