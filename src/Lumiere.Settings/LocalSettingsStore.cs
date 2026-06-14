using System.Text.Json;
using Lumiere.Graphics.Output;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lumiere.Settings;

/// <summary>
/// Reads and writes the local Lumiere settings file.
/// </summary>
public sealed class LocalSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private readonly ILogger logger;

    public LocalSettingsStore(string settingsPath, ILogger? logger = null)
    {
        SettingsPath = string.IsNullOrWhiteSpace(settingsPath)
            ? throw new ArgumentException("Settings path must be provided.", nameof(settingsPath))
            : settingsPath;
        this.logger = logger ?? NullLogger.Instance;
    }

    public string SettingsPath { get; }

    public static LocalSettingsStore CreateDefault(ILogger? logger = null)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var settingsPath = Path.Combine(localAppData, "Lumiere", "settings.json");
        return new LocalSettingsStore(settingsPath, logger);
    }

    public LocalSettingsLoadResult Load()
    {
        if (!File.Exists(SettingsPath))
        {
            const string detail = "Settings file is missing; using safe defaults.";
            logger.LogInformation("operation=LoadSettings, stage=Fallback, detail={Detail}", detail);
            return Fallback(detail);
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            var persisted = JsonSerializer.Deserialize<PersistedSettings>(json, JsonOptions);
            if (persisted is null)
            {
                return Invalid("Settings file is invalid or empty.");
            }

            return Validate(persisted);
        }
        catch (JsonException exception)
        {
            const string detail = "Settings file contains invalid JSON; using safe defaults.";
            logger.LogWarning(exception, "operation=LoadSettings, stage=Fallback, detail={Detail}", detail);
            return Fallback(detail);
        }
        catch (IOException exception)
        {
            const string detail = "Settings file could not be read; using safe defaults.";
            logger.LogWarning(exception, "operation=LoadSettings, stage=Fallback, detail={Detail}", detail);
            return Fallback(detail);
        }
        catch (UnauthorizedAccessException exception)
        {
            const string detail = "Settings file access was denied; using safe defaults.";
            logger.LogWarning(exception, "operation=LoadSettings, stage=Fallback, detail={Detail}", detail);
            return Fallback(detail);
        }
    }

    public void Save(LocalSettingsSnapshot settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var persisted = PersistedSettings.FromSnapshot(settings);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(persisted, JsonOptions));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogError(
                exception,
                "operation=SaveSettings, stage=Write, detail=Local settings could not be saved; in-memory preference remains active.");
        }
    }

    private static LocalSettingsLoadResult Fallback(string detail) =>
        new(LocalSettingsSnapshot.Default, UsedFallback: true, detail);

    private LocalSettingsLoadResult Invalid(string detail)
    {
        logger.LogWarning("operation=LoadSettings, stage=Fallback, detail={Detail}", detail);
        return Fallback(detail);
    }

    private LocalSettingsLoadResult Validate(PersistedSettings persisted)
    {
        if (persisted.SchemaVersion is not (1 or 2))
        {
            return Invalid($"Unsupported settings schema version {persisted.SchemaVersion}; using safe defaults.");
        }

        if (!TryParseEnum(persisted.OutputTarget, out OutputTarget outputTarget))
        {
            return Invalid($"Invalid OutputTarget value '{persisted.OutputTarget}'; using safe defaults.");
        }

        if (!TryParseEnum(persisted.AfterCaptureBehavior, out AfterCaptureBehavior afterCaptureBehavior))
        {
            return Invalid($"Invalid AfterCaptureBehavior value '{persisted.AfterCaptureBehavior}'; using safe defaults.");
        }

        if (persisted.TimestampNaming is null
            || persisted.CopyAsImage is null
            || persisted.HdrAlertsEnabled is null)
        {
            return Invalid("Required boolean settings are missing; using safe defaults.");
        }

        var exportColorFormat = string.IsNullOrWhiteSpace(persisted.ExportColorFormat)
            ? "sRGB"
            : persisted.ExportColorFormat.Trim();

        var settings = new LocalSettingsSnapshot(
            LocalSettingsSnapshot.CurrentSchemaVersion,
            outputTarget,
            NormalizeOptional(persisted.SavePath),
            persisted.TimestampNaming.Value,
            persisted.CopyAsImage.Value,
            persisted.HdrAlertsEnabled.Value,
            NormalizeRequired(persisted.FullscreenShortcut),
            NormalizeRequired(persisted.RegionShortcut),
            afterCaptureBehavior,
            exportColorFormat);

        return new LocalSettingsLoadResult(settings, UsedFallback: false, DiagnosticDetail: null);
    }

    private static bool TryParseEnum<TEnum>(string? value, out TEnum result)
        where TEnum : struct, Enum
    {
        if (!string.IsNullOrWhiteSpace(value)
            && Enum.TryParse(value.Trim(), ignoreCase: false, out result)
            && Enum.IsDefined(result))
        {
            return true;
        }

        result = default;
        return false;
    }

    private static string NormalizeRequired(string? value) => value?.Trim() ?? string.Empty;

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private sealed record PersistedSettings
    {
        public int SchemaVersion { get; init; }

        public string? OutputTarget { get; init; }

        public string? SavePath { get; init; }

        public bool? TimestampNaming { get; init; }

        public bool? CopyAsImage { get; init; }

        public bool? HdrAlertsEnabled { get; init; }

        public string? FullscreenShortcut { get; init; }

        public string? RegionShortcut { get; init; }

        public string? AfterCaptureBehavior { get; init; }

        public string? ExportColorFormat { get; init; }

        public static PersistedSettings FromSnapshot(LocalSettingsSnapshot snapshot) =>
            new()
            {
                SchemaVersion = snapshot.SchemaVersion,
                OutputTarget = snapshot.OutputTarget.ToString(),
                SavePath = snapshot.SavePath,
                TimestampNaming = snapshot.TimestampNaming,
                CopyAsImage = snapshot.CopyAsImage,
                HdrAlertsEnabled = snapshot.HdrAlertsEnabled,
                FullscreenShortcut = snapshot.FullscreenShortcut,
                RegionShortcut = snapshot.RegionShortcut,
                AfterCaptureBehavior = snapshot.AfterCaptureBehavior.ToString(),
                ExportColorFormat = snapshot.ExportColorFormat,
            };
    }
}
