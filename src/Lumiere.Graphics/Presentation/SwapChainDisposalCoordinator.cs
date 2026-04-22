namespace Lumiere.Graphics.Presentation;

public static class SwapChainDisposalCoordinator
{
    public static void DisposeOnce(
        Action detachPreview,
        Action releaseResources)
    {
        ArgumentNullException.ThrowIfNull(detachPreview);
        ArgumentNullException.ThrowIfNull(releaseResources);

        detachPreview();
        releaseResources();
    }
}
