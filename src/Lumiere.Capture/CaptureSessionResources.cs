using Lumiere.Infrastructure.Diagnostics;
using Microsoft.Extensions.Logging;
using Windows.Foundation;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;

namespace Lumiere.Capture;

public sealed class CaptureSessionResources : IDisposable
{
    private static readonly ILogger Logger = LumiereLoggerFactory.CreateLogger(LogCategories.Capture);
    private readonly Func<CaptureSessionDisposalEvidence> disposeAction;
    private int disposed;

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
        if (Interlocked.Exchange(ref disposed, 1) == 1)
        {
            return;
        }

        DisposalEvidence = disposeAction();
        Logger.LogDebug("CaptureSession disposed: {DisposalEvidence}", DisposalEvidence);
    }
}
