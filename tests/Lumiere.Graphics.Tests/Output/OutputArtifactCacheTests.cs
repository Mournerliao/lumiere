using Lumiere.Graphics.Output;
using Xunit;

namespace Lumiere.Graphics.Tests.Output;

public sealed class OutputArtifactCacheTests
{
    [Fact]
    public async Task GetOrCreateAsync_ReusesArtifactForSameKey()
    {
        var calls = 0;
        var cache = new OutputArtifactCache();
        var key = new OutputArtifactCacheKey(OutputProfileKind.SrgbCompatibilityPng, 0, 0, 10, 10);

        var first = await cache.GetOrCreateAsync(key, CreateArtifactAsync);
        var second = await cache.GetOrCreateAsync(key, CreateArtifactAsync);

        Assert.Same(first, second);
        Assert.Equal(1, calls);

        Task<OutputEncodedArtifact> CreateArtifactAsync()
        {
            calls++;
            return Task.FromResult(new OutputEncodedArtifact(
                [1, 2, 3],
                "png",
                OutputProfileContract.SrgbCompatibilityPng));
        }
    }

    [Fact]
    public async Task GetOrCreateAsync_DoesNotReuseFailedArtifactFactory()
    {
        var calls = 0;
        var cache = new OutputArtifactCache();
        var key = new OutputArtifactCacheKey(OutputProfileKind.SrgbCompatibilityPng, 0, 0, 10, 10);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cache.GetOrCreateAsync(key, () =>
            {
                calls++;
                throw new InvalidOperationException("Encoding failed.");
            }));

        var artifact = await cache.GetOrCreateAsync(key, () =>
        {
            calls++;
            return Task.FromResult(new OutputEncodedArtifact(
                [1, 2, 3],
                "png",
                OutputProfileContract.SrgbCompatibilityPng));
        });

        Assert.Equal("png", artifact.NormalizedFileExtension);
        Assert.Equal(2, calls);
    }
}
