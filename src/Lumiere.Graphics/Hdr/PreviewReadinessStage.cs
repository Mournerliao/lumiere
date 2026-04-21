namespace Lumiere.Graphics.Hdr;

public enum PreviewReadinessStage
{
    Unknown = 0,
    Capture,
    Graphics,
    Presentation,
    Overlay,
    Interop,
    Lifecycle,
}
