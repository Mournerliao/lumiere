using Windows.Graphics.Capture;

namespace Lumiere.Infrastructure.Interop;

public interface ICaptureTargetPicker
{
    Task<GraphicsCaptureItem?> PickSingleItemAsync();
}
