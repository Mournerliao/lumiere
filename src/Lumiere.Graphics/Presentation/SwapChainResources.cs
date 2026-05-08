using System.Diagnostics;
using Lumiere.Graphics.Hdr;
using Lumiere.Infrastructure.Diagnostics;
using Microsoft.Extensions.Logging;
using Vortice.DXGI;

namespace Lumiere.Graphics.Presentation;

public sealed class SwapChainResources : IDisposable
{
    private static readonly ILogger Logger = LumiereLoggerFactory.CreateLogger(LogCategories.Graphics);
    private readonly Action detachPreview;
    private readonly Action releaseResources;
    private readonly IDXGISwapChain1? swapChain;
    private bool disposed;

    public SwapChainResources(
        IDXGISwapChain1 swapChain,
        PreviewReadinessStatus presentationEvidence,
        Action detachPreview)
    {
        this.swapChain = swapChain ?? throw new ArgumentNullException(nameof(swapChain));
        PresentationEvidence = presentationEvidence ?? throw new ArgumentNullException(nameof(presentationEvidence));
        this.detachPreview = detachPreview ?? throw new ArgumentNullException(nameof(detachPreview));
        releaseResources = this.swapChain.Dispose;
    }

    internal SwapChainResources(
        PreviewReadinessStatus presentationEvidence,
        Action detachPreview,
        Action releaseResources)
    {
        PresentationEvidence = presentationEvidence ?? throw new ArgumentNullException(nameof(presentationEvidence));
        this.detachPreview = detachPreview ?? throw new ArgumentNullException(nameof(detachPreview));
        this.releaseResources = releaseResources ?? throw new ArgumentNullException(nameof(releaseResources));
    }

    public IDXGISwapChain1 SwapChain =>
        swapChain ?? throw new InvalidOperationException("This test-only resource wrapper was created without a swap chain.");

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
        Logger.LogDebug("SwapChain disposed: {Evidence}", FormatDisposalEvidence(DisposalEvidence));

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
            Logger.LogDebug("Swap chain released after failed UI detach. {Evidence}", FormatDisposalEvidence(DisposalEvidence));
            disposed = true;
        }
    }

    private static string FormatDisposalEvidence(SwapChainDisposalEvidence evidence) =>
        $"previewDetached={evidence.PreviewDetached}; resourcesReleased={evidence.ResourcesReleased}; detachedBeforeRelease={evidence.DetachedBeforeRelease}; completed={evidence.Completed}.";
}
