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
    }

    [Fact]
    public void OutputTarget_HasCorrectEnumValues()
    {
        Assert.Equal(0, (int)OutputTarget.Clipboard);
        Assert.Equal(1, (int)OutputTarget.Folder);
        Assert.Equal(2, (int)OutputTarget.Both);
    }
}
