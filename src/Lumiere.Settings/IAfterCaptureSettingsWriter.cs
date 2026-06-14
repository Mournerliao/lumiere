namespace Lumiere.Settings;

/// <summary>
/// Writes the after-capture behavior preference to the shared local settings source.
/// </summary>
public interface IAfterCaptureSettingsWriter
{
    /// <summary>
    /// Sets the after-capture behavior applied when folder output creates a file artifact.
    /// </summary>
    void SetAfterCaptureBehavior(AfterCaptureBehavior behavior);
}
