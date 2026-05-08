using Windows.Foundation.Metadata;
using Windows.Graphics.Capture;

namespace Lumiere.Capture;

public sealed record CaptureBorderOptions(bool IsSystemBorderRequired)
{
    private const string GraphicsCaptureSessionTypeName = "Windows.Graphics.Capture.GraphicsCaptureSession";
    private const string IsBorderRequiredPropertyName = "IsBorderRequired";

    public static CaptureBorderOptions RequireSystemBorder() =>
        new(IsSystemBorderRequired: true);

    public static CaptureBorderOptions TryBorderless() =>
        new(IsSystemBorderRequired: false);

    public CaptureBorderApplicationResult ApplyToSession(GraphicsCaptureSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (IsSystemBorderRequired)
        {
            return CaptureBorderApplicationResult.SystemBorderRequired();
        }

        if (!ApiInformation.IsPropertyPresent(GraphicsCaptureSessionTypeName, IsBorderRequiredPropertyName))
        {
            return CaptureBorderApplicationResult.BorderlessUnavailable(
                "GraphicsCaptureSession.IsBorderRequired is not available on this Windows build.");
        }

        var borderProperty = session.GetType().GetProperty(IsBorderRequiredPropertyName);
        if (borderProperty is null || !borderProperty.CanWrite || !borderProperty.CanRead)
        {
            return CaptureBorderApplicationResult.BorderlessUnavailable(
                "GraphicsCaptureSession.IsBorderRequired is present in OS metadata but unavailable in the current SDK projection.");
        }

        return ApplyToBorderAccessors(
            setIsBorderRequired: value => borderProperty.SetValue(session, value),
            getIsBorderRequired: () => borderProperty.GetValue(session) is true);
    }

    public CaptureBorderApplicationResult ApplyToBorderAccessors(
        Action<bool> setIsBorderRequired,
        Func<bool> getIsBorderRequired)
    {
        ArgumentNullException.ThrowIfNull(setIsBorderRequired);
        ArgumentNullException.ThrowIfNull(getIsBorderRequired);

        if (IsSystemBorderRequired)
        {
            return CaptureBorderApplicationResult.SystemBorderRequired();
        }

        try
        {
            setIsBorderRequired(false);
            var effectiveIsBorderRequired = getIsBorderRequired();
            return new CaptureBorderApplicationResult(
                RequestedBorderless: true,
                Attempted: true,
                Succeeded: !effectiveIsBorderRequired,
                EffectiveIsBorderRequired: effectiveIsBorderRequired,
                TechnicalDetail: effectiveIsBorderRequired
                    ? "Requested borderless WGC capture, but IsBorderRequired remained true. Unpackaged apps cannot guarantee graphicsCaptureWithoutBorder consent."
                    : "Requested borderless WGC capture and IsBorderRequired is false.");
        }
        catch (Exception exception)
        {
            return new CaptureBorderApplicationResult(
                RequestedBorderless: true,
                Attempted: true,
                Succeeded: false,
                EffectiveIsBorderRequired: null,
                TechnicalDetail: $"Requesting borderless WGC capture failed: {exception.GetType().Name}: {exception.Message}");
        }
    }
}
