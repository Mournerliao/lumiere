using System.Runtime.InteropServices;

namespace Lumiere.Infrastructure.Interop;

public static class MonitorSelectionInterop
{
    private const string User32Library = "user32.dll";

    public static MonitorHandle GetCurrentMonitorFromCursor()
    {
        if (!GetCursorPos(out var point))
        {
            throw MonitorSelectionFailure(
                "GetCursorPos",
                "Failed to retrieve the current cursor position.");
        }

        var monitor = MonitorFromPoint(point, MonitorFromPointFlags.MONITOR_DEFAULTTONEAREST);
        return GetMonitorHandleInfo(monitor);
    }

    public static MonitorHandle GetMonitorFromWindow(IntPtr windowHandle)
    {
        var monitor = MonitorFromWindow(windowHandle, MonitorFromWindowFlags.MONITOR_DEFAULTTONEAREST);
        if (monitor == IntPtr.Zero)
        {
            throw MonitorSelectionFailure(
                "MonitorFromWindow",
                $"MonitorFromWindow returned NULL for window handle 0x{windowHandle:X}.");
        }

        return GetMonitorHandleInfo(monitor);
    }

    private static MonitorHandle GetMonitorHandleInfo(IntPtr monitorHandle)
    {
        var monitorInfo = new MonitorInfoEx { Size = Marshal.SizeOf<MonitorInfoEx>() };
        if (!GetMonitorInfo(monitorHandle, ref monitorInfo))
        {
            return new MonitorHandle(monitorHandle, "Display");
        }

        return new MonitorHandle(
            monitorHandle,
            monitorInfo.DeviceName,
            monitorInfo.Monitor.Left,
            monitorInfo.Monitor.Top,
            monitorInfo.Monitor.Right - monitorInfo.Monitor.Left,
            monitorInfo.Monitor.Bottom - monitorInfo.Monitor.Top);
    }

    private static NativeInteropException MonitorSelectionFailure(
        string operationName,
        string technicalDetail) =>
        new(
            operationName,
            "Capture",
            Marshal.GetLastWin32Error(),
            technicalDetail,
            "Could not determine the target display for direct capture.",
            null);

    [DllImport(User32Library, SetLastError = true)]
    private static extern bool GetCursorPos(out PointInt32 point);

    [DllImport(User32Library, SetLastError = true)]
    private static extern IntPtr MonitorFromPoint(
        PointInt32 point,
        MonitorFromPointFlags flags);

    [DllImport(User32Library, SetLastError = true)]
    private static extern IntPtr MonitorFromWindow(
        IntPtr hwnd,
        MonitorFromWindowFlags flags);

    [DllImport(User32Library, SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(
        IntPtr monitor,
        ref MonitorInfoEx monitorInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct PointInt32
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfoEx
    {
        public int Size;
        public RectInt32 Monitor;
        public RectInt32 WorkArea;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RectInt32
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [Flags]
    private enum MonitorFromPointFlags : uint
    {
        MONITOR_DEFAULTTONULL = 0x00000000,
        MONITOR_DEFAULTTOPRIMARY = 0x00000001,
        MONITOR_DEFAULTTONEAREST = 0x00000002,
    }

    [Flags]
    private enum MonitorFromWindowFlags : uint
    {
        MONITOR_DEFAULTTONULL = 0x00000000,
        MONITOR_DEFAULTTOPRIMARY = 0x00000001,
        MONITOR_DEFAULTTONEAREST = 0x00000002,
    }
}
