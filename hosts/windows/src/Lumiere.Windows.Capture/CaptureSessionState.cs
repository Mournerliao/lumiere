using Lumiere.Windows.Graphics.Hdr;

namespace Lumiere.Windows.Capture;

internal sealed record CaptureSessionState
{
    private CaptureSessionState(
        CaptureSessionStatus status,
        CaptureTarget? target,
        EngineReadinessStatus readiness,
        string? userFacingReason = null,
        string? technicalDetail = null)
    {
        Status = status;
        Target = target;
        Readiness = readiness ?? throw new ArgumentNullException(nameof(readiness));
        UserFacingReason = userFacingReason ?? readiness.UserMessage;
        TechnicalDetail = technicalDetail ?? readiness.TechnicalDetail;
    }

    public CaptureSessionStatus Status { get; }

    public CaptureTarget? Target { get; }

    public EngineReadinessStatus Readiness { get; }

    public string? UserFacingReason { get; }

    public string? TechnicalDetail { get; }

    public bool HasNativeSession => Status is CaptureSessionStatus.Initializing or CaptureSessionStatus.Capturing or CaptureSessionStatus.Degraded
        || (Status is CaptureSessionStatus.Unsupported && Target is not null);

    public static CaptureSessionState Idle(EngineReadinessStatus? readiness = null) =>
        new(
            CaptureSessionStatus.Idle,
            null,
            readiness ?? EngineReadinessStatus.Initializing(
                EngineReadinessStage.Capture,
                "Choose a display or window to start the minimal HDR capture.",
                "Capture session is idle."));

    public static CaptureSessionState SelectingTarget(EngineReadinessStatus? readiness = null) =>
        new(
            CaptureSessionStatus.SelectingTarget,
            null,
            readiness ?? EngineReadinessStatus.Initializing(
                EngineReadinessStage.Capture,
                "Choose a display or window to start the minimal HDR capture.",
                "GraphicsCapturePicker is waiting for user selection."));

    public static CaptureSessionState Initializing(CaptureTarget target, EngineReadinessStatus readiness) =>
        new(CaptureSessionStatus.Initializing, target ?? throw new ArgumentNullException(nameof(target)), readiness);

    public static CaptureSessionState Capturing(CaptureTarget target, EngineReadinessStatus readiness) =>
        new(CaptureSessionStatus.Capturing, target ?? throw new ArgumentNullException(nameof(target)), readiness);

    public static CaptureSessionState Degraded(CaptureTarget? target, EngineReadinessStatus readiness) =>
        new(CaptureSessionStatus.Degraded, target, readiness);

    public static CaptureSessionState Unsupported(EngineReadinessStatus readiness) =>
        new(CaptureSessionStatus.Unsupported, null, readiness);

    public static CaptureSessionState Unsupported(CaptureTarget target, EngineReadinessStatus readiness) =>
        new(CaptureSessionStatus.Unsupported, target ?? throw new ArgumentNullException(nameof(target)), readiness);

    public static CaptureSessionState Failed(CaptureTarget? target, EngineReadinessStatus readiness) =>
        new(CaptureSessionStatus.Failed, target, readiness);

    public static CaptureSessionState Disposed(EngineReadinessStatus? readiness = null) =>
        new(
            CaptureSessionStatus.Disposed,
            null,
            readiness ?? EngineReadinessStatus.Initializing(
                EngineReadinessStage.Capture,
                "Capture stopped",
                "Capture session resources were disposed."));

    public static CaptureSessionState FromSelectionResult(CaptureTargetSelectionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Outcome switch
        {
            SelectionOutcome.Selected when result.Target is not null => Initializing(result.Target, result.Readiness),
            SelectionOutcome.Selected => Failed(null, EngineReadinessStatus.Failed(
                EngineReadinessStage.Capture,
                "Capture failed",
                "Selected capture target result did not contain a target.")),
            SelectionOutcome.Canceled => Idle(result.Readiness),
            SelectionOutcome.Unsupported => Unsupported(result.Readiness),
            SelectionOutcome.Failed => Failed(null, result.Readiness),
            _ => Failed(null, EngineReadinessStatus.Failed(
                EngineReadinessStage.Capture,
                "Capture failed",
                $"Unknown capture target selection outcome: {result.Outcome}.")),
        };
    }

    public static CaptureSessionState FromStartResult(CaptureTarget target, CaptureStartResult result)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(result);

        return FromReadiness(target, result.Readiness, treatReadyAsCapturing: false);
    }

    public static CaptureSessionState FromReadiness(CaptureTarget target, EngineReadinessStatus readiness) =>
        FromReadiness(target, readiness, treatReadyAsCapturing: true);

    private static CaptureSessionState FromReadiness(
        CaptureTarget target,
        EngineReadinessStatus readiness,
        bool treatReadyAsCapturing)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(readiness);

        return readiness.State switch
        {
            EngineReadinessState.Ready when treatReadyAsCapturing => Capturing(target, readiness),
            EngineReadinessState.Ready => Initializing(target, readiness),
            EngineReadinessState.Degraded => Degraded(target, readiness),
            EngineReadinessState.Unsupported => treatReadyAsCapturing ? Unsupported(target, readiness) : Unsupported(readiness),
            EngineReadinessState.Failed => Failed(target, readiness),
            _ => Initializing(target, readiness),
        };
    }
}
