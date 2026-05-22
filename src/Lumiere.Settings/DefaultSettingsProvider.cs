using Lumiere.Graphics.Output;

namespace Lumiere.Settings;

/// <summary>
/// Provides hardcoded MVP default settings.
/// This stub exists so that MainWindow and future entry points can consume settings through the interface;
/// real persistence comes in Story 5.5.
/// </summary>
public sealed class DefaultSettingsProvider : ISettingsProvider, IHdrAlertSettingsWriter
{
    private bool hdrAlertsEnabled = true;

    /// <inheritdoc/>
    public OutputTarget OutputTarget => OutputTarget.Clipboard;

    /// <inheritdoc/>
    public string? SavePath => null;

    /// <inheritdoc/>
    public bool TimestampNaming => true;

    /// <inheritdoc/>
    public bool CopyAsImage => true;

    /// <inheritdoc/>
    public bool HdrAlertsEnabled => hdrAlertsEnabled;

    /// <inheritdoc/>
    public string FullscreenShortcut => string.Empty;

    /// <inheritdoc/>
    public string RegionShortcut => string.Empty;

    /// <inheritdoc/>
    public void SetHdrAlertsEnabled(bool enabled) => hdrAlertsEnabled = enabled;
}
