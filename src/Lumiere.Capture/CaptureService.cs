using System.Threading;
using Lumiere.Graphics.Devices;
using Lumiere.Graphics.Hdr;
using Lumiere.Graphics.Presentation;
using Lumiere.Infrastructure.Diagnostics;
using Lumiere.Infrastructure.Interop;
using Microsoft.Extensions.Logging;
using Windows.Foundation;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;

namespace Lumiere.Capture;

public sealed class CaptureService
{
    private static readonly ILogger Logger = LumiereLoggerFactory.CreateLogger(LogCategories.Capture);
    private readonly GraphicsDeviceResources deviceResources;
    private readonly CaptureBorderOptions borderOptions;

    public CaptureService(
        GraphicsDeviceResources deviceResources,
        CaptureBorderOptions? borderOptions = null)
    {
        this.deviceResources = deviceResources ?? throw new ArgumentNullException(nameof(deviceResources));
        this.borderOptions = borderOptions ?? CaptureBorderOptions.RequireSystemBorder();
    }

    public CaptureTarget CreateTarget(GraphicsCaptureItem item) =>
        CaptureTarget.FromItem(item);

    public CaptureStartResult StartCapture(
        CaptureTarget target,
        Action<CapturedFrameTexture> onFrameArrived,
        Action<PreviewReadinessStatus>? onFrameFailed = null,
        Action<string>? onFrameDiagnostic = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(onFrameArrived);

        if (!target.HasCaptureItem)
        {
            Logger.LogWarning("StartCapture FAILED: CaptureTarget has no GraphicsCaptureItem");
            return CaptureStartResult.NotStarted(
                PreviewReadinessStatus.Failed(
                    PreviewReadinessStage.Capture,
                    "Preview failed",
                    "CaptureTarget was created for tests and does not contain a GraphicsCaptureItem."));
        }

        if (!GraphicsCaptureSession.IsSupported())
        {
            Logger.LogWarning("StartCapture FAILED: GraphicsCaptureSession.IsSupported=false");
            return CaptureStartResult.NotStarted(
                PreviewReadinessStatus.Unsupported(
                    PreviewReadinessStage.Capture,
                    "Unsupported capture",
                    "GraphicsCaptureSession.IsSupported returned false."));
        }

        IDirect3DDevice? direct3DDevice = null;
        Direct3D11CaptureFramePool? framePool = null;
        GraphicsCaptureSession? session = null;
        TypedEventHandler<Direct3D11CaptureFramePool, object?>? frameArrivedHandler = null;
        var frameFailureGate = new FrameFailureGate();

        try
        {
            var options = new CaptureSessionOptions(target.Size.Width, target.Size.Height);
            direct3DDevice = Direct3D11Interop.CreateDirect3DDevice(deviceResources.DxgiDevice);
            framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
                direct3DDevice,
                options.PixelFormat,
                options.BufferCount,
                options.BufferSize);

            Logger.LogDebug("WGC FramePool created: pixelFormat={PixelFormat}, bufferSize={BufferSize}, target={DisplayName} ({Width}x{Height})", options.PixelFormat, options.BufferCount, target.DisplayName, target.Size.Width, target.Size.Height);

            frameArrivedHandler = (sender, _) =>
            {
                if (!frameFailureGate.ShouldProcessFrame)
                {
                    return;
                }

                HandleFrameArrived(
                    sender,
                    onFrameArrived,
                    onFrameFailed,
                    onFrameDiagnostic,
                    frameFailureGate);
            };
            framePool.FrameArrived += frameArrivedHandler;
            session = framePool.CreateCaptureSession(target.Item);
            var borderResult = borderOptions.ApplyToSession(session);
            LogBorderResult(borderResult);
            session.StartCapture();

            Logger.LogInformation("WGC session started: IsSupported=true, target={DisplayName} ({Width}x{Height}), kind={Kind}", target.DisplayName, target.Size.Width, target.Size.Height, target.Kind);

            return CaptureStartResult.StartSucceeded(
                new CaptureSessionResources(direct3DDevice, framePool, session, frameArrivedHandler),
                PreviewReadinessStatus.Initializing(
                    PreviewReadinessStage.Capture,
                    "Initializing preview",
                    $"Direct3D11CaptureFramePool started with {options.PixelFormat} and {options.BufferCount} buffers for {target.DisplayName}. {borderResult.TechnicalDetail}"));
        }
        catch (Exception exception)
        {
            if (framePool is not null && frameArrivedHandler is not null)
            {
                framePool.FrameArrived -= frameArrivedHandler;
            }

            (session as IDisposable)?.Dispose();
            (framePool as IDisposable)?.Dispose();
            (direct3DDevice as IDisposable)?.Dispose();

            Logger.LogError(exception, "StartCapture EXCEPTION");

            return CaptureStartResult.NotStarted(MapFailureToReadiness(exception));
        }
    }

    private static void LogBorderResult(CaptureBorderApplicationResult borderResult)
    {
        if (borderResult.RequestedBorderless && !borderResult.Succeeded)
        {
            Logger.LogWarning(
                "WGC borderless request did not take effect: attempted={Attempted}, effectiveIsBorderRequired={EffectiveIsBorderRequired}, detail={Detail}",
                borderResult.Attempted,
                borderResult.EffectiveIsBorderRequired,
                borderResult.TechnicalDetail);
            return;
        }

        Logger.LogDebug(
            "WGC border policy applied: requestedBorderless={RequestedBorderless}, attempted={Attempted}, effectiveIsBorderRequired={EffectiveIsBorderRequired}, detail={Detail}",
            borderResult.RequestedBorderless,
            borderResult.Attempted,
            borderResult.EffectiveIsBorderRequired,
            borderResult.TechnicalDetail);
    }

    public static PreviewReadinessStatus MapFailureToReadiness(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is NativeInteropException nativeInteropException)
        {
            return PreviewReadinessStatus.Failed(
                PreviewReadinessStage.Interop,
                "Preview failed",
                InteropFailureDiagnostics.Write(nativeInteropException));
        }

        return PreviewReadinessStatus.Failed(
            PreviewReadinessStage.Capture,
            "Preview failed",
            InteropFailureDiagnostics.Write(exception));
    }

    private static void HandleFrameArrived(
        Direct3D11CaptureFramePool framePool,
        Action<CapturedFrameTexture> onFrameArrived,
        Action<PreviewReadinessStatus>? onFrameFailed,
        Action<string>? onFrameDiagnostic,
        FrameFailureGate frameFailureGate)
    {
        try
        {
            onFrameDiagnostic?.Invoke("FrameArrived event received.");

            using var frame = framePool.TryGetNextFrame();
            if (frame is null)
            {
                onFrameDiagnostic?.Invoke("FrameArrived event had no frame available.");
                return;
            }

            onFrameDiagnostic?.Invoke($"Captured frame received: {frame.ContentSize.Width}x{frame.ContentSize.Height}.");

            CapturedFrameTexture? capturedFrame = null;

            try
            {
                capturedFrame = new CapturedFrameTexture(
                    Direct3D11SurfaceInterop.CreateTexture(frame.Surface),
                    frame.ContentSize.Width,
                    frame.ContentSize.Height,
                    "Direct3D11CaptureFrame.Surface");
                onFrameDiagnostic?.Invoke("Captured frame surface unwrapped as ID3D11Texture2D.");
                onFrameArrived(capturedFrame);
                capturedFrame = null;
            }
            finally
            {
                capturedFrame?.Dispose();
            }
        }
        catch (Exception exception)
        {
            if (frameFailureGate.TryMarkFailed())
            {
                Logger.LogError(exception, "FrameArrived FAILED");
                TryReportFrameFailure(exception, onFrameFailed);
            }
        }
    }

    private static void TryReportFrameFailure(
        Exception exception,
        Action<PreviewReadinessStatus>? onFrameFailed)
    {
        try
        {
            onFrameFailed?.Invoke(MapFailureToReadiness(exception));
        }
        catch
        {
            // FrameArrived runs outside the UI thread; teardown races must not escape the WGC callback.
        }
    }

    private sealed class FrameFailureGate
    {
        private int failed;

        public bool ShouldProcessFrame => Volatile.Read(ref failed) == 0;

        public bool TryMarkFailed() => Interlocked.Exchange(ref failed, 1) == 0;
    }
}
