using Lumiere.App;
using Xunit;

namespace Lumiere.Graphics.Tests.App;

public sealed class TargetAppVersionPrefillProviderTests
{
    [Fact]
    public void TryGetVersion_MapsKnownAppsToTheirPackageOrExecutableResolvers()
    {
        var requestedFamilies = new List<string>();
        var requestedExecutables = new List<string>();
        var provider = new WindowsTargetAppVersionPrefillProvider(
            familyName =>
            {
                requestedFamilies.Add(familyName);
                return familyName switch
                {
                    "Microsoft.Paint_8wekyb3d8bbwe" => "11.2504.451.0",
                    "Microsoft.Windows.Photos_8wekyb3d8bbwe" => "2026.11040.12001.0",
                    _ => null,
                };
            },
            executableName =>
            {
                requestedExecutables.Add(executableName);
                return executableName == "msedge.exe"
                    ? "138.0.7204.101"
                    : null;
            });

        Assert.Equal("11.2504.451.0", provider.TryGetVersion("Microsoft Paint"));
        Assert.Equal("2026.11040.12001.0", provider.TryGetVersion("Windows Photos"));
        Assert.Equal("138.0.7204.101", provider.TryGetVersion("Microsoft Edge"));
        Assert.Equal(
            ["Microsoft.Paint_8wekyb3d8bbwe", "Microsoft.Windows.Photos_8wekyb3d8bbwe"],
            requestedFamilies);
        Assert.Equal(["msedge.exe"], requestedExecutables);
    }
}
