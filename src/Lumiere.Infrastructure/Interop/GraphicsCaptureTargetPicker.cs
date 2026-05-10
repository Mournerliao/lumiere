using Microsoft.UI.Xaml;
using Windows.Graphics.Capture;

namespace Lumiere.Infrastructure.Interop;

/// <summary>
/// WinUI-backed implementation of <see cref="ICaptureTargetPicker"/> using the system
/// GraphicsCapturePicker dialog. This is a fallback/debug-only path retained for scenarios
/// where direct monitor capture is unavailable.
/// </summary>
public sealed class GraphicsCaptureTargetPicker : ICaptureTargetPicker
{
    private readonly Window owner;

    public GraphicsCaptureTargetPicker(Window owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public Task<GraphicsCaptureItem?> PickSingleItemAsync()
    {
        return GraphicsCapturePickerInterop.PickSingleItemAsync(owner);
    }
}
