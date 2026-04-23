using Windows.Foundation;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;

namespace Lumiere.Capture;

public sealed class CaptureSessionResources : IDisposable
{
    private readonly Action disposeAction;
    private bool disposed;

    public CaptureSessionResources(
        IDirect3DDevice device,
        Direct3D11CaptureFramePool framePool,
        GraphicsCaptureSession session,
        TypedEventHandler<Direct3D11CaptureFramePool, object?> frameArrivedHandler)
        : this(
            () => CaptureSessionDisposalCoordinator.DisposeOnce(
                () => framePool.FrameArrived -= frameArrivedHandler,
                () => (session as IDisposable)?.Dispose(),
                () => (framePool as IDisposable)?.Dispose(),
                () => (device as IDisposable)?.Dispose()))
    {
    }

    internal CaptureSessionResources(Action disposeAction)
    {
        this.disposeAction = disposeAction ?? throw new ArgumentNullException(nameof(disposeAction));
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposeAction();
        disposed = true;
    }
}
