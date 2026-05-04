namespace Lumiere.Capture;

public enum CaptureSessionStatus
{
    Idle = 0,
    SelectingTarget,
    Initializing,
    Capturing,
    Degraded,
    Unsupported,
    Failed,
    Disposed,
}
