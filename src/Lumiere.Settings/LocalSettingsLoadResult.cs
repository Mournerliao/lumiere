namespace Lumiere.Settings;

/// <summary>
/// Result of reading local settings, including fallback diagnostics.
/// </summary>
public sealed record LocalSettingsLoadResult(
    LocalSettingsSnapshot Settings,
    bool UsedFallback,
    string? DiagnosticDetail);
