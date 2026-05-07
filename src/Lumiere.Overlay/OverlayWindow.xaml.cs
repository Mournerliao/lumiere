using Lumiere.Infrastructure.Interop;
using Lumiere.Overlay.Crop;
using Lumiere.Overlay.Input;
using Lumiere.Overlay.Windowing;
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
        RootGrid.SizeChanged += OnRootGridSizeChanged;
        RootGrid.Loaded += OnRootGridLoaded;
        Closed += OnClosed;
        ApplyState(OverlayState.Initializing("Preparing HDR preview."));
    }

    public event EventHandler? CloseRequested;

    public event EventHandler<ConfirmedCaptureSelection>? CaptureConfirmed;

    public ISwapChainPreviewSurface PreviewSurface { get; }

    public void ApplyPresenter(OverlayPlacementRequest placement)
    {
        presenterTechnicalDetail = presenter.Apply(this, placement);
        UpdateTechnicalDetail();
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

    private void OnRootGridSizeChanged(object sender, SizeChangedEventArgs args)
    {
        currentPreviewLayout = OverlayPreviewLayout.FillSurface(args.NewSize.Width, args.NewSize.Height);
        PreviewSwapChainPanel.Width = currentPreviewLayout.PreviewBounds.Width;
        PreviewSwapChainPanel.Height = currentPreviewLayout.PreviewBounds.Height;
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

    private void OnEscapeKeyboardAcceleratorInvoked(
        KeyboardAccelerator sender,
        KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        RequestClose();
    }

    private void OnRootGridLoaded(object sender, RoutedEventArgs args) =>
        RootGrid.Focus(FocusState.Programmatic);

    private void OnCancelButtonClick(object sender, RoutedEventArgs args) =>
        RequestClose();

    private void OnConfirmButtonClick(object sender, RoutedEventArgs args) =>
        RequestCaptureConfirm();

    private void RequestCaptureConfirm()
    {
        if (isClosingRequested)
        {
            return;
        }

        if (!ConfirmedCaptureSelection.TryCreate(
                cropController.Selection,
                currentPreviewLayout.PreviewBounds,
                currentCaptureFrameSize,
                currentState,
                out var confirmed))
        {
            UpdateConfirmAvailability();
            return;
        }

        isClosingRequested = true;
        var closingMessage = confirmed.Status is OverlayDisplayStatus.DegradedPreview
            ? "Crop confirmed (degraded preview). Closing..."
            : "Crop confirmed. Closing...";
        ApplyState(OverlayState.Closing(
            closingMessage,
            confirmed.Status is OverlayDisplayStatus.DegradedPreview
                ? $"Confirmed degraded preview crop: {confirmed.TechnicalDetail}"
                : $"Confirmed crop: {confirmed.TechnicalDetail}"));
        CaptureConfirmed?.Invoke(this, confirmed);
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
        CropCanvas.CapturePointer(args.Pointer);
        UpdateCropVisuals();
        UpdateConfirmAvailability();
        args.Handled = true;
    }

    private void OnCropCanvasPointerMoved(object sender, PointerRoutedEventArgs args)
    {
        if (!IsActiveCropPointer(args) || !cropController.IsGestureActive)
        {
            return;
        }

        cropController.Update(
            args.GetCurrentPoint(CropCanvas).Position,
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

        var commitResult = cropController.Commit(
            args.GetCurrentPoint(CropCanvas).Position,
            currentPreviewLayout.PreviewBounds);
        CropCanvas.ReleasePointerCapture(args.Pointer);
        activeCropPointerId = null;
        UpdateCropVisuals();
        UpdateConfirmAvailability();

        if (commitResult is CropCommitResult.Activated or CropCommitResult.Adjusted)
        {
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

        cropController.Cancel();
        activeCropPointerId = null;
        UpdateCropVisuals();
        UpdateConfirmAvailability();
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
            return;
        }

        isClosingRequested = true;
        ApplyState(OverlayState.Closing("Closing overlay."));
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        isClosed = true;
    }
}
