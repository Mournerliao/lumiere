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

        Assert.True(projection.FullscreenShortcut.IsReadOnly);
        Assert.True(projection.FullscreenShortcut.IsPendingRegistration);
        Assert.Equal("Global registration arrives in Epic 7", projection.FullscreenShortcut.PendingReason);
        Assert.True(projection.RegionShortcut.IsReadOnly);
        Assert.True(projection.RegionShortcut.IsPendingRegistration);
        Assert.Equal("Global registration arrives in Epic 7", projection.RegionShortcut.PendingReason);
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
        Assert.False(projection.OptionalHdrAlertChromeEnabled);
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

        Assert.True(projection.Output.IsSavePathReadOnly);
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

        Assert.True(projection.Output.IsSavePathReadOnly);
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

        Assert.False(projection.Output.IsReadOnly);
        Assert.False(projection.Output.IsCopyAsImageReadOnly);
        Assert.True(projection.Output.IsSavePathReadOnly);
        Assert.True(projection.Output.IsTimestampReadOnly);
        Assert.True(projection.Output.IsAfterCaptureReadOnly);
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
        Assert.All(projection.Output.ExportColorOptions, option => Assert.True(option.IsReadOnly));
        Assert.False(projection.Output.ExportColorOptions[0].IsSelected);
        Assert.False(projection.Output.ExportColorOptions[1].IsSelected);
        Assert.True(projection.Output.ExportColorOptions[2].IsSelected);
        Assert.Contains("pending", projection.Output.ExportColorOptions[0].HelpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pending", projection.Output.ExportColorOptions[1].HelpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("basic PNG", projection.Output.ExportColorOptions[2].HelpText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR-preserving", projection.Output.ExportColorHelpText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR preserving", projection.Output.ExportColorHelpText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR-preserving", projection.Output.ExportColorOptions[2].HelpText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR preserving", projection.Output.ExportColorOptions[2].HelpText, StringComparison.OrdinalIgnoreCase);
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
        Assert.Equal("2.3.4", projection.About.Version);
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

        Assert.Equal("HDR unavailable", projection.MainPanel.TrustLabel);
        Assert.Equal(MainPanelTrustSeverity.Error, projection.MainPanel.TrustSeverity);
        Assert.False(projection.OptionalHdrAlertChromeEnabled);
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
    }

    private sealed class TestAboutInfoProvider : IAboutInfoProvider
    {
        public string AppName { get; init; } = "Lumiere";

        public string Version { get; init; } = "1.0.0";

        public string Description { get; init; } = "Native Windows HDR-first capture and preview.";
    }
}
