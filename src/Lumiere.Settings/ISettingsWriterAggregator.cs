namespace Lumiere.Settings;

/// <summary>
/// Aggregates all settings writer interfaces and the settings provider
/// into a single injectable dependency.
/// </summary>
public interface ISettingsWriterAggregator :
    ISettingsProvider,
    IHdrAlertSettingsWriter,
    IOutputSettingsWriter,
    ITimestampSettingsWriter,
    ISavePathSettingsWriter,
    IAfterCaptureSettingsWriter,
    IShortcutSettingsWriter,
    IExportColorSettingsWriter
{
}
