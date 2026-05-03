using Microsoft.UI.Xaml;
using Windows.Graphics.Capture;

namespace Lumiere.Infrastructure.Interop;

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
