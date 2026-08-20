using System.Collections.Concurrent;

namespace Lumiere.Windows.Graphics.Output;

/// <summary>
/// Reuses the single sRGB Visual Match artifact across clipboard and folder delivery.
/// </summary>
public sealed class OutputArtifactCache
{
    private readonly ConcurrentDictionary<OutputArtifactCacheKey, Lazy<Task<OutputEncodedArtifact>>> artifacts = new();

    public async Task<OutputEncodedArtifact> GetOrCreateAsync(
        OutputArtifactCacheKey key,
        Func<Task<OutputEncodedArtifact>> createArtifact)
    {
        ArgumentNullException.ThrowIfNull(createArtifact);
        var lazyArtifact = artifacts.GetOrAdd(
            key,
            _ => new Lazy<Task<OutputEncodedArtifact>>(
                createArtifact,
                LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            return await lazyArtifact.Value;
        }
        catch
        {
            artifacts.TryRemove(
                new KeyValuePair<OutputArtifactCacheKey, Lazy<Task<OutputEncodedArtifact>>>(
                    key,
                    lazyArtifact));
            throw;
        }
    }
}

public sealed record OutputArtifactCacheKey(int X, int Y, int Width, int Height)
{
    public static OutputArtifactCacheKey Create(
        CropPixelRect? cropRegion,
        int sourceWidth,
        int sourceHeight) =>
        new(
            cropRegion?.X ?? 0,
            cropRegion?.Y ?? 0,
            cropRegion?.Width ?? sourceWidth,
            cropRegion?.Height ?? sourceHeight);
}
