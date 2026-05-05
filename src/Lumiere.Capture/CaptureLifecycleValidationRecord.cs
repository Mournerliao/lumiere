using Lumiere.Graphics.Hdr;

namespace Lumiere.Capture;

public sealed record CaptureLifecycleValidationRecord
{
    private CaptureLifecycleValidationRecord(
        int attemptNumber,
        CaptureLifecycleAttemptKind attemptKind,
        CaptureSessionStatus finalStatus,
        bool captureTeardownCompleted,
        bool previewDetachedBeforeRelease,
        CaptureResourceGrowthEvidence resourceGrowthEvidence,
        PreviewReadinessStatus? readiness)
    {
        AttemptNumber = attemptNumber;
        AttemptKind = attemptKind;
        FinalStatus = finalStatus;
        CaptureTeardownCompleted = captureTeardownCompleted;
        PreviewDetachedBeforeRelease = previewDetachedBeforeRelease;
        ResourceGrowthEvidence = resourceGrowthEvidence;
        Readiness = readiness;
    }

    public int AttemptNumber { get; }

    public CaptureLifecycleAttemptKind AttemptKind { get; }

    public CaptureSessionStatus FinalStatus { get; }

    public bool CaptureTeardownCompleted { get; }

    public bool PreviewDetachedBeforeRelease { get; }

    public CaptureResourceGrowthEvidence ResourceGrowthEvidence { get; }

    public PreviewReadinessStatus? Readiness { get; }

    public bool EndedRecoverably => FinalStatus is CaptureSessionStatus.Idle
        or CaptureSessionStatus.Disposed
        or CaptureSessionStatus.Unsupported
        or CaptureSessionStatus.Degraded
        or CaptureSessionStatus.Failed;

    public static CaptureLifecycleValidationRecord Create(
        int attemptNumber,
        CaptureLifecycleAttemptKind attemptKind,
        CaptureSessionStatus finalStatus,
        bool captureTeardownCompleted,
        bool previewDetachedBeforeRelease,
        PreviewReadinessStatus? readiness = null,
        CaptureResourceGrowthEvidence resourceGrowthEvidence = CaptureResourceGrowthEvidence.NoGrowthObserved)
    {
        if (attemptNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attemptNumber), attemptNumber, "Attempt number must be positive.");
        }

        return new CaptureLifecycleValidationRecord(
            attemptNumber,
            attemptKind,
            finalStatus,
            captureTeardownCompleted,
            previewDetachedBeforeRelease,
            resourceGrowthEvidence,
            readiness);
    }
}
