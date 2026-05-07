using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace Lumiere.Infrastructure.Interop;

public static class WindowCaptureExclusionInterop
{
    private const string OperationName = "SetWindowDisplayAffinity(WDA_EXCLUDEFROMCAPTURE)";
    private const uint WdaExcludeFromCapture = 0x00000011;

    public static string ExcludeFromCapture(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        var windowHandle = WindowNative.GetWindowHandle(window);
        if (windowHandle == IntPtr.Zero)
        {
            throw CreateFailure(0, "WindowNative.GetWindowHandle returned NULL for overlay window.");
        }

        if (!SetWindowDisplayAffinity(windowHandle, WdaExcludeFromCapture))
        {
            throw CreateFailure(
                Marshal.GetLastWin32Error(),
                $"SetWindowDisplayAffinity failed for overlay HWND 0x{windowHandle:X}.");
        }

        return "Overlay HWND is excluded from Windows Graphics Capture via WDA_EXCLUDEFROMCAPTURE.";
    }

    private static NativeInteropException CreateFailure(
        int hResult,
        string technicalDetail,
        Exception? innerException = null) =>
        new(
            OperationName,
            "Interop",
            hResult,
            technicalDetail,
            "Overlay window could not be excluded from capture.",
            innerException);

    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern bool SetWindowDisplayAffinity(
        IntPtr hwnd,
        uint affinity);
}
