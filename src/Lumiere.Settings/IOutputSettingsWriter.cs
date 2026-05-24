using Lumiere.Graphics.Output;

namespace Lumiere.Settings;

/// <summary>
/// Writes supported output preferences to the shared local settings source.
/// </summary>
public interface IOutputSettingsWriter
{
    /// <summary>
    /// Sets the output target selection consumed by the configured output pipeline.
    /// </summary>
    void SetOutputTarget(OutputTarget target);

    /// <summary>
    /// Sets whether clipboard output should copy the captured crop as an image.
    /// </summary>
    void SetCopyAsImage(bool enabled);
}
