using Vortice.Direct3D11;

namespace Lumiere.Graphics.Presentation;

public sealed class CapturedFrameTexture : IDisposable
{
    private bool disposed;

    public CapturedFrameTexture(
        ID3D11Texture2D? texture,
        int width,
        int height,
        string sourceDescription)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width),
                width,
                "A captured frame texture requires a positive width.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(height),
                height,
                "A captured frame texture requires a positive height.");
        }

        Texture = texture;
        Width = width;
        Height = height;
        SourceDescription = string.IsNullOrWhiteSpace(sourceDescription)
            ? "Captured frame"
            : sourceDescription;
    }

    public ID3D11Texture2D? Texture { get; }

    public int Width { get; }

    public int Height { get; }

    public string SourceDescription { get; }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        Texture?.Dispose();
        disposed = true;
    }
}
