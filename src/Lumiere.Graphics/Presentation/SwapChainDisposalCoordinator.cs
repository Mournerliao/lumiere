namespace Lumiere.Graphics.Presentation;

public static class SwapChainDisposalCoordinator
{
    public static SwapChainDisposalEvidence DisposeOnce(
        Action detachPreview,
        Action releaseResources)
    {
        ArgumentNullException.ThrowIfNull(detachPreview);
        ArgumentNullException.ThrowIfNull(releaseResources);

        detachPreview();
        releaseResources();

        return new SwapChainDisposalEvidence(
            PreviewDetached: true,
            ResourcesReleased: true,
            DetachedBeforeRelease: true);
    }

    public static SwapChainDisposalEvidence CreateIncompleteEvidence() =>
        new(
            PreviewDetached: false,
            ResourcesReleased: false,
            DetachedBeforeRelease: false);
}
