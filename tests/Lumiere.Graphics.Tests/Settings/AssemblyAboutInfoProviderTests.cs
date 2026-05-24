using System.Reflection;
using Lumiere.Settings;
using Xunit;

namespace Lumiere.Graphics.Tests.Settings;

public sealed class AssemblyAboutInfoProviderTests
{
    [Fact]
    public void Constructor_UsesAssemblyMetadata()
    {
        var provider = new AssemblyAboutInfoProvider(
            typeof(AssemblyAboutInfoProviderTests).Assembly,
            fallbackDescription: "Native Windows HDR-first capture and preview.");

        Assert.Equal("Lumiere.Graphics.Tests", provider.AppName);
        Assert.NotEqual("v0.1.0", provider.Version);
        Assert.Equal("Native Windows HDR-first capture and preview.", provider.Description);
    }

    [Fact]
    public void CreateFallback_UsesCentralSafeDefaults()
    {
        var provider = AssemblyAboutInfoProvider.CreateFallback();

        Assert.Equal("Lumiere", provider.AppName);
        Assert.Equal("1.0.0", provider.Version);
        Assert.Contains("HDR-first", provider.Description, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR-preserving", provider.Description, StringComparison.OrdinalIgnoreCase);
    }
}
