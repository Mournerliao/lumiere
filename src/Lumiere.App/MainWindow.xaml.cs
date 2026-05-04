using System.Threading;
using Lumiere.Capture;
using Lumiere.Graphics.Devices;
using Lumiere.Graphics.Hdr;
using Lumiere.Graphics.Presentation;
using Lumiere.Infrastructure.Interop;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

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
    private long previewGeneration;
    private long frameEventCount;
    private int previewSourceWidth;
    private int previewSourceHeight;
    private bool isClosed;

    public MainWindow()
    {
        InitializeComponent();
        Title = "Lumiere";
        Closed += OnWindowClosed;
        RootGrid.SizeChanged += OnRootGridSizeChanged;

        ApplySessionState(
            CaptureSessionState.Idle(PreviewReadinessStatus.Initializing(
                PreviewReadinessStage.Capture,
                "Choose a display or window to start the minimal HDR preview.",
                "Select Capture Target uses GraphicsCapturePicker and preserves the FP16/scRGB preview path.")));
    }

    private async void OnSelectCaptureTargetClick(object sender, RoutedEventArgs e)
    {
        SelectCaptureTargetButton.IsEnabled = false;

        try
        {
            ApplySessionState(
                CaptureSessionState.SelectingTarget(PreviewReadinessStatus.Initializing(
                    PreviewReadinessStage.Capture,
                    "Choose a display or window to start the minimal HDR preview.",
                    "GraphicsCapturePicker is waiting for user selection.")));

            var selectionService = new CaptureTargetSelectionService(
                new GraphicsCaptureTargetPicker(this));
            var result = await selectionService.SelectTargetAsync();

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
                ApplySessionState(
                    CaptureSessionState.Failed(null, PreviewReadinessStatus.Failed(
                        PreviewReadinessStage.Capture,
                        "Preview failed",
                        InteropFailureDiagnostics.Write(exception))));
            }
        }
        finally
        {
            if (!isClosed)
            {
                SelectCaptureTargetButton.IsEnabled = true;
            }
        }
    }

    private void StartPreview(CaptureTarget target)
    {
        if (isClosed)
        {
            return;
        }

        EnsureGraphicsServices();
        StopPreview();

        ApplyPreviewPanelFit(target.Size.Width, target.Size.Height);
        long currentGeneration;
        PreviewReadinessStatus presentationEvidence;
        lock (previewSync)
        {
            swapChainResources = graphicsEngine!.CreatePreviewSwapChain(
                new SwapChainCreationOptions(target.Size.Width, target.Size.Height),
                new SwapChainPanelPreviewSurface(PreviewSwapChainPanel));
            previewFramePresenter = new PreviewFramePresenter(deviceResources!, swapChainResources);
            activeCaptureTarget = target;
            presentationEvidence = swapChainResources.PresentationEvidence;
            activePresentationEvidence = presentationEvidence;

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
        using (frame)
        {
            lock (previewSync)
            {
                if (generation != Volatile.Read(ref previewGeneration) || previewFramePresenter is null)
                {
                    return;
                }

                result = previewFramePresenter.PresentFrame(frame);
                target = activeCaptureTarget;
            }
        }

        if (!PreviewSwapChainPanel.DispatcherQueue.TryEnqueue(() =>
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

    private void ApplyFrameReadiness(long generation, PreviewReadinessStatus readiness)
    {
        if (!PreviewSwapChainPanel.DispatcherQueue.TryEnqueue(() =>
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
        if (!PreviewSwapChainPanel.DispatcherQueue.TryEnqueue(() =>
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
    }

    private void StopPreview()
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

    private void ApplySessionState(CaptureSessionState state)
    {
        sessionState = state ?? throw new ArgumentNullException(nameof(state));
        PreviewStatusLabelTextBlock.Text = sessionState.Status switch
        {
            CaptureSessionStatus.Idle => "Ready to capture",
            CaptureSessionStatus.SelectingTarget => "Selecting target",
            CaptureSessionStatus.Initializing => "Initializing preview",
            CaptureSessionStatus.Capturing => "HDR-ready",
            CaptureSessionStatus.Degraded => "Degraded preview",
            CaptureSessionStatus.Unsupported => "Unsupported capture",
            CaptureSessionStatus.Failed => "Preview failed",
            CaptureSessionStatus.Disposed => "Preview stopped",
            _ => "Initializing preview",
        };
        PreviewStatusMessageTextBlock.Text = sessionState.UserFacingReason ?? string.Empty;
        PreviewTechnicalDetailTextBlock.Text = sessionState.TechnicalDetail ?? string.Empty;
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        isClosed = true;
        StopPreview();
        captureService = null;
        graphicsEngine = null;
        deviceResources?.Dispose();
        deviceResources = null;
    }

}
