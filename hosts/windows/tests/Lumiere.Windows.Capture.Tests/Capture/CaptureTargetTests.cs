using Lumiere.Windows.Capture;
using Windows.Graphics;
using Xunit;

namespace Lumiere.Windows.Capture.Tests;

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

    [Fact]
    public void CreateForTestCanAttachDisplayOutputIdentityWithoutNativeHandle()
    {
        var identity = new DisplayOutputIdentity("\\\\.\\DISPLAY2", 3840, 2160);

        var target = CaptureTarget.CreateForTest(
            new SizeInt32 { Width = 3840, Height = 2160 },
            "HDR Display",
            CaptureTargetKind.Display,
            identity);

        Assert.Equal(identity, target.DisplayIdentity);
        Assert.Equal("\\\\.\\DISPLAY2", target.DisplayIdentity?.DeviceName);
        Assert.Equal(3840, target.DisplayIdentity?.Width);
        Assert.Equal(2160, target.DisplayIdentity?.Height);
    }

    [Fact]
    public void CreateForTestDoesNotInventDisplayIdentityForWindowTargets()
    {
        var target = CaptureTarget.CreateForTest(
            new SizeInt32 { Width = 1280, Height = 720 },
            "Window",
            CaptureTargetKind.Window);

        Assert.Null(target.DisplayIdentity);
    }

    [Fact]
    public void WithSizePreservesDisplayIdentityNameAndUpdatesIdentitySize()
    {
        var target = CaptureTarget.CreateForTest(
            new SizeInt32 { Width = 1920, Height = 1080 },
            "HDR Display",
            CaptureTargetKind.Display,
            new DisplayOutputIdentity("\\\\.\\DISPLAY1", 1920, 1080));

        var resized = target.WithSize(new SizeInt32 { Width = 2560, Height = 1440 });

        Assert.Equal("\\\\.\\DISPLAY1", resized.DisplayIdentity?.DeviceName);
        Assert.Equal(2560, resized.DisplayIdentity?.Width);
        Assert.Equal(1440, resized.DisplayIdentity?.Height);
    }

    [Fact]
    public void DisplayIdentityCanCarryDesktopBoundsWithoutNativeHandle()
    {
        var identity = new DisplayOutputIdentity("\\\\.\\DISPLAY2", left: 3840, top: 0, width: 3840, height: 2160);

        Assert.Equal("\\\\.\\DISPLAY2", identity.DeviceName);
        Assert.Equal(3840, identity.Left);
        Assert.Equal(0, identity.Top);
        Assert.Equal(3840, identity.Width);
        Assert.Equal(2160, identity.Height);
    }

    [Fact]
    public void WithSizePreservesDisplayIdentityBoundsAndUpdatesIdentitySize()
    {
        var target = CaptureTarget.CreateForTest(
            new SizeInt32 { Width = 3840, Height = 2160 },
            "HDR Display",
            CaptureTargetKind.Display,
            new DisplayOutputIdentity("\\\\.\\DISPLAY2", left: 3840, top: 0, width: 3840, height: 2160));

        var resized = target.WithSize(new SizeInt32 { Width = 2560, Height = 1440 });

        Assert.Equal(3840, resized.DisplayIdentity?.Left);
        Assert.Equal(0, resized.DisplayIdentity?.Top);
        Assert.Equal(2560, resized.DisplayIdentity?.Width);
        Assert.Equal(1440, resized.DisplayIdentity?.Height);
    }
}
