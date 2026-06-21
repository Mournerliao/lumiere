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
        TargetDisplayUnresolved,
        Unsupported,
        Failed,
    }

    internal static AlertSeverity Classify(
        PreviewReadinessStatus readiness,
        OutputResult? outputResult,
        bool hdrAlertsEnabled)
    {
        if (!hdrAlertsEnabled || outputResult is not null)
        {
            return AlertSeverity.None;
        }

        if (readiness.Reason is PreviewReadinessReason.TargetDisplayUnresolved)
        {
            return AlertSeverity.TargetDisplayUnresolved;
        }

        return readiness.State switch
        {
            PreviewReadinessState.Degraded => AlertSeverity.Degraded,
            PreviewReadinessState.Unsupported => AlertSeverity.Unsupported,
            PreviewReadinessState.Failed => AlertSeverity.Failed,
            _ => AlertSeverity.None,
        };
    }
}
