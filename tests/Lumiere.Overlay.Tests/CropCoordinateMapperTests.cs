using Lumiere.Graphics.Output;
using Lumiere.Overlay.Crop;
using Windows.Foundation;
using Xunit;

namespace Lumiere.Overlay.Tests;

public sealed class CropCoordinateMapperTests
{
    [Fact]
    public void MapToCapturePixels_ScalesFullPreviewBounds()
    {
        var result = CropCoordinateMapper.MapToCapturePixels(
            new Rect(10, 20, 30, 40),
            new Rect(0, 0, 100, 100),
            new CaptureFrameSize(1000, 500));

        Assert.Equal(new CropPixelRect(100, 100, 300, 200), result);
    }

    [Fact]
    public void MapToCapturePixels_SubtractsNonZeroPreviewOrigin()
    {
        var result = CropCoordinateMapper.MapToCapturePixels(
            new Rect(30, 50, 40, 20),
            new Rect(10, 20, 100, 80),
            new CaptureFrameSize(1000, 800));

        Assert.Equal(new CropPixelRect(200, 300, 400, 200), result);
    }

    [Fact]
    public void MapToCapturePixels_RoundsOutwardToPreserveEdges()
    {
        var result = CropCoordinateMapper.MapToCapturePixels(
            new Rect(1.26, 1.26, 2.18, 2.18),
            new Rect(0, 0, 10, 10),
            new CaptureFrameSize(100, 100));

        Assert.Equal(new CropPixelRect(12, 12, 23, 23), result);
    }

    [Fact]
    public void MapToCapturePixels_ClampsToCaptureExtent()
    {
        var result = CropCoordinateMapper.MapToCapturePixels(
            new Rect(-10, 80, 150, 40),
            new Rect(0, 0, 100, 100),
            new CaptureFrameSize(1000, 500));

        Assert.Equal(new CropPixelRect(0, 400, 1000, 100), result);
    }

    [Fact]
    public void MapToCapturePixels_RejectsInvalidFrameSize()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CropCoordinateMapper.MapToCapturePixels(
                new Rect(0, 0, 10, 10),
                new Rect(0, 0, 10, 10),
                new CaptureFrameSize(0, 100)));
    }
}
