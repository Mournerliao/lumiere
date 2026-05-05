using Windows.Foundation;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;

namespace Lumiere.Capture;

public sealed class CaptureSessionResources : IDisposable
{
    private readonly Func<CaptureSessionDisposalEvidence> disposeAction;
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
        : this(() =>
        {
            disposeAction();
            return new CaptureSessionDisposalEvidence(
                FrameHandlerUnsubscribed: true,
                SessionStopped: true,
                FramePoolDisposed: true,
                DeviceDisposed: true);
        })
    {
    }

    internal CaptureSessionResources(Func<CaptureSessionDisposalEvidence> disposeAction)
    {
        this.disposeAction = disposeAction ?? throw new ArgumentNullException(nameof(disposeAction));
    }

    public CaptureSessionDisposalEvidence? DisposalEvidence { get; private set; }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        DisposalEvidence = disposeAction();
        disposed = true;
    }
}
