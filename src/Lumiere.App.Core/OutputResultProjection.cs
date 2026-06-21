using Lumiere.Graphics.Output;

namespace Lumiere.App;

public sealed record OutputResultProjection(
    string Title,
    string Detail,
    string FidelityDetail,
    OutputResultProjectionSeverity Severity)
{
    public static OutputResultProjection Project(OutputResult? outputResult, FidelityClaimProjection fidelityClaim)
    {
        ArgumentNullException.ThrowIfNull(fidelityClaim);

        var profile = outputResult is null
            ? null
            : PerfectHdrFidelityProjection.ProjectOutputProfile(outputResult.EffectiveProfile);
        return new OutputResultProjection(
            ProjectTitle(outputResult),
            ProjectDetail(outputResult),
            ProjectFidelityDetail(outputResult, fidelityClaim, profile),
            ProjectSeverity(outputResult));
    }

    public static OutputResultProjection Project(OutputResult? outputResult) =>
        Project(
            outputResult,
            PerfectHdrFidelityProjection.ProjectOutputProfile(outputResult?.EffectiveProfile
                ?? OutputProfileContract.SrgbCompatibilityPng).FidelityClaim);

    private static string ProjectTitle(OutputResult? outputResult)
    {
        if (outputResult is null)
        {
            return "Ready";
        }

        if (!outputResult.IsSuccess)
        {
            return outputResult.UserMessage ?? "Output failed";
        }

        var hasFailure = outputResult.Targets.Any(target => target.Outcome == OutputOutcome.Failed);
        if (hasFailure)
        {
            return outputResult.UserMessage ?? "Output partially complete";
        }

        var targets = outputResult.Targets.Select(target => target.Target).Distinct().ToArray();
        return targets.Contains(OutputTarget.Clipboard) && targets.Contains(OutputTarget.Folder)
            ? "Copied and saved"
            : targets.Contains(OutputTarget.Folder)
                ? "Saved"
                : "Copied";
    }

    private static string ProjectDetail(OutputResult? outputResult)
    {
        if (outputResult is null)
        {
            return "No capture output has completed yet.";
        }

        return string.Join(
            " | ",
            outputResult.Targets.Select(target =>
                target.Target switch
                {
                    OutputTarget.Folder => target.Outcome is OutputOutcome.Success ? "File saved" : target.UserMessage,
                    OutputTarget.Clipboard => target.Outcome is OutputOutcome.Success ? "Clipboard copied" : target.UserMessage,
                    _ => target.UserMessage,
                }));
    }

    private static OutputResultProjectionSeverity ProjectSeverity(OutputResult? outputResult)
    {
        if (outputResult is null)
        {
            return OutputResultProjectionSeverity.Neutral;
        }

        var hasFailure = outputResult.Targets.Any(target => target.Outcome == OutputOutcome.Failed);
        return outputResult.IsSuccess && !hasFailure
            ? OutputResultProjectionSeverity.Success
            : OutputResultProjectionSeverity.Warning;
    }

    private static string ProjectFidelityDetail(
        OutputResult? outputResult,
        FidelityClaimProjection fidelityClaim,
        OutputProfileProjection? profile)
    {
        var profilePrefix = profile is null
            ? "Output profile: not selected."
            : outputResult?.UsesCompatibilityProfileFallback is true
                ? $"Output profile: requested {outputResult.RequestedProfile.Label}; using {outputResult.EffectiveProfile.Label} compatibility fallback."
                : $"Output profile: {profile.Label}.";
        var viewerEvidence = outputResult is null
            ? string.Empty
            : $" Viewer evidence: {FormatViewerEvidence(outputResult.EffectiveProfile)}.";

        return $"{profilePrefix} Fidelity claim: {fidelityClaim.Label}. {fidelityClaim.Detail}{viewerEvidence}";
    }

    private static string FormatViewerEvidence(OutputProfileContract profile) =>
        string.Join(
            ", ",
            profile.ViewerEvidence.Select(viewer => $"{viewer.Name} {FormatEvidenceStatus(viewer.ArtifactHandlingStatus)}"));

    private static string FormatEvidenceStatus(OutputCompatibilityEvidenceStatus status) =>
        status switch
        {
            OutputCompatibilityEvidenceStatus.Pass => "PASS",
            OutputCompatibilityEvidenceStatus.Limited => "PASS with limitation",
            OutputCompatibilityEvidenceStatus.Fail => "FAIL",
            OutputCompatibilityEvidenceStatus.NotApplicable => "N/A",
            _ => "NOT RUN",
        };
}

public enum OutputResultProjectionSeverity
{
    Neutral = 0,
    Success,
    Warning,
}
