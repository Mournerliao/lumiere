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

public sealed class TrayMenuShowRequestedEventArgs(int cursorX, int cursorY, TrayMenuSnapshot snapshot) : EventArgs
{
    public int CursorX { get; } = cursorX;
    public int CursorY { get; } = cursorY;
    public TrayMenuSnapshot Snapshot { get; } = snapshot;
}

public enum TrayMenuCommand
{
    FullscreenCapture = 1,
    RegionCapture,
    OpenMainWindow,
    OpenSettings,
    Quit,
}
