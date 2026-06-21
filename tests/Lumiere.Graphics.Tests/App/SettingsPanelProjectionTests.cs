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

    [Fact]
    public void Project_TargetEvidenceScopesDisplayTargetReadiness()
    {
        var projection = SettingsPanelProjection.Project(new TestSettingsProvider(), CreateState());

        Assert.Equal("Display target", projection.TargetEvidence.ScopeLabel);
        Assert.Equal("Test Display", projection.TargetEvidence.TargetLabel);
        Assert.Equal("Presentation", projection.TargetEvidence.ReadinessStageLabel);
        Assert.Contains("selected display", projection.TargetEvidence.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("global HDR guess", projection.TargetEvidence.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Project_TargetEvidenceNamesDesktopBoundsWhenDisplayIdentityCarriesThem()
    {
        var target = CaptureTarget.CreateForTest(
            new SizeInt32
            {
                Width = 3840,
                Height = 2160,
            },
            "HDR Display",
            CaptureTargetKind.Display,
            new DisplayOutputIdentity("\\\\.\\DISPLAY2", left: 3840, top: 0, width: 3840, height: 2160));
        var state = CaptureSessionState.Capturing(
            target,
            PreviewReadinessStatus.Ready("HDR-ready", "Test readiness."));

        var projection = SettingsPanelProjection.Project(new TestSettingsProvider(), state);

        Assert.Equal("Display target", projection.TargetEvidence.ScopeLabel);
        Assert.Contains("desktop bounds", projection.TargetEvidence.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("3840,0", projection.TargetEvidence.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("global HDR guess", projection.TargetEvidence.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Project_TargetEvidenceMarksWindowTargetAsNeedsDisplayMapping()
    {
        var state = CaptureSessionState.Capturing(
            CaptureTarget.CreateForTest(
                new SizeInt32
                {
                    Width = 1280,
                    Height = 720,
                },
                "Test Window",
                CaptureTargetKind.Window),
            PreviewReadinessStatus.Ready("Ready", "Window capture readiness."));

        var projection = SettingsPanelProjection.Project(new TestSettingsProvider(), state);

        Assert.Equal("Window target", projection.TargetEvidence.ScopeLabel);
        Assert.Equal("Test Window", projection.TargetEvidence.TargetLabel);
        Assert.Contains("display mapping", projection.TargetEvidence.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("target-aware", projection.TargetEvidence.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Project_TargetEvidenceMarksIdleStateAsUnresolved()
    {
        var projection = SettingsPanelProjection.Project(new TestSettingsProvider(), CaptureSessionState.Idle());

        Assert.Equal("Target unresolved", projection.TargetEvidence.ScopeLabel);
        Assert.Equal("No active target", projection.TargetEvidence.TargetLabel);
        Assert.Contains("select a target", projection.TargetEvidence.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("global HDR guess", projection.TargetEvidence.Detail, StringComparison.OrdinalIgnoreCase);
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
        Assert.False(projection.Output.IsExportColorReadOnly);
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
        Assert.Contains("planned HDR output paths", projection.Output.ExportColorHelpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validation", projection.Output.ExportColorHelpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("metadata", projection.Output.ExportColorHelpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("target-app", projection.Output.ExportColorHelpText, StringComparison.OrdinalIgnoreCase);
        Assert.False(projection.Output.IsExportColorReadOnly);

        Assert.Equal(["HDR10", "P3", "sRGB"], projection.Output.ExportColorOptions.Select(option => option.Label).ToArray());
        Assert.Equal("Build", projection.Output.ExportColorOptions[0].StatusLabel);
        Assert.Equal("Build", projection.Output.ExportColorOptions[1].StatusLabel);
        Assert.Equal("Compat", projection.Output.ExportColorOptions[2].StatusLabel);
        Assert.True(projection.Output.ExportColorOptions[0].IsReadOnly);
        Assert.True(projection.Output.ExportColorOptions[1].IsReadOnly);
        Assert.False(projection.Output.ExportColorOptions[2].IsReadOnly);
        Assert.False(projection.Output.ExportColorOptions[0].IsSelected);
        Assert.False(projection.Output.ExportColorOptions[1].IsSelected);
        Assert.True(projection.Output.ExportColorOptions[2].IsSelected);
        Assert.False(projection.Output.ExportColorOptions[0].IsInteractive);
        Assert.False(projection.Output.ExportColorOptions[1].IsInteractive);
        Assert.True(projection.Output.ExportColorOptions[2].IsInteractive);
        Assert.Contains("profile contract", projection.Output.ExportColorOptions[0].HelpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Windows validation", projection.Output.ExportColorOptions[0].HelpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("shown for planning", projection.Output.ExportColorOptions[1].HelpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Compatibility output", projection.Output.ExportColorOptions[2].HelpText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not selected and currently unavailable", projection.Output.ExportColorOptions[0].AccessibilityHelpText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("design reference", projection.Output.ExportColorHelpText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("validation-scoped", projection.Output.ExportColorHelpText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("visible as intent", projection.Output.ExportColorOptions[1].HelpText, StringComparison.OrdinalIgnoreCase);
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
        Assert.True(projection.Output.ExportColorOptions[0].IsInteractive);
        Assert.Contains("selected and kept as the current choice for this session", projection.Output.ExportColorOptions[0].AccessibilityHelpText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("validation-scoped", projection.Output.ExportColorOptions[0].AccessibilityHelpText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Build", projection.MainPanel.OutputProfile.StatusLabel);
        Assert.Equal(FidelityClaimKind.Converted, projection.MainPanel.FidelityClaim.Kind);
        Assert.Equal("Converted", projection.MainPanel.FidelityClaim.Label);
        Assert.Contains("compatibility fallback", projection.MainPanel.OutputProfile.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("implementation prerequisites", projection.MainPanel.OutputProfile.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("validation-scoped", projection.MainPanel.OutputProfile.Detail, StringComparison.OrdinalIgnoreCase);
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
    public void Project_AppliesCompleteFormatContractBeforeExecutableHdr10()
    {
        var settings = new TestSettingsProvider
        {
            ExportColorFormat = "HDR10",
        };

        var projection = SettingsPanelProjection.Project(
            settings,
            CreateState(),
            [
                ArtifactWithIncompleteViewerEvidence("Microsoft Paint"),
                ArtifactWithIncompleteViewerEvidence("Windows Photos"),
                ArtifactWithIncompleteViewerEvidence("Chromium browsers"),
            ],
            executionCapabilities: ValidateOnlyHdr10Capabilities(
                [
                    ArtifactWithIncompleteViewerEvidence("Microsoft Paint"),
                    ArtifactWithIncompleteViewerEvidence("Windows Photos"),
                    ArtifactWithIncompleteViewerEvidence("Chromium browsers"),
                ]));

        Assert.All(projection.Validation.ViewerMatrix, viewer => Assert.Equal(ValidationEvidenceStatus.NotRun, viewer.Status));
        Assert.All(projection.Validation.ViewerMatrix, viewer => Assert.Equal(ValidationEvidenceStatus.Pass, viewer.ArtifactHandlingStatus));
        Assert.All(projection.Validation.ViewerMatrix, viewer => Assert.Equal(ValidationEvidenceStatus.Pass, viewer.VisualMatchStatus));
        Assert.All(projection.Validation.ViewerMatrix, viewer => Assert.Equal(ValidationEvidenceStatus.Pass, viewer.HdrPreservationStatus));
        Assert.All(projection.Validation.ViewerMatrix, viewer => Assert.Equal(ValidationEvidenceStatus.NotRun, viewer.Hdr10MetadataStatus));
        Assert.Equal("HDR10", projection.Validation.OutputProfileGate.ProfileLabel);
        Assert.Equal("Validate", projection.Validation.OutputProfileGate.StatusLabel);
        Assert.Equal("HDR10", projection.MainPanel.OutputProfile.Label);
        Assert.Equal("Validate", projection.MainPanel.OutputProfile.StatusLabel);
        Assert.Equal("HDR10 output contract is defined, but this session is still waiting for Windows manual viewer evidence.", projection.Output.SelectedProfileContract.DestinationPolicy);
        Assert.Contains("defined for the HDR10 path", projection.Output.SelectedProfileContract.ConversionPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("viewer evidence is still incomplete", projection.Output.SelectedProfileContract.MetadataPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Windows manual viewer evidence", projection.Output.SelectedProfileContract.ViewerCompatibilityPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(FidelityClaimKind.Converted, projection.MainPanel.FidelityClaim.Kind);
        Assert.Contains("compatibility", projection.MainPanel.FidelityClaim.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("compatibility fallback", projection.MainPanel.OutputProfile.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("viewer evidence", projection.MainPanel.OutputProfile.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR-preserved", projection.MainPanel.FidelityClaim.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Project_KeepsPendingImplementationContractWhenFormatContractEvidenceIsMissing()
    {
        var settings = new TestSettingsProvider
        {
            ExportColorFormat = "HDR10",
        };

        var projection = SettingsPanelProjection.Project(
            settings,
            CreateState(),
            [
                ArtifactFor("Microsoft Paint"),
                ArtifactFor("Windows Photos"),
                ArtifactFor("Chromium browsers"),
            ]);

        Assert.Equal("Build", projection.Validation.OutputProfileGate.StatusLabel);
        Assert.Equal("HDR10 output contract pending implementation", projection.Output.SelectedProfileContract.DestinationPolicy);
        Assert.Contains("tone", projection.Output.SelectedProfileContract.ConversionPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("metadata", projection.Output.SelectedProfileContract.MetadataPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("target-app", projection.Output.SelectedProfileContract.ViewerCompatibilityPolicy, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Project_EnablesHdr10OptionWhenRuntimeCapabilityAndManualEvidencePass()
    {
        var settings = new TestSettingsProvider
        {
            ExportColorFormat = "HDR10",
        };

        var projection = SettingsPanelProjection.Project(
            settings,
            CreateState(),
            [
                ArtifactWithFormatContract("Microsoft Paint"),
                ArtifactWithFormatContract("Windows Photos"),
                ArtifactWithFormatContract("Chromium browsers"),
            ],
            executionCapabilities: OutputProfileExecutionCapabilities.Create(
                OutputProfileExecutionCapability.SrgbCompatibility,
                OutputProfileExecutionCapability.Hdr10PreservedImplementedArtifactEncoder));

        Assert.Equal("HDR10", projection.Output.ExportColorDisplayValue);
        Assert.Equal("Ready", projection.Output.ExportColorOptions[0].StatusLabel);
        Assert.False(projection.Output.ExportColorOptions[0].IsReadOnly);
        Assert.True(projection.Output.ExportColorOptions[0].IsSelected);
        Assert.True(projection.Output.ExportColorOptions[0].IsInteractive);
        Assert.Equal("HDR10", projection.Validation.OutputProfileGate.ProfileLabel);
        Assert.Equal("Ready", projection.Validation.OutputProfileGate.StatusLabel);
        Assert.Equal("Ready", projection.MainPanel.OutputProfile.StatusLabel);
        Assert.Equal("Validated HDR10-preserved artifact contract is active for this session.", projection.Output.SelectedProfileContract.DestinationPolicy);
        Assert.Contains("validated HDR-preserved path", projection.Output.SelectedProfileContract.ConversionPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validated for the active HDR-preserved path", projection.Output.SelectedProfileContract.MetadataPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("compatibility evidence passed", projection.Output.SelectedProfileContract.ViewerCompatibilityPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(FidelityClaimKind.HdrPreserved, projection.MainPanel.FidelityClaim.Kind);
    }

    [Fact]
    public void Project_SurfacesManualFormatContractEvidenceInValidationPanel()
    {
        var settings = new TestSettingsProvider
        {
            ExportColorFormat = "HDR10",
        };

        var projection = SettingsPanelProjection.Project(
            settings,
            CreateState(),
            [
                ArtifactWithFormatContract("Windows Photos"),
            ]);
        var profileRow = Assert.Single(
            projection.Validation.Rows,
            row => row.Label == "HDR-preserved profile");

        Assert.Equal(ValidationEvidenceStatus.Limited, profileRow.Status);
        Assert.Contains("format contract evidence", profileRow.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("PQ ST.2084", projection.Output.SelectedProfileContract.TransferFunctionLabel);
        Assert.Equal("Attach HDR10 static metadata", projection.Output.SelectedProfileContract.MetadataPolicyLabel);
        Assert.Equal(FidelityClaimKind.Converted, projection.MainPanel.FidelityClaim.Kind);
    }

    [Fact]
    public void Project_SurfacesPassedVisualMatchEvidenceInValidationPanel()
    {
        var projection = SettingsPanelProjection.Project(
            new TestSettingsProvider(),
            CreateState(),
            [
                SdrArtifactFor("Microsoft Paint"),
                SdrArtifactFor("Windows Photos"),
                SdrArtifactFor("Chromium browsers"),
            ]);
        var visualRow = Assert.Single(
            projection.Validation.Rows,
            row => row.Label == "Visual-match output");

        Assert.Equal(ValidationEvidenceStatus.Pass, visualRow.Status);
        Assert.Contains("visual-match evidence passed", visualRow.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.All(
            projection.Validation.ViewerMatrix,
            viewer =>
            {
                Assert.Equal(ValidationEvidenceStatus.Pass, viewer.ArtifactHandlingStatus);
                Assert.Equal(ValidationEvidenceStatus.Pass, viewer.VisualMatchStatus);
            });
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
        Assert.Equal("sRGB", projection.Validation.OutputProfileGate.ProfileLabel);
        Assert.Equal("Compat", projection.Validation.OutputProfileGate.StatusLabel);
        Assert.Contains(projection.Validation.Rows, row => row.Label == "Target-aware HDR");
        Assert.Contains(projection.Validation.Rows, row => row.Label == "Visual-match output" && row.Status == ValidationEvidenceStatus.NotRun);
        Assert.Contains(projection.Validation.Rows, row => row.Label == "HDR-preserved profile");
        Assert.Contains("Named viewers", projection.Validation.ViewerMatrixSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(projection.Validation.ViewerMatrix, viewer => viewer.Name == "Microsoft Paint");
        Assert.Contains(projection.Validation.ViewerMatrix, viewer => viewer.Name == "Windows Photos");
        Assert.Contains(projection.Validation.ViewerMatrix, viewer => viewer.Name == "Chromium browsers");
    }

    [Fact]
    public void Project_ValidationTargetAwareRowNamesUnresolvedDisplayMappingBlocker()
    {
        var state = CaptureSessionState.Degraded(
            CreateTarget(),
            PreviewReadinessStatus.Degraded(
                PreviewReadinessStage.Presentation,
                "HDR readiness is unvalidated for the selected capture target.",
                "Target-aware display capability could not be matched to a DXGI output (match=NotMatched).",
                PreviewReadinessReason.TargetDisplayUnresolved));

        var projection = SettingsPanelProjection.Project(new TestSettingsProvider(), state);
        var targetAwareRow = Assert.Single(
            projection.Validation.Rows,
            row => row.Label == "Target-aware HDR");

        Assert.Equal(ValidationEvidenceStatus.NotRun, targetAwareRow.Status);
        Assert.Contains("selected capture target", targetAwareRow.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DXGI output", targetAwareRow.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("NotMatched", targetAwareRow.Detail, StringComparison.Ordinal);
        Assert.Contains("mixed HDR/SDR", targetAwareRow.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Enable HDR", targetAwareRow.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Project_ValidationTargetAwareRowSurfacesMatchedOutputEvidenceAsLimited()
    {
        var state = CaptureSessionState.Capturing(
            CreateTarget(),
            PreviewReadinessStatus.Initializing(
                PreviewReadinessStage.Presentation,
                "Preview presentation is configured for HDR; live capture still needs validation.",
                "IDXGISwapChain3.CheckColorSpaceSupport returned Present; IDXGISwapChain3.SetColorSpace1 set RgbFullG10NoneP709; display match=DesktopBounds."));

        var projection = SettingsPanelProjection.Project(new TestSettingsProvider(), state);
        var targetAwareRow = Assert.Single(
            projection.Validation.Rows,
            row => row.Label == "Target-aware HDR");

        Assert.Equal(ValidationEvidenceStatus.Limited, targetAwareRow.Status);
        Assert.Contains("DesktopBounds", targetAwareRow.Detail, StringComparison.Ordinal);
        Assert.Contains("Windows manual validation", targetAwareRow.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR Ready", targetAwareRow.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Project_ValidationTargetAwareRowSurfacesArtifactTargetHdrEvidence()
    {
        var projection = SettingsPanelProjection.Project(
            new TestSettingsProvider(),
            CreateState(),
            [
                SdrArtifactFor("Microsoft Paint") with
                {
                    TargetHdrEvidence = CompleteTargetHdrEvidence,
                },
            ]);
        var targetAwareRow = Assert.Single(
            projection.Validation.Rows,
            row => row.Label == "Target-aware HDR");

        Assert.Equal(ValidationEvidenceStatus.Limited, targetAwareRow.Status);
        Assert.Contains("DesktopBounds", targetAwareRow.Detail, StringComparison.Ordinal);
        Assert.Contains("artifact", targetAwareRow.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Windows manual validation", targetAwareRow.Detail, StringComparison.OrdinalIgnoreCase);
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
        Assert.Contains("harness/validation/release-validation-checklist.md", projection.Validation.Record.EvidenceDocumentPath);
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

    private static OutputViewerCompatibilityEvidence PassingHdrViewer(string name) =>
        new(
            name,
            OutputCompatibilityEvidenceStatus.Pass,
            OutputCompatibilityEvidenceStatus.Pass,
            OutputCompatibilityEvidenceStatus.Pass,
            "Validated HDR viewer.")
        {
            Hdr10MetadataStatus = OutputCompatibilityEvidenceStatus.Pass,
        };

    private static OutputValidationSessionArtifact ArtifactFor(string viewerName) =>
        new(
            Date: "2026-06-21",
            Tester: "QA",
            BuildCommit: "72c3be7",
            WindowsVersion: "Windows 11 24H2",
            Device: "HDR workstation",
            Gpu: "Test GPU",
            DisplaySetup: "HDR primary",
            HdrState: "HDR enabled",
            DpiScales: ["150%"],
            EntryPointsTested: ["Settings panel"],
            OutputTargetsTested: ["Clipboard"],
            TargetAppsTested: [viewerName],
            ChecklistIdsCovered: ["REL-OUT-01"],
            ResultSummary: $"{viewerName} HDR validation passed.",
            EvidencePaths: [$"docs/validation/evidence/{viewerName}.md"],
            KnownLimitations: [],
            FollowUpIssuesOrStories: [],
            OutputProfileRecords:
            [
                new(
                    OutputProfileKind.Hdr10Pq,
                    [
                        PassingHdrViewer(viewerName),
                    ]),
            ])
        {
            TargetHdrEvidence = CompleteTargetHdrEvidence,
        };

    private static OutputValidationSessionArtifact ArtifactWithFormatContract(string viewerName) =>
        new(
            Date: "2026-06-21",
            Tester: "QA",
            BuildCommit: "72c3be7",
            WindowsVersion: "Windows 11 24H2",
            Device: "HDR workstation",
            Gpu: "Test GPU",
            DisplaySetup: "HDR primary",
            HdrState: "HDR enabled",
            DpiScales: ["150%"],
            EntryPointsTested: ["Settings panel"],
            OutputTargetsTested: ["Clipboard"],
            TargetAppsTested: [viewerName],
            ChecklistIdsCovered: ["REL-OUT-01"],
            ResultSummary: $"{viewerName} HDR validation passed.",
            EvidencePaths: [$"docs/validation/evidence/{viewerName}.md"],
            KnownLimitations: [],
            FollowUpIssuesOrStories: [],
            OutputProfileRecords:
            [
                new(
                    OutputProfileKind.Hdr10Pq,
                    [
                        PassingHdrViewer(viewerName),
                    ])
                {
                    FormatContract = CompleteHdr10Contract,
                },
            ])
        {
            TargetHdrEvidence = CompleteTargetHdrEvidence,
        };

    private static OutputValidationSessionArtifact ArtifactWithIncompleteViewerEvidence(string viewerName) =>
        new(
            Date: "2026-06-21",
            Tester: "QA",
            BuildCommit: "72c3be7",
            WindowsVersion: "Windows 11 24H2",
            Device: "HDR workstation",
            Gpu: "Test GPU",
            DisplaySetup: "HDR primary",
            HdrState: "HDR enabled",
            DpiScales: ["150%"],
            EntryPointsTested: ["Settings panel"],
            OutputTargetsTested: ["Clipboard"],
            TargetAppsTested: [viewerName],
            ChecklistIdsCovered: ["REL-OUT-01"],
            ResultSummary: $"{viewerName} HDR validation is incomplete.",
            EvidencePaths: [$"docs/validation/evidence/{viewerName}.md"],
            KnownLimitations: ["Viewer evidence still incomplete."],
            FollowUpIssuesOrStories: ["11-3"],
            OutputProfileRecords:
            [
                new(
                    OutputProfileKind.Hdr10Pq,
                    [
                        PassingHdrViewer(viewerName) with
                        {
                            Hdr10MetadataStatus = OutputCompatibilityEvidenceStatus.NotRun,
                        },
                    ])
                {
                    FormatContract = CompleteHdr10Contract,
                },
            ])
        {
            TargetHdrEvidence = CompleteTargetHdrEvidence,
        };

    private static OutputValidationSessionArtifact SdrArtifactFor(string viewerName) =>
        new(
            Date: "2026-06-21",
            Tester: "QA",
            BuildCommit: "72c3be7",
            WindowsVersion: "Windows 11 24H2",
            Device: "HDR workstation",
            Gpu: "Test GPU",
            DisplaySetup: "HDR primary",
            HdrState: "HDR enabled",
            DpiScales: ["150%"],
            EntryPointsTested: ["Settings panel"],
            OutputTargetsTested: ["Clipboard"],
            TargetAppsTested: [viewerName],
            ChecklistIdsCovered: ["REL-OUT-01"],
            ResultSummary: $"{viewerName} visual-match validation passed.",
            EvidencePaths: [$"docs/validation/evidence/{viewerName}.md"],
            KnownLimitations: [],
            FollowUpIssuesOrStories: [],
            OutputProfileRecords:
            [
                new(
                    OutputProfileKind.SrgbCompatibilityPng,
                    [
                        new(
                            viewerName,
                            OutputCompatibilityEvidenceStatus.Pass,
                            OutputCompatibilityEvidenceStatus.Pass,
                            OutputCompatibilityEvidenceStatus.NotApplicable,
                            "Validated SDR visual-match viewer."),
                    ]),
            ])
        {
            TargetHdrEvidence = CompleteTargetHdrEvidence,
        };

    private static OutputFormatContract CompleteHdr10Contract { get; } =
        new(
            OutputPixelFormat.R16G16B16A16Float,
            OutputPixelFormat.R16G16B16A16Float,
            OutputTransferFunction.PqSt2084,
            OutputColorPrimaries.Bt2020,
            OutputConversionPolicy.PreserveHdrWithDefinedToneMapping,
            OutputMetadataPolicy.AttachHdr10StaticMetadata,
            OutputTargetAppAssumption.RequiresHdrViewerValidation,
            Hdr10StaticMetadataPolicy.Bt2020PqReference1000Nit);

    private static TargetAwareHdrValidationEvidence CompleteTargetHdrEvidence { get; } =
        new(
            TargetDisplayName: "HDR primary",
            Left: 0,
            Top: 0,
            Width: 3840,
            Height: 2160,
            MatchKind: "DesktopBounds",
            HdrState: "Active",
            ColorSpace: "RgbFullG2084NoneP2020",
            Detail: "Validated target-aware HDR match evidence.");

    private static OutputProfileExecutionCapabilities ValidateOnlyHdr10Capabilities(
        IEnumerable<OutputValidationSessionArtifact> artifacts) =>
        OutputProfileExecutionCapabilities.ResolveHdr10JxrReleaseCapabilities(
            ReadyHdr10JxrReadiness,
            artifacts);

    private static Hdr10JxrCodecReadiness ReadyHdr10JxrReadiness { get; } =
        new(
            HasNativeWicJpegXrEncoder: true,
            AcceptsRgba16FloatSource: true,
            WritesAuditMetadata: true,
            HasArtifactAuditMetadataRoundTripEvidence: true,
            HasViewerRecognizedHdr10StaticMetadata: true,
            Hdr10StaticMetadataPolicy: Hdr10StaticMetadataPolicy.Bt2020PqReference1000Nit,
            HasWindowsManualViewerValidation: true,
            Blockers: []);

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
