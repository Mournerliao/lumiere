namespace Lumiere.Windows.Capture;

/// <summary>
/// Represents a capture command with explicit mode (fullscreen or region).
/// Immutable record to ensure thread safety and prevent mutation after creation.
/// </summary>
public sealed record CaptureCommand
{
    private CaptureCommand(CaptureCommandMode mode, CaptureTarget? target = null)
    {
        Mode = mode;
        Target = target;
    }

    /// <summary>
    /// Gets the capture mode (fullscreen or region).
    /// </summary>
    public CaptureCommandMode Mode { get; }

    /// <summary>
    /// Gets the optional capture target for direct monitor capture.
    /// Null when command is routed through session contract without pre-selected target.
    /// </summary>
    public CaptureTarget? Target { get; }

    /// <summary>
    /// Creates a fullscreen capture command.
    /// </summary>
    /// <param name="target">Optional pre-selected capture target for direct monitor capture.</param>
    /// <returns>A new CaptureCommand for fullscreen capture.</returns>
    public static CaptureCommand Fullscreen(CaptureTarget? target = null) =>
        new(CaptureCommandMode.Fullscreen, target);

    /// <summary>
    /// Creates a region capture command.
    /// </summary>
    /// <param name="target">Optional pre-selected capture target for direct monitor capture.</param>
    /// <returns>A new CaptureCommand for region capture.</returns>
    public static CaptureCommand Region(CaptureTarget? target = null) =>
        new(CaptureCommandMode.Region, target);
}
