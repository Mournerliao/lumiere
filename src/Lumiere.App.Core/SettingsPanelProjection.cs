using Lumiere.Capture;
using Lumiere.Graphics.Output;
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
        var hotkeyPlan = GlobalHotkeyRegistrationPlan.Project(settingsProvider);

        return new SettingsPanelProjection(
            ShortcutSettingProjection.FromHotkeyBinding(
                hotkeyPlan.Fullscreen),
            ShortcutSettingProjection.FromHotkeyBinding(
                hotkeyPlan.Region),
            settingsProvider.HdrAlertsEnabled,
            "Show warnings when HDR is unavailable, degraded, unsupported, or failed.",
            settingsProvider.HdrAlertsEnabled,
            OutputSettingsProjection.ReadOnly(
                settingsProvider.OutputTarget,
                settingsProvider.SavePath,
                settingsProvider.TimestampNaming,
                settingsProvider.CopyAsImage,
                settingsProvider.AfterCaptureBehavior),
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
    bool IsAfterCaptureSelected,
    bool IsAfterCaptureReadOnly,
    string ExportColorDisplayValue,
    string ExportColorHelpText,
    bool IsExportColorReadOnly,
    IReadOnlyList<ExportColorOptionProjection> ExportColorOptions)
{
    private const string OutputPolicyActiveReason = "Output target policy is active for clipboard, folder, and both targets";
    private const string ExportColorHelp =
        "Export profiles are shown to match the design reference. HDR10 and P3 require encoder metadata, conversion policy, target-app assumptions, and Windows validation before they become real output behavior.";

    public static OutputSettingsProjection ReadOnly(
        Lumiere.Graphics.Output.OutputTarget outputTarget,
        string? savePath,
        bool timestampNaming,
        bool copyAsImage,
        AfterCaptureBehavior afterCaptureBehavior)
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

        var (afterCaptureDisplayValue, afterCaptureHelpText, isAfterCaptureSelected) = ProjectAfterCapture(
            outputTarget,
            afterCaptureBehavior);

        return new OutputSettingsProjection(
            displayValue,
            isClipboardSelected,
            isFolderSelected,
            isBothSelected,
            IsReadOnly: false,
            OutputPolicyActiveReason,
            savePathDisplayValue,
            "Folder output uses the configured save path. Editing the path remains read-only until picker behavior is implemented.",
            IsSavePathReadOnly: true,
            timestampNaming,
            "Timestamp naming is active for folder output and uses invariant safe filenames.",
            IsTimestampReadOnly: true,
            copyAsImage,
            "Copy-as-image controls basic usability; basic clipboard usability does not mean validated HDR preservation.",
            IsCopyAsImageReadOnly: false,
            afterCaptureDisplayValue,
            afterCaptureHelpText,
            isAfterCaptureSelected,
            IsAfterCaptureReadOnly: true,
            "sRGB",
            ExportColorHelp,
            IsExportColorReadOnly: true,
            CreateExportColorOptions());
    }

    private static IReadOnlyList<ExportColorOptionProjection> CreateExportColorOptions() =>
    [
        new(
            "HDR10",
            IsSelected: false,
            IsReadOnly: true,
            "HDR10 export is pending encoder metadata, HDR metadata policy, target-app compatibility, and Windows validation."),
        new(
            "P3",
            IsSelected: false,
            IsReadOnly: true,
            "P3 export is pending color metadata, conversion policy, target-app compatibility, and Windows validation."),
        new(
            "sRGB",
            IsSelected: true,
            IsReadOnly: true,
            "sRGB reflects the current basic PNG output surface; advanced fidelity validation is pending."),
    ];

    private static (string DisplayValue, string HelpText, bool IsSelected) ProjectAfterCapture(
        Lumiere.Graphics.Output.OutputTarget outputTarget,
        AfterCaptureBehavior afterCaptureBehavior)
    {
        if (outputTarget == Lumiere.Graphics.Output.OutputTarget.Clipboard)
        {
            return (
                "None",
                "Clipboard-only output has no file artifact, so open or reveal after capture is skipped.",
                IsSelected: false);
        }

        return afterCaptureBehavior switch
        {
            AfterCaptureBehavior.Open => (
                "Open saved file",
                "After folder output creates a file artifact, Lumiere opens the saved PNG through Windows.",
                IsSelected: true),
            AfterCaptureBehavior.Reveal => (
                "Reveal saved file",
                "After folder output creates a file artifact, Lumiere reveals the saved PNG in Explorer.",
                IsSelected: true),
            _ => (
                "None",
                "After-capture behavior is off; folder output completes through normal feedback.",
                IsSelected: false),
        };
    }
}

public sealed record ExportColorOptionProjection(
    string Label,
    bool IsSelected,
    bool IsReadOnly,
    string HelpText);

public sealed record ShortcutSettingProjection(
    string Label,
    string DisplayValue,
    bool IsReadOnly,
    bool IsPendingRegistration,
    string PendingReason,
    string HelpText)
{
    public static ShortcutSettingProjection FromHotkeyBinding(HotkeyBindingProjection binding)
    {
        ArgumentNullException.ThrowIfNull(binding);

        var registrationState = binding.CanRegister
            ? "Global registration is active when Windows accepts the shortcut."
            : binding.StatusMessage;
        return new ShortcutSettingProjection(
            binding.Label,
            binding.DisplayValue,
            IsReadOnly: true,
            IsPendingRegistration: !binding.CanRegister,
            registrationState,
            $"{binding.Label} is currently {binding.DisplayValue}. {registrationState}");
    }
}
