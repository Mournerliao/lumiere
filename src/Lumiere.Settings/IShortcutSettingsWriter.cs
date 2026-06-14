namespace Lumiere.Settings;

/// <summary>
/// Writes capture shortcut preferences to the shared local settings source.
/// </summary>
public interface IShortcutSettingsWriter
{
    /// <summary>
    /// Sets the fullscreen capture shortcut. Null or empty clears the shortcut.
    /// </summary>
    void SetFullscreenShortcut(string? shortcut);

    /// <summary>
    /// Sets the region capture shortcut. Null or empty clears the shortcut.
    /// </summary>
    void SetRegionShortcut(string? shortcut);
}
