using Lumiere.Graphics.Devices;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Lumiere.Graphics.Presentation;

internal sealed class SwapChainFrameOutput : IPreviewFrameOutput
{
    private const string CopyOperationName = "ID3D11DeviceContext.CopyResource";
    private const string PresentOperationName = "IDXGISwapChain1.Present";

    private readonly GraphicsDeviceResources deviceResources;
    private readonly SwapChainResources swapChainResources;

    public SwapChainFrameOutput(
        GraphicsDeviceResources deviceResources,
        SwapChainResources swapChainResources)
    {
        this.deviceResources = deviceResources ?? throw new ArgumentNullException(nameof(deviceResources));
        this.swapChainResources = swapChainResources ?? throw new ArgumentNullException(nameof(swapChainResources));
    }

    public void CopyFrame(ID3D11Texture2D? texture)
    {
        if (texture is null)
        {
            throw new SwapChainPresentationException(
                CopyOperationName,
                unchecked((int)0x80004003),
                "Captured frame texture was null.");
        }

        try
        {
            using var backBuffer = swapChainResources.SwapChain.GetBuffer<ID3D11Texture2D>(0);
            deviceResources.ImmediateContext.CopyResource(backBuffer, texture);
        }
        catch (SwapChainPresentationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new SwapChainPresentationException(
                CopyOperationName,
                exception.HResult,
                exception.Message,
                exception);
        }
    }

    public void Present()
    {
        try
        {
            swapChainResources.SwapChain.Present(1, PresentFlags.None);
        }
        catch (SwapChainPresentationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new SwapChainPresentationException(
                PresentOperationName,
                exception.HResult,
                exception.Message,
                exception);
        }
    }
}
