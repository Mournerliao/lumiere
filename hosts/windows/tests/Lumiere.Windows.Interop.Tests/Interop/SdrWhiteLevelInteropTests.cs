using Xunit;

namespace Lumiere.Windows.Interop.Tests;

public sealed class SdrWhiteLevelInteropTests
{
    [Theory]
    [InlineData(1000u, 80f)]
    [InlineData(2000u, 160f)]
    [InlineData(3000u, 240f)]
    public void ConvertsDisplayConfigMultiplierToNits(uint rawValue, float expectedNits)
    {
        Assert.Equal(expectedNits, SdrWhiteLevelInterop.ConvertRawSdrWhiteLevelToNits(rawValue));
    }

    [Fact]
    public void TreatsZeroWhiteLevelAsUnavailable()
    {
        Assert.Null(SdrWhiteLevelInterop.ConvertRawSdrWhiteLevelToNits(0));
    }
}
