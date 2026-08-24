namespace Lumiere.Windows.Capture;

internal sealed record CaptureSessionDisposalResult(
    bool FrameHandlerUnsubscribed,
    bool SessionStopped,
    bool FramePoolDisposed,
    bool DeviceDisposed,
    Exception? FirstException = null)
{
    public bool Completed =>
        FrameHandlerUnsubscribed
        && SessionStopped
        && FramePoolDisposed
        && DeviceDisposed
        && FirstException is null;
}
