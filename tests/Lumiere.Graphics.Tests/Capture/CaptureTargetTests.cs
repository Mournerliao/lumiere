using Lumiere.Capture;
using Windows.Graphics;
using Xunit;

namespace Lumiere.Graphics.Tests.Capture;

public sealed class CaptureTargetTests
{
    [Fact]
    public void CreateForTestPopulatesDisplayNameSizeAndKind()
    {
        var target = CaptureTarget.CreateForTest(
            new SizeInt32 { Width = 1920, Height = 1080 },
            "Test Monitor");

        Assert.Equal("Test Monitor", target.DisplayName);
        Assert.Equal(1920, target.Size.Width);
        Assert.Equal(1080, target.Size.Height);
        Assert.Equal(CaptureTargetKind.Unknown, target.Kind);
        Assert.False(target.HasCaptureItem);
    }

    [Fact]
    public void CreateForTestItemThrowsClearException()
    {
        var target = CaptureTarget.CreateForTest(
            new SizeInt32 { Width = 1920, Height = 1080 },
            "Test Monitor");

        var exception = Assert.Throws<InvalidOperationException>(() => target.Item);

        Assert.Contains("GraphicsCaptureItem", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateForTestRejectsZeroWidthTarget()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CaptureTarget.CreateForTest(
                new SizeInt32 { Width = 0, Height = 1080 },
                "Zero Width"));

        Assert.Contains("size", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateForTestRejectsZeroHeightTarget()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CaptureTarget.CreateForTest(
                new SizeInt32 { Width = 1920, Height = 0 },
                "Zero Height"));

        Assert.Contains("size", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateForTestRejectsNegativeWidthTarget()
    {
        Assert.Throws<ArgumentException>(
            () => CaptureTarget.CreateForTest(
                new SizeInt32 { Width = -1, Height = 1080 },
                "Negative Width"));
    }

    [Fact]
    public void CreateForTestRejectsNegativeHeightTarget()
    {
        Assert.Throws<ArgumentException>(
            () => CaptureTarget.CreateForTest(
                new SizeInt32 { Width = 1920, Height = -1 },
                "Negative Height"));
    }

    [Theory]
    [InlineData(16_385, 1080)]
    [InlineData(1920, 16_385)]
    public void CreateForTestRejectsTargetsBeyondD3D11TextureLimit(int width, int height)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CaptureTarget.CreateForTest(
                new SizeInt32 { Width = width, Height = height },
                "Oversized Target"));

        Assert.Contains("D3D11 texture limit", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateForTestDefaultsEmptyDisplayName()
    {
        var target = CaptureTarget.CreateForTest(
            new SizeInt32 { Width = 800, Height = 600 },
            "");

        Assert.Equal("Capture target", target.DisplayName);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(16384, 16384)]
    [InlineData(3840, 2160)]
    [InlineData(7680, 4320)]
    public void CreateForTestAcceptsValidPositiveSize(int width, int height)
    {
        var target = CaptureTarget.CreateForTest(
            new SizeInt32 { Width = width, Height = height },
            "Valid Monitor");

        Assert.Equal(width, target.Size.Width);
        Assert.Equal(height, target.Size.Height);
    }

    [Fact]
    public void CreateForTestStoresDisplayAndWindowKind()
    {
        var displayTarget = CaptureTarget.CreateForTest(
            new SizeInt32 { Width = 1920, Height = 1080 },
            "Display",
            CaptureTargetKind.Display);

        var windowTarget = CaptureTarget.CreateForTest(
            new SizeInt32 { Width = 800, Height = 600 },
            "Window",
            CaptureTargetKind.Window);

        Assert.Equal(CaptureTargetKind.Display, displayTarget.Kind);
        Assert.Equal(CaptureTargetKind.Window, windowTarget.Kind);
    }
}
