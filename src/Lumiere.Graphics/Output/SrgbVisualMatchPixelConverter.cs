using Half = System.Half;

namespace Lumiere.Graphics.Output;

public static class SrgbVisualMatchPixelConverter
{
    public static SrgbVisualMatchImage ConvertRgba16FloatToBgra8(CapturedFrameReadback readback)
    {
        ArgumentNullException.ThrowIfNull(readback);

        var bgra8Data = new byte[checked(readback.Width * readback.Height * SrgbVisualMatchImage.BytesPerPixel)];
        var pixelCount = checked(readback.Width * readback.Height);

        for (int pixelIndex = 0; pixelIndex < pixelCount; pixelIndex++)
        {
            var sourceOffset = pixelIndex * CapturedFrameReadback.BytesPerPixel;
            var destOffset = pixelIndex * SrgbVisualMatchImage.BytesPerPixel;

            var r = LinearToSrgb(ToneMapForVisualMatch(ReadHalf(readback.PixelData, sourceOffset)));
            var g = LinearToSrgb(ToneMapForVisualMatch(ReadHalf(readback.PixelData, sourceOffset + 2)));
            var b = LinearToSrgb(ToneMapForVisualMatch(ReadHalf(readback.PixelData, sourceOffset + 4)));
            var a = ReadHalf(readback.PixelData, sourceOffset + 6);

            bgra8Data[destOffset] = ToByte(b);
            bgra8Data[destOffset + 1] = ToByte(g);
            bgra8Data[destOffset + 2] = ToByte(r);
            bgra8Data[destOffset + 3] = ToByte(a);
        }

        return new SrgbVisualMatchImage(readback.Width, readback.Height, bgra8Data);
    }

    private static Half ToneMapForVisualMatch(Half linear)
    {
        var f = (float)linear;
        if (f <= 1f)
        {
            return linear;
        }

        const float shoulderHeadroom = 0.08f;
        const float shoulderTransitionWidth = 0.25f;

        var shoulder = 1f - shoulderHeadroom / (f + shoulderHeadroom);
        var transition = SmoothStep(Math.Clamp((f - 1f) / shoulderTransitionWidth, 0f, 1f));
        return (Half)Lerp(1f, shoulder, transition);
    }

    private static Half LinearToSrgb(Half linear)
    {
        var f = (float)linear;
        if (f <= 0.0031308f)
        {
            return (Half)(f * 12.92f);
        }

        return (Half)(1.055f * MathF.Pow(f, 1.0f / 2.4f) - 0.055f);
    }

    private static Half ReadHalf(byte[] source, int offset)
    {
        var bits = BitConverter.ToUInt16(source, offset);
        return BitConverter.UInt16BitsToHalf(bits);
    }

    private static float Lerp(float from, float to, float amount) =>
        from + ((to - from) * amount);

    private static float SmoothStep(float amount) =>
        amount * amount * (3f - (2f * amount));

    private static byte ToByte(Half value) =>
        (byte)(Math.Clamp((float)value, 0f, 1f) * 255);
}
