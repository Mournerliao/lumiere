using Lumiere.Windows.Graphics.Output;

namespace Lumiere.Windows.Capture;

public sealed record WindowsCaptureRequest
{
    public WindowsCaptureRequest(
        string correlationId,
        OutputTarget delivery,
        string? saveDirectory = null,
        bool timestampNaming = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        CorrelationId = correlationId.Trim();
        Delivery = delivery;
        SaveDirectory = string.IsNullOrWhiteSpace(saveDirectory) ? null : saveDirectory.Trim();
        TimestampNaming = timestampNaming;
    }

    public string CorrelationId { get; }

    public OutputTarget Delivery { get; }

    public string? SaveDirectory { get; }

    public bool TimestampNaming { get; }
}
