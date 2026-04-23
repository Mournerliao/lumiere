using Microsoft.UI.Xaml;
using Windows.Graphics.Capture;
using WinRT.Interop;

namespace Lumiere.Infrastructure.Interop;

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
