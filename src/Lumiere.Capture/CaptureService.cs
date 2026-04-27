using Lumiere.Graphics.Devices;
using Lumiere.Graphics.Hdr;
using Lumiere.Graphics.Presentation;
using Lumiere.Infrastructure.Interop;
using Windows.Foundation;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;

namespace Lumiere.Capture;

public sealed class CaptureService
{
    private readonly GraphicsDeviceResources deviceResources;

    public CaptureService(GraphicsDeviceResources deviceResources)
    {
        this.deviceResources = deviceResources ?? throw new ArgumentNullException(nameof(deviceResources));
    }

    public CaptureTarget CreateTarget(GraphicsCaptureItem item) =>
        CaptureTarget.FromItem(item);

    public CaptureStartResult StartCapture(
        CaptureTarget target,
        Action<CapturedFrameTexture> onFrameArrived)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(onFrameArrived);

        if (!GraphicsCaptureSession.IsSupported())
        {
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

        try
        {
            var options = new CaptureSessionOptions(target.Size.Width, target.Size.Height);
            direct3DDevice = Direct3D11Interop.CreateDirect3DDevice(deviceResources.DxgiDevice);
            framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
                direct3DDevice,
                options.PixelFormat,
                options.BufferCount,
                options.BufferSize);

            frameArrivedHandler = (sender, _) => HandleFrameArrived(sender, onFrameArrived);
            framePool.FrameArrived += frameArrivedHandler;
            session = framePool.CreateCaptureSession(target.Item);
            session.StartCapture();

            return CaptureStartResult.StartSucceeded(
                new CaptureSessionResources(direct3DDevice, framePool, session, frameArrivedHandler),
                PreviewReadinessStatus.Initializing(
                    PreviewReadinessStage.Capture,
                    "Initializing preview",
                    $"Direct3D11CaptureFramePool started with {options.PixelFormat} and {options.BufferCount} buffers for {target.DisplayName}."));
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

            return CaptureStartResult.NotStarted(MapFailureToReadiness(exception));
        }
    }

    public static PreviewReadinessStatus MapFailureToReadiness(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is NativeInteropException nativeInteropException)
        {
            return PreviewReadinessStatus.Failed(
                PreviewReadinessStage.Interop,
                "Preview failed",
                nativeInteropException.Message);
        }

        return PreviewReadinessStatus.Failed(
            PreviewReadinessStage.Capture,
            "Preview failed",
            exception.Message);
    }

    private static void HandleFrameArrived(
        Direct3D11CaptureFramePool framePool,
        Action<CapturedFrameTexture> onFrameArrived)
    {
        using var frame = framePool.TryGetNextFrame();
        if (frame is null)
        {
            return;
        }

        CapturedFrameTexture? capturedFrame = null;

        try
        {
            capturedFrame = new CapturedFrameTexture(
                Direct3D11SurfaceInterop.CreateTexture(frame.Surface),
                frame.ContentSize.Width,
                frame.ContentSize.Height,
                "Direct3D11CaptureFrame.Surface");
            onFrameArrived(capturedFrame);
            capturedFrame = null;
        }
        finally
        {
            capturedFrame?.Dispose();
        }
    }
}
