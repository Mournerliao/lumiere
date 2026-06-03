using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Lumiere.Graphics.Devices;
using Lumiere.Graphics.Hdr;
using Lumiere.Graphics.Output;
using Lumiere.Graphics.Presentation;
using Lumiere.Infrastructure.Diagnostics;
using Microsoft.Extensions.Logging;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Half = System.Half;

namespace Lumiere.Graphics.Clipboard;

public sealed class ClipboardOutputService : IOutputService, IOutputPngEncoder, IDisposable
{
    private static readonly ILogger Logger = LumiereLoggerFactory.CreateLogger(LogCategories.Graphics);
    private readonly GraphicsDeviceResources? deviceResources;
    private readonly Func<OutputRequest, CancellationToken, Task<OutputResult>> executeCoreAsync;
    private bool disposed;

    public ClipboardOutputService(GraphicsDeviceResources deviceResources)
    {
        this.deviceResources = deviceResources ?? throw new ArgumentNullException(nameof(deviceResources));
        executeCoreAsync = ExecuteNativeOutputAsync;
    }

    internal ClipboardOutputService(Func<OutputRequest, CancellationToken, Task<OutputResult>> executeCoreAsync)
    {
        this.executeCoreAsync = executeCoreAsync ?? throw new ArgumentNullException(nameof(executeCoreAsync));
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
                Logger.LogWarning("Clipboard region INVALID: ({Width}x{Height}) in {SourceWidth}x{SourceHeight}", pixelWidth, pixelHeight, sourceWidth, sourceHeight);
                return false;
            }

            var frame = new CapturedFrameTexture(sourceTexture, sourceWidth, sourceHeight, "Clipboard source texture");
            var pngBytes = await EncodePngAsync(frame, new CropPixelRect(pixelX, pixelY, pixelWidth, pixelHeight));
            await WriteToClipboardAsync(pngBytes);

            Logger.LogInformation("Clipboard output success: PNG encoded, {Bytes} bytes, crop=({Width}x{Height})", pngBytes.Length, pixelWidth, pixelHeight);
            return true;
        }
        catch (Exception ex)
        {
            var diagnostic = DiagnosticContext.OutputFailure(
                stage: "ClipboardWrite",
                userFacingState: "Failed to copy to clipboard",
                technicalDetail: $"operation=ClipboardOutput, stage=ExecuteOutput, exception={ex.GetType().Name}: {ex.Message}",
                exception: ex);
            diagnostic.LogTo(Logger);

            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<OutputResult> ExecuteOutputAsync(OutputRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.Policy.ShouldAttemptClipboard)
        {
            Logger.LogInformation(
                "operation=ClipboardOutput, stage=Policy, detail=Clipboard output skipped by configured output policy, target={Target}, copyAsImage={CopyAsImage}",
                request.Policy.Target,
                request.Policy.CopyAsImage);
            return OutputResult.ClipboardSkipped("Clipboard output skipped by settings");
        }

        try
        {
            return await executeCoreAsync(request, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Logger.LogInformation(
                "operation=ClipboardOutput, stage=Cancelled, detail=Output cancelled by caller, target={Target}",
                request.Policy.Target);
            throw;
        }
        catch (Exception ex)
        {
            var diagnostic = DiagnosticContext.OutputFailure(
                stage: "ClipboardOutput",
                userFacingState: "Failed to copy to clipboard",
                technicalDetail: $"operation=ClipboardOutput, stage=ExecuteOutput, exception={ex.GetType().Name}: {ex.Message}",
                exception: ex);
            diagnostic.LogTo(Logger);

            return OutputResult.ClipboardFailed(ex.Message);
        }
    }

    private async Task<OutputResult> ExecuteNativeOutputAsync(OutputRequest request, CancellationToken cancellationToken)
    {
        var texture = request.Texture;
        if (texture?.Texture is null)
        {
            Logger.LogWarning("ExecuteOutputAsync FAILED: operation=ClipboardOutput, stage=ValidateInput, detail=texture is null");
            return OutputResult.Skipped("No captured frame texture available");
        }

        // Determine crop region: use provided crop region or full frame
        var cropRegion = request.CropRegion;
        int pixelX = cropRegion?.X ?? 0;
        int pixelY = cropRegion?.Y ?? 0;
        int pixelWidth = cropRegion?.Width ?? texture.Width;
        int pixelHeight = cropRegion?.Height ?? texture.Height;

        try
        {
            if (!ValidateRegion(pixelX, pixelY, pixelWidth, pixelHeight, texture.Width, texture.Height))
            {
                Logger.LogWarning("ExecuteOutputAsync region INVALID: operation=ClipboardOutput, stage=ValidateRegion, crop=({Width}x{Height}) in {SourceWidth}x{SourceHeight}", pixelWidth, pixelHeight, texture.Width, texture.Height);
                return OutputResult.Skipped("Invalid crop region");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var pngBytes = await EncodePngAsync(texture, cropRegion, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            await WriteToClipboardAsync(pngBytes);

            Logger.LogInformation("ExecuteOutputAsync success: operation=ClipboardOutput, stage=Complete, bytes={Bytes}, crop=({Width}x{Height})", pngBytes.Length, pixelWidth, pixelHeight);
            return OutputResult.ClipboardSuccess(pngBytes.Length);
        }
        catch (OperationCanceledException)
        {
            Logger.LogInformation("operation=ClipboardOutput, stage=Cancelled, detail=Output cancelled by caller, crop=({Width}x{Height})", pixelWidth, pixelHeight);
            throw;
        }
        catch (Exception ex)
        {
            var diagnostic = DiagnosticContext.OutputFailure(
                stage: "ClipboardNativeOutput",
                userFacingState: "Failed to copy to clipboard",
                technicalDetail: $"operation=ClipboardOutput, stage=ExecuteOutput, exception={ex.GetType().Name}: {ex.Message}",
                exception: ex);
            diagnostic.LogTo(Logger);

            return OutputResult.ClipboardFailed(ex.Message);
        }
    }

    public async Task<byte[]> EncodePngAsync(
        CapturedFrameTexture texture,
        CropPixelRect? cropRegion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(texture);
        if (texture.Texture is null)
        {
            throw new InvalidOperationException("Captured frame texture is unavailable.");
        }

        int pixelX = cropRegion?.X ?? 0;
        int pixelY = cropRegion?.Y ?? 0;
        int pixelWidth = cropRegion?.Width ?? texture.Width;
        int pixelHeight = cropRegion?.Height ?? texture.Height;

        if (!ValidateRegion(pixelX, pixelY, pixelWidth, pixelHeight, texture.Width, texture.Height))
        {
            throw new InvalidOperationException("Invalid crop region.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        using var croppedTexture = CropTexture(texture.Texture, pixelX, pixelY, pixelWidth, pixelHeight);
        using var bgra8Texture = ConvertToBgra8(croppedTexture, pixelWidth, pixelHeight);

        cancellationToken.ThrowIfCancellationRequested();

        return await EncodeAsPngAsync(bgra8Texture, pixelWidth, pixelHeight);
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
        var resources = deviceResources ?? throw new InvalidOperationException("Clipboard output device resources are unavailable.");
        var device = resources.Device;
        var context = resources.ImmediateContext;

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
        var resources = deviceResources ?? throw new InvalidOperationException("Clipboard output device resources are unavailable.");
        var device = resources.Device;
        var context = resources.ImmediateContext;

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
        var resources = deviceResources ?? throw new InvalidOperationException("Clipboard output device resources are unavailable.");
        var device = resources.Device;
        var context = resources.ImmediateContext;

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
