namespace Lumiere.Windows.Graphics.Hdr;

public enum EngineReadinessState
{
    Unknown = 0,
    Initializing,
    Ready,
    Degraded,
    Unsupported,
    Failed,
}
