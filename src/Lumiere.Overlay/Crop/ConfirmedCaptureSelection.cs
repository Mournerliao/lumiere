using Lumiere.Graphics.Output;
using Lumiere.Infrastructure.Diagnostics;
using Microsoft.Extensions.Logging;
using Windows.Foundation;

namespace Lumiere.Overlay.Crop;

public sealed record ConfirmedCaptureSelection(
    Rect DipRegion,
    CropPixelRect PixelRegion,
    CaptureFrameSize FrameSize,
    OverlayDisplayStatus Status,
    string StatusText,
    string TechnicalDetail)
{
    private static readonly ILogger Logger = LumiereLoggerFactory.CreateLogger(LogCategories.Overlay);
    public static bool CanConfirm(CropSelection selection, OverlayDisplayStatus status) =>
        selection.Phase is CropSelectionPhase.Active
        && selection.Geometry.IsValid
        && status is OverlayDisplayStatus.HdrReady
            or OverlayDisplayStatus.DegradedPreview
            or OverlayDisplayStatus.Initializing
            or OverlayDisplayStatus.InvalidCrop;

    public static bool TryCreate(
        CropSelection selection,
        Rect previewBounds,
        CaptureFrameSize frameSize,
        OverlayState overlayState,
        double dpiScaleX,
        double dpiScaleY,
        out ConfirmedCaptureSelection confirmed)
    {
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(overlayState);

        if (!CanConfirm(selection, overlayState.Status))
        {
            confirmed = Empty(overlayState);
            return false;
        }

        var dipRegion = selection.Geometry.Region;
        var pixelRegion = CropCoordinateMapper.MapToCapturePixels(
            dipRegion, previewBounds, frameSize, dpiScaleX, dpiScaleY);

        var effectiveStatus = overlayState.Status is OverlayDisplayStatus.Initializing
            ? OverlayDisplayStatus.DegradedPreview
            : overlayState.Status;

        if (overlayState.Status is OverlayDisplayStatus.Initializing)
        {
            Logger.LogDebug("Confirm during Initializing: mapping to DegradedPreview (frames arriving but HDR status not yet confirmed)");
        }

        confirmed = new ConfirmedCaptureSelection(
            dipRegion,
            pixelRegion,
            frameSize,
            effectiveStatus,
            overlayState.Message,
            overlayState.TechnicalDetail);
        return true;
    }

    private static ConfirmedCaptureSelection Empty(OverlayState state) =>
        new(
            new Rect(0, 0, 0, 0),
            new CropPixelRect(0, 0, 0, 0),
            new CaptureFrameSize(0, 0),
            state.Status,
            state.Message,
            state.TechnicalDetail);
}
