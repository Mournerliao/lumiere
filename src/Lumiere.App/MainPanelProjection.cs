using Lumiere.Capture;
using Lumiere.Graphics.Hdr;

namespace Lumiere.App;

public sealed record MainPanelProjection(
    bool CanStartCapture,
    string ActionTitle,
    string TrustLabel,
    string TrustMessage,
    string TrustGlyph,
    MainPanelTrustSeverity TrustSeverity)
{
    public static MainPanelProjection Project(CaptureSessionState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var canStartCapture = state.Status is CaptureSessionStatus.Idle;
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
            PreviewReadinessState.Ready => ("HDR Ready", "\uE930", MainPanelTrustSeverity.Success),
            PreviewReadinessState.Degraded => ("Enable HDR", "\uE7BA", MainPanelTrustSeverity.Warning),
            PreviewReadinessState.Unsupported => ("HDR unsupported", "\uE783", MainPanelTrustSeverity.Error),
            PreviewReadinessState.Failed => ("HDR status failed", "\uE783", MainPanelTrustSeverity.Error),
            _ => ("Checking HDR", "\uE9D5", MainPanelTrustSeverity.Neutral),
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

public enum MainPanelTrustSeverity
{
    Neutral = 0,
    Success,
    Warning,
    Error,
}
