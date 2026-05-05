using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace Lumiere.Overlay.Windowing;

public sealed class OverlayWindowPresenter
{
    public string Apply(Window window, OverlayPlacementRequest placement)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(placement);

        var presenter = OverlappedPresenter.Create();
        presenter.SetBorderAndTitleBar(false, false);
        presenter.IsAlwaysOnTop = true;

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

        return CreatePresenterApplication(
                placement.TargetDisplayName,
                OverlayHitTestModeDefaults.MvpDefault)
            .TechnicalDetail;
    }

    internal static OverlayPresenterApplication CreatePresenterApplication(
        string targetDisplayName,
        OverlayHitTestMode hitTestMode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetDisplayName);

        var detail = hitTestMode switch
        {
            OverlayHitTestMode.Interactive =>
                $"Applied borderless topmost overlay for {targetDisplayName}; interactive hit testing is active for crop input.",
            OverlayHitTestMode.PassThrough =>
                $"Applied borderless topmost overlay for {targetDisplayName}; pass-through hit testing is active.",
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
}
