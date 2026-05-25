using Lumiere.Capture;
using Lumiere.Settings;

namespace Lumiere.App;

public sealed record TrayMenuProjection(
    string AppName,
    string HdrStatusLabel,
    string HdrStatusDetail,
    TrayMenuCommandProjection FullscreenCapture,
    TrayMenuCommandProjection RegionCapture,
    TrayMenuCommandProjection OpenMainWindow,
    TrayMenuCommandProjection OpenSettings,
    TrayMenuCommandProjection Quit)
{
    public static TrayMenuProjection Project(
        CaptureSessionState state,
        ISettingsProvider settingsProvider,
        IAboutInfoProvider aboutInfoProvider)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(settingsProvider);
        ArgumentNullException.ThrowIfNull(aboutInfoProvider);

        var main = MainPanelProjection.Project(state);
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

        return new TrayMenuProjection(
            AppName: appName,
            HdrStatusLabel: main.TrustLabel,
            HdrStatusDetail: main.TrustMessage,
            FullscreenCapture: CreateCaptureCommand(
                "Full Screen",
                settingsProvider.FullscreenShortcut,
                main.CanStartCapture,
                state.Status,
                activeLabel),
            RegionCapture: CreateCaptureCommand(
                "Region",
                settingsProvider.RegionShortcut,
                main.CanStartCapture,
                state.Status,
                activeLabel),
            OpenMainWindow: new TrayMenuCommandProjection("Open Lumiere", null, true, false),
            OpenSettings: new TrayMenuCommandProjection("Settings", null, true, false),
            Quit: new TrayMenuCommandProjection("Quit", null, true, false));
    }

    private static TrayMenuCommandProjection CreateCaptureCommand(
        string idleLabel,
        string? shortcut,
        bool canStartCapture,
        CaptureSessionStatus status,
        string activeLabel)
    {
        var isActive = status is CaptureSessionStatus.SelectingTarget
            or CaptureSessionStatus.Initializing
            or CaptureSessionStatus.Capturing
            or CaptureSessionStatus.Degraded;

        return new TrayMenuCommandProjection(
            isActive ? activeLabel : idleLabel,
            MainPanelProjection.FormatShortcut(shortcut),
            canStartCapture,
            isActive);
    }
}

public sealed record TrayMenuCommandProjection(
    string Label,
    string? ShortcutText,
    bool IsEnabled,
    bool IsActive);
