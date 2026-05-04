using Lumiere.Capture;
using Xunit;

namespace Lumiere.Graphics.Tests.Capture;

public sealed class CaptureFrameSizeChangeTests
{
    [Theory]
    [InlineData(1920, 1080, 1920, 1080, false)]
    [InlineData(1920, 1080, 1280, 1080, true)]
    [InlineData(1920, 1080, 1920, 720, true)]
    public void DetectsWhenCapturedFrameSizeNoLongerMatchesActivePreview(
        int activeWidth,
        int activeHeight,
        int frameWidth,
        int frameHeight,
        bool expectedMismatch)
    {
        var decision = CaptureFrameSizeChange.Evaluate(
            activeWidth,
            activeHeight,
            frameWidth,
            frameHeight);

        Assert.Equal(expectedMismatch, decision.RequiresRecreation);
    }

    [Fact]
    public void ReportsPositiveReplacementSizeForMismatch()
    {
        var decision = CaptureFrameSizeChange.Evaluate(
            activeWidth: 1920,
            activeHeight: 1080,
            frameWidth: 2560,
            frameHeight: 1440);

        Assert.True(decision.RequiresRecreation);
        Assert.Equal(2560, decision.ReplacementWidth);
        Assert.Equal(1440, decision.ReplacementHeight);
    }

    [Theory]
    [InlineData(0, 1080, 1920, 1080, "activeWidth")]
    [InlineData(1920, -1, 1920, 1080, "activeHeight")]
    [InlineData(1920, 1080, 0, 1080, "frameWidth")]
    [InlineData(1920, 1080, 1920, -1, "frameHeight")]
    public void RejectsInvalidSizes(
        int activeWidth,
        int activeHeight,
        int frameWidth,
        int frameHeight,
        string parameterName)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            CaptureFrameSizeChange.Evaluate(activeWidth, activeHeight, frameWidth, frameHeight));

        Assert.Equal(parameterName, exception.ParamName);
    }
}
