using System.Diagnostics;
using System.Globalization;
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
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Lumiere.App;

public sealed partial class MainWindow : Window
{
    private static readonly ILogger Logger = LumiereLoggerFactory.CreateLogger(LogCategories.App);
    private const string StatusCheckmarkCircleGlyph = "\uE930";
    private const string StatusMonitorGlyph = "\uE7F4";
    private const string StatusErrorCircleGlyph = "\uEA39";
    private const string StatusClockGlyph = "\uE121";
    private const string StatusErrorBadgeGlyph = "\uE783";
    private const string StatusWarningGlyph = "\uE7BA";
    private const string StatusInfoGlyph = "\uE946";
    private const int WorkAreaMarginPixels = 16;

    private readonly object previewSync = new();
    private readonly ICaptureCommandCoordinator captureCommandCoordinator;
    private readonly IOutputService outputService;
    private readonly ISettingsProvider settingsProvider;
    private readonly ISettingsWriterAggregator settingsWriter;
    private readonly IAboutInfoProvider aboutInfoProvider;
    private readonly IArtifactShellAction artifactShellAction;
    private readonly GraphicsDeviceResources deviceResources;
    private readonly Hdr10JxrCodecReadiness hdr10JxrCodecReadiness;
    private readonly IOutputValidationArtifactSource outputValidationArtifactSource;
    private readonly GraphicsEngine graphicsEngine;
    private OutputValidationArtifactSnapshot? outputValidationArtifacts;
    private ITrayMenu? trayMenu;
    private TrayMenuWindow? trayMenuWindow;
    private IGlobalHotkeyRegistrar? globalHotkeyRegistrar;
    private CaptureService? captureService;
    private CaptureSessionResources? captureSession;
    private PreviewFramePresenter? previewFramePresenter;
    private SwapChainResources? swapChainResources;
    private CaptureTarget? activeCaptureTarget;
    private CaptureTarget? lastOutputTarget;
    private CaptureCommandMode? activeCaptureMode;
    private OverlayPreviewFreezeController? overlayPreviewFreezeController;
    private CapturedFrameTexture? latestOutputFrameSnapshot;
    private PreviewReadinessStatus? activePresentationEvidence;
    private OverlayWindow? overlayWindow;
    private long previewGeneration;
    private long frameEventCount;
    private int fullscreenOutputStarted;
    private OutputResult? lastOutputResult;
    private int previewSourceWidth;
    private int previewSourceHeight;
    private int registeredHotkeyCount;
    private bool isClosed;
    private bool isDeviceDisposed;
    private bool applyingSessionState;
    private CaptureSessionState? pendingSessionState;
    private bool applyingSettingsProjection;
    private bool sizingMainPanel;
    private bool isExplicitShutdown;
    private bool hdrAlertDismissed;
    private bool isHdrAlertVisible;
    private bool backgroundWindowEventsEnabled;
    private AppShellView activeShellView = AppShellView.Main;
    private int overlayDispatcherFallbackReported;
    private int uiDispatchFailureReported;
    private int lastRequestedShellWidthDips = -1;
    private int lastRequestedShellHeightDips = -1;
    private double lastRequestedShellScale = -1;
    private InputNonClientPointerSource? nonClientPointerSource;

    public MainWindow(
        ICaptureCommandCoordinator captureCommandCoordinator,
        IOutputService outputService,
        ISettingsProvider settingsProvider,
        ISettingsWriterAggregator settingsWriter,
        IAboutInfoProvider aboutInfoProvider,
        CaptureService captureService,
        GraphicsDeviceResources deviceResources,
        Hdr10JxrCodecReadiness? hdr10JxrCodecReadiness = null,
        IOutputValidationArtifactSource? outputValidationArtifactSource = null,
        IArtifactShellAction? artifactShellAction = null)
    {
        this.captureCommandCoordinator = captureCommandCoordinator ?? throw new ArgumentNullException(nameof(captureCommandCoordinator));
        this.outputService = outputService ?? throw new ArgumentNullException(nameof(outputService));
        this.settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
        this.settingsWriter = settingsWriter ?? throw new ArgumentNullException(nameof(settingsWriter));
        this.aboutInfoProvider = aboutInfoProvider ?? throw new ArgumentNullException(nameof(aboutInfoProvider));
        this.artifactShellAction = artifactShellAction ?? throw new ArgumentNullException(nameof(artifactShellAction));
        this.captureService = captureService ?? throw new ArgumentNullException(nameof(captureService));
        this.deviceResources = deviceResources ?? throw new ArgumentNullException(nameof(deviceResources));
        this.hdr10JxrCodecReadiness = hdr10JxrCodecReadiness ?? Hdr10JxrCodecReadiness.PendingNativeWicImplementation;
        this.outputValidationArtifactSource = outputValidationArtifactSource ?? FileOutputValidationArtifactSource.CreateDefault();
        this.graphicsEngine = new GraphicsEngine(deviceResources);
        InitializeComponent();
        Title = "Lumiere";
        SystemBackdrop = new MicaBackdrop();
        ConfigureWindowPresenter();
        SuppressWindowFrameBorder();

        WindowFrameInterop.PreferRoundedCorners(this);

        WindowFrameInterop.ExtendFrameIntoClientArea(WindowNative.GetWindowHandle(this));
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
            LumiereLoggerFactory.DefaultMinimumLevel,
            "Lumiere Validation Log",
            $"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            $"OS: {Environment.OSVersion}",
            $".NET: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}",
            $"HdrConstants: WgcPixelFormat={HdrConstants.WgcFramePoolPixelFormat}, DxgiFormat={HdrConstants.DxgiSwapChainFormat}, ColorSpace={HdrConstants.DxgiColorSpace}",
            "NOTE: If NVIDIA GeForce Experience overlay appears (Alt+Z), disable 'In-Game Overlay' in GeForce Experience settings for stable capture.",
            "NOTE: Windows Graphics Capture may show a yellow system border. This unpackaged build cannot guarantee graphicsCaptureWithoutBorder consent.");

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
        MinimizeButton.IsEnabled = IsBackgroundAvailable;
    }

    public void AttachTrayMenuWindow(TrayMenuWindow window)
    {
        trayMenuWindow = window ?? throw new ArgumentNullException(nameof(window));
    }

    public TrayMenuSnapshot CreateTrayMenuSnapshot() =>
        CreateTrayMenuSnapshot(captureService?.CurrentSessionState ?? CaptureSessionState.Idle(), lastOutputResult);

    private OutputValidationArtifactSnapshot LoadOutputValidationArtifacts()
    {
        if (outputValidationArtifacts is not null)
        {
            return outputValidationArtifacts;
        }

        try
        {
            outputValidationArtifacts = outputValidationArtifactSource.Load();
            Logger.LogInformation(
                "operation=OutputValidationArtifacts, stage=Load, artifacts={ArtifactCount}, issues={IssueCount}",
                outputValidationArtifacts.Artifacts.Count,
                outputValidationArtifacts.LoadIssues.Count);
            foreach (var issue in outputValidationArtifacts.LoadIssues)
            {
                Logger.LogWarning(
                    "operation=OutputValidationArtifacts, stage=LoadIssue, path={Path}, detail={Detail}",
                    issue.Path,
                    issue.Detail);
            }

            return outputValidationArtifacts;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            Logger.LogWarning(
                ex,
                "operation=OutputValidationArtifacts, stage=LoadFailed, detail=Validation artifacts ignored for this session.");
            outputValidationArtifacts = OutputValidationArtifactSnapshot.Empty;
            return outputValidationArtifacts;
        }
    }

    public void AttachGlobalHotkeys(IGlobalHotkeyRegistrar registrar)
    {
        globalHotkeyRegistrar?.Dispose();
        globalHotkeyRegistrar = registrar ?? throw new ArgumentNullException(nameof(registrar));
        globalHotkeyRegistrar.HotkeyPressed += OnGlobalHotkeyPressed;
        RegisterConfiguredHotkeys();
        MinimizeButton.IsEnabled = IsBackgroundAvailable;
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

    private void OnMinimizeButtonClick(object sender, RoutedEventArgs e)
    {
        if (IsBackgroundAvailable)
        {
            HideToBackground("minimize");
        }
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

    private void OnSettingsHdrAlertsToggled(object sender, RoutedEventArgs e)
    {
        if (applyingSettingsProjection)
        {
            return;
        }

        if (sender is not ToggleSwitch toggleSwitch)
        {
            return;
        }

        settingsWriter.SetHdrAlertsEnabled(toggleSwitch.IsOn);
        hdrAlertDismissed = false;
        var currentState = captureService?.CurrentSessionState ?? CaptureSessionState.Idle();
        ApplySettingsProjection(currentState);
        UpdateMainPanelProjection(currentState, lastOutputResult);
        UpdateTrayMenu(currentState);
        Logger.LogDebug(
            "HDR alert preference updated in settings: enabled={HdrAlertsEnabled}",
            settingsProvider.HdrAlertsEnabled);
    }

    private void OnSettingsTimestampToggled(object sender, RoutedEventArgs e)
    {
        if (applyingSettingsProjection)
        {
            return;
        }

        if (sender is not ToggleSwitch toggleSwitch)
        {
            return;
        }

        settingsWriter.SetTimestampNaming(toggleSwitch.IsOn);
        var currentState = captureService?.CurrentSessionState ?? CaptureSessionState.Idle();
        ApplySettingsProjection(currentState);
        Logger.LogDebug(
            "Timestamp naming preference updated in settings: enabled={TimestampNaming}",
            settingsProvider.TimestampNaming);
    }

    private OutputProfileExecutionCapabilities ResolveOutputCapabilities(
        OutputValidationArtifactSnapshot? validationSnapshot = null)
    {
        var snapshot = validationSnapshot ?? LoadOutputValidationArtifacts();
        return OutputProfileExecutionCapabilities.ResolveHdr10JxrReleaseCapabilities(
            hdr10JxrCodecReadiness,
            snapshot.Artifacts,
            aboutInfoProvider.Version);
    }

    private async void OnSettingsSavePathBrowseClick(object sender, RoutedEventArgs e)
    {
        if (applyingSettingsProjection)
        {
            return;
        }

        var picker = new FolderPicker();
        var hwnd = WindowNative.GetWindowHandle(this);
        InitializeWithWindow.Initialize(picker, hwnd);
        picker.SuggestedStartLocation = PickerLocationId.Desktop;
        picker.FileTypeFilter.Add("*");

        try
        {
            var folder = await picker.PickSingleFolderAsync();
            if (folder is not null)
            {
                settingsWriter.SetSavePath(folder.Path);
                var currentState = captureService?.CurrentSessionState ?? CaptureSessionState.Idle();
                ApplySettingsProjection(currentState);
                Logger.LogDebug(
                    "Save path updated in settings: path={SavePath}",
                    settingsProvider.SavePath);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Folder picker failed; save path unchanged.");
        }
    }

    private void OnSettingsAfterCaptureToggled(object sender, RoutedEventArgs e)
    {
        if (applyingSettingsProjection)
        {
            return;
        }

        if (sender is not ToggleSwitch toggleSwitch)
        {
            return;
        }

        var next = toggleSwitch.IsOn
            ? AfterCaptureBehavior.Open
            : AfterCaptureBehavior.None;
        settingsWriter.SetAfterCaptureBehavior(next);
        var currentState = captureService?.CurrentSessionState ?? CaptureSessionState.Idle();
        ApplySettingsProjection(currentState);
        Logger.LogDebug(
            "After-capture behavior updated in settings: behavior={AfterCaptureBehavior}",
            settingsProvider.AfterCaptureBehavior);
    }

    private async void OnFullscreenShortcutClick(object sender, RoutedEventArgs e)
    {
        if (applyingSettingsProjection)
        {
            return;
        }

        var newShortcut = await ShowShortcutCaptureDialog("Fullscreen Capture Shortcut");
        if (newShortcut is not null)
        {
            settingsWriter.SetFullscreenShortcut(newShortcut);
            var currentState = captureService?.CurrentSessionState ?? CaptureSessionState.Idle();
            ApplySettingsProjection(currentState);
            Logger.LogDebug("Fullscreen shortcut updated: {Shortcut}", settingsProvider.FullscreenShortcut);
        }
    }

    private async void OnRegionShortcutClick(object sender, RoutedEventArgs e)
    {
        if (applyingSettingsProjection)
        {
            return;
        }

        var newShortcut = await ShowShortcutCaptureDialog("Region Capture Shortcut");
        if (newShortcut is not null)
        {
            settingsWriter.SetRegionShortcut(newShortcut);
            var currentState = captureService?.CurrentSessionState ?? CaptureSessionState.Idle();
            ApplySettingsProjection(currentState);
            Logger.LogDebug("Region shortcut updated: {Shortcut}", settingsProvider.RegionShortcut);
        }
    }

    private async Task<string?> ShowShortcutCaptureDialog(string title)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = "Press a key combination (Ctrl, Alt, or Shift + another key). Press Escape to cancel.",
            CloseButtonText = "Cancel",
            XamlRoot = Content.XamlRoot,
        };

        var tcs = new TaskCompletionSource<string?>();
        var keyDownHandler = (Microsoft.UI.Xaml.Input.KeyEventHandler)((sender, args) =>
        {
            var key = args.OriginalKey;
            if (key == Windows.System.VirtualKey.Escape)
            {
                tcs.TrySetResult(null);
                dialog.Hide();
                return;
            }

            var ctrl = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
            var shift = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
            var alt = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Menu).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

            if (!ctrl && !shift && !alt)
            {
                return;
            }

            var parts = new List<string>();
            if (ctrl) parts.Add("Ctrl");
            if (shift) parts.Add("Shift");
            if (alt) parts.Add("Alt");
            parts.Add(key.ToString());
            var shortcut = string.Join("+", parts);
            tcs.TrySetResult(shortcut);
            dialog.Hide();
        });

        dialog.PreviewKeyDown += keyDownHandler;
        var showTask = dialog.ShowAsync();
        var result = await tcs.Task;
        dialog.PreviewKeyDown -= keyDownHandler;
        return result;
    }

    private void OnSettingsDestinationSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (applyingSettingsProjection || sender is not RadioButtons radioButtons)
        {
            return;
        }

        var target = radioButtons.SelectedIndex switch
        {
            1 => OutputTarget.Folder,
            2 => OutputTarget.Both,
            _ => OutputTarget.Clipboard,
        };

        SetOutputTargetFromSettings(target);
    }

    private void SetOutputTargetFromSettings(OutputTarget target)
    {
        if (applyingSettingsProjection || settingsProvider.OutputTarget == target)
        {
            return;
        }

        settingsWriter.SetOutputTarget(target);
        ApplySettingsProjection(captureService?.CurrentSessionState ?? CaptureSessionState.Idle());
        Logger.LogDebug(
            "Output target preference updated in settings: target={OutputTarget}",
            settingsProvider.OutputTarget);
    }

    private void OnSettingsCopyAsImageToggled(object sender, RoutedEventArgs e)
    {
        if (applyingSettingsProjection)
        {
            return;
        }

        if (sender is not ToggleSwitch toggleSwitch)
        {
            return;
        }

        settingsWriter.SetCopyAsImage(toggleSwitch.IsOn);
        ApplySettingsProjection(captureService?.CurrentSessionState ?? CaptureSessionState.Idle());
        Logger.LogDebug(
            "Copy-as-image preference updated in settings: enabled={CopyAsImage}",
            settingsProvider.CopyAsImage);
    }

    private void OnSettingsExportProfileChecked(object sender, RoutedEventArgs e)
    {
        if (applyingSettingsProjection || sender is not RadioButton radioButton || radioButton.Tag is not string format)
        {
            return;
        }

        if (string.Equals(settingsProvider.ExportColorFormat, format, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        settingsWriter.SetExportColorFormat(format);
        var currentState = captureService?.CurrentSessionState ?? CaptureSessionState.Idle();
        ApplySettingsProjection(currentState);
        UpdateMainPanelProjection(currentState, lastOutputResult);
        UpdateTrayMenu(currentState);
        overlayWindow?.ApplyState(CreateOverlayState(currentState));
        Logger.LogDebug(
            "Export color preference updated in settings: format={ExportColorFormat}",
            settingsProvider.ExportColorFormat);
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
                        InteropFailureDiagnostics.LogAndFormat(exception, Logger))));
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

    public void DispatchTrayCommand(TrayMenuCommand command)
    {
        if (isClosed)
        {
            return;
        }

        if (!RootGrid.DispatcherQueue.TryEnqueue(async () => await HandleTrayMenuCommandAsync(command)))
        {
            Logger.LogWarning("Tray command dropped because UI dispatcher rejected it: command={Command}", command);
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
        var projection = AppShellProjection.Project(state, view, lastOutputResult, settingsProvider.HdrAlertsEnabled);
        ClearHeaderDragRegion();
        activeShellView = projection.ActiveView;

        MainPanelRoot.Visibility = projection.IsMainPanelVisible
            ? Visibility.Visible
            : Visibility.Collapsed;
        SettingsPanelRoot.Visibility = projection.IsSettingsVisible
            ? Visibility.Visible
            : Visibility.Collapsed;

        UpdateMainPanelProjection(state, lastOutputResult);
        ApplySettingsProjection(state);
        SizeToActiveShellView();
        UpdateHeaderDragRegion();
    }

    private void SizeToActiveShellView()
    {
        var layout = AppShellLayoutProjection.Project(activeShellView, isHdrAlertVisible);
        var scale = RootGrid.XamlRoot?.RasterizationScale ?? 1.0;
        if (layout.WidthDips == lastRequestedShellWidthDips
            && layout.HeightDips == lastRequestedShellHeightDips
            && Math.Abs(scale - lastRequestedShellScale) < 0.001)
        {
            return;
        }

        lastRequestedShellWidthDips = layout.WidthDips;
        lastRequestedShellHeightDips = layout.HeightDips;
        lastRequestedShellScale = scale;
        SizeToShell(layout.WidthDips, layout.HeightDips, scale);
    }

    private void SizeToShell(int widthDips, int heightDips, double scale)
    {
        if (sizingMainPanel)
        {
            return;
        }

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

        lastOutputResult = null;
        lastOutputTarget = null;
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
                overlayWindow!.PreviewSurface,
                new SwapChainTargetHint(
                    target.DisplayIdentity?.DeviceName ?? target.DisplayName,
                    target.DisplayIdentity?.Left,
                    target.DisplayIdentity?.Top,
                    target.DisplayIdentity?.Width ?? target.Size.Width,
                    target.DisplayIdentity?.Height ?? target.Size.Height));
            previewFramePresenter = new PreviewFramePresenter(deviceResources!, swapChainResources);
            activeCaptureTarget = target;
            overlayPreviewFreezeController = new OverlayPreviewFreezeController(activeCaptureMode.Value);
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
        var frameDisposition = OverlayPreviewFrameDisposition.Continue;
        var shouldCompleteFullscreenCapture = false;
        CapturedFrameTexture? snapshotToDispose = null;
        using (frame)
        {
            lock (previewSync)
            {
                var currentGeneration = Volatile.Read(ref previewGeneration);
                var callbacksFrozen = overlayPreviewFreezeController is { AcceptsCallbacks: false };
                if (generation != currentGeneration || previewFramePresenter is null || callbacksFrozen)
                {
                    Logger.LogDebug(
                        "operation=FrameCallback, stage=Reject, detail=Stale callback rejected: frameGeneration={FrameGeneration}, currentGeneration={CurrentGeneration}, hasPresenter={HasPresenter}, callbacksFrozen={CallbacksFrozen}",
                        generation, currentGeneration, previewFramePresenter is not null, callbacksFrozen);
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
                    frameDisposition = overlayPreviewFreezeController?.OnFramePresented(requiresRecreation: true)
                        ?? OverlayPreviewFrameDisposition.Continue;
                }
                else
                {
                    var outputSnapshot = CreateOutputFrameSnapshot(frame);
                    snapshotToDispose = latestOutputFrameSnapshot;
                    latestOutputFrameSnapshot = outputSnapshot;
                    result = previewFramePresenter.PresentFrame(frame);
                    target = activeCaptureTarget;
                    frameDisposition = overlayPreviewFreezeController?.OnFramePresented(requiresRecreation: false)
                        ?? OverlayPreviewFrameDisposition.Continue;
                    shouldCompleteFullscreenCapture = activeCaptureMode is CaptureCommandMode.Fullscreen
                        && target is not null
                        && Interlocked.CompareExchange(ref fullscreenOutputStarted, 1, 0) == 0;
                }
            }
        }
        snapshotToDispose?.Dispose();

        if (frameDisposition is OverlayPreviewFrameDisposition.FreezeAfterPresent)
        {
            FreezeOverlayPreviewCaptureSession(generation);
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
            overlayPreviewFreezeController = null;
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

    private void FreezeOverlayPreviewCaptureSession(long generation)
    {
        CaptureSessionResources? captureSessionToDispose;
        CaptureTarget? target;
        lock (previewSync)
        {
            if (generation != Volatile.Read(ref previewGeneration)
                || activeCaptureMode is not CaptureCommandMode.Region
                || overlayPreviewFreezeController is not { IsFrozen: true })
            {
                return;
            }

            captureSessionToDispose = captureSession;
            captureSession = null;
            target = activeCaptureTarget;
        }

        if (captureSessionToDispose is null)
        {
            return;
        }

        Logger.LogInformation(
            "operation=OverlayPreview, stage=Freeze, detail=Preview frozen after first presented frame for precise region selection: generation={Generation}, target={Target}",
            generation,
            target?.DisplayName ?? "unknown");
        captureSessionToDispose.Dispose();
    }

    private void ApplyFrameReadiness(long generation, PreviewReadinessStatus readiness)
    {
        if (!ShouldAcceptPreviewCallbacks(generation))
        {
            Logger.LogDebug(
                "operation=FrameReadiness, stage=Reject, detail=Readiness callback rejected for inactive or frozen preview generation={Generation}",
                generation);
            return;
        }

        if (!TryEnqueueUi(() =>
            {
                if (ShouldAcceptPreviewCallbacks(generation))
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
        if (!ShouldAcceptPreviewCallbacks(generation))
        {
            Logger.LogDebug(
                "operation=FrameDiagnostic, stage=Reject, detail=Diagnostic callback rejected for inactive or frozen preview generation={Generation}",
                generation);
            return;
        }

        var frameNumber = Interlocked.Increment(ref frameEventCount);
        Logger.LogDebug("Frame diagnostic: generation={Generation}, event={Event}, detail={Detail}", generation, frameNumber, detail);
    }

    private bool ShouldAcceptPreviewCallbacks(long generation)
    {
        lock (previewSync)
        {
            return generation == Volatile.Read(ref previewGeneration)
                && overlayPreviewFreezeController is not { AcceptsCallbacks: false };
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
            overlayPreviewFreezeController = null;
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

    /// <summary>
    /// Consolidates teardown and return-to-ready into a single atomic flow.
    /// Stops preview, closes overlay, and resets to Idle state without relying
    /// on fragile sequential Disposed then Idle UI calls.
    /// </summary>
    private void StopPreviewAndResetToIdle()
    {
        Logger.LogDebug("StopPreviewAndResetToIdle: beginning consolidated teardown and reset");
        StopPreview(reportStopped: false);
        CloseOverlayWindow();
        if (!isClosed)
        {
            Logger.LogDebug("StopPreviewAndResetToIdle: applying Idle state");
            ApplySessionState(CaptureSessionState.Idle());
        }
    }

    private void ApplySessionState(CaptureSessionState state)
    {
        // Must be called on the UI thread. Future entry points (hotkey, tray) must dispatch first.
        Debug.Assert(RootGrid.DispatcherQueue.HasThreadAccess, "ApplySessionState must be called on the UI thread.");

        if (applyingSessionState)
        {
            // Last-write-wins: overwrite the pending state with the latest.
            // For state machines, the latest state is authoritative 鈥?intermediate states are stale.
            // The pending state will be applied after the current projection completes.
            pendingSessionState = state;
            Logger.LogDebug(
                "ApplySessionState REENTRANT: overwrote pending state with latest={Status}",
                state.Status);
            return;
        }

        applyingSessionState = true;
        try
        {
            var validatedState = state ?? throw new ArgumentNullException(nameof(state));
            captureService?.UpdateSessionState(validatedState);
            UpdateMainPanelProjection(validatedState, lastOutputResult);
            UpdateTrayMenu(validatedState);
            var overlayState = CreateOverlayState(validatedState);
            overlayWindow?.ApplyState(overlayState);
            if (overlayWindow is not null && overlayState.RequiresFailureTeardown)
            {
                StopPreview(reportStopped: false);
                CloseOverlayWindow();
            }

            // Apply any pending state that arrived during projection (last-write-wins).
            // Guard against infinite loops from rapid reentrant calls and window close.
            const int maxDeferredIterations = 10;
            var deferredCount = 0;
            while (pendingSessionState is not null && !isClosed && deferredCount < maxDeferredIterations)
            {
                var queuedState = pendingSessionState;
                pendingSessionState = null;
                deferredCount++;

                Logger.LogDebug(
                    "ApplySessionState DEFERRED: applying pending state={Status} (iteration {Count})",
                    queuedState.Status, deferredCount);

                captureService?.UpdateSessionState(queuedState);
                UpdateMainPanelProjection(queuedState, lastOutputResult);
                UpdateTrayMenu(queuedState);
                var queuedOverlayState = CreateOverlayState(queuedState);
                overlayWindow?.ApplyState(queuedOverlayState);
                if (overlayWindow is not null && queuedOverlayState.RequiresFailureTeardown)
                {
                    StopPreview(reportStopped: false);
                    CloseOverlayWindow();
                }
            }

            if (deferredCount >= maxDeferredIterations)
            {
                Logger.LogWarning(
                    "ApplySessionState DEFERRED: hit max iterations ({Max}), clearing pending state",
                    maxDeferredIterations);
                pendingSessionState = null;
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
            trayMenu?.Update(CreateTrayMenuSnapshot(state, lastOutputResult));
        }
        catch (Exception exception)
        {
            Logger.LogWarning(exception, "Tray menu projection update failed.");
        }
    }

    private TrayMenuSnapshot CreateTrayMenuSnapshot(CaptureSessionState state, OutputResult? outputResult = null)
    {
        var validation = LoadOutputValidationArtifacts();
        var outputCapabilities = ResolveOutputCapabilities(validation);
        var projection = TrayMenuProjection.Project(
            state,
            settingsProvider,
            aboutInfoProvider,
            activeCaptureMode,
            outputResult,
            settingsProvider.HdrAlertsEnabled,
            validation.Artifacts,
            outputCapabilities,
            lastOutputTarget);
        return new TrayMenuSnapshot(
            projection.AppName,
            projection.HdrStatusLabel,
            projection.HdrStatusDetail,
            projection.OutputProfileLabel,
            projection.OutputProfileStatusLabel,
            projection.OutputProfileDetail,
            projection.OutputProfileSeverity,
            projection.FidelityClaimLabel,
            projection.FidelityClaimDetail,
            projection.FidelityClaimSeverity,
            projection.TrayAlertMessage,
            projection.TrayAlertSeverity,
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
        var validation = LoadOutputValidationArtifacts();
        var outputCapabilities = ResolveOutputCapabilities(validation);
        var projection = SettingsPanelProjection.Project(
            settingsProvider,
            state,
            validation,
            aboutInfoProvider: aboutInfoProvider,
            executionCapabilities: outputCapabilities);

        applyingSettingsProjection = true;
        try
        {
            SettingsFullscreenShortcutButton.IsEnabled = !projection.FullscreenShortcut.IsReadOnly;
            SettingsFullscreenShortcutValueText.Text = projection.FullscreenShortcut.DisplayValue;
            AutomationProperties.SetName(SettingsFullscreenShortcutButton, projection.FullscreenShortcut.Label);
            AutomationProperties.SetHelpText(SettingsFullscreenShortcutButton, projection.FullscreenShortcut.HelpText);
            ToolTipService.SetToolTip(SettingsFullscreenShortcutButton, projection.FullscreenShortcut.PendingReason);

            SettingsRegionShortcutButton.IsEnabled = !projection.RegionShortcut.IsReadOnly;
            SettingsRegionShortcutValueText.Text = projection.RegionShortcut.DisplayValue;
            AutomationProperties.SetName(SettingsRegionShortcutButton, projection.RegionShortcut.Label);
            AutomationProperties.SetHelpText(SettingsRegionShortcutButton, projection.RegionShortcut.HelpText);
            ToolTipService.SetToolTip(SettingsRegionShortcutButton, projection.RegionShortcut.PendingReason);

            SettingsHdrAlertsToggleSwitch.IsEnabled = !projection.IsHdrAlertsReadOnly;
            SettingsHdrAlertsToggleSwitch.IsOn = projection.HdrAlertsEnabled;
            AutomationProperties.SetName(
                SettingsHdrAlertsToggleSwitch,
                projection.HdrAlertsEnabled ? "HDR alerts: on" : "HDR alerts: off");
            AutomationProperties.SetHelpText(SettingsHdrAlertsToggleSwitch, "Show warnings when HDR is unavailable, degraded, unsupported, or failed.");
            SettingsTargetAwareStateLabel.Text = projection.TargetAwareStateLabel;
            SettingsTargetAwareStateHelpText.Text = projection.TargetAwareStateHelpText;
            AutomationProperties.SetName(SettingsTargetAwareStateLabel, $"Target-aware state: {projection.TargetAwareStateLabel}");
            AutomationProperties.SetHelpText(SettingsTargetAwareStateLabel, projection.TargetAwareStateHelpText);
            ApplyTargetEvidenceProjection(projection.TargetEvidence);
            ApplyDestinationSelectionProjection(projection.Output);

            var showSavePath = projection.Output.IsFolderSelected || projection.Output.IsBothSelected;
            SettingsSavePathRow.Visibility = showSavePath ? Visibility.Visible : Visibility.Collapsed;

            SettingsSavePathValueText.Text = projection.Output.SavePathDisplayValue;
            AutomationProperties.SetName(SettingsSavePathValueBorder, $"Save path: {projection.Output.SavePathDisplayValue}");
            AutomationProperties.SetHelpText(SettingsSavePathValueBorder, projection.Output.SavePathHelpText);
            SettingsSavePathBrowseButton.IsEnabled = !projection.Output.IsSavePathReadOnly;
            AutomationProperties.SetHelpText(SettingsSavePathBrowseButton, projection.Output.SavePathHelpText);
            ToolTipService.SetToolTip(SettingsSavePathBrowseButton, projection.Output.SavePathHelpText);

            var showClipboard = projection.Output.IsClipboardSelected || projection.Output.IsBothSelected;
            SettingsClipboardSection.Visibility = showClipboard ? Visibility.Visible : Visibility.Collapsed;

            SettingsOpenAfterCaptureToggleSwitch.IsEnabled = !projection.Output.IsAfterCaptureReadOnly;
            SettingsOpenAfterCaptureToggleSwitch.IsOn = projection.Output.IsAfterCaptureSelected;
            AutomationProperties.SetName(SettingsOpenAfterCaptureToggleSwitch, $"Open after capture: {(projection.Output.IsAfterCaptureSelected ? "on" : "off")}");
            AutomationProperties.SetHelpText(SettingsOpenAfterCaptureToggleSwitch, projection.Output.AfterCaptureHelpText);
            ToolTipService.SetToolTip(SettingsOpenAfterCaptureToggleSwitch, projection.Output.AfterCaptureHelpText);

            SettingsTimestampToggleSwitch.IsEnabled = !projection.Output.IsTimestampReadOnly;
            SettingsTimestampToggleSwitch.IsOn = projection.Output.TimestampNaming;
            AutomationProperties.SetName(
                SettingsTimestampToggleSwitch,
                projection.Output.TimestampNaming ? "Timestamp naming: on" : "Timestamp naming: off");
            AutomationProperties.SetHelpText(SettingsTimestampToggleSwitch, projection.Output.TimestampHelpText);
            ToolTipService.SetToolTip(SettingsTimestampToggleSwitch, projection.Output.TimestampHelpText);

            SettingsCopyAsImageToggleSwitch.IsEnabled = !projection.Output.IsCopyAsImageReadOnly;
            SettingsCopyAsImageToggleSwitch.IsOn = projection.Output.CopyAsImage;
            AutomationProperties.SetName(
                SettingsCopyAsImageToggleSwitch,
                projection.Output.CopyAsImage ? "Copy as image: on" : "Copy as image: off");
            AutomationProperties.SetHelpText(SettingsCopyAsImageToggleSwitch, projection.Output.CopyAsImageHelpText);
            ToolTipService.SetToolTip(SettingsCopyAsImageToggleSwitch, projection.Output.CopyAsImageHelpText);

            ApplyExportColorProjection(projection.Output);
            ApplyValidationProjection(projection.Validation);

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

    private void ApplyDestinationSelectionProjection(OutputSettingsProjection output)
    {
        SettingsDestinationRadioButtons.IsEnabled = !output.IsReadOnly;
        SettingsDestinationRadioButtons.SelectedIndex = output.IsFolderSelected
            ? 1
            : output.IsBothSelected
                ? 2
                : 0;
        var helpText = $"{output.PendingReason}. Current selection: {output.DisplayValue}.";
        AutomationProperties.SetName(SettingsDestinationRadioButtons, "Destination");
        AutomationProperties.SetHelpText(SettingsDestinationRadioButtons, helpText);
        ToolTipService.SetToolTip(SettingsDestinationRadioButtons, helpText);
    }

    private void ApplyTargetEvidenceProjection(TargetEvidenceProjection evidence)
    {
        SettingsTargetEvidenceScopeText.Text = evidence.ScopeLabel;
        SettingsTargetEvidenceTargetText.Text = evidence.TargetLabel;
        SettingsTargetEvidenceStageText.Text = $"Readiness stage: {evidence.ReadinessStageLabel}";
        SettingsTargetEvidenceDetailText.Text = evidence.Detail;
        var helpText = $"{evidence.ScopeLabel}. {evidence.TargetLabel}. Readiness stage: {evidence.ReadinessStageLabel}. {evidence.Detail}";
        AutomationProperties.SetName(SettingsTargetEvidenceScopeText, evidence.ScopeLabel);
        AutomationProperties.SetHelpText(SettingsTargetEvidenceScopeText, helpText);
        ToolTipService.SetToolTip(SettingsTargetEvidenceDetailText, helpText);
    }

    private void ApplyExportColorProjection(OutputSettingsProjection output)
    {
        AutomationProperties.SetName(SettingsExportOptionsPanel, $"Export profile: {output.ExportColorDisplayValue}");
        AutomationProperties.SetHelpText(SettingsExportOptionsPanel, output.ExportColorHelpText);
        ToolTipService.SetToolTip(SettingsExportOptionsPanel, output.ExportColorHelpText);
        SettingsExportProfileHelpText.Text = output.ExportColorHelpText;
        ApplyOutputProfileContractProjection(output.SelectedProfileContract);

        if (output.ExportColorOptions?.Count >= 3)
        {
            ApplyExportColorOption(
                output.ExportColorOptions[0],
                SettingsExportHdr10RadioButton,
                SettingsExportHdr10Text,
                SettingsExportHdr10StatusText);
            ApplyExportColorOption(
                output.ExportColorOptions[1],
                SettingsExportP3RadioButton,
                SettingsExportP3Text,
                SettingsExportP3StatusText);
            ApplyExportColorOption(
                output.ExportColorOptions[2],
                SettingsExportSrgbRadioButton,
                SettingsExportSrgbText,
                SettingsExportSrgbStatusText);
        }
        else
        {
            SettingsExportHdr10RadioButton.IsEnabled = false;
            SettingsExportP3RadioButton.IsEnabled = false;
            SettingsExportSrgbRadioButton.IsEnabled = false;
        }
    }

    private async void OnValidationOpenWorkspaceClick(object sender, RoutedEventArgs e)
    {
        var snapshot = LoadOutputValidationArtifacts();
        var path = snapshot.Workspace.IsConfigured
            ? snapshot.Workspace.DirectoryPath
            : PerfectHdrFidelityProjection.ProjectValidationRecord(
                aboutInfoProvider.Version,
                snapshot).ValidationWorkspacePath;
        await OpenValidationPathAsync(path, ArtifactShellActionKind.Open, "validation workspace");
    }

    private async void OnValidationOpenTemplateClick(object sender, RoutedEventArgs e)
    {
        var snapshot = LoadOutputValidationArtifacts();
        var path = snapshot.Workspace.SampleTemplatePath
            ?? PerfectHdrFidelityProjection.ProjectValidationRecord(
                aboutInfoProvider.Version,
                snapshot).ValidationTemplatePath;
        await OpenValidationPathAsync(path, ArtifactShellActionKind.Open, "validation template");
    }

    private async void OnValidationOpenResourceTrendTemplateClick(object sender, RoutedEventArgs e)
    {
        var snapshot = LoadOutputValidationArtifacts();
        var path = snapshot.Workspace.ResourceTrendTemplatePath
            ?? PerfectHdrFidelityProjection.ProjectValidationRecord(
                aboutInfoProvider.Version,
                snapshot).ResourceTrendTemplatePath;
        await OpenValidationPathAsync(path, ArtifactShellActionKind.Open, "resource trend template");
    }

    private async void OnValidationOpenResourceTrendScriptClick(object sender, RoutedEventArgs e)
    {
        var snapshot = LoadOutputValidationArtifacts();
        var path = snapshot.Workspace.ResourceTrendScriptPath
            ?? PerfectHdrFidelityProjection.ProjectValidationRecord(
                aboutInfoProvider.Version,
                snapshot).ResourceTrendScriptPath;
        await OpenValidationPathAsync(path, ArtifactShellActionKind.Open, "resource trend script");
    }

    private void OnValidationCopyResourceTrendCommandClick(object sender, RoutedEventArgs e)
    {
        var snapshot = LoadOutputValidationArtifacts();
        var record = PerfectHdrFidelityProjection.ProjectValidationRecord(
            aboutInfoProvider.Version,
            snapshot);
        var command = ResourceTrendValidationCommandProjection.Create(
            record.ResourceTrendScriptPath,
            record.ValidationWorkspacePath,
            Process.GetCurrentProcess().Id);
        if (string.IsNullOrWhiteSpace(command))
        {
            Logger.LogWarning(
                "operation=ValidationWorkspace, stage=CopyTrendCommandSkipped, detail=Command inputs are unavailable.");
            return;
        }

        var dataPackage = new DataPackage();
        dataPackage.SetText(command);
        Clipboard.SetContent(dataPackage);
        Logger.LogInformation(
            "operation=ValidationWorkspace, stage=CopyTrendCommandSucceeded, processId={ProcessId}, workspace={WorkspacePath}",
            Process.GetCurrentProcess().Id,
            record.ValidationWorkspacePath);
    }

    private async void OnValidationCreateDraftClick(object sender, RoutedEventArgs e)
    {
        var currentState = captureService?.CurrentSessionState ?? CaptureSessionState.Idle();
        var requestedProfile = OutputProfileContract.FromSettingsValue(settingsProvider.ExportColorFormat);
        var result = outputValidationArtifactSource.CreateDraft(
            new OutputValidationDraftRequest(
                aboutInfoProvider.Version,
                settingsProvider.OutputTarget,
                requestedProfile,
                currentState,
                CreateCurrentSessionValidationHint()));

        if (!result.IsSuccess)
        {
            Logger.LogWarning(
                "operation=ValidationWorkspace, stage=CreateDraftFailed, detail={Detail}",
                result.TechnicalDetail);
            return;
        }

        Logger.LogInformation(
            "operation=ValidationWorkspace, stage=CreateDraftSucceeded, path={Path}, outputTarget={OutputTarget}, profile={Profile}",
            result.DraftPath,
            settingsProvider.OutputTarget,
            requestedProfile.Label);
        await OpenValidationPathAsync(result.DraftPath, ArtifactShellActionKind.Open, "validation draft");
    }

    private OutputValidationCurrentSessionHint CreateCurrentSessionValidationHint() =>
        new(
            deviceResources.AdapterName,
            CreateCurrentSessionDpiScales(),
            CreateCurrentSessionDisplaySetupHint());

    private IReadOnlyList<string> CreateCurrentSessionDpiScales()
    {
        var scale = RootGrid.XamlRoot?.RasterizationScale;
        if (scale is null || scale <= 0)
        {
            return [];
        }

        return
        [
            $"{(scale.Value * 100).ToString("0.##", CultureInfo.InvariantCulture)}%",
        ];
    }

    private string? CreateCurrentSessionDisplaySetupHint()
    {
        var target = captureService?.CurrentSessionState?.Target;
        var areas = DisplayArea.FindAll();
        var topology = areas.Count switch
        {
            <= 0 => null,
            1 => "single display",
            _ => $"{areas.Count} displays",
        };

        var targetHint = target?.DisplayIdentity is { Left: { } left, Top: { } top } identity
            ? $"{target.DisplayName} at {left},{top} {identity.Width}x{identity.Height}"
            : target is not null
                ? $"{target.DisplayName} {target.Size.Width}x{target.Size.Height}"
                : null;

        if (topology is null && targetHint is null)
        {
            return null;
        }

        if (topology is not null && targetHint is not null)
        {
            return $"{topology}; active target {targetHint}";
        }

        return topology ?? $"active target {targetHint}";
    }

    private async void OnValidationOpenLatestEvidenceClick(object sender, RoutedEventArgs e)
    {
        var snapshot = LoadOutputValidationArtifacts();
        var path = PerfectHdrFidelityProjection
            .ProjectValidationEvidenceSummary(snapshot)
            .LatestArtifactPath;
        await OpenValidationPathAsync(path, ArtifactShellActionKind.Open, "latest validation evidence");
    }

    private void OnValidationReloadArtifactsClick(object sender, RoutedEventArgs e)
    {
        ReloadOutputValidationArtifacts();
    }

    private void ApplyOutputProfileContractProjection(OutputProfileContractProjection contract)
    {
        SettingsOutputContractSourceText.Text = contract.SourcePolicy;
        SettingsOutputContractDestinationText.Text = contract.DestinationPolicy;
        SettingsOutputContractConversionText.Text = contract.ConversionPolicy;
        SettingsOutputContractMetadataText.Text = contract.MetadataPolicy;
        SettingsOutputContractViewerText.Text = contract.ViewerCompatibilityPolicy;
        var contractSummary =
            $"Selected profile contract. Source format: {contract.SourcePixelFormatLabel}. Destination format: {contract.DestinationPixelFormatLabel}. Transfer: {contract.TransferFunctionLabel}. Primaries: {contract.ColorPrimariesLabel}. Conversion mode: {contract.ConversionPolicyLabel}. Metadata mode: {contract.MetadataPolicyLabel}. Target apps: {contract.TargetAppAssumptionLabel}. Source: {contract.SourcePolicy}. Destination: {contract.DestinationPolicy}. Viewer: {contract.ViewerCompatibilityPolicy}";
        AutomationProperties.SetHelpText(SettingsExportProfileHelpText, contractSummary);
        ToolTipService.SetToolTip(SettingsExportProfileHelpText, contractSummary);
    }

    private void ApplyExportColorOption(
        ExportColorOptionProjection option,
        RadioButton button,
        TextBlock label,
        TextBlock statusLabel)
    {
        label.Text = option.Label;
        statusLabel.Text = option.StatusLabel;
        button.IsChecked = option.IsSelected;
        button.IsEnabled = option.IsInteractive;
        label.Foreground = option.IsSelected
            ? (Brush)Application.Current.Resources["TextBrush"]
            : (Brush)Application.Current.Resources["MutedTextBrush"];
        statusLabel.Foreground = option.IsSelected
            ? (Brush)Application.Current.Resources["TextBrush"]
            : (Brush)Application.Current.Resources["MutedTextBrush"];
        AutomationProperties.SetName(button, $"Export option: {option.Label}");
        AutomationProperties.SetHelpText(button, option.AccessibilityHelpText);
        ToolTipService.SetToolTip(button, option.AccessibilityHelpText);
    }

    private void ApplyValidationProjection(ValidationPanelProjection validation)
    {
        ValidationReleaseTargetText.Text = validation.ReleaseTarget;
        ValidationSummaryText.Text = validation.Summary;
        ValidationGateLabel.Text = $"Current output gate: {validation.OutputProfileGate.ProfileLabel}";
        ValidationGateDetail.Text = validation.OutputProfileGate.Detail;
        ValidationGateStatus.Text = validation.OutputProfileGate.StatusLabel.ToUpperInvariant();
        ValidationGateStatus.Foreground = GetValidationStatusBrush(validation.OutputProfileGate.Status);
        AutomationProperties.SetHelpText(
            ValidationGateLabel,
            $"{validation.OutputProfileGate.ProfileLabel}. {validation.OutputProfileGate.StatusLabel}. {validation.OutputProfileGate.Detail}");
        ApplyValidationRow(validation.Rows.ElementAtOrDefault(0), ValidationRow1Label, ValidationRow1Detail, ValidationRow1Status);
        ApplyValidationRow(validation.Rows.ElementAtOrDefault(1), ValidationRow2Label, ValidationRow2Detail, ValidationRow2Status);
        ApplyValidationRow(validation.Rows.ElementAtOrDefault(2), ValidationRow3Label, ValidationRow3Detail, ValidationRow3Status);
        ApplyValidationRow(validation.Rows.ElementAtOrDefault(3), ValidationRow4Label, ValidationRow4Detail, ValidationRow4Status);
        ApplyValidationRow(validation.Rows.ElementAtOrDefault(4), ValidationRow5Label, ValidationRow5Detail, ValidationRow5Status);
        ApplyValidationRow(validation.Rows.ElementAtOrDefault(5), ValidationRow6Label, ValidationRow6Detail, ValidationRow6Status);
        ValidationViewerMatrixSummaryText.Text = validation.ViewerMatrixSummary;
        ApplyValidationViewerRow(
            validation.ViewerMatrix.ElementAtOrDefault(0),
            ValidationViewerRow1Label,
            ValidationViewerRow1Detail,
            ValidationViewerRow1Status);
        ApplyValidationViewerRow(
            validation.ViewerMatrix.ElementAtOrDefault(1),
            ValidationViewerRow2Label,
            ValidationViewerRow2Detail,
            ValidationViewerRow2Status);
        ApplyValidationViewerRow(
            validation.ViewerMatrix.ElementAtOrDefault(2),
            ValidationViewerRow3Label,
            ValidationViewerRow3Detail,
            ValidationViewerRow3Status);
        ValidationEvidenceSummaryHeadingText.Text = validation.EvidenceSummary.Heading;
        ValidationEvidenceSummaryText.Text = validation.EvidenceSummary.Summary;
        ValidationEvidenceSummaryStatus.Text = FormatValidationStatus(validation.EvidenceSummary.Status);
        ValidationEvidenceSummaryStatus.Foreground = GetValidationStatusBrush(validation.EvidenceSummary.Status);
        ValidationEvidenceCoverageText.Text = validation.EvidenceSummary.CoverageDetail;
        ValidationEvidenceGapText.Text = validation.EvidenceSummary.GapDetail;
        ValidationOpenLatestEvidenceButton.IsEnabled = validation.EvidenceSummary.CanOpenLatestArtifact;
        ToolTipService.SetToolTip(
            ValidationOpenLatestEvidenceButton,
            validation.EvidenceSummary.CanOpenLatestArtifact
                ? $"Open latest loaded validation evidence: {validation.EvidenceSummary.LatestArtifactPath}"
                : "No loaded validation evidence file is available for this session.");
        AutomationProperties.SetHelpText(
            ValidationOpenLatestEvidenceButton,
            validation.EvidenceSummary.CanOpenLatestArtifact
                ? $"Open the latest loaded validation evidence file at {validation.EvidenceSummary.LatestArtifactPath}."
                : "No loaded validation evidence file is available for this session.");
        AutomationProperties.SetHelpText(
            ValidationEvidenceSummaryHeadingText,
            $"{validation.EvidenceSummary.Summary} {validation.EvidenceSummary.CoverageDetail} {validation.EvidenceSummary.GapDetail}");
        ApplyValidationRecord(validation.Record);
    }

    private static void ApplyValidationRow(
        ValidationEvidenceRowProjection? row,
        TextBlock label,
        TextBlock detail,
        TextBlock status)
    {
        if (row is null)
        {
            label.Text = string.Empty;
            detail.Text = string.Empty;
            status.Text = string.Empty;
            return;
        }

        label.Text = row.Label;
        detail.Text = row.Detail;
        status.Text = FormatValidationStatus(row.Status);
        status.Foreground = GetValidationStatusBrush(row.Status);
        AutomationProperties.SetHelpText(label, row.Detail);
    }

    private static void ApplyValidationViewerRow(
        ValidationViewerMatrixRowProjection? row,
        TextBlock label,
        TextBlock detail,
        TextBlock status)
    {
        if (row is null)
        {
            label.Text = string.Empty;
            detail.Text = string.Empty;
            status.Text = string.Empty;
            return;
        }

        label.Text = row.Name;
        detail.Text = row.Detail;
        status.Text = FormatValidationStatus(row.Status);
        status.Foreground = GetValidationStatusBrush(row.Status);
        AutomationProperties.SetHelpText(label, row.Detail);
    }

    private void ApplyValidationRecord(ValidationRecordProjection record)
    {
        ValidationRecordBuildText.Text = record.BuildLabel;
        ValidationRecordAutomatedDetail.Text = record.AutomatedEvidenceDetail;
        ValidationRecordAutomatedStatus.Text = FormatValidationStatus(record.AutomatedEvidenceStatus);
        ValidationRecordAutomatedStatus.Foreground = GetValidationStatusBrush(record.AutomatedEvidenceStatus);
        ValidationRecordManualDetail.Text = record.WindowsManualValidationDetail;
        ValidationRecordManualStatus.Text = FormatValidationStatus(record.WindowsManualValidationStatus);
        ValidationRecordManualStatus.Foreground = GetValidationStatusBrush(record.WindowsManualValidationStatus);
        ValidationRecordEvidencePathText.Text = record.EvidenceDocumentPath;
        ValidationOpenWorkspaceButton.IsEnabled = record.CanOpenValidationWorkspace;
        ValidationOpenTemplateButton.IsEnabled = record.CanOpenValidationTemplate;
        ValidationOpenResourceTrendTemplateButton.IsEnabled = record.CanOpenResourceTrendTemplate;
        ValidationOpenResourceTrendScriptButton.IsEnabled = record.CanOpenResourceTrendScript;
        ValidationCopyResourceTrendCommandButton.IsEnabled = record.CanCopyResourceTrendCommand;
        ValidationCreateDraftButton.IsEnabled = record.CanOpenValidationWorkspace;
        ToolTipService.SetToolTip(
            ValidationOpenWorkspaceButton,
            record.CanOpenValidationWorkspace
                ? $"Open local validation workspace: {record.ValidationWorkspacePath}"
                : "Local validation workspace is not available for this session.");
        ToolTipService.SetToolTip(
            ValidationOpenTemplateButton,
            record.CanOpenValidationTemplate
                ? $"Open seeded validation template: {record.ValidationTemplatePath}"
                : "Seeded validation template is not available for this session.");
        ToolTipService.SetToolTip(
            ValidationCreateDraftButton,
            record.CanOpenValidationWorkspace
                ? $"Create a local validation draft in {record.ValidationWorkspacePath} using the current output target and selected profile."
                : "Local validation workspace is not available for this session.");
        ToolTipService.SetToolTip(
            ValidationOpenResourceTrendTemplateButton,
            record.CanOpenResourceTrendTemplate
                ? $"Open seeded resource trend session template: {record.ResourceTrendTemplatePath}"
                : "Seeded resource trend session template is not available for this session.");
        ToolTipService.SetToolTip(
            ValidationOpenResourceTrendScriptButton,
            record.CanOpenResourceTrendScript
                ? $"Open seeded resource trend sampler script: {record.ResourceTrendScriptPath}"
                : "Seeded resource trend sampler script is not available for this session.");
        ToolTipService.SetToolTip(
            ValidationCopyResourceTrendCommandButton,
            record.CanCopyResourceTrendCommand
                ? $"Copy the current-process resource trend sampler command using {record.ResourceTrendScriptPath}."
                : "Resource trend sampler command is unavailable until the workspace and script are ready.");
        AutomationProperties.SetHelpText(
            ValidationOpenWorkspaceButton,
            record.CanOpenValidationWorkspace
                ? $"Open the local validation workspace at {record.ValidationWorkspacePath}."
                : "Local validation workspace is not available for this session.");
        AutomationProperties.SetHelpText(
            ValidationOpenTemplateButton,
            record.CanOpenValidationTemplate
                ? $"Open the seeded validation template at {record.ValidationTemplatePath}."
                : "Seeded validation template is not available for this session.");
        AutomationProperties.SetHelpText(
            ValidationCreateDraftButton,
            record.CanOpenValidationWorkspace
                ? $"Create a local validation draft in {record.ValidationWorkspacePath} using the current output target and selected profile."
                : "Local validation workspace is not available for this session.");
        AutomationProperties.SetHelpText(
            ValidationOpenResourceTrendTemplateButton,
            record.CanOpenResourceTrendTemplate
                ? $"Open the seeded resource trend session template at {record.ResourceTrendTemplatePath}."
                : "Seeded resource trend session template is not available for this session.");
        AutomationProperties.SetHelpText(
            ValidationOpenResourceTrendScriptButton,
            record.CanOpenResourceTrendScript
                ? $"Open the seeded resource trend sampler script at {record.ResourceTrendScriptPath}."
                : "Seeded resource trend sampler script is not available for this session.");
        AutomationProperties.SetHelpText(
            ValidationCopyResourceTrendCommandButton,
            record.CanCopyResourceTrendCommand
                ? "Copy the current-process resource trend sampler command for the app-local validation workspace."
                : "Resource trend sampler command is unavailable until the workspace and script are ready.");
        AutomationProperties.SetHelpText(
            ValidationRecordBuildText,
            $"{record.BuildLabel}. {record.AutomatedEvidenceDetail} {record.WindowsManualValidationDetail} Evidence document: {record.EvidenceDocumentPath}. {record.WorkspaceSummary}");
    }

    private async Task OpenValidationPathAsync(
        string? path,
        ArtifactShellActionKind action,
        string description)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            Logger.LogWarning(
                "operation=ValidationWorkspace, stage=OpenSkipped, target={Target}, detail=Path is unavailable.",
                description);
            return;
        }

        var result = await artifactShellAction.ExecuteAsync(path, action);
        if (!result.IsSuccess)
        {
            Logger.LogWarning(
                "operation=ValidationWorkspace, stage=OpenFailed, target={Target}, path={Path}, detail={Detail}",
                description,
                path,
                result.TechnicalDetail);
        }
        else
        {
            Logger.LogInformation(
                "operation=ValidationWorkspace, stage=Opened, target={Target}, path={Path}",
                description,
                path);
        }
    }

    private void ReloadOutputValidationArtifacts()
    {
        outputValidationArtifacts = null;
        var snapshot = LoadOutputValidationArtifacts();
        var currentState = captureService?.CurrentSessionState ?? CaptureSessionState.Idle();
        UpdateMainPanelProjection(currentState, lastOutputResult);
        ApplySettingsProjection(currentState);
        UpdateTrayMenu(currentState);
        overlayWindow?.ApplyState(CreateOverlayState(currentState));
        Logger.LogInformation(
            "operation=OutputValidationArtifacts, stage=Reload, artifacts={ArtifactCount}, issues={IssueCount}",
            snapshot.Artifacts.Count,
            snapshot.LoadIssues.Count);
    }

    private static string FormatValidationStatus(ValidationEvidenceStatus status) =>
        status switch
        {
            ValidationEvidenceStatus.Pass => "PASS",
            ValidationEvidenceStatus.Limited => "LIMITED",
            ValidationEvidenceStatus.Fail => "FAIL",
            ValidationEvidenceStatus.NotApplicable => "N/A",
            _ => "NOT RUN",
        };

    private static Brush GetValidationStatusBrush(ValidationEvidenceStatus status) =>
        status switch
        {
            ValidationEvidenceStatus.Pass => (Brush)Application.Current.Resources["SuccessBrush"],
            ValidationEvidenceStatus.Limited => (Brush)Application.Current.Resources["WarningBrush"],
            ValidationEvidenceStatus.Fail => (Brush)Application.Current.Resources["ErrorBrush"],
            _ => (Brush)Application.Current.Resources["MutedTextBrush"],
        };

    private void UpdateMainPanelProjection(CaptureSessionState state, OutputResult? outputResult = null)
    {
        var validation = LoadOutputValidationArtifacts();
        var outputCapabilities = ResolveOutputCapabilities(validation);
        var projection = MainPanelProjection.Project(
            state,
            outputResult,
            settingsProvider.HdrAlertsEnabled,
            settingsProvider.ExportColorFormat,
            validation.Artifacts,
            executionCapabilities: outputCapabilities,
            outputTarget: settingsProvider.OutputTarget,
            outputContextTarget: lastOutputTarget);
        var isIdle = state.Status is CaptureSessionStatus.Idle;
        var statusBrush = GetTrustStatusBrush(projection.TrustSeverity);
        var fidelityBrush = GetTrustStatusBrush(projection.FidelityClaim.Severity);

        SelectCaptureTargetButton.IsEnabled = projection.CanStartCapture;
        RegionSelectButton.IsEnabled = projection.CanStartCapture;
        SelectCaptureTargetButton.Title = (!isIdle && activeCaptureMode is CaptureCommandMode.Fullscreen)
            ? projection.ActionTitle
            : "Full Screen";
        RegionSelectButton.Title = (!isIdle && activeCaptureMode is CaptureCommandMode.Region)
            ? projection.ActionTitle
            : "Region";
        ApplyMainCaptureCardStyles();

        TrustStatusGlyph.Glyph = GetTrustStatusGlyph(projection.TrustIcon);
        TrustStatusGlyph.Foreground = statusBrush;
        TrustStatusDot.Fill = statusBrush;
        TrustStatusLabel.Text = projection.TrustLabel;
        TrustStatusLabel.Foreground = statusBrush;
        ToolTipService.SetToolTip(TrustStatusLabel, projection.TrustMessage);
        AutomationProperties.SetHelpText(TrustStatusLabel, projection.TrustMessage);

        TargetHdrStatusGlyph.Glyph = GetTrustStatusGlyph(projection.TrustIcon);
        TargetHdrStatusGlyph.Foreground = statusBrush;
        TargetHdrStatusDotInline.Fill = statusBrush;
        TargetHdrStatusLabel.Text = projection.TrustLabel;
        TargetHdrStatusLabel.Foreground = statusBrush;
        TargetHdrStatusDetail.Text = projection.TrustMessage;
        AutomationProperties.SetHelpText(TargetHdrStatusPill, projection.TrustMessage);

        FidelityClaimStatusGlyph.Glyph = GetTrustStatusGlyph(projection.FidelityClaim.Icon);
        FidelityClaimStatusGlyph.Foreground = fidelityBrush;
        FidelityClaimStatusDot.Fill = fidelityBrush;
        FidelityClaimStatusLabel.Text = projection.FidelityClaim.Label;
        FidelityClaimStatusLabel.Foreground = fidelityBrush;
        FidelityClaimStatusDetail.Text = projection.FidelityClaim.Detail;
        AutomationProperties.SetHelpText(FidelityClaimStatusPill, projection.FidelityClaim.Detail);

        OutputProfileLabel.Text = $"Output profile: {projection.OutputProfile.Label}";
        OutputProfileDetail.Text = projection.OutputProfile.Detail;
        OutputProfileStatusLabel.Text = projection.OutputProfile.StatusLabel;
        ToolTipService.SetToolTip(OutputProfilePanel, projection.OutputProfile.Detail);
        AutomationProperties.SetHelpText(OutputProfilePanel, projection.OutputProfile.Detail);

        ApplyOutputResultProjection(projection.OutputResult);

        isHdrAlertVisible = ApplyHdrAlert(projection);
        if (activeShellView is AppShellView.Main)
        {
            SizeToActiveShellView();
        }
    }

    private void ApplyOutputResultProjection(OutputResultProjection outputResult)
    {
        OutputResultTitle.Text = outputResult.Title;
        OutputResultDetail.Text = outputResult.Detail;
        OutputResultFidelityDetail.Text = outputResult.FidelityDetail;
        var brush = outputResult.Severity switch
        {
            OutputResultProjectionSeverity.Success => (Brush)Application.Current.Resources["SuccessBrush"],
            OutputResultProjectionSeverity.Warning => (Brush)Application.Current.Resources["WarningBrush"],
            _ => (Brush)Application.Current.Resources["MutedTextBrush"],
        };
        OutputResultGlyph.Foreground = brush;
        AutomationProperties.SetName(OutputResultPanel, OutputResultTitle.Text);
        AutomationProperties.SetHelpText(
            OutputResultPanel,
            $"{outputResult.Detail} {outputResult.FidelityDetail}");
    }

    private bool ApplyHdrAlert(MainPanelProjection projection)
    {
        if (!projection.HasAlert)
        {
            hdrAlertDismissed = false;
            HdrAlertInfoBar.IsOpen = false;
            HdrAlertInfoBar.Visibility = Visibility.Collapsed;
            return false;
        }

        if (hdrAlertDismissed)
        {
            HdrAlertInfoBar.IsOpen = false;
            HdrAlertInfoBar.Visibility = Visibility.Collapsed;
            return false;
        }

        HdrAlertInfoBar.Severity = projection.TrustSeverity switch
        {
            MainPanelTrustSeverity.Error => InfoBarSeverity.Error,
            MainPanelTrustSeverity.Warning => InfoBarSeverity.Warning,
            _ => InfoBarSeverity.Informational,
        };
        HdrAlertInfoBar.Message = projection.AlertMessage;
        HdrAlertInfoBar.IsOpen = true;
        HdrAlertInfoBar.Visibility = Visibility.Visible;
        return true;
    }

    private void OnHdrAlertInfoBarClosed(InfoBar sender, InfoBarClosedEventArgs args)
    {
        hdrAlertDismissed = true;
        isHdrAlertVisible = false;
        if (activeShellView is AppShellView.Main)
        {
            SizeToActiveShellView();
        }
    }

    private void ApplyMainCaptureCardStyles()
    {
        ApplyMainCaptureCardStyle(
            SelectCaptureTargetButton,
            activeCaptureMode is CaptureCommandMode.Fullscreen);
        ApplyMainCaptureCardStyle(
            RegionSelectButton,
            activeCaptureMode is CaptureCommandMode.Region);
    }

    private void ApplyMainCaptureCardStyle(CaptureActionCard card, bool isActive)
    {
        card.Background = isActive
            ? (Brush)Application.Current.Resources["PrimaryActionBrush"]
            : (Brush)Application.Current.Resources["SecondaryPanelBrush"];
        card.BorderBrush = isActive
            ? (Brush)Application.Current.Resources["PrimaryActionBorderBrush"]
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
            MainPanelTrustSeverity.Info => "AccentBrush",
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
            MainPanelTrustIcon.ErrorBadge => StatusErrorBadgeGlyph,
            MainPanelTrustIcon.WarningCircle => StatusWarningGlyph,
            MainPanelTrustIcon.InfoCircle => StatusInfoGlyph,
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

        Logger.LogDebug("Overlay close requested (Escape/cancel), stopping preview and resetting to idle");
        StopPreviewAndResetToIdle();
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
            if (!isClosed)
            {
                Logger.LogDebug("Resetting to idle after overlay capture confirmed");
                StopPreviewAndResetToIdle();
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
            if (!isClosed)
            {
                Logger.LogDebug("Resetting to idle after fullscreen capture completed");
                StopPreviewAndResetToIdle();
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
        var outputTarget = activeCaptureTarget;
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
            var validation = LoadOutputValidationArtifacts();
            var outputCapabilities = ResolveOutputCapabilities(validation);

            var request = new OutputRequest
            {
                Texture = outputFrame,
                CropRegion = cropRegion,
                Policy = OutputPolicy.FromSettings(
                    settingsProvider.OutputTarget,
                    settingsProvider.CopyAsImage,
                    settingsProvider.SavePath,
                    settingsProvider.TimestampNaming,
                    settingsProvider.AfterCaptureBehavior.ToString(),
                    settingsProvider.ExportColorFormat,
                    validationArtifacts: validation.Artifacts,
                    executionCapabilities: outputCapabilities)
            };

            var result = await outputService.ExecuteOutputAsync(request);
            lastOutputResult = result;
            lastOutputTarget = outputTarget;

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

            Logger.LogDebug("Overlay reference cleared, verifying capture action state");

            var currentState = captureService?.CurrentSessionState ?? CaptureSessionState.Idle();
            if (!isClosed && currentState.HasNativeSession)
            {
                Logger.LogDebug("Stopping preview from OnOverlayClosed, current state: {Status}", currentState.Status);
                StopPreview();
            }

            // Verify capture actions are re-enabled after overlay completion.
            // This diagnostic confirms the authoritative session-state projection
            // is driving capture action state, not overlay completion ordering.
            if (!isClosed)
            {
                var validation = LoadOutputValidationArtifacts();
                var outputCapabilities = ResolveOutputCapabilities(validation);
                var projection = MainPanelProjection.Project(
                    captureService?.CurrentSessionState ?? CaptureSessionState.Idle(),
                    hdrAlertsEnabled: settingsProvider.HdrAlertsEnabled,
                    exportColorFormat: settingsProvider.ExportColorFormat,
                    outputTarget: settingsProvider.OutputTarget,
                    validationArtifacts: validation.Artifacts,
                    executionCapabilities: outputCapabilities);
                Logger.LogDebug(
                    "Post-overlay capture action state: canStartCapture={CanStart}, sessionStatus={Status}",
                    projection.CanStartCapture,
                    captureService?.CurrentSessionState?.Status ?? CaptureSessionStatus.Idle);
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

    private OverlayState CreateOverlayState(CaptureSessionState sessionState)
    {
        ArgumentNullException.ThrowIfNull(sessionState);

        var isFrozenPreview = overlayPreviewFreezeController?.IsFrozen == true
            && activeCaptureMode is CaptureCommandMode.Region;
        var message = CaptureTargetScopeProjection.PrefixDetail(
            sessionState.Target,
            sessionState.UserFacingReason);
        var detail = AppendOverlayFreezeDetail(
            sessionState.TechnicalDetail ?? string.Empty,
            isFrozenPreview);
        var hdrAlertsEnabled = settingsProvider.HdrAlertsEnabled;
        var validation = LoadOutputValidationArtifacts();
        var fidelityCue = CreateOverlayFidelityCue(
            settingsProvider.ExportColorFormat,
            sessionState.Readiness,
            validation.Artifacts,
            ResolveOutputCapabilities(validation));
        return sessionState.Status switch
        {
            CaptureSessionStatus.Capturing => OverlayState.HdrReady(
                AppendOverlayFrozenMessage(string.Empty, isFrozenPreview),
                detail,
                fidelityCue),
            CaptureSessionStatus.Degraded => OverlayState.DegradedPreview(
                AppendOverlayFrozenMessage(
                    hdrAlertsEnabled ? "Enable HDR in Windows for best capture quality" : string.Empty,
                    isFrozenPreview),
                detail,
                fidelityCue),
            CaptureSessionStatus.Unsupported => OverlayState.UnsupportedCapture(
                hdrAlertsEnabled ? "HDR capture is not supported on this display" : string.Empty,
                detail,
                fidelityCue),
            CaptureSessionStatus.Failed => OverlayState.PreviewFailed(message, detail, fidelityCue: fidelityCue),
            CaptureSessionStatus.Disposed => OverlayState.Disposed(message, detail, fidelityCue),
            _ => OverlayState.Initializing(message, detail, fidelityCue),
        };
    }

    private static string AppendOverlayFrozenMessage(string message, bool isFrozenPreview)
    {
        if (!isFrozenPreview)
        {
            return message;
        }

        const string frozenMessage = "Frame paused for precise selection.";
        return string.IsNullOrWhiteSpace(message)
            ? frozenMessage
            : $"{message} {frozenMessage}";
    }

    private static string AppendOverlayFreezeDetail(string detail, bool isFrozenPreview)
    {
        if (!isFrozenPreview)
        {
            return detail;
        }

        const string frozenDetail = "Overlay preview is frozen from the first presented frame to preserve capture timing while you crop.";
        return string.IsNullOrWhiteSpace(detail)
            ? frozenDetail
            : $"{detail} {frozenDetail}";
    }

    private OverlayFidelityCue CreateOverlayFidelityCue(
        string? exportColorFormat,
        PreviewReadinessStatus? readiness,
        IEnumerable<OutputValidationSessionArtifact>? validationArtifacts,
        OutputProfileExecutionCapabilities executionCapabilities)
    {
        var projection = OverlayFidelityProjection.Project(
            exportColorFormat,
            readiness,
            validationArtifacts,
            executionCapabilities,
            settingsProvider.OutputTarget);
        var overlayClaim = projection.Kind switch
        {
            OverlayFidelityClaimProjection.Converted => OverlayFidelityClaimKind.Converted,
            OverlayFidelityClaimProjection.VisualMatch => OverlayFidelityClaimKind.VisualMatch,
            OverlayFidelityClaimProjection.HdrPreserved => OverlayFidelityClaimKind.HdrPreserved,
            _ => OverlayFidelityClaimKind.Unvalidated,
        };

        return new OverlayFidelityCue(
            overlayClaim,
            projection.Label,
            projection.Detail);
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        if (isClosed)
        {
            return;
        }

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

            if (trayMenuWindow is not null)
            {
                trayMenuWindow.CommandSelected -= null;
                trayMenuWindow = null;
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
