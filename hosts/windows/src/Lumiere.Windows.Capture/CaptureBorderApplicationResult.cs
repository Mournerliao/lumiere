namespace Lumiere.Windows.Capture;

public sealed record CaptureBorderApplicationResult(
    bool RequestedBorderless,
    bool Attempted,
    bool Succeeded,
    bool? EffectiveIsBorderRequired,
    string TechnicalDetail)
{
    public static CaptureBorderApplicationResult SystemBorderRequired() =>
        new(
            RequestedBorderless: false,
            Attempted: false,
            Succeeded: true,
            EffectiveIsBorderRequired: true,
            TechnicalDetail: "WGC system capture border is required by current capture options.");

    public static CaptureBorderApplicationResult BorderlessUnavailable(string technicalDetail) =>
        new(
            RequestedBorderless: true,
            Attempted: false,
            Succeeded: false,
            EffectiveIsBorderRequired: null,
            TechnicalDetail: technicalDetail);
}
