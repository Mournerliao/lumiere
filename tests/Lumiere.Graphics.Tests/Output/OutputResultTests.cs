using Lumiere.Graphics.Output;
using Xunit;

namespace Lumiere.Graphics.Tests.Output;

public sealed class OutputResultTests
{
    [Fact]
    public void ClipboardSkipped_RecordsPerTargetSkippedState()
    {
        var result = OutputResult.ClipboardSkipped("Clipboard output skipped by settings");

        Assert.False(result.IsSuccess);
        Assert.Equal(OutputOutcome.Skipped, result.ClipboardOutcome);
        Assert.Equal(OutputOutcome.Skipped, result.FolderOutcome);
        Assert.Single(result.Targets);
        Assert.Equal("Clipboard output skipped by settings", result.UserMessage);
    }

    [Fact]
    public void FromTargets_ReportsPartialSuccessWithoutStringParsing()
    {
        var result = OutputResult.FromTargets(
            OutputTargetResult.Success(OutputTarget.Clipboard, "Copied to clipboard"),
            OutputTargetResult.Failed(OutputTarget.Folder, "Failed to save file", "Access denied"));

        Assert.True(result.IsSuccess);
        Assert.Equal(OutputOutcome.Success, result.ClipboardOutcome);
        Assert.Equal(OutputOutcome.Failed, result.FolderOutcome);
        Assert.Equal("Output partially complete", result.UserMessage);
        Assert.Contains("Access denied", result.TechnicalDetail);
    }

    [Fact]
    public void FromTargets_ReportsAllSkippedAsSkipped()
    {
        var result = OutputResult.FromTargets(
            OutputTargetResult.Skipped(OutputTarget.Clipboard, "Clipboard disabled"),
            OutputTargetResult.Skipped(OutputTarget.Folder, "Folder output not implemented"));

        Assert.False(result.IsSuccess);
        Assert.Equal(OutputOutcome.Skipped, result.ClipboardOutcome);
        Assert.Equal(OutputOutcome.Skipped, result.FolderOutcome);
        Assert.Equal("Output skipped", result.UserMessage);
    }

    [Fact]
    public void FromTargets_RequiresAtLeastOneTarget()
    {
        Assert.Throws<ArgumentException>(() => OutputResult.FromTargets(Array.Empty<OutputTargetResult>()));
    }
}
