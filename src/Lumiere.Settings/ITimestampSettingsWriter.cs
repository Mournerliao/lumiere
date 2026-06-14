namespace Lumiere.Settings;

/// <summary>
/// Writes the timestamp naming preference to the shared local settings source.
/// </summary>
public interface ITimestampSettingsWriter
{
    /// <summary>
    /// Sets whether folder output should use timestamp-based file naming.
    /// </summary>
    void SetTimestampNaming(bool enabled);
}
