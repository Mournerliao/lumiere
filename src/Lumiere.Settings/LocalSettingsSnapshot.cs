using Lumiere.Graphics.Output;

namespace Lumiere.Settings;

/// <summary>
/// Schema-versioned local settings payload consumed by Lumiere entry points.
/// </summary>
public sealed record LocalSettingsSnapshot(
    int SchemaVersion,
    OutputTarget OutputTarget,
    string? SavePath,
    bool TimestampNaming,
    bool CopyAsImage,
    bool HdrAlertsEnabled,
    string FullscreenShortcut,
    string RegionShortcut,
    AfterCaptureBehavior AfterCaptureBehavior)
{
    public const int CurrentSchemaVersion = 1;

    public static readonly LocalSettingsSnapshot Default = new(
        CurrentSchemaVersion,
        OutputTarget.Clipboard,
        SavePath: null,
        TimestampNaming: true,
        CopyAsImage: true,
        HdrAlertsEnabled: true,
        FullscreenShortcut: string.Empty,
        RegionShortcut: string.Empty,
        AfterCaptureBehavior.None);
}
