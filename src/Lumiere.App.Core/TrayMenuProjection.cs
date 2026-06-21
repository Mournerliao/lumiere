using Lumiere.Capture;
using Lumiere.Graphics.Hdr;
using Lumiere.Graphics.Output;
using Lumiere.Infrastructure.Interop;
using Lumiere.Settings;

namespace Lumiere.App;

public sealed record TrayMenuProjection(
    string AppName,
    string HdrStatusLabel,
    string HdrStatusDetail,
    string FidelityClaimLabel,
    string FidelityClaimDetail,
    TrayMenuStatusSeverity FidelityClaimSeverity,
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

        var main = MainPanelProjection.Project(
            state,
            outputResult,
            hdrAlertsEnabled,
            settingsProvider.ExportColorFormat);
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

        var trayAlertMessage = MapTrayAlertMessage(state.Readiness, outputResult, hdrAlertsEnabled);
        var alertSeverity = AlertMapping.Classify(state.Readiness, outputResult, hdrAlertsEnabled);

        return new TrayMenuProjection(
            AppName: appName,
            HdrStatusLabel: main.TrustLabel,
            HdrStatusDetail: main.TrustMessage,
            FidelityClaimLabel: main.FidelityClaim.Label,
            FidelityClaimDetail: main.FidelityClaim.Detail,
            FidelityClaimSeverity: ToTrayStatusSeverity(main.FidelityClaim.Severity),
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

    private static TrayMenuStatusSeverity ToTrayStatusSeverity(MainPanelTrustSeverity severity) =>
        severity switch
        {
            MainPanelTrustSeverity.Success => TrayMenuStatusSeverity.Success,
            MainPanelTrustSeverity.Warning => TrayMenuStatusSeverity.Warning,
            MainPanelTrustSeverity.Error => TrayMenuStatusSeverity.Error,
            MainPanelTrustSeverity.Info => TrayMenuStatusSeverity.Info,
            _ => TrayMenuStatusSeverity.Neutral,
        };

    private static string MapTrayAlertMessage(
        PreviewReadinessStatus readiness,
        OutputResult? outputResult,
        bool hdrAlertsEnabled)
    {
        return AlertMapping.Classify(readiness, outputResult, hdrAlertsEnabled) switch
        {
            AlertMapping.AlertSeverity.TargetDisplayUnresolved => "HDR unvalidated for selected target",
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
