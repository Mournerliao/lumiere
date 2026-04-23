using Microsoft.UI.Xaml;

using Lumiere.Capture;
using Lumiere.Graphics.Devices;
using Lumiere.Graphics.Hdr;
using Lumiere.Graphics.Presentation;
using Lumiere.Infrastructure.Interop;
using Windows.Graphics.Capture;

namespace Lumiere.App;

public sealed partial class MainWindow : Window
{
    private readonly GraphicsDeviceProvider deviceProvider = new();
    private CaptureService? captureService;
    private CaptureSessionResources? captureSession;
    private GraphicsDeviceResources? deviceResources;
    private GraphicsEngine? graphicsEngine;
    private PreviewFramePresenter? previewFramePresenter;
    private SwapChainResources? swapChainResources;
    private long previewGeneration;

    public MainWindow()
    {
        InitializeComponent();
        Title = "Lumiere";
        Closed += OnWindowClosed;

        ApplyReadiness(
            PreviewReadinessStatus.Initializing(
                PreviewReadinessStage.Capture,
                "Choose a display or window to start the minimal HDR preview.",
                "Select Capture Target uses GraphicsCapturePicker and preserves the FP16/scRGB preview path."));
    }

    private async void OnSelectCaptureTargetClick(object sender, RoutedEventArgs e)
    {
        SelectCaptureTargetButton.IsEnabled = false;

        try
        {
            ApplyReadiness(
                PreviewReadinessStatus.Initializing(
                    PreviewReadinessStage.Capture,
                    "Choose a display or window to start the minimal HDR preview.",
                    "GraphicsCapturePicker is waiting for user selection."));

            var item = await GraphicsCapturePickerInterop.PickSingleItemAsync(this);
            if (item is null)
            {
                ApplyReadiness(
                    PreviewReadinessStatus.Initializing(
                        PreviewReadinessStage.Capture,
                        "Choose a display or window to start the minimal HDR preview.",
                        "GraphicsCapturePicker was canceled."));
                return;
            }

            StartPreview(item);
        }
        catch (Exception exception)
        {
            ApplyReadiness(
                PreviewReadinessStatus.Failed(
                    PreviewReadinessStage.Capture,
                    "Preview failed",
                    exception.Message));
        }
        finally
        {
            SelectCaptureTargetButton.IsEnabled = true;
        }
    }

    private void StartPreview(GraphicsCaptureItem item)
    {
        EnsureGraphicsServices();
        StopPreview();

        var target = captureService!.CreateTarget(item);
        swapChainResources = graphicsEngine!.CreatePreviewSwapChain(
            new SwapChainCreationOptions(target.Size.Width, target.Size.Height),
            new SwapChainPanelPreviewSurface(PreviewSwapChainPanel));
        previewFramePresenter = new PreviewFramePresenter(deviceResources!, swapChainResources);
        ApplyReadiness(swapChainResources.PresentationEvidence);

        previewGeneration++;
        var currentGeneration = previewGeneration;
        var captureStart = captureService.StartCapture(target, frame => OnCapturedFrameArrived(currentGeneration, frame));
        captureSession = captureStart.SessionResources;
        ApplyReadiness(captureStart.Readiness);
    }

    private void OnCapturedFrameArrived(long generation, CapturedFrameTexture frame)
    {
        if (!PreviewSwapChainPanel.DispatcherQueue.TryEnqueue(() =>
            {
                using (frame)
                {
                    if (generation != previewGeneration || previewFramePresenter is null)
                    {
                        return;
                    }

                    var result = previewFramePresenter.PresentFrame(frame);
                    ApplyReadiness(result.Readiness);
                }
            }))
        {
            frame.Dispose();
        }
    }

    private void EnsureGraphicsServices()
    {
        if (deviceResources is null)
        {
            deviceResources = deviceProvider.CreateDevice();
        }

        graphicsEngine ??= new GraphicsEngine(deviceResources);
        captureService ??= new CaptureService(deviceResources);
    }

    private void StopPreview()
    {
        previewGeneration++;
        captureSession?.Dispose();
        captureSession = null;
        previewFramePresenter = null;
        swapChainResources?.Dispose();
        swapChainResources = null;
    }

    private void ApplyReadiness(PreviewReadinessStatus readiness)
    {
        PreviewStatusLabelTextBlock.Text = readiness.State switch
        {
            PreviewReadinessState.Ready => "HDR-ready",
            PreviewReadinessState.Degraded => "Degraded preview",
            PreviewReadinessState.Unsupported => "Unsupported capture",
            PreviewReadinessState.Failed => "Preview failed",
            _ => "Initializing preview",
        };
        PreviewStatusMessageTextBlock.Text = readiness.UserMessage;
        PreviewTechnicalDetailTextBlock.Text = readiness.TechnicalDetail ?? string.Empty;
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        StopPreview();
        deviceResources?.Dispose();
        deviceResources = null;
    }
}
