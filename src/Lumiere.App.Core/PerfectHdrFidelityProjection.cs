using Lumiere.Graphics.Hdr;
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
        return ProjectOutputProfileCore(contract, readiness: null);
    }

    public static OutputProfileProjection ProjectOutputProfile(
        OutputProfileContract contract,
        PreviewReadinessStatus? readiness)
    {
        ArgumentNullException.ThrowIfNull(contract);
        return ProjectOutputProfileCore(contract, readiness);
    }

    public static OutputProfileProjection ProjectOutputProfile(
        OutputProfileContract contract,
        PreviewReadinessStatus? readiness,
        OutputProfileExecutionCapabilities executionCapabilities)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(executionCapabilities);
        var effectiveContract = executionCapabilities.SelectEffectiveProfile(contract);
        var requestedProjection = ProjectOutputProfileCore(SelectRuntimeClaimContract(contract, effectiveContract), readiness);
        if (effectiveContract.Kind == contract.Kind)
        {
            return requestedProjection;
        }

        var effectiveProjection = ProjectOutputProfileCore(effectiveContract, readiness);
        return requestedProjection with
        {
            StatusLabel = "Fallback",
            Detail = $"{requestedProjection.Detail} Runtime output uses {effectiveContract.Label} compatibility fallback because the selected profile is not executable in this build.",
            FidelityClaim = effectiveProjection.FidelityClaim,
        };
    }

    private static OutputProfileProjection ProjectOutputProfileCore(
        OutputProfileContract contract,
        PreviewReadinessStatus? readiness)
    {
        return contract.Kind switch
        {
            OutputProfileKind.Hdr10Pq => CreateOutputProfile(
                contract,
                "Validate",
                "Requires profile contract, metadata policy, supported viewer evidence, and Windows validation.",
                isReadOnly: true,
                readiness),
            OutputProfileKind.DisplayP3 => CreateOutputProfile(
                contract,
                "Build",
                "Wide-gamut output is visible as intent, but not selectable as a fidelity claim yet.",
                isReadOnly: true,
                readiness),
            _ => CreateOutputProfile(
                contract,
                "Compat",
                "Compatibility output; useful fallback, not the public release target.",
                isReadOnly: false,
                readiness),
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

    public static OutputProfileProjection ProjectOutputProfile(
        OutputProfileContract contract,
        IEnumerable<OutputValidationSessionArtifact> artifacts,
        PreviewReadinessStatus? readiness)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(artifacts);
        return ProjectOutputProfile(
            OutputValidationSessionArtifact.ApplyAllTo(contract, artifacts),
            readiness);
    }

    public static OutputProfileProjection ProjectOutputProfile(
        OutputProfileContract contract,
        IEnumerable<OutputValidationSessionArtifact> artifacts,
        PreviewReadinessStatus? readiness,
        OutputProfileExecutionCapabilities executionCapabilities)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(artifacts);
        ArgumentNullException.ThrowIfNull(executionCapabilities);
        return ProjectOutputProfile(
            OutputValidationSessionArtifact.ApplyAllTo(contract, artifacts),
            readiness,
            executionCapabilities);
    }

    public static ValidationPanelProjection ProjectValidation(ValidationRecordProjection? record = null) =>
        ProjectValidation(OutputProfileContract.SrgbCompatibilityPng, record);

    public static ValidationPanelProjection ProjectValidation(
        OutputProfileContract outputProfile,
        ValidationRecordProjection? record = null,
        PreviewReadinessStatus? readiness = null)
    {
        ArgumentNullException.ThrowIfNull(outputProfile);
        return ProjectValidationCore(outputProfile, readiness, targetHdrEvidence: null, record);
    }

    public static ValidationPanelProjection ProjectValidation(
        OutputProfileContract outputProfile,
        OutputProfileExecutionCapabilities executionCapabilities,
        ValidationRecordProjection? record = null,
        PreviewReadinessStatus? readiness = null)
    {
        ArgumentNullException.ThrowIfNull(outputProfile);
        ArgumentNullException.ThrowIfNull(executionCapabilities);
        var effectiveProfile = executionCapabilities.SelectEffectiveProfile(outputProfile);
        return ProjectValidationCore(
            SelectRuntimeClaimContract(outputProfile, effectiveProfile),
            readiness,
            targetHdrEvidence: null,
            record);
    }

    public static ValidationPanelProjection ProjectValidation(
        OutputProfileContract outputProfile,
        OutputValidationSessionArtifact artifact,
        ValidationRecordProjection? record = null,
        PreviewReadinessStatus? readiness = null)
    {
        ArgumentNullException.ThrowIfNull(outputProfile);
        ArgumentNullException.ThrowIfNull(artifact);
        return ProjectValidationCore(
            artifact.ApplyTo(outputProfile),
            readiness,
            SelectCompleteTargetHdrEvidence([artifact]),
            record);
    }

    public static ValidationPanelProjection ProjectValidation(
        OutputProfileContract outputProfile,
        OutputValidationSessionArtifact artifact,
        OutputProfileExecutionCapabilities executionCapabilities,
        ValidationRecordProjection? record = null,
        PreviewReadinessStatus? readiness = null)
    {
        ArgumentNullException.ThrowIfNull(outputProfile);
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(executionCapabilities);
        var requestedProfile = artifact.ApplyTo(outputProfile);
        var effectiveProfile = executionCapabilities.SelectEffectiveProfile(requestedProfile);
        return ProjectValidationCore(
            SelectRuntimeClaimContract(requestedProfile, effectiveProfile),
            readiness,
            SelectCompleteTargetHdrEvidence([artifact]),
            record);
    }

    public static ValidationPanelProjection ProjectValidation(
        OutputProfileContract outputProfile,
        IEnumerable<OutputValidationSessionArtifact> artifacts,
        ValidationRecordProjection? record = null,
        PreviewReadinessStatus? readiness = null)
    {
        ArgumentNullException.ThrowIfNull(outputProfile);
        ArgumentNullException.ThrowIfNull(artifacts);
        var artifactArray = artifacts.ToArray();
        return ProjectValidationCore(
            OutputValidationSessionArtifact.ApplyAllTo(outputProfile, artifactArray),
            readiness,
            SelectCompleteTargetHdrEvidence(artifactArray),
            record);
    }

    public static ValidationPanelProjection ProjectValidation(
        OutputProfileContract outputProfile,
        IEnumerable<OutputValidationSessionArtifact> artifacts,
        OutputProfileExecutionCapabilities executionCapabilities,
        ValidationRecordProjection? record = null,
        PreviewReadinessStatus? readiness = null)
    {
        ArgumentNullException.ThrowIfNull(outputProfile);
        ArgumentNullException.ThrowIfNull(artifacts);
        ArgumentNullException.ThrowIfNull(executionCapabilities);
        var artifactArray = artifacts.ToArray();
        var requestedProfile = OutputValidationSessionArtifact.ApplyAllTo(outputProfile, artifactArray);
        var effectiveProfile = executionCapabilities.SelectEffectiveProfile(requestedProfile);
        return ProjectValidationCore(
            SelectRuntimeClaimContract(requestedProfile, effectiveProfile),
            readiness,
            SelectCompleteTargetHdrEvidence(artifactArray),
            record);
    }

    private static OutputProfileContract SelectRuntimeClaimContract(
        OutputProfileContract requestedProfile,
        OutputProfileContract effectiveProfile) =>
        effectiveProfile.Kind == requestedProfile.Kind
            ? effectiveProfile
            : requestedProfile with
            {
                IsExecutable = false,
                FidelityMode = OutputFidelityMode.Unvalidated,
            };

    private static ValidationPanelProjection ProjectValidationCore(
        OutputProfileContract outputProfile,
        PreviewReadinessStatus? readiness,
        TargetAwareHdrValidationEvidence? targetHdrEvidence,
        ValidationRecordProjection? record)
    {
        var viewerMatrix = outputProfile.ViewerEvidence.Select(ProjectViewerEvidence).ToArray();
        return new(
            ReleaseTarget,
            "Public release waits for evidence; SDR compatibility remains fallback only.",
            [
                ProjectTargetAwareHdrRow(readiness, targetHdrEvidence),
                ProjectVisualMatchRow(outputProfile),
                ProjectHdrPreservedProfileRow(outputProfile),
                ProjectTargetAppMatrixRow(viewerMatrix),
            ],
            "Named viewers must prove artifact handling, visual match, and fidelity separately.",
            viewerMatrix,
            record ?? ProjectValidationRecord(null));
    }

    private static ValidationEvidenceRowProjection ProjectVisualMatchRow(OutputProfileContract outputProfile)
    {
        var evidence = outputProfile.EvaluateEvidence();
        if (evidence.AllowsVisualMatchClaim)
        {
            return new ValidationEvidenceRowProjection(
                "Visual-match output",
                ValidationEvidenceStatus.Pass,
                $"{evidence.VisualMatchGateDetail} QQ-style gray, white, and highlight checks remain the visual benchmark.");
        }

        var blockers = outputProfile.ViewerEvidence
            .Where(viewer =>
                viewer.ArtifactHandlingStatus is not OutputCompatibilityEvidenceStatus.Pass
                || viewer.VisualMatchStatus is not OutputCompatibilityEvidenceStatus.Pass)
            .ToArray();
        var status = blockers.Length == outputProfile.ViewerEvidence.Count
            ? ValidationEvidenceStatus.NotRun
            : blockers.Any(viewer =>
                viewer.ArtifactHandlingStatus is OutputCompatibilityEvidenceStatus.Fail
                || viewer.VisualMatchStatus is OutputCompatibilityEvidenceStatus.Fail)
                    ? ValidationEvidenceStatus.Fail
                    : ValidationEvidenceStatus.Limited;
        var detail = status switch
        {
            ValidationEvidenceStatus.NotRun =>
                "Visual-match validation is not run for the selected profile. QQ-style gray, white, and highlight checks are the benchmark.",
            ValidationEvidenceStatus.Fail =>
                $"Visual-match evidence failed for {FormatViewerNames(blockers.Select(viewer => viewer.Name))}. QQ-style gray, white, and highlight checks are the benchmark.",
            _ =>
                $"Visual-match evidence is missing for {FormatViewerNames(blockers.Select(viewer => viewer.Name))}. QQ-style gray, white, and highlight checks are the benchmark.",
        };

        return new ValidationEvidenceRowProjection(
            "Visual-match output",
            status,
            detail);
    }

    private static ValidationEvidenceRowProjection ProjectHdrPreservedProfileRow(OutputProfileContract outputProfile)
    {
        var evidence = outputProfile.EvaluateEvidence();
        if (evidence.AllowsHdrPreservedClaim)
        {
            return new ValidationEvidenceRowProjection(
                "HDR-preserved profile",
                ValidationEvidenceStatus.Pass,
                "HDR-preserved profile evidence passed for the supported path, including format contract, named viewer HDR preservation, and HDR10 metadata recognition.");
        }

        if (outputProfile.FormatContract.TargetAppAssumption is OutputTargetAppAssumption.RequiresHdrViewerValidation
            && outputProfile.HasCompleteFormatContract)
        {
            var hasFailedViewer = outputProfile.ViewerEvidence.Any(viewer =>
                viewer.ArtifactHandlingStatus is OutputCompatibilityEvidenceStatus.Fail
                || viewer.VisualMatchStatus is OutputCompatibilityEvidenceStatus.Fail
                || viewer.HdrPreservationStatus is OutputCompatibilityEvidenceStatus.Fail
                || viewer.Hdr10MetadataStatus is OutputCompatibilityEvidenceStatus.Fail);
            return new ValidationEvidenceRowProjection(
                "HDR-preserved profile",
                hasFailedViewer ? ValidationEvidenceStatus.Fail : ValidationEvidenceStatus.Limited,
                hasFailedViewer
                    ? $"{evidence.HdrPreservedGateDetail} HDR-preserved profile cannot pass while any named viewer evidence has failed."
                    : "Windows manual format contract evidence is recorded for this profile; executable output, target-aware readiness, named viewer HDR preservation, and HDR10 metadata recognition gates must still pass before any HDR-preserved claim.");
        }

        return new ValidationEvidenceRowProjection(
            "HDR-preserved profile",
            ValidationEvidenceStatus.NotRun,
            "At least one supported profile must pass before public release.");
    }

    private static ValidationEvidenceRowProjection ProjectTargetAppMatrixRow(
        IReadOnlyList<ValidationViewerMatrixRowProjection> viewerMatrix)
    {
        if (viewerMatrix.Count == 0
            || viewerMatrix.All(viewer => viewer.Status is ValidationEvidenceStatus.NotRun))
        {
            return new ValidationEvidenceRowProjection(
                "Target app matrix",
                ValidationEvidenceStatus.NotRun,
                "Named viewers must separate artifact success from fidelity.");
        }

        if (viewerMatrix.Any(viewer => viewer.Status is ValidationEvidenceStatus.Fail))
        {
            return new ValidationEvidenceRowProjection(
                "Target app matrix",
                ValidationEvidenceStatus.Fail,
                $"Target app matrix failed for {FormatViewerNames(viewerMatrix.Where(viewer => viewer.Status is ValidationEvidenceStatus.Fail).Select(viewer => viewer.Name))}.");
        }

        if (viewerMatrix.All(viewer =>
            viewer.Status is ValidationEvidenceStatus.Pass
            || viewer.Status is ValidationEvidenceStatus.NotApplicable))
        {
            return new ValidationEvidenceRowProjection(
                "Target app matrix",
                ValidationEvidenceStatus.Pass,
                "All named target apps have complete viewer evidence for the selected profile.");
        }

        return new ValidationEvidenceRowProjection(
            "Target app matrix",
            ValidationEvidenceStatus.Limited,
            $"Target app matrix is missing complete evidence for {FormatViewerNames(viewerMatrix.Where(viewer => viewer.Status is not ValidationEvidenceStatus.Pass and not ValidationEvidenceStatus.NotApplicable).Select(viewer => viewer.Name))}.");
    }

    private static TargetAwareHdrValidationEvidence? SelectCompleteTargetHdrEvidence(
        IEnumerable<OutputValidationSessionArtifact> artifacts) =>
        artifacts
            .Select(artifact => artifact.TargetHdrEvidence)
            .FirstOrDefault(evidence => evidence is not null
                && !evidence.GetMissingFields().Any());

    private static ValidationEvidenceRowProjection ProjectTargetAwareHdrRow(
        PreviewReadinessStatus? readiness,
        TargetAwareHdrValidationEvidence? targetHdrEvidence)
    {
        if (targetHdrEvidence is not null)
        {
            return new ValidationEvidenceRowProjection(
                "Target-aware HDR",
                ValidationEvidenceStatus.Limited,
                $"Target-aware HDR artifact evidence is present (match={targetHdrEvidence.MatchKind}, state={targetHdrEvidence.HdrState}); Windows manual validation across mixed HDR/SDR monitor setups is still required.");
        }

        if (readiness?.Reason is PreviewReadinessReason.TargetDisplayUnresolved)
        {
            var matchEvidence = ExtractDisplayMatchEvidence(readiness.TechnicalDetail);
            var detail = string.IsNullOrEmpty(matchEvidence)
                ? "HDR readiness is unvalidated for the selected capture target because display capability could not be matched to a DXGI output; mixed HDR/SDR monitor evidence is still required."
                : $"HDR readiness is unvalidated for the selected capture target because display capability could not be matched to a DXGI output ({matchEvidence}); mixed HDR/SDR monitor evidence is still required.";

            return new ValidationEvidenceRowProjection(
                "Target-aware HDR",
                ValidationEvidenceStatus.NotRun,
                detail);
        }

        var resolvedMatchEvidence = ExtractDisplayMatchEvidence(readiness?.TechnicalDetail);
        if (!string.IsNullOrEmpty(resolvedMatchEvidence))
        {
            return new ValidationEvidenceRowProjection(
                "Target-aware HDR",
                ValidationEvidenceStatus.Limited,
                $"Target-aware display output evidence is present ({resolvedMatchEvidence}); Windows manual validation across mixed HDR/SDR monitor setups is still required.");
        }

        return new ValidationEvidenceRowProjection(
            "Target-aware HDR",
            ValidationEvidenceStatus.NotRun,
            "Mixed HDR/SDR monitor evidence is required.");
    }

    private static string ExtractDisplayMatchEvidence(string? technicalDetail)
    {
        if (string.IsNullOrWhiteSpace(technicalDetail))
        {
            return string.Empty;
        }

        const string displayMatchPrefix = "display match=";
        var displayMatchIndex = technicalDetail.IndexOf(displayMatchPrefix, StringComparison.OrdinalIgnoreCase);
        if (displayMatchIndex >= 0)
        {
            return FormatMatchEvidence(
                technicalDetail[(displayMatchIndex + displayMatchPrefix.Length)..]);
        }

        const string matchPrefix = "match=";
        var matchIndex = technicalDetail.IndexOf(matchPrefix, StringComparison.OrdinalIgnoreCase);
        return matchIndex < 0
            ? string.Empty
            : FormatMatchEvidence(technicalDetail[(matchIndex + matchPrefix.Length)..]);
    }

    private static string FormatMatchEvidence(string value)
    {
        var matchKind = new string(
            value
                .TakeWhile(character => char.IsLetterOrDigit(character))
                .ToArray());

        return string.IsNullOrWhiteSpace(matchKind)
            ? string.Empty
            : $"match={matchKind}";
    }

    private static string FormatViewerNames(IEnumerable<string> viewerNames)
    {
        var names = viewerNames.ToArray();
        return names.Length == 0 ? "named viewers" : string.Join(", ", names);
    }

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

    public static ValidationRecordProjection ProjectValidationRecord(
        string? buildVersion,
        OutputValidationArtifactSnapshot validationSnapshot)
    {
        ArgumentNullException.ThrowIfNull(validationSnapshot);

        var baseline = ProjectValidationRecord(buildVersion);
        if (validationSnapshot.HasLoadIssues)
        {
            var firstIssue = validationSnapshot.LoadIssues[0];
            return baseline with
            {
                WindowsManualValidationStatus = ValidationEvidenceStatus.Limited,
                WindowsManualValidationDetail =
                    $"{validationSnapshot.Artifacts.Count} output validation artifact(s) loaded, but {validationSnapshot.LoadIssues.Count} file(s) were ignored. Fix ignored JSON/schema files before counting Windows manual output evidence. First issue: {Path.GetFileName(firstIssue.Path)}: {firstIssue.Detail}",
                EvidenceDocumentPath = "docs/validation/output-validation.md",
            };
        }

        if (validationSnapshot.HasArtifacts)
        {
            return baseline with
            {
                WindowsManualValidationStatus = ValidationEvidenceStatus.Limited,
                WindowsManualValidationDetail =
                    $"{validationSnapshot.Artifacts.Count} output validation artifact(s) loaded for this session. Release gates still require target-aware HDR, visual match, HDR preservation, and HDR10 metadata recognition to pass.",
                EvidenceDocumentPath = "docs/validation/output-validation.md",
            };
        }

        return baseline;
    }

    public static string NormalizeExportColorFormat(string? exportColorFormat)
        => OutputProfileContract.FromSettingsValue(exportColorFormat).Label;

    private static OutputProfileProjection CreateOutputProfile(
        OutputProfileContract contract,
        string statusLabel,
        string detail,
        bool isReadOnly,
        PreviewReadinessStatus? readiness = null) =>
        new(
            contract.Label,
            statusLabel,
            detail,
            isReadOnly,
            new OutputProfileContractProjection(
                FormatPixelFormat(contract.FormatContract.SourcePixelFormat, isDestination: false),
                FormatPixelFormat(contract.FormatContract.DestinationPixelFormat, isDestination: true),
                FormatTransferFunction(contract.FormatContract.TransferFunction),
                FormatColorPrimaries(contract.FormatContract.ColorPrimaries),
                FormatConversionPolicy(contract.FormatContract.ConversionPolicy),
                FormatMetadataPolicy(contract.FormatContract.MetadataPolicy),
                FormatTargetAppAssumption(contract.FormatContract.TargetAppAssumption),
                contract.SourceFormatPolicy,
                contract.DestinationFormatPolicy,
                contract.ConversionPolicy,
                contract.MetadataPolicy,
                contract.ViewerCompatibilityPolicy),
            CreateFidelityClaim(contract, readiness));

    private static FidelityClaimProjection CreateFidelityClaim(
        OutputProfileContract contract,
        PreviewReadinessStatus? readiness) =>
        contract.FidelityMode switch
        {
            OutputFidelityMode.SdrCompatible => new FidelityClaimProjection(
                FidelityClaimKind.Converted,
                "Converted",
                "Output is optimized for compatibility, not HDR preservation.",
                MainPanelTrustIcon.InfoCircle,
                MainPanelTrustSeverity.Warning),
            OutputFidelityMode.VisualMatch => CreateVisualMatchClaim(contract, readiness),
            OutputFidelityMode.HdrPreserved => CreateHdrPreservedClaim(contract, readiness),
            _ => new FidelityClaimProjection(
                FidelityClaimKind.Unvalidated,
                "Unvalidated",
                "No fidelity claim is made for this path.",
                MainPanelTrustIcon.ErrorCircle,
                MainPanelTrustSeverity.Error),
        };

    private static FidelityClaimProjection CreateVisualMatchClaim(
        OutputProfileContract contract,
        PreviewReadinessStatus? readiness)
    {
        if (RequiresTargetAwareReadiness(readiness))
        {
            return TargetAwareReadinessBlockedClaim();
        }

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

    private static FidelityClaimProjection CreateHdrPreservedClaim(
        OutputProfileContract contract,
        PreviewReadinessStatus? readiness)
    {
        if (RequiresTargetAwareReadiness(readiness))
        {
            return TargetAwareReadinessBlockedClaim();
        }

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

    private static bool RequiresTargetAwareReadiness(PreviewReadinessStatus? readiness) =>
        readiness?.Reason is PreviewReadinessReason.TargetDisplayUnresolved;

    private static FidelityClaimProjection TargetAwareReadinessBlockedClaim() =>
        new(
            FidelityClaimKind.Unvalidated,
            "Unvalidated",
            "Fidelity claim blocked: target-aware HDR readiness is unvalidated for the selected capture target.",
            MainPanelTrustIcon.ErrorCircle,
            MainPanelTrustSeverity.Error);

    private static string FormatPixelFormat(OutputPixelFormat value, bool isDestination) =>
        value switch
        {
            OutputPixelFormat.R16G16B16A16Float => "R16G16B16A16 float",
            OutputPixelFormat.Rgba8UnsignedNormalized => isDestination ? "RGBA8 sRGB" : "RGBA8 unsigned normalized",
            _ => "Not defined",
        };

    private static string FormatTransferFunction(OutputTransferFunction value) =>
        value switch
        {
            OutputTransferFunction.Srgb => "sRGB",
            OutputTransferFunction.PqSt2084 => "PQ ST.2084",
            _ => "Not defined",
        };

    private static string FormatColorPrimaries(OutputColorPrimaries value) =>
        value switch
        {
            OutputColorPrimaries.Bt709 => "BT.709",
            OutputColorPrimaries.Bt2020 => "BT.2020",
            OutputColorPrimaries.DisplayP3 => "Display P3",
            _ => "Not defined",
        };

    private static string FormatConversionPolicy(OutputConversionPolicy value) =>
        value switch
        {
            OutputConversionPolicy.SdrToneMapped => "SDR tone-mapped",
            OutputConversionPolicy.PreserveHdrWithDefinedToneMapping => "HDR-preserving defined tone mapping",
            _ => "Required but undefined",
        };

    private static string FormatMetadataPolicy(OutputMetadataPolicy value) =>
        value switch
        {
            OutputMetadataPolicy.NoHdrMetadata => "No HDR metadata",
            OutputMetadataPolicy.AttachHdr10StaticMetadata => "Attach HDR10 static metadata",
            _ => "Required but undefined",
        };

    private static string FormatTargetAppAssumption(OutputTargetAppAssumption value) =>
        value switch
        {
            OutputTargetAppAssumption.CompatibilityFirst => "Compatibility-first target apps",
            OutputTargetAppAssumption.RequiresHdrViewerValidation => "Requires HDR viewer validation",
            OutputTargetAppAssumption.RequiresWideGamutViewerValidation => "Requires wide-gamut viewer validation",
            _ => "Not defined",
        };

    private static ValidationViewerMatrixRowProjection ProjectViewerEvidence(OutputViewerCompatibilityEvidence evidence) =>
        new(
            evidence.Name,
            MapEvidenceStatus(evidence.ArtifactHandlingStatus),
            MapEvidenceStatus(evidence.VisualMatchStatus),
            MapEvidenceStatus(evidence.HdrPreservationStatus),
            MapEvidenceStatus(evidence.Hdr10MetadataStatus),
            $"Artifact: {FormatEvidenceStatus(evidence.ArtifactHandlingStatus)}. "
                + $"Visual match: {FormatEvidenceStatus(evidence.VisualMatchStatus)}. "
                + $"HDR preservation: {FormatEvidenceStatus(evidence.HdrPreservationStatus)}. "
                + $"HDR10 metadata: {FormatEvidenceStatus(evidence.Hdr10MetadataStatus)}. "
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
    string SourcePixelFormatLabel,
    string DestinationPixelFormatLabel,
    string TransferFunctionLabel,
    string ColorPrimariesLabel,
    string ConversionPolicyLabel,
    string MetadataPolicyLabel,
    string TargetAppAssumptionLabel,
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
    ValidationEvidenceStatus Hdr10MetadataStatus,
    string Detail)
{
    public ValidationEvidenceStatus Status =>
        CombineStatus(ArtifactHandlingStatus, VisualMatchStatus, HdrPreservationStatus, Hdr10MetadataStatus);

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
