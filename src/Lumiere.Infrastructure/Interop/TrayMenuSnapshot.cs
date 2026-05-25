namespace Lumiere.Infrastructure.Interop;

public sealed record TrayMenuSnapshot(
    string AppName,
    string HdrStatusLabel,
    string HdrStatusDetail,
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
