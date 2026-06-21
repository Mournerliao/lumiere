using Lumiere.App;
using Lumiere.Capture;
using Lumiere.Graphics.Hdr;
using Lumiere.Graphics.Output;
using Lumiere.Infrastructure.Interop;
using Lumiere.Settings;
using Windows.Graphics;
using Xunit;

namespace Lumiere.Graphics.Tests.App;

public sealed class TrayMenuProjectionTests
{
    [Fact]
    public void Project_IdleState_ShowsIdentityStatusCommandsAndShortcutLabels()
    {
        var projection = TrayMenuProjection.Project(
            CaptureSessionState.Idle(PreviewReadinessStatus.Ready("Ready", "HDR preview is ready.")),
            new StubSettingsProvider("Ctrl+Shift+F", "Ctrl+Shift+R"),
            new StubAboutInfoProvider("Lumiere"));

        Assert.Equal("Lumiere", projection.AppName);
        Assert.Equal("HDR Ready", projection.HdrStatusLabel);
        Assert.Equal("Ready", projection.HdrStatusDetail);
        Assert.Equal("sRGB", projection.OutputProfileLabel);
        Assert.Equal("Compat", projection.OutputProfileStatusLabel);
        Assert.Equal(TrayMenuStatusSeverity.Info, projection.OutputProfileSeverity);
        Assert.Equal("Converted", projection.FidelityClaimLabel);
        Assert.Equal("Full Screen", projection.FullscreenCapture.Label);
        Assert.Equal("Ctrl+Shift+F", projection.FullscreenCapture.ShortcutText);
        Assert.True(projection.FullscreenCapture.IsEnabled);
        Assert.Equal("Region", projection.RegionCapture.Label);
        Assert.Equal("Ctrl+Shift+R", projection.RegionCapture.ShortcutText);
        Assert.True(projection.RegionCapture.IsEnabled);
        Assert.True(projection.OpenMainWindow.IsEnabled);
        Assert.True(projection.OpenSettings.IsEnabled);
        Assert.True(projection.Quit.IsEnabled);
    }

    [Fact]
    public void Project_ActiveCapture_DisablesCaptureCommandsWithoutDisablingNavigation()
    {
        var projection = TrayMenuProjection.Project(
            CaptureSessionState.Capturing(
                CreateTarget(),
                PreviewReadinessStatus.Ready("Ready", "HDR preview is ready.")),
            new StubSettingsProvider(string.Empty, string.Empty),
            new StubAboutInfoProvider("Lumiere"),
            CaptureCommandMode.Fullscreen);

        Assert.False(projection.FullscreenCapture.IsEnabled);
        Assert.True(projection.FullscreenCapture.IsActive);
        Assert.Equal("Capturing...", projection.FullscreenCapture.Label);
        Assert.Equal("Not assigned", projection.FullscreenCapture.ShortcutText);
        Assert.False(projection.RegionCapture.IsEnabled);
        Assert.False(projection.RegionCapture.IsActive);
        Assert.Equal("Region", projection.RegionCapture.Label);
        Assert.True(projection.OpenMainWindow.IsEnabled);
        Assert.True(projection.OpenSettings.IsEnabled);
    }

    [Fact]
    public void Project_OutputComplete_ShowsOutputCompleteInHdrStatus()
    {
        var projection = TrayMenuProjection.Project(
            CaptureSessionState.Idle(PreviewReadinessStatus.Ready("Ready", "HDR preview is ready.")),
            new StubSettingsProvider("Ctrl+Shift+F", "Ctrl+Shift+R"),
            new StubAboutInfoProvider("Lumiere"),
            outputResult: OutputResult.ClipboardSuccess(2048));

        Assert.Equal("Output complete", projection.HdrStatusLabel);
        Assert.Equal("Converted", projection.FidelityClaimLabel);
        Assert.True(projection.FullscreenCapture.IsEnabled);
        Assert.True(projection.RegionCapture.IsEnabled);
    }

    [Fact]
    public void Project_Hdr10ProfileMirrorsRuntimeFallbackFidelityClaim()
    {
        var projection = TrayMenuProjection.Project(
            CaptureSessionState.Idle(PreviewReadinessStatus.Ready("Ready", "HDR preview is ready.")),
            new StubSettingsProvider("Ctrl+Shift+F", "Ctrl+Shift+R")
            {
                ExportColorFormat = "HDR10",
                OutputTarget = OutputTarget.Folder,
            },
            new StubAboutInfoProvider("Lumiere"));

        Assert.Equal("HDR Ready", projection.HdrStatusLabel);
        Assert.Equal("HDR10", projection.OutputProfileLabel);
        Assert.Equal("Build", projection.OutputProfileStatusLabel);
        Assert.Contains("compatibility fallback", projection.OutputProfileDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(TrayMenuStatusSeverity.Warning, projection.OutputProfileSeverity);
        Assert.Equal("Converted", projection.FidelityClaimLabel);
        Assert.Contains("compatibility", projection.FidelityClaimDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(TrayMenuStatusSeverity.Warning, projection.FidelityClaimSeverity);
    }

    [Fact]
    public void Project_Hdr10ProfileShowsReadyGateWhenRuntimeCapabilityAndManualEvidencePass()
    {
        var projection = TrayMenuProjection.Project(
            CaptureSessionState.Idle(PreviewReadinessStatus.Ready("Ready", "HDR preview is ready.")),
            new StubSettingsProvider("Ctrl+Shift+F", "Ctrl+Shift+R")
            {
                ExportColorFormat = "HDR10",
                OutputTarget = OutputTarget.Folder,
            },
            new StubAboutInfoProvider("Lumiere"),
            validationArtifacts:
            [
                ArtifactWithFormatContract("Microsoft Paint"),
                ArtifactWithFormatContract("Windows Photos"),
                ArtifactWithFormatContract("Chromium browsers"),
            ],
            executionCapabilities: OutputProfileExecutionCapabilities.Create(
                OutputProfileExecutionCapability.SrgbCompatibility,
                OutputProfileExecutionCapability.Hdr10PreservedImplementedArtifactEncoder));

        Assert.Equal("HDR10", projection.OutputProfileLabel);
        Assert.Equal("Ready", projection.OutputProfileStatusLabel);
        Assert.Equal(TrayMenuStatusSeverity.Success, projection.OutputProfileSeverity);
        Assert.Contains("validated session", projection.OutputProfileDetail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Project_ClipboardTargetKeepsHdr10ProfileAtCompatEvenWhenFolderEvidenceWouldPass()
    {
        var projection = TrayMenuProjection.Project(
            CaptureSessionState.Idle(PreviewReadinessStatus.Ready("Ready", "HDR preview is ready.")),
            new StubSettingsProvider("Ctrl+Shift+F", "Ctrl+Shift+R")
            {
                ExportColorFormat = "HDR10",
                OutputTarget = OutputTarget.Clipboard,
            },
            new StubAboutInfoProvider("Lumiere"),
            validationArtifacts:
            [
                ArtifactWithFormatContract("Microsoft Paint"),
                ArtifactWithFormatContract("Windows Photos"),
                ArtifactWithFormatContract("Chromium browsers"),
            ],
            executionCapabilities: OutputProfileExecutionCapabilities.Create(
                OutputProfileExecutionCapability.SrgbCompatibility,
                OutputProfileExecutionCapability.Hdr10PreservedImplementedArtifactEncoder));

        Assert.Equal("Compat", projection.OutputProfileStatusLabel);
        Assert.Contains("clipboard output stays on sRGB compatibility output", projection.OutputProfileDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Converted", projection.FidelityClaimLabel);
    }

    [Fact]
    public void Project_OutputFailed_ShowsOutputFailedInHdrStatus()
    {
        var projection = TrayMenuProjection.Project(
            CaptureSessionState.Idle(PreviewReadinessStatus.Ready("Ready", "HDR preview is ready.")),
            new StubSettingsProvider("Ctrl+Shift+F", "Ctrl+Shift+R"),
            new StubAboutInfoProvider("Lumiere"),
            outputResult: OutputResult.ClipboardFailed("Access denied."));

        Assert.Equal("Failed to copy to clipboard", projection.HdrStatusLabel);
        Assert.True(projection.FullscreenCapture.IsEnabled);
        Assert.True(projection.RegionCapture.IsEnabled);
    }

    [Fact]
    public void TrayAlertMessage_NonEmptyForDegradedWhenAlertsEnabled()
    {
        var projection = TrayMenuProjection.Project(
            CaptureSessionState.Degraded(
                CreateTarget(),
                PreviewReadinessStatus.Degraded(PreviewReadinessStage.Presentation, "Degraded", "detail.")),
            new StubSettingsProvider(string.Empty, string.Empty),
            new StubAboutInfoProvider("Lumiere"),
            hdrAlertsEnabled: true);

        Assert.False(string.IsNullOrEmpty(projection.TrayAlertMessage));
    }

    [Fact]
    public void Project_TargetAwareUnvalidatedMirrorsSpecificHdrStatusAndTrayAlert()
    {
        var projection = TrayMenuProjection.Project(
            CaptureSessionState.Degraded(
                CreateTarget(),
                PreviewReadinessStatus.Degraded(
                    PreviewReadinessStage.Presentation,
                    "HDR readiness is unvalidated for the selected capture target.",
                    "Target-aware display capability could not be matched to a DXGI output.",
                    PreviewReadinessReason.TargetDisplayUnresolved)),
            new StubSettingsProvider(string.Empty, string.Empty),
            new StubAboutInfoProvider("Lumiere"),
            hdrAlertsEnabled: true);

        Assert.Equal("HDR unvalidated", projection.HdrStatusLabel);
        Assert.Contains("selected capture target", projection.HdrStatusDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("selected target", projection.TrayAlertMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Enable HDR", projection.TrayAlertMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Project_ActiveTargetDetailIncludesSelectedDisplayScope()
    {
        var projection = TrayMenuProjection.Project(
            CaptureSessionState.Capturing(
                CreateTarget(),
                PreviewReadinessStatus.Ready("HDR preview is ready.", "Target-aware readiness passed.")),
            new StubSettingsProvider(string.Empty, string.Empty),
            new StubAboutInfoProvider("Lumiere"));

        Assert.Contains("Selected display: Test Display.", projection.HdrStatusDetail, StringComparison.Ordinal);
        Assert.Contains("HDR preview is ready.", projection.HdrStatusDetail, StringComparison.Ordinal);
    }

    [Fact]
    public void TrayAlertMessage_EmptyWhenAlertsDisabled()
    {
        var projection = TrayMenuProjection.Project(
            CaptureSessionState.Degraded(
                CreateTarget(),
                PreviewReadinessStatus.Degraded(PreviewReadinessStage.Presentation, "Degraded", "detail.")),
            new StubSettingsProvider(string.Empty, string.Empty),
            new StubAboutInfoProvider("Lumiere"),
            hdrAlertsEnabled: false);

        Assert.Equal(string.Empty, projection.TrayAlertMessage);
    }

    [Fact]
    public void TrayAlertMessage_EmptyForReadyStateRegardlessOfAlertsEnabled()
    {
        var projectionEnabled = TrayMenuProjection.Project(
            CaptureSessionState.Idle(PreviewReadinessStatus.Ready("Ready", "HDR preview is ready.")),
            new StubSettingsProvider(string.Empty, string.Empty),
            new StubAboutInfoProvider("Lumiere"),
            hdrAlertsEnabled: true);

        Assert.Equal(string.Empty, projectionEnabled.TrayAlertMessage);
    }

    [Fact]
    public void TrayAlertMessage_NonEmptyForUnsupportedWhenAlertsEnabled()
    {
        var projection = TrayMenuProjection.Project(
            CaptureSessionState.Unsupported(
                PreviewReadinessStatus.Unsupported(PreviewReadinessStage.Presentation, "HDR unavailable", "HDR capture is not supported.")),
            new StubSettingsProvider(string.Empty, string.Empty),
            new StubAboutInfoProvider("Lumiere"),
            hdrAlertsEnabled: true);

        Assert.False(string.IsNullOrEmpty(projection.TrayAlertMessage));
        Assert.Contains("HDR", projection.TrayAlertMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TrayAlertMessage_NonEmptyForFailedWhenAlertsEnabled()
    {
        var projection = TrayMenuProjection.Project(
            CaptureSessionState.Failed(
                CreateTarget(),
                PreviewReadinessStatus.Failed(PreviewReadinessStage.Presentation, "Preview failed", "Preview failure.")),
            new StubSettingsProvider(string.Empty, string.Empty),
            new StubAboutInfoProvider("Lumiere"),
            hdrAlertsEnabled: true);

        Assert.False(string.IsNullOrEmpty(projection.TrayAlertMessage));
        Assert.Contains("failed", projection.TrayAlertMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static CaptureTarget CreateTarget() =>
        CaptureTarget.CreateForTest(
            new SizeInt32
            {
                Width = 1920,
                Height = 1080,
            },
            "Test Display",
            CaptureTargetKind.Display);

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
            EntryPointsTested: ["Tray menu"],
            OutputTargetsTested: ["Folder"],
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

    private static OutputViewerCompatibilityEvidence PassingHdrViewer(string viewerName) =>
        new(
            viewerName,
            OutputCompatibilityEvidenceStatus.Pass,
            OutputCompatibilityEvidenceStatus.Pass,
            OutputCompatibilityEvidenceStatus.Pass,
            $"Validated HDR-preserved viewer compatibility for {viewerName}.")
        {
            Hdr10MetadataStatus = OutputCompatibilityEvidenceStatus.Pass,
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

    private sealed class StubAboutInfoProvider(string appName) : IAboutInfoProvider
    {
        public string AppName => appName;

        public string Version => "0.1.0";

        public string Description => "Test description.";
    }

    private sealed class StubSettingsProvider(string fullscreenShortcut, string regionShortcut) : ISettingsProvider
    {
        public string FullscreenShortcut => fullscreenShortcut;

        public string RegionShortcut => regionShortcut;

        public bool HdrAlertsEnabled => true;

        public OutputTarget OutputTarget { get; init; } = OutputTarget.Clipboard;

        public string? SavePath => null;

        public bool TimestampNaming => true;

        public bool CopyAsImage => true;

        public AfterCaptureBehavior AfterCaptureBehavior => AfterCaptureBehavior.None;

        public string ExportColorFormat { get; init; } = "sRGB";
    }
}
