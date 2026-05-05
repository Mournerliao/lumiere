using Windows.Foundation;

namespace Lumiere.Overlay.Crop;

public sealed record CropGeometry(Rect Region, bool IsValid)
{
    public static CropGeometry Empty { get; } = new(new Rect(0, 0, 0, 0), false);

    public bool IsEmpty => Region.Width <= 0 || Region.Height <= 0;

    public static CropGeometry FromDrag(
        Point start,
        Point current,
        Rect previewBounds,
        double minimumSize = CropController.DefaultMinimumSize)
    {
        return FromEdges(start.X, start.Y, current.X, current.Y, previewBounds, minimumSize);
    }

    public static CropGeometry FromEdges(
        double left,
        double top,
        double right,
        double bottom,
        Rect previewBounds,
        double minimumSize = CropController.DefaultMinimumSize)
    {
        if (previewBounds.Width <= 0 || previewBounds.Height <= 0)
        {
            return Empty;
        }

        var first = Clamp(new Point(left, top), previewBounds);
        var second = Clamp(new Point(right, bottom), previewBounds);

        var normalizedLeft = Math.Min(first.X, second.X);
        var normalizedTop = Math.Min(first.Y, second.Y);
        var normalizedRight = Math.Max(first.X, second.X);
        var normalizedBottom = Math.Max(first.Y, second.Y);
        var width = normalizedRight - normalizedLeft;
        var height = normalizedBottom - normalizedTop;

        if (width < minimumSize || height < minimumSize)
        {
            return Empty;
        }

        return new CropGeometry(new Rect(normalizedLeft, normalizedTop, width, height), true);
    }

    private static Point Clamp(Point point, Rect bounds)
    {
        var x = Math.Clamp(point.X, bounds.X, bounds.X + bounds.Width);
        var y = Math.Clamp(point.Y, bounds.Y, bounds.Y + bounds.Height);
        return new Point(x, y);
    }
}
