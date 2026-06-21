using Lumiere.App;
using Xunit;

namespace Lumiere.Graphics.Tests.App;

public sealed class AppShellLayoutProjectionTests
{
    [Fact]
    public void Project_MainViewWithoutAlert_UsesCompactMainHeight()
    {
        var projection = AppShellLayoutProjection.Project(AppShellView.Main, hasAlert: false);

        Assert.Equal(AppShellLayoutProjection.DefaultWidthDips, projection.WidthDips);
        Assert.Equal(AppShellLayoutProjection.MainPanelHeightDips, projection.HeightDips);
    }

    [Fact]
    public void Project_MainViewWithAlert_UsesExpandedMainHeight()
    {
        var projection = AppShellLayoutProjection.Project(AppShellView.Main, hasAlert: true);

        Assert.Equal(AppShellLayoutProjection.DefaultWidthDips, projection.WidthDips);
        Assert.Equal(AppShellLayoutProjection.MainPanelAlertHeightDips, projection.HeightDips);
    }

    [Fact]
    public void Project_SettingsView_IgnoresMainAlertHeight()
    {
        var projection = AppShellLayoutProjection.Project(AppShellView.Settings, hasAlert: true);

        Assert.Equal(AppShellLayoutProjection.DefaultWidthDips, projection.WidthDips);
        Assert.Equal(AppShellLayoutProjection.SettingsPanelHeightDips, projection.HeightDips);
    }
}
