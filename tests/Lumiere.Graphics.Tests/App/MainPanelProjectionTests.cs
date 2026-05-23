using Lumiere.App;
using Lumiere.Capture;
using Lumiere.Graphics.Hdr;
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
    [InlineData(PreviewReadinessState.Ready, "HDR Ready", MainPanelTrustIcon.CheckmarkCircle)]
    [InlineData(PreviewReadinessState.Degraded, "Enable HDR", MainPanelTrustIcon.Desktop)]
    [InlineData(PreviewReadinessState.Unsupported, "HDR unavailable", MainPanelTrustIcon.ErrorCircle)]
    [InlineData(PreviewReadinessState.Failed, "HDR unavailable", MainPanelTrustIcon.ErrorCircle)]
    [InlineData(PreviewReadinessState.Initializing, "Checking HDR", MainPanelTrustIcon.Clock)]
    public void ProjectStatus_MapsReadinessToConciseTrustSummary(
        PreviewReadinessState readinessState,
        string expectedLabel,
        MainPanelTrustIcon expectedIcon)
    {
        var state = CreateState(readinessState);

        var projection = MainPanelProjection.Project(state);

        Assert.Equal(expectedLabel, projection.TrustLabel);
        Assert.Equal(expectedIcon, projection.TrustIcon);
        Assert.False(string.IsNullOrWhiteSpace(projection.TrustMessage));
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
}
