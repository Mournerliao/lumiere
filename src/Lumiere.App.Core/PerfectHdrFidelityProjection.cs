using Lumiere.Graphics.Output;

namespace Lumiere.App;

public static class PerfectHdrFidelityProjection
{
    public const string ReleaseTarget = "Perfect HDR Fidelity Public Release";

    public static OutputProfileProjection ProjectOutputProfile(string? exportColorFormat)
    {
        var contract = OutputProfileContract.FromSettingsValue(exportColorFormat);
        return contract.Kind switch
        {
            OutputProfileKind.Hdr10Pq => CreateOutputProfile(
                contract,
                "Validate",
                "Requires profile contract, metadata policy, supported viewer evidence, and Windows validation.",
                isReadOnly: true),
            OutputProfileKind.DisplayP3 => CreateOutputProfile(
                contract,
                "Build",
                "Wide-gamut output is visible as intent, but not selectable as a fidelity claim yet.",
                isReadOnly: true),
            _ => CreateOutputProfile(
                contract,
                "Compat",
                "Compatibility output; useful fallback, not the public release target.",
                isReadOnly: false),
        };
    }

    public static OutputProfileProjection ProjectOutputProfile(OutputProfileContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return contract.Kind switch
        {
            OutputProfileKind.Hdr10Pq => CreateOutputProfile(
                contract,
                "Validate",
                "Requires profile contract, metadata policy, supported viewer evidence, and Windows validation.",
                isReadOnly: true),
            OutputProfileKind.DisplayP3 => CreateOutputProfile(
                contract,
                "Build",
                "Wide-gamut output is visible as intent, but not selectable as a fidelity claim yet.",
                isReadOnly: true),
            _ => CreateOutputProfile(
                contract,
                "Compat",
                "Compatibility output; useful fallback, not the public release target.",
                isReadOnly: false),
        };
    }

    public static OutputProfileProjection ProjectOutputProfile(
        OutputProfileContract contract,
        IEnumerable<OutputValidationSessionArtifact> artifacts)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(artifacts);
        return ProjectOutputProfile(OutputValidationSessionArtifact.ApplyAllTo(contract, artifacts));
    }

    public static ValidationPanelProjection ProjectValidation(ValidationRecordProjection? record = null) =>
        ProjectValidation(OutputProfileContract.SrgbCompatibilityPng, record);

    public static ValidationPanelProjection ProjectValidation(
        OutputProfileContract outputProfile,
        ValidationRecordProjection? record = null)
    {
        ArgumentNullException.ThrowIfNull(outputProfile);
        return ProjectValidationCore(outputProfile, record);
    }

    public static ValidationPanelProjection ProjectValidation(
        OutputProfileContract outputProfile,
        OutputValidationSessionArtifact artifact,
        ValidationRecordProjection? record = null)
    {
        ArgumentNullException.ThrowIfNull(outputProfile);
        ArgumentNullException.ThrowIfNull(artifact);
        return ProjectValidationCore(artifact.ApplyTo(outputProfile), record);
    }

    public static ValidationPanelProjection ProjectValidation(
        OutputProfileContract outputProfile,
        IEnumerable<OutputValidationSessionArtifact> artifacts,
        ValidationRecordProjection? record = null)
    {
        ArgumentNullException.ThrowIfNull(outputProfile);
        ArgumentNullException.ThrowIfNull(artifacts);
        return ProjectValidationCore(OutputValidationSessionArtifact.ApplyAllTo(outputProfile, artifacts), record);
    }

    private static ValidationPanelProjection ProjectValidationCore(
        OutputProfileContract outputProfile,
        ValidationRecordProjection? record) =>
        new(
            ReleaseTarget,
            "Public release waits for evidence; SDR compatibility remains fallback only.",
            [
                new(
                    "Target-aware HDR",
                    ValidationEvidenceStatus.NotRun,
                    "Mixed HDR/SDR monitor evidence is required."),
                new(
                    "Visual-match output",
                    ValidationEvidenceStatus.Limited,
                    "QQ-style gray, white, and highlight checks are the benchmark."),
                new(
                    "HDR-preserved profile",
                    ValidationEvidenceStatus.NotRun,
                    "At least one supported profile must pass before public release."),
                new(
                    "Target app matrix",
                    ValidationEvidenceStatus.NotRun,
                    "Named viewers must separate artifact success from fidelity."),
            ],
            "Named viewers must prove artifact handling, visual match, and fidelity separately.",
            outputProfile.ViewerEvidence.Select(ProjectViewerEvidence).ToArray(),
            record ?? ProjectValidationRecord(null));

    public static ValidationRecordProjection ProjectValidationRecord(string? buildVersion)
    {
        var normalizedVersion = string.IsNullOrWhiteSpace(buildVersion)
            ? "unknown build"
            : buildVersion.Trim();
        var buildLabel = normalizedVersion.StartsWith("Build ", StringComparison.OrdinalIgnoreCase)
            ? normalizedVersion
            : $"Build {normalizedVersion}";

        return new ValidationRecordProjection(
            buildLabel,
            ValidationEvidenceStatus.Limited,
            "Windows CI restore, build, unit tests, and format gates can support implementation confidence only.",
            ValidationEvidenceStatus.NotRun,
            "Windows manual validation for HDR displays, target apps, mixed monitors, and visual match is not run.",
            "docs/validation/release-validation-checklist.md");
    }

    public static string NormalizeExportColorFormat(string? exportColorFormat)
        => OutputProfileContract.FromSettingsValue(exportColorFormat).Label;

    private static OutputProfileProjection CreateOutputProfile(
        OutputProfileContract contract,
        string statusLabel,
        string detail,
        bool isReadOnly) =>
        new(
            contract.Label,
            statusLabel,
            detail,
            isReadOnly,
            new OutputProfileContractProjection(
                contract.SourceFormatPolicy,
                contract.DestinationFormatPolicy,
                contract.ConversionPolicy,
                contract.MetadataPolicy,
                contract.ViewerCompatibilityPolicy),
            CreateFidelityClaim(contract));

    private static FidelityClaimProjection CreateFidelityClaim(OutputProfileContract contract) =>
        contract.FidelityMode switch
        {
            OutputFidelityMode.SdrCompatible => new FidelityClaimProjection(
                FidelityClaimKind.Converted,
                "Converted",
                "Output is optimized for compatibility, not HDR preservation.",
                MainPanelTrustIcon.InfoCircle,
                MainPanelTrustSeverity.Warning),
            OutputFidelityMode.VisualMatch => CreateVisualMatchClaim(contract),
            OutputFidelityMode.HdrPreserved => CreateHdrPreservedClaim(contract),
            _ => new FidelityClaimProjection(
                FidelityClaimKind.Unvalidated,
                "Unvalidated",
                "No fidelity claim is made for this path.",
                MainPanelTrustIcon.ErrorCircle,
                MainPanelTrustSeverity.Error),
        };

    private static FidelityClaimProjection CreateVisualMatchClaim(OutputProfileContract contract)
    {
        var evidence = contract.EvaluateEvidence();
        return evidence.AllowsVisualMatchClaim
            ? new FidelityClaimProjection(
                FidelityClaimKind.VisualMatch,
                "Visual match",
                "Output has visual-match validation for the supported path.",
                MainPanelTrustIcon.CheckmarkCircle,
                MainPanelTrustSeverity.Success)
            : new FidelityClaimProjection(
                FidelityClaimKind.Unvalidated,
                "Unvalidated",
                evidence.VisualMatchGateDetail,
                MainPanelTrustIcon.ErrorCircle,
                MainPanelTrustSeverity.Error);
    }

    private static FidelityClaimProjection CreateHdrPreservedClaim(OutputProfileContract contract)
    {
        var evidence = contract.EvaluateEvidence();
        return evidence.AllowsHdrPreservedClaim
            ? new FidelityClaimProjection(
                FidelityClaimKind.HdrPreserved,
                "HDR-preserved",
                "Output uses a validated HDR-preserved supported path.",
                MainPanelTrustIcon.CheckmarkCircle,
                MainPanelTrustSeverity.Success)
            : new FidelityClaimProjection(
                FidelityClaimKind.Unvalidated,
                "Unvalidated",
                evidence.HdrPreservedGateDetail,
                MainPanelTrustIcon.ErrorCircle,
                MainPanelTrustSeverity.Error);
    }

    private static ValidationViewerMatrixRowProjection ProjectViewerEvidence(OutputViewerCompatibilityEvidence evidence) =>
        new(
            evidence.Name,
            MapEvidenceStatus(evidence.ArtifactHandlingStatus),
            MapEvidenceStatus(evidence.VisualMatchStatus),
            MapEvidenceStatus(evidence.HdrPreservationStatus),
            $"Artifact: {FormatEvidenceStatus(evidence.ArtifactHandlingStatus)}. "
                + $"Visual match: {FormatEvidenceStatus(evidence.VisualMatchStatus)}. "
                + $"HDR preservation: {FormatEvidenceStatus(evidence.HdrPreservationStatus)}. "
                + "Fidelity evidence is separated by category. "
                + evidence.Detail);

    private static ValidationEvidenceStatus MapEvidenceStatus(OutputCompatibilityEvidenceStatus status) =>
        status switch
        {
            OutputCompatibilityEvidenceStatus.Pass => ValidationEvidenceStatus.Pass,
            OutputCompatibilityEvidenceStatus.Limited => ValidationEvidenceStatus.Limited,
            OutputCompatibilityEvidenceStatus.Fail => ValidationEvidenceStatus.Fail,
            OutputCompatibilityEvidenceStatus.NotApplicable => ValidationEvidenceStatus.NotApplicable,
            _ => ValidationEvidenceStatus.NotRun,
        };

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

public sealed record OutputProfileProjection(
    string Label,
    string StatusLabel,
    string Detail,
    bool IsReadOnly,
    OutputProfileContractProjection Contract,
    FidelityClaimProjection FidelityClaim);

public sealed record OutputProfileContractProjection(
    string SourcePolicy,
    string DestinationPolicy,
    string ConversionPolicy,
    string MetadataPolicy,
    string ViewerCompatibilityPolicy);

public sealed record FidelityClaimProjection(
    FidelityClaimKind Kind,
    string Label,
    string Detail,
    MainPanelTrustIcon Icon,
    MainPanelTrustSeverity Severity);

public enum FidelityClaimKind
{
    Converted = 0,
    VisualMatch,
    HdrPreserved,
    Unvalidated,
}

public sealed record ValidationPanelProjection(
    string ReleaseTarget,
    string Summary,
    IReadOnlyList<ValidationEvidenceRowProjection> Rows,
    string ViewerMatrixSummary,
    IReadOnlyList<ValidationViewerMatrixRowProjection> ViewerMatrix,
    ValidationRecordProjection Record);

public sealed record ValidationEvidenceRowProjection(
    string Label,
    ValidationEvidenceStatus Status,
    string Detail);

public sealed record ValidationViewerMatrixRowProjection(
    string Name,
    ValidationEvidenceStatus ArtifactHandlingStatus,
    ValidationEvidenceStatus VisualMatchStatus,
    ValidationEvidenceStatus HdrPreservationStatus,
    string Detail)
{
    public ValidationEvidenceStatus Status =>
        CombineStatus(ArtifactHandlingStatus, VisualMatchStatus, HdrPreservationStatus);

    private static ValidationEvidenceStatus CombineStatus(params ValidationEvidenceStatus[] statuses)
    {
        if (statuses.Any(status => status is ValidationEvidenceStatus.Fail))
        {
            return ValidationEvidenceStatus.Fail;
        }

        var applicable = statuses
            .Where(status => status is not ValidationEvidenceStatus.NotApplicable)
            .ToArray();
        if (applicable.Length == 0)
        {
            return ValidationEvidenceStatus.NotApplicable;
        }

        if (applicable.Any(status => status is ValidationEvidenceStatus.NotRun))
        {
            return ValidationEvidenceStatus.NotRun;
        }

        return applicable.Any(status => status is ValidationEvidenceStatus.Limited)
            ? ValidationEvidenceStatus.Limited
            : ValidationEvidenceStatus.Pass;
    }
}

public sealed record ValidationRecordProjection(
    string BuildLabel,
    ValidationEvidenceStatus AutomatedEvidenceStatus,
    string AutomatedEvidenceDetail,
    ValidationEvidenceStatus WindowsManualValidationStatus,
    string WindowsManualValidationDetail,
    string EvidenceDocumentPath);

public enum ValidationEvidenceStatus
{
    Pass = 0,
    Limited,
    Fail,
    NotRun,
    NotApplicable,
}
