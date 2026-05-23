using Lumiere.Capture;
using Lumiere.Graphics.Hdr;

namespace Lumiere.App;

public sealed record MainPanelProjection(
    bool CanStartCapture,
    string ActionTitle,
    string TrustLabel,
    string TrustMessage,
    MainPanelTrustIcon TrustIcon,
    MainPanelTrustSeverity TrustSeverity)
{
    public static MainPanelProjection Project(CaptureSessionState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var canStartCapture = state.Status is CaptureSessionStatus.Idle
            or CaptureSessionStatus.Unsupported
            or CaptureSessionStatus.Failed;
        var actionTitle = state.Status switch
        {
            CaptureSessionStatus.SelectingTarget => "Preparing capture...",
            CaptureSessionStatus.Initializing => "Starting preview...",
            CaptureSessionStatus.Capturing => "Capturing...",
            CaptureSessionStatus.Degraded => "Capture degraded",
            CaptureSessionStatus.Unsupported => "Capture unavailable",
            CaptureSessionStatus.Failed => "Capture failed",
            CaptureSessionStatus.Disposed => "Preview stopped",
            _ => "Ready to capture",
        };

        var trust = state.Readiness.State switch
        {
            PreviewReadinessState.Ready => ("HDR Ready", MainPanelTrustIcon.CheckmarkCircle, MainPanelTrustSeverity.Success),
            PreviewReadinessState.Degraded => ("Enable HDR", MainPanelTrustIcon.Desktop, MainPanelTrustSeverity.Warning),
            PreviewReadinessState.Unsupported => ("HDR unavailable", MainPanelTrustIcon.ErrorCircle, MainPanelTrustSeverity.Error),
            PreviewReadinessState.Failed => ("HDR unavailable", MainPanelTrustIcon.ErrorCircle, MainPanelTrustSeverity.Error),
            _ => ("Checking HDR", MainPanelTrustIcon.Clock, MainPanelTrustSeverity.Neutral),
        };

        return new MainPanelProjection(
            canStartCapture,
            actionTitle,
            trust.Item1,
            string.IsNullOrWhiteSpace(state.UserFacingReason)
                ? "HDR preview status is being checked."
                : state.UserFacingReason,
            trust.Item2,
            trust.Item3);
    }

    public static string FormatShortcut(string? shortcut) =>
        string.IsNullOrWhiteSpace(shortcut)
            ? "Not assigned"
            : shortcut.Trim();
}

public enum MainPanelTrustIcon
{
    Clock = 0,
    CheckmarkCircle,
    Desktop,
    ErrorCircle,
}

public enum MainPanelTrustSeverity
{
    Neutral = 0,
    Success,
    Warning,
    Error,
}
