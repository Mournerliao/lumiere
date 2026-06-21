namespace Lumiere.App;

public static class PerfectHdrFidelityProjection
{
    public const string ReleaseTarget = "Perfect HDR Fidelity Public Release";

    public static OutputProfileProjection ProjectOutputProfile(string? exportColorFormat)
    {
        var normalized = NormalizeExportColorFormat(exportColorFormat);
        return normalized switch
        {
            "HDR10" => new OutputProfileProjection(
                "HDR10",
                "Validate",
                "Requires profile contract, metadata policy, supported viewer evidence, and Windows validation.",
                IsReadOnly: true,
                new OutputProfileContractProjection(
                    "FP16/scRGB capture source",
                    "HDR10 output contract pending implementation",
                    "Transfer, tone mapping, and gamut mapping policy must be defined before use.",
                    "HDR10 metadata policy is required before this profile can make a fidelity claim.",
                    "Named target-app compatibility matrix is required."),
                new FidelityClaimProjection(
                    FidelityClaimKind.Unvalidated,
                    "Unvalidated",
                    "No fidelity claim is made for this path.",
                    MainPanelTrustIcon.ErrorCircle,
                    MainPanelTrustSeverity.Error)),
            "P3" => new OutputProfileProjection(
                "P3",
                "Build",
                "Wide-gamut output is visible as intent, but not selectable as a fidelity claim yet.",
                IsReadOnly: true,
                new OutputProfileContractProjection(
                    "FP16/scRGB capture source",
                    "Display P3 output contract pending implementation",
                    "Wide-gamut conversion policy must be specified before use.",
                    "Color profile and metadata attachment policy are not validated.",
                    "Target-app compatibility matrix is not run."),
                new FidelityClaimProjection(
                    FidelityClaimKind.Unvalidated,
                    "Unvalidated",
                    "No fidelity claim is made for this path.",
                    MainPanelTrustIcon.ErrorCircle,
                    MainPanelTrustSeverity.Error)),
            _ => new OutputProfileProjection(
                "sRGB",
                "Compat",
                "Compatibility output; useful fallback, not the public release target.",
                IsReadOnly: false,
                new OutputProfileContractProjection(
                    "FP16/scRGB capture source",
                    "Compatibility-converted sRGB artifact",
                    "Output is converted for common SDR destinations.",
                    "No HDR metadata is attached to the compatibility artifact.",
                    "Compatibility-oriented target-app checks are still required."),
                new FidelityClaimProjection(
                    FidelityClaimKind.Converted,
                    "Converted",
                    "Output is optimized for compatibility, not HDR preservation.",
                    MainPanelTrustIcon.InfoCircle,
                    MainPanelTrustSeverity.Warning)),
        };
    }

    public static ValidationPanelProjection ProjectValidation(ValidationRecordProjection? record = null) =>
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
            [
                new(
                    "Microsoft Paint",
                    ValidationEvidenceStatus.NotRun,
                    "Paste/open artifact handling, visual match, and fidelity are not validated."),
                new(
                    "Windows Photos",
                    ValidationEvidenceStatus.NotRun,
                    "Open artifact handling, visual match, and fidelity are not validated."),
                new(
                    "Chromium browsers",
                    ValidationEvidenceStatus.NotRun,
                    "Paste/drop artifact handling, visual match, and fidelity are not validated."),
            ],
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
    {
        var normalized = exportColorFormat?.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            return "sRGB";
        }

        return normalized.Equals("HDR10", StringComparison.OrdinalIgnoreCase)
            ? "HDR10"
            : normalized.Equals("P3", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Wide", StringComparison.OrdinalIgnoreCase)
                ? "P3"
                : "sRGB";
    }
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
    ValidationEvidenceStatus Status,
    string Detail);

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
