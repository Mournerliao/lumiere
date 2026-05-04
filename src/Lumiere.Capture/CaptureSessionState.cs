using Lumiere.Graphics.Hdr;

namespace Lumiere.Capture;

public sealed record CaptureSessionState
{
    private CaptureSessionState(
        CaptureSessionStatus status,
        CaptureTarget? target,
        PreviewReadinessStatus readiness,
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

    public PreviewReadinessStatus Readiness { get; }

    public string? UserFacingReason { get; }

    public string? TechnicalDetail { get; }

    public bool HasNativeSession => Status is CaptureSessionStatus.Initializing or CaptureSessionStatus.Capturing or CaptureSessionStatus.Degraded
        || (Status is CaptureSessionStatus.Unsupported && Target is not null);

    public static CaptureSessionState Idle(PreviewReadinessStatus? readiness = null) =>
        new(
            CaptureSessionStatus.Idle,
            null,
            readiness ?? PreviewReadinessStatus.Initializing(
                PreviewReadinessStage.Capture,
                "Choose a display or window to start the minimal HDR preview.",
                "Capture session is idle."));

    public static CaptureSessionState SelectingTarget(PreviewReadinessStatus? readiness = null) =>
        new(
            CaptureSessionStatus.SelectingTarget,
            null,
            readiness ?? PreviewReadinessStatus.Initializing(
                PreviewReadinessStage.Capture,
                "Choose a display or window to start the minimal HDR preview.",
                "GraphicsCapturePicker is waiting for user selection."));

    public static CaptureSessionState Initializing(CaptureTarget target, PreviewReadinessStatus readiness) =>
        new(CaptureSessionStatus.Initializing, target ?? throw new ArgumentNullException(nameof(target)), readiness);

    public static CaptureSessionState Capturing(CaptureTarget target, PreviewReadinessStatus readiness) =>
        new(CaptureSessionStatus.Capturing, target ?? throw new ArgumentNullException(nameof(target)), readiness);

    public static CaptureSessionState Degraded(CaptureTarget? target, PreviewReadinessStatus readiness) =>
        new(CaptureSessionStatus.Degraded, target, readiness);

    public static CaptureSessionState Unsupported(PreviewReadinessStatus readiness) =>
        new(CaptureSessionStatus.Unsupported, null, readiness);

    public static CaptureSessionState Unsupported(CaptureTarget target, PreviewReadinessStatus readiness) =>
        new(CaptureSessionStatus.Unsupported, target ?? throw new ArgumentNullException(nameof(target)), readiness);

    public static CaptureSessionState Failed(CaptureTarget? target, PreviewReadinessStatus readiness) =>
        new(CaptureSessionStatus.Failed, target, readiness);

    public static CaptureSessionState Disposed(PreviewReadinessStatus? readiness = null) =>
        new(
            CaptureSessionStatus.Disposed,
            null,
            readiness ?? PreviewReadinessStatus.Initializing(
                PreviewReadinessStage.Capture,
                "Preview stopped",
                "Capture session resources were disposed."));

    public static CaptureSessionState FromSelectionResult(CaptureTargetSelectionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Outcome switch
        {
            SelectionOutcome.Selected when result.Target is not null => Initializing(result.Target, result.Readiness),
            SelectionOutcome.Selected => Failed(null, PreviewReadinessStatus.Failed(
                PreviewReadinessStage.Capture,
                "Preview failed",
                "Selected capture target result did not contain a target.")),
            SelectionOutcome.Canceled => Idle(result.Readiness),
            SelectionOutcome.Unsupported => Unsupported(result.Readiness),
            SelectionOutcome.Failed => Failed(null, result.Readiness),
            _ => Failed(null, PreviewReadinessStatus.Failed(
                PreviewReadinessStage.Capture,
                "Preview failed",
                $"Unknown capture target selection outcome: {result.Outcome}.")),
        };
    }

    public static CaptureSessionState FromStartResult(CaptureTarget target, CaptureStartResult result)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(result);

        if (result.Started)
        {
            return FromReadiness(target, result.Readiness, treatReadyAsCapturing: false);
        }

        return FromReadiness(target, result.Readiness, treatReadyAsCapturing: false);
    }

    public static CaptureSessionState FromReadiness(CaptureTarget target, PreviewReadinessStatus readiness) =>
        FromReadiness(target, readiness, treatReadyAsCapturing: true);

    private static CaptureSessionState FromReadiness(
        CaptureTarget target,
        PreviewReadinessStatus readiness,
        bool treatReadyAsCapturing)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(readiness);

        return readiness.State switch
        {
            PreviewReadinessState.Ready when treatReadyAsCapturing => Capturing(target, readiness),
            PreviewReadinessState.Ready => Initializing(target, readiness),
            PreviewReadinessState.Degraded => Degraded(target, readiness),
            PreviewReadinessState.Unsupported => treatReadyAsCapturing ? Unsupported(target, readiness) : Unsupported(readiness),
            PreviewReadinessState.Failed => Failed(target, readiness),
            _ => Initializing(target, readiness),
        };
    }
}
