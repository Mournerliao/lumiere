using Lumiere.Graphics.Output;

namespace Lumiere.Settings;

/// <summary>
/// Read-only settings provider for shared capture and output settings.
/// </summary>
public interface ISettingsProvider
{
    /// <summary>
    /// Gets the output target selection (clipboard, folder, or both).
    /// </summary>
    OutputTarget OutputTarget { get; }

    /// <summary>
    /// Gets the save path for folder output. Null means use default location.
    /// </summary>
    string? SavePath { get; }

    /// <summary>
    /// Gets whether to include timestamp in output filename.
    /// </summary>
    bool TimestampNaming { get; }

    /// <summary>
    /// Gets whether to copy as image to clipboard (vs. file path).
    /// </summary>
    bool CopyAsImage { get; }

    /// <summary>
    /// Gets whether HDR alert notifications are enabled.
    /// </summary>
    bool HdrAlertsEnabled { get; }

    /// <summary>
    /// Gets the fullscreen capture shortcut key combination. Empty string means no shortcut configured.
    /// </summary>
    string FullscreenShortcut { get; }

    /// <summary>
    /// Gets the region capture shortcut key combination. Empty string means no shortcut configured.
    /// </summary>
    string RegionShortcut { get; }

    /// <summary>
    /// Gets the supported post-capture behavior preference.
    /// </summary>
    AfterCaptureBehavior AfterCaptureBehavior { get; }

    /// <summary>
    /// Gets the export color format preference (e.g., "sRGB").
    /// </summary>
    string ExportColorFormat { get; }
}
