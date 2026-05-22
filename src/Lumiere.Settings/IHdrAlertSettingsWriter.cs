namespace Lumiere.Settings;

/// <summary>
/// Writes the in-session HDR alert preference until durable settings persistence arrives.
/// </summary>
public interface IHdrAlertSettingsWriter
{
    /// <summary>
    /// Sets whether optional HDR alert chrome should be shown.
    /// </summary>
    void SetHdrAlertsEnabled(bool enabled);
}
