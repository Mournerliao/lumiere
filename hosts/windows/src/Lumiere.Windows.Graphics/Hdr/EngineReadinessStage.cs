namespace Lumiere.Windows.Graphics.Hdr;

internal enum EngineReadinessStage
{
    Unknown = 0,
    Capture,
    Graphics,
    Interop,
    Lifecycle,
}
