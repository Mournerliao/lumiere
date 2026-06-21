using Lumiere.Graphics.Hdr;
using Lumiere.Graphics.Output;

namespace Lumiere.App;

public static class OverlayFidelityProjection
{
    public static OverlayFidelityCueProjection Project(
        string? exportColorFormat,
        PreviewReadinessStatus? readiness,
        IEnumerable<OutputValidationSessionArtifact>? validationArtifacts,
        OutputProfileExecutionCapabilities executionCapabilities,
        OutputTarget outputTarget = OutputTarget.Folder)
    {
        ArgumentNullException.ThrowIfNull(executionCapabilities);

        var contract = OutputProfileContract.FromSettingsValue(exportColorFormat);
        var profile = validationArtifacts is null
            ? PerfectHdrFidelityProjection.ProjectOutputProfile(
                contract,
                readiness,
                executionCapabilities,
                outputTarget)
            : PerfectHdrFidelityProjection.ProjectOutputProfile(
                contract,
                validationArtifacts,
                readiness,
                executionCapabilities,
                outputTarget);
        var claim = MapClaim(profile.FidelityClaim.Kind);

        return new OverlayFidelityCueProjection(
            claim,
            $"{profile.Label} · {profile.StatusLabel}",
            ProjectDetail(profile, claim));
    }

    private static OverlayFidelityClaimProjection MapClaim(FidelityClaimKind claim) =>
        claim switch
        {
            FidelityClaimKind.Converted => OverlayFidelityClaimProjection.Converted,
            FidelityClaimKind.VisualMatch => OverlayFidelityClaimProjection.VisualMatch,
            FidelityClaimKind.HdrPreserved => OverlayFidelityClaimProjection.HdrPreserved,
            _ => OverlayFidelityClaimProjection.Unvalidated,
        };

    private static string ProjectDetail(
        OutputProfileProjection profile,
        OverlayFidelityClaimProjection claim) =>
        claim switch
        {
            OverlayFidelityClaimProjection.HdrPreserved =>
                $"{profile.Detail} Fidelity claim: HDR-preserved.",
            OverlayFidelityClaimProjection.VisualMatch =>
                $"{profile.Detail} Fidelity claim: Visual match.",
            OverlayFidelityClaimProjection.Converted =>
                $"{profile.Detail} Fidelity claim: Converted output.",
            _ =>
                $"{profile.Detail} {profile.FidelityClaim.Detail}",
        };
}

public sealed record OverlayFidelityCueProjection(
    OverlayFidelityClaimProjection Kind,
    string Label,
    string Detail);

public enum OverlayFidelityClaimProjection
{
    Unvalidated = 0,
    Converted,
    VisualMatch,
    HdrPreserved,
}
