using Lumiere.Capture;
using Lumiere.Graphics.Hdr;
using Lumiere.Graphics.Output;

namespace Lumiere.App;

public static class HdrAwareOutputProjection
{
    public const string ReleaseTarget = "HDR-aware MVP";

    public static OutputProfileProjection ProjectOutputProfile(string? exportColorFormat)
    {
        var contract = OutputProfileContract.FromSettingsValue(exportColorFormat);
        return contract.Kind switch
        {
            OutputProfileKind.Hdr10Pq => CreateOutputProfile(
                contract,
                "Validate",
                "HDR10 stays unavailable until profile contract, metadata policy, supported viewer evidence, and Windows validation are complete.",
                isReadOnly: true),
            OutputProfileKind.DisplayP3 => CreateOutputProfile(
                contract,
                "Build",
                "Wide-gamut output is shown for planning, but not available as a fidelity claim yet.",
                isReadOnly: true),
            _ => CreateOutputProfile(
                contract,
                "Compat",
                "Compatible output for the MVP; HDR-preserved export remains future work.",
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
        => ProjectOutputProfile(
            contract,
            readiness,
            executionCapabilities,
            OutputTarget.Folder);

    public static OutputProfileProjection ProjectOutputProfile(
        OutputProfileContract contract,
        PreviewReadinessStatus? readiness,
        OutputProfileExecutionCapabilities executionCapabilities,
        OutputTarget outputTarget)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(executionCapabilities);
        return outputTarget switch
        {
            OutputTarget.Clipboard when contract.Kind is not OutputProfileKind.SrgbCompatibilityPng =>
                ProjectClipboardCompatibilityProfile(contract, readiness),
            OutputTarget.Both when contract.Kind is not OutputProfileKind.SrgbCompatibilityPng =>
                ProjectMixedTargetProfile(contract, readiness, executionCapabilities),
            _ => ProjectFolderScopedProfile(contract, readiness, executionCapabilities),
        };
    }

    private static OutputProfileProjection ProjectFolderScopedProfile(
        OutputProfileContract contract,
        PreviewReadinessStatus? readiness,
        OutputProfileExecutionCapabilities executionCapabilities)
    {
        var effectiveContract = executionCapabilities.SelectEffectiveProfile(contract);
        var requestedContract = SelectRuntimeClaimContract(contract, effectiveContract);
        var requestedProjection = ProjectOutputProfileCore(requestedContract, readiness);
        var gate = executionCapabilities.DescribeGate(contract.Kind);
        var gatePresentation = DescribeGatePresentation(contract.Kind, gate, effectiveContract.Kind == contract.Kind);

        if (effectiveContract.Kind == contract.Kind)
        {
            return requestedProjection with
            {
                StatusLabel = gatePresentation.StatusLabel,
                Detail = gatePresentation.Detail,
                IsReadOnly = gatePresentation.IsReadOnly,
                Contract = CreateContractProjection(requestedContract, gatePresentation.StatusLabel),
            };
        }

        var effectiveProjection = ProjectOutputProfileCore(effectiveContract, readiness);
        return requestedProjection with
        {
            StatusLabel = gatePresentation.StatusLabel,
            Detail = $"{gatePresentation.Detail} Runtime output uses {effectiveContract.Label} compatibility fallback because the selected profile is not executable in this session.",
            IsReadOnly = gatePresentation.IsReadOnly,
            Contract = CreateContractProjection(requestedContract, gatePresentation.StatusLabel),
            FidelityClaim = effectiveProjection.FidelityClaim,
        };
    }

    private static OutputProfileProjection ProjectClipboardCompatibilityProfile(
        OutputProfileContract contract,
        PreviewReadinessStatus? readiness)
    {
        var requestedProjection = ProjectOutputProfileCore(
            contract with
            {
                IsExecutable = false,
                FidelityMode = OutputFidelityMode.Unvalidated,
            },
            readiness);
        var compatibilityProjection = ProjectOutputProfileCore(OutputProfileContract.SrgbCompatibilityPng, readiness);
        return requestedProjection with
        {
            StatusLabel = "Compat",
            Detail = $"Clipboard output stays on sRGB compatibility output for this session. {contract.Label} viewer evidence and format-contract progress do not promote the clipboard target into an HDR-preserved path.",
            IsReadOnly = true,
            Contract = CreateContractProjection(contract, "Compat"),
            FidelityClaim = compatibilityProjection.FidelityClaim,
        };
    }

    private static OutputProfileProjection ProjectMixedTargetProfile(
        OutputProfileContract contract,
        PreviewReadinessStatus? readiness,
        OutputProfileExecutionCapabilities executionCapabilities)
    {
        var folderProjection = ProjectFolderScopedProfile(contract, readiness, executionCapabilities);
        return folderProjection with
        {
            Detail = $"{folderProjection.Detail} Both-target output still keeps clipboard on sRGB compatibility fallback, so the combined session does not become one uniform {contract.Label} fidelity path.",
            FidelityClaim = new FidelityClaimProjection(
                FidelityClaimKind.Converted,
                "Converted",
                $"Both-target output can validate {contract.Label} for folder artifacts separately, but clipboard output still uses sRGB compatibility fallback for this session.",
                MainPanelTrustIcon.InfoCircle,
                MainPanelTrustSeverity.Warning),
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
                "HDR10 stays unavailable until profile contract, metadata policy, supported viewer evidence, and Windows validation are complete.",
                isReadOnly: true,
                readiness),
            OutputProfileKind.DisplayP3 => CreateOutputProfile(
                contract,
                "Build",
                "Wide-gamut output is shown for planning, but not available as a fidelity claim yet.",
                isReadOnly: true,
                readiness),
            _ => CreateOutputProfile(
                contract,
                "Compat",
                "Compatible output for the MVP; HDR-preserved export remains future work.",
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
        return ProjectOutputProfile(contract, artifacts, OutputTarget.Folder);
    }

    public static OutputProfileProjection ProjectOutputProfile(
        OutputProfileContract contract,
        IEnumerable<OutputValidationSessionArtifact> artifacts,
        OutputTarget outputTarget)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(artifacts);
        return ProjectOutputProfile(ApplyArtifactsForOutputTarget(contract, artifacts, outputTarget));
    }

    public static OutputProfileProjection ProjectOutputProfile(
        OutputProfileContract contract,
        IEnumerable<OutputValidationSessionArtifact> artifacts,
        PreviewReadinessStatus? readiness)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(artifacts);
        return ProjectOutputProfile(contract, artifacts, readiness, OutputTarget.Folder);
    }

    public static OutputProfileProjection ProjectOutputProfile(
        OutputProfileContract contract,
        IEnumerable<OutputValidationSessionArtifact> artifacts,
        PreviewReadinessStatus? readiness,
        OutputTarget outputTarget)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(artifacts);
        return ProjectOutputProfile(
            ApplyArtifactsForOutputTarget(contract, artifacts, outputTarget),
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
        return ProjectOutputProfile(contract, artifacts, readiness, executionCapabilities, OutputTarget.Folder);
    }

    public static OutputProfileProjection ProjectOutputProfile(
        OutputProfileContract contract,
        IEnumerable<OutputValidationSessionArtifact> artifacts,
        PreviewReadinessStatus? readiness,
        OutputProfileExecutionCapabilities executionCapabilities,
        OutputTarget outputTarget)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(artifacts);
        ArgumentNullException.ThrowIfNull(executionCapabilities);
        return ProjectOutputProfile(
            ApplyArtifactsForOutputTarget(contract, artifacts, outputTarget),
            readiness,
            executionCapabilities,
            outputTarget);
    }

    public static ValidationPanelProjection ProjectValidation(ValidationRecordProjection? record = null) =>
        ProjectValidation(OutputProfileContract.SrgbCompatibilityPng, record);

    public static ValidationPanelProjection ProjectValidation(
        OutputProfileContract outputProfile,
        ValidationRecordProjection? record = null,
        PreviewReadinessStatus? readiness = null,
        CaptureTarget? captureTarget = null)
    {
        ArgumentNullException.ThrowIfNull(outputProfile);
        return ProjectValidationCore(
            outputProfile,
            ProjectOutputProfile(outputProfile, readiness),
            readiness,
            captureTarget,
            targetHdrEvidence: null,
            artifacts: [],
            evidenceSummary: ValidationEvidenceSummaryProjection.Empty,
            record);
    }

    public static ValidationPanelProjection ProjectValidation(
        OutputProfileContract outputProfile,
        OutputProfileExecutionCapabilities executionCapabilities,
        ValidationRecordProjection? record = null,
        PreviewReadinessStatus? readiness = null,
        CaptureTarget? captureTarget = null)
    {
        ArgumentNullException.ThrowIfNull(outputProfile);
        ArgumentNullException.ThrowIfNull(executionCapabilities);
        var effectiveProfile = executionCapabilities.SelectEffectiveProfile(outputProfile);
        var outputProfileProjection = ProjectOutputProfile(outputProfile, readiness, executionCapabilities);
        return ProjectValidationCore(
            SelectRuntimeClaimContract(outputProfile, effectiveProfile),
            outputProfileProjection,
            readiness,
            captureTarget,
            targetHdrEvidence: null,
            artifacts: [],
            evidenceSummary: ValidationEvidenceSummaryProjection.Empty,
            record);
    }

    public static ValidationPanelProjection ProjectValidation(
        OutputProfileContract outputProfile,
        OutputValidationSessionArtifact artifact,
        ValidationRecordProjection? record = null,
        PreviewReadinessStatus? readiness = null,
        CaptureTarget? captureTarget = null)
    {
        ArgumentNullException.ThrowIfNull(outputProfile);
        ArgumentNullException.ThrowIfNull(artifact);
        var projectedProfile = ProjectOutputProfile(outputProfile, [artifact], readiness);
        return ProjectValidationCore(
            artifact.ApplyTo(outputProfile),
            projectedProfile,
            readiness,
            captureTarget,
            SelectCompleteTargetHdrEvidence([artifact]),
            [artifact],
            ProjectValidationEvidenceSummary([artifact]),
            record);
    }

    public static ValidationPanelProjection ProjectValidation(
        OutputProfileContract outputProfile,
        OutputValidationSessionArtifact artifact,
        OutputProfileExecutionCapabilities executionCapabilities,
        ValidationRecordProjection? record = null,
        PreviewReadinessStatus? readiness = null,
        CaptureTarget? captureTarget = null)
    {
        ArgumentNullException.ThrowIfNull(outputProfile);
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(executionCapabilities);
        var requestedProfile = artifact.ApplyTo(outputProfile);
        var effectiveProfile = executionCapabilities.SelectEffectiveProfile(requestedProfile);
        var projectedProfile = ProjectOutputProfile(outputProfile, [artifact], readiness, executionCapabilities);
        return ProjectValidationCore(
            SelectRuntimeClaimContract(requestedProfile, effectiveProfile),
            projectedProfile,
            readiness,
            captureTarget,
            SelectCompleteTargetHdrEvidence([artifact]),
            [artifact],
            ProjectValidationEvidenceSummary([artifact]),
            record);
    }

    public static ValidationPanelProjection ProjectValidation(
        OutputProfileContract outputProfile,
        IEnumerable<OutputValidationSessionArtifact> artifacts,
        ValidationRecordProjection? record = null,
        PreviewReadinessStatus? readiness = null,
        CaptureTarget? captureTarget = null)
    {
        ArgumentNullException.ThrowIfNull(outputProfile);
        ArgumentNullException.ThrowIfNull(artifacts);
        var artifactArray = artifacts.ToArray();
        var projectedProfile = ProjectOutputProfile(outputProfile, artifactArray, readiness);
        return ProjectValidationCore(
            OutputValidationSessionArtifact.ApplyAllTo(outputProfile, artifactArray),
            projectedProfile,
            readiness,
            captureTarget,
            SelectCompleteTargetHdrEvidence(artifactArray),
            artifactArray,
            ProjectValidationEvidenceSummary(artifactArray),
            record);
    }

    public static ValidationPanelProjection ProjectValidation(
        OutputProfileContract outputProfile,
        IEnumerable<OutputValidationSessionArtifact> artifacts,
        OutputProfileExecutionCapabilities executionCapabilities,
        ValidationRecordProjection? record = null,
        PreviewReadinessStatus? readiness = null,
        CaptureTarget? captureTarget = null)
        => ProjectValidation(
            outputProfile,
            artifacts,
            executionCapabilities,
            record,
            readiness,
            OutputTarget.Folder,
            captureTarget);

    public static ValidationPanelProjection ProjectValidation(
        OutputProfileContract outputProfile,
        IEnumerable<OutputValidationSessionArtifact> artifacts,
        OutputProfileExecutionCapabilities executionCapabilities,
        ValidationRecordProjection? record,
        PreviewReadinessStatus? readiness,
        OutputTarget outputTarget,
        CaptureTarget? captureTarget = null)
    {
        ArgumentNullException.ThrowIfNull(outputProfile);
        ArgumentNullException.ThrowIfNull(artifacts);
        ArgumentNullException.ThrowIfNull(executionCapabilities);
        var artifactArray = artifacts.ToArray();
        var requestedProfile = ApplyArtifactsForOutputTarget(outputProfile, artifactArray, outputTarget);
        var effectiveProfile = executionCapabilities.SelectEffectiveProfile(requestedProfile);
        var projectedProfile = ProjectOutputProfile(
            outputProfile,
            artifactArray,
            readiness,
            executionCapabilities,
            outputTarget);
        return ProjectValidationCore(
            SelectRuntimeClaimContract(requestedProfile, effectiveProfile),
            projectedProfile,
            readiness,
            captureTarget,
            SelectCompleteTargetHdrEvidence(artifactArray),
            artifactArray,
            ProjectValidationEvidenceSummary(artifactArray),
            record);
    }

    private static OutputProfileContract ApplyArtifactsForOutputTarget(
        OutputProfileContract contract,
        IEnumerable<OutputValidationSessionArtifact> artifacts,
        OutputTarget outputTarget) =>
        OutputProfileTargetScope.ApplyValidationArtifacts(
            contract,
            artifacts,
            outputTarget);

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
        OutputProfileProjection outputProfileProjection,
        PreviewReadinessStatus? readiness,
        CaptureTarget? captureTarget,
        TargetAwareHdrValidationEvidence? targetHdrEvidence,
        IReadOnlyList<OutputValidationSessionArtifact> artifacts,
        ValidationEvidenceSummaryProjection evidenceSummary,
        ValidationRecordProjection? record)
    {
        var viewerMatrix = CreateViewerMatrix(outputProfile, artifacts);
        var effectiveRecord = record ?? ProjectValidationRecord(null);
        return new(
            ReleaseTarget,
            "Public release waits for evidence; SDR compatibility remains fallback only.",
            ProjectValidationGate(outputProfileProjection),
            CreateValidationRows(outputProfile, readiness, captureTarget, targetHdrEvidence, viewerMatrix, evidenceSummary),
            "Named viewers must prove artifact handling, visual match, and fidelity separately.",
            viewerMatrix,
            effectiveRecord)
        {
            EvidenceSummary = evidenceSummary,
        };
    }

    public static ValidationPanelProjection ApplyEvidenceSummary(
        ValidationPanelProjection validation,
        ValidationEvidenceSummaryProjection evidenceSummary)
    {
        ArgumentNullException.ThrowIfNull(validation);
        ArgumentNullException.ThrowIfNull(evidenceSummary);

        return validation with
        {
            Rows = ReplaceEvidenceReviewRows(validation.Rows, evidenceSummary),
            EvidenceSummary = evidenceSummary,
        };
    }

    private static ValidationGateProjection ProjectValidationGate(OutputProfileProjection outputProfileProjection) =>
        new(
            outputProfileProjection.Label,
            outputProfileProjection.StatusLabel,
            outputProfileProjection.Detail,
            outputProfileProjection.StatusLabel switch
            {
                "Ready" => ValidationEvidenceStatus.Pass,
                _ => ValidationEvidenceStatus.Limited,
            });

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
            "HDR-preserved export remains blocked until a supported profile passes validation.");
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
        CaptureTarget? captureTarget,
        TargetAwareHdrValidationEvidence? targetHdrEvidence)
    {
        var runtimeContext = DescribeCaptureTargetContext(captureTarget);

        if (targetHdrEvidence is not null)
        {
            return new ValidationEvidenceRowProjection(
                "Target-aware HDR",
                ValidationEvidenceStatus.Limited,
                $"Target-aware HDR artifact evidence is present (match={targetHdrEvidence.MatchKind}, state={targetHdrEvidence.HdrState}). {runtimeContext} Windows manual validation across mixed HDR/SDR monitor setups is still required.");
        }

        if (readiness?.Reason is PreviewReadinessReason.TargetDisplayUnresolved)
        {
            var matchEvidence = ExtractDisplayMatchEvidence(readiness.TechnicalDetail);
            var detail = string.IsNullOrEmpty(matchEvidence)
                ? $"HDR readiness is unvalidated for the selected capture target because display capability could not be matched to a DXGI output. {runtimeContext} Mixed HDR/SDR monitor evidence is still required."
                : $"HDR readiness is unvalidated for the selected capture target because display capability could not be matched to a DXGI output ({matchEvidence}). {runtimeContext} Mixed HDR/SDR monitor evidence is still required.";

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
                $"Target-aware display output evidence is present ({resolvedMatchEvidence}). {runtimeContext} Windows manual validation across mixed HDR/SDR monitor setups is still required.");
        }

        return new ValidationEvidenceRowProjection(
            "Target-aware HDR",
            ValidationEvidenceStatus.NotRun,
            $"{runtimeContext} Mixed HDR/SDR monitor evidence is required.");
    }

    private static string DescribeCaptureTargetContext(CaptureTarget? captureTarget)
    {
        if (captureTarget is null)
        {
            return "Current runtime target: unresolved.";
        }

        var targetLabel = string.IsNullOrWhiteSpace(captureTarget.DisplayName)
            ? "Capture target"
            : captureTarget.DisplayName.Trim();

        return captureTarget.Kind switch
        {
            CaptureTargetKind.Display => DescribeDisplayTargetContext(targetLabel, captureTarget.DisplayIdentity, captureTarget.Size),
            CaptureTargetKind.Window => $"Current runtime target: window \"{targetLabel}\" still depends on display mapping before it can count as target-aware HDR evidence.",
            _ => $"Current runtime target: \"{targetLabel}\" has unresolved target kind, so target-aware HDR evidence still needs a concrete display mapping.",
        };
    }

    private static string DescribeDisplayTargetContext(
        string targetLabel,
        DisplayOutputIdentity? displayIdentity,
        Windows.Graphics.SizeInt32 targetSize)
    {
        if (displayIdentity is { Left: { } left, Top: { } top })
        {
            return $"Current runtime target: display \"{targetLabel}\" ({displayIdentity.DeviceName}) at desktop bounds {left},{top} {displayIdentity.Width}x{displayIdentity.Height}.";
        }

        if (displayIdentity is not null)
        {
            return $"Current runtime target: display \"{targetLabel}\" ({displayIdentity.DeviceName}) at {displayIdentity.Width}x{displayIdentity.Height}.";
        }

        return $"Current runtime target: display \"{targetLabel}\" at {targetSize.Width}x{targetSize.Height}, but display identity is not recorded yet.";
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
            "Windows manual validation for MVP capture, output, and HDR honesty is not run.",
            "knowledge/validation/mvp-checklist.md");
    }

    public static ValidationEvidenceSummaryProjection ProjectValidationEvidenceSummary(
        IEnumerable<OutputValidationSessionArtifact> artifacts)
    {
        ArgumentNullException.ThrowIfNull(artifacts);

        var artifactArray = artifacts.ToArray();
        return artifactArray.Length == 0
            ? ValidationEvidenceSummaryProjection.Empty
            : CreateLoadedEvidenceSummary(artifactArray, loadIssues: [], artifactReferences: [], buildVersion: null);
    }

    public static ValidationEvidenceSummaryProjection ProjectValidationEvidenceSummary(
        OutputValidationArtifactSnapshot validationSnapshot)
        => ProjectValidationEvidenceSummary(validationSnapshot, buildVersion: null);

    public static ValidationEvidenceSummaryProjection ProjectValidationEvidenceSummary(
        OutputValidationArtifactSnapshot validationSnapshot,
        string? buildVersion)
    {
        ArgumentNullException.ThrowIfNull(validationSnapshot);

        var artifactArray = validationSnapshot.Artifacts.ToArray();
        if (artifactArray.Length > 0 || validationSnapshot.HasLoadIssues)
        {
            return CreateLoadedEvidenceSummary(
                artifactArray,
                validationSnapshot.LoadIssues,
                validationSnapshot.ArtifactReferences,
                buildVersion);
        }

        var workspace = validationSnapshot.Workspace;
        if (!workspace.IsReady && workspace.IsConfigured)
        {
            var issueSummary = workspace.Issues.Count == 0
                ? "Validation workspace setup is still incomplete."
                : string.Join(
                    " ",
                    workspace.Issues.Select(issue => $"{Path.GetFileName(issue.Path)}: {issue.Detail}"));
            return new ValidationEvidenceSummaryProjection(
                "Loaded evidence",
                ValidationEvidenceStatus.NotRun,
                "Validation workspace is not ready, so no output validation artifact is loaded for this session.",
                "Coverage: none yet.",
                $"Next step: fix the local validation workspace, then record a real Windows session and reload evidence. {issueSummary}");
        }

        return ValidationEvidenceSummaryProjection.Empty;
    }

    public static ValidationRecordProjection ProjectValidationRecord(
        string? buildVersion,
        OutputValidationArtifactSnapshot validationSnapshot)
    {
        ArgumentNullException.ThrowIfNull(validationSnapshot);

        var baseline = ProjectValidationRecord(buildVersion);
        var buildAlignment = EvaluateBuildAlignment(
            buildVersion,
            validationSnapshot.Artifacts,
            validationSnapshot.ArtifactReferences);
        var workspace = validationSnapshot.Workspace;
        if (!workspace.IsConfigured)
        {
            workspace = new OutputValidationWorkspaceState(
                "%LOCALAPPDATA%\\Lumiere\\validation\\output",
                "%LOCALAPPDATA%\\Lumiere\\validation\\output\\templates",
                "%LOCALAPPDATA%\\Lumiere\\validation\\output\\guidance",
                "%LOCALAPPDATA%\\Lumiere\\validation\\output\\evidence",
                "%LOCALAPPDATA%\\Lumiere\\validation\\output\\README.txt",
                null,
                "%LOCALAPPDATA%\\Lumiere\\validation\\output\\guidance\\mvp-checklist.md",
                "%LOCALAPPDATA%\\Lumiere\\validation\\output\\guidance\\hdr-notes.md",
                []);
        }
        var workspaceSummary = CreateWorkspaceSummary(workspace);
        var workspaceRecord = baseline with
        {
            ValidationWorkspacePath = string.IsNullOrWhiteSpace(workspace.DirectoryPath) ? null : workspace.DirectoryPath,
            ValidationTemplatePath = workspace.HasSampleTemplate ? workspace.SampleTemplatePath : null,
            ReleaseChecklistPath = workspace.ReleaseChecklistPath,
            HdrSdrScenariosPath = workspace.HdrSdrScenariosPath,
        };

        if (!workspace.IsReady)
        {
            var detail = string.Join(
                " ",
                workspace.Issues.Select(issue => $"{Path.GetFileName(issue.Path)}: {issue.Detail}"));
            return workspaceRecord with
            {
                WindowsManualValidationStatus = ValidationEvidenceStatus.Limited,
                WindowsManualValidationDetail =
                    string.IsNullOrWhiteSpace(detail)
                        ? "Validation workspace is not ready on this machine. Lumiere could not prepare the local output-validation folder."
                        : $"Validation workspace is not ready on this machine. {detail}",
                EvidenceDocumentPath = "knowledge/validation/mvp-checklist.md",
            };
        }

        if (validationSnapshot.HasLoadIssues)
        {
            var firstIssue = validationSnapshot.LoadIssues[0];
            return workspaceRecord with
            {
                WindowsManualValidationStatus = ValidationEvidenceStatus.Limited,
                WindowsManualValidationDetail =
                    $"{validationSnapshot.Artifacts.Count} output validation artifact(s) loaded, but {validationSnapshot.LoadIssues.Count} file(s) were ignored. Fix ignored artifact or evidence files before counting Windows manual output evidence. {DescribeBuildAlignmentForRecord(buildAlignment)} {workspaceSummary} First issue: {Path.GetFileName(firstIssue.Path)}: {firstIssue.Detail}",
                EvidenceDocumentPath = "knowledge/validation/mvp-checklist.md",
            };
        }

        if (validationSnapshot.HasArtifacts)
        {
            return workspaceRecord with
            {
                WindowsManualValidationStatus = ValidationEvidenceStatus.Limited,
                WindowsManualValidationDetail =
                    $"{validationSnapshot.Artifacts.Count} output validation artifact(s) loaded for this session. {DescribeBuildAlignmentForRecord(buildAlignment)} {workspaceSummary} MVP release still requires usable capture/output behavior and honest HDR copy.",
                EvidenceDocumentPath = "knowledge/validation/mvp-checklist.md",
            };
        }

        return workspaceRecord with
        {
            WindowsManualValidationStatus = ValidationEvidenceStatus.Limited,
            WindowsManualValidationDetail =
                $"{workspaceSummary} No output validation artifact is loaded for this session yet; copy the seeded sample, replace placeholders, and reload Lumiere after recording real Windows observations.",
            EvidenceDocumentPath = "knowledge/validation/mvp-checklist.md",
        };
    }

    private static string CreateWorkspaceSummary(OutputValidationWorkspaceState workspace)
    {
        if (!workspace.IsReady)
        {
            return "Validation workspace setup is incomplete.";
        }

        return workspace.HasSampleTemplate
            ? $"Validation workspace: {workspace.DirectoryPath}. Seeded sample: {workspace.SampleTemplatePath}. Local guides: MVP checklist and HDR notes."
            : $"Validation workspace: {workspace.DirectoryPath}.";
    }

    private static ValidationEvidenceSummaryProjection CreateLoadedEvidenceSummary(
        IReadOnlyList<OutputValidationSessionArtifact> artifacts,
        IReadOnlyList<OutputValidationArtifactLoadIssue> loadIssues,
        IReadOnlyList<OutputValidationArtifactReference> artifactReferences,
        string? buildVersion)
    {
        if (artifacts.Count == 0 && loadIssues.Count > 0)
        {
            return CreateRejectedEvidenceSummary(loadIssues);
        }

        var latestArtifact = SelectLatestArtifact(artifacts);
        var latestArtifactReference = SelectLatestArtifactReference(artifactReferences);
        var buildAlignment = EvaluateBuildAlignment(buildVersion, artifacts, artifactReferences);
        var latestSummary = latestArtifact is null
            ? "No valid output validation artifact is loaded for this session."
            : $"Latest artifact: {FormatArtifactHeader(latestArtifact)}. {NormalizeSentence(latestArtifact.ResultSummary)}";
        var loadIssueSummary = loadIssues.Count == 0
            ? string.Empty
            : $" {loadIssues.Count} file(s) were ignored during load. {DescribeFirstLoadIssue(loadIssues[0])}";

        return new ValidationEvidenceSummaryProjection(
            "Loaded evidence",
            artifacts.Count == 0 ? ValidationEvidenceStatus.NotRun : ValidationEvidenceStatus.Limited,
            artifacts.Count == 0
                ? $"No valid output validation artifact is loaded for this session.{loadIssueSummary}"
                : $"{artifacts.Count} artifact(s) loaded for this session. {latestSummary}{loadIssueSummary}",
            CreateCoverageDetail(artifacts),
            CreateGapDetail(artifacts, loadIssues))
        {
            LatestArtifactPath = latestArtifactReference?.Path,
            BuildAlignment = buildAlignment,
            TargetAppVersionEvidence = EvaluateTargetAppVersionEvidence(artifacts),
        };
    }

    private static ValidationEvidenceSummaryProjection CreateRejectedEvidenceSummary(
        IReadOnlyList<OutputValidationArtifactLoadIssue> loadIssues)
    {
        var firstIssue = loadIssues[0];
        return new ValidationEvidenceSummaryProjection(
            "Loaded evidence",
            ValidationEvidenceStatus.NotRun,
            $"{loadIssues.Count} validation artifact or evidence file(s) were found but none loaded for this session. Runtime gates stay blocked until ignored files are fixed and reloaded.",
            "Coverage: none loaded; ignored files do not count as Windows manual evidence.",
            $"Next step: fix ignored artifact or evidence files, then reload evidence. {DescribeFirstLoadIssue(firstIssue)}")
        {
            BuildAlignment = ValidationEvidenceBuildAlignmentProjection.Empty,
            TargetAppVersionEvidence = ValidationEvidenceTargetAppVersionProjection.Empty,
        };
    }

    private static string DescribeFirstLoadIssue(OutputValidationArtifactLoadIssue issue) =>
        $"First issue: {Path.GetFileName(issue.Path)}: {issue.Detail}";

    private static ValidationEvidenceRowProjection ProjectCurrentBuildEvidenceRow(
        ValidationEvidenceBuildAlignmentProjection buildAlignment) =>
        new(
            "Current build evidence",
            buildAlignment.Status,
            buildAlignment.Detail);

    private static ValidationEvidenceRowProjection ProjectTargetAppVersionEvidenceRow(
        ValidationEvidenceTargetAppVersionProjection targetAppVersionEvidence) =>
        new(
            targetAppVersionEvidence.Label,
            targetAppVersionEvidence.Status,
            targetAppVersionEvidence.Detail);

    private static IReadOnlyList<ValidationEvidenceRowProjection> CreateValidationRows(
        OutputProfileContract outputProfile,
        PreviewReadinessStatus? readiness,
        CaptureTarget? captureTarget,
        TargetAwareHdrValidationEvidence? targetHdrEvidence,
        IReadOnlyList<ValidationViewerMatrixRowProjection> viewerMatrix,
        ValidationEvidenceSummaryProjection evidenceSummary) =>
        [
            ProjectTargetAwareHdrRow(readiness, captureTarget, targetHdrEvidence),
            ProjectVisualMatchRow(outputProfile),
            ProjectHdrPreservedProfileRow(outputProfile),
            ProjectTargetAppMatrixRow(viewerMatrix),
            ProjectTargetAppVersionEvidenceRow(evidenceSummary.TargetAppVersionEvidence),
            ProjectCurrentBuildEvidenceRow(evidenceSummary.BuildAlignment),
        ];

    private static IReadOnlyList<ValidationEvidenceRowProjection> ReplaceEvidenceReviewRows(
        IReadOnlyList<ValidationEvidenceRowProjection> rows,
        ValidationEvidenceSummaryProjection evidenceSummary)
    {
        var updatedRows = rows
            .Where(row =>
                !string.Equals(row.Label, "Current build evidence", StringComparison.Ordinal)
                && !string.Equals(row.Label, "Target app versions", StringComparison.Ordinal))
            .ToList();
        updatedRows.Add(ProjectTargetAppVersionEvidenceRow(evidenceSummary.TargetAppVersionEvidence));
        updatedRows.Add(ProjectCurrentBuildEvidenceRow(evidenceSummary.BuildAlignment));
        return updatedRows;
    }

    private static IReadOnlyList<ValidationViewerMatrixRowProjection> CreateViewerMatrix(
        OutputProfileContract outputProfile,
        IReadOnlyList<OutputValidationSessionArtifact> artifacts) =>
        outputProfile.ViewerEvidence
            .Select(evidence => ProjectViewerEvidence(
                evidence,
                ProjectViewerTargetAppVersionEvidence(evidence, artifacts),
                DescribeViewerTargetScope(outputProfile.Kind, evidence.Name, artifacts)))
            .ToArray();

    private static OutputValidationSessionArtifact? SelectLatestArtifact(
        IEnumerable<OutputValidationSessionArtifact> artifacts) =>
        artifacts
            .OrderByDescending(artifact => ParseArtifactDate(artifact.Date))
            .ThenByDescending(artifact => NormalizeEvidenceField(artifact.BuildCommit, "unknown build"))
            .FirstOrDefault();

    private static OutputValidationArtifactReference? SelectLatestArtifactReference(
        IEnumerable<OutputValidationArtifactReference> artifactReferences) =>
        artifactReferences
            .OrderByDescending(reference => ParseArtifactDate(reference.Artifact.Date))
            .ThenByDescending(reference => NormalizeEvidenceField(reference.Artifact.BuildCommit, "unknown build"))
            .FirstOrDefault();

    private static DateOnly ParseArtifactDate(string? value) =>
        DateOnly.TryParse(value, out var parsed)
            ? parsed
            : DateOnly.MinValue;

    private sealed record MvpChecklistGapGroup(
        string Label,
        IReadOnlyList<string> ChecklistIds,
        string RecommendedAction);

    private static readonly MvpChecklistGapGroup[] MvpChecklistGapGroups =
    [
        new(
            "Target-aware HDR",
            ["REL-HDR-01", "REL-HDR-02", "REL-HDR-03", "REL-HDR-04", "REL-HDR-05"],
            "Review HDR notes and record the missing target HDR observations."),
        new(
            "Viewer/output matrix",
            ["REL-OUT-01", "REL-OUT-02", "REL-OUT-03", "REL-OUT-04", "REL-OUT-05"],
            "Review the MVP checklist and record the missing output-target runs."),
        new(
            "Export-profile accessibility and DPI",
            ["REL-HDR-06", "REL-A11Y-01", "REL-A11Y-02", "REL-A11Y-03", "REL-A11Y-04", "REL-A11Y-05"],
            "Record any export-profile, keyboard, high-contrast, or DPI limitations that affect the MVP."),
        new(
            "MVP stability smoke",
            ["REL-STAB-01", "REL-STAB-02"],
            "Record repeated capture/output observations in the MVP checklist before counting stability evidence."),
    ];

    private static string CreateCoverageDetail(IReadOnlyList<OutputValidationSessionArtifact> artifacts)
    {
        if (artifacts.Count == 0)
        {
            return "Coverage: none yet.";
        }

        return
            $"Coverage: entry points {FormatEvidenceList(artifacts.SelectMany(artifact => artifact.EntryPointsTested), fallback: "none yet")}; "
            + $"targets {FormatEvidenceList(artifacts.SelectMany(artifact => artifact.OutputTargetsTested), fallback: "none yet")}; "
            + $"viewers {FormatEvidenceList(artifacts.SelectMany(artifact => artifact.TargetAppsTested), fallback: "none yet")}; "
            + $"viewer versions {FormatTargetAppVersionList(artifacts.SelectMany(artifact => artifact.TargetAppVersions), fallback: "none yet")}; "
            + $"topologies {FormatEvidenceList(OutputValidationRunPlanner.Create(artifacts).CoveredDisplayTopologies, fallback: "none yet", maxItems: 6)}; "
            + $"DPI {FormatEvidenceList(artifacts.SelectMany(artifact => artifact.DpiScales), fallback: "none yet")}; "
            + $"display setups {FormatEvidenceList(artifacts.Select(artifact => artifact.DisplaySetup), fallback: "none yet")}; "
            + $"HDR states {FormatEvidenceList(artifacts.Select(artifact => artifact.HdrState), fallback: "none yet")}; "
            + $"checklist {FormatEvidenceList(artifacts.SelectMany(artifact => artifact.ChecklistIdsCovered), fallback: "none yet")}.";
    }

    private static string CreateGapDetail(
        IReadOnlyList<OutputValidationSessionArtifact> artifacts,
        IReadOnlyList<OutputValidationArtifactLoadIssue> loadIssues)
    {
        var limitations = CollectDistinctEvidenceValues(artifacts.SelectMany(artifact => artifact.KnownLimitations));
        var followUps = CollectDistinctEvidenceValues(artifacts.SelectMany(artifact => artifact.FollowUpIssuesOrStories));
        var detailParts = new List<string>();

        if (limitations.Count > 0)
        {
            detailParts.Add($"Known limitations: {FormatEvidenceList(limitations, fallback: "none recorded")}.");
        }

        if (followUps.Count > 0)
        {
            detailParts.Add($"Follow-up: {FormatEvidenceList(followUps, fallback: "none recorded")}.");
        }

        var missingTargetAppVersions = CollectMissingTargetAppVersions(artifacts);
        if (missingTargetAppVersions.Count > 0)
        {
            detailParts.Add($"Target app versions are still missing for {FormatEvidenceList(missingTargetAppVersions, fallback: "named target apps")}.");
        }

        var missingTargetHdrEvidenceFields = CollectMissingTargetHdrEvidenceFields(artifacts);
        if (missingTargetHdrEvidenceFields.Count > 0)
        {
            detailParts.Add($"Target-aware HDR evidence is incomplete: {FormatEvidenceList(missingTargetHdrEvidenceFields, fallback: "target-aware HDR evidence", maxItems: 6)}.");
        }

        var missingManualSessionFields = CollectMissingManualSessionFields(artifacts);
        if (missingManualSessionFields.Count > 0)
        {
            detailParts.Add($"Manual validation session evidence is incomplete: {FormatEvidenceList(missingManualSessionFields, fallback: "manual session fields", maxItems: 6)}.");
        }

        var runPlan = OutputValidationRunPlanner.Create(artifacts);
        if (runPlan.MissingDisplayTopologies.Count > 0)
        {
            detailParts.Add($"Display topology gaps: {FormatEvidenceList(runPlan.MissingDisplayTopologies, fallback: "standard topology buckets", maxItems: 6)}.");
        }

        if (runPlan.MissingViewerTargets.Count > 0)
        {
            detailParts.Add($"HDR10 viewer target gaps: {FormatEvidenceList(runPlan.MissingViewerTargets, fallback: "named target apps")}.");
        }

        var checklistCoverageGaps = DescribeMvpChecklistGaps(artifacts);
        if (checklistCoverageGaps.Count > 0)
        {
            detailParts.Add($"Public gate gaps: {string.Join("; ", checklistCoverageGaps.Select(gap => gap.LabelWithIds))}.");
            detailParts.Add($"Next recommended runs: {string.Join(" ", checklistCoverageGaps.Select(gap => gap.RecommendedAction))}");
        }

        var nextRun = runPlan.CreateNextWindowsRunRecommendation();
        if (!string.IsNullOrWhiteSpace(nextRun))
        {
            detailParts.Add(nextRun);
        }

        if (loadIssues.Count > 0)
        {
            detailParts.Add($"Ignored files must be fixed before counting this session as release evidence. {DescribeFirstLoadIssue(loadIssues[0])}");
        }

        if (detailParts.Count == 0)
        {
            return artifacts.Count == 0
                ? "Next step: create or copy a validation artifact, replace placeholders with real Windows observations, then reload evidence."
                : "Known limitations: none recorded. Follow-up: none recorded yet.";
        }

        return string.Join(" ", detailParts);
    }

    private sealed record MvpChecklistGapDetail(
        string LabelWithIds,
        string RecommendedAction);

    private static IReadOnlyList<MvpChecklistGapDetail> DescribeMvpChecklistGaps(
        IReadOnlyList<OutputValidationSessionArtifact> artifacts)
    {
        var covered = artifacts
            .SelectMany(artifact => artifact.ChecklistIdsCovered)
            .Select(value => value?.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return MvpChecklistGapGroups
            .Select(group =>
            {
                var missing = group.ChecklistIds
                    .Where(id => !covered.Contains(id))
                    .ToArray();
                return (Group: group, Missing: missing);
            })
            .Where(result => result.Missing.Length > 0)
            .Select(result => new MvpChecklistGapDetail(
                $"{result.Group.Label} ({string.Join(", ", result.Missing)})",
                result.Group.RecommendedAction))
            .ToArray();
    }

    private static ValidationEvidenceTargetAppVersionProjection EvaluateTargetAppVersionEvidence(
        IReadOnlyList<OutputValidationSessionArtifact> artifacts)
    {
        if (artifacts.Count == 0)
        {
            return ValidationEvidenceTargetAppVersionProjection.Empty;
        }

        var namedTargetApps = CollectDistinctEvidenceValues(artifacts.SelectMany(artifact => artifact.TargetAppsTested));
        if (namedTargetApps.Count == 0)
        {
            return new ValidationEvidenceTargetAppVersionProjection(
                "Target app versions",
                ValidationEvidenceStatus.NotRun,
                "Named target apps must be recorded before target-app version evidence can pass.");
        }

        var missingTargetAppVersions = CollectMissingTargetAppVersions(artifacts);
        return missingTargetAppVersions.Count == 0
            ? new ValidationEvidenceTargetAppVersionProjection(
                "Target app versions",
                ValidationEvidenceStatus.Pass,
                "All named target apps in the loaded evidence are tied to concrete recorded app versions.")
            : new ValidationEvidenceTargetAppVersionProjection(
                "Target app versions",
                ValidationEvidenceStatus.Limited,
                $"Target app version evidence is missing for {FormatEvidenceList(missingTargetAppVersions, fallback: "named target apps")}.");
    }

    private static IReadOnlyList<string> CollectMissingTargetAppVersions(
        IEnumerable<OutputValidationSessionArtifact> artifacts) =>
        artifacts
            .SelectMany(artifact => artifact.GetMissingTargetAppVersions())
            .Where(IsRecordedEvidenceField)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static IReadOnlyList<string> CollectMissingTargetHdrEvidenceFields(
        IReadOnlyList<OutputValidationSessionArtifact> artifacts)
    {
        if (artifacts.Count == 0
            || artifacts.Any(artifact =>
                artifact.TargetHdrEvidence is { } targetEvidence
                && !targetEvidence.GetMissingFields().Any()))
        {
            return [];
        }

        return artifacts
            .SelectMany(artifact => artifact.TargetHdrEvidence is null
                ? Enumerable.Repeat("target-aware HDR evidence", 1)
                : artifact.TargetHdrEvidence
                    .GetMissingFields()
                    .Select(field => $"target-aware HDR evidence {field}"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(field => field, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<string> CollectMissingManualSessionFields(
        IReadOnlyList<OutputValidationSessionArtifact> artifacts)
    {
        if (artifacts.Count == 0)
        {
            return [];
        }

        return artifacts
            .SelectMany(artifact => artifact.GetMissingManualEvidenceFields())
            .Where(field =>
                !field.StartsWith("target-aware HDR evidence", StringComparison.OrdinalIgnoreCase)
                && !field.StartsWith("target app version", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(field => field, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string FormatArtifactHeader(OutputValidationSessionArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        var date = NormalizeEvidenceField(artifact.Date, "unknown date");
        var tester = NormalizeEvidenceField(artifact.Tester, "unknown tester");
        var build = NormalizeEvidenceField(artifact.BuildCommit, "unknown build");
        return $"{date} by {tester} on build {build}";
    }

    private static string NormalizeEvidenceField(string? value, string fallback)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed)
            || trimmed.Contains("REPLACE_WITH_", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("Template only", StringComparison.OrdinalIgnoreCase)
                ? fallback
                : trimmed;
    }

    private static IReadOnlyList<string> CollectDistinctEvidenceValues(IEnumerable<string> values)
    {
        var ordered = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values)
        {
            var trimmed = value?.Trim();
            if (!IsRecordedEvidenceValue(trimmed))
            {
                continue;
            }

            var recorded = trimmed!;
            if (seen.Add(recorded))
            {
                ordered.Add(recorded);
            }
        }

        return ordered;
    }

    private static bool IsRecordedEvidenceValue(string? value)
        => OutputValidationRunPlanner.IsRecordedEvidenceValue(value);

    private static string FormatEvidenceList(IEnumerable<string> values, string fallback, int maxItems = 3)
    {
        var distinctValues = CollectDistinctEvidenceValues(values);
        if (distinctValues.Count == 0)
        {
            return fallback;
        }

        if (distinctValues.Count <= maxItems)
        {
            return string.Join(", ", distinctValues);
        }

        return $"{string.Join(", ", distinctValues.Take(maxItems))}, +{distinctValues.Count - maxItems} more";
    }

    private static string FormatTargetAppVersionList(
        IEnumerable<OutputValidationTargetAppVersionRecord> values,
        string fallback,
        int maxItems = 3)
    {
        var distinctValues = values
            .Where(value =>
                IsRecordedEvidenceField(value.Name)
                && IsRecordedEvidenceField(value.Version))
            .Select(value => $"{value.Name} {value.Version}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (distinctValues.Length == 0)
        {
            return fallback;
        }

        if (distinctValues.Length <= maxItems)
        {
            return string.Join(", ", distinctValues);
        }

        return $"{string.Join(", ", distinctValues.Take(maxItems))}, +{distinctValues.Length - maxItems} more";
    }

    private static bool IsRecordedEvidenceField(string? value) =>
        !string.Equals(
            NormalizeEvidenceField(value, "unknown"),
            "unknown",
            StringComparison.Ordinal);

    private static string NormalizeSentence(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return "Result summary is not recorded yet.";
        }

        return trimmed.EndsWith('.')
            ? trimmed
            : $"{trimmed}.";
    }

    private static ValidationEvidenceBuildAlignmentProjection EvaluateBuildAlignment(
        string? buildVersion,
        IEnumerable<OutputValidationSessionArtifact> artifacts,
        IEnumerable<OutputValidationArtifactReference> artifactReferences)
    {
        var artifactArray = artifacts.ToArray();
        if (artifactArray.Length == 0)
        {
            return ValidationEvidenceBuildAlignmentProjection.Empty;
        }

        var latestArtifact = SelectLatestArtifact(artifactArray);
        var latestArtifactReference = SelectLatestArtifactReference(artifactReferences);
        if (latestArtifact is null)
        {
            return ValidationEvidenceBuildAlignmentProjection.Empty;
        }

        var alignment = ValidationArtifactBuildAlignment.Evaluate(buildVersion, artifactArray);
        var artifactPath = latestArtifactReference?.Path;
        var artifactPathDetail = string.IsNullOrWhiteSpace(artifactPath)
            ? string.Empty
            : $" File: {artifactPath}.";

        return alignment.Status switch
        {
            ValidationArtifactBuildAlignmentStatus.MatchedCurrentBuild =>
                new ValidationEvidenceBuildAlignmentProjection(
                    "Build alignment",
                    ValidationEvidenceStatus.Pass,
                    $"{alignment.Detail} Windows manual validation still remains scoped to the recorded target, viewer, and scenario coverage.{artifactPathDetail}",
                    "Matched current build")
                {
                    ExpectedBuild = alignment.ExpectedBuildCommit,
                    LatestArtifactBuild = alignment.LatestArtifactBuildCommit,
                },
            ValidationArtifactBuildAlignmentStatus.StaleForCurrentBuild =>
                new ValidationEvidenceBuildAlignmentProjection(
                    "Build alignment",
                    ValidationEvidenceStatus.Limited,
                    $"{alignment.Detail} Record fresh Windows evidence before treating this session as public-release support.{artifactPathDetail}",
                    "Stale for current build")
                {
                    ExpectedBuild = alignment.ExpectedBuildCommit,
                    LatestArtifactBuild = alignment.LatestArtifactBuildCommit,
                },
            ValidationArtifactBuildAlignmentStatus.Unknown =>
                new ValidationEvidenceBuildAlignmentProjection(
                    "Build alignment",
                    ValidationEvidenceStatus.Limited,
                    $"{alignment.Detail}{artifactPathDetail}",
                    "Unknown build match")
                {
                    ExpectedBuild = alignment.ExpectedBuildCommit,
                    LatestArtifactBuild = alignment.LatestArtifactBuildCommit,
                },
            _ => ValidationEvidenceBuildAlignmentProjection.Empty,
        };
    }

    private static string DescribeBuildAlignmentForRecord(
        ValidationEvidenceBuildAlignmentProjection buildAlignment) =>
        buildAlignment.Status switch
        {
            ValidationEvidenceStatus.Pass => "Loaded evidence matches the current build.",
            ValidationEvidenceStatus.Limited when buildAlignment.ExpectedBuild is not null && buildAlignment.LatestArtifactBuild is not null =>
                $"Loaded evidence is not aligned with the current build ({buildAlignment.ExpectedBuild} vs {buildAlignment.LatestArtifactBuild}).",
            ValidationEvidenceStatus.Limited => "Loaded evidence cannot be aligned to the current build yet.",
            _ => buildAlignment.Detail,
        };

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
            CreateContractProjection(contract, statusLabel),
            CreateFidelityClaim(contract, readiness));

    private static OutputProfileContractProjection CreateContractProjection(
        OutputProfileContract contract,
        string statusLabel)
    {
        var (sourcePolicy, destinationPolicy, conversionPolicy, metadataPolicy, viewerCompatibilityPolicy) =
            DescribeContractPolicies(contract, statusLabel);
        return new OutputProfileContractProjection(
            FormatPixelFormat(contract.FormatContract.SourcePixelFormat, isDestination: false),
            FormatPixelFormat(contract.FormatContract.DestinationPixelFormat, isDestination: true),
            FormatTransferFunction(contract.FormatContract.TransferFunction),
            FormatColorPrimaries(contract.FormatContract.ColorPrimaries),
            FormatConversionPolicy(contract.FormatContract.ConversionPolicy),
            FormatMetadataPolicy(contract.FormatContract.MetadataPolicy),
            FormatTargetAppAssumption(contract.FormatContract.TargetAppAssumption),
            sourcePolicy,
            destinationPolicy,
            conversionPolicy,
            metadataPolicy,
            viewerCompatibilityPolicy);
    }

    private static (
        string SourcePolicy,
        string DestinationPolicy,
        string ConversionPolicy,
        string MetadataPolicy,
        string ViewerCompatibilityPolicy) DescribeContractPolicies(
            OutputProfileContract contract,
            string statusLabel)
    {
        if (contract.Kind is not OutputProfileKind.Hdr10Pq || !contract.HasCompleteFormatContract)
        {
            return (
                contract.SourceFormatPolicy,
                contract.DestinationFormatPolicy,
                contract.ConversionPolicy,
                contract.MetadataPolicy,
                contract.ViewerCompatibilityPolicy);
        }

        return statusLabel switch
        {
            "Ready" => (
                contract.SourceFormatPolicy,
                "Validated HDR10-preserved artifact contract is active for this session.",
                "scRGB-to-HDR10 transfer, tone mapping, and gamut mapping policy are defined for the validated HDR-preserved path.",
                "HDR10 static metadata attachment is defined and validated for the active HDR-preserved path.",
                "Named target-app compatibility evidence passed for the active HDR10 path."),
            "Validate" => (
                contract.SourceFormatPolicy,
                "HDR10 output contract is defined, but this session is still waiting for Windows manual viewer evidence.",
                "scRGB-to-HDR10 transfer, tone mapping, and gamut mapping policy are defined for the HDR10 path; Windows manual viewer evidence is still incomplete.",
                "HDR10 static metadata attachment is defined for the HDR10 path; Windows manual viewer evidence is still incomplete before HDR-preserved claims can pass.",
                "Named target-app compatibility still depends on complete Windows manual viewer evidence for this session."),
            "Build" => (
                contract.SourceFormatPolicy,
                "HDR10 output contract is defined, but executable HDR10 output is still blocked by build or runtime prerequisites.",
                "scRGB-to-HDR10 transfer, tone mapping, and gamut mapping policy are defined for the HDR10 path, but executable output is still blocked by build or runtime prerequisites.",
                "HDR10 static metadata attachment is defined for the HDR10 path, but executable output is still blocked by build or runtime prerequisites.",
                "Named target-app compatibility still depends on executable HDR10 output plus Windows manual viewer evidence."),
            _ => (
                contract.SourceFormatPolicy,
                contract.DestinationFormatPolicy,
                contract.ConversionPolicy,
                contract.MetadataPolicy,
                contract.ViewerCompatibilityPolicy),
        };
    }

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

    private static (string StatusLabel, string Detail, bool IsReadOnly) DescribeGatePresentation(
        OutputProfileKind profileKind,
        OutputProfileExecutionGate gate,
        bool isExecutableForSession)
    {
        return profileKind switch
        {
            OutputProfileKind.Hdr10Pq when isExecutableForSession => (
                "Ready",
                "HDR10 profile is executable for this validated session.",
                false),
            OutputProfileKind.Hdr10Pq when gate.Status is OutputProfileGateStatus.PendingValidation => (
                "Validate",
                $"HDR10 build prerequisites are ready, but Windows manual viewer evidence is still incomplete. {gate.Detail}",
                true),
            OutputProfileKind.Hdr10Pq => (
                "Build",
                $"HDR10 is still blocked because implementation prerequisites, profile contract wiring, metadata policy work, or Windows validation are still incomplete. {gate.Detail}",
                true),
            OutputProfileKind.DisplayP3 when isExecutableForSession => (
                "Ready",
                "Wide-gamut output is executable for this validated session.",
                false),
            OutputProfileKind.DisplayP3 when gate.Status is OutputProfileGateStatus.PendingValidation => (
                "Validate",
                $"Wide-gamut output is implemented, but validation evidence is still incomplete. {gate.Detail}",
                true),
            OutputProfileKind.DisplayP3 => (
                "Build",
                $"Wide-gamut output is shown for planning, but implementation is still incomplete. {gate.Detail}",
                true),
            _ => (
                "Compat",
                "Compatible output for the MVP; HDR-preserved export remains future work.",
                false),
        };
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

    private static ValidationViewerMatrixRowProjection ProjectViewerEvidence(
        OutputViewerCompatibilityEvidence evidence,
        ViewerTargetAppVersionEvidence targetAppVersionEvidence,
        string targetScopeDetail) =>
        new(
            evidence.Name,
            MapEvidenceStatus(evidence.ArtifactHandlingStatus),
            MapEvidenceStatus(evidence.VisualMatchStatus),
            MapEvidenceStatus(evidence.HdrPreservationStatus),
            MapEvidenceStatus(evidence.Hdr10MetadataStatus),
            targetAppVersionEvidence.Status,
            $"Artifact handling: {FormatEvidenceStatus(evidence.ArtifactHandlingStatus)} · "
                + $"Visual match: {FormatEvidenceStatus(evidence.VisualMatchStatus)} · "
                + $"HDR preservation: {FormatEvidenceStatus(evidence.HdrPreservationStatus)} · "
                + $"HDR10 metadata: {FormatEvidenceStatus(evidence.Hdr10MetadataStatus)} · "
                + $"Target app version: {FormatValidationEvidenceStatus(targetAppVersionEvidence.Status)}",
            $"{targetScopeDetail} {targetAppVersionEvidence.Detail} Fidelity evidence is separated by category. {evidence.Detail}");

    private static ViewerTargetAppVersionEvidence ProjectViewerTargetAppVersionEvidence(
        OutputViewerCompatibilityEvidence evidence,
        IReadOnlyList<OutputValidationSessionArtifact> artifacts)
    {
        var viewerName = evidence.Name;
        if (artifacts.Count == 0)
        {
            return new(
                HasAppliedViewerEvidence(evidence)
                    ? ValidationEvidenceStatus.NotApplicable
                    : ValidationEvidenceStatus.NotRun,
                HasAppliedViewerEvidence(evidence)
                    ? "Target app version is not projected here because no loaded validation artifact is backing this viewer row yet."
                    : "Target app version is not recorded yet for this viewer.");
        }

        var targetedArtifacts = artifacts
            .Where(artifact => artifact.TargetAppsTested.Any(name => NamesMatch(name, viewerName)))
            .ToArray();
        if (targetedArtifacts.Length == 0)
        {
            return new(
                ValidationEvidenceStatus.NotRun,
                "Target app version is not recorded yet because this viewer is not covered in the loaded evidence.");
        }

        var recordedVersions = CollectDistinctEvidenceValues(
            targetedArtifacts
                .SelectMany(artifact => artifact.TargetAppVersions)
                .Where(record =>
                    NamesMatch(record.Name, viewerName)
                    && IsRecordedEvidenceField(record.Version))
                .Select(record => record.Version));
        var missingVersion = targetedArtifacts.Any(artifact =>
            OutputValidationSessionArtifact.GetMissingTargetAppVersionNames([viewerName], artifact.TargetAppVersions).Count > 0);
        if (recordedVersions.Count == 0)
        {
            return new(
                ValidationEvidenceStatus.Limited,
                "Target app version is still missing for this viewer in the loaded evidence.");
        }

        var recordedVersionDetail = recordedVersions.Count == 1
            ? $"Recorded version: {recordedVersions[0]}."
            : $"Recorded versions: {FormatEvidenceList(recordedVersions, fallback: "none yet")}.";
        return missingVersion
            ? new(
                ValidationEvidenceStatus.Limited,
                $"{recordedVersionDetail} At least one loaded session for this viewer still misses a concrete recorded app version.")
            : new(
                ValidationEvidenceStatus.Pass,
                recordedVersionDetail);
    }

    private static string DescribeViewerTargetScope(
        OutputProfileKind profileKind,
        string viewerName,
        IReadOnlyList<OutputValidationSessionArtifact> artifacts)
    {
        if (artifacts.Count == 0)
        {
            return "Output target scope is not recorded yet for this viewer.";
        }

        var scopes = artifacts
            .SelectMany(artifact => artifact.OutputProfileRecords
                .Where(record => record.ProfileKind == profileKind)
                .Where(record => record.ViewerEvidence.Any(viewer => NamesMatch(viewer.Name, viewerName)))
                .Select(record => DescribeTargetScope(record, artifact)))
            .ToArray();
        if (scopes.Length == 0)
        {
            return "Output target scope is not recorded yet for this viewer/profile combination.";
        }

        var distinctScopes = scopes
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return distinctScopes.Length == 1
            ? $"Output target scope: {distinctScopes[0]}."
            : $"Output target scope varies across loaded sessions: {string.Join(", ", distinctScopes)}.";
    }

    private static string DescribeTargetScope(
        OutputProfileValidationRecord record,
        OutputValidationSessionArtifact artifact)
    {
        var scope = record.OutputTargetsCovered.Count == 0
            ? FormatTargetScope(artifact.OutputTargetsTested)
            : FormatTargetScope(record.OutputTargetsCovered);
        return record.OutputTargetsCovered.Count == 0
            ? $"{scope} (session-level)"
            : scope;
    }

    private static string FormatTargetScope(IEnumerable<string> values)
    {
        var normalized = values
            .Select(NormalizeTargetScopeValue)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return normalized.Length == 0
            ? "Unspecified"
            : string.Join(", ", normalized);
    }

    private static string? NormalizeTargetScopeValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Equals("File", StringComparison.OrdinalIgnoreCase))
        {
            return "Folder";
        }

        return normalized;
    }

    private static bool HasAppliedViewerEvidence(OutputViewerCompatibilityEvidence evidence) =>
        evidence.ArtifactHandlingStatus is not OutputCompatibilityEvidenceStatus.NotRun
        || evidence.VisualMatchStatus is not OutputCompatibilityEvidenceStatus.NotRun
        || evidence.HdrPreservationStatus is not OutputCompatibilityEvidenceStatus.NotRun and not OutputCompatibilityEvidenceStatus.NotApplicable
        || evidence.Hdr10MetadataStatus is not OutputCompatibilityEvidenceStatus.NotRun and not OutputCompatibilityEvidenceStatus.NotApplicable;

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

    private static string FormatValidationEvidenceStatus(ValidationEvidenceStatus status) =>
        status switch
        {
            ValidationEvidenceStatus.Pass => "PASS",
            ValidationEvidenceStatus.Limited => "LIMITED",
            ValidationEvidenceStatus.Fail => "FAIL",
            ValidationEvidenceStatus.NotApplicable => "N/A",
            _ => "NOT RUN",
        };

    private static bool NamesMatch(string? left, string? right) =>
        string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);

    private sealed record ViewerTargetAppVersionEvidence(
        ValidationEvidenceStatus Status,
        string Detail);
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
    ValidationGateProjection OutputProfileGate,
    IReadOnlyList<ValidationEvidenceRowProjection> Rows,
    string ViewerMatrixSummary,
    IReadOnlyList<ValidationViewerMatrixRowProjection> ViewerMatrix,
    ValidationRecordProjection Record)
{
    public ValidationEvidenceSummaryProjection EvidenceSummary { get; init; } =
        ValidationEvidenceSummaryProjection.Empty;
}

public sealed record ValidationEvidenceSummaryProjection(
    string Heading,
    ValidationEvidenceStatus Status,
    string Summary,
    string CoverageDetail,
    string GapDetail)
{
    public string? LatestArtifactPath { get; init; }

    public ValidationEvidenceBuildAlignmentProjection BuildAlignment { get; init; } =
        ValidationEvidenceBuildAlignmentProjection.Empty;

    public ValidationEvidenceTargetAppVersionProjection TargetAppVersionEvidence { get; init; } =
        ValidationEvidenceTargetAppVersionProjection.Empty;

    public bool CanOpenLatestArtifact => !string.IsNullOrWhiteSpace(LatestArtifactPath);

    public static ValidationEvidenceSummaryProjection Empty { get; } =
        new(
            "Loaded evidence",
            ValidationEvidenceStatus.NotRun,
            "No output validation artifact is loaded for this session.",
            "Coverage: none yet.",
            "Next step: create or copy a validation artifact, replace placeholders with real Windows observations, then reload evidence.");
}

public sealed record ValidationEvidenceTargetAppVersionProjection(
    string Label,
    ValidationEvidenceStatus Status,
    string Detail)
{
    public static ValidationEvidenceTargetAppVersionProjection Empty { get; } =
        new(
            "Target app versions",
            ValidationEvidenceStatus.NotRun,
            "Named target apps must be tied to concrete recorded app versions before release evidence can pass.");
}

public sealed record ValidationEvidenceBuildAlignmentProjection(
    string Label,
    ValidationEvidenceStatus Status,
    string Detail,
    string StatusLabel)
{
    public string? ExpectedBuild { get; init; }

    public string? LatestArtifactBuild { get; init; }

    public static ValidationEvidenceBuildAlignmentProjection Empty { get; } =
        new(
            "Build alignment",
            ValidationEvidenceStatus.NotRun,
            "No loaded evidence is available yet, so current-build alignment cannot be checked.",
            "Not checked");
}

public sealed record ValidationGateProjection(
    string ProfileLabel,
    string StatusLabel,
    string Detail,
    ValidationEvidenceStatus Status);

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
    ValidationEvidenceStatus TargetAppVersionStatus,
    string StatusBreakdown,
    string Detail)
{
    public ValidationEvidenceStatus Status =>
        CombineStatus(ArtifactHandlingStatus, VisualMatchStatus, HdrPreservationStatus, Hdr10MetadataStatus, TargetAppVersionStatus);

    public string AutomationSummary => $"{StatusBreakdown}. {Detail}";

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
    string EvidenceDocumentPath)
{
    public string? ValidationWorkspacePath { get; init; }

    public string? ValidationTemplatePath { get; init; }

    public string? ReleaseChecklistPath { get; init; }

    public string? HdrSdrScenariosPath { get; init; }

    public bool CanOpenValidationWorkspace => !string.IsNullOrWhiteSpace(ValidationWorkspacePath);

    public bool CanOpenValidationTemplate => !string.IsNullOrWhiteSpace(ValidationTemplatePath);

    public bool CanOpenReleaseChecklist => !string.IsNullOrWhiteSpace(ReleaseChecklistPath);

    public bool CanOpenHdrSdrScenarios => !string.IsNullOrWhiteSpace(HdrSdrScenariosPath);

    public string WorkspaceSummary =>
        string.IsNullOrWhiteSpace(ValidationWorkspacePath)
            ? EvidenceDocumentPath
            : string.IsNullOrWhiteSpace(ValidationTemplatePath)
                ? $"Workspace: {ValidationWorkspacePath}"
                : $"Workspace: {ValidationWorkspacePath} | Template: {ValidationTemplatePath} | Guides: {ReleaseChecklistPath ?? HdrSdrScenariosPath ?? "not seeded"}";
}

public enum ValidationEvidenceStatus
{
    Pass = 0,
    Limited,
    Fail,
    NotRun,
    NotApplicable,
}
