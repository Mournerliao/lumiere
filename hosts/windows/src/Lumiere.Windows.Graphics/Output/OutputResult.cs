namespace Lumiere.Windows.Graphics.Output;

/// <summary>
/// Reports delivery outcomes without claiming visual match or HDR preservation.
/// </summary>
public sealed record OutputResult(
    IReadOnlyList<OutputTargetResult> Targets,
    string UserMessage,
    string TechnicalDetail)
{
    public bool IsSuccess => Targets.Any(target => target.Outcome == OutputOutcome.Success);

    public OutputOutcome ClipboardOutcome => OutcomeFor(OutputTarget.Clipboard);

    public OutputOutcome FolderOutcome => OutcomeFor(OutputTarget.Folder);

    public static OutputResult ClipboardSuccess(int bytesCopied) =>
        FromTarget(OutputTargetResult.Success(
            OutputTarget.Clipboard,
            "Copied to clipboard",
            $"Clipboard output success: {bytesCopied} bytes",
            bytesWritten: bytesCopied));

    public static OutputResult ClipboardFailed(string? detail = null) =>
        FromTarget(OutputTargetResult.Failed(
            OutputTarget.Clipboard,
            "Failed to copy to clipboard",
            detail ?? "Clipboard output failed"));

    public static OutputResult ClipboardSkipped(string reason) =>
        FromTarget(OutputTargetResult.Skipped(OutputTarget.Clipboard, reason, reason));

    public static OutputResult Skipped(string reason) => ClipboardSkipped(reason);

    public static OutputResult FromTargets(params OutputTargetResult[] targets) =>
        FromTargets((IEnumerable<OutputTargetResult>)targets);

    public static OutputResult FromTargets(IEnumerable<OutputTargetResult> targets)
    {
        ArgumentNullException.ThrowIfNull(targets);
        var targetResults = targets.ToArray();
        if (targetResults.Length == 0)
        {
            throw new ArgumentException("At least one target result is required.", nameof(targets));
        }

        if (targetResults.Length == 1)
        {
            return FromTarget(targetResults[0]);
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
            string.Join("; ", targetResults.Select(target => target.TechnicalDetail)));
    }

    private static OutputResult FromTarget(OutputTargetResult target) =>
        new([target], target.UserMessage, target.TechnicalDetail);

    private OutputOutcome OutcomeFor(OutputTarget target) =>
        Targets.FirstOrDefault(result => result.Target == target)?.Outcome ?? OutputOutcome.Skipped;
}

public sealed record OutputTargetResult(
    OutputTarget Target,
    OutputOutcome Outcome,
    string UserMessage,
    string TechnicalDetail,
    string? ArtifactPath,
    int? BytesWritten)
{
    public static OutputTargetResult Success(
        OutputTarget target,
        string userMessage,
        string? technicalDetail = null,
        string? artifactPath = null,
        int? bytesWritten = null) =>
        new(target, OutputOutcome.Success, userMessage, technicalDetail ?? userMessage, artifactPath, bytesWritten);

    public static OutputTargetResult Failed(
        OutputTarget target,
        string userMessage,
        string? technicalDetail = null) =>
        new(target, OutputOutcome.Failed, userMessage, technicalDetail ?? userMessage, null, null);

    public static OutputTargetResult Skipped(
        OutputTarget target,
        string userMessage,
        string? technicalDetail = null) =>
        new(target, OutputOutcome.Skipped, userMessage, technicalDetail ?? userMessage, null, null);
}

public enum OutputOutcome
{
    Skipped = 0,
    Success = 1,
    Failed = 2,
}
