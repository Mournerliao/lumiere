using Lumiere.Graphics.Hdr;
using Lumiere.Graphics.Presentation;
using Vortice.DXGI;
using Xunit;

namespace Lumiere.Graphics.Tests.Hdr;

public sealed class HdrDisplayCapabilityTests
{
    [Fact]
    public void Unknown_HasUnknownStateAndNullColorSpace()
    {
        var capability = HdrDisplayCapability.Unknown();

        Assert.Equal(HdrDisplayState.Unknown, capability.State);
        Assert.Null(capability.DisplayColorSpace);
        Assert.Null(capability.DeviceName);
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
    }

    [Fact]
    public void Configure_WithInactiveDisplayCapability_ReturnsDegraded()
    {
        var controller = new FakeColorSpaceController(
            SwapChainColorSpaceSupportFlags.Present,
            setSucceeds: true);
        var displayCapability = new HdrDisplayCapability(
            HdrDisplayState.Inactive,
            ColorSpaceType.RgbFullG22NoneP709,
            "SDR Display");

        var status = SwapChainColorSpaceConfigurator.Configure(
            controller,
            HdrConstants.DxgiColorSpace,
            displayCapability);

        Assert.Equal(PreviewReadinessState.Degraded, status.State);
        Assert.Equal(PreviewReadinessStage.Presentation, status.Stage);
        Assert.Contains("Enable HDR", status.UserMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not active", status.TechnicalDetail ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Configure_WithActiveDisplayCapability_ReturnsInitializing()
    {
        var controller = new FakeColorSpaceController(
            SwapChainColorSpaceSupportFlags.Present,
            setSucceeds: true);
        var displayCapability = new HdrDisplayCapability(
            HdrDisplayState.Active,
            ColorSpaceType.RgbFullG2084NoneP2020,
            "HDR Display");

        var status = SwapChainColorSpaceConfigurator.Configure(
            controller,
            HdrConstants.DxgiColorSpace,
            displayCapability);

        Assert.Equal(PreviewReadinessState.Initializing, status.State);
    }

    [Fact]
    public void Configure_WithUnknownDisplayCapability_FallsBackToSwapChainCheck()
    {
        var controller = new FakeColorSpaceController(
            SwapChainColorSpaceSupportFlags.Present,
            setSucceeds: true);
        var displayCapability = HdrDisplayCapability.Unknown();

        var status = SwapChainColorSpaceConfigurator.Configure(
            controller,
            HdrConstants.DxgiColorSpace,
            displayCapability);

        Assert.Equal(PreviewReadinessState.Initializing, status.State);
    }

    [Fact]
    public void Configure_WithNullDisplayCapability_FallsBackToSwapChainCheck()
    {
        var controller = new FakeColorSpaceController(
            SwapChainColorSpaceSupportFlags.Present,
            setSucceeds: true);

        var status = SwapChainColorSpaceConfigurator.Configure(
            controller,
            HdrConstants.DxgiColorSpace,
            null);

        Assert.Equal(PreviewReadinessState.Initializing, status.State);
    }

    [Fact]
    public void Configure_UnsupportedSwapChain_StillReturnsDegradedRegardlessOfDisplayCapability()
    {
        var controller = new FakeColorSpaceController(
            SwapChainColorSpaceSupportFlags.None,
            setSucceeds: true);
        var displayCapability = new HdrDisplayCapability(
            HdrDisplayState.Active,
            ColorSpaceType.RgbFullG2084NoneP2020,
            "HDR Display");

        var status = SwapChainColorSpaceConfigurator.Configure(
            controller,
            HdrConstants.DxgiColorSpace,
            displayCapability);

        Assert.Equal(PreviewReadinessState.Degraded, status.State);
    }

    private sealed class FakeColorSpaceController : ISwapChainColorSpaceController
    {
        private readonly SwapChainColorSpaceSupportFlags supportFlags;
        private readonly bool setSucceeds;

        public FakeColorSpaceController(
            SwapChainColorSpaceSupportFlags supportFlags,
            bool setSucceeds)
        {
            this.supportFlags = supportFlags;
            this.setSucceeds = setSucceeds;
        }

        public SwapChainColorSpaceSupportFlags CheckColorSpaceSupport(ColorSpaceType colorSpace) =>
            supportFlags;

        public void SetColorSpace1(ColorSpaceType colorSpace)
        {
            if (!setSucceeds)
            {
                throw new SwapChainPresentationException(
                    "IDXGISwapChain3.SetColorSpace1",
                    unchecked((int)0x887A0001),
                    "SetColorSpace1 rejected the scRGB color space.");
            }
        }
    }
}
