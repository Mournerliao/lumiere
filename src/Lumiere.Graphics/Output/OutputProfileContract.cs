namespace Lumiere.Graphics.Output;

/// <summary>
/// Describes the fidelity semantics for a concrete output profile.
/// </summary>
public sealed record OutputProfileContract(
    OutputProfileKind Kind,
    string Label,
    bool IsExecutable,
    OutputFidelityMode FidelityMode,
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
            "FP16/scRGB capture source",
            "Display P3 output contract pending implementation",
            "Wide-gamut conversion policy must be specified before use.",
            "Color profile and metadata attachment policy are not validated.",
            "Target-app compatibility matrix is not run.",
            CreateWideGamutViewerEvidence());

    public bool AllowsHdrPreservedClaim =>
        IsExecutable && FidelityMode is OutputFidelityMode.HdrPreserved;

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
        IsExecutable ? this : SrgbCompatibilityPng;

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
