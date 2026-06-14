using Lumiere.Graphics.Output;

namespace Lumiere.Settings;

/// <summary>
/// Provides shared local settings for MVP settings consumers.
/// </summary>
public sealed class DefaultSettingsProvider : ISettingsWriterAggregator
{
    private readonly LocalSettingsStore store;
    private LocalSettingsSnapshot settings;

    public DefaultSettingsProvider()
        : this(LocalSettingsStore.CreateDefault())
    {
    }

    public DefaultSettingsProvider(LocalSettingsStore store)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        settings = store.Load().Settings;
    }

    /// <inheritdoc/>
    public OutputTarget OutputTarget => settings.OutputTarget;

    /// <inheritdoc/>
    public string? SavePath => settings.SavePath;

    /// <inheritdoc/>
    public bool TimestampNaming => settings.TimestampNaming;

    /// <inheritdoc/>
    public bool CopyAsImage => settings.CopyAsImage;

    /// <inheritdoc/>
    public bool HdrAlertsEnabled => settings.HdrAlertsEnabled;

    /// <inheritdoc/>
    public string FullscreenShortcut => settings.FullscreenShortcut;

    /// <inheritdoc/>
    public string RegionShortcut => settings.RegionShortcut;

    /// <inheritdoc/>
    public AfterCaptureBehavior AfterCaptureBehavior => settings.AfterCaptureBehavior;

    /// <inheritdoc/>
    public string ExportColorFormat => settings.ExportColorFormat;

    /// <inheritdoc/>
    public void SetHdrAlertsEnabled(bool enabled)
    {
        settings = settings with { HdrAlertsEnabled = enabled };
        store.Save(settings);
    }

    /// <inheritdoc/>
    public void SetOutputTarget(OutputTarget target)
    {
        if (!Enum.IsDefined(target))
        {
            throw new ArgumentOutOfRangeException(nameof(target), target, "Output target must be a defined value.");
        }

        settings = settings with { OutputTarget = target };
        store.Save(settings);
    }

    /// <inheritdoc/>
    public void SetCopyAsImage(bool enabled)
    {
        settings = settings with { CopyAsImage = enabled };
        store.Save(settings);
    }

    /// <inheritdoc/>
    public void SetTimestampNaming(bool enabled)
    {
        settings = settings with { TimestampNaming = enabled };
        store.Save(settings);
    }

    /// <inheritdoc/>
    public void SetSavePath(string? path)
    {
        settings = settings with { SavePath = path };
        store.Save(settings);
    }

    /// <inheritdoc/>
    public void SetAfterCaptureBehavior(AfterCaptureBehavior behavior)
    {
        if (!Enum.IsDefined(behavior))
        {
            throw new ArgumentOutOfRangeException(nameof(behavior), behavior, "After-capture behavior must be a defined value.");
        }

        settings = settings with { AfterCaptureBehavior = behavior };
        store.Save(settings);
    }

    /// <inheritdoc/>
    public void SetFullscreenShortcut(string? shortcut)
    {
        settings = settings with { FullscreenShortcut = shortcut ?? string.Empty };
        store.Save(settings);
    }

    /// <inheritdoc/>
    public void SetRegionShortcut(string? shortcut)
    {
        settings = settings with { RegionShortcut = shortcut ?? string.Empty };
        store.Save(settings);
    }

    /// <inheritdoc/>
    public void SetExportColorFormat(string format)
    {
        settings = settings with { ExportColorFormat = format };
        store.Save(settings);
    }
}
