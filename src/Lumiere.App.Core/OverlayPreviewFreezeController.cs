using Lumiere.Capture;

namespace Lumiere.App;

internal sealed class OverlayPreviewFreezeController
{
    private readonly CaptureCommandMode captureMode;

    public OverlayPreviewFreezeController(CaptureCommandMode captureMode)
    {
        this.captureMode = captureMode;
    }

    public bool IsFrozen { get; private set; }

    public bool AcceptsCallbacks => !IsFrozen;

    public OverlayPreviewFrameDisposition OnFramePresented(bool requiresRecreation)
    {
        if (IsFrozen)
        {
            return OverlayPreviewFrameDisposition.Ignore;
        }

        if (requiresRecreation || captureMode is not CaptureCommandMode.Region)
        {
            return OverlayPreviewFrameDisposition.Continue;
        }

        IsFrozen = true;
        return OverlayPreviewFrameDisposition.FreezeAfterPresent;
    }
}

internal enum OverlayPreviewFrameDisposition
{
    Ignore = 0,
    Continue,
    FreezeAfterPresent,
}
