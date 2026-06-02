namespace Lumiere.Infrastructure.Interop;

public sealed record TrayMenuSnapshot(
    string AppName,
    string HdrStatusLabel,
    string HdrStatusDetail,
    string TrayAlertMessage,
    TrayMenuItemSnapshot FullscreenCapture,
    TrayMenuItemSnapshot RegionCapture,
    TrayMenuItemSnapshot OpenMainWindow,
    TrayMenuItemSnapshot OpenSettings,
    TrayMenuItemSnapshot Quit);

public sealed record TrayMenuItemSnapshot(
    string Label,
    string? ShortcutText,
    bool IsEnabled,
    bool IsActive);
