using Lumiere.Graphics.Output;
using Xunit;

namespace Lumiere.Graphics.Tests.Output;

public sealed class SrgbVisualMatchPixelConverterTests
{
    [Fact]
    public void ConvertRgba16FloatToBgra8_PreservesSdrWhiteAndAlpha()
    {
        var readback = CreateReadback(1, 1, [(1f, 1f, 1f, 1f)]);

        var image = SrgbVisualMatchPixelConverter.ConvertRgba16FloatToBgra8(readback);

        Assert.Equal(1, image.Width);
        Assert.Equal(1, image.Height);
        Assert.Equal([255, 255, 255, 255], image.Bgra8PixelData);
    }

    [Fact]
    public void ConvertRgba16FloatToBgra8_WritesBgraChannelOrder()
    {
        var readback = CreateReadback(1, 1, [(1f, 0f, 0f, 0.5f)]);

        var image = SrgbVisualMatchPixelConverter.ConvertRgba16FloatToBgra8(readback);

        Assert.Equal([0, 0, 255, 127], image.Bgra8PixelData);
    }

    [Fact]
    public void ConvertRgba16FloatToBgra8_KeepsLegacyHardClampForHdrRangeUntilToneMapperLands()
    {
        var readback = CreateReadback(1, 1, [(4f, 2f, 1.5f, 1f)]);

        var image = SrgbVisualMatchPixelConverter.ConvertRgba16FloatToBgra8(readback);

        Assert.Equal([255, 255, 255, 255], image.Bgra8PixelData);
    }

    private static CapturedFrameReadback CreateReadback(
        int width,
        int height,
        IReadOnlyList<(float R, float G, float B, float A)> pixels)
    {
        var data = new byte[checked(width * height * CapturedFrameReadback.BytesPerPixel)];
        for (int index = 0; index < pixels.Count; index++)
        {
            WriteHalf(data, index * CapturedFrameReadback.BytesPerPixel, pixels[index].R);
            WriteHalf(data, index * CapturedFrameReadback.BytesPerPixel + 2, pixels[index].G);
            WriteHalf(data, index * CapturedFrameReadback.BytesPerPixel + 4, pixels[index].B);
            WriteHalf(data, index * CapturedFrameReadback.BytesPerPixel + 6, pixels[index].A);
        }

        return new CapturedFrameReadback(
            width,
            height,
            OutputPixelFormat.R16G16B16A16Float,
            data);
    }

    private static void WriteHalf(byte[] data, int offset, float value)
    {
        var bytes = BitConverter.GetBytes(BitConverter.HalfToUInt16Bits((Half)value));
        data[offset] = bytes[0];
        data[offset + 1] = bytes[1];
    }
}
