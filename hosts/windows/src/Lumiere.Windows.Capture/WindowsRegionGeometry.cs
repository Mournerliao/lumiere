namespace Lumiere.Windows.Capture;

public sealed record WindowsRegionGeometry
{
    public WindowsRegionGeometry(double x, double y, double width, double height)
    {
        if (!double.IsFinite(x) || x < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(x), x, "Region x must be finite and non-negative.");
        }

        if (!double.IsFinite(y) || y < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(y), y, "Region y must be finite and non-negative.");
        }

        if (!double.IsFinite(width) || width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Region width must be finite and positive.");
        }

        if (!double.IsFinite(height) || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Region height must be finite and positive.");
        }

        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public double X { get; }

    public double Y { get; }

    public double Width { get; }

    public double Height { get; }
}
