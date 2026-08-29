using Lumiere.Windows.Graphics.Hdr;
using Vortice.DXGI;
using Xunit;

namespace Lumiere.Windows.Graphics.Tests.Hdr;

public sealed class HdrDisplayCapabilityTests
{
    [Fact]
    public void Unknown_HasUnknownStateAndNullColorSpace()
    {
        var capability = HdrDisplayCapability.Unknown();

        Assert.Equal(HdrDisplayState.Unknown, capability.State);
        Assert.Null(capability.DisplayColorSpace);
        Assert.Null(capability.DeviceName);
        Assert.Equal(HdrDisplayMatchKind.NotMatched, capability.MatchKind);
        Assert.False(capability.IsHdrActive);
    }

    [Fact]
    public void Active_IsHdrActiveReturnsTrue()
    {
        var capability = new HdrDisplayCapability(
            HdrDisplayState.Active,
            ColorSpaceType.RgbFullG2084NoneP2020,
            "Test Display");

        Assert.True(capability.IsHdrActive);
    }

    [Fact]
    public void Inactive_IsHdrActiveReturnsFalse()
    {
        var capability = new HdrDisplayCapability(
            HdrDisplayState.Inactive,
            ColorSpaceType.RgbFullG22NoneP709,
            "Test Display");

        Assert.False(capability.IsHdrActive);
    }

    [Theory]
    [InlineData(ColorSpaceType.RgbFullG2084NoneP2020, true)]
    [InlineData(ColorSpaceType.YcbcrStudioG2084LeftP2020, true)]
    [InlineData(ColorSpaceType.YcbcrStudioG2084TopLeftP2020, true)]
    [InlineData(ColorSpaceType.RgbStudioG2084NoneP2020, true)]
    [InlineData(ColorSpaceType.RgbFullG22NoneP709, false)]
    [InlineData(ColorSpaceType.RgbFullG10NoneP709, false)]
    public void HdrDisplayState_MapsCorrectlyFromColorSpace(ColorSpaceType colorSpace, bool expectedHdr)
    {
        var state = expectedHdr ? HdrDisplayState.Active : HdrDisplayState.Inactive;
        var capability = new HdrDisplayCapability(state, colorSpace, "Display");

        Assert.Equal(expectedHdr, capability.IsHdrActive);
        Assert.Equal(HdrDisplayMatchKind.Unspecified, capability.MatchKind);
    }

    [Fact]
    public void SelectForTarget_PrefersDisplayNameMatch()
    {
        var capability = HdrDisplayCapability.SelectForTarget(
            [
                new HdrDisplayOutputSnapshot(
                    DeviceName: "SDR Display",
                    Left: 0,
                    Top: 0,
                    Width: 3840,
                    Height: 2160,
                    ColorSpace: ColorSpaceType.RgbFullG22NoneP709),
                new HdrDisplayOutputSnapshot(
                    DeviceName: "HDR Display",
                    Left: 3840,
                    Top: 0,
                    Width: 3840,
                    Height: 2160,
                    ColorSpace: ColorSpaceType.RgbFullG2084NoneP2020),
            ],
            targetDisplayName: "HDR Display",
            targetWidth: 3840,
            targetHeight: 2160);

        Assert.Equal(HdrDisplayState.Active, capability.State);
        Assert.Equal("HDR Display", capability.DeviceName);
        Assert.Equal(HdrDisplayMatchKind.DeviceName, capability.MatchKind);
    }

    [Fact]
    public void SelectForTarget_PropagatesMatchedDisplaySdrWhiteLevel()
    {
        var capability = HdrDisplayCapability.SelectForTarget(
            [
                new HdrDisplayOutputSnapshot(
                    DeviceName: "HDR Display",
                    Left: 0,
                    Top: 0,
                    Width: 3840,
                    Height: 2160,
                    ColorSpace: ColorSpaceType.RgbFullG2084NoneP2020,
                    SdrWhiteLevelInNits: 240f),
            ],
            targetDisplayName: "HDR Display",
            targetWidth: 3840,
            targetHeight: 2160);

        Assert.True(capability.IsHdrActive);
        Assert.Equal(240f, capability.SdrWhiteLevelInNits);
    }

    [Fact]
    public void SelectForTarget_UsesSizeWhenNameIsNotAvailable()
    {
        var capability = HdrDisplayCapability.SelectForTarget(
            [
                new HdrDisplayOutputSnapshot(
                    DeviceName: "Primary",
                    Left: 0,
                    Top: 0,
                    Width: 1920,
                    Height: 1080,
                    ColorSpace: ColorSpaceType.RgbFullG22NoneP709),
                new HdrDisplayOutputSnapshot(
                    DeviceName: "Reference HDR",
                    Left: 1920,
                    Top: 0,
                    Width: 2560,
                    Height: 1440,
                    ColorSpace: ColorSpaceType.RgbFullG2084NoneP2020),
            ],
            targetDisplayName: null,
            targetWidth: 2560,
            targetHeight: 1440);

        Assert.Equal(HdrDisplayState.Active, capability.State);
        Assert.Equal("Reference HDR", capability.DeviceName);
        Assert.Equal(HdrDisplayMatchKind.Size, capability.MatchKind);
    }

    [Fact]
    public void SelectForTarget_ReturnsUnknownWhenTargetCannotBeMatched()
    {
        var capability = HdrDisplayCapability.SelectForTarget(
            [
                new HdrDisplayOutputSnapshot(
                    DeviceName: "Primary",
                    Left: 0,
                    Top: 0,
                    Width: 1920,
                    Height: 1080,
                    ColorSpace: ColorSpaceType.RgbFullG22NoneP709),
            ],
            targetDisplayName: "Missing Display",
            targetWidth: 2560,
            targetHeight: 1440);

        Assert.Equal(HdrDisplayState.Unknown, capability.State);
        Assert.Null(capability.DisplayColorSpace);
        Assert.Null(capability.DeviceName);
        Assert.Equal(HdrDisplayMatchKind.NotMatched, capability.MatchKind);
    }

    [Fact]
    public void SelectForTarget_ReturnsUnknownWhenSizeMatchIsAmbiguous()
    {
        var capability = HdrDisplayCapability.SelectForTarget(
            [
                new HdrDisplayOutputSnapshot(
                    DeviceName: "SDR Display",
                    Left: 0,
                    Top: 0,
                    Width: 3840,
                    Height: 2160,
                    ColorSpace: ColorSpaceType.RgbFullG22NoneP709),
                new HdrDisplayOutputSnapshot(
                    DeviceName: "HDR Display",
                    Left: 3840,
                    Top: 0,
                    Width: 3840,
                    Height: 2160,
                    ColorSpace: ColorSpaceType.RgbFullG2084NoneP2020),
            ],
            targetDisplayName: null,
            targetWidth: 3840,
            targetHeight: 2160);

        Assert.Equal(HdrDisplayState.Unknown, capability.State);
        Assert.Null(capability.DisplayColorSpace);
        Assert.Null(capability.DeviceName);
        Assert.Equal(HdrDisplayMatchKind.NotMatched, capability.MatchKind);
    }

    [Fact]
    public void SelectForTarget_UsesDesktopBoundsWhenDisplayNameIsUnavailableAndSizeIsAmbiguous()
    {
        var capability = HdrDisplayCapability.SelectForTarget(
            [
                new HdrDisplayOutputSnapshot(
                    DeviceName: "SDR Display",
                    Left: 0,
                    Top: 0,
                    Width: 3840,
                    Height: 2160,
                    ColorSpace: ColorSpaceType.RgbFullG22NoneP709),
                new HdrDisplayOutputSnapshot(
                    DeviceName: "HDR Display",
                    Left: 3840,
                    Top: 0,
                    Width: 3840,
                    Height: 2160,
                    ColorSpace: ColorSpaceType.RgbFullG2084NoneP2020),
            ],
            targetDisplayName: null,
            targetLeft: 3840,
            targetTop: 0,
            targetWidth: 3840,
            targetHeight: 2160);

        Assert.Equal(HdrDisplayState.Active, capability.State);
        Assert.Equal("HDR Display", capability.DeviceName);
        Assert.Equal(HdrDisplayMatchKind.DesktopBounds, capability.MatchKind);
    }

}
