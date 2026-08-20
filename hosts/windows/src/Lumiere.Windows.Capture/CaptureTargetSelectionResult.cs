using System.Diagnostics.CodeAnalysis;
using Lumiere.Windows.Graphics.Hdr;

namespace Lumiere.Windows.Capture;

public sealed class CaptureTargetSelectionResult
{
    private CaptureTargetSelectionResult(
        SelectionOutcome outcome,
        CaptureTarget? target,
        EngineReadinessStatus readiness)
    {
        Outcome = outcome;
        Target = target;
        Readiness = readiness ?? throw new ArgumentNullException(nameof(readiness));
    }

    public SelectionOutcome Outcome { get; }

    public CaptureTarget? Target { get; }

    public EngineReadinessStatus Readiness { get; }

    [MemberNotNullWhen(true, nameof(Target))]
    public bool IsSelected => Outcome == SelectionOutcome.Selected;

    public bool IsCanceled => Outcome == SelectionOutcome.Canceled;

    public bool IsUnsupported => Outcome == SelectionOutcome.Unsupported;

    public bool IsFailed => Outcome == SelectionOutcome.Failed;

    public static CaptureTargetSelectionResult Selected(
        CaptureTarget target,
        EngineReadinessStatus readiness) =>
        new(SelectionOutcome.Selected,
            target ?? throw new ArgumentNullException(nameof(target)),
            readiness);

    public static CaptureTargetSelectionResult Canceled(
        EngineReadinessStatus readiness) =>
        new(SelectionOutcome.Canceled, null, readiness);

    public static CaptureTargetSelectionResult Unsupported(
        EngineReadinessStatus readiness) =>
        new(SelectionOutcome.Unsupported, null, readiness);

    public static CaptureTargetSelectionResult Failed(
        EngineReadinessStatus readiness) =>
        new(SelectionOutcome.Failed, null, readiness);
}
