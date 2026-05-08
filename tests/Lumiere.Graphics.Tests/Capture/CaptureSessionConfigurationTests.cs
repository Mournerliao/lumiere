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

    [Fact]
    public void DefaultCaptureBorderOptionsRequireSystemBorder()
    {
        var options = CaptureBorderOptions.RequireSystemBorder();

        var result = options.ApplyToBorderAccessors(
            _ => throw new InvalidOperationException("Default options should not set IsBorderRequired."),
            () => throw new InvalidOperationException("Default options should not read IsBorderRequired."));

        Assert.True(options.IsSystemBorderRequired);
        Assert.False(result.RequestedBorderless);
        Assert.False(result.Attempted);
        Assert.True(result.Succeeded);
        Assert.True(result.EffectiveIsBorderRequired);
    }

    [Fact]
    public void TryBorderlessCaptureBorderOptionsRequestIsBorderRequiredFalse()
    {
        var isBorderRequired = true;
        var options = CaptureBorderOptions.TryBorderless();

        var result = options.ApplyToBorderAccessors(
            value => isBorderRequired = value,
            () => isBorderRequired);

        Assert.False(options.IsSystemBorderRequired);
        Assert.True(result.RequestedBorderless);
        Assert.True(result.Attempted);
        Assert.True(result.Succeeded);
        Assert.False(result.EffectiveIsBorderRequired);
    }

    [Fact]
    public void TryBorderlessCaptureBorderOptionsReportsIgnoredBorderlessRequest()
    {
        var options = CaptureBorderOptions.TryBorderless();

        var result = options.ApplyToBorderAccessors(
            _ => { },
            () => true);

        Assert.True(result.RequestedBorderless);
        Assert.True(result.Attempted);
        Assert.False(result.Succeeded);
        Assert.True(result.EffectiveIsBorderRequired);
        Assert.Contains("Unpackaged", result.TechnicalDetail, StringComparison.OrdinalIgnoreCase);
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
