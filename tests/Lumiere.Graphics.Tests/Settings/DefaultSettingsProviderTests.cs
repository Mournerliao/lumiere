using Lumiere.Graphics.Output;
using Lumiere.Settings;
using Xunit;

namespace Lumiere.Graphics.Tests.Settings;

public sealed class DefaultSettingsProviderTests
{
    [Fact]
    public void OutputTarget_ReturnsClipboard()
    {
        var provider = new DefaultSettingsProvider();

        Assert.Equal(OutputTarget.Clipboard, provider.OutputTarget);
    }

    [Fact]
    public void SavePath_ReturnsNull()
    {
        var provider = new DefaultSettingsProvider();

        Assert.Null(provider.SavePath);
    }

    [Fact]
    public void TimestampNaming_ReturnsTrue()
    {
        var provider = new DefaultSettingsProvider();

        Assert.True(provider.TimestampNaming);
    }

    [Fact]
    public void CopyAsImage_ReturnsTrue()
    {
        var provider = new DefaultSettingsProvider();

        Assert.True(provider.CopyAsImage);
    }

    [Fact]
    public void HdrAlertsEnabled_ReturnsTrue()
    {
        var provider = new DefaultSettingsProvider();

        Assert.True(provider.HdrAlertsEnabled);
    }

    [Fact]
    public void SetHdrAlertsEnabled_UpdatesInSessionPreference()
    {
        var provider = new DefaultSettingsProvider();

        provider.SetHdrAlertsEnabled(false);

        Assert.False(provider.HdrAlertsEnabled);
    }

    [Fact]
    public void SetHdrAlertsEnabled_PersistsPreferenceAcrossProviderInstances()
    {
        using var fixture = new SettingsFileFixture();
        var provider = new DefaultSettingsProvider(new LocalSettingsStore(fixture.SettingsPath));

        provider.SetHdrAlertsEnabled(false);
        var reloadedProvider = new DefaultSettingsProvider(new LocalSettingsStore(fixture.SettingsPath));

        Assert.False(reloadedProvider.HdrAlertsEnabled);
    }

    [Fact]
    public void SetOutputTarget_PersistsPreferenceAcrossProviderInstances()
    {
        using var fixture = new SettingsFileFixture();
        var provider = new DefaultSettingsProvider(new LocalSettingsStore(fixture.SettingsPath));

        provider.SetOutputTarget(OutputTarget.Both);
        var reloadedProvider = new DefaultSettingsProvider(new LocalSettingsStore(fixture.SettingsPath));

        Assert.Equal(OutputTarget.Both, provider.OutputTarget);
        Assert.Equal(OutputTarget.Both, reloadedProvider.OutputTarget);
    }

    [Fact]
    public void SetOutputTarget_RejectsUndefinedTarget()
    {
        var provider = new DefaultSettingsProvider();

        Assert.Throws<ArgumentOutOfRangeException>(() => provider.SetOutputTarget((OutputTarget)99));
    }

    [Fact]
    public void SetCopyAsImage_PersistsPreferenceAcrossProviderInstances()
    {
        using var fixture = new SettingsFileFixture();
        var provider = new DefaultSettingsProvider(new LocalSettingsStore(fixture.SettingsPath));

        provider.SetCopyAsImage(false);
        var reloadedProvider = new DefaultSettingsProvider(new LocalSettingsStore(fixture.SettingsPath));

        Assert.False(provider.CopyAsImage);
        Assert.False(reloadedProvider.CopyAsImage);
    }

    [Fact]
    public void Constructor_LoadsPersistedSettingsFromStore()
    {
        using var fixture = new SettingsFileFixture();
        var store = new LocalSettingsStore(fixture.SettingsPath);
        store.Save(new LocalSettingsSnapshot(
            SchemaVersion: LocalSettingsSnapshot.CurrentSchemaVersion,
            OutputTarget: OutputTarget.Folder,
            SavePath: @"D:\Captures",
            TimestampNaming: false,
            CopyAsImage: false,
            HdrAlertsEnabled: false,
            FullscreenShortcut: "Ctrl+Alt+F",
            RegionShortcut: "Ctrl+Alt+R",
            AfterCaptureBehavior: AfterCaptureBehavior.Open,
            ExportColorFormat: "sRGB"));

        var provider = new DefaultSettingsProvider(new LocalSettingsStore(fixture.SettingsPath));

        Assert.Equal(OutputTarget.Folder, provider.OutputTarget);
        Assert.Equal(@"D:\Captures", provider.SavePath);
        Assert.False(provider.TimestampNaming);
        Assert.False(provider.CopyAsImage);
        Assert.False(provider.HdrAlertsEnabled);
        Assert.Equal("Ctrl+Alt+F", provider.FullscreenShortcut);
        Assert.Equal("Ctrl+Alt+R", provider.RegionShortcut);
        Assert.Equal(AfterCaptureBehavior.Open, provider.AfterCaptureBehavior);
    }

    [Fact]
    public void FullscreenShortcut_ReturnsEmptyString()
    {
        var provider = new DefaultSettingsProvider();

        Assert.Equal(string.Empty, provider.FullscreenShortcut);
    }

    [Fact]
    public void RegionShortcut_ReturnsEmptyString()
    {
        var provider = new DefaultSettingsProvider();

        Assert.Equal(string.Empty, provider.RegionShortcut);
    }

    [Fact]
    public void ImplementsISettingsProvider()
    {
        var provider = new DefaultSettingsProvider();

        Assert.IsAssignableFrom<ISettingsProvider>(provider);
        Assert.IsAssignableFrom<IHdrAlertSettingsWriter>(provider);
        Assert.IsAssignableFrom<IOutputSettingsWriter>(provider);
    }

    [Fact]
    public void AllProperties_ReturnConsistentValues()
    {
        var provider = new DefaultSettingsProvider();

        // Verify all properties return expected MVP defaults
        Assert.Equal(OutputTarget.Clipboard, provider.OutputTarget);
        Assert.Null(provider.SavePath);
        Assert.True(provider.TimestampNaming);
        Assert.True(provider.CopyAsImage);
        Assert.True(provider.HdrAlertsEnabled);
        Assert.Equal(string.Empty, provider.FullscreenShortcut);
        Assert.Equal(string.Empty, provider.RegionShortcut);
        Assert.Equal(AfterCaptureBehavior.None, provider.AfterCaptureBehavior);
    }

    [Fact]
    public void OutputTarget_HasCorrectEnumValues()
    {
        Assert.Equal(0, (int)OutputTarget.Clipboard);
        Assert.Equal(1, (int)OutputTarget.Folder);
        Assert.Equal(2, (int)OutputTarget.Both);
    }

    private sealed class SettingsFileFixture : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "lumiere-provider-tests", Guid.NewGuid().ToString("N"));

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
