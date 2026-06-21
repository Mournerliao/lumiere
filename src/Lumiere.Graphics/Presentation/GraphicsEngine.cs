using Lumiere.Graphics.Devices;
using Lumiere.Infrastructure.Interop;

namespace Lumiere.Graphics.Presentation;

public sealed class GraphicsEngine
{
    private readonly GraphicsDeviceResources deviceResources;
    private readonly SwapChainManager swapChainManager;

    public GraphicsEngine(
        GraphicsDeviceResources deviceResources,
        SwapChainManager? swapChainManager = null)
    {
        this.deviceResources = deviceResources ?? throw new ArgumentNullException(nameof(deviceResources));
        this.swapChainManager = swapChainManager ?? new SwapChainManager();
    }

    public SwapChainResources CreatePreviewSwapChain(
        SwapChainCreationOptions options,
        ISwapChainPreviewSurface previewSurface,
        SwapChainTargetHint? targetHint = null) =>
        swapChainManager.CreateAttachedCompositionSwapChain(deviceResources, options, previewSurface, targetHint);
}
