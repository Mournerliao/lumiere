namespace Lumiere.Windows.Capture;

public sealed record CaptureSessionDisposalResult(
    bool FrameHandlerUnsubscribed,
    bool SessionStopped,
    bool FramePoolDisposed,
    bool DeviceDisposed)
{
    public bool Completed => FrameHandlerUnsubscribed && SessionStopped && FramePoolDisposed && DeviceDisposed;
}
