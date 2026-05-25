namespace Lumiere.Infrastructure.Interop;

public interface IGlobalHotkeyRegistrar : IDisposable
{
    event EventHandler<GlobalHotkeyPressedEventArgs>? HotkeyPressed;

    IReadOnlyList<GlobalHotkeyRegistrationResult> Register(IReadOnlyCollection<GlobalHotkeyRegistration> registrations);

    void UnregisterAll();
}

public sealed record GlobalHotkeyRegistration(
    HotkeyCommand Command,
    int Id,
    bool Control,
    bool Shift,
    bool Alt,
    bool Windows,
    int VirtualKey,
    string DisplayText);

public sealed record GlobalHotkeyRegistrationResult(
    HotkeyCommand Command,
    string DisplayText,
    bool Registered,
    string Detail);

public sealed class GlobalHotkeyPressedEventArgs(HotkeyCommand command) : EventArgs
{
    public HotkeyCommand Command { get; } = command;
}

public enum HotkeyCommand
{
    FullscreenCapture = 1,
    RegionCapture,
}
