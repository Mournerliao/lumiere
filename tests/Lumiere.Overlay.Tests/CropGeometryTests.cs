using Lumiere.Overlay.Crop;
using Windows.Foundation;
using Xunit;

namespace Lumiere.Overlay.Tests;

public sealed class CropGeometryTests
{
    [Fact]
    public void FromDrag_NormalizesNegativeDragAndClampsToPreviewBounds()
    {
        var bounds = new Rect(10, 20, 100, 80);

        var geometry = CropGeometry.FromDrag(
            new Point(120, 110),
            new Point(0, 10),
            bounds);

        Assert.True(geometry.IsValid);
        Assert.Equal(new Rect(10, 20, 100, 80), geometry.Region);
    }

    [Fact]
    public void FromDrag_RejectsTooSmallGeometry()
    {
        var bounds = new Rect(0, 0, 100, 80);

        var geometry = CropGeometry.FromDrag(
            new Point(10, 10),
            new Point(11, 11),
            bounds,
            minimumSize: 4);

        Assert.False(geometry.IsValid);
        Assert.True(geometry.IsEmpty);
    }

    [Fact]
    public void Empty_UsesZeroRectAndInvalidState()
    {
        var geometry = CropGeometry.Empty;

        Assert.True(geometry.IsEmpty);
        Assert.False(geometry.IsValid);
        Assert.Equal(new Rect(0, 0, 0, 0), geometry.Region);
    }
}
