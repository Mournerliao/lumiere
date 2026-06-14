using Lumiere.Capture;
using Lumiere.Graphics.Hdr;
using Lumiere.Graphics.Output;
using Lumiere.Settings;

namespace Lumiere.App;

public sealed record TrayMenuProjection(
    string AppName,
    string HdrStatusLabel,
    string HdrStatusDetail,
    string TrayAlertMessage,
    int TrayAlertSeverity,
    TrayMenuCommandProjection FullscreenCapture,
    TrayMenuCommandProjection RegionCapture,
    TrayMenuCommandProjection OpenMainWindow,
    TrayMenuCommandProjection OpenSettings,
    TrayMenuCommandProjection Quit)
{
    public static TrayMenuProjection Project(
        CaptureSessionState state,
        ISettingsProvider settingsProvider,
        IAboutInfoProvider aboutInfoProvider,
        CaptureCommandMode? activeCaptureMode = null,
        OutputResult? outputResult = null,
        bool hdrAlertsEnabled = false)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(settingsProvider);
        ArgumentNullException.ThrowIfNull(aboutInfoProvider);

        var main = MainPanelProjection.Project(state, outputResult, hdrAlertsEnabled);
        var appName = string.IsNullOrWhiteSpace(aboutInfoProvider.AppName)
            ? "Lumiere"
            : aboutInfoProvider.AppName;
        var activeLabel = state.Status switch
        {
            CaptureSessionStatus.SelectingTarget => "Preparing...",
            CaptureSessionStatus.Initializing => "Starting...",
            CaptureSessionStatus.Capturing => "Capturing...",
            CaptureSessionStatus.Degraded => "Capture degraded",
            _ => main.ActionTitle,
        };

        var trayAlertMessage = MapTrayAlertMessage(state.Readiness.State, outputResult, hdrAlertsEnabled);
        var alertSeverity = AlertMapping.Classify(state.Readiness.State, outputResult, hdrAlertsEnabled);

        return new TrayMenuProjection(
            AppName: appName,
            HdrStatusLabel: main.TrustLabel,
            HdrStatusDetail: main.TrustMessage,
            TrayAlertMessage: trayAlertMessage,
            TrayAlertSeverity: (int)alertSeverity,
            FullscreenCapture: CreateCaptureCommand(
                "Full Screen",
                settingsProvider.FullscreenShortcut,
                main.CanStartCapture,
                state.Status,
                activeLabel,
                isActiveMode: activeCaptureMode == CaptureCommandMode.Fullscreen),
            RegionCapture: CreateCaptureCommand(
                "Region",
                settingsProvider.RegionShortcut,
                main.CanStartCapture,
                state.Status,
                activeLabel,
                isActiveMode: activeCaptureMode == CaptureCommandMode.Region),
            OpenMainWindow: new TrayMenuCommandProjection("Open Lumiere", null, true, false),
            OpenSettings: new TrayMenuCommandProjection("Settings", null, true, false),
            Quit: new TrayMenuCommandProjection("Quit", null, true, false));
    }

    private static string MapTrayAlertMessage(PreviewReadinessState readinessState, OutputResult? outputResult, bool hdrAlertsEnabled)
    {
        return AlertMapping.Classify(readinessState, outputResult, hdrAlertsEnabled) switch
        {
            AlertMapping.AlertSeverity.Degraded => "Enable HDR for best quality",
            AlertMapping.AlertSeverity.Unsupported => "HDR unavailable on this display",
            AlertMapping.AlertSeverity.Failed => "Preview failed",
            _ => string.Empty,
        };
    }

    private static TrayMenuCommandProjection CreateCaptureCommand(
        string idleLabel,
        string? shortcut,
        bool canStartCapture,
        CaptureSessionStatus status,
        string activeLabel,
        bool isActiveMode)
    {
        var isSessionActive = status is CaptureSessionStatus.SelectingTarget
            or CaptureSessionStatus.Initializing
            or CaptureSessionStatus.Capturing
            or CaptureSessionStatus.Degraded;

        var isBlocked = status is CaptureSessionStatus.Unsupported
            or CaptureSessionStatus.Failed
            or CaptureSessionStatus.Disposed;

        var isActive = isSessionActive && isActiveMode;

        return new TrayMenuCommandProjection(
            isActive ? activeLabel : idleLabel,
            MainPanelProjection.FormatShortcut(shortcut),
            canStartCapture && !isBlocked,
            isActive);
    }
}

public sealed record TrayMenuCommandProjection(
    string Label,
    string? ShortcutText,
    bool IsEnabled,
    bool IsActive);
