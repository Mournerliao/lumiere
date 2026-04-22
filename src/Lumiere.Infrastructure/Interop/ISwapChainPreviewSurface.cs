using Vortice.DXGI;

namespace Lumiere.Infrastructure.Interop;

public interface ISwapChainPreviewSurface
{
    void AttachSwapChain(IDXGISwapChain swapChain);

    void DetachSwapChain();
}
