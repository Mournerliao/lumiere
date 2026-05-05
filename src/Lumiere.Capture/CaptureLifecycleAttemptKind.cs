namespace Lumiere.Capture;

public enum CaptureLifecycleAttemptKind
{
    Start = 0,
    Stop,
    CancelSelection,
    Restart,
    ResizeRecreate,
    FailedInitialization,
    Close,
}
