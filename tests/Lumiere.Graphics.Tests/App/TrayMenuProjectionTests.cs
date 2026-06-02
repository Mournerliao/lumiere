using Lumiere.App;
using Lumiere.Capture;
using Lumiere.Graphics.Hdr;
using Lumiere.Graphics.Output;
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
        Assert.True(projection.FullscreenCapture.IsEnabled);
        Assert.True(projection.RegionCapture.IsEnabled);
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

        public OutputTarget OutputTarget => OutputTarget.Clipboard;

        public string? SavePath => null;

        public bool TimestampNaming => true;

        public bool CopyAsImage => true;

        public AfterCaptureBehavior AfterCaptureBehavior => AfterCaptureBehavior.None;
    }
}
