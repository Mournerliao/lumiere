namespace Lumiere.Windows.Capture;

internal enum CaptureSessionStatus
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
