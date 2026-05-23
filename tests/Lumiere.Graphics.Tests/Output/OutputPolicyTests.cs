using Lumiere.Graphics.Output;
using Xunit;

namespace Lumiere.Graphics.Tests.Output;

public sealed class OutputPolicyTests
{
    [Fact]
    public void Default_AttemptsClipboardOnly()
    {
        var policy = OutputPolicy.Default;

        Assert.True(policy.ShouldAttemptClipboard);
        Assert.False(policy.ShouldAttemptFolder);
    }

    [Theory]
    [InlineData(OutputTarget.Clipboard, true, true, false)]
    [InlineData(OutputTarget.Clipboard, false, false, false)]
    [InlineData(OutputTarget.Folder, true, false, true)]
    [InlineData(OutputTarget.Folder, false, false, true)]
    [InlineData(OutputTarget.Both, true, true, true)]
    [InlineData(OutputTarget.Both, false, false, true)]
    public void FromSettings_DerivesAttemptPolicy(
        OutputTarget target,
        bool copyAsImage,
        bool shouldAttemptClipboard,
        bool shouldAttemptFolder)
    {
        var policy = OutputPolicy.FromSettings(
            target,
            copyAsImage,
            " C:\\Captures ",
            timestampNaming: true,
            afterCaptureBehavior: "RevealInFolder");

        Assert.Equal(shouldAttemptClipboard, policy.ShouldAttemptClipboard);
        Assert.Equal(shouldAttemptFolder, policy.ShouldAttemptFolder);
        Assert.Equal("C:\\Captures", policy.SavePath);
        Assert.Equal("RevealInFolder", policy.AfterCaptureBehavior);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromSettings_NormalizesBlankOptionalValues(string? value)
    {
        var policy = OutputPolicy.FromSettings(
            OutputTarget.Folder,
            copyAsImage: true,
            value,
            timestampNaming: false,
            afterCaptureBehavior: value);

        Assert.Null(policy.SavePath);
        Assert.Null(policy.AfterCaptureBehavior);
    }
}
