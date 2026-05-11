namespace Lumiere.Overlay;

public sealed record OverlayStatusStyle(uint BackgroundArgb, uint BorderArgb)
{
    public static OverlayStatusStyle FromStatus(OverlayDisplayStatus status) =>
        status switch
        {
            OverlayDisplayStatus.HdrReady => new(0xCC052E16, 0xAA86EFAC),
            OverlayDisplayStatus.DegradedPreview => new(0xCC422006, 0xAAFBBF24),
            OverlayDisplayStatus.UnsupportedCapture => new(0xCC3B0764, 0xAAC084FC),
            OverlayDisplayStatus.PreviewFailed => new(0xCC450A0A, 0xAAFCA5A5),
            OverlayDisplayStatus.Closing or OverlayDisplayStatus.Disposed => new(0xCC1F2937, 0xAA9CA3AF),
            OverlayDisplayStatus.InvalidCrop => new(0xCC3B1A00, 0xAAF59E0B),
            _ => new(0xCC111827, 0x80F9FAFB),
        };
}
