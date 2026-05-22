using Lumiere.App;
using Lumiere.Capture;
using Lumiere.Graphics.Hdr;
using Windows.Graphics;
using Xunit;

namespace Lumiere.Graphics.Tests.App;

public sealed class AppShellProjectionTests
{
    [Fact]
    public void Project_StartsOnMainPanelWithSettingsHidden()
    {
        var projection = AppShellProjection.Project(CreateState(), AppShellView.Main);

        Assert.Equal(AppShellView.Main, projection.ActiveView);
        Assert.True(projection.IsMainPanelVisible);
        Assert.False(projection.IsSettingsVisible);
    }

    [Fact]
    public void OpenSettings_ShowsSettingsWithoutChangingSessionProjection()
    {
        var state = CreateState();
        var mainProjection = AppShellProjection.Project(state, AppShellView.Main);

        var settingsProjection = mainProjection.OpenSettings(state);

        Assert.Equal(AppShellView.Settings, settingsProjection.ActiveView);
        Assert.False(settingsProjection.IsMainPanelVisible);
        Assert.True(settingsProjection.IsSettingsVisible);
        Assert.Equal(mainProjection.MainPanel, settingsProjection.MainPanel);
    }

    [Fact]
    public void CloseSettings_ReturnsToMainPanelWithLatestSessionProjection()
    {
        var settingsProjection = AppShellProjection.Project(CreateState(), AppShellView.Settings);
        var latestState = CaptureSessionState.Failed(
            CreateTarget(),
            PreviewReadinessStatus.Failed(
                PreviewReadinessStage.Capture,
                "Preview failed",
                "Failure while settings was visible."));

        var mainProjection = settingsProjection.CloseSettings(latestState);

        Assert.Equal(AppShellView.Main, mainProjection.ActiveView);
        Assert.True(mainProjection.IsMainPanelVisible);
        Assert.False(mainProjection.IsSettingsVisible);
        Assert.Equal("Capture failed", mainProjection.MainPanel.ActionTitle);
        Assert.Equal("HDR unavailable", mainProjection.MainPanel.TrustLabel);
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
}
