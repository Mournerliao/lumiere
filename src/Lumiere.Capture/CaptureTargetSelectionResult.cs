using System.Diagnostics.CodeAnalysis;
using Lumiere.Graphics.Hdr;

namespace Lumiere.Capture;

public sealed class CaptureTargetSelectionResult
{
    private CaptureTargetSelectionResult(
        SelectionOutcome outcome,
        CaptureTarget? target,
        PreviewReadinessStatus readiness)
    {
        Outcome = outcome;
        Target = target;
        Readiness = readiness ?? throw new ArgumentNullException(nameof(readiness));
    }

    public SelectionOutcome Outcome { get; }

    public CaptureTarget? Target { get; }

    public PreviewReadinessStatus Readiness { get; }

    [MemberNotNullWhen(true, nameof(Target))]
    public bool IsSelected => Outcome == SelectionOutcome.Selected;

    public bool IsCanceled => Outcome == SelectionOutcome.Canceled;

    public bool IsUnsupported => Outcome == SelectionOutcome.Unsupported;

    public bool IsFailed => Outcome == SelectionOutcome.Failed;

    public static CaptureTargetSelectionResult Selected(
        CaptureTarget target,
        PreviewReadinessStatus readiness) =>
        new(SelectionOutcome.Selected,
            target ?? throw new ArgumentNullException(nameof(target)),
            readiness);

    public static CaptureTargetSelectionResult Canceled(
        PreviewReadinessStatus readiness) =>
        new(SelectionOutcome.Canceled, null, readiness);

    public static CaptureTargetSelectionResult Unsupported(
        PreviewReadinessStatus readiness) =>
        new(SelectionOutcome.Unsupported, null, readiness);

    public static CaptureTargetSelectionResult Failed(
        PreviewReadinessStatus readiness) =>
        new(SelectionOutcome.Failed, null, readiness);
}
