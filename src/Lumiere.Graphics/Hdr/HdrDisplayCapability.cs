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

    /// <summary>
    /// Probes HDR display capability using a DXGI factory.
    /// Compatibility path: when no target hint is available, this reflects
    /// the first enumerated output only.
    /// </summary>
    public static HdrDisplayCapability Probe(IDXGIFactory2 factory)
    {
        var outputs = ProbeOutputs(factory);
        return outputs.Count == 0
            ? Unknown()
            : FromOutputSnapshot(outputs[0]);
    }

    public static HdrDisplayCapability Probe(
        IDXGIFactory2 factory,
        string? targetDisplayName,
        int targetWidth,
        int targetHeight)
    {
        var outputs = ProbeOutputs(factory);
        return SelectForTarget(outputs, targetDisplayName, targetWidth, targetHeight);
    }

    private static IReadOnlyList<HdrDisplayOutputSnapshot> ProbeOutputs(IDXGIFactory2 factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        IDXGIAdapter? adapter = null;

        try
        {
            var hr = factory.EnumAdapters(0, out adapter);
            if (hr.Failure || adapter is null)
            {
                Logger.LogWarning("HDR display probe (factory): EnumAdapters(0) failed (hr={HResult}); returning no output evidence.", FormatHResult(hr.Code));
                return [];
            }

            return ProbeAdapterOutputs(adapter);
        }
        catch (Exception exception)
        {
            var diagnostic = DiagnosticContext.PreviewWarning(
                stage: "HdrDisplayProbe",
                userFacingState: "HDR display detection failed",
                technicalDetail: $"Probe method=IDXGIFactory2, Detail={exception.GetType().Name}: {exception.Message}",
                exception: exception);
            diagnostic.LogTo(Logger);

            return [];
        }
        finally
        {
            adapter?.Dispose();
        }
    }

    private static IReadOnlyList<HdrDisplayOutputSnapshot> ProbeAdapterOutputs(IDXGIAdapter adapter)
    {
        var outputs = new List<HdrDisplayOutputSnapshot>();

        for (var outputIndex = 0; ; outputIndex++)
        {
            IDXGIOutput? output = null;
            IDXGIOutput6? output6 = null;

            var hr = adapter.EnumOutputs((uint)outputIndex, out output);
            if (hr.Failure || output is null)
            {
                if (outputIndex == 0)
                {
                    Logger.LogWarning("HDR display probe: EnumOutputs(0) failed (hr={HResult}); returning no output evidence.", FormatHResult(hr.Code));
                }

                break;
            }

            try
            {
                output6 = output.QueryInterface<IDXGIOutput6>();
                if (output6 is null)
                {
                    Logger.LogWarning("HDR display probe: QueryInterface<IDXGIOutput6> returned null for outputIndex={OutputIndex}.", outputIndex);
                    continue;
                }

                var desc = output6.Description1;
                var colorSpace = desc.ColorSpace;
                var deviceName = desc.DeviceName;
                var snapshot = new HdrDisplayOutputSnapshot(
                    deviceName,
                    desc.DesktopCoordinates.Right - desc.DesktopCoordinates.Left,
                    desc.DesktopCoordinates.Bottom - desc.DesktopCoordinates.Top,
                    colorSpace);
                var capability = FromOutputSnapshot(snapshot);

                Logger.LogDebug(
                    "HDR display probe: outputIndex={OutputIndex}, deviceName={DeviceName}, colorSpace={ColorSpace}, isHdrActive={IsHdrActive}",
                    outputIndex, deviceName, colorSpace, capability.IsHdrActive);

                outputs.Add(snapshot);
            }
            finally
            {
                output6?.Dispose();
                output.Dispose();
            }
        }

        return outputs;
    }

    public static HdrDisplayCapability Unknown() =>
        new(HdrDisplayState.Unknown, null, null);

    public static HdrDisplayCapability SelectForTarget(
        IReadOnlyList<HdrDisplayOutputSnapshot> outputs,
        string? targetDisplayName,
        int targetWidth,
        int targetHeight)
    {
        ArgumentNullException.ThrowIfNull(outputs);

        var matchingOutput = FindByDisplayName(outputs, targetDisplayName)
            ?? FindBySize(outputs, targetWidth, targetHeight);

        return matchingOutput is null
            ? Unknown()
            : FromOutputSnapshot(matchingOutput);
    }

    private static HdrDisplayOutputSnapshot? FindByDisplayName(
        IReadOnlyList<HdrDisplayOutputSnapshot> outputs,
        string? targetDisplayName)
    {
        if (string.IsNullOrWhiteSpace(targetDisplayName))
        {
            return null;
        }

        return outputs.FirstOrDefault(output =>
            string.Equals(output.DeviceName, targetDisplayName.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static HdrDisplayOutputSnapshot? FindBySize(
        IReadOnlyList<HdrDisplayOutputSnapshot> outputs,
        int targetWidth,
        int targetHeight)
    {
        if (targetWidth <= 0 || targetHeight <= 0)
        {
            return null;
        }

        var matchingOutputs = outputs
            .Where(output => output.Width == targetWidth && output.Height == targetHeight)
            .Take(2)
            .ToArray();

        return matchingOutputs.Length == 1
            ? matchingOutputs[0]
            : null;
    }

    private static HdrDisplayCapability FromOutputSnapshot(HdrDisplayOutputSnapshot output)
    {
        var isHdr = IsHdrColorSpace(output.ColorSpace);
        return new HdrDisplayCapability(
            isHdr ? HdrDisplayState.Active : HdrDisplayState.Inactive,
            output.ColorSpace,
            output.DeviceName);
    }

    private static bool IsHdrColorSpace(ColorSpaceType colorSpace) =>
        colorSpace is ColorSpaceType.RgbFullG2084NoneP2020
            or ColorSpaceType.YcbcrStudioG2084LeftP2020
            or ColorSpaceType.YcbcrStudioG2084TopLeftP2020
            or ColorSpaceType.RgbStudioG2084NoneP2020;

    private static string FormatHResult(int hResult) =>
        hResult == 0 ? string.Empty : $"0x{hResult:X8}";
}

public sealed record HdrDisplayOutputSnapshot(
    string DeviceName,
    int Width,
    int Height,
    ColorSpaceType ColorSpace);
