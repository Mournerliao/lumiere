using Lumiere.App;
using Lumiere.Capture;
using Lumiere.Graphics.Hdr;
using Lumiere.Graphics.Output;
using Lumiere.Settings;
using Windows.Graphics;
using Xunit;

namespace Lumiere.Graphics.Tests.App;

public sealed class SettingsPanelProjectionTests
{
    [Fact]
    public void Project_UsesNotAssignedFallbackForShortcutRows()
    {
        var projection = SettingsPanelProjection.Project(new TestSettingsProvider(), CreateState());

        Assert.Equal("Not assigned", projection.FullscreenShortcut.DisplayValue);
        Assert.Equal("Not assigned", projection.RegionShortcut.DisplayValue);
    }

    [Fact]
    public void Project_UsesConfiguredShortcutValuesSeparately()
    {
        var settings = new TestSettingsProvider
        {
            FullscreenShortcut = " Ctrl+Shift+F ",
            RegionShortcut = "Ctrl+Shift+R",
        };

        var projection = SettingsPanelProjection.Project(settings, CreateState());

        Assert.Equal("Ctrl+Shift+F", projection.FullscreenShortcut.DisplayValue);
        Assert.Equal("Ctrl+Shift+R", projection.RegionShortcut.DisplayValue);
    }

    [Fact]
    public void Project_MarksShortcutRowsReadOnlyAndPendingRegistration()
    {
        var projection = SettingsPanelProjection.Project(new TestSettingsProvider(), CreateState());

        Assert.False(projection.FullscreenShortcut.IsReadOnly);
        Assert.True(projection.FullscreenShortcut.IsPendingRegistration);
        Assert.Contains("skipped", projection.FullscreenShortcut.PendingReason, StringComparison.OrdinalIgnoreCase);
        Assert.False(projection.RegionShortcut.IsReadOnly);
        Assert.True(projection.RegionShortcut.IsPendingRegistration);
        Assert.Contains("skipped", projection.RegionShortcut.PendingReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Project_ValidShortcutRowsShowActiveRegistrationState()
    {
        var settings = new TestSettingsProvider
        {
            FullscreenShortcut = "Ctrl+Shift+F",
            RegionShortcut = "Alt+F12",
        };

        var projection = SettingsPanelProjection.Project(settings, CreateState());

        Assert.False(projection.FullscreenShortcut.IsPendingRegistration);
        Assert.Equal("Ctrl+Shift+F", projection.FullscreenShortcut.DisplayValue);
        Assert.Contains("active", projection.FullscreenShortcut.PendingReason, StringComparison.OrdinalIgnoreCase);
        Assert.False(projection.RegionShortcut.IsPendingRegistration);
        Assert.Equal("Alt+F12", projection.RegionShortcut.DisplayValue);
    }

    [Fact]
    public void Project_ReflectsHdrAlertPreference()
    {
        var settings = new TestSettingsProvider
        {
            HdrAlertsEnabled = false,
        };

        var projection = SettingsPanelProjection.Project(settings, CreateState());

        Assert.False(projection.HdrAlertsEnabled);
    }

    [Fact]
    public void Project_ShowsTargetAwareStateAsRequiredForPublicRelease()
    {
        var projection = SettingsPanelProjection.Project(new TestSettingsProvider(), CreateState());

        Assert.Equal("Required", projection.TargetAwareStateLabel);
        Assert.Contains("selected target", projection.TargetAwareStateHelpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("global HDR guess", projection.TargetAwareStateHelpText, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(OutputTarget.Clipboard, "Clipboard", true, false, false)]
    [InlineData(OutputTarget.Folder, "Folder", false, true, false)]
    [InlineData(OutputTarget.Both, "Both", false, false, true)]
    public void Project_ReflectsReadOnlyOutputTargetDisplay(
        OutputTarget target,
        string displayValue,
        bool clipboardSelected,
        bool folderSelected,
        bool bothSelected)
    {
        var settings = new TestSettingsProvider
        {
            OutputTarget = target,
        };

        var projection = SettingsPanelProjection.Project(settings, CreateState());

        Assert.False(projection.Output.IsReadOnly);
        Assert.Equal(displayValue, projection.Output.DisplayValue);
        Assert.Equal(clipboardSelected, projection.Output.IsClipboardSelected);
        Assert.Equal(folderSelected, projection.Output.IsFolderSelected);
        Assert.Equal(bothSelected, projection.Output.IsBothSelected);
    }

    [Fact]
    public void Project_ReflectsReadOnlyTimestampAndCopyAsImageDisplayState()
    {
        var settings = new TestSettingsProvider
        {
            TimestampNaming = false,
            CopyAsImage = false,
        };

        var projection = SettingsPanelProjection.Project(settings, CreateState());

        Assert.False(projection.TimestampNaming);
        Assert.False(projection.CopyAsImage);
    }

    [Fact]
    public void Project_UsesNotConfiguredFallbackForNullSavePath()
    {
        var settings = new TestSettingsProvider
        {
            SavePath = null,
        };

        var projection = SettingsPanelProjection.Project(settings, CreateState());

        Assert.False(projection.Output.IsSavePathReadOnly);
        Assert.Equal("Not configured", projection.Output.SavePathDisplayValue);
        Assert.Contains("Folder output uses", projection.Output.SavePathHelpText);
        Assert.Contains("read-only", projection.Output.SavePathHelpText);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Project_UsesNotConfiguredFallbackForBlankSavePath(string savePath)
    {
        var settings = new TestSettingsProvider
        {
            SavePath = savePath,
        };

        var projection = SettingsPanelProjection.Project(settings, CreateState());

        Assert.False(projection.Output.IsSavePathReadOnly);
        Assert.Equal("Not configured", projection.Output.SavePathDisplayValue);
        Assert.Contains("Folder output uses", projection.Output.SavePathHelpText);
        Assert.Contains("read-only", projection.Output.SavePathHelpText);
    }

    [Fact]
    public void Project_UsesTrimmedConfiguredSavePathDisplay()
    {
        var settings = new TestSettingsProvider
        {
            SavePath = "  D:\\Screenshots\\HDR Captures  ",
        };

        var projection = SettingsPanelProjection.Project(settings, CreateState());

        Assert.Equal("D:\\Screenshots\\HDR Captures", projection.Output.SavePathDisplayValue);
    }

    [Fact]
    public void Project_EnablesSupportedOutputPreferencesAndKeepsUnsupportedPreferencesReadOnly()
    {
        var projection = SettingsPanelProjection.Project(new TestSettingsProvider(), CreateState());

        Assert.False(projection.IsHdrAlertsReadOnly);
        Assert.False(projection.Output.IsReadOnly);
        Assert.False(projection.Output.IsCopyAsImageReadOnly);
        Assert.False(projection.Output.IsSavePathReadOnly);
        Assert.False(projection.Output.IsTimestampReadOnly);
        Assert.False(projection.Output.IsAfterCaptureReadOnly);
        Assert.True(projection.Output.IsExportColorReadOnly);
        Assert.Contains("Output target policy is active", projection.Output.PendingReason);
        Assert.Contains("clipboard", projection.Output.PendingReason);
        Assert.Contains("folder", projection.Output.PendingReason);
    }

    [Fact]
    public void Project_ReflectsTimestampAndCopyAsImageDefaultsInOutputProjection()
    {
        var settings = new TestSettingsProvider
        {
            TimestampNaming = false,
            CopyAsImage = false,
        };

        var projection = SettingsPanelProjection.Project(settings, CreateState());

        Assert.False(projection.Output.TimestampNaming);
        Assert.False(projection.Output.CopyAsImage);
        Assert.Contains("active", projection.Output.TimestampHelpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("invariant", projection.Output.TimestampHelpText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Project_CopyAsImageHelpTextDoesNotClaimHdrPreservation()
    {
        var projection = SettingsPanelProjection.Project(new TestSettingsProvider(), CreateState());

        Assert.Contains("basic usability", projection.Output.CopyAsImageHelpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("basic clipboard usability", projection.Output.CopyAsImageHelpText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR-preserving", projection.Output.CopyAsImageHelpText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR preserving", projection.Output.CopyAsImageHelpText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Project_RestoresExportProfileSegmentsAndScopesAdvancedProfiles()
    {
        var projection = SettingsPanelProjection.Project(new TestSettingsProvider(), CreateState());

        Assert.Equal("None", projection.Output.AfterCaptureDisplayValue);
        Assert.Equal("sRGB", projection.Output.ExportColorDisplayValue);
        Assert.Contains("no file artifact", projection.Output.AfterCaptureHelpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("skipped", projection.Output.AfterCaptureHelpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("design reference", projection.Output.ExportColorHelpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validation", projection.Output.ExportColorHelpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("metadata", projection.Output.ExportColorHelpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("target-app", projection.Output.ExportColorHelpText, StringComparison.OrdinalIgnoreCase);
        Assert.True(projection.Output.IsExportColorReadOnly);

        Assert.Equal(["HDR10", "P3", "sRGB"], projection.Output.ExportColorOptions.Select(option => option.Label).ToArray());
        Assert.Equal("Validate", projection.Output.ExportColorOptions[0].StatusLabel);
        Assert.Equal("Build", projection.Output.ExportColorOptions[1].StatusLabel);
        Assert.Equal("Compat", projection.Output.ExportColorOptions[2].StatusLabel);
        Assert.True(projection.Output.ExportColorOptions[0].IsReadOnly);
        Assert.True(projection.Output.ExportColorOptions[1].IsReadOnly);
        Assert.False(projection.Output.ExportColorOptions[2].IsReadOnly);
        Assert.False(projection.Output.ExportColorOptions[0].IsSelected);
        Assert.False(projection.Output.ExportColorOptions[1].IsSelected);
        Assert.True(projection.Output.ExportColorOptions[2].IsSelected);
        Assert.Contains("profile contract", projection.Output.ExportColorOptions[0].HelpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Windows validation", projection.Output.ExportColorOptions[0].HelpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("visible as intent", projection.Output.ExportColorOptions[1].HelpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Compatibility output", projection.Output.ExportColorOptions[2].HelpText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR-preserving", projection.Output.ExportColorHelpText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR preserving", projection.Output.ExportColorHelpText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR-preserving", projection.Output.ExportColorOptions[2].HelpText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR preserving", projection.Output.ExportColorOptions[2].HelpText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Project_SelectedHdr10ProfileIsVisibleButDoesNotEnableHdrPreservedClaim()
    {
        var settings = new TestSettingsProvider
        {
            ExportColorFormat = "HDR10",
        };

        var projection = SettingsPanelProjection.Project(settings, CreateState());

        Assert.Equal("HDR10", projection.Output.ExportColorDisplayValue);
        Assert.True(projection.Output.ExportColorOptions[0].IsSelected);
        Assert.True(projection.Output.ExportColorOptions[0].IsReadOnly);
        Assert.Equal(FidelityClaimKind.Unvalidated, projection.MainPanel.FidelityClaim.Kind);
        Assert.Equal("Unvalidated", projection.MainPanel.FidelityClaim.Label);
        Assert.DoesNotContain("HDR-preserved", projection.MainPanel.FidelityClaim.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Project_SelectedHdr10ProfileSurfacesOutputContractPolicy()
    {
        var settings = new TestSettingsProvider
        {
            ExportColorFormat = "HDR10",
        };

        var projection = SettingsPanelProjection.Project(settings, CreateState());

        Assert.Equal("FP16/scRGB capture source", projection.Output.SelectedProfileContract.SourcePolicy);
        Assert.Equal("HDR10 output contract pending implementation", projection.Output.SelectedProfileContract.DestinationPolicy);
        Assert.Contains("tone", projection.Output.SelectedProfileContract.ConversionPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("metadata", projection.Output.SelectedProfileContract.MetadataPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("target-app", projection.Output.SelectedProfileContract.ViewerCompatibilityPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR-preserved", projection.Output.SelectedProfileContract.MetadataPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR-preserved", projection.Output.SelectedProfileContract.ViewerCompatibilityPolicy, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Project_SelectedSrgbProfileSurfacesCompatibilityContractPolicy()
    {
        var projection = SettingsPanelProjection.Project(new TestSettingsProvider(), CreateState());

        Assert.Equal("FP16/scRGB capture source", projection.Output.SelectedProfileContract.SourcePolicy);
        Assert.Equal("Compatibility-converted sRGB artifact", projection.Output.SelectedProfileContract.DestinationPolicy);
        Assert.Contains("converted", projection.Output.SelectedProfileContract.ConversionPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No HDR metadata", projection.Output.SelectedProfileContract.MetadataPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("compatibility", projection.Output.SelectedProfileContract.ViewerCompatibilityPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR-preserved", projection.Output.SelectedProfileContract.DestinationPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR-preserved", projection.Output.SelectedProfileContract.MetadataPolicy, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Project_IncludesValidationEvidencePanel()
    {
        var projection = SettingsPanelProjection.Project(new TestSettingsProvider(), CreateState());

        Assert.Equal(PerfectHdrFidelityProjection.ReleaseTarget, projection.Validation.ReleaseTarget);
        Assert.Contains(projection.Validation.Rows, row => row.Label == "Target-aware HDR");
        Assert.Contains(projection.Validation.Rows, row => row.Label == "HDR-preserved profile");
        Assert.Contains("Named viewers", projection.Validation.ViewerMatrixSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(projection.Validation.ViewerMatrix, viewer => viewer.Name == "Microsoft Paint");
        Assert.Contains(projection.Validation.ViewerMatrix, viewer => viewer.Name == "Windows Photos");
        Assert.Contains(projection.Validation.ViewerMatrix, viewer => viewer.Name == "Chromium browsers");
    }

    [Fact]
    public void Project_ValidationRecordUsesAboutVersionAndReleaseChecklist()
    {
        var aboutInfo = new TestAboutInfoProvider
        {
            Version = "2.3.4+abcdef",
        };

        var projection = SettingsPanelProjection.Project(new TestSettingsProvider(), CreateState(), aboutInfo);

        Assert.Equal("Build v2.3.4", projection.Validation.Record.BuildLabel);
        Assert.Equal(ValidationEvidenceStatus.Limited, projection.Validation.Record.AutomatedEvidenceStatus);
        Assert.Equal(ValidationEvidenceStatus.NotRun, projection.Validation.Record.WindowsManualValidationStatus);
        Assert.Contains("Windows CI", projection.Validation.Record.AutomatedEvidenceDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("manual validation", projection.Validation.Record.WindowsManualValidationDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("docs/validation/release-validation-checklist.md", projection.Validation.Record.EvidenceDocumentPath);
    }

    [Fact]
    public void Project_ReflectsAfterCaptureRevealForFolderArtifacts()
    {
        var settings = new TestSettingsProvider
        {
            OutputTarget = OutputTarget.Folder,
            AfterCaptureBehavior = AfterCaptureBehavior.Reveal,
        };

        var projection = SettingsPanelProjection.Project(settings, CreateState());

        Assert.Equal("Reveal saved file", projection.Output.AfterCaptureDisplayValue);
        Assert.True(projection.Output.IsAfterCaptureSelected);
        Assert.Contains("Explorer", projection.Output.AfterCaptureHelpText);
    }

    [Fact]
    public void Project_ClipboardOnlyExplainsAfterCaptureSkip()
    {
        var settings = new TestSettingsProvider
        {
            OutputTarget = OutputTarget.Clipboard,
            AfterCaptureBehavior = AfterCaptureBehavior.Open,
        };

        var projection = SettingsPanelProjection.Project(settings, CreateState());

        Assert.Equal("None", projection.Output.AfterCaptureDisplayValue);
        Assert.False(projection.Output.IsAfterCaptureSelected);
        Assert.Contains("Clipboard-only", projection.Output.AfterCaptureHelpText);
        Assert.Contains("skipped", projection.Output.AfterCaptureHelpText);
    }

    [Fact]
    public void Project_UsesAboutInfoProviderValues()
    {
        var aboutInfo = new TestAboutInfoProvider
        {
            AppName = "Lumiere Preview",
            Version = "2.3.4",
            Description = "Native Windows HDR-first capture and preview.",
        };

        var projection = SettingsPanelProjection.Project(new TestSettingsProvider(), CreateState(), aboutInfo);

        Assert.Equal("Lumiere Preview", projection.About.AppName);
        Assert.Equal("v2.3.4", projection.About.Version);
        Assert.Equal("Native Windows HDR-first capture and preview.", projection.About.Description);
    }

    [Fact]
    public void Project_AboutDescriptionAvoidsHdrPreservingOutputClaim()
    {
        var projection = SettingsPanelProjection.Project(new TestSettingsProvider(), CreateState());

        Assert.Contains("HDR-first", projection.About.Description, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR-preserving", projection.About.Description, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR preserving", projection.About.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Project_DisabledHdrAlertsPreserveTypedTrustProjection()
    {
        var settings = new TestSettingsProvider
        {
            HdrAlertsEnabled = false,
        };
        var state = CaptureSessionState.Failed(
            CreateTarget(),
            PreviewReadinessStatus.Failed(
                PreviewReadinessStage.Presentation,
                "Preview failed",
                "Presentation failure remains diagnostic state."));

        var projection = SettingsPanelProjection.Project(settings, state);

        Assert.Equal("Preview failed", projection.MainPanel.TrustLabel);
        Assert.Equal(MainPanelTrustSeverity.Error, projection.MainPanel.TrustSeverity);
    }

    private static CaptureSessionState CreateState() =>
        CaptureSessionState.Capturing(
            CreateTarget(),
            PreviewReadinessStatus.Ready("HDR-ready", "Test readiness."));

    private static CaptureTarget CreateTarget() =>
        CaptureTarget.CreateForTest(
            new SizeInt32
            {
                Width = 1920,
                Height = 1080,
            },
            "Test Display",
            CaptureTargetKind.Display);

    private sealed class TestSettingsProvider : ISettingsProvider
    {
        public OutputTarget OutputTarget { get; init; } = OutputTarget.Clipboard;

        public string? SavePath { get; init; }

        public bool TimestampNaming { get; init; } = true;

        public bool CopyAsImage { get; init; } = true;

        public bool HdrAlertsEnabled { get; init; } = true;

        public string FullscreenShortcut { get; init; } = string.Empty;

        public string RegionShortcut { get; init; } = string.Empty;

        public AfterCaptureBehavior AfterCaptureBehavior { get; init; } = AfterCaptureBehavior.None;

        public string ExportColorFormat { get; init; } = "sRGB";
    }

    private sealed class TestAboutInfoProvider : IAboutInfoProvider
    {
        public string AppName { get; init; } = "Lumiere";

        public string Version { get; init; } = "1.0.0";

        public string Description { get; init; } = "Native Windows HDR-first capture and preview.";
    }
}
