using Lumiere.Windows.Graphics.Output;
using Lumiere.Windows.Graphics.Presentation;
using Xunit;

namespace Lumiere.Windows.Graphics.Tests.Output;

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
    public void ConvertRgba16FloatToBgra8_KeepsSdrRangeStable()
    {
        var readback = CreateReadback(
            3,
            1,
            [
                (0f, 0f, 0f, 1f),
                (0.18f, 0.18f, 0.18f, 1f),
                (0.5f, 0.5f, 0.5f, 1f),
            ]);

        var image = SrgbVisualMatchPixelConverter.ConvertRgba16FloatToBgra8(readback);

        Assert.Equal(
            [
                0, 0, 0, 255,
                117, 117, 117, 255,
                187, 187, 187, 255,
            ],
            image.Bgra8PixelData);
    }

    [Fact]
    public void ConvertRgba16FloatToBgra8_CompressesHdrHighlightsWithoutHardClamp()
    {
        var readback = CreateReadback(1, 1, [(4f, 2f, 1.5f, 1f)]);

        var image = SrgbVisualMatchPixelConverter.ConvertRgba16FloatToBgra8(readback);

        Assert.InRange((int)image.Bgra8PixelData[0], 245, 254);
        Assert.InRange((int)image.Bgra8PixelData[1], 245, 254);
        Assert.InRange((int)image.Bgra8PixelData[2], 245, 254);
        Assert.True(image.Bgra8PixelData[2] > image.Bgra8PixelData[1]);
        Assert.True(image.Bgra8PixelData[1] > image.Bgra8PixelData[0]);
        Assert.Equal(255, image.Bgra8PixelData[3]);
    }

    [Fact]
    public void ConvertRgba16FloatToBgra8_UsesSmoothCompressionAboveSdrRange()
    {
        var readback = CreateReadback(
            4,
            1,
            [
                (1.25f, 1.25f, 1.25f, 1f),
                (2f, 2f, 2f, 1f),
                (4f, 4f, 4f, 1f),
                (8f, 8f, 8f, 1f),
            ]);

        var image = SrgbVisualMatchPixelConverter.ConvertRgba16FloatToBgra8(readback);

        var first = (int)image.Bgra8PixelData[0];
        var second = (int)image.Bgra8PixelData[4];
        var third = (int)image.Bgra8PixelData[8];
        var fourth = (int)image.Bgra8PixelData[12];

        Assert.InRange(first, 245, 254);
        Assert.InRange(second, first, 254);
        Assert.InRange(third, second, 254);
        Assert.InRange(fourth, third, 254);
        Assert.True(fourth - first < 10);
    }

    [Fact]
    public void Convert_UsesReadbackCropAndPreservesConvertedDimensions()
    {
        var crop = new CropPixelRect(2, 3, 4, 5);
        var readback = new TestReadback(CreateReadback(4, 5, [(1f, 0f, 0f, 0.25f)]));
        var converter = new SrgbVisualMatchConverter(readback);
        using var texture = new CapturedFrameTexture(null, 16, 12, "Test frame");

        var image = converter.Convert(texture, crop);

        Assert.Same(texture, readback.Texture);
        Assert.Equal(crop, readback.CropRegion);
        Assert.Equal(4, image.Width);
        Assert.Equal(5, image.Height);
        Assert.Equal([0, 0, 255, 63], image.Bgra8PixelData[..4]);
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

    private sealed class TestReadback : ICapturedFrameTextureReadback
    {
        private readonly CapturedFrameReadback readback;

        public TestReadback(CapturedFrameReadback readback)
        {
            this.readback = readback;
        }

        public CapturedFrameTexture? Texture { get; private set; }

        public CropPixelRect? CropRegion { get; private set; }

        public CapturedFrameReadback ReadRgba16Float(
            CapturedFrameTexture texture,
            CropPixelRect? cropRegion)
        {
            Texture = texture;
            CropRegion = cropRegion;
            return readback;
        }
    }
}
