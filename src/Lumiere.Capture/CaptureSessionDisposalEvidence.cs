namespace Lumiere.Capture;

public sealed record CaptureSessionDisposalEvidence(
    bool FrameHandlerUnsubscribed,
    bool SessionStopped,
    bool FramePoolDisposed,
    bool DeviceDisposed)
{
    public bool Completed => FrameHandlerUnsubscribed && SessionStopped && FramePoolDisposed && DeviceDisposed;
}
