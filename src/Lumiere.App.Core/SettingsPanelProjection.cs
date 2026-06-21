using Lumiere.Capture;
using Lumiere.Graphics.Output;
using Lumiere.Settings;

namespace Lumiere.App;

public sealed record SettingsPanelProjection(
    ShortcutSettingProjection FullscreenShortcut,
    ShortcutSettingProjection RegionShortcut,
    bool HdrAlertsEnabled,
    bool IsHdrAlertsReadOnly,
    string HdrAlertsHelpText,
    string TargetAwareStateLabel,
    string TargetAwareStateHelpText,
    TargetEvidenceProjection TargetEvidence,
    OutputSettingsProjection Output,
    ValidationPanelProjection Validation,
    AboutInfoProjection About,
    bool TimestampNaming,
    bool CopyAsImage,
    MainPanelProjection MainPanel)
{
    public static SettingsPanelProjection Project(
        ISettingsProvider settingsProvider,
        CaptureSessionState sessionState,
        IAboutInfoProvider? aboutInfoProvider = null,
        OutputProfileExecutionCapabilities? executionCapabilities = null)
    {
        return Project(
            settingsProvider,
            sessionState,
            Array.Empty<OutputValidationSessionArtifact>(),
            aboutInfoProvider,
            executionCapabilities);
    }

    public static SettingsPanelProjection Project(
        ISettingsProvider settingsProvider,
        CaptureSessionState sessionState,
        IEnumerable<OutputValidationSessionArtifact> validationArtifacts,
        IAboutInfoProvider? aboutInfoProvider = null,
        OutputProfileExecutionCapabilities? executionCapabilities = null)
    {
        ArgumentNullException.ThrowIfNull(validationArtifacts);
        return ProjectCore(
            settingsProvider,
            sessionState,
            validationArtifacts,
            aboutInfoProvider,
            executionCapabilities,
            validationRecordFactory: PerfectHdrFidelityProjection.ProjectValidationRecord);
    }

    public static SettingsPanelProjection Project(
        ISettingsProvider settingsProvider,
        CaptureSessionState sessionState,
        OutputValidationArtifactSnapshot validationSnapshot,
        IAboutInfoProvider? aboutInfoProvider = null,
        OutputProfileExecutionCapabilities? executionCapabilities = null)
    {
        ArgumentNullException.ThrowIfNull(validationSnapshot);
        return ProjectCore(
            settingsProvider,
            sessionState,
            validationSnapshot.Artifacts,
            aboutInfoProvider,
            executionCapabilities,
            buildVersion => PerfectHdrFidelityProjection.ProjectValidationRecord(buildVersion, validationSnapshot));
    }

    private static SettingsPanelProjection ProjectCore(
        ISettingsProvider settingsProvider,
        CaptureSessionState sessionState,
        IEnumerable<OutputValidationSessionArtifact> validationArtifacts,
        IAboutInfoProvider? aboutInfoProvider,
        OutputProfileExecutionCapabilities? executionCapabilities,
        Func<string?, ValidationRecordProjection> validationRecordFactory)
    {
        ArgumentNullException.ThrowIfNull(settingsProvider);
        ArgumentNullException.ThrowIfNull(sessionState);
        ArgumentNullException.ThrowIfNull(validationArtifacts);
        ArgumentNullException.ThrowIfNull(validationRecordFactory);
        aboutInfoProvider ??= AssemblyAboutInfoProvider.CreateFallback();
        var capabilities = executionCapabilities ?? OutputProfileExecutionCapabilities.CompatibilityOnly;
        var hotkeyPlan = GlobalHotkeyRegistrationPlan.Project(settingsProvider);
        var about = AboutInfoProjection.FromProvider(aboutInfoProvider);
        var artifacts = validationArtifacts.ToArray();
        var selectedProfileContract = OutputProfileContract.FromSettingsValue(settingsProvider.ExportColorFormat);

        return new SettingsPanelProjection(
            ShortcutSettingProjection.FromHotkeyBinding(
                hotkeyPlan.Fullscreen),
            ShortcutSettingProjection.FromHotkeyBinding(
                hotkeyPlan.Region),
            settingsProvider.HdrAlertsEnabled,
            IsHdrAlertsReadOnly: false,
            "Show warnings when HDR is unavailable, degraded, unsupported, or failed.",
            "Required",
            "Public release cannot use a global HDR guess; state must follow the selected target.",
            TargetEvidenceProjection.Project(sessionState),
            OutputSettingsProjection.ReadOnly(
                settingsProvider.OutputTarget,
                settingsProvider.SavePath,
                settingsProvider.TimestampNaming,
                settingsProvider.CopyAsImage,
                settingsProvider.AfterCaptureBehavior,
                settingsProvider.ExportColorFormat,
                artifacts,
                capabilities),
            PerfectHdrFidelityProjection.ProjectValidation(
                selectedProfileContract,
                artifacts,
                capabilities,
                validationRecordFactory(about.Version),
                readiness: sessionState.Readiness,
                outputTarget: settingsProvider.OutputTarget),
            about,
            settingsProvider.TimestampNaming,
            settingsProvider.CopyAsImage,
            MainPanelProjection.Project(
                sessionState,
                hdrAlertsEnabled: settingsProvider.HdrAlertsEnabled,
                exportColorFormat: settingsProvider.ExportColorFormat,
                outputTarget: settingsProvider.OutputTarget,
                validationArtifacts: artifacts,
                executionCapabilities: capabilities));
    }
}

public sealed record TargetEvidenceProjection(
    string ScopeLabel,
    string TargetLabel,
    string ReadinessStageLabel,
    string Detail)
{
    public static TargetEvidenceProjection Project(CaptureSessionState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var stageLabel = state.Readiness.Stage.ToString();
        if (state.Target is null)
        {
            return new TargetEvidenceProjection(
                "Target unresolved",
                "No active target",
                stageLabel,
                "Select a target before public release can replace the global HDR guess with target-aware evidence.");
        }

        var targetLabel = string.IsNullOrWhiteSpace(state.Target.DisplayName)
            ? "Capture target"
            : state.Target.DisplayName;

        var displayDetail = state.Target.DisplayIdentity is { Left: { } left, Top: { } top } identity
            ? $"HDR readiness is scoped to the selected display target, desktop bounds {left},{top} {identity.Width}x{identity.Height}, and its preview path."
            : "HDR readiness is scoped to the selected display target and its preview path.";

        return state.Target.Kind switch
        {
            CaptureTargetKind.Display => new TargetEvidenceProjection(
                "Display target",
                targetLabel,
                stageLabel,
                displayDetail),
            CaptureTargetKind.Window => new TargetEvidenceProjection(
                "Window target",
                targetLabel,
                stageLabel,
                "Window capture still needs display mapping before it can satisfy target-aware HDR evidence."),
            _ => new TargetEvidenceProjection(
                "Target kind unknown",
                targetLabel,
                stageLabel,
                "Capture target kind is unresolved, so target-aware HDR evidence remains incomplete."),
        };
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
            FormatVersion(provider.Version),
            Normalize(provider.Description, "Native Windows HDR-first capture and preview."));
    }

    private static string FormatVersion(string? version)
    {
        var normalized = Normalize(version, "1.0.0");
        var dashIndex = normalized.IndexOf('-');
        var plusIndex = normalized.IndexOf('+');
        var trimmed = normalized;
        if (plusIndex >= 0)
        {
            trimmed = normalized[..plusIndex];
        }
        else if (dashIndex >= 0)
        {
            trimmed = normalized[..dashIndex];
        }

        return trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : $"v{trimmed}";
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
    OutputProfileContractProjection SelectedProfileContract,
    IReadOnlyList<ExportColorOptionProjection> ExportColorOptions)
{
    private const string OutputPolicyActiveReason = "Output target policy is active for clipboard, folder, and both targets";
    private const string ExportColorHelp =
        "HDR10 and P3 stay visible so you can review planned HDR output paths. They need encoder metadata, conversion policy, target-app support, and Windows validation before they become available.";

    public static OutputSettingsProjection ReadOnly(
        Lumiere.Graphics.Output.OutputTarget outputTarget,
        string? savePath,
        bool timestampNaming,
        bool copyAsImage,
        AfterCaptureBehavior afterCaptureBehavior,
        string? exportColorFormat = null,
        IEnumerable<OutputValidationSessionArtifact>? validationArtifacts = null,
        OutputProfileExecutionCapabilities? executionCapabilities = null)
    {
        var capabilities = executionCapabilities ?? OutputProfileExecutionCapabilities.CompatibilityOnly;
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
        var selectedContract = OutputProfileContract.FromSettingsValue(exportColorFormat);
        var selectedProfile = validationArtifacts is null
            ? PerfectHdrFidelityProjection.ProjectOutputProfile(selectedContract, readiness: null, capabilities, outputTarget)
            : PerfectHdrFidelityProjection.ProjectOutputProfile(selectedContract, validationArtifacts, readiness: null, capabilities, outputTarget);
        var exportColorOptions = CreateExportColorOptions(
            selectedProfile.Label,
            outputTarget,
            validationArtifacts,
            capabilities);

        return new OutputSettingsProjection(
            displayValue,
            isClipboardSelected,
            isFolderSelected,
            isBothSelected,
            IsReadOnly: false,
            OutputPolicyActiveReason,
            savePathDisplayValue,
            "Folder output uses the configured save path. Editing the path remains read-only until picker behavior is implemented.",
            IsSavePathReadOnly: false,
            timestampNaming,
            "Timestamp naming is active for folder output and uses invariant safe filenames.",
            IsTimestampReadOnly: false,
            copyAsImage,
            "Copy-as-image controls basic usability; basic clipboard usability does not mean validated HDR preservation.",
            IsCopyAsImageReadOnly: false,
            afterCaptureDisplayValue,
            afterCaptureHelpText,
            isAfterCaptureSelected,
            IsAfterCaptureReadOnly: false,
            selectedProfile.Label,
            ExportColorHelp,
            IsExportColorReadOnly: false,
            selectedProfile.Contract,
            exportColorOptions);
    }

    private static IReadOnlyList<ExportColorOptionProjection> CreateExportColorOptions(
        string selectedLabel,
        Lumiere.Graphics.Output.OutputTarget outputTarget,
        IEnumerable<OutputValidationSessionArtifact>? validationArtifacts,
        OutputProfileExecutionCapabilities capabilities) =>
    [
        CreateExportColorOption(
            ProjectExportColorOptionProfile("HDR10", outputTarget, validationArtifacts, capabilities),
            selectedLabel),
        CreateExportColorOption(
            ProjectExportColorOptionProfile("P3", outputTarget, validationArtifacts, capabilities),
            selectedLabel),
        CreateExportColorOption(
            ProjectExportColorOptionProfile("sRGB", outputTarget, validationArtifacts, capabilities),
            selectedLabel),
    ];

    private static OutputProfileProjection ProjectExportColorOptionProfile(
        string exportColorFormat,
        Lumiere.Graphics.Output.OutputTarget outputTarget,
        IEnumerable<OutputValidationSessionArtifact>? validationArtifacts,
        OutputProfileExecutionCapabilities capabilities)
    {
        var contract = OutputProfileContract.FromSettingsValue(exportColorFormat);
        return validationArtifacts is null
            ? PerfectHdrFidelityProjection.ProjectOutputProfile(contract, readiness: null, capabilities, outputTarget)
            : PerfectHdrFidelityProjection.ProjectOutputProfile(contract, validationArtifacts, readiness: null, capabilities, outputTarget);
    }

    private static ExportColorOptionProjection CreateExportColorOption(
        OutputProfileProjection profile,
        string selectedLabel) =>
        new(
            profile.Label,
            profile.StatusLabel,
            IsSelected: string.Equals(profile.Label, selectedLabel, StringComparison.OrdinalIgnoreCase),
            profile.IsReadOnly,
            profile.Detail);

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
    string StatusLabel,
    bool IsSelected,
    bool IsReadOnly,
    string HelpText)
{
    public bool IsInteractive => IsSelected || !IsReadOnly;

    public string AccessibilityHelpText
    {
        get
        {
            var selectionState = IsSelected ? "selected" : "not selected";
            var availabilityState = IsReadOnly
                ? (IsSelected ? "kept as the current choice for this session" : "currently unavailable")
                : "available";
            return $"{Label} is {selectionState} and {availabilityState}. {HelpText}";
        }
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
    public static ShortcutSettingProjection FromHotkeyBinding(HotkeyBindingProjection binding)
    {
        ArgumentNullException.ThrowIfNull(binding);

        var registrationState = binding.CanRegister
            ? "Global registration is active when Windows accepts the shortcut."
            : binding.StatusMessage;
        return new ShortcutSettingProjection(
            binding.Label,
            binding.DisplayValue,
            IsReadOnly: false,
            IsPendingRegistration: !binding.CanRegister,
            registrationState,
            $"{binding.Label} is currently {binding.DisplayValue}. {registrationState}");
    }
}
