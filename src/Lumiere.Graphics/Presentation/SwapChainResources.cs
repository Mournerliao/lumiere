using Lumiere.Graphics.Hdr;
using Vortice.DXGI;

namespace Lumiere.Graphics.Presentation;

public sealed class SwapChainResources : IDisposable
{
    private readonly Action detachPreview;
    private readonly Action releaseResources;
    private bool disposed;

    public SwapChainResources(
        IDXGISwapChain1 swapChain,
        PreviewReadinessStatus presentationEvidence,
        Action detachPreview)
    {
        SwapChain = swapChain ?? throw new ArgumentNullException(nameof(swapChain));
        PresentationEvidence = presentationEvidence ?? throw new ArgumentNullException(nameof(presentationEvidence));
        this.detachPreview = detachPreview ?? throw new ArgumentNullException(nameof(detachPreview));
        releaseResources = SwapChain.Dispose;
    }

    internal SwapChainResources(
        PreviewReadinessStatus presentationEvidence,
        Action detachPreview,
        Action releaseResources)
    {
        SwapChain = null!;
        PresentationEvidence = presentationEvidence ?? throw new ArgumentNullException(nameof(presentationEvidence));
        this.detachPreview = detachPreview ?? throw new ArgumentNullException(nameof(detachPreview));
        this.releaseResources = releaseResources ?? throw new ArgumentNullException(nameof(releaseResources));
    }

    public IDXGISwapChain1 SwapChain { get; }

    public PreviewReadinessStatus PresentationEvidence { get; }

    public SwapChainDisposalEvidence? DisposalEvidence { get; private set; }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        DisposalEvidence = SwapChainDisposalCoordinator.DisposeOnce(
            detachPreview,
            releaseResources);

        disposed = true;
    }

    public void DisposeAfterFailedUiDetach()
    {
        if (disposed)
        {
            return;
        }

        try
        {
            releaseResources();
        }
        finally
        {
            DisposalEvidence = new SwapChainDisposalEvidence(
                PreviewDetached: false,
                ResourcesReleased: true,
                DetachedBeforeRelease: false);
            disposed = true;
        }
    }
}
