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
    AboutInfoProjection About,
    bool TimestampNaming,
    bool CopyAsImage,
    MainPanelProjection MainPanel)
{
    public static SettingsPanelProjection Project(
        ISettingsProvider settingsProvider,
        CaptureSessionState sessionState,
        IAboutInfoProvider? aboutInfoProvider = null)
    {
        ArgumentNullException.ThrowIfNull(settingsProvider);
        ArgumentNullException.ThrowIfNull(sessionState);
        aboutInfoProvider ??= AssemblyAboutInfoProvider.CreateFallback();

        return new SettingsPanelProjection(
            ShortcutSettingProjection.PendingRegistration("Fullscreen shortcut", settingsProvider.FullscreenShortcut),
            ShortcutSettingProjection.PendingRegistration("Region shortcut", settingsProvider.RegionShortcut),
            settingsProvider.HdrAlertsEnabled,
            "Show warnings when HDR is unavailable, degraded, unsupported, or failed.",
            settingsProvider.HdrAlertsEnabled,
            OutputSettingsProjection.ReadOnly(
                settingsProvider.OutputTarget,
                settingsProvider.SavePath,
                settingsProvider.TimestampNaming,
                settingsProvider.CopyAsImage),
            AboutInfoProjection.FromProvider(aboutInfoProvider),
            settingsProvider.TimestampNaming,
            settingsProvider.CopyAsImage,
            MainPanelProjection.Project(sessionState));
    }
}

public sealed record AboutInfoProjection(
    string AppName,
    string Version,
    string Description)
{
    public static AboutInfoProjection FromProvider(IAboutInfoProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        return new AboutInfoProjection(
            Normalize(provider.AppName, "Lumiere"),
            Normalize(provider.Version, "1.0.0"),
            Normalize(provider.Description, "Native Windows HDR-first capture and preview."));
    }

    private static string Normalize(string? value, string fallback)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? fallback : trimmed;
    }
}

public sealed record OutputSettingsProjection(
    string DisplayValue,
    bool IsClipboardSelected,
    bool IsFolderSelected,
    bool IsBothSelected,
    bool IsReadOnly,
    string PendingReason,
    string SavePathDisplayValue,
    string SavePathHelpText,
    bool IsSavePathReadOnly,
    bool TimestampNaming,
    string TimestampHelpText,
    bool IsTimestampReadOnly,
    bool CopyAsImage,
    string CopyAsImageHelpText,
    bool IsCopyAsImageReadOnly,
    string AfterCaptureDisplayValue,
    string AfterCaptureHelpText,
    bool IsAfterCaptureReadOnly,
    string ExportColorDisplayValue,
    string ExportColorHelpText,
    bool IsExportColorReadOnly)
{
    private const string OutputPolicyActiveReason = "Output target policy is active for clipboard, folder, and both targets";

    public static OutputSettingsProjection ReadOnly(
        Lumiere.Graphics.Output.OutputTarget outputTarget,
        string? savePath,
        bool timestampNaming,
        bool copyAsImage)
    {
        var (displayValue, isClipboardSelected, isFolderSelected, isBothSelected) = outputTarget switch
        {
            Lumiere.Graphics.Output.OutputTarget.Folder => ("Folder", false, true, false),
            Lumiere.Graphics.Output.OutputTarget.Both => ("Both", false, false, true),
            _ => ("Clipboard", true, false, false),
        };

        var savePathDisplayValue = string.IsNullOrWhiteSpace(savePath)
            ? "Not configured"
            : savePath.Trim();

        return new OutputSettingsProjection(
            displayValue,
            isClipboardSelected,
            isFolderSelected,
            isBothSelected,
            IsReadOnly: true,
            OutputPolicyActiveReason,
            savePathDisplayValue,
            "Folder output uses the configured save path. Editing the path remains read-only until picker behavior is implemented.",
            IsSavePathReadOnly: true,
            timestampNaming,
            "Timestamp naming is active for folder output and uses invariant safe filenames.",
            IsTimestampReadOnly: true,
            copyAsImage,
            "Copy-as-image controls basic usability; basic clipboard usability does not mean validated HDR preservation.",
            IsCopyAsImageReadOnly: true,
            "Pending",
            "After-capture behavior arrives in Epic 6 after output artifact semantics are defined.",
            IsAfterCaptureReadOnly: true,
            "Not available",
            "Advanced color/export options are unavailable until encoder metadata, conversion policy, target-app assumptions, and Windows validation exist.",
            IsExportColorReadOnly: true);
    }
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
