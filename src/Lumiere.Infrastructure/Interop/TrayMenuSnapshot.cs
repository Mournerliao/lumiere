namespace Lumiere.Infrastructure.Interop;

public sealed record TrayMenuSnapshot(
    string AppName,
    string HdrStatusLabel,
    string HdrStatusDetail,
    string OutputProfileLabel,
    string OutputProfileStatusLabel,
    string OutputProfileDetail,
    TrayMenuStatusSeverity OutputProfileSeverity,
    string FidelityClaimLabel,
    string FidelityClaimDetail,
    TrayMenuStatusSeverity FidelityClaimSeverity,
    string TrayAlertMessage,
    int TrayAlertSeverity,
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

public enum TrayMenuStatusSeverity
{
    Neutral = 0,
    Success,
    Warning,
    Error,
    Info,
}
