using Vortice.DXGI;

namespace Lumiere.Graphics.Presentation;

public sealed class SwapChainColorSpaceController : ISwapChainColorSpaceController
{
    private readonly IDXGISwapChain3 swapChain;

    public SwapChainColorSpaceController(IDXGISwapChain3 swapChain)
    {
        this.swapChain = swapChain ?? throw new ArgumentNullException(nameof(swapChain));
    }

    public SwapChainColorSpaceSupportFlags CheckColorSpaceSupport(ColorSpaceType colorSpace)
    {
        return swapChain.CheckColorSpaceSupport(colorSpace);
    }

    public void SetColorSpace1(ColorSpaceType colorSpace) =>
        swapChain.SetColorSpace1(colorSpace);
}
