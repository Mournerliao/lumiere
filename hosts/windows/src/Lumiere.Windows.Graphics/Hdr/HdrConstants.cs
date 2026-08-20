using Vortice.DXGI;
using Windows.Graphics.DirectX;

namespace Lumiere.Windows.Graphics.Hdr;

public static class HdrConstants
{
    public static DirectXPixelFormat WgcFramePoolPixelFormat => DirectXPixelFormat.R16G16B16A16Float;

    public static Format DxgiTextureFormat => Format.R16G16B16A16_Float;
}
