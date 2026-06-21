using Lumiere.Graphics.Hdr;
using Lumiere.Graphics.Presentation;
using Vortice.DXGI;
using Xunit;

namespace Lumiere.Graphics.Tests.Presentation;

public sealed class SwapChainConfigurationTests
{
    [Fact]
    public void DefaultDescriptionUsesFp16FlipModelForComposition()
    {
        var options = new SwapChainCreationOptions(1920, 1080);

        var description = options.CreateDescription();

        Assert.Equal(1920u, description.Width);
        Assert.Equal(1080u, description.Height);
        Assert.Equal(HdrConstants.DxgiSwapChainFormat, description.Format);
        Assert.Equal(SwapEffect.FlipSequential, description.SwapEffect);
        Assert.Equal(Scaling.Stretch, description.Scaling);
        Assert.Equal(2u, description.BufferCount);
        Assert.Equal(1u, description.SampleDescription.Count);
        Assert.Equal(0u, description.SampleDescription.Quality);
        Assert.Equal(Usage.RenderTargetOutput, description.BufferUsage);
    }

    [Fact]
    public void DefaultDescriptionUsesScrgbColorSpace()
    {
        var options = new SwapChainCreationOptions(1280, 720);

        Assert.Equal(HdrConstants.DxgiColorSpace, options.ColorSpace);
    }

    [Fact]
    public void TargetHintNormalizeTrimsNameAndRejectsNegativeSize()
    {
        var hint = new SwapChainTargetHint("  HDR Display  ", -1, 2160);

        var normalized = hint.Normalize();

        Assert.Equal("HDR Display", normalized.DisplayName);
        Assert.Equal(0, normalized.Width);
        Assert.Equal(2160, normalized.Height);
    }

    [Fact]
    public void TargetHintNormalizeUsesNullForBlankName()
    {
        var hint = new SwapChainTargetHint("   ", 3840, 2160);

        var normalized = hint.Normalize();

        Assert.Null(normalized.DisplayName);
        Assert.Equal(3840, normalized.Width);
        Assert.Equal(2160, normalized.Height);
    }

    [Theory]
    [InlineData(0, 1080)]
    [InlineData(1920, 0)]
    [InlineData(-1, 1080)]
    [InlineData(1920, -1)]
    public void CreationOptionsRejectInvalidCompositionSize(int width, int height)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new SwapChainCreationOptions(width, height));

        Assert.Contains("composition swap chain", exception.Message, StringComparison.Ordinal);
    }
}
