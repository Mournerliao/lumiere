using Lumiere.Graphics.Output;
using Lumiere.Settings;
using Xunit;

namespace Lumiere.Graphics.Tests.Settings;

public sealed class SettingsWriterTests
{
    [Fact]
    public void SetHdrAlertsEnabled_PersistsAcrossInstances()
    {
        using var fixture = new SettingsFileFixture();
        var store = new LocalSettingsStore(fixture.SettingsPath);
        var provider = new DefaultSettingsProvider(store);

        provider.SetHdrAlertsEnabled(false);
        var reloaded = new DefaultSettingsProvider(store);

        Assert.False(reloaded.HdrAlertsEnabled);
    }

    [Fact]
    public void SetOutputTarget_PersistsAcrossInstances()
    {
        using var fixture = new SettingsFileFixture();
        var store = new LocalSettingsStore(fixture.SettingsPath);
        var provider = new DefaultSettingsProvider(store);

        provider.SetOutputTarget(OutputTarget.Folder);
        var reloaded = new DefaultSettingsProvider(store);

        Assert.Equal(OutputTarget.Folder, reloaded.OutputTarget);
    }

    [Fact]
    public void SetTimestampNaming_PersistsAcrossInstances()
    {
        using var fixture = new SettingsFileFixture();
        var store = new LocalSettingsStore(fixture.SettingsPath);
        var provider = new DefaultSettingsProvider(store);

        provider.SetTimestampNaming(false);
        var reloaded = new DefaultSettingsProvider(store);

        Assert.False(reloaded.TimestampNaming);
    }

    [Fact]
    public void SetSavePath_PersistsAcrossInstances()
    {
        using var fixture = new SettingsFileFixture();
        var store = new LocalSettingsStore(fixture.SettingsPath);
        var provider = new DefaultSettingsProvider(store);

        provider.SetSavePath(@"D:\Captures");
        var reloaded = new DefaultSettingsProvider(store);

        Assert.Equal(@"D:\Captures", reloaded.SavePath);
    }

    [Fact]
    public void SetSavePath_NullClearsPath()
    {
        using var fixture = new SettingsFileFixture();
        var store = new LocalSettingsStore(fixture.SettingsPath);
        var provider = new DefaultSettingsProvider(store);

        provider.SetSavePath(@"D:\Captures");
        provider.SetSavePath(null);
        var reloaded = new DefaultSettingsProvider(store);

        Assert.Null(reloaded.SavePath);
    }

    [Fact]
    public void SetAfterCaptureBehavior_PersistsAcrossInstances()
    {
        using var fixture = new SettingsFileFixture();
        var store = new LocalSettingsStore(fixture.SettingsPath);
        var provider = new DefaultSettingsProvider(store);

        provider.SetAfterCaptureBehavior(AfterCaptureBehavior.Open);
        var reloaded = new DefaultSettingsProvider(store);

        Assert.Equal(AfterCaptureBehavior.Open, reloaded.AfterCaptureBehavior);
    }

    [Fact]
    public void SetFullscreenShortcut_PersistsAcrossInstances()
    {
        using var fixture = new SettingsFileFixture();
        var store = new LocalSettingsStore(fixture.SettingsPath);
        var provider = new DefaultSettingsProvider(store);

        provider.SetFullscreenShortcut("Ctrl+Alt+F");
        var reloaded = new DefaultSettingsProvider(store);

        Assert.Equal("Ctrl+Alt+F", reloaded.FullscreenShortcut);
    }

    [Fact]
    public void SetRegionShortcut_PersistsAcrossInstances()
    {
        using var fixture = new SettingsFileFixture();
        var store = new LocalSettingsStore(fixture.SettingsPath);
        var provider = new DefaultSettingsProvider(store);

        provider.SetRegionShortcut("Ctrl+Alt+R");
        var reloaded = new DefaultSettingsProvider(store);

        Assert.Equal("Ctrl+Alt+R", reloaded.RegionShortcut);
    }

    [Fact]
    public void SetExportColorFormat_PersistsAcrossInstances()
    {
        using var fixture = new SettingsFileFixture();
        var store = new LocalSettingsStore(fixture.SettingsPath);
        var provider = new DefaultSettingsProvider(store);

        provider.SetExportColorFormat("HDR10");
        var reloaded = new DefaultSettingsProvider(store);

        Assert.Equal("HDR10", reloaded.ExportColorFormat);
    }

    [Fact]
    public void ImplementsISettingsWriterAggregator()
    {
        using var fixture = new SettingsFileFixture();
        var provider = new DefaultSettingsProvider(new LocalSettingsStore(fixture.SettingsPath));

        Assert.IsAssignableFrom<ISettingsWriterAggregator>(provider);
        Assert.IsAssignableFrom<ISettingsProvider>(provider);
        Assert.IsAssignableFrom<IHdrAlertSettingsWriter>(provider);
        Assert.IsAssignableFrom<IOutputSettingsWriter>(provider);
        Assert.IsAssignableFrom<ITimestampSettingsWriter>(provider);
        Assert.IsAssignableFrom<ISavePathSettingsWriter>(provider);
        Assert.IsAssignableFrom<IAfterCaptureSettingsWriter>(provider);
        Assert.IsAssignableFrom<IShortcutSettingsWriter>(provider);
        Assert.IsAssignableFrom<IExportColorSettingsWriter>(provider);
    }

    private sealed class SettingsFileFixture : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "lumiere-writer-tests", Guid.NewGuid().ToString("N"));

        public string SettingsPath => Path.Combine(directory, "settings.json");

        public void Dispose()
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
