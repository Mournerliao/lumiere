using Lumiere.Capture;
using Lumiere.Settings;

namespace Lumiere.App;

public sealed record SettingsPanelProjection(
    ShortcutSettingProjection FullscreenShortcut,
    ShortcutSettingProjection RegionShortcut,
    bool HdrAlertsEnabled,
    string HdrAlertsHelpText,
    bool OptionalHdrAlertChromeEnabled,
    OutputSettingsProjection Output,
    bool TimestampNaming,
    bool CopyAsImage,
    MainPanelProjection MainPanel)
{
    public static SettingsPanelProjection Project(ISettingsProvider settingsProvider, CaptureSessionState sessionState)
    {
        ArgumentNullException.ThrowIfNull(settingsProvider);
        ArgumentNullException.ThrowIfNull(sessionState);

        return new SettingsPanelProjection(
            ShortcutSettingProjection.PendingRegistration("Fullscreen shortcut", settingsProvider.FullscreenShortcut),
            ShortcutSettingProjection.PendingRegistration("Region shortcut", settingsProvider.RegionShortcut),
            settingsProvider.HdrAlertsEnabled,
            "Show warnings when HDR is unavailable, degraded, unsupported, or failed.",
            settingsProvider.HdrAlertsEnabled,
            OutputSettingsProjection.ReadOnly(settingsProvider.OutputTarget),
            settingsProvider.TimestampNaming,
            settingsProvider.CopyAsImage,
            MainPanelProjection.Project(sessionState));
    }
}

public sealed record OutputSettingsProjection(
    string DisplayValue,
    bool IsClipboardSelected,
    bool IsFolderSelected,
    bool IsBothSelected,
    bool IsReadOnly)
{
    public static OutputSettingsProjection ReadOnly(Lumiere.Graphics.Output.OutputTarget outputTarget) =>
        outputTarget switch
        {
            Lumiere.Graphics.Output.OutputTarget.Folder => new("Folder", false, true, false, true),
            Lumiere.Graphics.Output.OutputTarget.Both => new("Both", false, false, true, true),
            _ => new("Clipboard", true, false, false, true),
        };
}

public sealed record ShortcutSettingProjection(
    string Label,
    string DisplayValue,
    bool IsReadOnly,
    bool IsPendingRegistration,
    string PendingReason,
    string HelpText)
{
    public static ShortcutSettingProjection PendingRegistration(string label, string? shortcut)
    {
        var displayValue = MainPanelProjection.FormatShortcut(shortcut);
        const string pendingReason = "Global registration arrives in Epic 7";
        return new ShortcutSettingProjection(
            label,
            displayValue,
            IsReadOnly: true,
            IsPendingRegistration: true,
            pendingReason,
            $"{label} is currently {displayValue}. Editing is pending until Epic 7 global hotkey registration.");
    }
}
