namespace Lumiere.Overlay;

public enum OverlayDisplayStatus
{
    Initializing = 0,
    HdrReady,
    DegradedPreview,
    UnsupportedCapture,
    PreviewFailed,
    Closing,
    Disposed,
    InvalidCrop,
}
