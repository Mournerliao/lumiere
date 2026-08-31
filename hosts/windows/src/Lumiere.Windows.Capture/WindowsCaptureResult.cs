using Lumiere.Windows.Graphics.Hdr;
using Lumiere.Windows.Graphics.Output;

namespace Lumiere.Windows.Capture;

public sealed record WindowsCaptureResult(
    WindowsCaptureOutcome Outcome,
    string UserMessage,
    string TechnicalDetail,
    HdrDisplayCapability? HdrCapability = null,
    OutputResult? Output = null)
{
    public bool HasDeliveredArtifact => Output?.IsSuccess == true;
}

public enum WindowsCaptureOutcome
{
    Delivered = 0,
    DeliveryFailed,
    Cancelled,
    TimedOut,
    Unavailable,
    Unsupported,
    Failed,
}

public sealed record WindowsPrepareRegionResult(
    bool Prepared,
    WindowsCaptureOutcome Outcome,
    string UserMessage,
    string TechnicalDetail,
    string? SessionId = null,
    WindowsTargetLogicalSize? TargetLogicalSize = null,
    string? PreviewPath = null,
    int PreviewPixelWidth = 0,
    int PreviewPixelHeight = 0,
    int LeaseMilliseconds = 60_000);
