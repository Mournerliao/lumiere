using Lumiere.Graphics.Hdr;

namespace Lumiere.Capture;

public sealed class CaptureStartResult
{
    private CaptureStartResult(
        CaptureSessionResources? sessionResources,
        PreviewReadinessStatus readiness)
    {
        SessionResources = sessionResources;
        Readiness = readiness ?? throw new ArgumentNullException(nameof(readiness));
    }

    public CaptureSessionResources? SessionResources { get; }

    public PreviewReadinessStatus Readiness { get; }

    public bool Started => SessionResources is not null;

    public static CaptureStartResult StartSucceeded(
        CaptureSessionResources sessionResources,
        PreviewReadinessStatus readiness) =>
        new(sessionResources ?? throw new ArgumentNullException(nameof(sessionResources)), readiness);

    public static CaptureStartResult NotStarted(PreviewReadinessStatus readiness) =>
        new(null, readiness);
}
