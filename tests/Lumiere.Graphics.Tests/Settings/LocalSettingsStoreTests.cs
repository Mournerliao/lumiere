using Lumiere.Graphics.Output;
using Lumiere.Settings;
using Xunit;

namespace Lumiere.Graphics.Tests.Settings;

public sealed class LocalSettingsStoreTests : IDisposable
{
    private readonly string testDirectory = Path.Combine(Path.GetTempPath(), "lumiere-settings-tests", Guid.NewGuid().ToString("N"));
    private readonly string settingsPath;

    public LocalSettingsStoreTests()
    {
        settingsPath = Path.Combine(testDirectory, "settings.json");
    }

    [Fact]
    public void Load_WhenFileMissing_ReturnsSafeDefaultsWithDiagnostics()
    {
        var store = new LocalSettingsStore(settingsPath);

        var result = store.Load();

        Assert.True(result.UsedFallback);
        Assert.Contains("missing", result.DiagnosticDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(LocalSettingsSnapshot.Default, result.Settings);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsAllPersistedSettings()
    {
        var expected = new LocalSettingsSnapshot(
            SchemaVersion: LocalSettingsSnapshot.CurrentSchemaVersion,
            OutputTarget: OutputTarget.Both,
            SavePath: @"D:\Captures\HDR",
            TimestampNaming: false,
            CopyAsImage: false,
            HdrAlertsEnabled: false,
            FullscreenShortcut: "Ctrl+Shift+F",
            RegionShortcut: "Ctrl+Shift+R",
            AfterCaptureBehavior: AfterCaptureBehavior.Reveal,
            ExportColorFormat: "sRGB");
        var store = new LocalSettingsStore(settingsPath);

        store.Save(expected);
        var result = store.Load();

        Assert.False(result.UsedFallback);
        Assert.Null(result.DiagnosticDetail);
        Assert.Equal(expected, result.Settings);
    }

    [Fact]
    public void Load_WhenJsonInvalid_ReturnsSafeDefaultsWithDiagnostics()
    {
        Directory.CreateDirectory(testDirectory);
        File.WriteAllText(settingsPath, "{not-json");
        var store = new LocalSettingsStore(settingsPath);

        var result = store.Load();

        Assert.True(result.UsedFallback);
        Assert.Contains("invalid", result.DiagnosticDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(LocalSettingsSnapshot.Default, result.Settings);
    }

    [Fact]
    public void Load_WhenEnumValueInvalid_ReturnsSafeDefaultsWithDiagnostics()
    {
        Directory.CreateDirectory(testDirectory);
        File.WriteAllText(settingsPath, """
            {
              "schemaVersion": 1,
              "outputTarget": "Archive",
              "savePath": "D:\\Captures",
              "timestampNaming": true,
              "copyAsImage": true,
              "hdrAlertsEnabled": true,
              "fullscreenShortcut": "Ctrl+Shift+F",
              "regionShortcut": "Ctrl+Shift+R",
              "afterCaptureBehavior": "Reveal"
            }
            """);
        var store = new LocalSettingsStore(settingsPath);

        var result = store.Load();

        Assert.True(result.UsedFallback);
        Assert.Contains("OutputTarget", result.DiagnosticDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(LocalSettingsSnapshot.Default, result.Settings);
    }

    [Fact]
    public void Load_WhenSchemaVersionUnsupported_ReturnsSafeDefaultsWithDiagnostics()
    {
        Directory.CreateDirectory(testDirectory);
        File.WriteAllText(settingsPath, """
            {
              "schemaVersion": 0,
              "outputTarget": "Clipboard",
              "timestampNaming": true,
              "copyAsImage": true,
              "hdrAlertsEnabled": true,
              "fullscreenShortcut": "",
              "regionShortcut": "",
              "afterCaptureBehavior": "None"
            }
            """);
        var store = new LocalSettingsStore(settingsPath);

        var result = store.Load();

        Assert.True(result.UsedFallback);
        Assert.Contains("schema", result.DiagnosticDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(LocalSettingsSnapshot.Default, result.Settings);
    }

    public void Dispose()
    {
        if (Directory.Exists(testDirectory))
        {
            Directory.Delete(testDirectory, recursive: true);
        }
    }
}
