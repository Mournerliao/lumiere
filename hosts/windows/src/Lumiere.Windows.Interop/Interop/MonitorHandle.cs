namespace Lumiere.Windows.Interop;

internal sealed record MonitorHandle(
    IntPtr RawHandle,
    string DisplayName,
    int? Left = null,
    int? Top = null,
    int? Width = null,
    int? Height = null)
{
    public bool IsInvalid => RawHandle == IntPtr.Zero;
}
