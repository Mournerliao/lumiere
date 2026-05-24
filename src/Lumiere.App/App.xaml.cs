using Lumiere.Capture;
using Lumiere.Graphics.Clipboard;
using Lumiere.Graphics.Devices;
using Lumiere.Graphics.Output;
using Lumiere.Infrastructure.Diagnostics;
using Lumiere.Infrastructure.Interop;
using Lumiere.Settings;
using Microsoft.UI.Xaml;

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
            var captureService = new CaptureService(deviceResources, borderOptions);
            var captureCommandCoordinator = new CaptureCommandCoordinator(captureService);
            var clipboardOutputService = new ClipboardOutputService(deviceResources);
            var configuredOutputService = new ConfiguredOutputService(
                clipboardOutputService,
                new FolderOutputService(clipboardOutputService));
            var outputService = new AfterCaptureOutputService(
                configuredOutputService,
                new WindowsArtifactShellAction());
            var settingsProvider = new DefaultSettingsProvider(
                LocalSettingsStore.CreateDefault(LumiereLoggerFactory.CreateLogger(LogCategories.Settings)));
            var aboutInfoProvider = new AssemblyAboutInfoProvider(typeof(App).Assembly);

            _window = new MainWindow(captureCommandCoordinator, outputService, settingsProvider, settingsProvider, settingsProvider, aboutInfoProvider, captureService, deviceResources);
            _window.Activate();
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

