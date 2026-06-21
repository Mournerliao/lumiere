namespace Lumiere.App;

public sealed record AppShellLayoutProjection(
    int WidthDips,
    int HeightDips)
{
    public const int DefaultWidthDips = 360;
    public const int MainPanelHeightDips = 680;
    public const int MainPanelAlertHeightDips = 736;
    public const int SettingsPanelHeightDips = 720;

    public static AppShellLayoutProjection Project(
        AppShellView activeView,
        bool hasAlert) =>
        new(
            DefaultWidthDips,
            activeView switch
            {
                AppShellView.Settings => SettingsPanelHeightDips,
                _ => hasAlert ? MainPanelAlertHeightDips : MainPanelHeightDips,
            });
}
