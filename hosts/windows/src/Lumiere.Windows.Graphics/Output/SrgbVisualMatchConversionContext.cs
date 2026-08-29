namespace Lumiere.Windows.Graphics.Output;

internal readonly record struct SrgbVisualMatchConversionContext
{
    private const float ScrgbReferenceWhiteInNits = 80f;

    private SrgbVisualMatchConversionContext(float inputLinearScale)
    {
        InputLinearScale = inputLinearScale;
    }

    public float InputLinearScale { get; }

    public static SrgbVisualMatchConversionContext ForSdrDisplay() =>
        new(1f);

    public static SrgbVisualMatchConversionContext ForHdrDisplay(float sdrWhiteLevelInNits)
    {
        if (!float.IsFinite(sdrWhiteLevelInNits) || sdrWhiteLevelInNits <= 0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sdrWhiteLevelInNits),
                sdrWhiteLevelInNits,
                "SDR white level must be a positive finite luminance value.");
        }

        return new(ScrgbReferenceWhiteInNits / sdrWhiteLevelInNits);
    }
}
