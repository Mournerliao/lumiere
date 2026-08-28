using Lumiere.Windows.Graphics.Hdr;
using Lumiere.Windows.Interop;
using Lumiere.Windows.Interop.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Lumiere.Windows.Capture;

public enum WindowsTargetHdrState
{
    Unknown = 0,
    Active,
    Inactive,
}

public sealed record WindowsTargetLogicalSize(double Width, double Height);

public sealed record WindowsTargetCapability(
    WindowsTargetHdrState HdrState,
    WindowsTargetLogicalSize? LogicalSize);

public sealed class WindowsTargetCapabilityProvider
{
    private static readonly ILogger Logger = LumiereLoggerFactory.CreateLogger(LogCategories.Capture);
    private readonly Func<MonitorHandle> monitorResolver;
    private readonly Func<MonitorHandle, HdrDisplayCapability> probeHdrCapability;

    public WindowsTargetCapabilityProvider()
        : this(
            MonitorSelectionInterop.GetCurrentMonitorFromCursor,
            WindowsDisplayTargetFactory.ProbeHdrCapability)
    {
    }

    internal WindowsTargetCapabilityProvider(
        Func<MonitorHandle> monitorResolver,
        Func<MonitorHandle, HdrDisplayCapability> probeHdrCapability)
    {
        this.monitorResolver = monitorResolver ?? throw new ArgumentNullException(nameof(monitorResolver));
        this.probeHdrCapability = probeHdrCapability ?? throw new ArgumentNullException(nameof(probeHdrCapability));
    }

    public WindowsTargetCapability? GetCurrent()
    {
        try
        {
            var monitor = monitorResolver();
            var hdrCapability = probeHdrCapability(monitor);
            var logicalSize = monitor.Width is > 0 && monitor.Height is > 0
                ? WindowsDisplayTargetFactory.CalculateLogicalSize(
                    monitor.Width.Value,
                    monitor.Height.Value,
                    monitor)
                : null;
            return new WindowsTargetCapability(
                MapHdrState(hdrCapability.State),
                logicalSize is { } size
                    ? new WindowsTargetLogicalSize(size.Width, size.Height)
                    : null);
        }
        catch (Exception exception)
        {
            _ = InteropFailureDiagnostics.LogAndFormat(exception, Logger);
            return null;
        }
    }

    private static WindowsTargetHdrState MapHdrState(HdrDisplayState state) =>
        state switch
        {
            HdrDisplayState.Active => WindowsTargetHdrState.Active,
            HdrDisplayState.Inactive => WindowsTargetHdrState.Inactive,
            _ => WindowsTargetHdrState.Unknown,
        };
}
