namespace Lumiere.Overlay;

public sealed record OverlayState(
    OverlayDisplayStatus Status,
    string Label,
    string Message,
    string TechnicalDetail,
    OverlayFailureAction FailureAction)
{
    public bool IsTerminal => Status is OverlayDisplayStatus.Closing or OverlayDisplayStatus.Disposed;

    public bool RequiresFailureTeardown =>
        Status is OverlayDisplayStatus.PreviewFailed && FailureAction is OverlayFailureAction.CloseAfterTeardown;

    public static OverlayState Initializing(string message, string technicalDetail = "") =>
        new(
            OverlayDisplayStatus.Initializing,
            "Initializing preview",
            message,
            technicalDetail,
            OverlayFailureAction.KeepOpenWithFailure);

    public static OverlayState HdrReady(string message, string technicalDetail = "") =>
        new(
            OverlayDisplayStatus.HdrReady,
            "HDR-ready",
            message,
            technicalDetail,
            OverlayFailureAction.KeepOpenWithFailure);

    public static OverlayState DegradedPreview(string message, string technicalDetail = "") =>
        new(
            OverlayDisplayStatus.DegradedPreview,
            "Degraded preview",
            message,
            technicalDetail,
            OverlayFailureAction.KeepOpenWithFailure);

    public static OverlayState UnsupportedCapture(string message, string technicalDetail = "") =>
        new(
            OverlayDisplayStatus.UnsupportedCapture,
            "Unsupported capture",
            message,
            technicalDetail,
            OverlayFailureAction.KeepOpenWithFailure);

    public static OverlayState PreviewFailed(
        string message,
        string technicalDetail = "",
        OverlayFailureAction failureAction = OverlayFailureAction.CloseAfterTeardown) =>
        new(
            OverlayDisplayStatus.PreviewFailed,
            "Preview failed",
            message,
            technicalDetail,
            failureAction);

    public static OverlayState Closing(string message = "Closing overlay", string technicalDetail = "") =>
        new(
            OverlayDisplayStatus.Closing,
            "Closing",
            message,
            technicalDetail,
            OverlayFailureAction.CloseAfterTeardown);

    public static OverlayState InvalidCrop(string message = "Crop region too small. Try again.", string technicalDetail = "") =>
        new(
            OverlayDisplayStatus.InvalidCrop,
            "Selection too small",
            message,
            technicalDetail,
            OverlayFailureAction.KeepOpenWithFailure);

    public static OverlayState Disposed(string message = "Preview stopped", string technicalDetail = "") =>
        new(
            OverlayDisplayStatus.Disposed,
            "Preview stopped",
            message,
            technicalDetail,
            OverlayFailureAction.CloseAfterTeardown);
}
