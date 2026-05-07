using Lumiere.Graphics.Hdr;
using Lumiere.Infrastructure.Interop;
using Windows.Graphics.Capture;

namespace Lumiere.Capture;

public sealed class DirectMonitorCaptureTargetSelectionService
{
    private const int E_NOTIMPL = unchecked((int)0x80004001);

    private readonly Func<MonitorHandle> monitorResolver;
    private readonly Func<MonitorHandle, GraphicsCaptureItem> monitorItemFactory;
    private readonly Func<bool> isCaptureSupported;
    private readonly ICaptureTargetPicker? fallbackPicker;

    public DirectMonitorCaptureTargetSelectionService(
        Func<MonitorHandle> monitorResolver,
        Func<MonitorHandle, GraphicsCaptureItem> monitorItemFactory,
        Func<bool>? isCaptureSupported = null,
        ICaptureTargetPicker? fallbackPicker = null)
    {
        this.monitorResolver = monitorResolver ?? throw new ArgumentNullException(nameof(monitorResolver));
        this.monitorItemFactory = monitorItemFactory ?? throw new ArgumentNullException(nameof(monitorItemFactory));
        this.isCaptureSupported = isCaptureSupported ?? GraphicsCaptureSession.IsSupported;
        this.fallbackPicker = fallbackPicker;
    }

    public bool HasFallbackPicker => fallbackPicker is not null;

    public Task<CaptureTargetSelectionResult> SelectDirectMonitorTargetAsync()
    {
        try
        {
            if (!isCaptureSupported())
            {
                return Task.FromResult(CaptureTargetSelectionResult.Unsupported(
                    PreviewReadinessStatus.Unsupported(
                        PreviewReadinessStage.Capture,
                        "Unsupported capture",
                        "GraphicsCaptureSession.IsSupported returned false.")));
            }

            var monitor = monitorResolver();
            var item = monitorItemFactory(monitor);
            var target = CaptureTarget.FromDisplayItem(item, monitor.DisplayName);

            return Task.FromResult(CaptureTargetSelectionResult.Selected(
                target,
                PreviewReadinessStatus.Initializing(
                    PreviewReadinessStage.Capture,
                    "Direct monitor capture starting.",
                    $"Selected display: {target.DisplayName} ({target.Size.Width}x{target.Size.Height}).")));
        }
        catch (NativeInteropException exception)
        {
            return Task.FromResult(MapNativeInteropFailure(exception));
        }
        catch (NotSupportedException exception)
        {
            return Task.FromResult(CaptureTargetSelectionResult.Unsupported(
                PreviewReadinessStatus.Unsupported(
                    PreviewReadinessStage.Capture,
                    "Unsupported capture",
                    InteropFailureDiagnostics.Write(exception))));
        }
        catch (ArgumentException exception)
        {
            return Task.FromResult(CaptureTargetSelectionResult.Failed(
                PreviewReadinessStatus.Failed(
                    PreviewReadinessStage.Capture,
                    "Preview failed",
                    InteropFailureDiagnostics.Write(exception))));
        }
        catch (Exception exception)
        {
            return Task.FromResult(CaptureTargetSelectionResult.Failed(
                PreviewReadinessStatus.Failed(
                    PreviewReadinessStage.Interop,
                    "Preview failed",
                    InteropFailureDiagnostics.Write(exception))));
        }
    }

    public async Task<CaptureTargetSelectionResult> SelectWithFallbackPickerAsync()
    {
        if (fallbackPicker is null)
        {
            return CaptureTargetSelectionResult.Failed(
                PreviewReadinessStatus.Failed(
                    PreviewReadinessStage.Capture,
                    "No fallback picker available",
                    "Direct monitor capture failed and no fallback picker was configured."));
        }

        try
        {
            if (!isCaptureSupported())
            {
                return CaptureTargetSelectionResult.Unsupported(
                    PreviewReadinessStatus.Unsupported(
                        PreviewReadinessStage.Capture,
                        "Unsupported capture",
                        "GraphicsCaptureSession.IsSupported returned false."));
            }

            var item = await fallbackPicker.PickSingleItemAsync();
            if (item is null)
            {
                return CaptureTargetSelectionResult.Canceled(
                    PreviewReadinessStatus.Initializing(
                        PreviewReadinessStage.Capture,
                        "Choose a display or window to start the minimal HDR preview.",
                        "GraphicsCapturePicker was canceled."));
            }

            var target = CaptureTarget.FromItem(item);
            return CaptureTargetSelectionResult.Selected(
                target,
                PreviewReadinessStatus.Initializing(
                    PreviewReadinessStage.Capture,
                    "Target selected. Starting preview.",
                    $"Selected: {target.DisplayName} ({target.Size.Width}x{target.Size.Height})."));
        }
        catch (NativeInteropException exception)
        {
            return MapNativeInteropFailure(exception);
        }
        catch (NotSupportedException exception)
        {
            return CaptureTargetSelectionResult.Unsupported(
                PreviewReadinessStatus.Unsupported(
                    PreviewReadinessStage.Capture,
                    "Unsupported capture",
                    InteropFailureDiagnostics.Write(exception)));
        }
        catch (ArgumentException exception)
        {
            return CaptureTargetSelectionResult.Failed(
                PreviewReadinessStatus.Failed(
                    PreviewReadinessStage.Capture,
                    "Preview failed",
                    InteropFailureDiagnostics.Write(exception)));
        }
        catch (Exception exception)
        {
            return CaptureTargetSelectionResult.Failed(
                PreviewReadinessStatus.Failed(
                    PreviewReadinessStage.Interop,
                    "Preview failed",
                    InteropFailureDiagnostics.Write(exception)));
        }
    }

    private static CaptureTargetSelectionResult MapNativeInteropFailure(NativeInteropException exception)
    {
        var isInteropStage = exception.Stage == "Interop";
        var stage = isInteropStage
            ? PreviewReadinessStage.Interop
            : PreviewReadinessStage.Capture;

        if (isInteropStage || exception.HResultCode == E_NOTIMPL)
        {
            return CaptureTargetSelectionResult.Unsupported(
                PreviewReadinessStatus.Unsupported(
                    stage,
                    "Unsupported capture",
                    InteropFailureDiagnostics.Write(exception)));
        }

        return CaptureTargetSelectionResult.Failed(
            PreviewReadinessStatus.Failed(
                stage,
                "Preview failed",
                InteropFailureDiagnostics.Write(exception)));
    }
}
