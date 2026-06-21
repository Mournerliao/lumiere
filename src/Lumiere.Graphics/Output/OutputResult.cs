namespace Lumiere.Graphics.Output;

/// <summary>
/// Represents the result of an output operation, with per-target success/failure/skipped state.
/// </summary>
public sealed record OutputResult(
    IReadOnlyList<OutputTargetResult> Targets,
    string? UserMessage,
    string? TechnicalDetail,
    AfterCaptureResult? AfterCapture = null,
    OutputProfileContract? RequestedProfileEvidence = null,
    OutputProfileContract? EffectiveProfileEvidence = null,
    IReadOnlyList<OutputTargetProfileEvidence>? TargetProfileEvidence = null)
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

    public OutputProfileContract RequestedProfile => RequestedProfileEvidence ?? OutputProfileContract.SrgbCompatibilityPng;

    public OutputProfileContract EffectiveProfile => EffectiveProfileEvidence ?? RequestedProfile.EffectiveExecutableProfile;

    public bool UsesCompatibilityProfileFallback => RequestedProfile.Kind != EffectiveProfile.Kind;

    public IReadOnlyList<OutputTargetProfileEvidence> TargetProfiles => TargetProfileEvidence ?? [];

    public OutputProfileContract EffectiveProfileFor(OutputTarget target) =>
        TargetProfiles.FirstOrDefault(profile => profile.Target == target)?.EffectiveProfile
        ?? EffectiveProfile;

    public bool UsesCompatibilityProfileFallbackFor(OutputTarget target) =>
        RequestedProfile.Kind != EffectiveProfileFor(target).Kind;

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

        if (targetResults.Length == 1)
        {
            var target = targetResults[0];
            return new OutputResult(
                targetResults,
                target.UserMessage,
                target.TechnicalDetail ?? target.UserMessage);
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

    /// <summary>
    /// Creates a copy with post-output artifact action details attached.
    /// </summary>
    public OutputResult WithAfterCapture(AfterCaptureResult afterCapture)
    {
        ArgumentNullException.ThrowIfNull(afterCapture);
        return this with { AfterCapture = afterCapture };
    }

    /// <summary>
    /// Creates a copy with output profile evidence attached.
    /// </summary>
    public OutputResult WithRequestedProfile(OutputProfileContract requestedProfile)
    {
        ArgumentNullException.ThrowIfNull(requestedProfile);
        return WithOutputProfiles(requestedProfile, requestedProfile.EffectiveExecutableProfile);
    }

    /// <summary>
    /// Creates a copy with output profile evidence resolved from the runtime output policy.
    /// </summary>
    public OutputResult WithOutputPolicy(OutputPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        var targetProfiles = Targets
            .Select(target => new OutputTargetProfileEvidence(
                target.Target,
                policy.EffectiveProfileFor(target.Target)))
            .ToArray();
        return WithOutputProfiles(policy.RequestedProfile, policy.EffectiveProfile, targetProfiles);
    }

    public OutputResult WithTargetProfile(OutputTarget target, OutputProfileContract effectiveProfile)
    {
        ArgumentNullException.ThrowIfNull(effectiveProfile);

        var profiles = TargetProfiles
            .Where(profile => profile.Target != target)
            .Append(new OutputTargetProfileEvidence(target, effectiveProfile))
            .OrderBy(profile => profile.Target)
            .ToArray();

        return this with
        {
            TargetProfileEvidence = profiles,
        };
    }

    private OutputResult WithOutputProfiles(
        OutputProfileContract requestedProfile,
        OutputProfileContract effectiveProfile,
        IReadOnlyList<OutputTargetProfileEvidence>? targetProfiles = null)
    {
        return this with
        {
            RequestedProfileEvidence = requestedProfile,
            EffectiveProfileEvidence = effectiveProfile,
            TargetProfileEvidence = targetProfiles ?? TargetProfileEvidence,
        };
    }
}

public sealed record OutputTargetProfileEvidence(
    OutputTarget Target,
    OutputProfileContract EffectiveProfile);

/// <summary>
/// Represents post-output artifact action state, separate from target output success.
/// </summary>
public sealed record AfterCaptureResult(
    OutputAfterCaptureAction Action,
    AfterCaptureOutcome Outcome,
    string UserMessage,
    string? TechnicalDetail,
    string? ArtifactPath)
{
    public static AfterCaptureResult NotRequested() =>
        new(
            OutputAfterCaptureAction.None,
            AfterCaptureOutcome.NotRequested,
            "No after-capture action requested",
            "After-capture behavior is None.",
            ArtifactPath: null);

    public static AfterCaptureResult Skipped(OutputAfterCaptureAction action, string reason) =>
        new(action, AfterCaptureOutcome.Skipped, reason, reason, ArtifactPath: null);

    public static AfterCaptureResult Success(OutputAfterCaptureAction action, string artifactPath) =>
        new(
            action,
            AfterCaptureOutcome.Success,
            FormatSuccess(action),
            $"{action} after-capture action succeeded.",
            artifactPath);

    public static AfterCaptureResult Failed(OutputAfterCaptureAction action, string artifactPath, string? detail = null) =>
        new(
            action,
            AfterCaptureOutcome.Failed,
            "After-capture action failed",
            detail ?? $"{action} after-capture action failed.",
            artifactPath);

    private static string FormatSuccess(OutputAfterCaptureAction action) =>
        action switch
        {
            OutputAfterCaptureAction.Reveal => "Revealed saved file",
            OutputAfterCaptureAction.Open => "Opened saved file",
            _ => "After-capture action complete",
        };
}

/// <summary>
/// Represents post-output artifact action outcome.
/// </summary>
public enum AfterCaptureOutcome
{
    NotRequested = 0,
    Skipped = 1,
    Success = 2,
    Failed = 3,
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
