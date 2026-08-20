using Lumiere.Windows.Graphics.Clipboard;
using Lumiere.Windows.Graphics.Output;
using Lumiere.Windows.Graphics.Presentation;
using Xunit;

namespace Lumiere.Windows.Graphics.Tests.Output;

public sealed class ClipboardOutputServicePolicyTests
{
    [Fact]
    public async Task SkipsClipboardWhenItWasNotRequested()
    {
        var called = false;
        using var service = new ClipboardOutputService((_, _) =>
        {
            called = true;
            return Task.FromResult(OutputResult.ClipboardSuccess(1));
        });
        using var texture = new CapturedFrameTexture(null, 2, 2, "test");

        var result = await service.ExecuteOutputAsync(new OutputRequest
        {
            Texture = texture,
            Delivery = OutputTarget.Folder,
        });

        Assert.False(called);
        Assert.Equal(OutputOutcome.Skipped, result.ClipboardOutcome);
    }
}
