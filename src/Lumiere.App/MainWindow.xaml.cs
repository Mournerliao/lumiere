using System.Diagnostics;
using System.Threading;
using Lumiere.Capture;
using Lumiere.Graphics.Clipboard;
using Lumiere.Graphics.Devices;
using Lumiere.Graphics.Hdr;
using Lumiere.Graphics.Presentation;
using Lumiere.Infrastructure.Interop;
using Lumiere.Overlay;
using Lumiere.Overlay.Crop;
using Lumiere.Overlay.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using Windows.UI;

namespace Lumiere.App;

public sealed partial class MainWindow : Window
{
    private readonly object previewSync = new();
    private readonly GraphicsDeviceProvider deviceProvider = new();
    private CaptureService? captureService;
    private CaptureSessionResources? captureSession;
    private GraphicsDeviceResources? deviceResources;
    private GraphicsEngine? graphicsEngine;
    private PreviewFramePresenter? previewFramePresenter;
    private SwapChainResources? swapChainResources;
    private CaptureTarget? activeCaptureTarget;
    private PreviewReadinessStatus? activePresentationEvidence;
    private CaptureSessionState sessionState = CaptureSessionState.Idle();
    private OverlayWindow? overlayWindow;
    private ClipboardOutputService? clipboardOutputService;
    private long previewGeneration;
    private long frameEventCount;
    private int previewSourceWidth;
    private int previewSourceHeight;
    private bool isClosed;
    private bool applyingSessionState;
    private int overlayDispatcherFallbackReported;
    private int uiDispatchFailureReported;

    public MainWindow()
    {
        InitializeComponent();
        Title = "Lumiere Tool - Dashboard - Capture Home";
        SystemBackdrop = new MicaBackdrop();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBarDragArea);
        ConfigureTitleBar();
        AppWindow.Resize(new SizeInt32(2560, 1440));
        Closed += OnWindowClosed;

        ApplySessionState(
            CaptureSessionState.Idle(PreviewReadinessStatus.Initializing(
                PreviewReadinessStage.Capture,
                "Click Capture to start HDR preview of your current display.",
                "Direct monitor capture bypasses the picker and starts preview immediately.")));
    }

    private async void OnSelectCaptureTargetClick(object sender, RoutedEventArgs e)
    {
        SetCaptureActionsEnabled(false);

        try
        {
            StopPreview(reportStopped: false);
            CloseOverlayWindow();
            ApplySessionState(
                CaptureSessionState.SelectingTarget(PreviewReadinessStatus.Initializing(
                    PreviewReadinessStage.Capture,
                    "Starting direct monitor capture...",
                    "Resolving current monitor for direct capture.")));

            var directService = new DirectMonitorCaptureTargetSelectionService(
                MonitorSelectionInterop.GetCurrentMonitorFromCursor,
                monitor => CaptureTarget.FromDisplayItem(
                    GraphicsCaptureMonitorInterop.CreateForMonitor(monitor),
                    monitor.DisplayName),
                fallbackPicker: new GraphicsCaptureTargetPicker(this));
            var result = await directService.SelectDirectMonitorTargetAsync();

            if (isClosed)
            {
                return;
            }

            if (result.IsSelected)
            {
                StartPreview(result.Target);
                return;
            }

            ApplySessionState(CaptureSessionState.FromSelectionResult(result));
        }
        catch (Exception exception)
        {
            if (!isClosed)
            {
                StopPreview(reportStopped: false);
                ApplySessionState(
                    CaptureSessionState.Failed(null, PreviewReadinessStatus.Failed(
                        PreviewReadinessStage.Capture,
                        "Preview failed",
                        InteropFailureDiagnostics.Write(exception))));
                CloseOverlayWindow();
            }
        }
        finally
        {
            if (!isClosed)
            {
                SetCaptureActionsEnabled(true);
            }
        }
    }

    private void ConfigureTitleBar()
    {
        var titleBar = AppWindow.TitleBar;
        titleBar.BackgroundColor = Color.FromArgb(0, 0, 0, 0);
        titleBar.ForegroundColor = Color.FromArgb(255, 160, 160, 160);
        titleBar.InactiveBackgroundColor = Color.FromArgb(0, 0, 0, 0);
        titleBar.InactiveForegroundColor = Color.FromArgb(255, 112, 112, 112);
        titleBar.ButtonBackgroundColor = Color.FromArgb(0, 0, 0, 0);
        titleBar.ButtonForegroundColor = Color.FromArgb(255, 160, 160, 160);
        titleBar.ButtonHoverBackgroundColor = Color.FromArgb(31, 255, 255, 255);
        titleBar.ButtonHoverForegroundColor = Color.FromArgb(255, 255, 255, 255);
        titleBar.ButtonPressedBackgroundColor = Color.FromArgb(46, 255, 255, 255);
        titleBar.ButtonPressedForegroundColor = Color.FromArgb(255, 255, 255, 255);
        titleBar.ButtonInactiveBackgroundColor = Color.FromArgb(0, 0, 0, 0);
        titleBar.ButtonInactiveForegroundColor = Color.FromArgb(255, 112, 112, 112);
    }

    private void StartPreview(CaptureTarget target)
    {
        if (isClosed)
        {
            return;
        }

        EnsureGraphicsServices();
        StopPreview(reportStopped: false);
        EnsureOverlayWindow(target);

        long currentGeneration;
        PreviewReadinessStatus presentationEvidence;
        lock (previewSync)
        {
            swapChainResources = graphicsEngine!.CreatePreviewSwapChain(
                new SwapChainCreationOptions(target.Size.Width, target.Size.Height),
                overlayWindow!.PreviewSurface);
            previewFramePresenter = new PreviewFramePresenter(deviceResources!, swapChainResources);
            activeCaptureTarget = target;
            presentationEvidence = swapChainResources.PresentationEvidence;
            activePresentationEvidence = presentationEvidence;
            previewSourceWidth = target.Size.Width;
            previewSourceHeight = target.Size.Height;

            Interlocked.Increment(ref previewGeneration);
            frameEventCount = 0;
            currentGeneration = Volatile.Read(ref previewGeneration);
        }

        ApplySessionState(CaptureSessionState.FromReadiness(target, presentationEvidence));

        var activeCaptureService = captureService ?? throw new InvalidOperationException("Capture service was not initialized.");
        var captureStart = activeCaptureService.StartCapture(
            target,
            frame => OnCapturedFrameArrived(currentGeneration, frame),
            readiness => ApplyFrameReadiness(currentGeneration, readiness),
            detail => ApplyFrameDiagnostic(currentGeneration, detail));
        CaptureSessionResources? captureSessionToDispose = null;
        SwapChainResources? swapChainToDispose = null;
        var shouldApplyCaptureReadiness = false;
        lock (previewSync)
        {
            if (currentGeneration != Volatile.Read(ref previewGeneration))
            {
                captureSessionToDispose = captureStart.SessionResources;
            }
            else if (captureStart.Started)
            {
                captureSession = captureStart.SessionResources;
                shouldApplyCaptureReadiness = true;
            }
            else
            {
                previewFramePresenter = null;
                swapChainToDispose = swapChainResources;
                swapChainResources = null;
                shouldApplyCaptureReadiness = true;
            }
        }

        captureSessionToDispose?.Dispose();
        swapChainToDispose?.Dispose();
        if (shouldApplyCaptureReadiness)
        {
            ApplySessionState(CreateSessionStateFromCurrentEvidence(target, captureStart.Readiness, allowCapturing: false));
        }
    }

    private void OnCapturedFrameArrived(long generation, CapturedFrameTexture frame)
    {
        var frameNumber = Interlocked.Increment(ref frameEventCount);
        CaptureTarget? target;
        PreviewRenderResult result;
        CaptureFrameSizeChange? sizeChange = null;
        using (frame)
        {
            lock (previewSync)
            {
                if (generation != Volatile.Read(ref previewGeneration) || previewFramePresenter is null)
                {
                    return;
                }

                sizeChange = CaptureFrameSizeChange.Evaluate(
                    previewSourceWidth,
                    previewSourceHeight,
                    frame.Width,
                    frame.Height);
                if (sizeChange.RequiresRecreation)
                {
                    result = null!;
                    target = null;
                }
                else
                {
                    result = previewFramePresenter.PresentFrame(frame);
                    target = activeCaptureTarget;
                }
            }
        }

        if (sizeChange?.RequiresRecreation == true)
        {
            QueuePreviewRecreation(generation, sizeChange);
            return;
        }

        if (!TryEnqueueUi(() =>
            {
                if (generation == Volatile.Read(ref previewGeneration))
                {
                    var readiness = AppendFrameDetail(result.Readiness, $"Presented frame #{frameNumber}.");
                    ApplySessionState(
                        target is null
                            ? CaptureSessionState.Failed(null, readiness)
                            : CreateSessionStateFromCurrentEvidence(target, readiness, allowCapturing: true));
                }
            }))
        {
            // The frame has already been presented and disposed; no UI status update is possible.
        }
    }

    private void QueuePreviewRecreation(
        long generation,
        CaptureFrameSizeChange sizeChange)
    {
        CaptureTarget? target;
        CaptureSessionResources? captureSessionToDispose;
        SwapChainResources? swapChainToDispose;
        long recreationGeneration;

        lock (previewSync)
        {
            if (generation != Volatile.Read(ref previewGeneration))
            {
                return;
            }

            Interlocked.Increment(ref previewGeneration);
            recreationGeneration = Volatile.Read(ref previewGeneration);
            target = activeCaptureTarget;
            captureSessionToDispose = captureSession;
            captureSession = null;
            previewFramePresenter = null;
            activeCaptureTarget = null;
            activePresentationEvidence = null;
            previewSourceWidth = 0;
            previewSourceHeight = 0;
            swapChainToDispose = swapChainResources;
            swapChainResources = null;
        }

        if (target is null)
        {
            captureSessionToDispose?.Dispose();
            swapChainToDispose?.Dispose();
            return;
        }

        var replacementTarget = target.WithSize(new SizeInt32
        {
            Width = sizeChange.ReplacementWidth,
            Height = sizeChange.ReplacementHeight,
        });
        var recreationRequest = CapturePreviewRecreationRequest.Create(
            replacementTarget,
            sizeChange,
            recreationGeneration);

        captureSessionToDispose?.Dispose();

        if (!TryEnqueueUi(() =>
            {
                swapChainToDispose?.Dispose();

                if (isClosed)
                {
                    return;
                }

                if (!recreationRequest.MatchesGeneration(Volatile.Read(ref previewGeneration)))
                {
                    return;
                }

                ApplySessionState(CaptureSessionState.Initializing(
                    recreationRequest.Target,
                    PreviewReadinessStatus.Initializing(
                        PreviewReadinessStage.Capture,
                        "Rebuilding preview",
                        $"Captured frame size changed to {sizeChange.ReplacementWidth}x{sizeChange.ReplacementHeight}; recreating WGC frame pool and FP16 scRGB swap chain resources.")));
                StartPreview(recreationRequest.Target);
            }))
        {
            swapChainToDispose?.DisposeAfterFailedUiDetach();
        }
    }

    private void ApplyFrameReadiness(long generation, PreviewReadinessStatus readiness)
    {
        if (!TryEnqueueUi(() =>
            {
                if (generation == Volatile.Read(ref previewGeneration))
                {
                    ApplySessionState(
                        sessionState.Target is null
                            ? CaptureSessionState.Failed(null, readiness)
                            : CreateSessionStateFromCurrentEvidence(sessionState.Target, readiness, allowCapturing: true));
                }
            }))
        {
        }
    }

    private void ApplyFrameDiagnostic(long generation, string detail)
    {
        var frameNumber = Interlocked.Increment(ref frameEventCount);
        if (!TryEnqueueUi(() =>
            {
                if (generation == Volatile.Read(ref previewGeneration))
                {
                    ApplySessionState(
                        sessionState.Target is null
                            ? CaptureSessionState.SelectingTarget(PreviewReadinessStatus.Initializing(
                                PreviewReadinessStage.Capture,
                                "Waiting for preview frame presentation.",
                                $"{detail} Frame event #{frameNumber}."))
                            : CaptureSessionState.Initializing(sessionState.Target, PreviewReadinessStatus.Initializing(
                            PreviewReadinessStage.Capture,
                            "Waiting for preview frame presentation.",
                            $"{detail} Frame event #{frameNumber}.")));
                }
            }))
        {
        }
    }

    private static PreviewReadinessStatus AppendFrameDetail(
        PreviewReadinessStatus readiness,
        string detail) =>
        readiness.State switch
        {
            PreviewReadinessState.Ready => PreviewReadinessStatus.Ready(
                readiness.UserMessage,
                $"{readiness.TechnicalDetail} {detail}"),
            PreviewReadinessState.Failed => PreviewReadinessStatus.Failed(
                readiness.Stage,
                readiness.UserMessage,
                $"{readiness.TechnicalDetail} {detail}"),
            PreviewReadinessState.Degraded => PreviewReadinessStatus.Degraded(
                readiness.Stage,
                readiness.UserMessage,
                $"{readiness.TechnicalDetail} {detail}"),
            PreviewReadinessState.Unsupported => PreviewReadinessStatus.Unsupported(
                readiness.Stage,
                readiness.UserMessage,
                $"{readiness.TechnicalDetail} {detail}"),
            _ => PreviewReadinessStatus.Initializing(
                readiness.Stage,
                readiness.UserMessage,
                $"{readiness.TechnicalDetail} {detail}"),
        };

    private CaptureSessionState CreateSessionStateFromCurrentEvidence(
        CaptureTarget target,
        PreviewReadinessStatus readiness,
        bool allowCapturing)
    {
        var presentationEvidence = activePresentationEvidence;
        if (presentationEvidence is { RequiresUserAttention: true })
        {
            return CaptureSessionState.FromReadiness(target, presentationEvidence);
        }

        return allowCapturing
            ? CaptureSessionState.FromReadiness(target, readiness)
            : CaptureSessionState.FromStartResult(target, CaptureStartResult.NotStarted(readiness));
    }

    private void EnsureGraphicsServices()
    {
        if (deviceResources is null)
        {
            deviceResources = deviceProvider.CreateDevice();
        }

        graphicsEngine ??= new GraphicsEngine(deviceResources);
        captureService ??= new CaptureService(deviceResources);
        clipboardOutputService ??= new ClipboardOutputService(deviceResources);
    }

    private void EnsureOverlayWindow(CaptureTarget target)
    {
        if (target.Size.Width <= 0 || target.Size.Height <= 0)
        {
            return;
        }

        var frameSize = new CaptureFrameSize(target.Size.Width, target.Size.Height);
        if (overlayWindow is not null)
        {
            overlayWindow.ApplyCaptureFrameSize(frameSize);
            return;
        }

        overlayWindow = new OverlayWindow();
        Interlocked.Exchange(ref overlayDispatcherFallbackReported, 0);
        Interlocked.Exchange(ref uiDispatchFailureReported, 0);
        overlayWindow.CloseRequested += OnOverlayCloseRequested;
        overlayWindow.CaptureConfirmed += OnOverlayCaptureConfirmed;
        overlayWindow.Closed += OnOverlayClosed;
        overlayWindow.ApplyCaptureFrameSize(frameSize);
        overlayWindow.ApplyPresenter(CreateOverlayPlacementRequest(target));
        overlayWindow.Activate();
        overlayWindow.ApplyState(CreateOverlayState(sessionState));
    }

    private void StopPreview(bool reportStopped = true)
    {
        CaptureSessionResources? captureSessionToDispose;
        SwapChainResources? swapChainToDispose;
        lock (previewSync)
        {
            Interlocked.Increment(ref previewGeneration);
            captureSessionToDispose = captureSession;
            captureSession = null;
            previewFramePresenter = null;
            activeCaptureTarget = null;
            activePresentationEvidence = null;
            swapChainToDispose = swapChainResources;
            swapChainResources = null;
        }

        captureSessionToDispose?.Dispose();
        swapChainToDispose?.Dispose();

        if (reportStopped && !isClosed)
        {
            ApplySessionState(CaptureSessionState.Disposed());
        }
    }

    private void ApplySessionState(CaptureSessionState state)
    {
        if (applyingSessionState)
        {
            return;
        }

        applyingSessionState = true;
        try
        {
            sessionState = state ?? throw new ArgumentNullException(nameof(state));
            var overlayState = CreateOverlayState(sessionState);
            overlayWindow?.ApplyState(overlayState);
            if (overlayWindow is not null && overlayState.RequiresFailureTeardown)
            {
                StopPreview(reportStopped: false);
                CloseOverlayWindow();
            }
        }
        finally
        {
            applyingSessionState = false;
        }
    }

    private void SetCaptureActionsEnabled(bool isEnabled)
    {
        SelectCaptureTargetButton.IsEnabled = isEnabled;
        RegionSelectButton.IsEnabled = isEnabled;
    }

    private bool TryEnqueueUi(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var overlayDispatcher = overlayWindow?.DispatcherQueue;
        if (overlayDispatcher is not null && overlayDispatcher.TryEnqueue(() => action()))
        {
            return true;
        }

        if (overlayDispatcher is not null &&
            Interlocked.Exchange(ref overlayDispatcherFallbackReported, 1) == 0)
        {
            Debug.WriteLine("Overlay dispatcher rejected UI work; falling back to RootGrid dispatcher.");
        }

        var enqueued = RootGrid.DispatcherQueue.TryEnqueue(() => action());
        if (!enqueued && Interlocked.Exchange(ref uiDispatchFailureReported, 1) == 0)
        {
            Debug.WriteLine("RootGrid dispatcher rejected UI work; preview status update was dropped.");
        }

        return enqueued;
    }

    private void OnOverlayCloseRequested(object? sender, EventArgs args)
    {
        if (isClosed)
        {
            return;
        }

        StopPreview();
        CloseOverlayWindow();
    }

    private void OnOverlayCaptureConfirmed(object? sender, ConfirmedCaptureSelection selection)
    {
        if (!ReferenceEquals(sender, overlayWindow) || isClosed)
        {
            return;
        }

        try
        {
            _ = TryCopyCropToClipboardAsync(selection);
            StopPreview(reportStopped: false);
        }
        finally
        {
            ApplySessionState(CaptureSessionState.Disposed(PreviewReadinessStatus.Ready(
                "Crop confirmed. Preview resources are stopping.",
                CreateConfirmedSelectionDetail(selection))));
            CloseOverlayWindow();
        }
    }

    private async Task TryCopyCropToClipboardAsync(ConfirmedCaptureSelection selection)
    {
        if (clipboardOutputService is null || swapChainResources is null)
        {
            return;
        }

        try
        {
            // Capture back buffer reference before async operation
            var backBuffer = swapChainResources.SwapChain.GetBuffer<Vortice.Direct3D11.ID3D11Texture2D>(0);
            try
            {
                await clipboardOutputService.TryCopyToClipboardAsync(
                    backBuffer,
                    selection.PixelRegion.X,
                    selection.PixelRegion.Y,
                    selection.PixelRegion.Width,
                    selection.PixelRegion.Height,
                    selection.FrameSize.Width,
                    selection.FrameSize.Height);
            }
            finally
            {
                backBuffer?.Dispose();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Clipboard output failed: {ex.Message}");
        }
    }

    private void OnOverlayClosed(object sender, WindowEventArgs args)
    {
        if (ReferenceEquals(sender, overlayWindow))
        {
            overlayWindow.CloseRequested -= OnOverlayCloseRequested;
            overlayWindow.CaptureConfirmed -= OnOverlayCaptureConfirmed;
            overlayWindow.Closed -= OnOverlayClosed;
            overlayWindow = null;

            if (!isClosed && sessionState.HasNativeSession)
            {
                StopPreview();
            }
        }
    }

    private void CloseOverlayWindow()
    {
        if (overlayWindow is null)
        {
            return;
        }

        overlayWindow.CloseRequested -= OnOverlayCloseRequested;
        overlayWindow.CaptureConfirmed -= OnOverlayCaptureConfirmed;
        overlayWindow.Closed -= OnOverlayClosed;
        var window = overlayWindow;
        overlayWindow = null;
        window.CloseSafely();
    }

    private static string CreateConfirmedSelectionDetail(ConfirmedCaptureSelection selection)
    {
        var degradedPrefix = selection.Status is OverlayDisplayStatus.DegradedPreview
            ? "Confirmed from degraded preview. "
            : string.Empty;
        return $"{degradedPrefix}DIP crop={selection.DipRegion.X:0.##},{selection.DipRegion.Y:0.##},{selection.DipRegion.Width:0.##}x{selection.DipRegion.Height:0.##}; pixel crop={selection.PixelRegion.X},{selection.PixelRegion.Y},{selection.PixelRegion.Width}x{selection.PixelRegion.Height}; frame={selection.FrameSize.Width}x{selection.FrameSize.Height}. {selection.TechnicalDetail}";
    }

    private static OverlayPlacementRequest CreateOverlayPlacementRequest(CaptureTarget target) =>
        new(
            target.Size,
            target.Kind is CaptureTargetKind.Display,
            target.DisplayName);

    private static OverlayState CreateOverlayState(CaptureSessionState sessionState)
    {
        ArgumentNullException.ThrowIfNull(sessionState);

        var message = sessionState.UserFacingReason ?? string.Empty;
        var detail = sessionState.TechnicalDetail ?? string.Empty;
        return sessionState.Status switch
        {
            CaptureSessionStatus.Capturing => OverlayState.HdrReady(message, detail),
            CaptureSessionStatus.Degraded => OverlayState.DegradedPreview(message, detail),
            CaptureSessionStatus.Unsupported => OverlayState.UnsupportedCapture(message, detail),
            CaptureSessionStatus.Failed => OverlayState.PreviewFailed(message, detail),
            CaptureSessionStatus.Disposed => OverlayState.Disposed(message, detail),
            _ => OverlayState.Initializing(message, detail),
        };
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        isClosed = true;
        StopPreview(reportStopped: false);
        CloseOverlayWindow();
        captureService = null;
        graphicsEngine = null;
        deviceResources?.Dispose();
        deviceResources = null;
    }

}
