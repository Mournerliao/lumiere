using Windows.Graphics.Capture;

namespace Lumiere.Infrastructure.Interop;

/// <summary>
/// Abstraction for the Windows Graphics Capture picker UI.
/// This is a fallback/debug-only interface retained for scenarios where direct monitor capture
/// is unavailable. The default current-baseline path bypasses the picker entirely.
/// </summary>
public interface ICaptureTargetPicker
{
    Task<GraphicsCaptureItem?> PickSingleItemAsync();
}
