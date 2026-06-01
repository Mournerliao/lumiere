using Lumiere.Capture;
using Lumiere.Graphics.Output;

namespace Lumiere.App;

public sealed record AppShellProjection(
    AppShellView ActiveView,
    MainPanelProjection MainPanel)
{
    public bool IsMainPanelVisible => ActiveView is AppShellView.Main;

    public bool IsSettingsVisible => ActiveView is AppShellView.Settings;

    public static AppShellProjection Project(CaptureSessionState state, AppShellView activeView, OutputResult? outputResult = null) =>
        new(activeView, MainPanelProjection.Project(state, outputResult));

    public AppShellProjection OpenSettings(CaptureSessionState state, OutputResult? outputResult = null) =>
        Project(state, AppShellView.Settings, outputResult);

    public AppShellProjection CloseSettings(CaptureSessionState state, OutputResult? outputResult = null) =>
        Project(state, AppShellView.Main, outputResult);
}

public enum AppShellView
{
    Main = 0,
    Settings,
}
