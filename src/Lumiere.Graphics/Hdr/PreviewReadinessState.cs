namespace Lumiere.Graphics.Hdr;

public enum PreviewReadinessState
{
    Unknown = 0,
    Initializing,
    Ready,
    Degraded,
    Unsupported,
    Failed,
}
