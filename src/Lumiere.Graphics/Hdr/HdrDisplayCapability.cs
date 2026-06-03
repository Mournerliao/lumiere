using Lumiere.Infrastructure.Diagnostics;
using Microsoft.Extensions.Logging;
using Vortice.DXGI;

namespace Lumiere.Graphics.Hdr;

public enum HdrDisplayState
{
    Unknown = 0,
    Active,
    Inactive,
}

public sealed record HdrDisplayCapability(
    HdrDisplayState State,
    ColorSpaceType? DisplayColorSpace,
    string? DeviceName)
{
    private static readonly ILogger Logger = LumiereLoggerFactory.CreateLogger(LogCategories.Graphics);

    public bool IsHdrActive => State == HdrDisplayState.Active;

    public static HdrDisplayCapability Probe(IDXGIDevice dxgiDevice)
    {
        ArgumentNullException.ThrowIfNull(dxgiDevice);

        IDXGIAdapter? adapter = null;

        try
        {
            adapter = dxgiDevice.GetAdapter();
            if (adapter is null)
            {
                Logger.LogWarning("HDR display probe: IDXGIDevice.Adapter returned null; falling back to Unknown.");
                return Unknown();
            }

            return ProbeAdapter(adapter);
        }
        catch (Exception exception)
        {
            var diagnostic = DiagnosticContext.PreviewWarning(
                stage: "HdrDisplayProbe",
                userFacingState: "HDR display detection failed",
                technicalDetail: $"Probe method=IDXGIDevice, Detail={exception.GetType().Name}: {exception.Message}",
                exception: exception);
            diagnostic.LogTo(Logger);

            return Unknown();
        }
        finally
        {
            adapter?.Dispose();
        }
    }

    /// <summary>
    /// Probes HDR display capability using a freshly-created DXGI factory.
    /// Unlike <see cref="Probe(IDXGIDevice)"/>, this enumerates a fresh adapter
    /// from the factory, ensuring the output description reflects the current
    /// display HDR state rather than cached state from an older adapter object.
    /// </summary>
    public static HdrDisplayCapability Probe(IDXGIFactory2 factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        IDXGIAdapter? adapter = null;

        try
        {
            var hr = factory.EnumAdapters(0, out adapter);
            if (hr.Failure || adapter is null)
            {
                Logger.LogWarning("HDR display probe (factory): EnumAdapters(0) failed (hr={HResult}); falling back to Unknown.", FormatHResult(hr.Code));
                return Unknown();
            }

            return ProbeAdapter(adapter);
        }
        catch (Exception exception)
        {
            var diagnostic = DiagnosticContext.PreviewWarning(
                stage: "HdrDisplayProbe",
                userFacingState: "HDR display detection failed",
                technicalDetail: $"Probe method=IDXGIFactory2, Detail={exception.GetType().Name}: {exception.Message}",
                exception: exception);
            diagnostic.LogTo(Logger);

            return Unknown();
        }
        finally
        {
            adapter?.Dispose();
        }
    }

    private static HdrDisplayCapability ProbeAdapter(IDXGIAdapter adapter)
    {
        IDXGIOutput? output = null;
        IDXGIOutput6? output6 = null;

        try
        {
            var hr = adapter.EnumOutputs(0, out output);
            if (hr.Failure || output is null)
            {
                Logger.LogWarning("HDR display probe: EnumOutputs(0) failed (hr={HResult}); falling back to Unknown.", FormatHResult(hr.Code));
                return Unknown();
            }

            output6 = output.QueryInterface<IDXGIOutput6>();
            if (output6 is null)
            {
                Logger.LogWarning("HDR display probe: QueryInterface<IDXGIOutput6> returned null; falling back to Unknown.");
                return Unknown();
            }

            var desc = output6.Description1;
            var colorSpace = desc.ColorSpace;
            var deviceName = desc.DeviceName;
            var isHdr = IsHdrColorSpace(colorSpace);

            Logger.LogDebug(
                "HDR display probe: deviceName={DeviceName}, colorSpace={ColorSpace}, isHdrActive={IsHdrActive}",
                deviceName, colorSpace, isHdr);

            return new HdrDisplayCapability(
                isHdr ? HdrDisplayState.Active : HdrDisplayState.Inactive,
                colorSpace,
                deviceName);
        }
        finally
        {
            output6?.Dispose();
            output?.Dispose();
        }
    }

    public static HdrDisplayCapability Unknown() =>
        new(HdrDisplayState.Unknown, null, null);

    private static bool IsHdrColorSpace(ColorSpaceType colorSpace) =>
        colorSpace is ColorSpaceType.RgbFullG2084NoneP2020
            or ColorSpaceType.YcbcrStudioG2084LeftP2020
            or ColorSpaceType.YcbcrStudioG2084TopLeftP2020
            or ColorSpaceType.RgbStudioG2084NoneP2020;

    private static string FormatHResult(int hResult) =>
        hResult == 0 ? string.Empty : $"0x{hResult:X8}";
}
