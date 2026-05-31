using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace Lumiere.Infrastructure.Interop;

public static class WindowFrameInterop
{
    public const int DefaultRoundedCornerRadiusDips = 12;

    private const int GwlStyle = -16;
    private const long WsCaption = 0x00C00000L;
    private const long WsThickFrame = 0x00040000L;
    private const long WsMinimizeBox = 0x00020000L;
    private const long WsMaximizeBox = 0x00010000L;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpFrameChanged = 0x0020;
    private const int DwmwaNcRenderingPolicy = 2;
    private const int DwmncrpDisabled = 1;
    private const int DwmwaBorderColor = 34;
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwaColorNone = unchecked((int)0xFFFFFFFE);
    private const int DwmwcpRound = 2;

    public static string SuppressNonClientBorder(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        var windowHandle = WindowNative.GetWindowHandle(window);
        if (windowHandle == IntPtr.Zero)
        {
            throw new NativeInteropException(
                "WindowNative.GetWindowHandle",
                "MainWindowFrame",
                0,
                "Main window HWND was unavailable while suppressing the DWM border.",
                "The main window border could not be suppressed.");
        }

        var currentStyle = ReadWindowStyle(windowHandle);
        var updatedStyle = new IntPtr(
            currentStyle.ToInt64()
            & ~WsCaption
            & ~WsThickFrame
            & ~WsMinimizeBox
            & ~WsMaximizeBox);

        var styleChanged = updatedStyle != currentStyle;
        try
        {
            if (styleChanged)
            {
                WriteWindowStyle(windowHandle, updatedStyle);
                RefreshFrame(windowHandle);
            }

            var ncRenderingPolicy = DwmncrpDisabled;
            var ncResult = WindowNativeMethods.DwmSetWindowAttribute(
                windowHandle,
                DwmwaNcRenderingPolicy,
                ref ncRenderingPolicy,
                sizeof(int));

            var borderColor = DwmwaColorNone;
            var borderResult = WindowNativeMethods.DwmSetWindowAttribute(
                windowHandle,
                DwmwaBorderColor,
                ref borderColor,
                sizeof(int));

            if (ncResult < 0 || borderResult < 0)
            {
                var hResult = ncResult < 0 ? ncResult : borderResult;
                throw new NativeInteropException(
                    nameof(WindowNativeMethods.DwmSetWindowAttribute),
                    "MainWindowFrame",
                    hResult,
                    $"DwmSetWindowAttribute failed for main window HWND 0x{windowHandle:X}; ncResult=0x{ncResult:X8}, borderResult=0x{borderResult:X8}.",
                    "The main window border could not be suppressed.");
            }
        }
        catch
        {
            if (styleChanged)
            {
                TryRestoreWindowStyle(windowHandle, currentStyle);
            }

            throw;
        }

        return $"Main window non-client frame was suppressed (style 0x{currentStyle.ToInt64():X} -> 0x{updatedStyle.ToInt64():X}).";
    }

    public static void PreferRoundedCorners(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        PreferRoundedCorners(WindowNative.GetWindowHandle(window));
    }

    public static void PreferRoundedCorners(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return;
        }

        try
        {
            var preference = DwmwcpRound;
            WindowNativeMethods.DwmSetWindowAttribute(
                windowHandle,
                DwmwaWindowCornerPreference,
                ref preference,
                sizeof(int));
        }
        catch (DllNotFoundException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }
    }

    public static void ExtendFrameIntoClientArea(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return;
        }

        var margins = new WindowNativeMethods.MARGINS
        {
            cxLeftWidth = -1,
            cxRightWidth = -1,
            cyTopHeight = -1,
            cyBottomHeight = -1
        };

        var hr = WindowNativeMethods.DwmExtendFrameIntoClientArea(windowHandle, ref margins);
        if (hr < 0)
        {
            System.Diagnostics.Debug.WriteLine($"DwmExtendFrameIntoClientArea failed with HRESULT 0x{hr:X8}.");
        }
    }

    public static void SuppressDwmBorder(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return;
        }

        try
        {
            var ncRenderingPolicy = DwmncrpDisabled;
            WindowNativeMethods.DwmSetWindowAttribute(
                windowHandle,
                DwmwaNcRenderingPolicy,
                ref ncRenderingPolicy,
                sizeof(int));

            var borderColor = DwmwaColorNone;
            WindowNativeMethods.DwmSetWindowAttribute(
                windowHandle,
                DwmwaBorderColor,
                ref borderColor,
                sizeof(int));
        }
        catch (DllNotFoundException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }
    }

    public static void ApplyRoundedRegion(
        Window window,
        int widthPixels,
        int heightPixels,
        double scale,
        int cornerRadiusDips = DefaultRoundedCornerRadiusDips)
    {
        ArgumentNullException.ThrowIfNull(window);
        ApplyRoundedRegion(WindowNative.GetWindowHandle(window), widthPixels, heightPixels, scale, cornerRadiusDips);
    }

    public static void ApplyRoundedRegion(
        IntPtr windowHandle,
        int widthPixels,
        int heightPixels,
        double scale,
        int cornerRadiusDips = DefaultRoundedCornerRadiusDips)
    {
        if (windowHandle == IntPtr.Zero || widthPixels <= 0 || heightPixels <= 0)
        {
            return;
        }

        var radius = Math.Max(1, (int)Math.Ceiling(cornerRadiusDips * scale));
        var region = WindowNativeMethods.CreateRoundRectRgn(
            0,
            0,
            widthPixels + 1,
            heightPixels + 1,
            radius * 2,
            radius * 2);
        if (region == IntPtr.Zero)
        {
            return;
        }

        if (WindowNativeMethods.SetWindowRgn(windowHandle, region, true) == 0)
        {
            WindowNativeMethods.DeleteObject(region);
        }
    }

    private static IntPtr ReadWindowStyle(IntPtr windowHandle)
    {
        WindowNativeMethods.SetLastError(0);
        var value = WindowNativeMethods.GetWindowLongPtr(windowHandle, GwlStyle);
        var error = Marshal.GetLastWin32Error();
        if (value == IntPtr.Zero && error != 0)
        {
            throw new NativeInteropException(
                nameof(WindowNativeMethods.GetWindowLongPtr),
                "MainWindowFrame",
                Marshal.GetHRForLastWin32Error(),
                $"GetWindowLongPtr(GWL_STYLE) failed for main window HWND 0x{windowHandle:X}.",
                "The main window border could not be suppressed.");
        }

        return value;
    }

    private static void WriteWindowStyle(IntPtr windowHandle, IntPtr value)
    {
        WindowNativeMethods.SetLastError(0);
        var previous = WindowNativeMethods.SetWindowLongPtr(windowHandle, GwlStyle, value);
        var error = Marshal.GetLastWin32Error();
        if (previous == IntPtr.Zero && error != 0)
        {
            throw new NativeInteropException(
                nameof(WindowNativeMethods.SetWindowLongPtr),
                "MainWindowFrame",
                Marshal.GetHRForLastWin32Error(),
                $"SetWindowLongPtr(GWL_STYLE) failed for main window HWND 0x{windowHandle:X}.",
                "The main window border could not be suppressed.");
        }
    }

    private static void RefreshFrame(IntPtr windowHandle)
    {
        if (!WindowNativeMethods.SetWindowPos(
            windowHandle,
            IntPtr.Zero,
            0,
            0,
            0,
            0,
            SwpNoMove | SwpNoSize | SwpNoZOrder | SwpNoActivate | SwpFrameChanged))
        {
            throw new NativeInteropException(
                nameof(WindowNativeMethods.SetWindowPos),
                "MainWindowFrame",
                Marshal.GetHRForLastWin32Error(),
                $"SetWindowPos(SWP_FRAMECHANGED) failed for main window HWND 0x{windowHandle:X}.",
                "The main window border could not be suppressed.");
        }
    }

    private static void TryRestoreWindowStyle(IntPtr windowHandle, IntPtr value)
    {
        try
        {
            WriteWindowStyle(windowHandle, value);
            RefreshFrame(windowHandle);
        }
        catch
        {
            // Best-effort rollback; the original suppression failure is more actionable to callers.
        }
    }
}
