using Lumiere.Graphics.Hdr;
using Vortice.DXGI;

namespace Lumiere.Graphics.Presentation;

public sealed class SwapChainResources : IDisposable
{
    private readonly Action detachPreview;
    private bool disposed;

    public SwapChainResources(
        IDXGISwapChain1 swapChain,
        PreviewReadinessStatus presentationEvidence,
        Action detachPreview)
    {
        SwapChain = swapChain ?? throw new ArgumentNullException(nameof(swapChain));
        PresentationEvidence = presentationEvidence ?? throw new ArgumentNullException(nameof(presentationEvidence));
        this.detachPreview = detachPreview ?? throw new ArgumentNullException(nameof(detachPreview));
    }

    public IDXGISwapChain1 SwapChain { get; }

    public PreviewReadinessStatus PresentationEvidence { get; }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        SwapChainDisposalCoordinator.DisposeOnce(
            detachPreview,
            SwapChain.Dispose);

        disposed = true;
    }
}
