namespace Lumiere.Graphics.Output;

/// <summary>
/// Represents the result of an output operation, with per-target success/failure/skipped state.
/// </summary>
public sealed record OutputResult(
    IReadOnlyList<OutputTargetResult> Targets,
    string? UserMessage,
    string? TechnicalDetail)
{
    /// <summary>
    /// Gets whether the overall output operation succeeded (at least one target succeeded).
    /// </summary>
    public bool IsSuccess => Targets.Any(target => target.Outcome == OutputOutcome.Success);

    /// <summary>
    /// Gets the clipboard output outcome.
    /// </summary>
    public OutputOutcome ClipboardOutcome => OutcomeFor(OutputTarget.Clipboard);

    /// <summary>
    /// Gets the folder output outcome.
    /// </summary>
    public OutputOutcome FolderOutcome => OutcomeFor(OutputTarget.Folder);

    /// <summary>
    /// Creates a successful clipboard output result.
    /// </summary>
    public static OutputResult ClipboardSuccess(int bytesCopied) =>
        FromTarget(OutputTargetResult.Success(
            OutputTarget.Clipboard,
            "Copied to clipboard",
            $"Clipboard output success: {bytesCopied} bytes"));

    /// <summary>
    /// Creates a failed clipboard output result.
    /// </summary>
    public static OutputResult ClipboardFailed(string? detail = null) =>
        FromTarget(OutputTargetResult.Failed(
            OutputTarget.Clipboard,
            "Failed to copy to clipboard",
            detail ?? "Clipboard output failed"));

    /// <summary>
    /// Creates a skipped clipboard output result.
    /// </summary>
    public static OutputResult ClipboardSkipped(string reason) =>
        FromTarget(OutputTargetResult.Skipped(OutputTarget.Clipboard, reason, reason));

    /// <summary>
    /// Creates a result indicating the output was skipped (no valid region or target).
    /// </summary>
    public static OutputResult Skipped(string reason) =>
        FromTarget(OutputTargetResult.Skipped(OutputTarget.Clipboard, reason, reason));

    /// <summary>
    /// Creates an aggregate result from individual target results.
    /// </summary>
    public static OutputResult FromTargets(params OutputTargetResult[] targets)
    {
        ArgumentNullException.ThrowIfNull(targets);

        return FromTargets((IEnumerable<OutputTargetResult>)targets);
    }

    /// <summary>
    /// Creates an aggregate result from individual target results.
    /// </summary>
    public static OutputResult FromTargets(IEnumerable<OutputTargetResult> targets)
    {
        ArgumentNullException.ThrowIfNull(targets);
        var targetResults = targets.ToArray();
        if (targetResults.Length == 0)
        {
            throw new ArgumentException("At least one target result is required.", nameof(targets));
        }

        var successCount = targetResults.Count(target => target.Outcome == OutputOutcome.Success);
        var failedCount = targetResults.Count(target => target.Outcome == OutputOutcome.Failed);
        var skippedCount = targetResults.Count(target => target.Outcome == OutputOutcome.Skipped);

        var userMessage = (successCount, failedCount, skippedCount) switch
        {
            ( > 0, 0, 0) => "Output complete",
            ( > 0, > 0, _) => "Output partially complete",
            (0, > 0, _) => "Output failed",
            (0, 0, > 0) => "Output skipped",
            _ => "Output complete",
        };

        return new OutputResult(
            targetResults,
            userMessage,
            string.Join("; ", targetResults.Select(target => target.TechnicalDetail ?? target.UserMessage)));
    }

    private static OutputResult FromTarget(OutputTargetResult target) =>
        new([target], target.UserMessage, target.TechnicalDetail);

    private OutputOutcome OutcomeFor(OutputTarget target) =>
        Targets.FirstOrDefault(result => result.Target == target)?.Outcome ?? OutputOutcome.Skipped;
}

/// <summary>
/// Represents the outcome of one concrete output target.
/// </summary>
public sealed record OutputTargetResult(
    OutputTarget Target,
    OutputOutcome Outcome,
    string UserMessage,
    string? TechnicalDetail,
    string? ArtifactPath)
{
    public static OutputTargetResult Success(
        OutputTarget target,
        string userMessage,
        string? technicalDetail = null,
        string? artifactPath = null) =>
        new(target, OutputOutcome.Success, userMessage, technicalDetail, artifactPath);

    public static OutputTargetResult Failed(
        OutputTarget target,
        string userMessage,
        string? technicalDetail = null) =>
        new(target, OutputOutcome.Failed, userMessage, technicalDetail, ArtifactPath: null);

    public static OutputTargetResult Skipped(
        OutputTarget target,
        string userMessage,
        string? technicalDetail = null) =>
        new(target, OutputOutcome.Skipped, userMessage, technicalDetail, ArtifactPath: null);
}

/// <summary>
/// Represents the outcome of a single output target operation.
/// </summary>
public enum OutputOutcome
{
    /// <summary>
    /// The target was not requested or not applicable.
    /// </summary>
    Skipped = 0,

    /// <summary>
    /// The target output succeeded.
    /// </summary>
    Success = 1,

    /// <summary>
    /// The target output failed.
    /// </summary>
    Failed = 2
}
