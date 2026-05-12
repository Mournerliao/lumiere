using Lumiere.Infrastructure.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Lumiere.Graphics.Presentation;

public static class SwapChainDisposalCoordinator
{
    private static readonly ILogger Logger = LumiereLoggerFactory.CreateLogger(LogCategories.Graphics);

    public static SwapChainDisposalEvidence DisposeOnce(
        Action detachPreview,
        Action releaseResources)
    {
        ArgumentNullException.ThrowIfNull(detachPreview);
        ArgumentNullException.ThrowIfNull(releaseResources);

        Logger.LogDebug("operation=SwapChainTeardown, stage=5/6, detail=Detaching preview surface");
        detachPreview();
        Logger.LogDebug("operation=SwapChainTeardown, stage=5/6, detail=Preview surface detached");

        Logger.LogDebug("operation=SwapChainTeardown, stage=6/6, detail=Releasing DXGI swap-chain resources");
        releaseResources();
        Logger.LogDebug("operation=SwapChainTeardown, stage=6/6, detail=DXGI swap-chain resources released");

        Logger.LogInformation("operation=SwapChainTeardown, stage=Complete, detail=Swap chain teardown completed: all steps finished in order");

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
