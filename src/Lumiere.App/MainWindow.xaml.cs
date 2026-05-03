using System.Threading;
using Lumiere.Capture;
using Lumiere.Graphics.Devices;
using Lumiere.Graphics.Hdr;
using Lumiere.Graphics.Presentation;
using Lumiere.Infrastructure.Interop;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
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
    private long frameEventCount;
    private int previewSourceWidth;
    private int previewSourceHeight;

    public MainWindow()
    {
        InitializeComponent();
        Title = "Lumiere";
        Closed += OnWindowClosed;
        RootGrid.SizeChanged += OnRootGridSizeChanged;

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
                    InteropFailureDiagnostics.Write(exception)));
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
        if (target.Size.Width <= 0 || target.Size.Height <= 0)
        {
            ApplyReadiness(
                PreviewReadinessStatus.Failed(
                    PreviewReadinessStage.Capture,
                    "Preview failed",
                    $"Capture target reported an invalid size: {target.Size.Width}x{target.Size.Height}."));
            return;
        }

        ApplyPreviewPanelFit(target.Size.Width, target.Size.Height);
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
            frameEventCount = 0;
            currentGeneration = previewGeneration;
        }

        ApplyReadiness(presentationEvidence);

        var captureStart = captureService.StartCapture(
            target,
            frame => OnCapturedFrameArrived(currentGeneration, frame),
            readiness => ApplyFrameReadiness(currentGeneration, readiness),
            detail => ApplyFrameDiagnostic(currentGeneration, detail));
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
        var frameNumber = Interlocked.Increment(ref frameEventCount);
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
                    ApplyReadiness(AppendFrameDetail(result.Readiness, $"Presented frame #{frameNumber}."));
                }
            }))
        {
            // The frame has already been presented and disposed; no UI status update is possible.
        }
    }

    private void ApplyFrameReadiness(long generation, PreviewReadinessStatus readiness)
    {
        if (!PreviewSwapChainPanel.DispatcherQueue.TryEnqueue(() =>
            {
                if (generation == previewGeneration)
                {
                    ApplyReadiness(readiness);
                }
            }))
        {
        }
    }

    private void ApplyFrameDiagnostic(long generation, string detail)
    {
        var frameNumber = Interlocked.Increment(ref frameEventCount);
        if (!PreviewSwapChainPanel.DispatcherQueue.TryEnqueue(() =>
            {
                if (generation == previewGeneration)
                {
                    ApplyReadiness(
                        PreviewReadinessStatus.Initializing(
                            PreviewReadinessStage.Capture,
                            "Waiting for preview frame presentation.",
                            $"{detail} Frame event #{frameNumber}."));
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

    private void ApplyPreviewPanelFit(int sourceWidth, int sourceHeight)
    {
        if (sourceWidth <= 0 || sourceHeight <= 0)
        {
            return;
        }

        previewSourceWidth = sourceWidth;
        previewSourceHeight = sourceHeight;

        PreviewSwapChainPanel.Width = sourceWidth;
        PreviewSwapChainPanel.Height = sourceHeight;

        var availableWidth = Math.Max(1, RootGrid.ActualWidth);
        var availableHeight = Math.Max(1, RootGrid.ActualHeight);
        var scale = Math.Min(availableWidth / sourceWidth, availableHeight / sourceHeight);

        PreviewSwapChainPanel.RenderTransform = new CompositeTransform
        {
            ScaleX = scale,
            ScaleY = scale,
            TranslateX = Math.Max(0, (availableWidth - (sourceWidth * scale)) / 2),
            TranslateY = Math.Max(0, (availableHeight - (sourceHeight * scale)) / 2),
        };
    }

    private void OnRootGridSizeChanged(object sender, SizeChangedEventArgs args)
    {
        if (previewSourceWidth > 0 && previewSourceHeight > 0)
        {
            ApplyPreviewPanelFit(previewSourceWidth, previewSourceHeight);
        }
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
