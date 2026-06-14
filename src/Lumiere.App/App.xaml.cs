using Lumiere.Capture;
using Lumiere.Graphics.Clipboard;
using Lumiere.Graphics.Devices;
using Lumiere.Graphics.Output;
using Lumiere.Infrastructure.Diagnostics;
using Lumiere.Infrastructure.Interop;
using Lumiere.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace Lumiere.App;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        GraphicsDeviceResources? deviceResources = null;
        CaptureService? captureService = null;
        ClipboardOutputService? clipboardOutputService = null;
        ConfiguredOutputService? configuredOutputService = null;
        AfterCaptureOutputService? outputService = null;
        try
        {
            // Manual service wiring — no DI container
            var deviceProvider = new GraphicsDeviceProvider();
            deviceResources = deviceProvider.CreateDevice();
            var borderOptions = CaptureBorderOptions.TryBorderless();
            if (borderOptions == null)
            {
                System.Diagnostics.Debug.WriteLine("Warning: Borderless capture not available, using default border options");
                borderOptions = CaptureBorderOptions.RequireSystemBorder();
            }
            captureService = new CaptureService(deviceResources, borderOptions);
            var captureCommandCoordinator = new CaptureCommandCoordinator(captureService);
            clipboardOutputService = new ClipboardOutputService(deviceResources);
            configuredOutputService = new ConfiguredOutputService(
                clipboardOutputService,
                new FolderOutputService(clipboardOutputService));
            outputService = new AfterCaptureOutputService(
                configuredOutputService,
                new WindowsArtifactShellAction());
            var settingsProvider = new DefaultSettingsProvider(
                LocalSettingsStore.CreateDefault(LumiereLoggerFactory.CreateLogger(LogCategories.Settings)));
            var aboutInfoProvider = new AssemblyAboutInfoProvider(typeof(App).Assembly);

            var mainWindow = new MainWindow(captureCommandCoordinator, outputService, settingsProvider, settingsProvider, aboutInfoProvider, captureService, deviceResources);
            _window = mainWindow;
            _window.Activate();

            try
            {
                var trayMenu = WindowsTrayMenu.CreateForWindow(mainWindow, mainWindow.CreateTrayMenuSnapshot());
                mainWindow.AttachTrayMenu(trayMenu);

                try
                {
                    var ownerHwnd = WindowNative.GetWindowHandle(mainWindow);
                    var trayMenuWindow = new TrayMenuWindow(ownerHwnd);
                    trayMenuWindow.CommandSelected += (_, command) => mainWindow.DispatchTrayCommand(command);
                    trayMenu.MenuShowRequested += (_, args) =>
                        trayMenuWindow.ShowAt(args.CursorX, args.CursorY, args.Snapshot);
                    mainWindow.AttachTrayMenuWindow(trayMenuWindow);
                }
                catch (Exception trayMenuWindowException)
                {
                    LumiereLoggerFactory.CreateLogger(LogCategories.App)
                        .LogWarning(trayMenuWindowException, "Tray menu window could not be initialized; using native tray only.");
                }
            }
            catch (Exception trayException)
            {
                LumiereLoggerFactory.CreateLogger(LogCategories.App)
                    .LogWarning(trayException, "Native tray menu could not be initialized.");
            }

            try
            {
                mainWindow.AttachGlobalHotkeys(new WindowsGlobalHotkeyRegistrar());
            }
            catch (Exception hotkeyException)
            {
                LumiereLoggerFactory.CreateLogger(LogCategories.App)
                    .LogWarning(hotkeyException, "Global hotkeys could not be initialized.");
            }
        }
        catch (Exception ex)
        {
            deviceResources?.Dispose();
            WriteStartupFailure(ex);
            System.Diagnostics.Debug.WriteLine($"Failed to initialize application: {ex.Message}");
            Current.Exit();
        }
    }

    private static void WriteStartupFailure(Exception exception)
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var logDirectory = Path.Combine(localAppData, "Lumiere", "logs");
            Directory.CreateDirectory(logDirectory);
            var detail = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Lumiere startup failed{Environment.NewLine}{exception}";
            File.WriteAllText(Path.Combine(Path.GetTempPath(), "lumiere-last-error.txt"), detail);
            File.AppendAllText(Path.Combine(logDirectory, "startup-failure.log"), detail + Environment.NewLine);
        }
        catch
        {
            // Last-chance diagnostics must never mask the original startup failure.
        }
    }
}

