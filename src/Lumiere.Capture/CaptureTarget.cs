using Windows.Graphics;
using Windows.Graphics.Capture;

namespace Lumiere.Capture;

public sealed class CaptureTarget
{
    private CaptureTarget(
        GraphicsCaptureItem item,
        SizeInt32 size,
        string displayName)
    {
        Item = item;
        Size = size;
        DisplayName = displayName;
    }

    public GraphicsCaptureItem Item { get; }

    public SizeInt32 Size { get; }

    public string DisplayName { get; }

    public static CaptureTarget FromItem(GraphicsCaptureItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return new CaptureTarget(
            item,
            item.Size,
            string.IsNullOrWhiteSpace(item.DisplayName)
                ? "Capture target"
                : item.DisplayName);
    }
}
