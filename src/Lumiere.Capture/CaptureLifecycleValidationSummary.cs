namespace Lumiere.Capture;

public sealed record CaptureLifecycleValidationSummary
{
    private CaptureLifecycleValidationSummary(IReadOnlyList<CaptureLifecycleValidationRecord> attempts)
    {
        Attempts = attempts;
    }

    public IReadOnlyList<CaptureLifecycleValidationRecord> Attempts { get; }

    public int AttemptCount => Attempts.Count;

    public bool HasAttempts => Attempts.Count > 0;

    public CaptureSessionStatus? FinalStatus => Attempts.Count == 0 ? null : Attempts[^1].FinalStatus;

    public bool AllAttemptsEndedRecoverably => HasAttempts && Attempts.All(static attempt => attempt.EndedRecoverably);

    public bool AllCaptureTeardownsCompleted => HasAttempts && Attempts.All(static attempt => attempt.CaptureTeardownCompleted);

    public bool AllPreviewResourcesDetachedBeforeRelease => HasAttempts && Attempts.All(static attempt => attempt.PreviewDetachedBeforeRelease);

    public bool HasNoUnboundedResourceGrowthEvidence => Attempts.All(static attempt =>
        attempt.ResourceGrowthEvidence is CaptureResourceGrowthEvidence.NoGrowthObserved);

    public IReadOnlyList<CaptureSessionStatus> StuckFinalStates => Attempts
        .Where(static attempt => !attempt.EndedRecoverably)
        .Select(static attempt => attempt.FinalStatus)
        .ToArray();

    public string? FinalReadinessTechnicalDetail => Attempts.Count == 0 ? null : Attempts[^1].Readiness?.TechnicalDetail;

    public static CaptureLifecycleValidationSummary Create(IEnumerable<CaptureLifecycleValidationRecord> attempts)
    {
        ArgumentNullException.ThrowIfNull(attempts);

        return new CaptureLifecycleValidationSummary(attempts.ToArray());
    }
}
