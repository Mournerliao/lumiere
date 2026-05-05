using Lumiere.Capture;
using Lumiere.Overlay.Windowing;
using Windows.Graphics;
using Xunit;

namespace Lumiere.Overlay.Tests;

public sealed class OverlayPlacementRequestTests
{
    [Fact]
    public void Constructor_CarriesTargetIdentityForWindowingBoundary()
    {
        var target = CaptureTarget.CreateForTest(
            new SizeInt32 { Width = 3840, Height = 2160 },
            "HDR Display",
            CaptureTargetKind.Display);

        var request = new OverlayPlacementRequest(
            target.Size,
            target.Kind is CaptureTargetKind.Display,
            target.DisplayName);

        Assert.Equal(target.Size, request.TargetSize);
        Assert.True(request.IsDisplayTarget);
        Assert.Equal("HDR Display", request.TargetDisplayName);
    }

    [Fact]
    public void SelectOverlayBounds_UsesDisplayAreaMatchingCaptureDisplaySize()
    {
        var target = CaptureTarget.CreateForTest(
            new SizeInt32 { Width = 3840, Height = 2160 },
            "HDR Display",
            CaptureTargetKind.Display);
        var request = new OverlayPlacementRequest(
            target.Size,
            target.Kind is CaptureTargetKind.Display,
            target.DisplayName);
        var fallback = new RectInt32 { X = 0, Y = 0, Width = 1920, Height = 1080 };
        var targetDisplay = new RectInt32 { X = 1920, Y = 0, Width = 3840, Height = 2160 };

        var selected = OverlayWindowPresenter.SelectOverlayBounds(
            request,
            [fallback, targetDisplay],
            fallback);

        Assert.Equal(targetDisplay, selected);
    }

    [Fact]
    public void SelectOverlayBounds_FallsBackWhenTargetDisplaySizeIsUnavailable()
    {
        var target = CaptureTarget.CreateForTest(
            new SizeInt32 { Width = 2560, Height = 1440 },
            "HDR Display",
            CaptureTargetKind.Display);
        var request = new OverlayPlacementRequest(
            target.Size,
            target.Kind is CaptureTargetKind.Display,
            target.DisplayName);
        var fallback = new RectInt32 { X = 0, Y = 0, Width = 1920, Height = 1080 };

        var selected = OverlayWindowPresenter.SelectOverlayBounds(
            request,
            [new RectInt32 { X = 1920, Y = 0, Width = 3840, Height = 2160 }],
            fallback);

        Assert.Equal(fallback, selected);
    }
}
