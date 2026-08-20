using Lumiere.Windows.Graphics.Hdr;
using Vortice.DXGI;
using Windows.Graphics.DirectX;
using Xunit;

namespace Lumiere.Windows.Graphics.Tests.Hdr;

public sealed class HdrConstantsTests
{
    [Fact]
    public void WgcFramePoolPixelFormatUsesFp16HdrFormat()
    {
        Assert.Equal(DirectXPixelFormat.R16G16B16A16Float, HdrConstants.WgcFramePoolPixelFormat);
    }

    [Fact]
    public void DxgiTextureFormatUsesFp16HdrFormat()
    {
        Assert.Equal(Format.R16G16B16A16_Float, HdrConstants.DxgiTextureFormat);
        Assert.DoesNotContain("R8G8B8A8", HdrConstants.DxgiTextureFormat.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("B8G8R8A8", HdrConstants.DxgiTextureFormat.ToString(), StringComparison.Ordinal);
    }

}
