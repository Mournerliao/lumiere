using Vortice.DXGI;

namespace Lumiere.Graphics.Presentation;

public interface ISwapChainColorSpaceController
{
    SwapChainColorSpaceSupportFlags CheckColorSpaceSupport(ColorSpaceType colorSpace);

    void SetColorSpace1(ColorSpaceType colorSpace);
}
