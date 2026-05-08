using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using WinRT.Interop;

namespace Lumiere.Infrastructure.Interop;

public static class WindowZOrderInterop
{
    private const int GwlStyle = -16;
    private const int GwlExStyle = -20;
    private const long WsPopup = unchecked((long)0x80000000);
    private const long WsCaption = 0x00C00000L;
    private const long WsThickFrame = 0x00040000L;
    private const long WsMinimizeBox = 0x00020000L;
    private const long WsMaximizeBox = 0x00010000L;
    private const long WsSysMenu = 0x00080000L;
    private const long WsExToolWindow = 0x00000080L;
    private const long WsExAppWindow = 0x00040000L;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpFrameChanged = 0x0020;
    private static readonly IntPtr HwndTopmost = new(-1);

    public static string ApplyTopmostToolWindow(Window window)
    {
        var styleDetail = ApplyPopupToolWindowStyle(window);
        var topmostDetail = ApplyTopmost(window);
        return $"{styleDetail} {topmostDetail}";
    }

    public static string ApplyTopmostToolWindow(Window window, RectInt32 bounds)
    {
        var styleDetail = ApplyPopupToolWindowStyle(window);
        var boundsDetail = ApplyTopmostBounds(window, bounds);
        return $"{styleDetail} {boundsDetail}";
    }

    public static string ApplyTopmost(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        var windowHandle = WindowNative.GetWindowHandle(window);
        if (windowHandle == IntPtr.Zero)
        {
            throw new NativeInteropException(
                nameof(SetWindowPos),
                "OverlayWindowZOrder",
                0,
                "Overlay HWND was unavailable while applying topmost z-order.",
                "The crop overlay could not be placed above other windows.");
        }

        if (!SetWindowPos(windowHandle, HwndTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate))
        {
            var hResult = Marshal.GetHRForLastWin32Error();
            throw new NativeInteropException(
                nameof(SetWindowPos),
                "OverlayWindowZOrder",
                hResult,
                $"SetWindowPos(HWND_TOPMOST) failed for overlay HWND 0x{windowHandle:X}.",
                "The crop overlay could not be placed above other windows.");
        }

        return "Overlay HWND was placed in the topmost z-order band for stable crop input.";
    }

    private static string ApplyTopmostBounds(Window window, RectInt32 bounds)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bounds));
        }

        var windowHandle = WindowNative.GetWindowHandle(window);
        if (windowHandle == IntPtr.Zero)
        {
            throw new NativeInteropException(
                nameof(SetWindowPos),
                "OverlayWindowBounds",
                0,
                "Overlay HWND was unavailable while applying exact topmost bounds.",
                "The crop overlay could not be aligned to the target display.");
        }

        var (ncInsetLeft, ncInsetTop, ncInsetRight, ncInsetBottom) = GetNonClientInset(windowHandle);

        var adjustedX = bounds.X - ncInsetLeft;
        var adjustedY = bounds.Y - ncInsetTop;
        var adjustedWidth = bounds.Width + ncInsetLeft + ncInsetRight;
        var adjustedHeight = bounds.Height + ncInsetTop + ncInsetBottom;

        if (!SetWindowPos(windowHandle, HwndTopmost, adjustedX, adjustedY, adjustedWidth, adjustedHeight, SwpNoActivate | SwpFrameChanged))
        {
            throw new NativeInteropException(
                nameof(SetWindowPos),
                "OverlayWindowBounds",
                Marshal.GetHRForLastWin32Error(),
                $"SetWindowPos(HWND_TOPMOST) failed for overlay HWND 0x{windowHandle:X} at {adjustedX},{adjustedY},{adjustedWidth}x{adjustedHeight}.",
                "The crop overlay could not be aligned to the target display.");
        }

        return $"Overlay HWND was aligned to target bounds {bounds.X},{bounds.Y},{bounds.Width}x{bounds.Height} (adjusted for nc inset {ncInsetLeft},{ncInsetTop}).";
    }

    private static (int Left, int Top, int Right, int Bottom) GetNonClientInset(IntPtr windowHandle)
    {
        if (!WindowNativeMethods.GetWindowRect(windowHandle, out var windowRect))
        {
            throw new NativeInteropException(
                nameof(WindowNativeMethods.GetWindowRect),
                "OverlayWindowBounds",
                Marshal.GetHRForLastWin32Error(),
                $"GetWindowRect failed for overlay HWND 0x{windowHandle:X}.",
                "The crop overlay could not be aligned to the target display.");
        }

        if (!WindowNativeMethods.GetClientRect(windowHandle, out var clientRect))
        {
            throw new NativeInteropException(
                nameof(WindowNativeMethods.GetClientRect),
                "OverlayWindowBounds",
                Marshal.GetHRForLastWin32Error(),
                $"GetClientRect failed for overlay HWND 0x{windowHandle:X}.",
                "The crop overlay could not be aligned to the target display.");
        }

        var clientOrigin = new WindowNativeMethods.POINT();
        if (!WindowNativeMethods.ClientToScreen(windowHandle, ref clientOrigin))
        {
            throw new NativeInteropException(
                nameof(WindowNativeMethods.ClientToScreen),
                "OverlayWindowBounds",
                Marshal.GetHRForLastWin32Error(),
                $"ClientToScreen failed for overlay HWND 0x{windowHandle:X}.",
                "The crop overlay could not be aligned to the target display.");
        }

        var ncInsetLeft = clientOrigin.X - windowRect.left;
        var ncInsetTop = clientOrigin.Y - windowRect.top;
        var ncInsetRight = (windowRect.right - windowRect.left) - clientRect.right - ncInsetLeft;
        var ncInsetBottom = (windowRect.bottom - windowRect.top) - clientRect.bottom - ncInsetTop;

        return (ncInsetLeft, ncInsetTop, ncInsetRight, ncInsetBottom);
    }

    private static string ApplyPopupToolWindowStyle(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        var windowHandle = WindowNative.GetWindowHandle(window);
        if (windowHandle == IntPtr.Zero)
        {
            throw new NativeInteropException(
                nameof(SetWindowLongPtr),
                "OverlayWindowStyle",
                0,
                "Overlay HWND was unavailable while applying tool-window extended style.",
                "The crop overlay could not be styled as a tool window.");
        }

        var currentStyle = ReadWindowLongPtr(windowHandle, GwlStyle, "GWL_STYLE");
        var currentExStyle = ReadWindowLongPtr(windowHandle, GwlExStyle, "GWL_EXSTYLE");

        var updatedStyle = new IntPtr(
            (currentStyle.ToInt64()
                | WsPopup)
            & ~WsCaption
            & ~WsThickFrame
            & ~WsMinimizeBox
            & ~WsMaximizeBox
            & ~WsSysMenu);
        var updatedExStyle = new IntPtr((currentExStyle.ToInt64() | WsExToolWindow) & ~WsExAppWindow);

        WriteWindowLongPtr(windowHandle, GwlStyle, updatedStyle, "GWL_STYLE");
        WriteWindowLongPtr(windowHandle, GwlExStyle, updatedExStyle, "GWL_EXSTYLE");

        if (!SetWindowPos(windowHandle, IntPtr.Zero, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoZOrder | SwpNoActivate | SwpFrameChanged))
        {
            throw new NativeInteropException(
                nameof(SetWindowPos),
                "OverlayWindowStyle",
                Marshal.GetHRForLastWin32Error(),
                $"SetWindowPos(SWP_FRAMECHANGED) failed for overlay HWND 0x{windowHandle:X}.",
                "The crop overlay could not refresh its tool-window style.");
        }

        return "Overlay HWND was styled as a popup tool window to avoid game-like fullscreen window behavior and client-area insets.";
    }

    private static IntPtr ReadWindowLongPtr(IntPtr windowHandle, int index, string indexName)
    {
        SetLastError(0);
        var value = GetWindowLongPtr(windowHandle, index);
        var error = Marshal.GetLastWin32Error();
        if (value == IntPtr.Zero && error != 0)
        {
            throw new NativeInteropException(
                nameof(GetWindowLongPtr),
                "OverlayWindowStyle",
                Marshal.GetHRForLastWin32Error(),
                $"GetWindowLongPtr({indexName}) failed for overlay HWND 0x{windowHandle:X}.",
                "The crop overlay could not inspect its window style.");
        }

        return value;
    }

    private static void WriteWindowLongPtr(IntPtr windowHandle, int index, IntPtr value, string indexName)
    {
        SetLastError(0);
        var previous = SetWindowLongPtr(windowHandle, index, value);
        var error = Marshal.GetLastWin32Error();
        if (previous == IntPtr.Zero && error != 0)
        {
            throw new NativeInteropException(
                nameof(SetWindowLongPtr),
                "OverlayWindowStyle",
                Marshal.GetHRForLastWin32Error(),
                $"SetWindowLongPtr({indexName}) failed for overlay HWND 0x{windowHandle:X}.",
                "The crop overlay could not be styled as a popup tool window.");
        }
    }

    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint flags);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern void SetLastError(uint dwErrCode);
}
