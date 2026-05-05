namespace Lumiere.Graphics.Presentation;

public sealed record SwapChainDisposalEvidence(
    bool PreviewDetached,
    bool ResourcesReleased,
    bool DetachedBeforeRelease)
{
    public bool Completed => PreviewDetached && ResourcesReleased && DetachedBeforeRelease;
}
