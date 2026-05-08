using System.Runtime.InteropServices;
using Lumiere.Infrastructure.Diagnostics;
using Lumiere.Infrastructure.Interop;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using WinRT.Interop;

namespace Lumiere.Overlay.Windowing;

public sealed class OverlayWindowPresenter
{
    private static readonly ILogger Logger = LumiereLoggerFactory.CreateLogger(LogCategories.Overlay);

    public double DpiScale { get; private set; } = 1.0;

    public string Apply(Window window, OverlayPlacementRequest placement)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(placement);

        var presenter = OverlappedPresenter.Create();
        presenter.SetBorderAndTitleBar(false, false);
        presenter.IsResizable = false;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;

        window.AppWindow.SetPresenter(presenter);
        var fallbackDisplayArea = DisplayArea.GetFromWindowId(
            window.AppWindow.Id,
            DisplayAreaFallback.Nearest);
        var allAreas = DisplayArea.FindAll();
        var displayAreas = new RectInt32[allAreas.Count];
        for (int i = 0; i < allAreas.Count; i++)
        {
            displayAreas[i] = allAreas[i].OuterBounds;
        }
        var overlayBounds = SelectOverlayBounds(
            placement,
            displayAreas,
            fallbackDisplayArea.OuterBounds);

        window.AppWindow.MoveAndResize(overlayBounds);

        var hwnd = WindowNative.GetWindowHandle(window);
        DpiScale = GetDpiForWindow(hwnd) / 96.0;
        var margins = new MARGINS { leftWidth = -1, rightWidth = -1, topHeight = -1, bottomHeight = -1 };
        DwmExtendFrameIntoClientArea(hwnd, ref margins);
        var ncRenderingPolicy = 1; // DWMNCRP_DISABLED
        DwmSetWindowAttribute(hwnd, 2, ref ncRenderingPolicy, sizeof(int)); // DWMWA_NCRENDERING_POLICY = 2
        var borderColor = unchecked((int)0xFFFFFFFE); // DWMWA_COLOR_NONE: suppress border drawing entirely
        DwmSetWindowAttribute(hwnd, 34, ref borderColor, sizeof(int)); // DWMWA_BORDER_COLOR = 34

        var zOrderDetail = WindowZOrderInterop.ApplyTopmostToolWindow(window, overlayBounds);
        var geometry = OverlayWindowGeometryDiagnostics.Capture(window, overlayBounds, DpiScale);
        Logger.LogInformation("{GeometryDiagnostic}", geometry.ToDiagnosticString());
        var presenterDetail = CreatePresenterApplication(
                placement.TargetDisplayName,
                OverlayHitTestModeDefaults.MvpDefault)
                .TechnicalDetail;
        return $"{presenterDetail} {zOrderDetail}";
    }

    internal static OverlayPresenterApplication CreatePresenterApplication(
        string targetDisplayName,
        OverlayHitTestMode hitTestMode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetDisplayName);

        var detail = hitTestMode switch
        {
            OverlayHitTestMode.Interactive =>
                $"Applied borderless topmost tool overlay for {targetDisplayName}; interactive hit testing is active for crop input.",
            OverlayHitTestMode.PassThrough =>
                $"Applied borderless topmost tool overlay for {targetDisplayName}; pass-through hit testing is active.",
            _ => throw new ArgumentOutOfRangeException(nameof(hitTestMode), hitTestMode, "Unknown overlay hit-test mode."),
        };

        return new OverlayPresenterApplication(hitTestMode, detail);
    }

    internal static RectInt32 SelectOverlayBounds(
        OverlayPlacementRequest placement,
        IReadOnlyList<RectInt32> displayAreaBounds,
        RectInt32 fallbackBounds)
    {
        ArgumentNullException.ThrowIfNull(placement);
        ArgumentNullException.ThrowIfNull(displayAreaBounds);

        if (placement.IsDisplayTarget)
        {
            foreach (var bounds in displayAreaBounds)
            {
                if (bounds.Width == placement.TargetSize.Width
                    && bounds.Height == placement.TargetSize.Height)
                {
                    return bounds;
                }
            }
        }

        return fallbackBounds;
    }

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    [DllImport("dwmapi.dll", ExactSpelling = true)]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

    [DllImport("dwmapi.dll", ExactSpelling = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct MARGINS
    {
        public int leftWidth;
        public int rightWidth;
        public int topHeight;
        public int bottomHeight;
    }
}
