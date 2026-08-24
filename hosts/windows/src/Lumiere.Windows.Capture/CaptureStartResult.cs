using Lumiere.Windows.Graphics.Hdr;

namespace Lumiere.Windows.Capture;

internal sealed class CaptureStartResult
{
    private CaptureStartResult(
        CaptureSessionResources? sessionResources,
        EngineReadinessStatus readiness)
    {
        SessionResources = sessionResources;
        Readiness = readiness ?? throw new ArgumentNullException(nameof(readiness));
    }

    public CaptureSessionResources? SessionResources { get; }

    public EngineReadinessStatus Readiness { get; }

    public bool Started => SessionResources is not null;

    public static CaptureStartResult StartSucceeded(
        CaptureSessionResources sessionResources,
        EngineReadinessStatus readiness) =>
        new(sessionResources ?? throw new ArgumentNullException(nameof(sessionResources)), readiness);

    public static CaptureStartResult NotStarted(EngineReadinessStatus readiness) =>
        new(null, readiness);
}
