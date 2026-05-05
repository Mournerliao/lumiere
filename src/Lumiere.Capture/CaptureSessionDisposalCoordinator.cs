namespace Lumiere.Capture;

public static class CaptureSessionDisposalCoordinator
{
    public static CaptureSessionDisposalEvidence DisposeOnce(
        Action unsubscribeFrameHandler,
        Action stopSession,
        Action disposeFramePool,
        Action disposeDevice)
    {
        ArgumentNullException.ThrowIfNull(unsubscribeFrameHandler);
        ArgumentNullException.ThrowIfNull(stopSession);
        ArgumentNullException.ThrowIfNull(disposeFramePool);
        ArgumentNullException.ThrowIfNull(disposeDevice);

        unsubscribeFrameHandler();
        stopSession();
        disposeFramePool();
        disposeDevice();

        return new CaptureSessionDisposalEvidence(
            FrameHandlerUnsubscribed: true,
            SessionStopped: true,
            FramePoolDisposed: true,
            DeviceDisposed: true);
    }
}
