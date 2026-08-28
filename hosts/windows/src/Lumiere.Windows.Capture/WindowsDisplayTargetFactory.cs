using Lumiere.Windows.Graphics.Hdr;
using Lumiere.Windows.Interop;
using Vortice.DXGI;

namespace Lumiere.Windows.Capture;

internal static class WindowsDisplayTargetFactory
{
    private const double LogicalDpi = 96;

    public static CaptureTarget Create(MonitorHandle monitor)
    {
        ArgumentNullException.ThrowIfNull(monitor);

        var item = GraphicsCaptureMonitorInterop.CreateForMonitor(monitor);
        var identity = monitor.Left is { } left
            && monitor.Top is { } top
            && monitor.Width is > 0
            && monitor.Height is > 0
                ? new DisplayOutputIdentity(
                    monitor.DisplayName,
                    left,
                    top,
                    monitor.Width.Value,
                    monitor.Height.Value)
                : null;
        return CaptureTarget.FromDisplayItem(item, monitor.DisplayName, identity);
    }

    public static HdrDisplayCapability ProbeHdrCapability(CaptureTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        using var factory = DXGI.CreateDXGIFactory2<IDXGIFactory2>(debug: false);
        var identity = target.DisplayIdentity;
        return identity is null
            ? HdrDisplayCapability.Probe(factory, target.DisplayName, target.Size.Width, target.Size.Height)
            : HdrDisplayCapability.Probe(
                factory,
                identity.DeviceName,
                identity.Left,
                identity.Top,
                identity.Width,
                identity.Height);
    }

    public static HdrDisplayCapability ProbeHdrCapability(MonitorHandle monitor)
    {
        ArgumentNullException.ThrowIfNull(monitor);

        using var factory = DXGI.CreateDXGIFactory2<IDXGIFactory2>(debug: false);
        return monitor.Left is { } left
            && monitor.Top is { } top
            && monitor.Width is > 0
            && monitor.Height is > 0
                ? HdrDisplayCapability.Probe(
                    factory,
                    monitor.DisplayName,
                    left,
                    top,
                    monitor.Width.Value,
                    monitor.Height.Value)
                : HdrDisplayCapability.Probe(
                    factory,
                    monitor.DisplayName,
                    monitor.Width ?? 0,
                    monitor.Height ?? 0);
    }

    internal static WindowsTargetLogicalSize? CalculateLogicalSize(
        int pixelWidth,
        int pixelHeight,
        MonitorHandle monitor)
    {
        ArgumentNullException.ThrowIfNull(monitor);

        return pixelWidth > 0
            && pixelHeight > 0
            && monitor.EffectiveDpiX is >= 96
            && monitor.EffectiveDpiY is >= 96
                ? new WindowsTargetLogicalSize(
                    pixelWidth * LogicalDpi / monitor.EffectiveDpiX.Value,
                    pixelHeight * LogicalDpi / monitor.EffectiveDpiY.Value)
                : null;
    }
}
