using Windows.Foundation;

namespace Lumiere.Overlay.Crop;

public sealed record CropSelection(
    CropSelectionPhase Phase,
    CropGeometry Geometry,
    Point? DragStart)
{
    public static CropSelection Empty { get; } =
        new(CropSelectionPhase.Empty, CropGeometry.Empty, null);

    public bool IsCreating => Phase is CropSelectionPhase.Creating;

    public bool IsAdjusting => Phase is CropSelectionPhase.Adjusting;

    public bool IsGestureActive => IsCreating || IsAdjusting;

    public bool IsVisible =>
        (Phase is CropSelectionPhase.Creating or CropSelectionPhase.Adjusting or CropSelectionPhase.Active)
        && Geometry.IsValid;
}
