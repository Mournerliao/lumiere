using Lumiere.Infrastructure.Interop;
using Windows.Graphics;
using Xunit;

namespace Lumiere.Overlay.Tests;

public sealed class OverlayWindowGeometryDiagnosticsTests
{
    [Fact]
    public void DiagnosticStringIncludesTargetWindowClientDwmAndDpiFields()
    {
        var snapshot = new OverlayWindowGeometrySnapshot(
            new RectInt32 { X = 10, Y = 20, Width = 300, Height = 200 },
            new NativeRect(11, 22, 300, 200),
            new NativeRect(12, 23, 298, 198),
            new NativePoint(12, 23),
            new NativeRect(11, 22, 300, 200),
            DpiScale: 1.5);

        var diagnostic = snapshot.ToDiagnosticString();

        Assert.Contains("target=10,20,300x200", diagnostic, StringComparison.Ordinal);
        Assert.Contains("hwnd=11,22,300x200", diagnostic, StringComparison.Ordinal);
        Assert.Contains("client=12,23,298x198", diagnostic, StringComparison.Ordinal);
        Assert.Contains("clientOrigin=12,23", diagnostic, StringComparison.Ordinal);
        Assert.Contains("dwmFrame=11,22,300x200", diagnostic, StringComparison.Ordinal);
        Assert.Contains("dpiScale=1.5", diagnostic, StringComparison.Ordinal);
    }
}
