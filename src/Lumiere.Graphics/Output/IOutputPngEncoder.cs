using Lumiere.Graphics.Presentation;

namespace Lumiere.Graphics.Output;

public interface IOutputPngEncoder
{
    Task<byte[]> EncodePngAsync(
        CapturedFrameTexture texture,
        CropPixelRect? cropRegion,
        CancellationToken cancellationToken = default);
}
