namespace Lumiere.Settings;

/// <summary>
/// Represents the output target selection for screenshot output.
/// </summary>
public enum OutputTarget
{
    /// <summary>
    /// Output to clipboard only.
    /// </summary>
    Clipboard = 0,

    /// <summary>
    /// Output to folder only.
    /// </summary>
    Folder = 1,

    /// <summary>
    /// Output to both clipboard and folder.
    /// </summary>
    Both = 2
}
