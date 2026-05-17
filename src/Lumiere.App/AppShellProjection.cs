using Lumiere.Capture;

namespace Lumiere.App;

public sealed record AppShellProjection(
    AppShellView ActiveView,
    MainPanelProjection MainPanel)
{
    public bool IsMainPanelVisible => ActiveView is AppShellView.Main;

    public bool IsSettingsVisible => ActiveView is AppShellView.Settings;

    public static AppShellProjection Project(CaptureSessionState state, AppShellView activeView) =>
        new(activeView, MainPanelProjection.Project(state));

    public AppShellProjection OpenSettings(CaptureSessionState state) =>
        Project(state, AppShellView.Settings);

    public AppShellProjection CloseSettings(CaptureSessionState state) =>
        Project(state, AppShellView.Main);
}

public enum AppShellView
{
    Main = 0,
    Settings,
}
