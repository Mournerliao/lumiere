namespace Lumiere.Infrastructure.Interop;

public interface IWicJpegXrEncoder
{
    WicJpegXrEncoderReadiness Readiness { get; }

    byte[] EncodeRgbaHalf(WicJpegXrEncodeRequest request);
}

public sealed record WicJpegXrEncodeRequest
{
    public const int RgbaHalfBytesPerPixel = 8;

    public WicJpegXrEncodeRequest(
        int width,
        int height,
        int strideBytes,
        byte[] rgbaHalfPixels,
        IReadOnlyList<WicJpegXrMetadataEntry>? metadata = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfLessThan(strideBytes, width * RgbaHalfBytesPerPixel);
        ArgumentNullException.ThrowIfNull(rgbaHalfPixels);

        var requiredByteLength = checked(strideBytes * height);
        if (rgbaHalfPixels.Length != requiredByteLength)
        {
            throw new ArgumentException(
                $"RGBA half JPEG XR input requires exactly {requiredByteLength} bytes.",
                nameof(rgbaHalfPixels));
        }

        Width = width;
        Height = height;
        StrideBytes = strideBytes;
        RgbaHalfPixels = rgbaHalfPixels;
        Metadata = metadata?.ToArray() ?? [];
    }

    public int Width { get; }

    public int Height { get; }

    public int StrideBytes { get; }

    public byte[] RgbaHalfPixels { get; }

    public IReadOnlyList<WicJpegXrMetadataEntry> Metadata { get; }
}

public sealed record WicJpegXrMetadataEntry
{
    public WicJpegXrMetadataEntry(string queryPath, string value)
    {
        if (string.IsNullOrWhiteSpace(queryPath))
        {
            throw new ArgumentException("WIC JPEG XR metadata query path must be provided.", nameof(queryPath));
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("WIC JPEG XR metadata value must be provided.", nameof(value));
        }

        QueryPath = queryPath;
        Value = value;
    }

    public string QueryPath { get; }

    public string Value { get; }
}

public sealed record WicJpegXrEncoderReadiness(
    bool HasWindowsWicFactory,
    bool HasJpegXrContainerEncoder,
    bool AcceptsRgbaHalfPixelFormat,
    IReadOnlyList<string> Blockers)
{
    public static WicJpegXrEncoderReadiness Unknown { get; } =
        new(
            HasWindowsWicFactory: false,
            HasJpegXrContainerEncoder: false,
            AcceptsRgbaHalfPixelFormat: false,
            Blockers: ["Windows WIC JPEG XR encoder readiness has not been probed."]);

    public bool IsReady =>
        HasWindowsWicFactory
        && HasJpegXrContainerEncoder
        && AcceptsRgbaHalfPixelFormat
        && Blockers.Count == 0;
}
