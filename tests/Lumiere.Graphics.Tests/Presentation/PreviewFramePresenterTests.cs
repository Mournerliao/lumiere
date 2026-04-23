using Lumiere.Graphics.Hdr;
using Lumiere.Graphics.Presentation;
using Vortice.Direct3D11;
using Xunit;

namespace Lumiere.Graphics.Tests.Presentation;

public sealed class PreviewFramePresenterTests
{
    [Fact]
    public void PresentFrameReportsReadyAfterGpuCopyAndPresent()
    {
        var output = new FakePreviewFrameOutput();
        var presenter = new PreviewFramePresenter(output);
        using var frame = new CapturedFrameTexture(
            texture: null,
            width: 1920,
            height: 1080,
            sourceDescription: "Direct3D11CaptureFrame.Surface");

        var result = presenter.PresentFrame(frame);

        Assert.True(output.CopyCalled);
        Assert.True(output.PresentCalled);
        Assert.Equal(PreviewReadinessState.Ready, result.Readiness.State);
        Assert.Equal(PreviewReadinessStage.Presentation, result.Readiness.Stage);
        Assert.Contains("without CPU readback", result.Readiness.TechnicalDetail ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void PresentationFailureMapsToFailedStatus()
    {
        var output = new FakePreviewFrameOutput
        {
            ExceptionToThrow = new SwapChainPresentationException(
                "IDXGISwapChain1.Present",
                unchecked((int)0x887A0001),
                "Present rejected the current back buffer."),
        };
        var presenter = new PreviewFramePresenter(output);
        using var frame = new CapturedFrameTexture(
            texture: null,
            width: 1280,
            height: 720,
            sourceDescription: "Direct3D11CaptureFrame.Surface");

        var result = presenter.PresentFrame(frame);

        Assert.Equal(PreviewReadinessState.Failed, result.Readiness.State);
        Assert.Equal(PreviewReadinessStage.Presentation, result.Readiness.Stage);
        Assert.Contains("IDXGISwapChain1.Present", result.Readiness.TechnicalDetail ?? string.Empty, StringComparison.Ordinal);
    }

    private sealed class FakePreviewFrameOutput : IPreviewFrameOutput
    {
        public bool CopyCalled { get; private set; }

        public bool PresentCalled { get; private set; }

        public Exception? ExceptionToThrow { get; init; }

        public void CopyFrame(ID3D11Texture2D? texture)
        {
            CopyCalled = true;
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }
        }

        public void Present()
        {
            PresentCalled = true;
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }
        }
    }
}
