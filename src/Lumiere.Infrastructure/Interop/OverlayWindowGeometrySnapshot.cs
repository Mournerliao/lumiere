using System.Globalization;
using Windows.Graphics;

namespace Lumiere.Infrastructure.Interop;

public sealed record OverlayWindowGeometrySnapshot(
    RectInt32 TargetBounds,
    NativeRect WindowBounds,
    NativeRect ClientBounds,
    NativePoint ClientOrigin,
    NativeRect? DwmExtendedFrameBounds,
    double DpiScale)
{
    public string ToDiagnosticString()
    {
        var dwmBounds = DwmExtendedFrameBounds?.ToString() ?? "unavailable";
        return string.Create(
            CultureInfo.InvariantCulture,
            $"Overlay geometry: target={Format(TargetBounds)}, hwnd={WindowBounds}, client={ClientBounds}, clientOrigin={ClientOrigin}, dwmFrame={dwmBounds}, dpiScale={DpiScale:0.###}.");
    }

    private static string Format(RectInt32 bounds) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{bounds.X},{bounds.Y},{bounds.Width}x{bounds.Height}");
}
