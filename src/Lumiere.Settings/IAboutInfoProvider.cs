namespace Lumiere.Settings;

/// <summary>
/// Provides app identity metadata for native About surfaces.
/// </summary>
public interface IAboutInfoProvider
{
    string AppName { get; }

    string Version { get; }

    string Description { get; }
}
