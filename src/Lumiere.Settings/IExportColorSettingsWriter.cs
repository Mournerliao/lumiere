namespace Lumiere.Settings;

/// <summary>
/// Writes the export color format preference to the shared local settings source.
/// </summary>
public interface IExportColorSettingsWriter
{
    /// <summary>
    /// Sets the export color format. Only formats with validated implementation semantics should be passed.
    /// </summary>
    void SetExportColorFormat(string format);
}
