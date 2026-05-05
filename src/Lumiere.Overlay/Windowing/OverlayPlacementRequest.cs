using Windows.Graphics;

namespace Lumiere.Overlay.Windowing;

public sealed record OverlayPlacementRequest(
    SizeInt32 TargetSize,
    bool IsDisplayTarget,
    string TargetDisplayName);
