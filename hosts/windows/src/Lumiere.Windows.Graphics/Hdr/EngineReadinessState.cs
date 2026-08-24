namespace Lumiere.Windows.Graphics.Hdr;

internal enum EngineReadinessState
{
    Unknown = 0,
    Initializing,
    Ready,
    Degraded,
    Unsupported,
    Failed,
}
