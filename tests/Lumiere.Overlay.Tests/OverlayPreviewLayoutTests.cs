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

    [Fact]
    public void FitFrameToSurface_LetterboxesInsteadOfStretchingWhenAspectRatioDiffers()
    {
        var layout = OverlayPreviewLayout.FitFrameToSurface(
            frameWidth: 3840,
            frameHeight: 2160,
            availableWidth: 1000,
            availableHeight: 1000);

        Assert.Equal(0, layout.PreviewBounds.X, precision: 10);
        Assert.Equal(218.75, layout.PreviewBounds.Y, precision: 10);
        Assert.Equal(1000, layout.PreviewBounds.Width, precision: 10);
        Assert.Equal(562.5, layout.PreviewBounds.Height, precision: 10);
    }

    [Fact]
    public void FitFrameToSurface_PillarboxesInsteadOfStretchingWhenSurfaceIsWide()
    {
        var layout = OverlayPreviewLayout.FitFrameToSurface(
            frameWidth: 1000,
            frameHeight: 1000,
            availableWidth: 1920,
            availableHeight: 1080);

        Assert.Equal(420, layout.PreviewBounds.X, precision: 10);
        Assert.Equal(0, layout.PreviewBounds.Y, precision: 10);
        Assert.Equal(1080, layout.PreviewBounds.Width, precision: 10);
        Assert.Equal(1080, layout.PreviewBounds.Height, precision: 10);
    }
}
