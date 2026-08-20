namespace Lumiere.Windows.Interop;

public sealed record MonitorHandle(
    IntPtr RawHandle,
    string DisplayName,
    int? Left = null,
    int? Top = null,
    int? Width = null,
    int? Height = null)
{
    public bool IsInvalid => RawHandle == IntPtr.Zero;
}
