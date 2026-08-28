using Lumiere.Windows.Capture;
using Windows.Graphics;
using Xunit;

namespace Lumiere.Windows.Capture.Tests;

public sealed class RegionCropResolverTests
{
    [Fact]
    public void ResolvesTargetLogicalGeometryWithOutwardPixelAlignment()
    {
        var captureTarget = CaptureTarget.CreateForTest(
            new SizeInt32 { Width = 3840, Height = 2160 },
            "scaled display",
            CaptureTargetKind.Display);
        var target = WindowsTargetCapability.CreateForTest(
            WindowsTargetHdrState.Active,
            new WindowsTargetLogicalSize(2560, 1440),
            captureTarget);

        var crop = RegionCropResolver.Resolve(
            new WindowsRegionGeometry(10.25, 20.5, 100.5, 50.25),
            target,
            captureTarget);

        Assert.Equal(15, crop.X);
        Assert.Equal(30, crop.Y);
        Assert.Equal(152, crop.Width);
        Assert.Equal(77, crop.Height);
    }

    [Fact]
    public void RejectsGeometryOutsideIssuedLogicalTarget()
    {
        var captureTarget = CaptureTarget.CreateForTest(
            new SizeInt32 { Width = 1920, Height = 1080 },
            "display",
            CaptureTargetKind.Display);
        var target = WindowsTargetCapability.CreateForTest(
            WindowsTargetHdrState.Inactive,
            new WindowsTargetLogicalSize(1920, 1080),
            captureTarget);

        Assert.Throws<ArgumentOutOfRangeException>(() => RegionCropResolver.Resolve(
            new WindowsRegionGeometry(1900, 100, 40, 40),
            target,
            captureTarget));
    }
}
