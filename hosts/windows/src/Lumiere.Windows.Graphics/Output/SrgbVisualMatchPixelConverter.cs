using Half = System.Half;

namespace Lumiere.Windows.Graphics.Output;

internal static class SrgbVisualMatchPixelConverter
{
    public static SrgbVisualMatchImage ConvertRgba16FloatToBgra8(CapturedFrameReadback readback)
        => ConvertRgba16FloatToBgra8(readback, SrgbVisualMatchConversionContext.ForSdrDisplay());

    public static SrgbVisualMatchImage ConvertRgba16FloatToBgra8(
        CapturedFrameReadback readback,
        SrgbVisualMatchConversionContext context)
        => ConvertRgba16FloatToBgra8(readback, readback.Width, readback.Height, context);

    public static SrgbVisualMatchImage ConvertRgba16FloatToBgra8(
        CapturedFrameReadback readback,
        int outputWidth,
        int outputHeight,
        SrgbVisualMatchConversionContext context)
    {
        ArgumentNullException.ThrowIfNull(readback);
        if (outputWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(outputWidth));
        }

        if (outputHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(outputHeight));
        }

        var bgra8Data = new byte[checked(outputWidth * outputHeight * SrgbVisualMatchImage.BytesPerPixel)];

        for (int outputY = 0; outputY < outputHeight; outputY++)
        {
            var sourceY = Math.Clamp(
                ((outputY + 0.5f) * readback.Height / outputHeight) - 0.5f,
                0f,
                readback.Height - 1f);
            var y0 = (int)MathF.Floor(sourceY);
            var y1 = Math.Min(y0 + 1, readback.Height - 1);
            var fy = sourceY - y0;
            for (int outputX = 0; outputX < outputWidth; outputX++)
            {
                var sourceX = Math.Clamp(
                    ((outputX + 0.5f) * readback.Width / outputWidth) - 0.5f,
                    0f,
                    readback.Width - 1f);
                var x0 = (int)MathF.Floor(sourceX);
                var x1 = Math.Min(x0 + 1, readback.Width - 1);
                var fx = sourceX - x0;
                var destOffset = ((outputY * outputWidth) + outputX) * SrgbVisualMatchImage.BytesPerPixel;

                var r = ConvertChannel(SampleLinear(readback, x0, y0, x1, y1, fx, fy, 0), context);
                var g = ConvertChannel(SampleLinear(readback, x0, y0, x1, y1, fx, fy, 2), context);
                var b = ConvertChannel(SampleLinear(readback, x0, y0, x1, y1, fx, fy, 4), context);
                var a = SampleLinear(readback, x0, y0, x1, y1, fx, fy, 6);

                bgra8Data[destOffset] = ToByte(b);
                bgra8Data[destOffset + 1] = ToByte(g);
                bgra8Data[destOffset + 2] = ToByte(r);
                bgra8Data[destOffset + 3] = ToByte(a);
            }
        }

        return new SrgbVisualMatchImage(outputWidth, outputHeight, bgra8Data);
    }

    private static Half ConvertChannel(float capturedLinear, SrgbVisualMatchConversionContext context) =>
        LinearToSrgb(ToneMapForVisualMatch((Half)(capturedLinear * context.InputLinearScale)));

    private static float SampleLinear(
        CapturedFrameReadback readback,
        int x0,
        int y0,
        int x1,
        int y1,
        float fx,
        float fy,
        int channelOffset)
    {
        var top = Lerp(
            ReadChannel(readback, x0, y0, channelOffset),
            ReadChannel(readback, x1, y0, channelOffset),
            fx);
        var bottom = Lerp(
            ReadChannel(readback, x0, y1, channelOffset),
            ReadChannel(readback, x1, y1, channelOffset),
            fx);
        return Lerp(top, bottom, fy);
    }

    private static float ReadChannel(CapturedFrameReadback readback, int x, int y, int channelOffset)
    {
        var offset = ((y * readback.Width) + x) * CapturedFrameReadback.BytesPerPixel;
        return (float)ReadHalf(readback.PixelData, offset + channelOffset);
    }

    private static Half ToneMapForVisualMatch(Half linear)
    {
        var f = (float)linear;
        if (f <= 0f)
        {
            return (Half)0f;
        }

        const float shoulderHeadroom = 0.08f;
        const float shoulderStart = 0.75f;

        if (f <= 1f)
        {
            var transition = SmoothStep(Math.Clamp((f - shoulderStart) / (1f - shoulderStart), 0f, 1f));
            return (Half)(f * Lerp(1f, 1f - shoulderHeadroom, transition));
        }

        return (Half)(1f - (shoulderHeadroom / f));
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

    private static byte ToByte(Half value) => ToByte((float)value);

    private static byte ToByte(float value) =>
        (byte)(Math.Clamp(value, 0f, 1f) * 255);
}
