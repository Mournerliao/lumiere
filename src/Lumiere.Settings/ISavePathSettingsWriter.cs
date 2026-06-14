namespace Lumiere.Settings;

/// <summary>
/// Writes the save path preference to the shared local settings source.
/// </summary>
public interface ISavePathSettingsWriter
{
    /// <summary>
    /// Sets the folder path used for file output. Null clears the configured path.
    /// </summary>
    void SetSavePath(string? path);
}
