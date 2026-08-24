using Lumiere.Windows.Graphics.Presentation;

namespace Lumiere.Windows.Graphics.Output;

/// <summary>
/// Describes one explicit sRGB Visual Match delivery operation.
/// </summary>
internal sealed record OutputRequest
{
    public required CapturedFrameTexture Texture { get; init; }

    public CropPixelRect? CropRegion { get; init; }

    public OutputTarget Delivery { get; init; } = OutputTarget.Clipboard;

    public bool CopyAsImage { get; init; } = true;

    public string? SaveDirectory { get; init; }

    public bool TimestampNaming { get; init; } = true;

    public bool ShouldWriteClipboard =>
        CopyAsImage && Delivery is (OutputTarget.Clipboard or OutputTarget.Both);

    public bool ShouldWriteFolder => Delivery is OutputTarget.Folder or OutputTarget.Both;
}
