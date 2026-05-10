using Microsoft.UI.Xaml;
using Windows.Graphics.Capture;
using WinRT.Interop;

namespace Lumiere.Infrastructure.Interop;

/// <summary>
/// Low-level Win32 interop for presenting the Windows GraphicsCapturePicker dialog.
/// This is a fallback/debug-only path retained for scenarios where direct monitor capture
/// is unavailable. The default MVP path bypasses the picker entirely.
/// </summary>
public static class GraphicsCapturePickerInterop
{
    public static async Task<GraphicsCaptureItem?> PickSingleItemAsync(Window owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        var picker = new GraphicsCapturePicker();
        InitializeWithWindow.Initialize(
            picker,
            WindowNative.GetWindowHandle(owner));

        return await picker.PickSingleItemAsync();
    }
}
