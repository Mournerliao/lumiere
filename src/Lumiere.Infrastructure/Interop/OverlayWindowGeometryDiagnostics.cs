using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using WinRT.Interop;

namespace Lumiere.Infrastructure.Interop;

public static class OverlayWindowGeometryDiagnostics
{
    private const int DwmwaExtendedFrameBounds = 9;

    public static OverlayWindowGeometrySnapshot Capture(
        Window window,
        RectInt32 targetBounds,
        double dpiScale)
    {
        ArgumentNullException.ThrowIfNull(window);

        var windowHandle = WindowNative.GetWindowHandle(window);
        if (windowHandle == IntPtr.Zero)
        {
            throw new NativeInteropException(
                "OverlayWindowGeometryDiagnostics",
                "OverlayGeometry",
                0,
                "Overlay HWND was unavailable while capturing geometry diagnostics.",
                "The crop overlay geometry could not be inspected.");
        }

        var windowRect = GetRequiredWindowRect(windowHandle);
        var clientRect = GetRequiredClientRect(windowHandle);
        var clientOrigin = GetRequiredClientOrigin(windowHandle);
        var dwmFrame = TryGetDwmExtendedFrameBounds(windowHandle);

        return new OverlayWindowGeometrySnapshot(
            targetBounds,
            ToNativeRect(windowRect),
            new NativeRect(clientOrigin.X, clientOrigin.Y, clientRect.right - clientRect.left, clientRect.bottom - clientRect.top),
            new NativePoint(clientOrigin.X, clientOrigin.Y),
            dwmFrame,
            dpiScale);
    }

    private static WindowNativeMethods.RECT GetRequiredWindowRect(IntPtr windowHandle)
    {
        if (!WindowNativeMethods.GetWindowRect(windowHandle, out var rect))
        {
            throw CreateFailure(nameof(WindowNativeMethods.GetWindowRect), windowHandle, "GetWindowRect failed while inspecting overlay geometry.");
        }

        return rect;
    }

    private static WindowNativeMethods.RECT GetRequiredClientRect(IntPtr windowHandle)
    {
        if (!WindowNativeMethods.GetClientRect(windowHandle, out var rect))
        {
            throw CreateFailure(nameof(WindowNativeMethods.GetClientRect), windowHandle, "GetClientRect failed while inspecting overlay geometry.");
        }

        return rect;
    }

    private static WindowNativeMethods.POINT GetRequiredClientOrigin(IntPtr windowHandle)
    {
        var point = new WindowNativeMethods.POINT();
        if (!WindowNativeMethods.ClientToScreen(windowHandle, ref point))
        {
            throw CreateFailure(nameof(WindowNativeMethods.ClientToScreen), windowHandle, "ClientToScreen failed while inspecting overlay geometry.");
        }

        return point;
    }

    private static NativeRect? TryGetDwmExtendedFrameBounds(IntPtr windowHandle)
    {
        var result = DwmGetWindowAttribute(
            windowHandle,
            DwmwaExtendedFrameBounds,
            out var rect,
            Marshal.SizeOf<WindowNativeMethods.RECT>());

        return result == 0
            ? ToNativeRect(rect)
            : null;
    }

    private static NativeInteropException CreateFailure(
        string operationName,
        IntPtr windowHandle,
        string technicalDetail) =>
        new(
            operationName,
            "OverlayGeometry",
            Marshal.GetHRForLastWin32Error(),
            $"{technicalDetail} HWND=0x{windowHandle:X}.",
            "The crop overlay geometry could not be inspected.");

    private static NativeRect ToNativeRect(WindowNativeMethods.RECT rect) =>
        new(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);

    [DllImport("dwmapi.dll", ExactSpelling = true)]
    private static extern int DwmGetWindowAttribute(
        IntPtr hwnd,
        int dwAttribute,
        out WindowNativeMethods.RECT pvAttribute,
        int cbAttribute);
}
