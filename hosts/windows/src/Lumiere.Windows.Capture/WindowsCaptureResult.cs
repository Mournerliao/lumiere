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
