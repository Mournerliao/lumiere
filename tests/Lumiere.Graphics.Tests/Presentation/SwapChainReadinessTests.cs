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
    public void UnknownTargetAwareDisplayCapabilityReportsDegradedPresentationStatus()
    {
        var controller = new FakeColorSpaceController(
            SwapChainColorSpaceSupportFlags.Present,
            setSucceeds: true);

        var status = SwapChainColorSpaceConfigurator.Configure(
            controller,
            HdrConstants.DxgiColorSpace,
            HdrDisplayCapability.Unknown(),
            requireTargetedDisplayCapability: true);

        Assert.Equal(PreviewReadinessState.Degraded, status.State);
        Assert.Equal(PreviewReadinessStage.Presentation, status.Stage);
        Assert.True(status.RequiresUserAttention);
        Assert.Contains("target", status.TechnicalDetail ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("NotMatched", status.TechnicalDetail ?? string.Empty, StringComparison.Ordinal);
        Assert.Null(controller.SetColorSpace);
    }

    [Fact]
    public void TargetAwareDisplayCapabilityReportsMatchEvidenceInPresentationStatus()
    {
        var controller = new FakeColorSpaceController(
            SwapChainColorSpaceSupportFlags.Present,
            setSucceeds: true);
        var displayCapability = new HdrDisplayCapability(
            HdrDisplayState.Active,
            ColorSpaceType.RgbFullG2084NoneP2020,
            "HDR Display",
            HdrDisplayMatchKind.DesktopBounds);

        var status = SwapChainColorSpaceConfigurator.Configure(
            controller,
            HdrConstants.DxgiColorSpace,
            displayCapability,
            requireTargetedDisplayCapability: true);

        Assert.Equal(PreviewReadinessState.Initializing, status.State);
        Assert.Contains("match=DesktopBounds", status.TechnicalDetail ?? string.Empty, StringComparison.Ordinal);
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

        var status = SwapChainManager.FormatFailureAsReadiness(exception);

        Assert.Equal(PreviewReadinessState.Failed, status.State);
        Assert.Equal(PreviewReadinessStage.Presentation, status.Stage);
        Assert.Contains("ISwapChainPanelNative.SetSwapChain", status.TechnicalDetail ?? string.Empty, StringComparison.Ordinal);
        Assert.Contains("0x8001010E", status.TechnicalDetail ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void TargetHintNormalizePreservesDesktopBounds()
    {
        var hint = new SwapChainTargetHint(
            "  \\\\.\\DISPLAY2  ",
            Left: 3840,
            Top: 0,
            Width: 3840,
            Height: 2160);

        var normalized = hint.Normalize();

        Assert.Equal("\\\\.\\DISPLAY2", normalized.DisplayName);
        Assert.Equal(3840, normalized.Left);
        Assert.Equal(0, normalized.Top);
        Assert.Equal(3840, normalized.Width);
        Assert.Equal(2160, normalized.Height);
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
