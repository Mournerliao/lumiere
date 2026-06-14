using Lumiere.Capture;
using Lumiere.Graphics.Hdr;
using Lumiere.Graphics.Output;

namespace Lumiere.App;

internal static class AlertMapping
{
    internal enum AlertSeverity
    {
        None = 0,
        Degraded,
        Unsupported,
        Failed,
    }

    internal static AlertSeverity Classify(PreviewReadinessState readinessState, OutputResult? outputResult, bool hdrAlertsEnabled)
    {
        if (!hdrAlertsEnabled || outputResult is not null)
        {
            return AlertSeverity.None;
        }

        return readinessState switch
        {
            PreviewReadinessState.Degraded => AlertSeverity.Degraded,
            PreviewReadinessState.Unsupported => AlertSeverity.Unsupported,
            PreviewReadinessState.Failed => AlertSeverity.Failed,
            _ => AlertSeverity.None,
        };
    }
}
