namespace Lumiere.Infrastructure.Interop;

public sealed record MonitorHandle(IntPtr RawHandle, string DisplayName)
{
    public bool IsInvalid => RawHandle == IntPtr.Zero;
}
