namespace Lumiere.Capture;

public sealed record CaptureFrameSizeChange
{
    private CaptureFrameSizeChange(
        bool requiresRecreation,
        int replacementWidth,
        int replacementHeight)
    {
        RequiresRecreation = requiresRecreation;
        ReplacementWidth = replacementWidth;
        ReplacementHeight = replacementHeight;
    }

    public bool RequiresRecreation { get; }

    public int ReplacementWidth { get; }

    public int ReplacementHeight { get; }

    public static CaptureFrameSizeChange Evaluate(
        int activeWidth,
        int activeHeight,
        int frameWidth,
        int frameHeight)
    {
        ThrowIfNotPositive(activeWidth, nameof(activeWidth));
        ThrowIfNotPositive(activeHeight, nameof(activeHeight));
        ThrowIfNotPositive(frameWidth, nameof(frameWidth));
        ThrowIfNotPositive(frameHeight, nameof(frameHeight));

        return new CaptureFrameSizeChange(
            activeWidth != frameWidth || activeHeight != frameHeight,
            frameWidth,
            frameHeight);
    }

    private static void ThrowIfNotPositive(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Capture frame size comparison requires positive dimensions.");
        }
    }
}
