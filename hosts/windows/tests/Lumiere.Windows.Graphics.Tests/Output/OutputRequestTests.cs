using Lumiere.Windows.Graphics.Output;
using Lumiere.Windows.Graphics.Presentation;
using Xunit;

namespace Lumiere.Windows.Graphics.Tests.Output;

public sealed class OutputRequestTests
{
    [Theory]
    [InlineData(OutputTarget.Clipboard, true, true, false)]
    [InlineData(OutputTarget.Clipboard, false, false, false)]
    [InlineData(OutputTarget.Folder, true, false, true)]
    [InlineData(OutputTarget.Both, true, true, true)]
    public void ResolvesExplicitDeliveryWithoutSettings(
        OutputTarget delivery,
        bool copyAsImage,
        bool expectedClipboard,
        bool expectedFolder)
    {
        using var texture = new CapturedFrameTexture(null, 2, 2, "test");
        var request = new OutputRequest
        {
            Texture = texture,
            Delivery = delivery,
            CopyAsImage = copyAsImage,
        };

        Assert.Equal(expectedClipboard, request.ShouldWriteClipboard);
        Assert.Equal(expectedFolder, request.ShouldWriteFolder);
    }
}
