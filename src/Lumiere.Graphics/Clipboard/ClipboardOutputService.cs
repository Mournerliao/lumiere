using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Lumiere.Graphics.Devices;
using Lumiere.Graphics.Hdr;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Half = System.Half;

namespace Lumiere.Graphics.Clipboard;

public sealed class ClipboardOutputService : IDisposable
{
    private readonly GraphicsDeviceResources deviceResources;
    private bool disposed;

    public ClipboardOutputService(GraphicsDeviceResources deviceResources)
    {
        this.deviceResources = deviceResources ?? throw new ArgumentNullException(nameof(deviceResources));
    }

    public async Task<bool> TryCopyToClipboardAsync(
        ID3D11Texture2D sourceTexture,
        int pixelX,
        int pixelY,
        int pixelWidth,
        int pixelHeight,
        int sourceWidth,
        int sourceHeight)
    {
        try
        {
            if (!ValidateRegion(pixelX, pixelY, pixelWidth, pixelHeight, sourceWidth, sourceHeight))
            {
                return false;
            }

            using var croppedTexture = CropTexture(sourceTexture, pixelX, pixelY, pixelWidth, pixelHeight);
            using var bgra8Texture = ConvertToBgra8(croppedTexture, pixelWidth, pixelHeight);
            var pngBytes = await EncodeAsPngAsync(bgra8Texture, pixelWidth, pixelHeight);
            await WriteToClipboardAsync(pngBytes);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Clipboard output failed: {ex.Message}");
            return false;
        }
    }

    private static bool ValidateRegion(
        int x,
        int y,
        int width,
        int height,
        int sourceWidth,
        int sourceHeight)
    {
        return x >= 0
            && y >= 0
            && width > 0
            && height > 0
            && x + width <= sourceWidth
            && y + height <= sourceHeight;
    }

    private ID3D11Texture2D CropTexture(
        ID3D11Texture2D sourceTexture,
        int x,
        int y,
        int width,
        int height)
    {
        var device = deviceResources.Device;
        var context = deviceResources.ImmediateContext;

        var cropDesc = new Texture2DDescription
        {
            Width = (uint)width,
            Height = (uint)height,
            MipLevels = 1,
            ArraySize = 1,
            Format = HdrConstants.DxgiSwapChainFormat,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.RenderTarget,
            CPUAccessFlags = CpuAccessFlags.None,
            MiscFlags = ResourceOptionFlags.None
        };

        var cropTexture = device.CreateTexture2D(cropDesc);

        var sourceBox = new Box(x, y, 0, x + width, y + height, 1);
        context.CopySubresourceRegion(cropTexture, 0, 0, 0, 0, sourceTexture, 0, sourceBox);

        return cropTexture;
    }

    private ID3D11Texture2D ConvertToBgra8(ID3D11Texture2D fp16Texture, int width, int height)
    {
        var device = deviceResources.Device;
        var context = deviceResources.ImmediateContext;

        // Create staging texture to read FP16 data
        var stagingDesc = new Texture2DDescription
        {
            Width = (uint)width,
            Height = (uint)height,
            MipLevels = 1,
            ArraySize = 1,
            Format = HdrConstants.DxgiSwapChainFormat,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Staging,
            BindFlags = BindFlags.None,
            CPUAccessFlags = CpuAccessFlags.Read,
            MiscFlags = ResourceOptionFlags.None
        };

        using var stagingTexture = device.CreateTexture2D(stagingDesc);
        context.CopyResource(stagingTexture, fp16Texture);

        // Read FP16 data and convert to BGRA8
        var map = context.Map(stagingTexture, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);
        try
        {
            var bgra8Data = new byte[width * height * 4];
            var sourcePtr = map.DataPointer;
            var stride = checked((int)map.RowPitch);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var sourceOffset = y * stride + x * 8; // FP16 = 4 channels * 2 bytes
                    var destOffset = (y * width + x) * 4;

                    // Read FP16 values (R16G16B16A16_FLOAT)
                    var r = ReadHalf(sourcePtr, sourceOffset);
                    var g = ReadHalf(sourcePtr, sourceOffset + 2);
                    var b = ReadHalf(sourcePtr, sourceOffset + 4);
                    var a = ReadHalf(sourcePtr, sourceOffset + 6);

                    // Convert scRGB linear to sRGB (simple gamma correction)
                    r = LinearToSrgb(r);
                    g = LinearToSrgb(g);
                    b = LinearToSrgb(b);

                    // Clamp to [0, 1] and convert to 8-bit
                    bgra8Data[destOffset] = ToByte(b); // B
                    bgra8Data[destOffset + 1] = ToByte(g); // G
                    bgra8Data[destOffset + 2] = ToByte(r); // R
                    bgra8Data[destOffset + 3] = ToByte(a); // A
                }
            }

            // Create BGRA8 texture
            var bgra8Desc = new Texture2DDescription
            {
                Width = (uint)width,
                Height = (uint)height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget,
                CPUAccessFlags = CpuAccessFlags.None,
                MiscFlags = ResourceOptionFlags.None
            };

            var bgra8Texture = device.CreateTexture2D(bgra8Desc);

            // Upload converted data
            var uploadDesc = new Texture2DDescription
            {
                Width = (uint)width,
                Height = (uint)height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                CPUAccessFlags = CpuAccessFlags.Write,
                MiscFlags = ResourceOptionFlags.None
            };

            using var uploadTexture = device.CreateTexture2D(uploadDesc);
            var uploadMap = context.Map(uploadTexture, 0, MapMode.Write, Vortice.Direct3D11.MapFlags.None);
            try
            {
                for (int y = 0; y < height; y++)
                {
                    var destination = IntPtr.Add(
                        uploadMap.DataPointer,
                        checked((int)(y * uploadMap.RowPitch)));
                    Marshal.Copy(bgra8Data, y * width * 4, destination, width * 4);
                }
            }
            finally
            {
                context.Unmap(uploadTexture, 0);
            }

            context.CopyResource(bgra8Texture, uploadTexture);
            return bgra8Texture;
        }
        finally
        {
            context.Unmap(stagingTexture, 0);
        }
    }

    private static Half LinearToSrgb(Half linear)
    {
        var f = (float)linear;
        if (f <= 0.0031308f)
            return (Half)(f * 12.92f);
        return (Half)(1.055f * MathF.Pow(f, 1.0f / 2.4f) - 0.055f);
    }

    private static Half ReadHalf(IntPtr source, int offset)
    {
        var bits = unchecked((ushort)Marshal.ReadInt16(source, offset));
        return BitConverter.UInt16BitsToHalf(bits);
    }

    private static byte ToByte(Half value) =>
        (byte)(Math.Clamp((float)value, 0f, 1f) * 255);

    private async Task<byte[]> EncodeAsPngAsync(ID3D11Texture2D bgra8Texture, int width, int height)
    {
        var device = deviceResources.Device;
        var context = deviceResources.ImmediateContext;

        var stagingDesc = new Texture2DDescription
        {
            Width = (uint)width,
            Height = (uint)height,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.B8G8R8A8_UNorm,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Staging,
            BindFlags = BindFlags.None,
            CPUAccessFlags = CpuAccessFlags.Read,
            MiscFlags = ResourceOptionFlags.None
        };

        using var stagingTexture = device.CreateTexture2D(stagingDesc);
        context.CopyResource(stagingTexture, bgra8Texture);

        var map = context.Map(stagingTexture, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);

        try
        {
            var pixelData = new byte[width * height * 4];
            var sourcePtr = map.DataPointer;
            var stride = checked((int)map.RowPitch);

            for (int y = 0; y < height; y++)
            {
                Marshal.Copy(IntPtr.Add(sourcePtr, y * stride), pixelData, y * width * 4, width * 4);
            }

            return await EncodeAsPngAsync(pixelData, width, height);
        }
        finally
        {
            context.Unmap(stagingTexture, 0);
        }
    }

    private static async Task<byte[]> EncodeAsPngAsync(byte[] bgra8Data, int width, int height)
    {
        using var stream = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);

        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            (uint)width,
            (uint)height,
            96.0, // DPI: using standard 96 DPI for clipboard output
            96.0,
            bgra8Data);

        await encoder.FlushAsync();

        stream.Seek(0);
        var bytes = new byte[checked((int)stream.Size)];
        await stream.ReadAsync(bytes.AsBuffer(), (uint)bytes.Length, InputStreamOptions.None);

        return bytes;
    }

    private static async Task WriteToClipboardAsync(byte[] pngBytes)
    {
        var stream = new InMemoryRandomAccessStream();
        try
        {
            await stream.WriteAsync(pngBytes.AsBuffer());
            stream.Seek(0);

            // The clipboard may open the stream after SetContent returns, so the stream
            // must outlive this method once ownership is handed to the data package.
            var reference = RandomAccessStreamReference.CreateFromStream(stream);
            var dataPackage = new DataPackage();
            dataPackage.SetBitmap(reference);

            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
    }
}
