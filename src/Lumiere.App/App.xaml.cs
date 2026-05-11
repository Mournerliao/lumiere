using Lumiere.Capture;
using Lumiere.Graphics.Clipboard;
using Lumiere.Graphics.Devices;
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
        try
        {
            // Manual service wiring — no DI container
            var deviceProvider = new GraphicsDeviceProvider();
            var deviceResources = deviceProvider.CreateDevice();
            var borderOptions = CaptureBorderOptions.TryBorderless();
            if (borderOptions == null)
            {
                System.Diagnostics.Debug.WriteLine("Warning: Borderless capture not available, using default border options");
                borderOptions = CaptureBorderOptions.Default; // Assuming there's a default
            }
            var captureService = new CaptureService(deviceResources, borderOptions);
            var captureCommandCoordinator = new CaptureCommandCoordinator(captureService);
            var outputService = new ClipboardOutputService(deviceResources);
            var settingsProvider = new DefaultSettingsProvider();

            _window = new MainWindow(captureCommandCoordinator, outputService, settingsProvider, captureService);
            _window.Activate();
        }
        catch (Exception ex)
        {
            // Log the error and exit gracefully
            System.Diagnostics.Debug.WriteLine($"Failed to initialize application: {ex.Message}");
            Current.Exit();
        }
    }
}

