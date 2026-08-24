using Lumiere.Windows.Graphics.Hdr;
using Lumiere.Windows.Interop;
using Lumiere.Windows.Interop.Diagnostics;
using Microsoft.Extensions.Logging;
using Windows.Graphics.Capture;

namespace Lumiere.Windows.Capture;

/// <summary>
/// Resolves the active monitor directly. Target-selection UI belongs to the shared shell.
/// </summary>
internal sealed class DirectMonitorCaptureTargetSelectionService
{
    private const int E_NOTIMPL = unchecked((int)0x80004001);
    private static readonly ILogger Logger = LumiereLoggerFactory.CreateLogger(LogCategories.Capture);
    private readonly Func<MonitorHandle> monitorResolver;
    private readonly Func<MonitorHandle, CaptureTarget> monitorTargetFactory;
    private readonly Func<bool> isCaptureSupported;

    public DirectMonitorCaptureTargetSelectionService(
        Func<MonitorHandle> monitorResolver,
        Func<MonitorHandle, CaptureTarget> monitorTargetFactory,
        Func<bool>? isCaptureSupported = null)
    {
        this.monitorResolver = monitorResolver ?? throw new ArgumentNullException(nameof(monitorResolver));
        this.monitorTargetFactory = monitorTargetFactory ?? throw new ArgumentNullException(nameof(monitorTargetFactory));
        this.isCaptureSupported = isCaptureSupported ?? GraphicsCaptureSession.IsSupported;
    }

    public Task<CaptureTargetSelectionResult> SelectTargetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!isCaptureSupported())
            {
                return Task.FromResult(CaptureTargetSelectionResult.Unsupported(
                    EngineReadinessStatus.Unsupported(
                        EngineReadinessStage.Capture,
                        "Unsupported capture",
                        "GraphicsCaptureSession.IsSupported returned false.")));
            }

            var monitor = monitorResolver();
            cancellationToken.ThrowIfCancellationRequested();
            var target = EnsureDisplayIdentity(
                monitor,
                monitorTargetFactory(monitor)
                    ?? throw new InvalidOperationException("Monitor target factory returned null."));

            Logger.LogInformation(
                "operation=SelectCaptureTarget, stage=Complete, display={DisplayName}, width={Width}, height={Height}",
                target.DisplayName,
                target.Size.Width,
                target.Size.Height);
            return Task.FromResult(CaptureTargetSelectionResult.Selected(
                target,
                EngineReadinessStatus.Initializing(
                    EngineReadinessStage.Capture,
                    "Direct monitor capture starting.",
                    $"Selected display: {target.DisplayName} ({target.Size.Width}x{target.Size.Height}).")));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (NativeInteropException exception)
        {
            return Task.FromResult(MapNativeInteropFailure(exception));
        }
        catch (NotSupportedException exception)
        {
            return Task.FromResult(CaptureTargetSelectionResult.Unsupported(
                EngineReadinessStatus.Unsupported(
                    EngineReadinessStage.Capture,
                    "Unsupported capture",
                    InteropFailureDiagnostics.LogAndFormat(exception, Logger))));
        }
        catch (Exception exception)
        {
            return Task.FromResult(CaptureTargetSelectionResult.Failed(
                EngineReadinessStatus.Failed(
                    EngineReadinessStage.Interop,
                    "Capture target selection failed",
                    InteropFailureDiagnostics.LogAndFormat(exception, Logger))));
        }
    }

    private static CaptureTargetSelectionResult MapNativeInteropFailure(NativeInteropException exception)
    {
        var stage = exception.Stage == "Interop"
            ? EngineReadinessStage.Interop
            : EngineReadinessStage.Capture;
        return exception.Stage == "Interop" || exception.HResultCode == E_NOTIMPL
            ? CaptureTargetSelectionResult.Unsupported(
                EngineReadinessStatus.Unsupported(
                    stage,
                    "Unsupported capture",
                    InteropFailureDiagnostics.LogAndFormat(exception, Logger)))
            : CaptureTargetSelectionResult.Failed(
                EngineReadinessStatus.Failed(
                    stage,
                    "Capture target selection failed",
                    InteropFailureDiagnostics.LogAndFormat(exception, Logger)));
    }

    private static CaptureTarget EnsureDisplayIdentity(MonitorHandle monitor, CaptureTarget target)
    {
        if (target.Kind is not CaptureTargetKind.Display || target.DisplayIdentity is not null)
        {
            return target;
        }

        var identity = monitor.Left is { } left
            && monitor.Top is { } top
            && monitor.Width is > 0
            && monitor.Height is > 0
                ? DisplayOutputIdentity.FromMonitorDisplayName(
                    monitor.DisplayName,
                    left,
                    top,
                    monitor.Width.Value,
                    monitor.Height.Value)
                : DisplayOutputIdentity.FromMonitorDisplayName(
                    monitor.DisplayName,
                    target.Size.Width,
                    target.Size.Height);
        return target.WithDisplayIdentity(identity);
    }
}
