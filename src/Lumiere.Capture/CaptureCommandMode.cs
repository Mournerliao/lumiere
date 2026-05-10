namespace Lumiere.Capture;

/// <summary>
/// Represents the capture mode for a capture command.
/// Explicit enum prevents mode inference from UI context or button names.
/// </summary>
public enum CaptureCommandMode
{
    /// <summary>
    /// Fullscreen capture of the current display.
    /// </summary>
    Fullscreen = 0,

    /// <summary>
    /// Region capture with user-selected crop area.
    /// </summary>
    Region = 1,
}
