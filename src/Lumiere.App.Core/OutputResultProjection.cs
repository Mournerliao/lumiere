using Lumiere.Capture;
using Lumiere.Graphics.Output;

namespace Lumiere.App;

public sealed record OutputResultProjection(
    string Title,
    string Detail,
    string FidelityDetail,
    OutputResultProjectionSeverity Severity)
{
    public static OutputResultProjection Project(
        OutputResult? outputResult,
        OutputProfileProjection profile,
        CaptureTarget? captureTarget = null)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return new OutputResultProjection(
            ProjectTitle(outputResult),
            ProjectDetail(outputResult),
            ProjectFidelityDetail(outputResult, profile.FidelityClaim, profile, captureTarget),
            ProjectSeverity(outputResult));
    }

    public static OutputResultProjection Project(
        OutputResult? outputResult,
        FidelityClaimProjection fidelityClaim,
        CaptureTarget? captureTarget = null)
    {
        ArgumentNullException.ThrowIfNull(fidelityClaim);

        var profile = outputResult is null
            ? null
            : HdrAwareOutputProjection.ProjectOutputProfile(outputResult.EffectiveProfile);
        return new OutputResultProjection(
            ProjectTitle(outputResult),
            ProjectDetail(outputResult),
            ProjectFidelityDetail(outputResult, fidelityClaim, profile, captureTarget),
            ProjectSeverity(outputResult));
    }

    public static OutputResultProjection Project(OutputResult? outputResult, CaptureTarget? captureTarget = null) =>
        Project(
            outputResult,
            HdrAwareOutputProjection.ProjectOutputProfile(outputResult?.EffectiveProfile
                ?? OutputProfileContract.SrgbCompatibilityPng).FidelityClaim,
            captureTarget);

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
                    OutputTarget.Folder => target.Outcome is OutputOutcome.Success
                        ? FormatSuccessfulTargetDetail(outputResult, target.Target, "File saved")
                        : target.UserMessage,
                    OutputTarget.Clipboard => target.Outcome is OutputOutcome.Success
                        ? FormatSuccessfulTargetDetail(outputResult, target.Target, "Clipboard copied")
                        : target.UserMessage,
                    _ => target.UserMessage,
                }));
    }

    private static string FormatSuccessfulTargetDetail(
        OutputResult outputResult,
        OutputTarget target,
        string fallback)
    {
        var profile = outputResult.EffectiveProfileFor(target);
        return profile.Kind is OutputProfileKind.SrgbCompatibilityPng
            ? $"{fallback} as sRGB Visual Match"
            : $"{fallback} as {profile.Label}";
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
        OutputProfileProjection? profile,
        CaptureTarget? captureTarget)
    {
        var targetPrefix = CaptureTargetScopeProjection.PrefixOutputDetail(captureTarget, null);
        var profilePrefix = profile is null
            ? "Output mode: not selected."
            : HasMixedTargetProfiles(outputResult)
                ? $"Output modes: {FormatPerTargetProfiles(outputResult!)}."
            : outputResult?.UsesCompatibilityProfileFallback is true
                ? $"Requested {outputResult.RequestedProfile.Label}; using {outputResult.EffectiveProfile.Label} Visual Match output."
                : $"Output mode: {profile.Label} Visual Match.";
        var formatContract = profile is null
            ? string.Empty
            : HasMixedTargetProfiles(outputResult)
                ? $" Formats: {FormatPerTargetFormatContracts(outputResult!)}."
            : $" Format: {FormatFormatContract(profile.Contract)}.";

        var detail = $"{profilePrefix} Output handling: {FormatFidelityClaim(fidelityClaim)}.{formatContract}";
        return string.IsNullOrWhiteSpace(targetPrefix)
            ? detail
            : $"{targetPrefix} {detail}";
    }

    private static bool HasMixedTargetProfiles(OutputResult? outputResult) =>
        outputResult is not null
        && outputResult.TargetProfiles
            .Select(profile => profile.EffectiveProfile.Kind)
            .Distinct()
            .Skip(1)
            .Any();

    private static string FormatFormatContract(OutputProfileContractProjection contract) =>
        $"{contract.DestinationPixelFormatLabel}; Transfer: {contract.TransferFunctionLabel}; "
        + $"Primaries: {contract.ColorPrimariesLabel}; Metadata: {contract.MetadataPolicyLabel}";

    private static string FormatPerTargetProfiles(OutputResult outputResult) =>
        string.Join(
            "; ",
            outputResult.TargetProfiles.Select(profile =>
            {
                var fallback = outputResult.UsesCompatibilityProfileFallbackFor(profile.Target)
                    ? " compatibility fallback"
                    : string.Empty;
                return $"{FormatTarget(profile.Target)} {profile.EffectiveProfile.Label}{fallback}";
            }));

    private static string FormatPerTargetFormatContracts(OutputResult outputResult) =>
        string.Join(
            "; ",
            outputResult.TargetProfiles.Select(profile =>
            {
                var contract = HdrAwareOutputProjection.ProjectOutputProfile(profile.EffectiveProfile).Contract;
                return $"{FormatTarget(profile.Target)} {FormatFormatContract(contract)}";
            }));

    private static string FormatFidelityClaim(FidelityClaimProjection fidelityClaim) =>
        fidelityClaim.Kind switch
        {
            FidelityClaimKind.VisualMatch => "sRGB Visual Match output",
            FidelityClaimKind.Converted => "compatibility output",
            FidelityClaimKind.HdrPreserved => "validated HDR output",
            _ => "no completed output yet",
        };

    private static string FormatTarget(OutputTarget target) =>
        target switch
        {
            OutputTarget.Folder => "Folder",
            OutputTarget.Clipboard => "Clipboard",
            _ => target.ToString(),
        };

}

public enum OutputResultProjectionSeverity
{
    Neutral = 0,
    Success,
    Warning,
}
