using System.Diagnostics;
using System.Threading;
using Lumiere.Capture;
using Lumiere.Graphics.Clipboard;
using Lumiere.Graphics.Devices;
using Lumiere.Graphics.Hdr;
using Lumiere.Graphics.Output;
using Lumiere.Graphics.Presentation;
using Lumiere.Infrastructure.Diagnostics;
using Lumiere.Infrastructure.Interop;
using Lumiere.Overlay;
using Lumiere.Overlay.Crop;
using Lumiere.Overlay.Windowing;
using Lumiere.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace Lumiere.App;

public sealed partial class MainWindow : Window
{
    private static readonly ILogger Logger = LumiereLoggerFactory.CreateLogger(LogCategories.App);
    private const string StatusCheckmarkCircleGlyph = "\uE930";
    private const string StatusMonitorGlyph = "\uE7F4";
    private const string StatusErrorCircleGlyph = "\uEA39";
    private const string StatusClockGlyph = "\uE121";
    private const int MainPanelWidthDips = 360;
    private const int MainPanelHeightDips = 310;
    private const int SettingsPanelHeightDips = 640;
    private const int WorkAreaMarginPixels = 16;

    private readonly object previewSync = new();
    private readonly ICaptureCommandCoordinator captureCommandCoordinator;
    private readonly IOutputService outputService;
    private readonly ISettingsProvider settingsProvider;
    private readonly IHdrAlertSettingsWriter hdrAlertSettingsWriter;
    private readonly IAboutInfoProvider aboutInfoProvider;
    private readonly GraphicsDeviceResources deviceResources;
    private readonly GraphicsEngine graphicsEngine;
    private CaptureService? captureService;
    private CaptureSessionResources? captureSession;
    private PreviewFramePresenter? previewFramePresenter;
    private SwapChainResources? swapChainResources;
    private CaptureTarget? activeCaptureTarget;
    private PreviewReadinessStatus? activePresentationEvidence;
    private OverlayWindow? overlayWindow;
    private long previewGeneration;
    private long frameEventCount;
    private int previewSourceWidth;
    private int previewSourceHeight;
    private bool isClosed;
    private bool isDeviceDisposed;
    private bool applyingSessionState;
    private bool applyingSettingsProjection;
    private bool sizingMainPanel;
    private AppShellView activeShellView = AppShellView.Main;
    private int overlayDispatcherFallbackReported;
    private int uiDispatchFailureReported;
    private InputNonClientPointerSource? nonClientPointerSource;

    public MainWindow(
        ICaptureCommandCoordinator captureCommandCoordinator,
        IOutputService outputService,
        ISettingsProvider settingsProvider,
        IHdrAlertSettingsWriter hdrAlertSettingsWriter,
        IAboutInfoProvider aboutInfoProvider,
        CaptureService captureService,
        GraphicsDeviceResources deviceResources)
    {
        this.captureCommandCoordinator = captureCommandCoordinator ?? throw new ArgumentNullException(nameof(captureCommandCoordinator));
        this.outputService = outputService ?? throw new ArgumentNullException(nameof(outputService));
        this.settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
        this.hdrAlertSettingsWriter = hdrAlertSettingsWriter ?? throw new ArgumentNullException(nameof(hdrAlertSettingsWriter));
        this.aboutInfoProvider = aboutInfoProvider ?? throw new ArgumentNullException(nameof(aboutInfoProvider));
        this.captureService = captureService ?? throw new ArgumentNullException(nameof(captureService));
        this.deviceResources = deviceResources ?? throw new ArgumentNullException(nameof(deviceResources));
        this.graphicsEngine = new GraphicsEngine(deviceResources);
        InitializeComponent();
        Title = "Lumiere";
        SystemBackdrop = new MicaBackdrop();
        ConfigureWindowPresenter();
        SuppressWindowFrameBorder();
        ApplyShortcutLabels();
        SizeToActiveShellView();
        RootGrid.Loaded += OnRootGridLoaded;
        HeaderDragArea.SizeChanged += OnHeaderDragAreaSizeChanged;
        SettingsButton.SizeChanged += OnHeaderDragAreaSizeChanged;
        SettingsHeaderDragArea.SizeChanged += OnHeaderDragAreaSizeChanged;
        SettingsBackButton.SizeChanged += OnHeaderDragAreaSizeChanged;
        Closed += OnWindowClosed;

        LumiereLoggerFactory.InitializeWithHeader(
            LogLevel.Information,
            "Lumiere Validation Log",
            $"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            $"OS: {Environment.OSVersion}",
            $".NET: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}",
            $"HdrConstants: WgcPixelFormat={HdrConstants.WgcFramePoolPixelFormat}, DxgiFormat={HdrConstants.DxgiSwapChainFormat}, ColorSpace={HdrConstants.DxgiColorSpace}",
            "NOTE: If NVIDIA GeForce Experience overlay appears (Alt+Z), disable 'In-Game Overlay' in GeForce Experience settings for stable capture.",
            "NOTE: Windows Graphics Capture may show a yellow system border. This unpackaged build cannot guarantee graphicsCaptureWithoutBorder consent.");

        ValidationLogger.SetBridgeLogger(Logger);

        ApplySessionState(
            CaptureSessionState.Idle(PreviewReadinessStatus.Initializing(
                PreviewReadinessStage.Capture,
                "Select a capture mode to begin.",
                "Direct monitor capture bypasses the picker and starts preview immediately.")));
    }

    private async void OnSelectCaptureTargetClick(object sender, RoutedEventArgs e)
    {
        await ExecuteCaptureFromUiAsync(CaptureCommandMode.Fullscreen);
    }

    private async void OnRegionCaptureClick(object sender, RoutedEventArgs e)
    {
        await ExecuteCaptureFromUiAsync(CaptureCommandMode.Region);
    }

    private void OnSettingsButtonClick(object sender, RoutedEventArgs e)
    {
        ApplyShellView(AppShellView.Settings);
        Logger.LogDebug("Settings shell opened; capture session state was preserved.");
    }

    private void OnSettingsBackButtonClick(object sender, RoutedEventArgs e)
    {
        ApplyShellView(AppShellView.Main);
        Logger.LogDebug("Settings shell closed; returning to main panel.");
    }

    private void OnSettingsHdrAlertsToggleToggled(object sender, RoutedEventArgs e)
    {
        if (applyingSettingsProjection)
        {
            return;
        }

        hdrAlertSettingsWriter.SetHdrAlertsEnabled(SettingsHdrAlertsToggle.IsOn);
        ApplySettingsProjection(captureService?.CurrentSessionState ?? CaptureSessionState.Idle());
        Logger.LogDebug(
            "HDR alert preference updated in settings: enabled={HdrAlertsEnabled}",
            settingsProvider.HdrAlertsEnabled);
    }

    private async Task ExecuteCaptureFromUiAsync(CaptureCommandMode mode)
    {
        var probeCommand = mode == CaptureCommandMode.Region
            ? CaptureCommand.Region()
            : CaptureCommand.Fullscreen();

        CaptureCommandResult guardResult;
        try
        {
            guardResult = await captureCommandCoordinator.ExecuteAsync(probeCommand);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "ExecuteAsync FAILED for probe command");
            ApplySessionState(
                CaptureSessionState.Failed(null, PreviewReadinessStatus.Failed(
                    PreviewReadinessStage.Capture,
                    "Capture failed",
                    "Command execution failed.")));
            return;
        }

        if (!guardResult.IsAccepted)
        {
            Logger.LogWarning(
                "Capture command rejected before starting: mode={Mode}, outcome={Outcome}",
                mode, guardResult.Outcome);
            ApplySessionState(
                captureService?.CurrentSessionState
                ?? CaptureSessionState.Failed(null, guardResult.Readiness
                    ?? PreviewReadinessStatus.Failed(
                        PreviewReadinessStage.Capture,
                        "Capture rejected",
                        "Command was rejected by the capture service.")));
            return;
        }

        try
        {
            StopPreview(reportStopped: false);
            CloseOverlayWindow();
            ApplySessionState(
                CaptureSessionState.SelectingTarget(PreviewReadinessStatus.Initializing(
                    PreviewReadinessStage.Capture,
                    "Starting direct monitor capture...",
                    "Resolving current monitor for direct capture.")));

            Logger.LogDebug("Direct capture started (no picker)");

            var directService = DirectMonitorCaptureTargetSelectionService.CreateDirectOnly(
                MonitorSelectionInterop.GetCurrentMonitorFromCursor,
                monitor => CaptureTarget.FromDisplayItem(
                    GraphicsCaptureMonitorInterop.CreateForMonitor(monitor),
                    monitor.DisplayName));
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
                Logger.LogError(exception, "Direct capture failed");
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
                UpdateMainPanelProjection(captureService?.CurrentSessionState ?? CaptureSessionState.Idle());
            }
        }
    }

    private void ConfigureWindowPresenter()
    {
        if (AppWindow.Presenter is not OverlappedPresenter presenter)
        {
            return;
        }

        presenter.SetBorderAndTitleBar(false, false);
        presenter.IsResizable = false;
        presenter.IsMaximizable = false;
    }

    private void SuppressWindowFrameBorder()
    {
        try
        {
            Logger.LogDebug("{Detail}", WindowFrameInterop.SuppressNonClientBorder(this));
        }
        catch (Exception exception)
        {
            Logger.LogWarning(exception, "Main window DWM border suppression was skipped.");
        }
    }

    private void OnRootGridLoaded(object sender, RoutedEventArgs e)
    {
        RootGrid.Loaded -= OnRootGridLoaded;
        if (RootGrid.XamlRoot is not null)
        {
            RootGrid.XamlRoot.Changed += OnXamlRootChanged;
        }

        SizeToActiveShellView();
        SuppressWindowFrameBorder();
        UpdateHeaderDragRegion();
    }

    private void OnXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args)
    {
        SizeToActiveShellView();
        UpdateHeaderDragRegion();
    }

    private void OnHeaderDragAreaSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateHeaderDragRegion();
    }

    private void UpdateHeaderDragRegion()
    {
        if (RootGrid.XamlRoot is null)
        {
            return;
        }

        try
        {
            nonClientPointerSource ??= InputNonClientPointerSource.GetForWindowId(AppWindow.Id);
            if (activeShellView is AppShellView.Settings)
            {
                UpdateSettingsHeaderDragRegion();
            }
            else
            {
                UpdateMainHeaderDragRegion();
            }
        }
        catch (Exception exception)
        {
            Logger.LogWarning(exception, "Main window header drag region could not be registered.");
        }
    }

    private void UpdateMainHeaderDragRegion()
    {
        if (HeaderDragArea.ActualWidth <= 0
            || HeaderDragArea.ActualHeight <= 0
            || SettingsButton.ActualWidth <= 0)
        {
            ClearHeaderDragRegion();
            return;
        }

        var headerOrigin = HeaderDragArea
            .TransformToVisual(RootGrid)
            .TransformPoint(new Windows.Foundation.Point(0, 0));
        var settingsOrigin = SettingsButton
            .TransformToVisual(RootGrid)
            .TransformPoint(new Windows.Foundation.Point(0, 0));
        var scale = RootGrid.XamlRoot!.RasterizationScale;
        var dragWidthDips = Math.Max(0, settingsOrigin.X - headerOrigin.X);

        if (dragWidthDips <= 0)
        {
            nonClientPointerSource?.ClearRegionRects(NonClientRegionKind.Caption);
            return;
        }

        var dragRegion = new RectInt32(
            ScaleToPhysicalPixels(headerOrigin.X, scale),
            ScaleToPhysicalPixels(headerOrigin.Y, scale),
            ScaleToPhysicalPixels(dragWidthDips, scale),
            ScaleToPhysicalPixels(HeaderDragArea.ActualHeight, scale));

        nonClientPointerSource?.SetRegionRects(NonClientRegionKind.Caption, [dragRegion]);
    }

    private void UpdateSettingsHeaderDragRegion()
    {
        if (SettingsHeaderDragArea.ActualWidth <= 0
            || SettingsHeaderDragArea.ActualHeight <= 0
            || SettingsBackButton.ActualWidth <= 0)
        {
            ClearHeaderDragRegion();
            return;
        }

        var headerOrigin = SettingsHeaderDragArea
            .TransformToVisual(RootGrid)
            .TransformPoint(new Windows.Foundation.Point(0, 0));
        var backOrigin = SettingsBackButton
            .TransformToVisual(RootGrid)
            .TransformPoint(new Windows.Foundation.Point(0, 0));
        var scale = RootGrid.XamlRoot!.RasterizationScale;
        var dragStartDips = Math.Max(0, backOrigin.X - headerOrigin.X + SettingsBackButton.ActualWidth + 8);
        var dragWidthDips = Math.Max(0, SettingsHeaderDragArea.ActualWidth - dragStartDips);

        if (dragWidthDips <= 0)
        {
            nonClientPointerSource?.ClearRegionRects(NonClientRegionKind.Caption);
            return;
        }

        var dragRegion = new RectInt32(
            ScaleToPhysicalPixels(headerOrigin.X + dragStartDips, scale),
            ScaleToPhysicalPixels(headerOrigin.Y, scale),
            ScaleToPhysicalPixels(dragWidthDips, scale),
            ScaleToPhysicalPixels(SettingsHeaderDragArea.ActualHeight, scale));

        nonClientPointerSource?.SetRegionRects(NonClientRegionKind.Caption, [dragRegion]);
    }

    private void ApplyShellView(AppShellView view)
    {
        var state = captureService?.CurrentSessionState ?? CaptureSessionState.Idle();
        var projection = AppShellProjection.Project(state, view);
        ClearHeaderDragRegion();
        activeShellView = projection.ActiveView;

        MainPanelRoot.Visibility = projection.IsMainPanelVisible
            ? Visibility.Visible
            : Visibility.Collapsed;
        SettingsPanelRoot.Visibility = projection.IsSettingsVisible
            ? Visibility.Visible
            : Visibility.Collapsed;

        UpdateMainPanelProjection(state);
        ApplySettingsProjection(state);
        SizeToActiveShellView();
        UpdateHeaderDragRegion();
    }

    private void SizeToActiveShellView()
    {
        var height = activeShellView is AppShellView.Settings
            ? SettingsPanelHeightDips
            : MainPanelHeightDips;
        SizeToShell(MainPanelWidthDips, height);
    }

    private void SizeToShell(int widthDips, int heightDips)
    {
        if (sizingMainPanel)
        {
            return;
        }

        var scale = RootGrid.XamlRoot?.RasterizationScale ?? 1.0;
        try
        {
            sizingMainPanel = true;
            var clientSize = ConstrainClientSizeToWorkArea(new SizeInt32(
                ScaleToPhysicalPixels(widthDips, scale),
                ScaleToPhysicalPixels(heightDips, scale)));
            AppWindow.ResizeClient(clientSize);
            KeepWindowInsideWorkArea();
        }
        finally
        {
            sizingMainPanel = false;
        }
    }

    private SizeInt32 ConstrainClientSizeToWorkArea(SizeInt32 requestedSize)
    {
        var workArea = DisplayArea.GetFromWindowId(
            AppWindow.Id,
            DisplayAreaFallback.Nearest).WorkArea;
        var maxWidth = Math.Max(1, workArea.Width - (WorkAreaMarginPixels * 2));
        var maxHeight = Math.Max(1, workArea.Height - (WorkAreaMarginPixels * 2));
        return new SizeInt32(
            Math.Min(requestedSize.Width, maxWidth),
            Math.Min(requestedSize.Height, maxHeight));
    }

    private void KeepWindowInsideWorkArea()
    {
        var workArea = DisplayArea.GetFromWindowId(
            AppWindow.Id,
            DisplayAreaFallback.Nearest).WorkArea;
        var size = AppWindow.Size;
        var width = Math.Min(size.Width, workArea.Width);
        var height = Math.Min(size.Height, workArea.Height);
        var maxX = Math.Max(workArea.X, workArea.X + workArea.Width - width);
        var maxY = Math.Max(workArea.Y, workArea.Y + workArea.Height - height);
        var x = Math.Clamp(AppWindow.Position.X, workArea.X, maxX);
        var y = Math.Clamp(AppWindow.Position.Y, workArea.Y, maxY);

        if (x != AppWindow.Position.X || y != AppWindow.Position.Y || width != size.Width || height != size.Height)
        {
            AppWindow.MoveAndResize(new RectInt32(x, y, width, height));
        }
    }

    private void ClearHeaderDragRegion()
    {
        try
        {
            nonClientPointerSource?.ClearRegionRects(NonClientRegionKind.Caption);
        }
        catch (Exception exception)
        {
            Logger.LogWarning(exception, "Main window header drag region could not be cleared.");
        }
    }

    private static int ScaleToPhysicalPixels(int dips, double rasterizationScale) =>
        Math.Max(1, (int)Math.Ceiling(dips * rasterizationScale));

    private static int ScaleToPhysicalPixels(double dips, double rasterizationScale) =>
        Math.Max(0, (int)Math.Round(dips * rasterizationScale));

    private void StartPreview(CaptureTarget target)
    {
        if (isClosed)
        {
            return;
        }

        StopPreview(reportStopped: false);

        lock (previewSync)
        {
            Logger.LogInformation(
                "operation=StartPreview, stage=PreCheck, detail=Previous cycle cleanup confirmed: captureSession={CaptureSession}, swapChainResources={SwapChainResources}",
                captureSession is null, swapChainResources is null);
        }

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

        Logger.LogInformation(
            "Capture started: generation={Generation}, target={DisplayName} ({Width}x{Height}), kind={Kind}",
            currentGeneration, target.DisplayName, target.Size.Width, target.Size.Height, target.Kind);

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
                var currentGeneration = Volatile.Read(ref previewGeneration);
                if (generation != currentGeneration || previewFramePresenter is null)
                {
                    Logger.LogDebug(
                        "operation=FrameCallback, stage=Reject, detail=Stale callback rejected: frameGeneration={FrameGeneration}, currentGeneration={CurrentGeneration}, hasPresenter={HasPresenter}",
                        generation, currentGeneration, previewFramePresenter is not null);
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

        if (frameNumber == 1 && !sizeChange?.RequiresRecreation == true)
        {
            Logger.LogDebug("First frame arrived: {Width}x{Height}, source={Source}", frame.Width, frame.Height, frame.SourceDescription);
        }

        if (sizeChange?.RequiresRecreation == true)
        {
            Logger.LogInformation(
                "Frame size changed: {OldWidth}x{OldHeight} -> {NewWidth}x{NewHeight}, requiresRecreation=true",
                previewSourceWidth, previewSourceHeight, frame.Width, frame.Height);
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

        Logger.LogInformation(
            "Preview recreation queued: newSize={Width}x{Height}, newGeneration={Generation}",
            sizeChange.ReplacementWidth, sizeChange.ReplacementHeight, recreationGeneration);

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
                    var currentState = captureService?.CurrentSessionState ?? CaptureSessionState.Idle();
                    ApplySessionState(
                        currentState.Target is null
                            ? CaptureSessionState.Failed(null, readiness)
                            : CreateSessionStateFromCurrentEvidence(currentState.Target, readiness, allowCapturing: true));
                }
            }))
        {
        }
    }

    private void ApplyFrameDiagnostic(long generation, string detail)
    {
        var frameNumber = Interlocked.Increment(ref frameEventCount);
        Logger.LogDebug("Frame diagnostic: generation={Generation}, event={Event}, detail={Detail}", generation, frameNumber, detail);
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
        overlayWindow.ReassertTopmost();
        overlayWindow.ExcludeFromCapture();
        overlayWindow.ApplyState(CreateOverlayState(captureService?.CurrentSessionState ?? CaptureSessionState.Idle()));

        Logger.LogInformation(
            "Overlay created: fullscreen, frameSize={Width}x{Height}, displayName={DisplayName}",
            target.Size.Width, target.Size.Height, target.DisplayName);
    }

    private void StopPreview(bool reportStopped = true)
    {
        CaptureSessionResources? captureSessionToDispose;
        SwapChainResources? swapChainToDispose;
        long stoppedGeneration;
        lock (previewSync)
        {
            Interlocked.Increment(ref previewGeneration);
            stoppedGeneration = Volatile.Read(ref previewGeneration);
            captureSessionToDispose = captureSession;
            captureSession = null;
            previewFramePresenter = null;
            activeCaptureTarget = null;
            activePresentationEvidence = null;
            swapChainToDispose = swapChainResources;
            swapChainResources = null;
        }

        Logger.LogDebug("operation=StopPreview, stage=Start, detail=Disposing capture and swap chain resources, generation={Generation}", stoppedGeneration);
        captureSessionToDispose?.Dispose();
        swapChainToDispose?.Dispose();

        Logger.LogInformation(
            "operation=StopPreview, stage=Complete, detail=StopPreview completed: generation={Generation}, captureDisposed={CaptureDisposed}, swapChainDisposed={SwapChainDisposed}, previousCycleCleaned={PreviousCycleCleaned}",
            stoppedGeneration, captureSessionToDispose is not null, swapChainToDispose is not null, captureSessionToDispose is null);

        if (reportStopped && !isClosed)
        {
            ApplySessionState(CaptureSessionState.Disposed());
        }
    }

    private void ApplySessionState(CaptureSessionState state)
    {
        // Must be called on the UI thread. Future entry points (hotkey, tray) must dispatch first.
        Debug.Assert(RootGrid.DispatcherQueue.HasThreadAccess, "ApplySessionState must be called on the UI thread.");

        if (applyingSessionState)
        {
            return;
        }

        applyingSessionState = true;
        try
        {
            var validatedState = state ?? throw new ArgumentNullException(nameof(state));
            captureService?.UpdateSessionState(validatedState);
            UpdateMainPanelProjection(validatedState);
            var overlayState = CreateOverlayState(validatedState);
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

    private void ApplyShortcutLabels()
    {
        SelectCaptureTargetButton.ShortcutText = MainPanelProjection.FormatShortcut(settingsProvider.FullscreenShortcut);
        RegionSelectButton.ShortcutText = MainPanelProjection.FormatShortcut(settingsProvider.RegionShortcut);
        ApplySettingsProjection(captureService?.CurrentSessionState ?? CaptureSessionState.Idle());
    }

    private void ApplySettingsProjection(CaptureSessionState state)
    {
        var projection = SettingsPanelProjection.Project(settingsProvider, state, aboutInfoProvider);

        applyingSettingsProjection = true;
        try
        {
            SettingsFullscreenShortcutValueText.Text = projection.FullscreenShortcut.DisplayValue;
            AutomationProperties.SetName(SettingsFullscreenShortcutValuePill, projection.FullscreenShortcut.Label);
            AutomationProperties.SetHelpText(SettingsFullscreenShortcutValuePill, projection.FullscreenShortcut.HelpText);
            ToolTipService.SetToolTip(SettingsFullscreenShortcutValuePill, projection.FullscreenShortcut.PendingReason);

            SettingsRegionShortcutValueText.Text = projection.RegionShortcut.DisplayValue;
            AutomationProperties.SetName(SettingsRegionShortcutValuePill, projection.RegionShortcut.Label);
            AutomationProperties.SetHelpText(SettingsRegionShortcutValuePill, projection.RegionShortcut.HelpText);
            ToolTipService.SetToolTip(SettingsRegionShortcutValuePill, projection.RegionShortcut.PendingReason);

            SettingsHdrAlertsToggle.IsOn = projection.HdrAlertsEnabled;
            SettingsHdrAlertsHelperText.Text = "When HDR is unavailable";
            AutomationProperties.SetHelpText(SettingsHdrAlertsToggle, projection.HdrAlertsHelpText);
            ApplyDestinationSegmentProjection(projection.Output);
            SettingsSavePathValueText.Text = projection.Output.SavePathDisplayValue;
            SettingsSavePathHelperText.Text = projection.Output.SavePathHelpText;
            AutomationProperties.SetName(SettingsSavePathValuePill, $"Save path: {projection.Output.SavePathDisplayValue}");
            AutomationProperties.SetHelpText(SettingsSavePathValuePill, projection.Output.SavePathHelpText);
            ToolTipService.SetToolTip(
                SettingsSavePathValuePill,
                $"{projection.Output.SavePathDisplayValue}. {projection.Output.SavePathHelpText}");

            SettingsOpenAfterCaptureToggle.IsEnabled = !projection.Output.IsAfterCaptureReadOnly;
            SettingsOpenAfterCaptureToggle.IsOn = projection.Output.IsAfterCaptureSelected;
            SettingsAfterCaptureHelperText.Text = projection.Output.AfterCaptureHelpText;
            AutomationProperties.SetName(SettingsOpenAfterCaptureToggle, $"After capture: {projection.Output.AfterCaptureDisplayValue}");
            AutomationProperties.SetHelpText(SettingsOpenAfterCaptureToggle, projection.Output.AfterCaptureHelpText);

            SettingsTimestampToggle.IsEnabled = !projection.Output.IsTimestampReadOnly;
            SettingsTimestampToggle.IsOn = projection.Output.TimestampNaming;
            SettingsTimestampHelperText.Text = projection.Output.TimestampHelpText;
            AutomationProperties.SetHelpText(SettingsTimestampToggle, projection.Output.TimestampHelpText);

            SettingsCopyAsImageToggle.IsEnabled = !projection.Output.IsCopyAsImageReadOnly;
            SettingsCopyAsImageToggle.IsOn = projection.Output.CopyAsImage;
            SettingsCopyAsImageHelperText.Text = projection.Output.CopyAsImageHelpText;
            AutomationProperties.SetHelpText(SettingsCopyAsImageToggle, projection.Output.CopyAsImageHelpText);

            SettingsColorOutputValueText.Text = projection.Output.ExportColorDisplayValue;
            SettingsColorOutputHelperText.Text = projection.Output.ExportColorHelpText;
            AutomationProperties.SetName(SettingsColorOutputValuePill, $"Color output: {projection.Output.ExportColorDisplayValue}");
            AutomationProperties.SetHelpText(SettingsColorOutputValuePill, projection.Output.ExportColorHelpText);
            ToolTipService.SetToolTip(SettingsColorOutputValuePill, projection.Output.ExportColorHelpText);

            SettingsAboutAppNameText.Text = projection.About.AppName;
            SettingsAboutVersionText.Text = projection.About.Version;
            SettingsAboutDescriptionText.Text = projection.About.Description;
            AutomationProperties.SetName(SettingsAboutInfoPanel, $"{projection.About.AppName} {projection.About.Version}");
            AutomationProperties.SetHelpText(SettingsAboutInfoPanel, projection.About.Description);
        }
        finally
        {
            applyingSettingsProjection = false;
        }
    }

    private void ApplyDestinationSegmentProjection(OutputSettingsProjection output)
    {
        ApplySegmentState(
            SettingsDestinationClipboardSegment,
            SettingsDestinationClipboardText,
            SettingsDestinationClipboardStatusText,
            output.IsClipboardSelected);
        ApplySegmentState(
            SettingsDestinationFolderSegment,
            SettingsDestinationFolderText,
            SettingsDestinationFolderStatusText,
            output.IsFolderSelected);
        ApplySegmentState(
            SettingsDestinationBothSegment,
            SettingsDestinationBothText,
            SettingsDestinationBothStatusText,
            output.IsBothSelected);
        AutomationProperties.SetHelpText(
            SettingsDestinationClipboardSegment,
            GetDestinationOptionHelpText("Clipboard", output.IsClipboardSelected, output.PendingReason));
        AutomationProperties.SetHelpText(
            SettingsDestinationFolderSegment,
            GetDestinationOptionHelpText("Folder", output.IsFolderSelected, output.PendingReason));
        AutomationProperties.SetHelpText(
            SettingsDestinationBothSegment,
            GetDestinationOptionHelpText("Both", output.IsBothSelected, output.PendingReason));
        AutomationProperties.SetName(SettingsDestinationClipboardSegment, "Destination option: Clipboard");
        AutomationProperties.SetName(SettingsDestinationFolderSegment, "Destination option: Folder");
        AutomationProperties.SetName(SettingsDestinationBothSegment, "Destination option: Both");
        SettingsDestinationHelperText.Text = output.PendingReason;
    }

    private static string GetDestinationOptionHelpText(string option, bool isSelected, string pendingReason)
    {
        var state = isSelected ? "selected" : "not selected";
        return $"{option} is {state}. {pendingReason}.";
    }

    private void ApplySegmentState(Border segment, TextBlock label, TextBlock status, bool isSelected)
    {
        segment.Background = isSelected
            ? (Brush)Application.Current.Resources["AccentSoftBrush"]
            : null;
        label.Foreground = isSelected
            ? (Brush)Application.Current.Resources["TextBrush"]
            : (Brush)Application.Current.Resources["MutedTextBrush"];
        status.Foreground = label.Foreground;
        status.Visibility = isSelected ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateMainPanelProjection(CaptureSessionState state)
    {
        var projection = MainPanelProjection.Project(state);
        var isIdle = state.Status is CaptureSessionStatus.Idle;
        var statusBrush = GetTrustStatusBrush(projection.TrustSeverity);

        SelectCaptureTargetButton.IsEnabled = projection.CanStartCapture;
        RegionSelectButton.IsEnabled = projection.CanStartCapture;
        SelectCaptureTargetButton.Title = isIdle ? "Full Screen" : projection.ActionTitle;
        RegionSelectButton.Title = isIdle ? "Region" : projection.ActionTitle;

        TrustStatusGlyph.Glyph = GetTrustStatusGlyph(projection.TrustIcon);
        TrustStatusGlyph.Foreground = statusBrush;
        TrustStatusDot.Fill = statusBrush;
        TrustStatusLabel.Text = projection.TrustLabel;
        TrustStatusLabel.Foreground = statusBrush;
        ToolTipService.SetToolTip(TrustStatusLabel, projection.TrustMessage);
        AutomationProperties.SetHelpText(TrustStatusLabel, projection.TrustMessage);
    }

    private Brush GetTrustStatusBrush(MainPanelTrustSeverity severity)
    {
        var key = severity switch
        {
            MainPanelTrustSeverity.Success => "SuccessBrush",
            MainPanelTrustSeverity.Warning => "WarningBrush",
            MainPanelTrustSeverity.Error => "ErrorBrush",
            _ => "MutedTextBrush",
        };

        return (Brush)Application.Current.Resources[key];
    }

    private static string GetTrustStatusGlyph(MainPanelTrustIcon icon) =>
        icon switch
        {
            MainPanelTrustIcon.CheckmarkCircle => StatusCheckmarkCircleGlyph,
            MainPanelTrustIcon.Desktop => StatusMonitorGlyph,
            MainPanelTrustIcon.ErrorCircle => StatusErrorCircleGlyph,
            _ => StatusClockGlyph,
        };

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
            Logger.LogWarning("Overlay dispatcher rejected UI work; falling back to RootGrid dispatcher.");
        }

        var enqueued = RootGrid.DispatcherQueue.TryEnqueue(() => action());
        if (!enqueued && Interlocked.Exchange(ref uiDispatchFailureReported, 1) == 0)
        {
            Logger.LogWarning("RootGrid dispatcher rejected UI work; preview status update was dropped.");
        }

        return enqueued;
    }

    private void OnOverlayCloseRequested(object? sender, EventArgs args)
    {
        if (isClosed)
        {
            return;
        }

        Logger.LogDebug("Overlay close requested (Escape/cancel), StopPreview called");
        StopPreview();
        CloseOverlayWindow();
        Logger.LogDebug("Resetting session state to Idle after overlay close requested");
        ApplySessionState(CaptureSessionState.Idle());
    }

    private async void OnOverlayCaptureConfirmed(object? sender, ConfirmedCaptureSelection selection)
    {
        if (!ReferenceEquals(sender, overlayWindow) || isClosed)
        {
            return;
        }

        Logger.LogInformation(
            "CaptureConfirmed received: pixelRegion=({X},{Y},{Width}x{Height}), status={Status}, frame={FrameW}x{FrameH}",
            selection.PixelRegion.X, selection.PixelRegion.Y, selection.PixelRegion.Width, selection.PixelRegion.Height,
            selection.Status, selection.FrameSize.Width, selection.FrameSize.Height);

        var copied = false;
        try
        {
            copied = await TryCopyCropToClipboardAsync(selection);
            StopPreview(reportStopped: false);
        }
        finally
        {
            overlayWindow?.ApplyClipboardResult(copied, selection.Status);
            CloseOverlayWindow();
            if (!isClosed)
            {
                Logger.LogDebug("Resetting session state to Idle after overlay capture confirmed");
                ApplySessionState(CaptureSessionState.Idle());
            }
        }
    }

    private async Task<bool> TryCopyCropToClipboardAsync(ConfirmedCaptureSelection selection)
    {
        if (swapChainResources is null)
        {
            Logger.LogWarning("Clipboard output SKIPPED: swapChainResources is null");
            return false;
        }

        try
        {
            Logger.LogDebug(
                "Clipboard output started: crop=({X},{Y},{Width}x{Height}), frame={FrameW}x{FrameH}",
                selection.PixelRegion.X, selection.PixelRegion.Y, selection.PixelRegion.Width, selection.PixelRegion.Height,
                selection.FrameSize.Width, selection.FrameSize.Height);

            // Capture back buffer reference before async operation
            var backBuffer = swapChainResources.SwapChain.GetBuffer<Vortice.Direct3D11.ID3D11Texture2D>(0);
            try
            {
                var request = new OutputRequest
                {
                    Texture = new CapturedFrameTexture(
                        backBuffer,
                        selection.FrameSize.Width,
                        selection.FrameSize.Height,
                        "SwapChain back buffer"),
                    CropRegion = new CropPixelRect(
                        selection.PixelRegion.X,
                        selection.PixelRegion.Y,
                        selection.PixelRegion.Width,
                        selection.PixelRegion.Height),
                    Policy = OutputPolicy.FromSettings(
                        settingsProvider.OutputTarget,
                        settingsProvider.CopyAsImage,
                        settingsProvider.SavePath,
                        settingsProvider.TimestampNaming,
                        settingsProvider.AfterCaptureBehavior.ToString())
                };

                var result = await outputService.ExecuteOutputAsync(request);

                Logger.Log(
                    result.IsSuccess ? LogLevel.Information : LogLevel.Warning,
                    "Configured output {Outcome}: crop=({X},{Y},{Width}x{Height}), message={Message}, detail={Detail}, afterCapture={AfterCaptureOutcome}, afterCaptureDetail={AfterCaptureDetail}",
                    result.IsSuccess ? "completed" : "failed",
                    selection.PixelRegion.X, selection.PixelRegion.Y, selection.PixelRegion.Width, selection.PixelRegion.Height,
                    result.UserMessage,
                    result.TechnicalDetail,
                    result.AfterCapture?.Outcome,
                    result.AfterCapture?.TechnicalDetail);
                return result.IsSuccess;
            }
            finally
            {
                backBuffer?.Dispose();
            }
        }
        catch (Exception ex)
        {
            var cropInfo = selection?.PixelRegion is { } r
                ? $"crop=({r.X},{r.Y},{r.Width}x{r.Height})"
                : "crop=unknown";
            Logger.LogError(ex, "operation=ClipboardOutput, stage=ExecuteOutput, detail=Clipboard output EXCEPTION: {CropInfo}", cropInfo);
            return false;
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

            var currentState = captureService?.CurrentSessionState ?? CaptureSessionState.Idle();
            if (!isClosed && currentState.HasNativeSession)
            {
                Logger.LogDebug("Stopping preview from OnOverlayClosed, current state: {Status}", currentState.Status);
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
        if (RootGrid.XamlRoot is not null)
        {
            RootGrid.XamlRoot.Changed -= OnXamlRootChanged;
        }

        try
        {
            StopPreview(reportStopped: false);
            CloseOverlayWindow();
        }
        finally
        {
            captureService = null;
            if (!isDeviceDisposed)
            {
                isDeviceDisposed = true;
                deviceResources.Dispose();
            }
        }
    }

}
