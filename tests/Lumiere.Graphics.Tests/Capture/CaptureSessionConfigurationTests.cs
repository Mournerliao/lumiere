using Lumiere.Capture;
using Lumiere.Graphics.Hdr;
using Xunit;

namespace Lumiere.Graphics.Tests.Capture;

public sealed class CaptureSessionConfigurationTests
{
    [Fact]
    public void DefaultCaptureOptionsUseFp16PixelFormatAndDoubleBuffering()
    {
        var options = new CaptureSessionOptions(3840, 2160);

        Assert.Equal(3840, options.BufferSize.Width);
        Assert.Equal(2160, options.BufferSize.Height);
        Assert.Equal(2, options.BufferCount);
        Assert.Equal(HdrConstants.WgcFramePoolPixelFormat, options.PixelFormat);
    }

    [Theory]
    [InlineData(0, 1080)]
    [InlineData(1920, 0)]
    [InlineData(-1, 1080)]
    [InlineData(1920, -1)]
    public void CaptureOptionsRejectInvalidFramePoolSize(int width, int height)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new CaptureSessionOptions(width, height));

        Assert.Contains("frame pool", exception.Message, StringComparison.Ordinal);
    }
}
