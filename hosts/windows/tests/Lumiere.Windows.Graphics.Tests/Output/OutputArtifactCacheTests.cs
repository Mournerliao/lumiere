using Lumiere.Windows.Graphics.Output;
using Xunit;

namespace Lumiere.Windows.Graphics.Tests.Output;

public sealed class OutputArtifactCacheTests
{
    [Fact]
    public async Task ReusesOneVisualMatchArtifactForTheSameCrop()
    {
        var cache = new OutputArtifactCache();
        var key = new OutputArtifactCacheKey(0, 0, 10, 10);
        var encodeCount = 0;

        async Task<OutputEncodedArtifact> EncodeAsync()
        {
            encodeCount++;
            await Task.Yield();
            return new OutputEncodedArtifact([1, 2, 3], "png");
        }

        var first = await cache.GetOrCreateAsync(key, EncodeAsync);
        var second = await cache.GetOrCreateAsync(key, EncodeAsync);

        Assert.Same(first, second);
        Assert.Equal(1, encodeCount);
        Assert.Equal("srgb-visual-match", OutputEncodedArtifact.Profile);
    }
}
