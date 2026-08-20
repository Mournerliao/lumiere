using Lumiere.Windows.Graphics.Output;
using Xunit;

namespace Lumiere.Windows.Graphics.Tests.Output;

public sealed class OutputResultTests
{
    [Fact]
    public void AggregatesPartialDeliveryWithoutProfileState()
    {
        var result = OutputResult.FromTargets(
            OutputTargetResult.Success(OutputTarget.Clipboard, "copied", bytesWritten: 128),
            OutputTargetResult.Failed(OutputTarget.Folder, "failed", "disk full"));

        Assert.True(result.IsSuccess);
        Assert.Equal(OutputOutcome.Success, result.ClipboardOutcome);
        Assert.Equal(OutputOutcome.Failed, result.FolderOutcome);
        Assert.Equal("Output partially complete", result.UserMessage);
        Assert.DoesNotContain("profile", result.TechnicalDetail, StringComparison.OrdinalIgnoreCase);
    }
}
