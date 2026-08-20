using System.Runtime.InteropServices.WindowsRuntime;
using Lumiere.Windows.Graphics.Devices;
using Lumiere.Windows.Graphics.Output;
using Lumiere.Windows.Graphics.Presentation;
using Lumiere.Windows.Interop.Diagnostics;
using Microsoft.Extensions.Logging;
using Vortice.Direct3D11;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;

namespace Lumiere.Windows.Graphics.Clipboard;

public sealed class ClipboardOutputService : IOutputService, IDisposable
{
    private static readonly ILogger Logger = LumiereLoggerFactory.CreateLogger(LogCategories.Graphics);
    private readonly IOutputPngEncoder? encoder;
    private readonly Func<OutputRequest, CancellationToken, Task<OutputResult>> executeCoreAsync;
    private bool disposed;

    public ClipboardOutputService(GraphicsDeviceResources deviceResources)
        : this(new SrgbVisualMatchPngEncoder(
            new SrgbVisualMatchConverter(
                new CapturedFrameTextureReadback(deviceResources))))
    {
    }

    public ClipboardOutputService(IOutputPngEncoder encoder)
    {
        this.encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
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

        if (!request.ShouldWriteClipboard)
        {
            Logger.LogInformation(
                "operation=ClipboardOutput, stage=Policy, detail=Clipboard output was not requested, delivery={Delivery}, copyAsImage={CopyAsImage}",
                request.Delivery,
                request.CopyAsImage);
            return OutputResult.ClipboardSkipped("Clipboard output was not requested");
        }

        try
        {
            return await executeCoreAsync(request, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Logger.LogInformation(
                "operation=ClipboardOutput, stage=Cancelled, detail=Output cancelled by caller, target={Target}",
                request.Delivery);
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

            var artifact = await RequireEncoder().EncodeArtifactAsync(
                texture,
                cropRegion,
                cancellationToken,
                request.ArtifactCache);

            cancellationToken.ThrowIfCancellationRequested();

            await WriteToClipboardAsync(artifact.Bytes);

            Logger.LogInformation("ExecuteOutputAsync success: operation=ClipboardOutput, stage=Complete, bytes={Bytes}, crop=({Width}x{Height}), profile={Profile}", artifact.Bytes.Length, pixelWidth, pixelHeight, OutputEncodedArtifact.Profile);
            return OutputResult.ClipboardSuccess(artifact.Bytes.Length);
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
        CancellationToken cancellationToken = default) =>
        await RequireEncoder().EncodePngAsync(texture, cropRegion, cancellationToken);

    public async Task<OutputEncodedArtifact> EncodeArtifactAsync(
        CapturedFrameTexture texture,
        CropPixelRect? cropRegion,
        CancellationToken cancellationToken = default,
        OutputArtifactCache? artifactCache = null) =>
        await RequireEncoder().EncodeArtifactAsync(
            texture,
            cropRegion,
            cancellationToken,
            artifactCache);

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

    private IOutputPngEncoder RequireEncoder() =>
        encoder ?? throw new InvalidOperationException("Clipboard output encoder is unavailable.");

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
