using Lumiere.Graphics.Hdr;
using Vortice.DXGI;

namespace Lumiere.Graphics.Presentation;

public sealed class SwapChainCreationOptions
{
    public const int DefaultBufferCount = 2;

    public SwapChainCreationOptions(
        int width,
        int height,
        int bufferCount = DefaultBufferCount)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width),
                width,
                "A composition swap chain requires a positive preview pixel width.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(height),
                height,
                "A composition swap chain requires a positive preview pixel height.");
        }

        if (bufferCount is < 2 or > 16)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bufferCount),
                bufferCount,
                "A composition swap chain buffer count must be between 2 and 16.");
        }

        Width = width;
        Height = height;
        BufferCount = bufferCount;
    }

    public int Width { get; }

    public int Height { get; }

    public int BufferCount { get; }

    public Format Format => HdrConstants.DxgiSwapChainFormat;

    public ColorSpaceType ColorSpace => HdrConstants.DxgiColorSpace;

    public SwapChainDescription1 CreateDescription() =>
        new()
        {
            Width = (uint)Width,
            Height = (uint)Height,
            Format = Format,
            Stereo = false,
            SampleDescription = new SampleDescription(1, 0),
            BufferUsage = Usage.RenderTargetOutput,
            BufferCount = (uint)BufferCount,
            Scaling = Scaling.Stretch,
            SwapEffect = SwapEffect.FlipSequential,
            AlphaMode = AlphaMode.Ignore,
            Flags = SwapChainFlags.None,
        };
}
