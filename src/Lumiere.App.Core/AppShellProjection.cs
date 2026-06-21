using Lumiere.Capture;
using Lumiere.Graphics.Output;

namespace Lumiere.App;

public sealed record AppShellProjection(
    AppShellView ActiveView,
    MainPanelProjection MainPanel)
{
    public bool IsMainPanelVisible => ActiveView is AppShellView.Main;

    public bool IsSettingsVisible => ActiveView is AppShellView.Settings;

    public static AppShellProjection Project(
        CaptureSessionState state,
        AppShellView activeView,
        OutputResult? outputResult = null,
        bool hdrAlertsEnabled = false,
        OutputProfileExecutionCapabilities? executionCapabilities = null) =>
        new(activeView, MainPanelProjection.Project(
            state,
            outputResult,
            hdrAlertsEnabled,
            executionCapabilities: executionCapabilities));

    public AppShellProjection OpenSettings(
        CaptureSessionState state,
        OutputResult? outputResult = null,
        bool hdrAlertsEnabled = false,
        OutputProfileExecutionCapabilities? executionCapabilities = null) =>
        Project(state, AppShellView.Settings, outputResult, hdrAlertsEnabled, executionCapabilities);

    public AppShellProjection CloseSettings(
        CaptureSessionState state,
        OutputResult? outputResult = null,
        bool hdrAlertsEnabled = false,
        OutputProfileExecutionCapabilities? executionCapabilities = null) =>
        Project(state, AppShellView.Main, outputResult, hdrAlertsEnabled, executionCapabilities);
}

public enum AppShellView
{
    Main = 0,
    Settings,
}
