using Lumiere.Capture;
using Lumiere.Graphics.Output;

namespace Lumiere.App;

public sealed record AppShellProjection(
    AppShellView ActiveView,
    MainPanelProjection MainPanel)
{
    public bool IsMainPanelVisible => ActiveView is AppShellView.Main;

    public bool IsSettingsVisible => ActiveView is AppShellView.Settings;

    public static AppShellProjection Project(CaptureSessionState state, AppShellView activeView, OutputResult? outputResult = null, bool hdrAlertsEnabled = false) =>
        new(activeView, MainPanelProjection.Project(state, outputResult, hdrAlertsEnabled));

    public AppShellProjection OpenSettings(CaptureSessionState state, OutputResult? outputResult = null, bool hdrAlertsEnabled = false) =>
        Project(state, AppShellView.Settings, outputResult, hdrAlertsEnabled);

    public AppShellProjection CloseSettings(CaptureSessionState state, OutputResult? outputResult = null, bool hdrAlertsEnabled = false) =>
        Project(state, AppShellView.Main, outputResult, hdrAlertsEnabled);
}

public enum AppShellView
{
    Main = 0,
    Settings,
}
