namespace Lumiere.Overlay.Crop;

public sealed record CropHitTestResult(
    CropHitTestKind Kind,
    CropAdjustmentHandle Handle)
{
    public static CropHitTestResult None { get; } =
        new(CropHitTestKind.None, CropAdjustmentHandle.None);

    public bool StartsAdjustment =>
        Kind is CropHitTestKind.Edge or CropHitTestKind.Corner;
}
