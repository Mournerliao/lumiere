using Microsoft.UI.Xaml.Controls;
using Vortice.DXGI;

namespace Lumiere.Infrastructure.Interop;

public sealed class SwapChainPanelPreviewSurface : ISwapChainPreviewSurface
{
    private readonly SwapChainPanel panel;

    public SwapChainPanelPreviewSurface(SwapChainPanel panel)
    {
        this.panel = panel ?? throw new ArgumentNullException(nameof(panel));
    }

    public void AttachSwapChain(IDXGISwapChain swapChain) =>
        SwapChainPanelNativeInterop.AttachSwapChain(panel, swapChain);

    public void DetachSwapChain() =>
        SwapChainPanelNativeInterop.DetachSwapChain(panel);
}
