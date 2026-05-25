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
using Microsoft.UI.Xaml.Shapes;
using Vortice.Direct3D11;
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
    private readonly IOutputSettingsWriter outputSettingsWriter;
    private readonly IAboutInfoProvider aboutInfoProvider;
    private readonly GraphicsDeviceResources deviceResources;
    private readonly GraphicsEngine graphicsEngine;
    private ITrayMenu? trayMenu;
    private IGlobalHotkeyRegistrar? globalHotkeyRegistrar;
    private CaptureService? captureService;
    private CaptureSessionResources? captureSession;
    private PreviewFramePresenter? previewFramePresenter;
    private SwapChainResources? swapChainResources;
    private CaptureTarget? activeCaptureTarget;
    private CaptureCommandMode? activeCaptureMode;
    private CapturedFrameTexture? latestOutputFrameSnapshot;
    private PreviewReadinessStatus? activePresentationEvidence;
    private OverlayWindow? overlayWindow;
    private long previewGeneration;
    private long frameEventCount;
    private int fullscreenOutputStarted;
    private int previewSourceWidth;
    private int previewSourceHeight;
    private int registeredHotkeyCount;
    private bool isClosed;
    private bool isDeviceDisposed;
    private bool applyingSessionState;
    private bool applyingSettingsProjection;
    private bool sizingMainPanel;
    private bool isExplicitShutdown;
    private bool backgroundWindowEventsEnabled;
    private AppShellView activeShellView = AppShellView.Main;
    private int overlayDispatcherFallbackReported;
    private int uiDispatchFailureReported;
    private InputNonClientPointerSource? nonClientPointerSource;

    public MainWindow(
        ICaptureCommandCoordinator captureCommandCoordinator,
        IOutputService outputService,
        ISettingsProvider settingsProvider,
        IHdrAlertSettingsWriter hdrAlertSettingsWriter,
        IOutputSettingsWriter outputSettingsWriter,
        IAboutInfoProvider aboutInfoProvider,
        CaptureService captureService,
        GraphicsDeviceResources deviceResources)
    {
        this.captureCommandCoordinator = captureCommandCoordinator ?? throw new ArgumentNullException(nameof(captureCommandCoordinator));
        this.outputService = outputService ?? throw new ArgumentNullException(nameof(outputService));
        this.settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
        this.hdrAlertSettingsWriter = hdrAlertSettingsWriter ?? throw new ArgumentNullException(nameof(hdrAlertSettingsWriter));
        this.outputSettingsWriter = outputSettingsWriter ?? throw new ArgumentNullException(nameof(outputSettingsWriter));
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
        Activated += OnWindowActivated;
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

    public void AttachTrayMenu(ITrayMenu menu)
    {
        trayMenu?.Dispose();
        trayMenu = menu ?? throw new ArgumentNullException(nameof(menu));
        trayMenu.CommandRequested += OnTrayMenuCommandRequested;
        UpdateTrayMenu(captureService?.CurrentSessionState ?? CaptureSessionState.Idle());
    }

    public TrayMenuSnapshot CreateTrayMenuSnapshot() =>
        CreateTrayMenuSnapshot(captureService?.CurrentSessionState ?? CaptureSessionState.Idle());

    public void AttachGlobalHotkeys(IGlobalHotkeyRegistrar registrar)
    {
        globalHotkeyRegistrar?.Dispose();
        globalHotkeyRegistrar = registrar ?? throw new ArgumentNullException(nameof(registrar));
        globalHotkeyRegistrar.HotkeyPressed += OnGlobalHotkeyPressed;
        RegisterConfiguredHotkeys();
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

    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        if (backgroundWindowEventsEnabled || args.WindowActivationState is WindowActivationState.Deactivated)
        {
            return;
        }

        backgroundWindowEventsEnabled = true;
        AppWindow.Closing += OnAppWindowClosing;
        AppWindow.Changed += OnAppWindowChanged;
        Activated -= OnWindowActivated;
        Logger.LogDebug("Background close/minimize handlers enabled after first window activation.");
    }

    private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (isExplicitShutdown || isClosed)
        {
            return;
        }

        if (!IsBackgroundAvailable)
        {
            Logger.LogWarning("Background close requested, but tray/hotkeys are unavailable; closing normally.");
            return;
        }

        args.Cancel = true;
        HideToBackground("close");
    }

    private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (isExplicitShutdown || isClosed || !IsBackgroundAvailable || !args.DidPresenterChange)
        {
            return;
        }

        if (AppWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Minimized })
        {
            HideToBackground("minimize");
        }
    }

    private void OnSettingsHdrAlertsButtonClick(object sender, RoutedEventArgs e)
    {
        if (applyingSettingsProjection)
        {
            return;
        }

        hdrAlertSettingsWriter.SetHdrAlertsEnabled(!settingsProvider.HdrAlertsEnabled);
        ApplySettingsProjection(captureService?.CurrentSessionState ?? CaptureSessionState.Idle());
        Logger.LogDebug(
            "HDR alert preference updated in settings: enabled={HdrAlertsEnabled}",
            settingsProvider.HdrAlertsEnabled);
    }

    private void OnSettingsDestinationClipboardClick(object sender, RoutedEventArgs e) =>
        SetOutputTargetFromSettings(OutputTarget.Clipboard);

    private void OnSettingsDestinationFolderClick(object sender, RoutedEventArgs e) =>
        SetOutputTargetFromSettings(OutputTarget.Folder);

    private void OnSettingsDestinationBothClick(object sender, RoutedEventArgs e) =>
        SetOutputTargetFromSettings(OutputTarget.Both);

    private void SetOutputTargetFromSettings(OutputTarget target)
    {
        if (applyingSettingsProjection || settingsProvider.OutputTarget == target)
        {
            return;
        }

        outputSettingsWriter.SetOutputTarget(target);
        ApplySettingsProjection(captureService?.CurrentSessionState ?? CaptureSessionState.Idle());
        Logger.LogDebug(
            "Output target preference updated in settings: target={OutputTarget}",
            settingsProvider.OutputTarget);
    }

    private void OnSettingsCopyAsImageButtonClick(object sender, RoutedEventArgs e)
    {
        if (applyingSettingsProjection)
        {
            return;
        }

        outputSettingsWriter.SetCopyAsImage(!settingsProvider.CopyAsImage);
        ApplySettingsProjection(captureService?.CurrentSessionState ?? CaptureSessionState.Idle());
        Logger.LogDebug(
            "Copy-as-image preference updated in settings: enabled={CopyAsImage}",
            settingsProvider.CopyAsImage);
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
            activeCaptureMode = null;
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
            activeCaptureMode = null;
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
            activeCaptureMode = mode;
            UpdateMainPanelProjection(captureService?.CurrentSessionState ?? CaptureSessionState.Idle());
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

            activeCaptureMode = null;
            ApplySessionState(CaptureSessionState.FromSelectionResult(result));
        }
        catch (Exception exception)
        {
            if (!isClosed)
            {
                activeCaptureMode = null;
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

    private void OnTrayMenuCommandRequested(object? sender, TrayMenuCommandRequestedEventArgs args)
    {
        if (isClosed)
        {
            return;
        }

        if (!RootGrid.DispatcherQueue.TryEnqueue(async () => await HandleTrayMenuCommandAsync(args.Command)))
        {
            Logger.LogWarning("Tray command dropped because UI dispatcher rejected it: command={Command}", args.Command);
        }
    }

    private void OnGlobalHotkeyPressed(object? sender, GlobalHotkeyPressedEventArgs args)
    {
        if (isClosed)
        {
            return;
        }

        if (!RootGrid.DispatcherQueue.TryEnqueue(async () => await HandleGlobalHotkeyAsync(args.Command)))
        {
            Logger.LogWarning("Global hotkey dropped because UI dispatcher rejected it: command={Command}", args.Command);
        }
    }

    private async Task HandleTrayMenuCommandAsync(TrayMenuCommand command)
    {
        if (isClosed)
        {
            return;
        }

        Logger.LogDebug("Tray command received: {Command}", command);
        switch (command)
        {
            case TrayMenuCommand.FullscreenCapture:
                await ExecuteCaptureFromUiAsync(CaptureCommandMode.Fullscreen);
                break;
            case TrayMenuCommand.RegionCapture:
                await ExecuteCaptureFromUiAsync(CaptureCommandMode.Region);
                break;
            case TrayMenuCommand.OpenMainWindow:
                ShowMainWindowFromTray();
                break;
            case TrayMenuCommand.OpenSettings:
                ShowSettingsFromTray();
                break;
            case TrayMenuCommand.Quit:
                QuitFromTray();
                break;
        }
    }

    private async Task HandleGlobalHotkeyAsync(HotkeyCommand command)
    {
        if (isClosed)
        {
            return;
        }

        Logger.LogDebug("Global hotkey received: {Command}", command);
        switch (command)
        {
            case HotkeyCommand.FullscreenCapture:
                await ExecuteCaptureFromUiAsync(CaptureCommandMode.Fullscreen);
                break;
            case HotkeyCommand.RegionCapture:
                await ExecuteCaptureFromUiAsync(CaptureCommandMode.Region);
                break;
        }
    }

    private void ShowMainWindowFromTray()
    {
        RestoreAndActivateFromTray();
        ApplyShellView(AppShellView.Main);
        Logger.LogDebug("Main window opened from tray; capture session state was preserved.");
    }

    private void ShowSettingsFromTray()
    {
        RestoreAndActivateFromTray();
        ApplyShellView(AppShellView.Settings);
        Logger.LogDebug("Settings opened from tray; capture session state was preserved.");
    }

    private void RestoreAndActivateFromTray()
    {
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.Restore();
        }

        AppWindow.Show(true);
        Activate();
    }

    private bool IsBackgroundAvailable => trayMenu is not null || registeredHotkeyCount > 0;

    private void HideToBackground(string reason)
    {
        AppWindow.Hide();
        UpdateTrayMenu(captureService?.CurrentSessionState ?? CaptureSessionState.Idle());
        Logger.LogInformation(
            "Background workflow activated: reason={Reason}, trayAvailable={TrayAvailable}, hotkeysAvailable={HotkeysAvailable}",
            reason,
            trayMenu is not null,
            registeredHotkeyCount > 0);
    }

    private void QuitFromTray()
    {
        Logger.LogInformation(
            "Tray quit requested: captureActive={CaptureActive}, outputFrameAvailable={OutputFrameAvailable}",
            captureService?.CurrentSessionState.HasNativeSession ?? false,
            latestOutputFrameSnapshot is not null);
        isExplicitShutdown = true;
        Close();
        Application.Current.Exit();
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

        var requestedMode = activeCaptureMode;
        StopPreview(reportStopped: false);
        activeCaptureMode = requestedMode ?? CaptureCommandMode.Region;

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
            fullscreenOutputStarted = 0;

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
        var shouldCompleteFullscreenCapture = false;
        CapturedFrameTexture? snapshotToDispose = null;
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
                    var outputSnapshot = CreateOutputFrameSnapshot(frame);
                    snapshotToDispose = latestOutputFrameSnapshot;
                    latestOutputFrameSnapshot = outputSnapshot;
                    result = previewFramePresenter.PresentFrame(frame);
                    target = activeCaptureTarget;
                    shouldCompleteFullscreenCapture = activeCaptureMode is CaptureCommandMode.Fullscreen
                        && target is not null
                        && Interlocked.CompareExchange(ref fullscreenOutputStarted, 1, 0) == 0;
                }
            }
        }
        snapshotToDispose?.Dispose();

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
                    if (shouldCompleteFullscreenCapture && target is not null)
                    {
                        _ = CompleteFullscreenCaptureAsync(
                            generation,
                            target.Size.Width,
                            target.Size.Height,
                            ToOverlayDisplayStatus(readiness.State));
                    }
                }
            }))
        {
            if (shouldCompleteFullscreenCapture)
            {
                Interlocked.Exchange(ref fullscreenOutputStarted, 0);
            }

            // The frame has already been presented and disposed; no UI status update is possible.
        }
    }

    private void QueuePreviewRecreation(
        long generation,
        CaptureFrameSizeChange sizeChange)
    {
        CaptureTarget? target;
        CaptureCommandMode? mode;
        CaptureSessionResources? captureSessionToDispose;
        SwapChainResources? swapChainToDispose;
        CapturedFrameTexture? outputFrameSnapshotToDispose;
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
            mode = activeCaptureMode;
            captureSessionToDispose = captureSession;
            captureSession = null;
            previewFramePresenter = null;
            activeCaptureTarget = null;
            activeCaptureMode = null;
            outputFrameSnapshotToDispose = latestOutputFrameSnapshot;
            latestOutputFrameSnapshot = null;
            activePresentationEvidence = null;
            fullscreenOutputStarted = 0;
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
        outputFrameSnapshotToDispose?.Dispose();

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
                activeCaptureMode = mode;
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
            overlayWindow.SetCropInteractionEnabled(activeCaptureMode is CaptureCommandMode.Region);
            return;
        }

        overlayWindow = new OverlayWindow();
        Interlocked.Exchange(ref overlayDispatcherFallbackReported, 0);
        Interlocked.Exchange(ref uiDispatchFailureReported, 0);
        overlayWindow.CloseRequested += OnOverlayCloseRequested;
        overlayWindow.CaptureConfirmed += OnOverlayCaptureConfirmed;
        overlayWindow.Closed += OnOverlayClosed;
        overlayWindow.ApplyCaptureFrameSize(frameSize);
        overlayWindow.SetCropInteractionEnabled(activeCaptureMode is CaptureCommandMode.Region);
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
        CapturedFrameTexture? outputFrameSnapshotToDispose;
        long stoppedGeneration;
        lock (previewSync)
        {
            Interlocked.Increment(ref previewGeneration);
            stoppedGeneration = Volatile.Read(ref previewGeneration);
            captureSessionToDispose = captureSession;
            captureSession = null;
            previewFramePresenter = null;
            activeCaptureTarget = null;
            activeCaptureMode = null;
            outputFrameSnapshotToDispose = latestOutputFrameSnapshot;
            latestOutputFrameSnapshot = null;
            activePresentationEvidence = null;
            fullscreenOutputStarted = 0;
            swapChainToDispose = swapChainResources;
            swapChainResources = null;
        }

        Logger.LogDebug("operation=StopPreview, stage=Start, detail=Disposing capture and swap chain resources, generation={Generation}", stoppedGeneration);
        captureSessionToDispose?.Dispose();
        swapChainToDispose?.Dispose();
        outputFrameSnapshotToDispose?.Dispose();

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
            UpdateTrayMenu(validatedState);
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

    private void UpdateTrayMenu(CaptureSessionState state)
    {
        try
        {
            trayMenu?.Update(CreateTrayMenuSnapshot(state));
        }
        catch (Exception exception)
        {
            Logger.LogWarning(exception, "Tray menu projection update failed.");
        }
    }

    private TrayMenuSnapshot CreateTrayMenuSnapshot(CaptureSessionState state)
    {
        var projection = TrayMenuProjection.Project(state, settingsProvider, aboutInfoProvider);
        return new TrayMenuSnapshot(
            projection.AppName,
            projection.HdrStatusLabel,
            projection.HdrStatusDetail,
            ToTrayItem(projection.FullscreenCapture),
            ToTrayItem(projection.RegionCapture),
            ToTrayItem(projection.OpenMainWindow),
            ToTrayItem(projection.OpenSettings),
            ToTrayItem(projection.Quit));
    }

    private static TrayMenuItemSnapshot ToTrayItem(TrayMenuCommandProjection command) =>
        new(command.Label, command.ShortcutText, command.IsEnabled, command.IsActive);

    private void ApplyShortcutLabels()
    {
        SelectCaptureTargetButton.ShortcutText = MainPanelProjection.FormatShortcut(settingsProvider.FullscreenShortcut);
        RegionSelectButton.ShortcutText = MainPanelProjection.FormatShortcut(settingsProvider.RegionShortcut);
        ApplySettingsProjection(captureService?.CurrentSessionState ?? CaptureSessionState.Idle());
    }

    private void RegisterConfiguredHotkeys()
    {
        if (globalHotkeyRegistrar is null)
        {
            return;
        }

        var plan = GlobalHotkeyRegistrationPlan.Project(settingsProvider);
        var registrations = plan.RegistrableBindings()
            .Select(ToGlobalHotkeyRegistration)
            .ToArray();
        var results = globalHotkeyRegistrar.Register(registrations);
        registeredHotkeyCount = results.Count(result => result.Registered);
        foreach (var binding in new[] { plan.Fullscreen, plan.Region }.Where(binding => !binding.CanRegister))
        {
            Logger.LogInformation(
                "Global hotkey skipped: command={Command}, shortcut={Shortcut}, detail={Detail}",
                binding.Command,
                binding.DisplayValue,
                binding.StatusMessage);
        }

        foreach (var result in results)
        {
            Logger.Log(
                result.Registered ? LogLevel.Information : LogLevel.Warning,
                "Global hotkey registration {Outcome}: command={Command}, shortcut={Shortcut}, detail={Detail}",
                result.Registered ? "succeeded" : "failed",
                result.Command,
                result.DisplayText,
                result.Detail);
        }
    }

    private static GlobalHotkeyRegistration ToGlobalHotkeyRegistration(HotkeyBindingProjection binding)
    {
        var gesture = binding.Gesture ?? throw new InvalidOperationException("Registrable hotkey binding must include a gesture.");
        var command = binding.Command switch
        {
            GlobalHotkeyCommand.RegionCapture => HotkeyCommand.RegionCapture,
            _ => HotkeyCommand.FullscreenCapture,
        };

        return new GlobalHotkeyRegistration(
            command,
            Id: (int)command,
            gesture.Control,
            gesture.Shift,
            gesture.Alt,
            gesture.Windows,
            gesture.VirtualKey,
            gesture.DisplayText);
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

            SettingsHdrAlertsHelperText.Text = "When HDR is unavailable";
            ApplySwitchState(
                SettingsHdrAlertsSwitchTrack,
                SettingsHdrAlertsSwitchKnob,
                projection.HdrAlertsEnabled,
                isReadOnly: true);
            AutomationProperties.SetName(
                SettingsHdrAlertsButton,
                projection.HdrAlertsEnabled ? "HDR alerts: on" : "HDR alerts: off");
            AutomationProperties.SetHelpText(SettingsHdrAlertsButton, projection.HdrAlertsHelpText);
            ApplyDestinationSegmentProjection(projection.Output);
            SettingsSavePathValueText.Text = projection.Output.SavePathDisplayValue;
            SettingsSavePathHelperText.Text = projection.Output.SavePathHelpText;
            AutomationProperties.SetName(SettingsSavePathValuePill, $"Save path: {projection.Output.SavePathDisplayValue}");
            AutomationProperties.SetHelpText(SettingsSavePathValuePill, projection.Output.SavePathHelpText);
            ToolTipService.SetToolTip(
                SettingsSavePathValuePill,
                $"{projection.Output.SavePathDisplayValue}. {projection.Output.SavePathHelpText}");

            SettingsOpenAfterCaptureButton.IsEnabled = !projection.Output.IsAfterCaptureReadOnly;
            ApplySwitchState(
                SettingsOpenAfterCaptureSwitchTrack,
                SettingsOpenAfterCaptureSwitchKnob,
                projection.Output.IsAfterCaptureSelected,
                projection.Output.IsAfterCaptureReadOnly);
            SettingsAfterCaptureHelperText.Text = projection.Output.AfterCaptureHelpText;
            AutomationProperties.SetName(SettingsOpenAfterCaptureButton, $"After capture: {projection.Output.AfterCaptureDisplayValue}");
            AutomationProperties.SetHelpText(SettingsOpenAfterCaptureButton, projection.Output.AfterCaptureHelpText);

            SettingsTimestampButton.IsEnabled = !projection.Output.IsTimestampReadOnly;
            ApplySwitchState(
                SettingsTimestampSwitchTrack,
                SettingsTimestampSwitchKnob,
                projection.Output.TimestampNaming,
                projection.Output.IsTimestampReadOnly);
            SettingsTimestampHelperText.Text = projection.Output.TimestampHelpText;
            AutomationProperties.SetName(
                SettingsTimestampButton,
                projection.Output.TimestampNaming ? "Timestamp naming: on" : "Timestamp naming: off");
            AutomationProperties.SetHelpText(SettingsTimestampButton, projection.Output.TimestampHelpText);

            SettingsCopyAsImageButton.IsEnabled = !projection.Output.IsCopyAsImageReadOnly;
            ApplySwitchState(
                SettingsCopyAsImageSwitchTrack,
                SettingsCopyAsImageSwitchKnob,
                projection.Output.CopyAsImage,
                projection.Output.IsCopyAsImageReadOnly);
            SettingsCopyAsImageHelperText.Text = projection.Output.CopyAsImageHelpText;
            AutomationProperties.SetName(
                SettingsCopyAsImageButton,
                projection.Output.CopyAsImage ? "Copy as image: on" : "Copy as image: off");
            AutomationProperties.SetHelpText(SettingsCopyAsImageButton, projection.Output.CopyAsImageHelpText);

            ApplyExportColorProjection(projection.Output);

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
            output.IsClipboardSelected);
        ApplySegmentState(
            SettingsDestinationFolderSegment,
            SettingsDestinationFolderText,
            output.IsFolderSelected);
        ApplySegmentState(
            SettingsDestinationBothSegment,
            SettingsDestinationBothText,
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

    private void ApplyExportColorProjection(OutputSettingsProjection output)
    {
        SettingsExportHelperText.Text = output.ExportColorHelpText;
        AutomationProperties.SetName(SettingsExportSegmentsPanel, $"Export profile: {output.ExportColorDisplayValue}");
        AutomationProperties.SetHelpText(SettingsExportSegmentsPanel, output.ExportColorHelpText);
        ToolTipService.SetToolTip(SettingsExportSegmentsPanel, output.ExportColorHelpText);

        if (output.ExportColorOptions?.Count >= 3)
        {
            ApplyExportColorOption(
                output.ExportColorOptions[0],
                SettingsExportHdr10Segment,
                SettingsExportHdr10Text);
            ApplyExportColorOption(
                output.ExportColorOptions[1],
                SettingsExportP3Segment,
                SettingsExportP3Text);
            ApplyExportColorOption(
                output.ExportColorOptions[2],
                SettingsExportSrgbSegment,
                SettingsExportSrgbText);
        }
        else
        {
            SettingsExportHdr10Segment.IsEnabled = false;
            SettingsExportP3Segment.IsEnabled = false;
            SettingsExportSrgbSegment.IsEnabled = false;
        }
    }

    private void ApplyExportColorOption(ExportColorOptionProjection option, Control segment, TextBlock label)
    {
        label.Text = option.Label;
        segment.IsEnabled = !option.IsReadOnly;
        ApplySegmentState(segment, label, option.IsSelected);
        AutomationProperties.SetName(segment, $"Export option: {option.Label}");
        AutomationProperties.SetHelpText(segment, GetExportColorOptionHelpText(option));
        ToolTipService.SetToolTip(segment, GetExportColorOptionHelpText(option));
    }

    private static string GetExportColorOptionHelpText(ExportColorOptionProjection option)
    {
        var state = option.IsSelected ? "selected" : "not selected";
        var availability = option.IsReadOnly ? "read-only" : "available";
        return $"{option.Label} is {state} and {availability}. {option.HelpText}";
    }

    private static string GetDestinationOptionHelpText(string option, bool isSelected, string pendingReason)
    {
        var state = isSelected ? "selected" : "not selected";
        return $"{option} is {state}. {pendingReason}.";
    }

    private void ApplySegmentState(Control segment, TextBlock label, bool isSelected)
    {
        segment.Background = isSelected
            ? (Brush)Application.Current.Resources["SegmentSelectedBrush"]
            : new SolidColorBrush(Windows.UI.Color.FromArgb(0x00, 0x00, 0x00, 0x00));
        segment.BorderBrush = null;
        segment.BorderThickness = new Thickness(0);
        label.Foreground = isSelected
            ? (Brush)Application.Current.Resources["TextBrush"]
            : (Brush)Application.Current.Resources["MutedTextBrush"];
    }

    private static void ApplySwitchState(Border track, Ellipse knob, bool isOn, bool isReadOnly)
    {
        track.Background = isOn
            ? (Brush)Application.Current.Resources["SwitchTrackOnBrush"]
            : (Brush)Application.Current.Resources["SwitchTrackOffBrush"];
        knob.Fill = isOn
            ? (Brush)Application.Current.Resources["SwitchKnobOnBrush"]
            : (Brush)Application.Current.Resources["SwitchKnobOffBrush"];
        knob.HorizontalAlignment = isOn ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        track.Opacity = isReadOnly ? 0.7 : 1.0;
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
        ApplyMainCaptureCardStyles();

        TrustStatusGlyph.Glyph = GetTrustStatusGlyph(projection.TrustIcon);
        TrustStatusGlyph.Foreground = statusBrush;
        TrustStatusDot.Fill = statusBrush;
        TrustStatusLabel.Text = projection.TrustLabel;
        TrustStatusLabel.Foreground = statusBrush;
        ToolTipService.SetToolTip(TrustStatusLabel, projection.TrustMessage);
        AutomationProperties.SetHelpText(TrustStatusLabel, projection.TrustMessage);
    }

    private void ApplyMainCaptureCardStyles()
    {
        ApplyMainCaptureCardStyle(SelectCaptureTargetButton, activeCaptureMode is CaptureCommandMode.Fullscreen);
        ApplyMainCaptureCardStyle(RegionSelectButton, activeCaptureMode is CaptureCommandMode.Region);
    }

    private void ApplyMainCaptureCardStyle(CaptureActionCard card, bool isActive)
    {
        card.Background = isActive
            ? (Brush)Application.Current.Resources["AccentSoftBrush"]
            : (Brush)Application.Current.Resources["SecondaryPanelBrush"];
        card.BorderBrush = isActive
            ? (Brush)Application.Current.Resources["AccentBorderBrush"]
            : (Brush)Application.Current.Resources["BorderBrush"];
        card.IconBackground = card.Background;
        card.IconBorderBrush = card.BorderBrush;
        card.IconForeground = isActive
            ? (Brush)Application.Current.Resources["AccentBrush"]
            : (Brush)Application.Current.Resources["MutedTextBrush"];
        card.ShortcutForeground = isActive
            ? (Brush)Application.Current.Resources["TextBrush"]
            : (Brush)Application.Current.Resources["SubtleTextBrush"];
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

    private async Task CompleteFullscreenCaptureAsync(
        long generation,
        int frameWidth,
        int frameHeight,
        OverlayDisplayStatus status)
    {
        if (isClosed || generation != Volatile.Read(ref previewGeneration))
        {
            return;
        }

        Logger.LogInformation(
            "Fullscreen capture auto-confirmed: frame={FrameW}x{FrameH}, status={Status}",
            frameWidth, frameHeight, status);

        var copied = false;
        try
        {
            copied = await TryExecuteOutputAsync(null, "Fullscreen capture");
        }
        finally
        {
            overlayWindow?.ApplyClipboardResult(copied, status);
            CloseOverlayWindow();
            if (!isClosed)
            {
                Logger.LogDebug("Resetting session state to Idle after fullscreen capture completed");
                ApplySessionState(CaptureSessionState.Idle());
            }
        }
    }

    private async Task<bool> TryCopyCropToClipboardAsync(ConfirmedCaptureSelection selection)
    {
        return await TryExecuteOutputAsync(
            new CropPixelRect(
                selection.PixelRegion.X,
                selection.PixelRegion.Y,
                selection.PixelRegion.Width,
                selection.PixelRegion.Height),
            "Region capture");
    }

    private async Task<bool> TryExecuteOutputAsync(
        CropPixelRect? cropRegion,
        string sourceDescription)
    {
        using var outputFrame = CloneLatestOutputFrameSnapshot();
        if (outputFrame is null)
        {
            Logger.LogWarning("Configured output SKIPPED: no WGC output frame snapshot is available");
            return false;
        }

        StopPreview(reportStopped: false);

        try
        {
            Logger.LogDebug(
                "Configured output started: source={Source}, crop={Crop}, frame={FrameW}x{FrameH}",
                sourceDescription,
                FormatCrop(cropRegion),
                outputFrame.Width,
                outputFrame.Height);

            var request = new OutputRequest
            {
                Texture = outputFrame,
                CropRegion = cropRegion,
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
                "Configured output {Outcome}: source={Source}, crop={Crop}, message={Message}, detail={Detail}, afterCapture={AfterCaptureOutcome}, afterCaptureDetail={AfterCaptureDetail}",
                result.IsSuccess ? "completed" : "failed",
                sourceDescription,
                FormatCrop(cropRegion),
                result.UserMessage,
                result.TechnicalDetail,
                result.AfterCapture?.Outcome,
                result.AfterCapture?.TechnicalDetail);
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "operation=ConfiguredOutput, stage=ExecuteOutput, detail=Configured output EXCEPTION: source={Source}, crop={Crop}",
                sourceDescription,
                FormatCrop(cropRegion));
            return false;
        }
    }

    private static string FormatCrop(CropPixelRect? cropRegion) =>
        cropRegion is { } crop
            ? $"({crop.X},{crop.Y},{crop.Width}x{crop.Height})"
            : "full-frame";

    private CapturedFrameTexture? CloneLatestOutputFrameSnapshot()
    {
        lock (previewSync)
        {
            return latestOutputFrameSnapshot is null
                ? null
                : CreateOutputFrameSnapshot(latestOutputFrameSnapshot);
        }
    }

    private CapturedFrameTexture CreateOutputFrameSnapshot(CapturedFrameTexture source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.Texture is null)
        {
            throw new InvalidOperationException("Captured frame texture is unavailable for output snapshot.");
        }

        var description = source.Texture.Description;
        description.Usage = ResourceUsage.Default;
        description.CPUAccessFlags = CpuAccessFlags.None;
        description.MiscFlags = ResourceOptionFlags.None;

        var snapshot = deviceResources.Device.CreateTexture2D(description);
        try
        {
            deviceResources.ImmediateContext.CopyResource(snapshot, source.Texture);
            return new CapturedFrameTexture(
                snapshot,
                source.Width,
                source.Height,
                "WGC output snapshot");
        }
        catch
        {
            snapshot.Dispose();
            throw;
        }
    }

    private static OverlayDisplayStatus ToOverlayDisplayStatus(PreviewReadinessState state) =>
        state is PreviewReadinessState.Ready
            ? OverlayDisplayStatus.HdrReady
            : OverlayDisplayStatus.DegradedPreview;

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
            Activated -= OnWindowActivated;
            if (backgroundWindowEventsEnabled)
            {
                AppWindow.Closing -= OnAppWindowClosing;
                AppWindow.Changed -= OnAppWindowChanged;
                backgroundWindowEventsEnabled = false;
            }

            StopPreview(reportStopped: false);
            CloseOverlayWindow();
            if (globalHotkeyRegistrar is not null)
            {
                globalHotkeyRegistrar.HotkeyPressed -= OnGlobalHotkeyPressed;
                globalHotkeyRegistrar.Dispose();
                globalHotkeyRegistrar = null;
                registeredHotkeyCount = 0;
            }

            if (trayMenu is not null)
            {
                trayMenu.CommandRequested -= OnTrayMenuCommandRequested;
                trayMenu.Dispose();
                trayMenu = null;
            }
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
