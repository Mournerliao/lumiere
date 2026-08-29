using System.Runtime.InteropServices;

namespace Lumiere.Windows.Interop;

internal static class SdrWhiteLevelInterop
{
    private const string User32Library = "user32.dll";
    private const uint QdcOnlyActivePaths = 0x00000002;
    private const int ErrorSuccess = 0;
    private const int ErrorInsufficientBuffer = 122;
    private const int MaxTopologyRetries = 3;
    private const float ScrgbReferenceWhiteInNits = 80f;
    private const float SdrWhiteLevelScale = 1000f;

    public static float? GetForDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return null;
        }

        for (var attempt = 0; attempt < MaxTopologyRetries; attempt++)
        {
            var bufferResult = GetDisplayConfigBufferSizes(
                QdcOnlyActivePaths,
                out var pathCount,
                out var modeCount);
            if (bufferResult != ErrorSuccess)
            {
                throw DisplayConfigFailure(
                    "GetDisplayConfigBufferSizes",
                    bufferResult,
                    "Could not size the active display topology buffers.");
            }

            var paths = new DisplayConfigPathInfo[pathCount];
            var modes = new DisplayConfigModeInfo[modeCount];
            var queryResult = QueryDisplayConfig(
                QdcOnlyActivePaths,
                ref pathCount,
                paths,
                ref modeCount,
                modes,
                IntPtr.Zero);
            if (queryResult == ErrorInsufficientBuffer)
            {
                continue;
            }

            if (queryResult != ErrorSuccess)
            {
                throw DisplayConfigFailure(
                    "QueryDisplayConfig",
                    queryResult,
                    "Could not query the active display topology.");
            }

            return FindForDisplayName(paths.AsSpan(0, checked((int)pathCount)), displayName);
        }

        throw DisplayConfigFailure(
            "QueryDisplayConfig",
            ErrorInsufficientBuffer,
            "The active display topology changed repeatedly while it was queried.");
    }

    internal static float? ConvertRawSdrWhiteLevelToNits(uint rawSdrWhiteLevel) =>
        rawSdrWhiteLevel == 0
            ? null
            : rawSdrWhiteLevel / SdrWhiteLevelScale * ScrgbReferenceWhiteInNits;

    private static float? FindForDisplayName(
        ReadOnlySpan<DisplayConfigPathInfo> paths,
        string displayName)
    {
        foreach (var path in paths)
        {
            var sourceName = new DisplayConfigSourceDeviceName
            {
                Header = new DisplayConfigDeviceInfoHeader
                {
                    Type = DisplayConfigDeviceInfoType.GetSourceName,
                    Size = (uint)Marshal.SizeOf<DisplayConfigSourceDeviceName>(),
                    AdapterId = path.SourceInfo.AdapterId,
                    Id = path.SourceInfo.Id,
                },
            };
            if (DisplayConfigGetDeviceInfo(ref sourceName) != ErrorSuccess
                || !string.Equals(
                    sourceName.ViewGdiDeviceName,
                    displayName.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var whiteLevel = new DisplayConfigSdrWhiteLevel
            {
                Header = new DisplayConfigDeviceInfoHeader
                {
                    Type = DisplayConfigDeviceInfoType.GetSdrWhiteLevel,
                    Size = (uint)Marshal.SizeOf<DisplayConfigSdrWhiteLevel>(),
                    AdapterId = path.TargetInfo.AdapterId,
                    Id = path.TargetInfo.Id,
                },
            };
            return DisplayConfigGetDeviceInfo(ref whiteLevel) == ErrorSuccess
                ? ConvertRawSdrWhiteLevelToNits(whiteLevel.SdrWhiteLevel)
                : null;
        }

        return null;
    }

    private static NativeInteropException DisplayConfigFailure(
        string operationName,
        int errorCode,
        string technicalDetail) =>
        new(
            operationName,
            "DisplayConfig",
            errorCode,
            technicalDetail,
            "Could not determine the target display's SDR white level.");

    [DllImport(User32Library)]
    private static extern int GetDisplayConfigBufferSizes(
        uint flags,
        out uint numPathArrayElements,
        out uint numModeInfoArrayElements);

    [DllImport(User32Library)]
    private static extern int QueryDisplayConfig(
        uint flags,
        ref uint numPathArrayElements,
        [Out] DisplayConfigPathInfo[] pathInfoArray,
        ref uint numModeInfoArrayElements,
        [Out] DisplayConfigModeInfo[] modeInfoArray,
        IntPtr currentTopologyId);

    [DllImport(User32Library, EntryPoint = "DisplayConfigGetDeviceInfo")]
    private static extern int DisplayConfigGetDeviceInfo(ref DisplayConfigSourceDeviceName requestPacket);

    [DllImport(User32Library, EntryPoint = "DisplayConfigGetDeviceInfo")]
    private static extern int DisplayConfigGetDeviceInfo(ref DisplayConfigSdrWhiteLevel requestPacket);

    [StructLayout(LayoutKind.Sequential)]
    private struct DisplayConfigPathInfo
    {
        public DisplayConfigPathSourceInfo SourceInfo;
        public DisplayConfigPathTargetInfo TargetInfo;
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DisplayConfigPathSourceInfo
    {
        public Luid AdapterId;
        public uint Id;
        public uint ModeInfoIndex;
        public uint StatusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DisplayConfigPathTargetInfo
    {
        public Luid AdapterId;
        public uint Id;
        public uint ModeInfoIndex;
        public uint OutputTechnology;
        public uint Rotation;
        public uint Scaling;
        public DisplayConfigRational RefreshRate;
        public uint ScanLineOrdering;
        [MarshalAs(UnmanagedType.Bool)]
        public bool TargetAvailable;
        public uint StatusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DisplayConfigRational
    {
        public uint Numerator;
        public uint Denominator;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DisplayConfigModeInfo
    {
        public uint InfoType;
        public uint Id;
        public Luid AdapterId;
        public DisplayConfigModeInfoUnion ModeInfo;
    }

    [StructLayout(LayoutKind.Explicit, Size = 48)]
    private struct DisplayConfigModeInfoUnion
    {
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DisplayConfigDeviceInfoHeader
    {
        public DisplayConfigDeviceInfoType Type;
        public uint Size;
        public Luid AdapterId;
        public uint Id;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DisplayConfigSourceDeviceName
    {
        public DisplayConfigDeviceInfoHeader Header;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string ViewGdiDeviceName;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DisplayConfigSdrWhiteLevel
    {
        public DisplayConfigDeviceInfoHeader Header;
        public uint SdrWhiteLevel;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct Luid
    {
        private readonly uint lowPart;
        private readonly int highPart;
    }

    private enum DisplayConfigDeviceInfoType : uint
    {
        GetSourceName = 1,
        GetSdrWhiteLevel = 11,
    }
}
