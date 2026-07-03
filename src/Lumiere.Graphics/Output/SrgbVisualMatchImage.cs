namespace Lumiere.Graphics.Output;

public sealed record SrgbVisualMatchImage
{
    public SrgbVisualMatchImage(int width, int height, byte[] bgra8PixelData)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Image width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Image height must be positive.");
        }

        ArgumentNullException.ThrowIfNull(bgra8PixelData);
        var expectedLength = checked(width * height * BytesPerPixel);
        if (bgra8PixelData.Length != expectedLength)
        {
            throw new ArgumentException(
                $"BGRA8 pixel data length must be {expectedLength} bytes for {width}x{height}.",
                nameof(bgra8PixelData));
        }

        Width = width;
        Height = height;
        Bgra8PixelData = bgra8PixelData;
    }

    public const int BytesPerPixel = 4;

    public int Width { get; }

    public int Height { get; }

    public byte[] Bgra8PixelData { get; }
}
