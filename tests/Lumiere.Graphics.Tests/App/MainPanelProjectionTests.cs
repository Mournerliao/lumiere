using Lumiere.App;
using Lumiere.Capture;
using Lumiere.Graphics.Hdr;
using Lumiere.Graphics.Output;
using Windows.Graphics;
using Xunit;

namespace Lumiere.Graphics.Tests.App;

public sealed class MainPanelProjectionTests
{
    [Theory]
    [InlineData(null, "Not assigned")]
    [InlineData("", "Not assigned")]
    [InlineData("   ", "Not assigned")]
    [InlineData("Ctrl+Shift+R", "Ctrl+Shift+R")]
    public void FormatShortcut_UsesHonestFallbackForEmptyValues(string? shortcut, string expected)
    {
        var display = MainPanelProjection.FormatShortcut(shortcut);

        Assert.Equal(expected, display);
    }

    [Theory]
    [InlineData(CaptureSessionStatus.Idle, true)]
    [InlineData(CaptureSessionStatus.SelectingTarget, false)]
    [InlineData(CaptureSessionStatus.Initializing, false)]
    [InlineData(CaptureSessionStatus.Capturing, false)]
    [InlineData(CaptureSessionStatus.Degraded, false)]
    [InlineData(CaptureSessionStatus.Unsupported, true)]
    [InlineData(CaptureSessionStatus.Failed, true)]
    [InlineData(CaptureSessionStatus.Disposed, false)]
    public void ProjectActions_AllowsRecoverableStatesOnly(CaptureSessionStatus status, bool expectedCanStart)
    {
        var state = CreateState(status);

        var projection = MainPanelProjection.Project(state);

        Assert.Equal(expectedCanStart, projection.CanStartCapture);
    }

    [Theory]
    [InlineData(PreviewReadinessState.Ready, "HDR Ready", MainPanelTrustIcon.CheckmarkCircle, MainPanelTrustSeverity.Success)]
    [InlineData(PreviewReadinessState.Degraded, "Enable HDR", MainPanelTrustIcon.Desktop, MainPanelTrustSeverity.Warning)]
    [InlineData(PreviewReadinessState.Unsupported, "HDR unavailable", MainPanelTrustIcon.ErrorCircle, MainPanelTrustSeverity.Error)]
    [InlineData(PreviewReadinessState.Failed, "Preview failed", MainPanelTrustIcon.ErrorBadge, MainPanelTrustSeverity.Error)]
    [InlineData(PreviewReadinessState.Initializing, "Checking HDR", MainPanelTrustIcon.Clock, MainPanelTrustSeverity.Neutral)]
    public void ProjectStatus_MapsReadinessToConciseTrustSummary(
        PreviewReadinessState readinessState,
        string expectedLabel,
        MainPanelTrustIcon expectedIcon,
        MainPanelTrustSeverity expectedSeverity)
    {
        var state = CreateState(readinessState);

        var projection = MainPanelProjection.Project(state);

        Assert.Equal(expectedLabel, projection.TrustLabel);
        Assert.Equal(expectedIcon, projection.TrustIcon);
        Assert.Equal(expectedSeverity, projection.TrustSeverity);
        Assert.False(string.IsNullOrWhiteSpace(projection.TrustMessage));
    }

    [Fact]
    public void ProjectStatus_OutputCompleteShowsDistinctTrustLabel()
    {
        var state = CreateState(PreviewReadinessState.Ready);
        var outputResult = OutputResult.ClipboardSuccess(1024);

        var projection = MainPanelProjection.Project(state, outputResult);

        Assert.Equal("Output complete", projection.TrustLabel);
        Assert.Equal(MainPanelTrustIcon.InfoCircle, projection.TrustIcon);
        Assert.Equal(MainPanelTrustSeverity.Info, projection.TrustSeverity);
    }

    [Fact]
    public void ProjectStatus_SeparatesOutputSuccessFromFidelityClaim()
    {
        var state = CreateState(PreviewReadinessState.Ready);
        var outputResult = OutputResult.ClipboardSuccess(1024)
            .WithRequestedProfile(OutputProfileContract.FromSettingsValue("HDR10"));

        var projection = MainPanelProjection.Project(
            state,
            outputResult,
            exportColorFormat: "HDR10");

        Assert.Equal("Output complete", projection.TrustLabel);
        Assert.Equal("HDR10", projection.OutputProfile.Label);
        Assert.Equal("Validate", projection.OutputProfile.StatusLabel);
        Assert.Equal(FidelityClaimKind.Unvalidated, projection.FidelityClaim.Kind);
        Assert.Equal("Unvalidated", projection.FidelityClaim.Label);
        Assert.Contains("requested HDR10", projection.OutputResult.FidelityDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("using sRGB", projection.OutputResult.FidelityDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Fidelity claim: Converted", projection.OutputResult.FidelityDetail);
        Assert.DoesNotContain("HDR-preserved", projection.TrustLabel, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR-preserved", projection.FidelityClaim.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR-preserved", projection.OutputResult.FidelityDetail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectStatus_DefaultOutputProfileIsCompatibilityConvertedFallback()
    {
        var state = CreateState(PreviewReadinessState.Ready);

        var projection = MainPanelProjection.Project(state);

        Assert.Equal("sRGB", projection.OutputProfile.Label);
        Assert.Equal("Compat", projection.OutputProfile.StatusLabel);
        Assert.Equal(FidelityClaimKind.Converted, projection.FidelityClaim.Kind);
        Assert.Equal(PerfectHdrFidelityProjection.ReleaseTarget, projection.ReleaseTarget);
    }

    [Fact]
    public void ProjectStatus_OutputFailedShowsDistinctTrustLabel()
    {
        var state = CreateState(PreviewReadinessState.Ready);
        var outputResult = OutputResult.ClipboardFailed("Clipboard write denied.");

        var projection = MainPanelProjection.Project(state, outputResult);

        Assert.Equal("Failed to copy to clipboard", projection.TrustLabel);
        Assert.Equal(MainPanelTrustIcon.WarningCircle, projection.TrustIcon);
        Assert.Equal(MainPanelTrustSeverity.Warning, projection.TrustSeverity);
    }

    [Fact]
    public void ProjectStatus_TargetAwareUnvalidatedDoesNotTellUserToEnableHdr()
    {
        var state = CaptureSessionState.Degraded(
            CreateTarget(),
            PreviewReadinessStatus.Degraded(
                PreviewReadinessStage.Presentation,
                "HDR readiness is unvalidated for the selected capture target.",
                "Target-aware display capability could not be matched to a DXGI output.",
                PreviewReadinessReason.TargetDisplayUnresolved));

        var projection = MainPanelProjection.Project(state, hdrAlertsEnabled: true);

        Assert.Equal("HDR unvalidated", projection.TrustLabel);
        Assert.Equal(MainPanelTrustIcon.WarningCircle, projection.TrustIcon);
        Assert.Equal(MainPanelTrustSeverity.Warning, projection.TrustSeverity);
        Assert.Contains("selected capture target", projection.AlertMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Enable HDR", projection.TrustLabel, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Enable HDR", projection.AlertMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectStatus_TargetAwareUnvalidatedKeepsHdr10FidelityUnvalidatedEvenWithViewerEvidence()
    {
        var state = CaptureSessionState.Degraded(
            CreateTarget(),
            PreviewReadinessStatus.Degraded(
                PreviewReadinessStage.Presentation,
                "HDR readiness is unvalidated for the selected capture target.",
                "Target-aware display capability could not be matched to a DXGI output.",
                PreviewReadinessReason.TargetDisplayUnresolved));
        var artifacts = new[]
        {
            ArtifactFor("Microsoft Paint"),
            ArtifactFor("Windows Photos"),
            ArtifactFor("Chromium browsers"),
        };

        var projection = MainPanelProjection.Project(
            state,
            hdrAlertsEnabled: true,
            exportColorFormat: "HDR10",
            validationArtifacts: artifacts);

        Assert.Equal("HDR10", projection.OutputProfile.Label);
        Assert.Equal(FidelityClaimKind.Unvalidated, projection.FidelityClaim.Kind);
        Assert.Equal("Unvalidated", projection.FidelityClaim.Label);
        Assert.Contains("No fidelity claim", projection.FidelityClaim.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("validated HDR-preserved", projection.FidelityClaim.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectStatus_AllDistinguishableStatesHaveDistinctLabels()
    {
        var target = CreateTarget();
        var readyState = CaptureSessionState.Capturing(
            target, PreviewReadinessStatus.Ready("Ready", "detail."));
        var degradedState = CaptureSessionState.Degraded(
            target,
            PreviewReadinessStatus.Degraded(PreviewReadinessStage.Presentation, "Degraded", "detail."));
        var unsupportedState = CaptureSessionState.Unsupported(
            target,
            PreviewReadinessStatus.Unsupported(PreviewReadinessStage.Capture, "Unsupported", "detail."));
        var failedState = CaptureSessionState.Failed(
            target,
            PreviewReadinessStatus.Failed(PreviewReadinessStage.Capture, "Failed", "detail."));
        var initializingState = CaptureSessionState.Initializing(
            target, PreviewReadinessStatus.Initializing(PreviewReadinessStage.Capture, "Init", "detail."));

        var outputSuccess = OutputResult.ClipboardSuccess(1);
        var outputFailure = OutputResult.ClipboardFailed("fail");

        var states = new (string Name, string ExpectedLabel, MainPanelTrustIcon ExpectedIcon)[]
        {
            ("HDR ready", "HDR Ready", MainPanelTrustIcon.CheckmarkCircle),
            ("Checking HDR", "Checking HDR", MainPanelTrustIcon.Clock),
            ("Enable HDR", "Enable HDR", MainPanelTrustIcon.Desktop),
            ("HDR unavailable", "HDR unavailable", MainPanelTrustIcon.ErrorCircle),
            ("Preview failed", "Preview failed", MainPanelTrustIcon.ErrorBadge),
            ("Output complete", "Output complete", MainPanelTrustIcon.InfoCircle),
            ("Output failed", "Failed to copy to clipboard", MainPanelTrustIcon.WarningCircle),
        };

        var projections = new[]
        {
            MainPanelProjection.Project(readyState),
            MainPanelProjection.Project(initializingState),
            MainPanelProjection.Project(degradedState),
            MainPanelProjection.Project(unsupportedState),
            MainPanelProjection.Project(failedState),
            MainPanelProjection.Project(readyState, outputSuccess),
            MainPanelProjection.Project(readyState, outputFailure),
        };

        for (int i = 0; i < states.Length; i++)
        {
            Assert.Equal(states[i].ExpectedLabel, projections[i].TrustLabel);
            Assert.Equal(states[i].ExpectedIcon, projections[i].TrustIcon);
        }

        var distinctLabels = projections.Select(p => p.TrustLabel).Distinct().Count();
        var distinctIcons = projections.Select(p => p.TrustIcon).Distinct().Count();
        Assert.Equal(7, distinctLabels);
        Assert.Equal(7, distinctIcons);
    }

    [Fact]
    public void ProjectStatus_OutputResultOverridesReadiness()
    {
        var state = CreateState(PreviewReadinessState.Degraded);
        var outputResult = OutputResult.ClipboardSuccess(512);

        var projection = MainPanelProjection.Project(state, outputResult);

        Assert.Equal("Output complete", projection.TrustLabel);
        Assert.Equal(MainPanelTrustSeverity.Info, projection.TrustSeverity);
    }

    [Fact]
    public void ProjectStatus_NullOutputResultShowsReadinessState()
    {
        var state = CreateState(PreviewReadinessState.Ready);

        var withNull = MainPanelProjection.Project(state, null);
        var withoutParam = MainPanelProjection.Project(state);

        Assert.Equal(withoutParam.TrustLabel, withNull.TrustLabel);
        Assert.Equal(withoutParam.TrustIcon, withNull.TrustIcon);
        Assert.Equal(withoutParam.TrustSeverity, withNull.TrustSeverity);
    }

    [Theory]
    [InlineData(PreviewReadinessState.Degraded)]
    [InlineData(PreviewReadinessState.Unsupported)]
    [InlineData(PreviewReadinessState.Failed)]
    public void ProjectStatus_DegradedUnsupportedFailedDoNotUseSuccessLanguage(PreviewReadinessState readinessState)
    {
        var state = CreateState(readinessState);

        var projection = MainPanelProjection.Project(state);

        Assert.DoesNotContain("success", projection.TrustLabel, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("complete", projection.TrustLabel, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ready", projection.TrustLabel, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectStatus_OutputFailedDoesNotUseSuccessLanguage()
    {
        var state = CreateState(PreviewReadinessState.Ready);
        var outputResult = OutputResult.ClipboardFailed("denied");

        var projection = MainPanelProjection.Project(state, outputResult);

        Assert.DoesNotContain("success", projection.TrustLabel, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("complete", projection.TrustLabel, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectStatus_PartialSuccessShowsPartialLabel()
    {
        var state = CreateState(PreviewReadinessState.Ready);
        var outputResult = OutputResult.FromTargets(
            OutputTargetResult.Success(OutputTarget.Clipboard, "Copied to clipboard"),
            OutputTargetResult.Failed(OutputTarget.Folder, "Failed to save to folder"));

        var projection = MainPanelProjection.Project(state, outputResult);

        Assert.Equal("Output partially complete", projection.TrustLabel);
        Assert.Equal(MainPanelTrustIcon.WarningCircle, projection.TrustIcon);
        Assert.Equal(MainPanelTrustSeverity.Warning, projection.TrustSeverity);
    }

    [Fact]
    public void ProjectStatus_AllSkippedShowsSkippedLabel()
    {
        var state = CreateState(PreviewReadinessState.Ready);
        var outputResult = OutputResult.ClipboardSkipped("Clipboard output skipped by settings");

        var projection = MainPanelProjection.Project(state, outputResult);

        Assert.Equal("Output skipped", projection.TrustLabel);
        Assert.Equal(MainPanelTrustIcon.WarningCircle, projection.TrustIcon);
        Assert.Equal(MainPanelTrustSeverity.Warning, projection.TrustSeverity);
    }

    [Theory]
    [InlineData(PreviewReadinessState.Degraded)]
    [InlineData(PreviewReadinessState.Unsupported)]
    [InlineData(PreviewReadinessState.Failed)]
    public void AlertMessage_NonEmptyForDegradedUnsupportedFailedWhenAlertsEnabled(PreviewReadinessState readinessState)
    {
        var state = CreateState(readinessState);

        var projection = MainPanelProjection.Project(state, hdrAlertsEnabled: true);

        Assert.True(projection.HasAlert);
        Assert.False(string.IsNullOrWhiteSpace(projection.AlertMessage));
    }

    [Theory]
    [InlineData(PreviewReadinessState.Degraded)]
    [InlineData(PreviewReadinessState.Unsupported)]
    [InlineData(PreviewReadinessState.Failed)]
    [InlineData(PreviewReadinessState.Ready)]
    [InlineData(PreviewReadinessState.Initializing)]
    public void AlertMessage_EmptyForAllStatesWhenAlertsDisabled(PreviewReadinessState readinessState)
    {
        var state = CreateState(readinessState);

        var projection = MainPanelProjection.Project(state, hdrAlertsEnabled: false);

        Assert.False(projection.HasAlert);
        Assert.Equal(string.Empty, projection.AlertMessage);
    }

    [Theory]
    [InlineData(PreviewReadinessState.Ready)]
    [InlineData(PreviewReadinessState.Initializing)]
    public void AlertMessage_EmptyWhenReadinessReadyOrInitializingRegardlessOfAlertsEnabled(PreviewReadinessState readinessState)
    {
        var state = CreateState(readinessState);

        var projectionEnabled = MainPanelProjection.Project(state, hdrAlertsEnabled: true);
        var projectionDisabled = MainPanelProjection.Project(state, hdrAlertsEnabled: false);

        Assert.False(projectionEnabled.HasAlert);
        Assert.Equal(string.Empty, projectionEnabled.AlertMessage);
        Assert.False(projectionDisabled.HasAlert);
        Assert.Equal(string.Empty, projectionDisabled.AlertMessage);
    }

    [Fact]
    public void AlertMessage_EmptyWhenOutputResultPresentRegardlessOfAlertsEnabled()
    {
        var state = CreateState(PreviewReadinessState.Degraded);
        var outputResult = OutputResult.ClipboardSuccess(512);

        var projection = MainPanelProjection.Project(state, outputResult, hdrAlertsEnabled: true);

        Assert.False(projection.HasAlert);
        Assert.Equal(string.Empty, projection.AlertMessage);
    }

    [Fact]
    public void AlertMessage_DegradedShowsEnableHdrHint()
    {
        var state = CreateState(PreviewReadinessState.Degraded);

        var projection = MainPanelProjection.Project(state, hdrAlertsEnabled: true);

        Assert.Contains("HDR", projection.AlertMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AlertMessage_UnsupportedShowsNotSupportedHint()
    {
        var state = CreateState(PreviewReadinessState.Unsupported);

        var projection = MainPanelProjection.Project(state, hdrAlertsEnabled: true);

        Assert.Contains("not supported", projection.AlertMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AlertMessage_FailedShowsPreviewFailedHint()
    {
        var state = CreateState(PreviewReadinessState.Failed);

        var projection = MainPanelProjection.Project(state, hdrAlertsEnabled: true);

        Assert.Contains("Preview failed", projection.AlertMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AlertMessage_DoesNotClaimHdrPreservation()
    {
        var degradedState = CreateState(PreviewReadinessState.Degraded);
        var unsupportedState = CreateState(PreviewReadinessState.Unsupported);
        var failedState = CreateState(PreviewReadinessState.Failed);

        var degraded = MainPanelProjection.Project(degradedState, hdrAlertsEnabled: true);
        var unsupported = MainPanelProjection.Project(unsupportedState, hdrAlertsEnabled: true);
        var failed = MainPanelProjection.Project(failedState, hdrAlertsEnabled: true);

        Assert.DoesNotContain("HDR-preserving", degraded.AlertMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR preserving", degraded.AlertMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR-preserving", unsupported.AlertMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR preserving", unsupported.AlertMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR-preserving", failed.AlertMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR preserving", failed.AlertMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static CaptureSessionState CreateState(CaptureSessionStatus status)
    {
        var target = CreateTarget();
        var readiness = PreviewReadinessStatus.Ready("HDR-ready", "Test readiness.");

        return status switch
        {
            CaptureSessionStatus.Idle => CaptureSessionState.Idle(readiness),
            CaptureSessionStatus.SelectingTarget => CaptureSessionState.SelectingTarget(),
            CaptureSessionStatus.Initializing => CaptureSessionState.Initializing(target, readiness),
            CaptureSessionStatus.Capturing => CaptureSessionState.Capturing(target, readiness),
            CaptureSessionStatus.Degraded => CaptureSessionState.Degraded(
                target,
                PreviewReadinessStatus.Degraded(
                    PreviewReadinessStage.Presentation,
                    "Degraded preview",
                    "Test degradation.")),
            CaptureSessionStatus.Unsupported => CaptureSessionState.Unsupported(
                target,
                PreviewReadinessStatus.Unsupported(
                    PreviewReadinessStage.Capture,
                    "Unsupported capture",
                    "Test unsupported.")),
            CaptureSessionStatus.Failed => CaptureSessionState.Failed(
                target,
                PreviewReadinessStatus.Failed(
                    PreviewReadinessStage.Capture,
                    "Preview failed",
                    "Test failure.")),
            CaptureSessionStatus.Disposed => CaptureSessionState.Disposed(),
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };
    }

    private static CaptureSessionState CreateState(PreviewReadinessState readinessState)
    {
        var target = CreateTarget();

        var readiness = readinessState switch
        {
            PreviewReadinessState.Ready => PreviewReadinessStatus.Ready("Ready", "Ready detail."),
            PreviewReadinessState.Degraded => PreviewReadinessStatus.Degraded(
                PreviewReadinessStage.Presentation,
                "Degraded",
                "Degraded detail."),
            PreviewReadinessState.Unsupported => PreviewReadinessStatus.Unsupported(
                PreviewReadinessStage.Capture,
                "Unsupported",
                "Unsupported detail."),
            PreviewReadinessState.Failed => PreviewReadinessStatus.Failed(
                PreviewReadinessStage.Capture,
                "Failed",
                "Failed detail."),
            _ => PreviewReadinessStatus.Initializing(
                PreviewReadinessStage.Capture,
                "Initializing",
                "Initializing detail."),
        };

        return CaptureSessionState.FromReadiness(target, readiness);
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

    private static OutputViewerCompatibilityEvidence PassingHdrViewer(string name) =>
        new(
            name,
            OutputCompatibilityEvidenceStatus.Pass,
            OutputCompatibilityEvidenceStatus.Pass,
            OutputCompatibilityEvidenceStatus.Pass,
            "Validated HDR viewer.");

    private static OutputValidationSessionArtifact ArtifactFor(string viewerName) =>
        new(
            Date: "2026-06-21",
            Tester: "QA",
            BuildCommit: "485bc31",
            WindowsVersion: "Windows 11 24H2",
            Device: "HDR workstation",
            Gpu: "Test GPU",
            DisplaySetup: "HDR primary",
            HdrState: "HDR enabled",
            DpiScales: ["150%"],
            EntryPointsTested: ["Main panel"],
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
            ]);
}
