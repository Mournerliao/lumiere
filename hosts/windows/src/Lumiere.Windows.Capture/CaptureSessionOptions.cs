using Lumiere.Windows.Graphics.Hdr;
using Windows.Graphics;
using Windows.Graphics.DirectX;

namespace Lumiere.Windows.Capture;

internal sealed class CaptureSessionOptions
{
    public const int DefaultBufferCount = 2;

    public CaptureSessionOptions(
        int width,
        int height,
        int bufferCount = DefaultBufferCount)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width),
                width,
                "A capture frame pool requires a positive pixel width.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(height),
                height,
                "A capture frame pool requires a positive pixel height.");
        }

        if (bufferCount is < 2 or > 16)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bufferCount),
                bufferCount,
                "A capture frame pool buffer count must be between 2 and 16.");
        }

        BufferSize = new SizeInt32
        {
            Width = width,
            Height = height,
        };
        BufferCount = bufferCount;
    }

    public SizeInt32 BufferSize { get; }

    public int BufferCount { get; }

    public DirectXPixelFormat PixelFormat => HdrConstants.WgcFramePoolPixelFormat;
}
