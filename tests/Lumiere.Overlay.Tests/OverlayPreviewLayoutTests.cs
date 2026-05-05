using Xunit;

namespace Lumiere.Overlay.Tests;

public sealed class OverlayPreviewLayoutTests
{
    [Theory]
    [InlineData(1920, 1080)]
    [InlineData(1280, 720)]
    [InlineData(0, 0)]
    public void FillSurface_KeepsPreviewAtFullOverlayBounds(double width, double height)
    {
        var layout = OverlayPreviewLayout.FillSurface(width, height);

        Assert.Equal(0, layout.PreviewBounds.X);
        Assert.Equal(0, layout.PreviewBounds.Y);
        Assert.Equal(Math.Max(1, width), layout.PreviewBounds.Width);
        Assert.Equal(Math.Max(1, height), layout.PreviewBounds.Height);
    }
}
