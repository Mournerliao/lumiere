using System.Reflection;

namespace Lumiere.Settings;

/// <summary>
/// Provides About metadata from assembly/build metadata with safe product-copy fallbacks.
/// </summary>
public sealed class AssemblyAboutInfoProvider : IAboutInfoProvider
{
    private const string FallbackAppName = "Lumiere";
    private const string FallbackVersion = "1.0.0";
    private const string FallbackDescription = "Native Windows HDR-first capture and preview.";

    public AssemblyAboutInfoProvider(Assembly assembly, string? fallbackDescription = null)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        AppName = Normalize(
            assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product
            ?? assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title
            ?? assembly.GetName().Name,
            FallbackAppName);
        Version = Normalize(
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString(3),
            FallbackVersion);
        Description = Normalize(
            assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description,
            fallbackDescription ?? FallbackDescription);
    }

    private AssemblyAboutInfoProvider(string appName, string version, string description)
    {
        AppName = appName;
        Version = version;
        Description = description;
    }

    public string AppName { get; }

    public string Version { get; }

    public string Description { get; }

    public static AssemblyAboutInfoProvider CreateDefault() =>
        Assembly.GetEntryAssembly() is { } entryAssembly
            ? new AssemblyAboutInfoProvider(entryAssembly)
            : CreateFallback();

    public static AssemblyAboutInfoProvider CreateFallback() =>
        new(FallbackAppName, FallbackVersion, FallbackDescription);

    private static string Normalize(string? value, string fallback)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? fallback : trimmed;
    }
}
