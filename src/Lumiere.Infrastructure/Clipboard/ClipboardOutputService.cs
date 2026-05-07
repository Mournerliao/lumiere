using System.Runtime.InteropServices;
using Lumiere.Graphics.Devices;
using Lumiere.Graphics.Hdr;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Half = System.Half;

namespace Lumiere.Infrastructure.Clipboard;

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
        CropPixelRect pixelRegion,
        int sourceWidth,
        int sourceHeight)
    {
        try
        {
            if (!ValidateRegion(pixelRegion, sourceWidth, sourceHeight))
            {
                return false;
            }

            using var croppedTexture = CropTexture(sourceTexture, pixelRegion);
            using var bgra8Texture = ConvertToBgra8(croppedTexture, pixelRegion.Width, pixelRegion.Height);
            var pngBytes = await EncodeAsPngAsync(bgra8Texture, pixelRegion.Width, pixelRegion.Height);
            await WriteToClipboardAsync(pngBytes, pixelRegion.Width, pixelRegion.Height);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Clipboard output failed: {ex.Message}");
            return false;
        }
    }

    private bool ValidateRegion(CropPixelRect region, int sourceWidth, int sourceHeight)
    {
        return region.X >= 0
            && region.Y >= 0
            && region.Width > 0
            && region.Height > 0
            && region.X + region.Width <= sourceWidth
            && region.Y + region.Height <= sourceHeight;
    }

    private ID3D11Texture2D CropTexture(ID3D11Texture2D sourceTexture, CropPixelRect region)
    {
        var device = deviceResources.Device;
        var context = deviceResources.ImmediateContext;

        var cropDesc = new Texture2DDescription
        {
            Width = region.Width,
            Height = region.Height,
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

        var sourceBox = new Box(region.X, region.Y, 0, region.X + region.Width, region.Y + region.Height, 1);
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
            Width = width,
            Height = height,
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
            var stride = map.RowPitch;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var sourceOffset = y * stride + x * 8; // FP16 = 4 channels * 2 bytes
                    var destOffset = (y * width + x) * 4;

                    // Read FP16 values (R16G16B16A16_FLOAT)
                    var r = Half.ToHalf(BitConverter.ReadInt16(sourcePtr + sourceOffset));
                    var g = Half.ToHalf(BitConverter.ReadInt16(sourcePtr + sourceOffset + 2));
                    var b = Half.ToHalf(BitConverter.ReadInt16(sourcePtr + sourceOffset + 4));
                    var a = Half.ToHalf(BitConverter.ReadInt16(sourcePtr + sourceOffset + 6));

                    // Convert scRGB linear to sRGB (simple gamma correction)
                    r = LinearToSrgb(r);
                    g = LinearToSrgb(g);
                    b = LinearToSrgb(b);

                    // Clamp to [0, 1] and convert to 8-bit
                    bgra8Data[destOffset] = (byte)(Math.Clamp(b, 0f, 1f) * 255); // B
                    bgra8Data[destOffset + 1] = (byte)(Math.Clamp(g, 0f, 1f) * 255); // G
                    bgra8Data[destOffset + 2] = (byte)(Math.Clamp(r, 0f, 1f) * 255); // R
                    bgra8Data[destOffset + 3] = (byte)(Math.Clamp(a, 0f, 1f) * 255); // A
                }
            }

            // Create BGRA8 texture
            var bgra8Desc = new Texture2DDescription
            {
                Width = width,
                Height = height,
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
                Width = width,
                Height = height,
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
                    Marshal.Copy(bgra8Data, y * width * 4, uploadMap.DataPointer + y * uploadMap.RowPitch, width * 4);
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

    private async Task<byte[]> EncodeAsPngAsync(ID3D11Texture2D bgra8Texture, int width, int height)
    {
        var device = deviceResources.Device;
        var context = deviceResources.ImmediateContext;

        var stagingDesc = new Texture2DDescription
        {
            Width = width,
            Height = height,
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
            var stride = map.RowPitch;

            for (int y = 0; y < height; y++)
            {
                Marshal.Copy(sourcePtr + y * stride, pixelData, y * width * 4, width * 4);
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
        var bytes = new byte[stream.Size];
        await stream.ReadAsync(bytes.AsBuffer(), (uint)bytes.Length, InputStreamOptions.None);

        return bytes;
    }

    private static async Task WriteToClipboardAsync(byte[] pngBytes, int width, int height)
    {
        using var stream = new InMemoryRandomAccessStream();
        await stream.WriteAsync(pngBytes.AsBuffer());
        stream.Seek(0);

        var reference = RandomAccessStreamReference.CreateFromStream(stream);
        var dataPackage = new DataPackage();
        dataPackage.SetBitmap(reference);

        Clipboard.SetContent(dataPackage);
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
