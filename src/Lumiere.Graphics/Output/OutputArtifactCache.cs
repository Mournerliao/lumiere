using System.Collections.Concurrent;

namespace Lumiere.Graphics.Output;

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
            artifacts.TryRemove(new KeyValuePair<OutputArtifactCacheKey, Lazy<Task<OutputEncodedArtifact>>>(
                key,
                lazyArtifact));
            throw;
        }
    }
}

public sealed record OutputArtifactCacheKey(
    OutputProfileKind ProfileKind,
    int X,
    int Y,
    int Width,
    int Height)
{
    public static OutputArtifactCacheKey Create(
        OutputProfileKind profileKind,
        CapturedFrameReadback readback) =>
        new(profileKind, 0, 0, readback.Width, readback.Height);

    public static OutputArtifactCacheKey Create(
        OutputProfileKind profileKind,
        CropPixelRect? cropRegion,
        int sourceWidth,
        int sourceHeight)
    {
        var x = cropRegion?.X ?? 0;
        var y = cropRegion?.Y ?? 0;
        var width = cropRegion?.Width ?? sourceWidth;
        var height = cropRegion?.Height ?? sourceHeight;
        return new OutputArtifactCacheKey(profileKind, x, y, width, height);
    }
}
