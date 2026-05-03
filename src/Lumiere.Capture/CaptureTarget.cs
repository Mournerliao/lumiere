using Windows.Graphics;
using Windows.Graphics.Capture;

namespace Lumiere.Capture;

public sealed class CaptureTarget
{
    private CaptureTarget(
        GraphicsCaptureItem item,
        SizeInt32 size,
        string displayName,
        CaptureTargetKind kind)
    {
        Item = item;
        Size = size;
        DisplayName = displayName;
        Kind = kind;
    }

    public GraphicsCaptureItem Item { get; }

    public SizeInt32 Size { get; }

    public string DisplayName { get; }

    public CaptureTargetKind Kind { get; }

    internal static CaptureTarget CreateForTest(
        SizeInt32 size,
        string displayName,
        CaptureTargetKind kind = CaptureTargetKind.Unknown)
    {
        if (size.Width <= 0 || size.Height <= 0)
        {
            throw new ArgumentException(
                $"Capture target reported an invalid size: {size.Width}x{size.Height}.",
                nameof(size));
        }

        return new CaptureTarget(
            null!,
            size,
            string.IsNullOrWhiteSpace(displayName)
                ? "Capture target"
                : displayName,
            kind);
    }

    public static CaptureTarget FromItem(GraphicsCaptureItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item.Size.Width <= 0 || item.Size.Height <= 0)
        {
            throw new ArgumentException(
                $"Capture target reported an invalid size: {item.Size.Width}x{item.Size.Height}.",
                nameof(item));
        }

        return new CaptureTarget(
            item,
            item.Size,
            string.IsNullOrWhiteSpace(item.DisplayName)
                ? "Capture target"
                : item.DisplayName,
            CaptureTargetKind.Unknown);
    }
}
