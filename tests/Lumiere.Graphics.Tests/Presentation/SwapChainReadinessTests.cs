using Lumiere.Graphics.Hdr;
using Lumiere.Graphics.Presentation;
using Lumiere.Infrastructure.Interop;
using Vortice.DXGI;
using Xunit;

namespace Lumiere.Graphics.Tests.Presentation;

public sealed class SwapChainReadinessTests
{
    [Fact]
    public void SuccessfulColorSpaceConfigurationReportsPresentationEvidenceWithoutReady()
    {
        var controller = new FakeColorSpaceController(
            SwapChainColorSpaceSupportFlags.Present,
            setSucceeds: true);

        var status = SwapChainColorSpaceConfigurator.Configure(
            controller,
            HdrConstants.DxgiColorSpace);

        Assert.Equal(PreviewReadinessState.Initializing, status.State);
        Assert.Equal(PreviewReadinessStage.Presentation, status.Stage);
        Assert.Contains("SetColorSpace1", status.TechnicalDetail ?? string.Empty, StringComparison.Ordinal);
        Assert.Equal(HdrConstants.DxgiColorSpace, controller.CheckedColorSpace);
        Assert.Equal(HdrConstants.DxgiColorSpace, controller.SetColorSpace);
    }

    [Fact]
    public void UnsupportedColorSpaceReportsDegradedPresentationStatus()
    {
        var controller = new FakeColorSpaceController(
            SwapChainColorSpaceSupportFlags.None,
            setSucceeds: true);

        var status = SwapChainColorSpaceConfigurator.Configure(
            controller,
            HdrConstants.DxgiColorSpace);

        Assert.Equal(PreviewReadinessState.Degraded, status.State);
        Assert.Equal(PreviewReadinessStage.Presentation, status.Stage);
        Assert.True(status.RequiresUserAttention);
        Assert.Contains("CheckColorSpaceSupport", status.TechnicalDetail ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void SetColorSpaceFailureReportsFailedPresentationStatus()
    {
        var controller = new FakeColorSpaceController(
            SwapChainColorSpaceSupportFlags.Present,
            setSucceeds: false);

        var status = SwapChainColorSpaceConfigurator.Configure(
            controller,
            HdrConstants.DxgiColorSpace);

        Assert.Equal(PreviewReadinessState.Failed, status.State);
        Assert.Equal(PreviewReadinessStage.Presentation, status.Stage);
        Assert.True(status.RequiresUserAttention);
        Assert.Contains("0x887A0001", status.TechnicalDetail ?? string.Empty, StringComparison.Ordinal);
        Assert.Contains("SetColorSpace1", status.TechnicalDetail ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void AttachFailureMapsToFailedPresentationStatus()
    {
        var exception = SwapChainPanelNativeInterop.CreateFailure(
            "ISwapChainPanelNative.SetSwapChain",
            unchecked((int)0x8001010E),
            "SetSwapChain must run on the owning UI thread.");

        var status = SwapChainManager.MapFailureToReadiness(exception);

        Assert.Equal(PreviewReadinessState.Failed, status.State);
        Assert.Equal(PreviewReadinessStage.Presentation, status.Stage);
        Assert.Contains("ISwapChainPanelNative.SetSwapChain", status.TechnicalDetail ?? string.Empty, StringComparison.Ordinal);
        Assert.Contains("0x8001010E", status.TechnicalDetail ?? string.Empty, StringComparison.Ordinal);
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

        public ColorSpaceType? CheckedColorSpace { get; private set; }

        public ColorSpaceType? SetColorSpace { get; private set; }

        public SwapChainColorSpaceSupportFlags CheckColorSpaceSupport(ColorSpaceType colorSpace)
        {
            CheckedColorSpace = colorSpace;
            return supportFlags;
        }

        public void SetColorSpace1(ColorSpaceType colorSpace)
        {
            SetColorSpace = colorSpace;
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
