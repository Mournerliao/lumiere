using Lumiere.Graphics.Hdr;
using Lumiere.Infrastructure.Diagnostics;
using Lumiere.Infrastructure.Interop;
using Microsoft.Extensions.Logging;
using Windows.Graphics.Capture;

namespace Lumiere.Capture;

public sealed class DirectMonitorCaptureTargetSelectionService
{
    private static readonly ILogger Logger = LumiereLoggerFactory.CreateLogger(LogCategories.Capture);
    private const int E_NOTIMPL = unchecked((int)0x80004001);

    private readonly Func<MonitorHandle> monitorResolver;
    private readonly Func<MonitorHandle, CaptureTarget> monitorTargetFactory;
    private readonly Func<bool> isCaptureSupported;
    private readonly ICaptureTargetPicker? fallbackPicker;
    private readonly Func<Task<CaptureTarget?>>? fallbackTargetProvider;

    public DirectMonitorCaptureTargetSelectionService(
        Func<MonitorHandle> monitorResolver,
        Func<MonitorHandle, CaptureTarget> monitorTargetFactory,
        Func<bool>? isCaptureSupported = null,
        ICaptureTargetPicker? fallbackPicker = null)
        : this(
            monitorResolver,
            monitorTargetFactory,
            isCaptureSupported,
            fallbackPicker,
            null)
    {
    }

    public static DirectMonitorCaptureTargetSelectionService CreateDirectOnly(
        Func<MonitorHandle> monitorResolver,
        Func<MonitorHandle, CaptureTarget> monitorTargetFactory,
        Func<bool>? isCaptureSupported = null) =>
        new(
            monitorResolver,
            monitorTargetFactory,
            isCaptureSupported,
            fallbackPicker: null);

    internal DirectMonitorCaptureTargetSelectionService(
        Func<MonitorHandle> monitorResolver,
        Func<MonitorHandle, CaptureTarget> monitorTargetFactory,
        Func<bool>? isCaptureSupported,
        Func<Task<CaptureTarget?>> fallbackTargetProvider)
        : this(
            monitorResolver,
            monitorTargetFactory,
            isCaptureSupported,
            null,
            fallbackTargetProvider)
    {
    }

    private DirectMonitorCaptureTargetSelectionService(
        Func<MonitorHandle> monitorResolver,
        Func<MonitorHandle, CaptureTarget> monitorTargetFactory,
        Func<bool>? isCaptureSupported,
        ICaptureTargetPicker? fallbackPicker,
        Func<Task<CaptureTarget?>>? fallbackTargetProvider)
    {
        this.monitorResolver = monitorResolver ?? throw new ArgumentNullException(nameof(monitorResolver));
        this.monitorTargetFactory = monitorTargetFactory ?? throw new ArgumentNullException(nameof(monitorTargetFactory));
        this.isCaptureSupported = isCaptureSupported ?? GraphicsCaptureSession.IsSupported;
        this.fallbackPicker = fallbackPicker;
        this.fallbackTargetProvider = fallbackTargetProvider;
    }

    public bool HasFallbackPicker => fallbackPicker is not null || fallbackTargetProvider is not null;

    public Task<CaptureTargetSelectionResult> SelectDirectMonitorTargetAsync()
    {
        try
        {
            if (!isCaptureSupported())
            {
                Logger.LogWarning("Direct capture FAILED: GraphicsCaptureSession.IsSupported=false");
                return Task.FromResult(CaptureTargetSelectionResult.Unsupported(
                    PreviewReadinessStatus.Unsupported(
                        PreviewReadinessStage.Capture,
                        "Unsupported capture",
                        "GraphicsCaptureSession.IsSupported returned false.")));
            }

            var monitor = monitorResolver();
            var target = monitorTargetFactory(monitor)
                ?? throw new InvalidOperationException("Monitor target factory returned null.");

            Logger.LogInformation("Monitor resolved: displayName={DisplayName}, size={Width}x{Height}, kind={Kind}", target.DisplayName, target.Size.Width, target.Size.Height, target.Kind);

            return Task.FromResult(CaptureTargetSelectionResult.Selected(
                target,
                PreviewReadinessStatus.Initializing(
                    PreviewReadinessStage.Capture,
                    "Direct monitor capture starting.",
                    $"Selected display: {target.DisplayName} ({target.Size.Width}x{target.Size.Height}).")));
        }
        catch (NativeInteropException exception)
        {
            Logger.LogError(exception, "Direct capture NativeInterop FAILED");
            return Task.FromResult(MapNativeInteropFailure(exception));
        }
        catch (NotSupportedException exception)
        {
            Logger.LogWarning(exception, "Direct capture NOT SUPPORTED");
            return Task.FromResult(CaptureTargetSelectionResult.Unsupported(
                PreviewReadinessStatus.Unsupported(
                    PreviewReadinessStage.Capture,
                    "Unsupported capture",
                    InteropFailureDiagnostics.Write(exception))));
        }
        catch (ArgumentException exception)
        {
            Logger.LogError(exception, "Direct capture FAILED (ArgumentException)");
            return Task.FromResult(CaptureTargetSelectionResult.Failed(
                PreviewReadinessStatus.Failed(
                    PreviewReadinessStage.Capture,
                    "Preview failed",
                    InteropFailureDiagnostics.Write(exception))));
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Direct capture FAILED (Exception)");
            return Task.FromResult(CaptureTargetSelectionResult.Failed(
                PreviewReadinessStatus.Failed(
                    PreviewReadinessStage.Interop,
                    "Preview failed",
                    InteropFailureDiagnostics.Write(exception))));
        }
    }

    public async Task<CaptureTargetSelectionResult> SelectWithFallbackPickerAsync()
    {
        if (fallbackPicker is null && fallbackTargetProvider is null)
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

            var target = await PickFallbackTargetAsync();
            if (target is null)
            {
                return CaptureTargetSelectionResult.Canceled(
                    PreviewReadinessStatus.Initializing(
                        PreviewReadinessStage.Capture,
                        "Choose a display or window to start the minimal HDR preview.",
                        "GraphicsCapturePicker was canceled."));
            }

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

    private async Task<CaptureTarget?> PickFallbackTargetAsync()
    {
        if (fallbackTargetProvider is not null)
        {
            return await fallbackTargetProvider();
        }

        var item = await fallbackPicker!.PickSingleItemAsync();
        return item is null
            ? null
            : CaptureTarget.FromItem(item);
    }
}
