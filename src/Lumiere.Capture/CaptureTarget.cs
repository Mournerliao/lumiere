using Windows.Graphics;
using Windows.Graphics.Capture;

namespace Lumiere.Capture;

public sealed class CaptureTarget
{
    internal const int MaxTextureDimension = 16_384;

    // GraphicsCaptureItem has no documented IDisposable contract; session resources own WGC teardown.
    private readonly GraphicsCaptureItem? item;

    private CaptureTarget(
        GraphicsCaptureItem? item,
        SizeInt32 size,
        string displayName,
        CaptureTargetKind kind)
    {
        this.item = item;
        Size = size;
        DisplayName = displayName;
        Kind = kind;
    }

    public GraphicsCaptureItem Item =>
        item ?? throw new InvalidOperationException("Capture target does not contain a GraphicsCaptureItem.");

    public bool HasCaptureItem => item is not null;

    public SizeInt32 Size { get; }

    public string DisplayName { get; }

    public CaptureTargetKind Kind { get; }

    internal static CaptureTarget CreateForTest(
        SizeInt32 size,
        string displayName,
        CaptureTargetKind kind = CaptureTargetKind.Unknown)
    {
        ValidateSize(size);

        return new CaptureTarget(
            null,
            size,
            string.IsNullOrWhiteSpace(displayName)
                ? "Capture target"
                : displayName,
            kind);
    }

    public static CaptureTarget FromItem(GraphicsCaptureItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        ValidateSize(item.Size);

        return new CaptureTarget(
            item,
            item.Size,
            string.IsNullOrWhiteSpace(item.DisplayName)
                ? "Capture target"
                : item.DisplayName,
            CaptureTargetKind.Unknown);
    }

    private static void ValidateSize(SizeInt32 size)
    {
        if (size.Width <= 0 || size.Height <= 0)
        {
            throw new ArgumentException(
                $"Capture target reported an invalid size: {size.Width}x{size.Height}.",
                nameof(size));
        }

        if (size.Width > MaxTextureDimension || size.Height > MaxTextureDimension)
        {
            throw new ArgumentException(
                $"Capture target size exceeds the D3D11 texture limit: {size.Width}x{size.Height}.",
                nameof(size));
        }
    }
}
