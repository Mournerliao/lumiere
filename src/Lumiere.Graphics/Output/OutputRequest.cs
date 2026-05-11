using Lumiere.Graphics.Presentation;

namespace Lumiere.Graphics.Output;

/// <summary>
/// Represents a request to output a captured frame to one or more targets.
/// </summary>
public sealed record OutputRequest
{
    /// <summary>
    /// Gets the captured frame texture to output.
    /// </summary>
    public required CapturedFrameTexture Texture { get; init; }

    /// <summary>
    /// Gets the optional crop region to apply before output.
    /// Null means output the entire frame.
    /// </summary>
    public CropPixelRect? CropRegion { get; init; }

    /// <summary>
    /// Gets the output target settings placeholder.
    /// Real settings will be defined in Story 5.5; this is a forward-compatible stub.
    /// </summary>
    public OutputTargetSettings Settings { get; init; } = OutputTargetSettings.Default;
}

/// <summary>
/// Placeholder for output target settings. Will be replaced by ISettingsProvider integration in Story 5.5.
/// </summary>
public sealed record OutputTargetSettings
{
    /// <summary>
    /// Gets the default output target settings (clipboard only).
    /// </summary>
    public static readonly OutputTargetSettings Default = new();

    /// <summary>
    /// Gets the output target selection.
    /// </summary>
    public OutputTarget Target { get; init; } = OutputTarget.Clipboard;
}
