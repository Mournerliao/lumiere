using Lumiere.Infrastructure.Diagnostics;
using Lumiere.Infrastructure.Interop;
using Lumiere.Overlay.Crop;
using Lumiere.Overlay.Input;
using Lumiere.Overlay.Windowing;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.System;
using Windows.UI;

namespace Lumiere.Overlay;

public sealed partial class OverlayWindow : Window
{
    private static readonly ILogger Logger = LumiereLoggerFactory.CreateLogger(LogCategories.Overlay);
    private const double CropHandleVisualSize = 12;

    private readonly OverlayWindowPresenter presenter;
    private readonly OverlayCancelRequestGate cancelRequestGate = new();
    private readonly CropController cropController = new();
    private bool isClosed;
    private bool isClosingRequested;
    private OverlayPreviewLayout currentPreviewLayout = OverlayPreviewLayout.FillSurface(1, 1);
    private CaptureFrameSize currentCaptureFrameSize = new(1, 1);
    private OverlayState currentState = OverlayState.Initializing("Preparing HDR preview.");
    private string presenterTechnicalDetail = string.Empty;
    private uint? activeCropPointerId;
    private Point? lastCropPointerPosition;

    public OverlayWindow()
        : this(new OverlayWindowPresenter())
    {
    }

    public OverlayWindow(OverlayWindowPresenter presenter)
    {
        this.presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
        InitializeComponent();

        Title = "Lumiere Capture Overlay";
        PreviewSurface = new SwapChainPanelPreviewSurface(PreviewSwapChainPanel);
        PreviewSwapChainPanel.Loaded += OnSwapChainPanelLoaded;
        PreviewSwapChainPanel.HorizontalAlignment = HorizontalAlignment.Left;
        PreviewSwapChainPanel.VerticalAlignment = VerticalAlignment.Top;
        RootGrid.SizeChanged += OnRootGridSizeChanged;
        RootGrid.Loaded += OnRootGridLoaded;
        Closed += OnClosed;
        ApplyState(OverlayState.Initializing("Preparing HDR preview."));
    }

    public event EventHandler? CloseRequested;

    public event EventHandler<ConfirmedCaptureSelection>? CaptureConfirmed;

    public ISwapChainPreviewSurface PreviewSurface { get; }

    public double DpiScale => presenter.DpiScale;

    public void ApplyPresenter(OverlayPlacementRequest placement)
    {
        presenterTechnicalDetail = presenter.Apply(this, placement);
        UpdateTechnicalDetail();
        UpdatePreviewLayout(RootGrid.ActualWidth, RootGrid.ActualHeight);
    }

    public void ExcludeFromCapture()
    {
        presenterTechnicalDetail = string.Join(
            " ",
            new[] { presenterTechnicalDetail, WindowCaptureExclusionInterop.ExcludeFromCapture(this) }
                .Where(detail => !string.IsNullOrWhiteSpace(detail)));
        UpdateTechnicalDetail();
    }

    public void ReassertTopmost()
    {
        var topmostDetail = WindowZOrderInterop.ApplyTopmost(this);
        Logger.LogDebug("Overlay topmost z-order reasserted.");
        Logger.LogDebug("{Detail}", topmostDetail);
    }

    public void ApplyState(OverlayState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        currentState = state;
        StatusLabelTextBlock.Text = state.Label;
        StatusMessageTextBlock.Text = state.Message;
        UpdateTechnicalDetail();
        ApplyStatusStyle(OverlayStatusStyle.FromStatus(state.Status));
        ApplyCropSelectionAvailability(state);
        UpdateConfirmAvailability();
    }

    public void ApplyCaptureFrameSize(CaptureFrameSize frameSize)
    {
        if (frameSize.Width <= 0 || frameSize.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frameSize));
        }

        currentCaptureFrameSize = frameSize;
        UpdatePreviewLayout(RootGrid.ActualWidth, RootGrid.ActualHeight);
    }

    public void CloseSafely()
    {
        if (isClosed)
        {
            return;
        }

        isClosed = true;
        Close();
    }

    private void OnRootGridSizeChanged(object sender, SizeChangedEventArgs args) =>
        UpdatePreviewLayout(args.NewSize.Width, args.NewSize.Height);

    private void UpdatePreviewLayout(double availableWidth, double availableHeight)
    {
        // Use the window DPI scale from GetDpiForWindow instead of SwapChainPanel.CompositionScale.
        // WinUI 3's CompositionScale can report 1.0 after the panel Loaded event even when
        // the actual DPI is higher (e.g. 1.5x), causing the preview to appear zoomed in.
        var dpiScale = presenter.DpiScale;
        WriteDebugLog(
            $"UpdatePreviewLayout: " +
            $"frameSize=({currentCaptureFrameSize.Width}x{currentCaptureFrameSize.Height}), " +
            $"dpiScale={dpiScale:0.###}, " +
            $"available=({availableWidth:0.##}x{availableHeight:0.##}), " +
            $"physicalAvailable=({availableWidth * dpiScale:0.##}x{availableHeight * dpiScale:0.##})");

        // Convert available size to physical pixels so FitFrameToSurface operates
        // in the same coordinate space as currentCaptureFrameSize (physical pixels).
        // The RenderTransform + DPI scaling then maps back to logical coordinates.
        var physicalAvailableWidth = availableWidth * dpiScale;
        var physicalAvailableHeight = availableHeight * dpiScale;
        currentPreviewLayout = OverlayPreviewLayout.FitFrameToSurface(
            currentCaptureFrameSize.Width,
            currentCaptureFrameSize.Height,
            physicalAvailableWidth,
            physicalAvailableHeight);
        PreviewSwapChainPanel.Margin = new Thickness(
            currentPreviewLayout.PreviewBounds.X,
            currentPreviewLayout.PreviewBounds.Y,
            0,
            0);
        PreviewSwapChainPanel.Width = currentPreviewLayout.PreviewBounds.Width;
        PreviewSwapChainPanel.Height = currentPreviewLayout.PreviewBounds.Height;

        if (dpiScale > 0 && Math.Abs(dpiScale - 1.0) > 0.001)
        {
            PreviewSwapChainPanel.RenderTransform = new ScaleTransform
            {
                ScaleX = 1.0 / dpiScale,
                ScaleY = 1.0 / dpiScale,
            };
        }
        else
        {
            PreviewSwapChainPanel.RenderTransform = null;
        }

        WriteDebugLog(
            $"Panel size: " +
            $"({PreviewSwapChainPanel.Width:0.##}x{PreviewSwapChainPanel.Height:0.##}), " +
            $"RootGrid=({RootGrid.ActualWidth:0.##}x{RootGrid.ActualHeight:0.##}), " +
            $"PreviewBounds: ({currentPreviewLayout.PreviewBounds.Width:0.##}x{currentPreviewLayout.PreviewBounds.Height:0.##}), " +
            $"PreviewOrigin: ({currentPreviewLayout.PreviewBounds.X:0.##},{currentPreviewLayout.PreviewBounds.Y:0.##}), " +
            $"RenderTransform: ScaleX={1.0 / dpiScale:0.###}, ScaleY={1.0 / dpiScale:0.###}");
        UpdateCropVisuals();
    }

    private void OnRootGridKeyDown(object sender, KeyRoutedEventArgs args)
    {
        if (OverlayKeyboardInputRouter.ShouldRequestCancel(args.Key))
        {
            args.Handled = true;
            RequestClose();
        }
    }

    private void OnRootGridLoaded(object sender, RoutedEventArgs args) =>
        RootGrid.Focus(FocusState.Programmatic);

    private void OnSwapChainPanelLoaded(object sender, RoutedEventArgs args)
    {
        // Diagnostic only: logs CompositionScale for debugging WinUI 3 DPI behavior.
        // This handler does NOT trigger UpdatePreviewLayout — the layout is driven by
        // ApplyPresenter, ApplyCaptureFrameSize, and RootGrid.SizeChanged.
        WriteDebugLog(
            $"SwapChainPanel loaded: " +
            $"CompositionScaleX={PreviewSwapChainPanel.CompositionScaleX}, " +
            $"CompositionScaleY={PreviewSwapChainPanel.CompositionScaleY}");
    }

    private void OnCancelButtonClick(object sender, RoutedEventArgs args) =>
        RequestClose();

    private void OnConfirmButtonClick(object sender, RoutedEventArgs args) =>
        RequestCaptureConfirm();

    private void RequestCaptureConfirm()
    {
        if (isClosingRequested)
        {
            Logger.LogWarning("RequestCaptureConfirm BLOCKED: isClosingRequested=true");
            return;
        }

        if (!ConfirmedCaptureSelection.TryCreate(
                cropController.Selection,
                currentPreviewLayout.PreviewBounds,
                currentCaptureFrameSize,
                currentState,
                presenter.DpiScale,
                presenter.DpiScale,
                out var confirmed))
        {
            Logger.LogWarning("RequestCaptureConfirm BLOCKED: TryCreate failed (selection invalid or status not confirmable)");
            UpdateConfirmAvailability();
            return;
        }

        Logger.LogInformation(
            "RequestCaptureConfirm: confirmed crop=({X},{Y},{Width}x{Height}), status={Status}",
            confirmed.PixelRegion.X, confirmed.PixelRegion.Y, confirmed.PixelRegion.Width, confirmed.PixelRegion.Height,
            confirmed.Status);

        isClosingRequested = true;
        ApplyState(OverlayState.Closing(
            CreateClipboardProgressMessage(confirmed.Status),
            confirmed.Status is OverlayDisplayStatus.DegradedPreview
                ? $"Confirmed degraded preview crop: {confirmed.TechnicalDetail}"
                : $"Confirmed crop: {confirmed.TechnicalDetail}"));
        CaptureConfirmed?.Invoke(this, confirmed);
    }

    public void ApplyClipboardResult(bool copied, OverlayDisplayStatus status)
    {
        ApplyState(OverlayState.Closing(
            copied
                ? CreateClipboardClosingMessage(status)
                : CreateClipboardFailureClosingMessage(status)));
    }

    private void OnCropCanvasPointerPressed(object sender, PointerRoutedEventArgs args)
    {
        if (!CropCanvas.IsHitTestVisible || activeCropPointerId is not null)
        {
            return;
        }

        var pointerPoint = args.GetCurrentPoint(CropCanvas);
        if (!pointerPoint.Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (!cropController.BeginGesture(pointerPoint.Position, currentPreviewLayout.PreviewBounds))
        {
            return;
        }

        activeCropPointerId = args.Pointer.PointerId;
        lastCropPointerPosition = pointerPoint.Position;
        CropCanvas.CapturePointer(args.Pointer);
        UpdateCropVisuals();
        UpdateConfirmAvailability();

        var pos = pointerPoint.Position;
        var bounds = currentPreviewLayout.PreviewBounds;
        Logger.LogDebug(
            "Crop gesture begin: pos=({X:0.#},{Y:0.#}), previewBounds=({BX:0.#},{BY:0.#},{BW:0.#},{BH:0.#})",
            pos.X, pos.Y, bounds.X, bounds.Y, bounds.Width, bounds.Height);

        args.Handled = true;
    }

    private void OnCropCanvasPointerMoved(object sender, PointerRoutedEventArgs args)
    {
        if (!IsActiveCropPointer(args) || !cropController.IsGestureActive)
        {
            return;
        }

        var pointerPosition = args.GetCurrentPoint(CropCanvas).Position;
        lastCropPointerPosition = pointerPosition;
        cropController.Update(
            pointerPosition,
            currentPreviewLayout.PreviewBounds);
        UpdateCropVisuals();
        UpdateConfirmAvailability();
        args.Handled = true;
    }

    private void OnCropCanvasPointerReleased(object sender, PointerRoutedEventArgs args)
    {
        if (!IsActiveCropPointer(args) || !cropController.IsGestureActive)
        {
            return;
        }

        var pointerPosition = args.GetCurrentPoint(CropCanvas).Position;
        lastCropPointerPosition = pointerPosition;
        var commitResult = cropController.Commit(
            pointerPosition,
            currentPreviewLayout.PreviewBounds);
        CropCanvas.ReleasePointerCapture(args.Pointer);
        activeCropPointerId = null;
        lastCropPointerPosition = null;
        UpdateCropVisuals();
        UpdateConfirmAvailability();

        var selection = cropController.Selection;
        var canConfirm = ConfirmedCaptureSelection.CanConfirm(selection, currentState.Status);
        Logger.LogDebug(
            "Crop commit: result={Result}, canConfirm={CanConfirm}, isClosingRequested={IsClosing}, overlayStatus={Status}",
            commitResult, canConfirm, isClosingRequested, currentState.Status);

        if (commitResult is CropCommitResult.Activated or CropCommitResult.Adjusted)
        {
            Logger.LogDebug("Release-to-capture: auto-confirming (commitResult={CommitResult})", commitResult);
            RequestCaptureConfirm();
        }

        args.Handled = true;
    }

    private void OnCropCanvasPointerCanceled(object sender, PointerRoutedEventArgs args)
    {
        if (!IsActiveCropPointer(args))
        {
            return;
        }

        cropController.Cancel();
        CropCanvas.ReleasePointerCapture(args.Pointer);
        activeCropPointerId = null;
        lastCropPointerPosition = null;
        UpdateCropVisuals();
        UpdateConfirmAvailability();
        args.Handled = true;
    }

    private void OnCropCanvasPointerCaptureLost(object sender, PointerRoutedEventArgs args)
    {
        if (!IsActiveCropPointer(args))
        {
            return;
        }

        var commitResult = CropCommitResult.NoGesture;
        if (lastCropPointerPosition is { } pointerPosition && cropController.IsGestureActive)
        {
            commitResult = cropController.Commit(pointerPosition, currentPreviewLayout.PreviewBounds);
            Logger.LogWarning(
                "Crop pointer capture lost; preserved interrupted gesture with result={Result}.",
                commitResult);
        }
        else
        {
            cropController.Cancel();
            Logger.LogWarning("Crop pointer capture lost with no last pointer position; gesture canceled.");
        }

        activeCropPointerId = null;
        lastCropPointerPosition = null;
        UpdateCropVisuals();
        UpdateConfirmAvailability();
        RootGrid.Focus(FocusState.Programmatic);
        ReassertTopmost();
        args.Handled = true;
    }

    private void ApplyStatusStyle(OverlayStatusStyle style)
    {
        StatusPanelBorder.Background = CreateBrush(style.BackgroundArgb);
        StatusPanelBorder.BorderBrush = CreateBrush(style.BorderArgb);
    }

    private void UpdateTechnicalDetail()
    {
        TechnicalDetailTextBlock.Text = string.Join(
            " ",
            new[] { currentState.TechnicalDetail, presenterTechnicalDetail }
                .Where(detail => !string.IsNullOrWhiteSpace(detail)));
    }

    private void ApplyCropSelectionAvailability(OverlayState state)
    {
        var isEnabled = state.Status is not (
            OverlayDisplayStatus.UnsupportedCapture
            or OverlayDisplayStatus.PreviewFailed
            or OverlayDisplayStatus.Closing
            or OverlayDisplayStatus.Disposed);

        CropCanvas.IsHitTestVisible = isEnabled;
        if (!isEnabled)
        {
            cropController.Clear();
            UpdateCropVisuals();
        }
    }

    private void UpdateConfirmAvailability()
    {
        ConfirmButton.IsEnabled = !isClosingRequested
            && !cropController.IsGestureActive
            && ConfirmedCaptureSelection.CanConfirm(cropController.Selection, currentState.Status);
    }

    private void UpdateCropVisuals()
    {
        var selection = cropController.DisplaySelection;
        if (!selection.IsVisible)
        {
            HideCropVisuals();
            return;
        }

        var bounds = currentPreviewLayout.PreviewBounds;
        var crop = selection.Geometry.Region;

        SetMaskRectangle(TopMaskRectangle, bounds.X, bounds.Y, bounds.Width, crop.Y - bounds.Y);
        SetMaskRectangle(LeftMaskRectangle, bounds.X, crop.Y, crop.X - bounds.X, crop.Height);
        SetMaskRectangle(
            RightMaskRectangle,
            crop.X + crop.Width,
            crop.Y,
            bounds.X + bounds.Width - (crop.X + crop.Width),
            crop.Height);
        SetMaskRectangle(
            BottomMaskRectangle,
            bounds.X,
            crop.Y + crop.Height,
            bounds.Width,
            bounds.Y + bounds.Height - (crop.Y + crop.Height));

        SetRectangleBounds(CropOuterBorderRectangle, crop);
        SetRectangleBounds(CropInnerBorderRectangle, crop);
        CropOuterBorderRectangle.Visibility = Visibility.Visible;
        CropInnerBorderRectangle.Visibility = Visibility.Visible;
        UpdateHandleVisuals(crop);
    }

    private void HideCropVisuals()
    {
        TopMaskRectangle.Visibility = Visibility.Collapsed;
        LeftMaskRectangle.Visibility = Visibility.Collapsed;
        RightMaskRectangle.Visibility = Visibility.Collapsed;
        BottomMaskRectangle.Visibility = Visibility.Collapsed;
        CropOuterBorderRectangle.Visibility = Visibility.Collapsed;
        CropInnerBorderRectangle.Visibility = Visibility.Collapsed;
        TopLeftCropHandleRectangle.Visibility = Visibility.Collapsed;
        TopCropHandleRectangle.Visibility = Visibility.Collapsed;
        TopRightCropHandleRectangle.Visibility = Visibility.Collapsed;
        RightCropHandleRectangle.Visibility = Visibility.Collapsed;
        BottomRightCropHandleRectangle.Visibility = Visibility.Collapsed;
        BottomCropHandleRectangle.Visibility = Visibility.Collapsed;
        BottomLeftCropHandleRectangle.Visibility = Visibility.Collapsed;
        LeftCropHandleRectangle.Visibility = Visibility.Collapsed;
    }

    private void UpdateHandleVisuals(Rect crop)
    {
        var centerX = crop.X + crop.Width / 2;
        var centerY = crop.Y + crop.Height / 2;
        var right = crop.X + crop.Width;
        var bottom = crop.Y + crop.Height;

        SetHandleBounds(TopLeftCropHandleRectangle, crop.X, crop.Y);
        SetHandleBounds(TopCropHandleRectangle, centerX, crop.Y);
        SetHandleBounds(TopRightCropHandleRectangle, right, crop.Y);
        SetHandleBounds(RightCropHandleRectangle, right, centerY);
        SetHandleBounds(BottomRightCropHandleRectangle, right, bottom);
        SetHandleBounds(BottomCropHandleRectangle, centerX, bottom);
        SetHandleBounds(BottomLeftCropHandleRectangle, crop.X, bottom);
        SetHandleBounds(LeftCropHandleRectangle, crop.X, centerY);
    }

    private static void SetHandleBounds(Rectangle rectangle, double centerX, double centerY)
    {
        rectangle.Visibility = Visibility.Visible;
        SetRectangleBounds(
            rectangle,
            new Rect(
                centerX - CropHandleVisualSize / 2,
                centerY - CropHandleVisualSize / 2,
                CropHandleVisualSize,
                CropHandleVisualSize));
    }

    private static void SetMaskRectangle(Rectangle rectangle, double x, double y, double width, double height)
    {
        rectangle.Visibility = width > 0 && height > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
        SetRectangleBounds(rectangle, new Rect(x, y, Math.Max(0, width), Math.Max(0, height)));
    }

    private static void SetRectangleBounds(Rectangle rectangle, Rect bounds)
    {
        Canvas.SetLeft(rectangle, bounds.X);
        Canvas.SetTop(rectangle, bounds.Y);
        rectangle.Width = bounds.Width;
        rectangle.Height = bounds.Height;
    }

    private static SolidColorBrush CreateBrush(uint argb) =>
        new(Color.FromArgb(
            (byte)(argb >> 24),
            (byte)(argb >> 16),
            (byte)(argb >> 8),
            (byte)argb));

    private bool IsActiveCropPointer(PointerRoutedEventArgs args) =>
        activeCropPointerId == args.Pointer.PointerId;

    private void RequestClose()
    {
        if (isClosingRequested || !cancelRequestGate.TryRequestCancel())
        {
            Logger.LogWarning("Escape cancel BLOCKED: isClosingRequested={IsClosing}", isClosingRequested);
            return;
        }

        isClosingRequested = true;
        Logger.LogDebug("Escape cancel: closing overlay");
        ApplyState(OverlayState.Closing("Closing overlay."));
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    internal static string CreateClipboardClosingMessage(OverlayDisplayStatus status) =>
        status is OverlayDisplayStatus.DegradedPreview
            ? "Copied to clipboard (degraded preview). Closing..."
            : "Copied to clipboard. Closing...";

    internal static string CreateClipboardProgressMessage(OverlayDisplayStatus status) =>
        status is OverlayDisplayStatus.DegradedPreview
            ? "Copying degraded preview to clipboard..."
            : "Copying to clipboard...";

    internal static string CreateClipboardFailureClosingMessage(OverlayDisplayStatus status) =>
        status is OverlayDisplayStatus.DegradedPreview
            ? "Clipboard copy failed (degraded preview). Closing..."
            : "Clipboard copy failed. Closing...";

    private void OnClosed(object sender, WindowEventArgs args)
    {
        isClosed = true;
    }

    private static void WriteDebugLog(string message)
    {
        Logger.LogInformation("{Message}", message);
    }
}
