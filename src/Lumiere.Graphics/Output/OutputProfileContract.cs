namespace Lumiere.Graphics.Output;

/// <summary>
/// Describes the fidelity semantics for a concrete output profile.
/// </summary>
public sealed record OutputProfileContract(
    OutputProfileKind Kind,
    string Label,
    bool IsExecutable,
    OutputFidelityMode FidelityMode,
    OutputFormatContract FormatContract,
    string SourceFormatPolicy,
    string DestinationFormatPolicy,
    string ConversionPolicy,
    string MetadataPolicy,
    string ViewerCompatibilityPolicy,
    IReadOnlyList<OutputViewerCompatibilityEvidence> ViewerEvidence)
{
    public static OutputProfileContract SrgbCompatibilityPng { get; } =
        new(
            OutputProfileKind.SrgbCompatibilityPng,
            "sRGB",
            IsExecutable: true,
            OutputFidelityMode.SdrCompatible,
            OutputFormatContract.SrgbCompatibility,
            "FP16/scRGB capture source",
            "Compatibility-converted sRGB artifact",
            "scRGB linear values are converted into SDR-compatible sRGB for common destinations.",
            "No HDR metadata is attached to the compatibility artifact.",
            "Paint, Photos, and Chromium compatibility still require Windows validation.",
            CreateCompatibilityViewerEvidence());

    public static OutputProfileContract Hdr10Pq { get; } =
        new(
            OutputProfileKind.Hdr10Pq,
            "HDR10",
            IsExecutable: false,
            OutputFidelityMode.Unvalidated,
            OutputFormatContract.Hdr10Pending,
            "FP16/scRGB capture source",
            "HDR10 output contract pending implementation",
            "Transfer, tone mapping, and gamut mapping policy must be defined before use.",
            "HDR10 metadata policy is required before this profile can make a fidelity claim.",
            "Named target-app compatibility matrix is required.",
            CreateHdrViewerEvidence());

    public static OutputProfileContract DisplayP3 { get; } =
        new(
            OutputProfileKind.DisplayP3,
            "P3",
            IsExecutable: false,
            OutputFidelityMode.Unvalidated,
            OutputFormatContract.DisplayP3Pending,
            "FP16/scRGB capture source",
            "Display P3 output contract pending implementation",
            "Wide-gamut conversion policy must be specified before use.",
            "Color profile and metadata attachment policy are not validated.",
            "Target-app compatibility matrix is not run.",
            CreateWideGamutViewerEvidence());

    public bool AllowsHdrPreservedClaim =>
        IsExecutable && HasCompleteFormatContract && FidelityMode is OutputFidelityMode.HdrPreserved;

    public bool HasCompleteFormatContract => FormatContract.IsComplete;

    public static OutputProfileContract FromSettingsValue(string? value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            return SrgbCompatibilityPng;
        }

        if (normalized.Equals("HDR10", StringComparison.OrdinalIgnoreCase))
        {
            return Hdr10Pq;
        }

        return normalized.Equals("P3", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Wide", StringComparison.OrdinalIgnoreCase)
                ? DisplayP3
                : SrgbCompatibilityPng;
    }

    public OutputProfileContract EffectiveExecutableProfile =>
        IsExecutable && HasCompleteFormatContract ? this : SrgbCompatibilityPng;

    public OutputProfileContract ApplyValidationRecord(OutputProfileValidationRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (record.ProfileKind != Kind)
        {
            return this;
        }

        var evidenceByViewer = record.ViewerEvidence.ToDictionary(
            evidence => evidence.Name,
            StringComparer.OrdinalIgnoreCase);
        var updatedViewerEvidence = ViewerEvidence
            .Select(viewer => evidenceByViewer.TryGetValue(viewer.Name, out var validated)
                ? MergeViewerEvidence(viewer, ApplyEvidenceSource(validated, record.EvidenceSource))
                : viewer)
            .ToArray();

        return this with { ViewerEvidence = updatedViewerEvidence };
    }

    public OutputProfileEvidenceSummary EvaluateEvidence()
    {
        var applicableViewers = ViewerEvidence.ToArray();
        var allowsVisualMatch = applicableViewers.Length > 0
            && applicableViewers.All(viewer =>
                viewer.ArtifactHandlingStatus is OutputCompatibilityEvidenceStatus.Pass
                && viewer.VisualMatchStatus is OutputCompatibilityEvidenceStatus.Pass);
        var visualDetail = allowsVisualMatch
            ? "All named viewers have artifact handling and visual-match evidence passed."
            : $"Visual-match claim blocked for {FormatViewerNames(FindVisualMatchBlockers(applicableViewers))}: artifact handling or visual-match evidence is NOT RUN, limited, or failed.";

        var hdrEvidenceRequired = FidelityMode is OutputFidelityMode.HdrPreserved;
        var allowsHdrPreserved = IsExecutable
            && HasCompleteFormatContract
            && hdrEvidenceRequired
            && applicableViewers.Length > 0
            && applicableViewers.All(viewer =>
                viewer.ArtifactHandlingStatus is OutputCompatibilityEvidenceStatus.Pass
                && viewer.VisualMatchStatus is OutputCompatibilityEvidenceStatus.Pass
                && viewer.HdrPreservationStatus is OutputCompatibilityEvidenceStatus.Pass);
        var hdrDetail = allowsHdrPreserved
            ? "HDR preservation evidence passed for all named viewers."
            : !IsExecutable
                ? "HDR-preserved claim blocked: output profile is not executable, and HDR preservation evidence cannot be counted yet."
                : !HasCompleteFormatContract
                    ? "HDR-preserved claim blocked: output format contract is incomplete."
                    : !hdrEvidenceRequired
                        ? "HDR-preserved claim blocked: this is not an HDR-preserved profile."
                        : $"HDR-preserved claim blocked for {FormatViewerNames(FindHdrPreservedBlockers(applicableViewers))}: HDR preservation evidence is NOT RUN, limited, or failed for at least one named viewer.";

        return new OutputProfileEvidenceSummary(
            allowsVisualMatch,
            allowsHdrPreserved,
            visualDetail,
            hdrDetail);
    }

    private static IEnumerable<OutputViewerCompatibilityEvidence> FindVisualMatchBlockers(
        IEnumerable<OutputViewerCompatibilityEvidence> viewers) =>
        viewers.Where(viewer =>
            viewer.ArtifactHandlingStatus is not OutputCompatibilityEvidenceStatus.Pass
            || viewer.VisualMatchStatus is not OutputCompatibilityEvidenceStatus.Pass);

    private static IEnumerable<OutputViewerCompatibilityEvidence> FindHdrPreservedBlockers(
        IEnumerable<OutputViewerCompatibilityEvidence> viewers) =>
        viewers.Where(viewer =>
            viewer.ArtifactHandlingStatus is not OutputCompatibilityEvidenceStatus.Pass
            || viewer.VisualMatchStatus is not OutputCompatibilityEvidenceStatus.Pass
            || viewer.HdrPreservationStatus is not OutputCompatibilityEvidenceStatus.Pass);

    private static string FormatViewerNames(IEnumerable<OutputViewerCompatibilityEvidence> viewers)
    {
        var names = viewers.Select(viewer => viewer.Name).ToArray();
        return names.Length == 0 ? "named viewers" : string.Join(", ", names);
    }

    private static OutputViewerCompatibilityEvidence ApplyEvidenceSource(
        OutputViewerCompatibilityEvidence evidence,
        OutputValidationEvidenceSource source)
    {
        if (source is OutputValidationEvidenceSource.WindowsManual)
        {
            return evidence;
        }

        return evidence with
        {
            ArtifactHandlingStatus = CapAutomatedPass(evidence.ArtifactHandlingStatus),
            VisualMatchStatus = CapAutomatedPass(evidence.VisualMatchStatus),
            HdrPreservationStatus = CapAutomatedPass(evidence.HdrPreservationStatus),
            Detail = $"{evidence.Detail} Windows manual validation is still required before this evidence can count as PASS.",
        };
    }

    private static OutputCompatibilityEvidenceStatus CapAutomatedPass(OutputCompatibilityEvidenceStatus status) =>
        status is OutputCompatibilityEvidenceStatus.Pass
            ? OutputCompatibilityEvidenceStatus.Limited
            : status;

    private static OutputViewerCompatibilityEvidence MergeViewerEvidence(
        OutputViewerCompatibilityEvidence current,
        OutputViewerCompatibilityEvidence incoming)
    {
        var merged = incoming with
        {
            ArtifactHandlingStatus = MergeEvidenceStatus(current.ArtifactHandlingStatus, incoming.ArtifactHandlingStatus),
            VisualMatchStatus = MergeEvidenceStatus(current.VisualMatchStatus, incoming.VisualMatchStatus),
            HdrPreservationStatus = MergeEvidenceStatus(current.HdrPreservationStatus, incoming.HdrPreservationStatus),
        };

        if (HasSameStatuses(merged, current))
        {
            return current;
        }

        if (HasSameStatuses(merged, incoming))
        {
            return incoming;
        }

        return merged with { Detail = $"{current.Detail} {incoming.Detail}" };
    }

    private static OutputCompatibilityEvidenceStatus MergeEvidenceStatus(
        OutputCompatibilityEvidenceStatus current,
        OutputCompatibilityEvidenceStatus incoming)
    {
        if (current is OutputCompatibilityEvidenceStatus.NotApplicable)
        {
            return OutputCompatibilityEvidenceStatus.NotApplicable;
        }

        if (incoming is OutputCompatibilityEvidenceStatus.NotApplicable)
        {
            return current;
        }

        if (current is OutputCompatibilityEvidenceStatus.Fail
            || incoming is OutputCompatibilityEvidenceStatus.Fail)
        {
            return OutputCompatibilityEvidenceStatus.Fail;
        }

        if (current is OutputCompatibilityEvidenceStatus.Pass
            || incoming is OutputCompatibilityEvidenceStatus.Pass)
        {
            return OutputCompatibilityEvidenceStatus.Pass;
        }

        if (current is OutputCompatibilityEvidenceStatus.Limited
            || incoming is OutputCompatibilityEvidenceStatus.Limited)
        {
            return OutputCompatibilityEvidenceStatus.Limited;
        }

        return OutputCompatibilityEvidenceStatus.NotRun;
    }

    private static bool HasSameStatuses(
        OutputViewerCompatibilityEvidence left,
        OutputViewerCompatibilityEvidence right) =>
        left.ArtifactHandlingStatus == right.ArtifactHandlingStatus
        && left.VisualMatchStatus == right.VisualMatchStatus
        && left.HdrPreservationStatus == right.HdrPreservationStatus;

    private static IReadOnlyList<OutputViewerCompatibilityEvidence> CreateCompatibilityViewerEvidence() =>
    [
        OutputViewerCompatibilityEvidence.ForSdrCompatibility("Microsoft Paint"),
        OutputViewerCompatibilityEvidence.ForSdrCompatibility("Windows Photos"),
        OutputViewerCompatibilityEvidence.ForSdrCompatibility("Chromium browsers"),
    ];

    private static IReadOnlyList<OutputViewerCompatibilityEvidence> CreateHdrViewerEvidence() =>
    [
        OutputViewerCompatibilityEvidence.ForHdrProfile("Microsoft Paint"),
        OutputViewerCompatibilityEvidence.ForHdrProfile("Windows Photos"),
        OutputViewerCompatibilityEvidence.ForHdrProfile("Chromium browsers"),
    ];

    private static IReadOnlyList<OutputViewerCompatibilityEvidence> CreateWideGamutViewerEvidence() =>
    [
        OutputViewerCompatibilityEvidence.ForWideGamutProfile("Microsoft Paint"),
        OutputViewerCompatibilityEvidence.ForWideGamutProfile("Windows Photos"),
        OutputViewerCompatibilityEvidence.ForWideGamutProfile("Chromium browsers"),
    ];
}

public sealed record OutputProfileEvidenceSummary(
    bool AllowsVisualMatchClaim,
    bool AllowsHdrPreservedClaim,
    string VisualMatchGateDetail,
    string HdrPreservedGateDetail);

public sealed record OutputFormatContract(
    OutputPixelFormat SourcePixelFormat,
    OutputPixelFormat DestinationPixelFormat,
    OutputTransferFunction TransferFunction,
    OutputColorPrimaries ColorPrimaries,
    OutputConversionPolicy ConversionPolicy,
    OutputMetadataPolicy MetadataPolicy,
    OutputTargetAppAssumption TargetAppAssumption)
{
    public static OutputFormatContract SrgbCompatibility { get; } =
        new(
            OutputPixelFormat.R16G16B16A16Float,
            OutputPixelFormat.Rgba8UnsignedNormalized,
            OutputTransferFunction.Srgb,
            OutputColorPrimaries.Bt709,
            OutputConversionPolicy.SdrToneMapped,
            OutputMetadataPolicy.NoHdrMetadata,
            OutputTargetAppAssumption.CompatibilityFirst);

    public static OutputFormatContract Hdr10Pending { get; } =
        new(
            OutputPixelFormat.R16G16B16A16Float,
            OutputPixelFormat.NotDefined,
            OutputTransferFunction.NotDefined,
            OutputColorPrimaries.NotDefined,
            OutputConversionPolicy.RequiredButUndefined,
            OutputMetadataPolicy.RequiredButUndefined,
            OutputTargetAppAssumption.RequiresHdrViewerValidation);

    public static OutputFormatContract DisplayP3Pending { get; } =
        new(
            OutputPixelFormat.R16G16B16A16Float,
            OutputPixelFormat.NotDefined,
            OutputTransferFunction.NotDefined,
            OutputColorPrimaries.DisplayP3,
            OutputConversionPolicy.RequiredButUndefined,
            OutputMetadataPolicy.RequiredButUndefined,
            OutputTargetAppAssumption.RequiresWideGamutViewerValidation);

    public bool IsComplete =>
        SourcePixelFormat is not OutputPixelFormat.NotDefined
        && DestinationPixelFormat is not OutputPixelFormat.NotDefined
        && TransferFunction is not OutputTransferFunction.NotDefined
        && ColorPrimaries is not OutputColorPrimaries.NotDefined
        && ConversionPolicy is not OutputConversionPolicy.RequiredButUndefined
        && MetadataPolicy is not OutputMetadataPolicy.RequiredButUndefined;
}

public sealed record OutputProfileValidationRecord(
    OutputProfileKind ProfileKind,
    IReadOnlyList<OutputViewerCompatibilityEvidence> ViewerEvidence)
{
    public OutputValidationEvidenceSource EvidenceSource { get; init; } =
        OutputValidationEvidenceSource.WindowsManual;
}

public sealed record OutputViewerCompatibilityEvidence(
    string Name,
    OutputCompatibilityEvidenceStatus ArtifactHandlingStatus,
    OutputCompatibilityEvidenceStatus VisualMatchStatus,
    OutputCompatibilityEvidenceStatus HdrPreservationStatus,
    string Detail)
{
    public static OutputViewerCompatibilityEvidence ForSdrCompatibility(string name) =>
        new(
            name,
            OutputCompatibilityEvidenceStatus.NotRun,
            OutputCompatibilityEvidenceStatus.NotRun,
            OutputCompatibilityEvidenceStatus.NotApplicable,
            "Artifact handling and visual match are not validated; HDR preservation is not applicable for this compatibility output.");

    public static OutputViewerCompatibilityEvidence ForHdrProfile(string name) =>
        new(
            name,
            OutputCompatibilityEvidenceStatus.NotRun,
            OutputCompatibilityEvidenceStatus.NotRun,
            OutputCompatibilityEvidenceStatus.NotRun,
            "Artifact handling, visual match, and HDR preservation all require Windows validation for this HDR profile.");

    public static OutputViewerCompatibilityEvidence ForWideGamutProfile(string name) =>
        new(
            name,
            OutputCompatibilityEvidenceStatus.NotRun,
            OutputCompatibilityEvidenceStatus.NotRun,
            OutputCompatibilityEvidenceStatus.NotRun,
            "Artifact handling, visual match, and wide-gamut/HDR preservation evidence require Windows validation for this profile.");
}

public enum OutputCompatibilityEvidenceStatus
{
    Pass = 0,
    Limited,
    Fail,
    NotRun,
    NotApplicable,
}

public enum OutputPixelFormat
{
    NotDefined = 0,
    R16G16B16A16Float,
    Rgba8UnsignedNormalized,
}

public enum OutputTransferFunction
{
    NotDefined = 0,
    Srgb,
    PqSt2084,
}

public enum OutputColorPrimaries
{
    NotDefined = 0,
    Bt709,
    Bt2020,
    DisplayP3,
}

public enum OutputConversionPolicy
{
    RequiredButUndefined = 0,
    SdrToneMapped,
    PreserveHdrWithDefinedToneMapping,
}

public enum OutputMetadataPolicy
{
    RequiredButUndefined = 0,
    NoHdrMetadata,
    AttachHdr10StaticMetadata,
}

public enum OutputTargetAppAssumption
{
    CompatibilityFirst = 0,
    RequiresHdrViewerValidation,
    RequiresWideGamutViewerValidation,
}

public enum OutputValidationEvidenceSource
{
    WindowsManual = 0,
    Automated = 1,
    IncompleteManualSession = 2,
}

public enum OutputProfileKind
{
    SrgbCompatibilityPng = 0,
    Hdr10Pq = 1,
    DisplayP3 = 2,
}

public enum OutputFidelityMode
{
    SdrCompatible = 0,
    VisualMatch = 1,
    HdrPreserved = 2,
    Unvalidated = 3,
}
