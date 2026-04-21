using Vortice.DXGI;
using Windows.Graphics.DirectX;

namespace Lumiere.Graphics.Hdr;

public static class HdrConstants
{
    public static DirectXPixelFormat WgcFramePoolPixelFormat => DirectXPixelFormat.R16G16B16A16Float;

    public static Format DxgiSwapChainFormat => Format.R16G16B16A16_Float;

    public static ColorSpaceType DxgiColorSpace => ColorSpaceType.RgbFullG10NoneP709;
}
