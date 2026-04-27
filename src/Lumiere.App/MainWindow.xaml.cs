using Lumiere.Capture;
using Lumiere.Graphics.Devices;
using Lumiere.Graphics.Hdr;
using Lumiere.Graphics.Presentation;
using Lumiere.Infrastructure.Interop;
using Microsoft.UI.Xaml;
using Windows.Graphics.Capture;

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
        long currentGeneration;
        PreviewReadinessStatus presentationEvidence;
        lock (previewSync)
        {
            swapChainResources = graphicsEngine!.CreatePreviewSwapChain(
                new SwapChainCreationOptions(target.Size.Width, target.Size.Height),
                new SwapChainPanelPreviewSurface(PreviewSwapChainPanel));
            previewFramePresenter = new PreviewFramePresenter(deviceResources!, swapChainResources);
            presentationEvidence = swapChainResources.PresentationEvidence;

            previewGeneration++;
            currentGeneration = previewGeneration;
        }

        ApplyReadiness(presentationEvidence);

        var captureStart = captureService.StartCapture(target, frame => OnCapturedFrameArrived(currentGeneration, frame));
        CaptureSessionResources? captureSessionToDispose = null;
        SwapChainResources? swapChainToDispose = null;
        var shouldApplyCaptureReadiness = false;
        lock (previewSync)
        {
            if (currentGeneration != previewGeneration)
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
            ApplyReadiness(captureStart.Readiness);
        }
    }

    private void OnCapturedFrameArrived(long generation, CapturedFrameTexture frame)
    {
        PreviewRenderResult result;
        using (frame)
        {
            lock (previewSync)
            {
                if (generation != previewGeneration || previewFramePresenter is null)
                {
                    return;
                }

                result = previewFramePresenter.PresentFrame(frame);
            }
        }

        if (!PreviewSwapChainPanel.DispatcherQueue.TryEnqueue(() =>
            {
                if (generation == previewGeneration)
                {
                    ApplyReadiness(result.Readiness);
                }
            }))
        {
            // The frame has already been presented and disposed; no UI status update is possible.
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
        CaptureSessionResources? captureSessionToDispose;
        SwapChainResources? swapChainToDispose;
        lock (previewSync)
        {
            previewGeneration++;
            captureSessionToDispose = captureSession;
            captureSession = null;
            previewFramePresenter = null;
            swapChainToDispose = swapChainResources;
            swapChainResources = null;
        }

        captureSessionToDispose?.Dispose();
        swapChainToDispose?.Dispose();
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
