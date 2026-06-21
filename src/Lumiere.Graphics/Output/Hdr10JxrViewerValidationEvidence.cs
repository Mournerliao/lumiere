namespace Lumiere.Graphics.Output;

/// <summary>
/// Evaluates whether Windows manual output artifacts can satisfy the viewer-facing
/// HDR10 gates for the JPEG XR HDR10 path. This does not enable runtime execution.
/// </summary>
public sealed record Hdr10JxrViewerValidationEvidence(
    bool HasArtifacts,
    bool HasCompleteTargetAwareHdrEvidence,
    bool HasCompleteFormatContract,
    bool HasViewerRecognizedHdr10StaticMetadata,
    bool HasWindowsManualViewerValidation,
    IReadOnlyList<string> Blockers)
{
    public bool IsComplete =>
        HasArtifacts
        && HasCompleteTargetAwareHdrEvidence
        && HasCompleteFormatContract
        && HasViewerRecognizedHdr10StaticMetadata
        && HasWindowsManualViewerValidation
        && Blockers.Count == 0;

    public static Hdr10JxrViewerValidationEvidence FromArtifacts(
        IEnumerable<OutputValidationSessionArtifact> artifacts)
    {
        ArgumentNullException.ThrowIfNull(artifacts);

        var artifactArray = artifacts
            .Where(artifact => artifact.CoversProfileOutputTarget(OutputProfileKind.Hdr10Pq, OutputTarget.Folder))
            .ToArray();
        var evaluatedProfile = OutputValidationSessionArtifact.ApplyAllTo(
            OutputProfileContract.Hdr10Pq with
            {
                IsExecutable = true,
                FidelityMode = OutputFidelityMode.HdrPreserved,
            },
            artifactArray,
            OutputTarget.Folder);
        var viewerEvidence = evaluatedProfile.ViewerEvidence.ToArray();
        var hasArtifacts = artifactArray.Length > 0;
        var hasCompleteTargetAwareHdrEvidence = artifactArray.Any(ArtifactHasCompleteTargetAwareHdrEvidence);
        var hasCompleteFormatContract = evaluatedProfile.HasCompleteFormatContract;
        var metadataBlockers = viewerEvidence
            .Where(viewer => viewer.Hdr10MetadataStatus is not OutputCompatibilityEvidenceStatus.Pass)
            .Select(viewer => viewer.Name)
            .ToArray();
        var manualViewerBlockers = viewerEvidence
            .Where(viewer =>
                viewer.ArtifactHandlingStatus is not OutputCompatibilityEvidenceStatus.Pass
                || viewer.VisualMatchStatus is not OutputCompatibilityEvidenceStatus.Pass
                || viewer.HdrPreservationStatus is not OutputCompatibilityEvidenceStatus.Pass
                || viewer.Hdr10MetadataStatus is not OutputCompatibilityEvidenceStatus.Pass)
            .Select(viewer => viewer.Name)
            .ToArray();
        var blockers = new List<string>();

        if (!hasArtifacts)
        {
            blockers.Add("No folder-output validation artifacts were loaded for the HDR10 JXR path.");
        }

        if (!hasCompleteTargetAwareHdrEvidence)
        {
            blockers.Add("Complete target-aware HDR evidence is missing.");
        }

        if (!hasCompleteFormatContract)
        {
            blockers.Add("Complete HDR10 JXR format contract evidence is missing.");
        }

        if (metadataBlockers.Length > 0)
        {
            blockers.Add($"Viewer-recognized HDR10 static metadata evidence is missing for {FormatNames(metadataBlockers)}.");
        }

        if (manualViewerBlockers.Length > 0)
        {
            blockers.Add($"Windows manual viewer validation is incomplete for {FormatNames(manualViewerBlockers)}.");
        }

        return new Hdr10JxrViewerValidationEvidence(
            hasArtifacts,
            hasCompleteTargetAwareHdrEvidence,
            hasCompleteFormatContract,
            metadataBlockers.Length == 0,
            manualViewerBlockers.Length == 0,
            blockers);
    }

    private static bool ArtifactHasCompleteTargetAwareHdrEvidence(OutputValidationSessionArtifact artifact) =>
        artifact.TargetHdrEvidence is { } targetEvidence
        && !targetEvidence.GetMissingFields().Any();

    private static string FormatNames(IReadOnlyList<string> names) =>
        names.Count == 0 ? "named viewers" : string.Join(", ", names);
}
