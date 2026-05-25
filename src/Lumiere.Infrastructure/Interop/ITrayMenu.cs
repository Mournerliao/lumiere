namespace Lumiere.Infrastructure.Interop;

public interface ITrayMenu : IDisposable
{
    event EventHandler<TrayMenuCommandRequestedEventArgs>? CommandRequested;

    void Update(TrayMenuSnapshot snapshot);
}

public sealed class TrayMenuCommandRequestedEventArgs(TrayMenuCommand command) : EventArgs
{
    public TrayMenuCommand Command { get; } = command;
}

public enum TrayMenuCommand
{
    FullscreenCapture = 1,
    RegionCapture,
    OpenMainWindow,
    OpenSettings,
    Quit,
}
