using Lumiere.Windows.Interop.Diagnostics;
using Microsoft.Extensions.Logging;
using Vortice.DXGI;

namespace Lumiere.Windows.Graphics.Hdr;

public enum HdrDisplayState
{
    Unknown = 0,
    Active,
    Inactive,
}

public enum HdrDisplayMatchKind
{
    Unspecified = 0,
    DeviceName,
    DesktopBounds,
    Size,
    FirstOutput,
    NotMatched,
}

public sealed record HdrDisplayCapability(
    HdrDisplayState State,
    ColorSpaceType? DisplayColorSpace,
    string? DeviceName,
    HdrDisplayMatchKind MatchKind)
{
    private static readonly ILogger Logger = LumiereLoggerFactory.CreateLogger(LogCategories.Graphics);

    public HdrDisplayCapability(
        HdrDisplayState state,
        ColorSpaceType? displayColorSpace,
        string? deviceName)
        : this(state, displayColorSpace, deviceName, HdrDisplayMatchKind.Unspecified)
    {
    }

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
            : FromOutputSnapshot(outputs[0], HdrDisplayMatchKind.FirstOutput);
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

    public static HdrDisplayCapability Probe(
        IDXGIFactory2 factory,
        string? targetDisplayName,
        int? targetLeft,
        int? targetTop,
        int targetWidth,
        int targetHeight)
    {
        var outputs = ProbeOutputs(factory);
        return SelectForTarget(outputs, targetDisplayName, targetLeft, targetTop, targetWidth, targetHeight);
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
                Logger.LogWarning("HDR display probe (factory): EnumAdapters(0) failed (hr={HResult}); returning an unknown result.", FormatHResult(hr.Code));
                return [];
            }

            return ProbeAdapterOutputs(adapter);
        }
        catch (Exception exception)
        {
            var diagnostic = DiagnosticContext.EngineWarning(
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
                    Logger.LogWarning("HDR display probe: EnumOutputs(0) failed (hr={HResult}); returning an unknown result.", FormatHResult(hr.Code));
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
                    desc.DesktopCoordinates.Left,
                    desc.DesktopCoordinates.Top,
                    desc.DesktopCoordinates.Right - desc.DesktopCoordinates.Left,
                    desc.DesktopCoordinates.Bottom - desc.DesktopCoordinates.Top,
                    colorSpace);
                var capability = FromOutputSnapshot(snapshot, HdrDisplayMatchKind.Unspecified);

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
        new(HdrDisplayState.Unknown, null, null, HdrDisplayMatchKind.NotMatched);

    public static HdrDisplayCapability SelectForTarget(
        IReadOnlyList<HdrDisplayOutputSnapshot> outputs,
        string? targetDisplayName,
        int targetWidth,
        int targetHeight) =>
        SelectForTarget(
            outputs,
            targetDisplayName,
            targetLeft: null,
            targetTop: null,
            targetWidth,
            targetHeight);

    public static HdrDisplayCapability SelectForTarget(
        IReadOnlyList<HdrDisplayOutputSnapshot> outputs,
        string? targetDisplayName,
        int? targetLeft,
        int? targetTop,
        int targetWidth,
        int targetHeight)
    {
        ArgumentNullException.ThrowIfNull(outputs);

        var matchingOutput = FindByDisplayName(outputs, targetDisplayName)
            ?? FindByBounds(outputs, targetLeft, targetTop, targetWidth, targetHeight)
            ?? FindBySize(outputs, targetWidth, targetHeight);

        return matchingOutput is null
            ? Unknown()
            : FromOutputSnapshot(matchingOutput.Output, matchingOutput.MatchKind);
    }

    private static HdrDisplayOutputMatch? FindByDisplayName(
        IReadOnlyList<HdrDisplayOutputSnapshot> outputs,
        string? targetDisplayName)
    {
        if (string.IsNullOrWhiteSpace(targetDisplayName))
        {
            return null;
        }

        var output = outputs.FirstOrDefault(output =>
            string.Equals(output.DeviceName, targetDisplayName.Trim(), StringComparison.OrdinalIgnoreCase));

        return output is null
            ? null
            : new HdrDisplayOutputMatch(output, HdrDisplayMatchKind.DeviceName);
    }

    private static HdrDisplayOutputMatch? FindByBounds(
        IReadOnlyList<HdrDisplayOutputSnapshot> outputs,
        int? targetLeft,
        int? targetTop,
        int targetWidth,
        int targetHeight)
    {
        if (targetLeft is null || targetTop is null || targetWidth <= 0 || targetHeight <= 0)
        {
            return null;
        }

        var output = outputs.FirstOrDefault(output =>
            output.Left == targetLeft
            && output.Top == targetTop
            && output.Width == targetWidth
            && output.Height == targetHeight);

        return output is null
            ? null
            : new HdrDisplayOutputMatch(output, HdrDisplayMatchKind.DesktopBounds);
    }

    private static HdrDisplayOutputMatch? FindBySize(
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
            ? new HdrDisplayOutputMatch(matchingOutputs[0], HdrDisplayMatchKind.Size)
            : null;
    }

    private static HdrDisplayCapability FromOutputSnapshot(
        HdrDisplayOutputSnapshot output,
        HdrDisplayMatchKind matchKind)
    {
        var isHdr = IsHdrColorSpace(output.ColorSpace);
        return new HdrDisplayCapability(
            isHdr ? HdrDisplayState.Active : HdrDisplayState.Inactive,
            output.ColorSpace,
            output.DeviceName,
            matchKind);
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
    int Left,
    int Top,
    int Width,
    int Height,
    ColorSpaceType ColorSpace);

internal sealed record HdrDisplayOutputMatch(
    HdrDisplayOutputSnapshot Output,
    HdrDisplayMatchKind MatchKind);
