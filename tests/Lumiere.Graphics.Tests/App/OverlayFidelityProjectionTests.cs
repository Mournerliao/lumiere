using Lumiere.App;
using Lumiere.Graphics.Hdr;
using Lumiere.Graphics.Output;
using Xunit;

namespace Lumiere.Graphics.Tests.App;

public sealed class OverlayFidelityProjectionTests
{
    [Fact]
    public void CreateOverlayFidelityCue_BlocksHdrPreservedWhenTargetAwareReadinessIsUnresolved()
    {
        var readiness = PreviewReadinessStatus.Degraded(
            PreviewReadinessStage.Presentation,
            "HDR readiness is unvalidated for the selected capture target.",
            "Target-aware display capability could not be matched to a DXGI output.",
            PreviewReadinessReason.TargetDisplayUnresolved);
        OutputValidationSessionArtifact[] artifacts =
        [
            ArtifactWithFormatContract("Microsoft Paint"),
            ArtifactWithFormatContract("Windows Photos"),
            ArtifactWithFormatContract("Microsoft Edge"),
        ];

        var cue = OverlayFidelityProjection.Project(
            "HDR10",
            readiness,
            artifacts,
            ValidateOnlyHdr10Capabilities(artifacts));

        Assert.Equal(OverlayFidelityClaimProjection.Unvalidated, cue.Kind);
        Assert.Equal("HDR10 · Ready", cue.Label);
        Assert.Contains("target-aware HDR readiness is unvalidated", cue.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateOverlayFidelityCue_AllowsHdrPreservedWhenReadinessAndRuntimeCapabilityPass()
    {
        var readiness = PreviewReadinessStatus.Ready("HDR ready", "Target-aware readiness passed.");
        OutputValidationSessionArtifact[] artifacts =
        [
            ArtifactWithFormatContract("Microsoft Paint"),
            ArtifactWithFormatContract("Windows Photos"),
            ArtifactWithFormatContract("Microsoft Edge"),
        ];

        var cue = OverlayFidelityProjection.Project(
            "HDR10",
            readiness,
            artifacts,
            ValidateOnlyHdr10Capabilities(artifacts));

        Assert.Equal(OverlayFidelityClaimProjection.HdrPreserved, cue.Kind);
        Assert.Equal("HDR10 · Ready", cue.Label);
        Assert.Contains("validated session", cue.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("HDR-preserved", cue.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateOverlayFidelityCue_SurfacesValidateGateWhenViewerEvidenceIsStillIncomplete()
    {
        var readiness = PreviewReadinessStatus.Ready("HDR ready", "Target-aware readiness passed.");
        OutputValidationSessionArtifact[] artifacts =
        [
            ArtifactWithIncompleteViewerEvidence("Microsoft Paint"),
            ArtifactWithIncompleteViewerEvidence("Windows Photos"),
            ArtifactWithIncompleteViewerEvidence("Microsoft Edge"),
        ];

        var cue = OverlayFidelityProjection.Project(
            "HDR10",
            readiness,
            artifacts,
            ValidateOnlyHdr10Capabilities(artifacts));

        Assert.Equal(OverlayFidelityClaimProjection.Converted, cue.Kind);
        Assert.Equal("HDR10 · Validate", cue.Label);
        Assert.Contains("Windows manual viewer evidence", cue.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sRGB compatibility fallback", cue.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Converted output", cue.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateOverlayFidelityCue_SurfacesCompatGateForSrgbFallbackProfile()
    {
        var cue = OverlayFidelityProjection.Project(
            "sRGB",
            PreviewReadinessStatus.Ready("HDR ready", "Target-aware readiness passed."),
            [],
            OutputProfileExecutionCapabilities.CompatibilityOnly);

        Assert.Equal(OverlayFidelityClaimProjection.Converted, cue.Kind);
        Assert.Equal("sRGB · Compat", cue.Label);
        Assert.Contains("Compatible output", cue.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Converted output", cue.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateOverlayFidelityCue_ClipboardTargetKeepsHdr10OnCompatibilityPath()
    {
        var readiness = PreviewReadinessStatus.Ready("HDR ready", "Target-aware readiness passed.");
        OutputValidationSessionArtifact[] artifacts =
        [
            ArtifactWithFormatContract("Microsoft Paint"),
            ArtifactWithFormatContract("Windows Photos"),
            ArtifactWithFormatContract("Microsoft Edge"),
        ];

        var cue = OverlayFidelityProjection.Project(
            "HDR10",
            readiness,
            artifacts,
            ValidateOnlyHdr10Capabilities(artifacts),
            OutputTarget.Clipboard);

        Assert.Equal(OverlayFidelityClaimProjection.Converted, cue.Kind);
        Assert.Equal("HDR10 · Compat", cue.Label);
        Assert.Contains("clipboard output stays on sRGB compatibility output", cue.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateOverlayFidelityCue_BothTargetKeepsOverallClaimConvertedEvenWhenFolderReady()
    {
        var readiness = PreviewReadinessStatus.Ready("HDR ready", "Target-aware readiness passed.");
        OutputValidationSessionArtifact[] artifacts =
        [
            ArtifactWithFormatContract("Microsoft Paint"),
            ArtifactWithFormatContract("Windows Photos"),
            ArtifactWithFormatContract("Microsoft Edge"),
        ];

        var cue = OverlayFidelityProjection.Project(
            "HDR10",
            readiness,
            artifacts,
            ValidateOnlyHdr10Capabilities(artifacts),
            OutputTarget.Both);

        Assert.Equal(OverlayFidelityClaimProjection.Converted, cue.Kind);
        Assert.Equal("HDR10 · Ready", cue.Label);
        Assert.Contains("Both-target output still keeps clipboard on sRGB compatibility fallback", cue.Detail, StringComparison.OrdinalIgnoreCase);
    }

    private static OutputValidationSessionArtifact ArtifactWithFormatContract(string viewerName) =>
        new(
            Date: "2026-06-21",
            Tester: "QA",
            BuildCommit: "72c3be7",
            WindowsVersion: "Windows 11 24H2",
            Device: "HDR workstation",
            Gpu: "Test GPU",
            DisplaySetup: "HDR primary",
            HdrState: "HDR enabled",
            DpiScales: ["150%"],
            EntryPointsTested: ["Overlay"],
            OutputTargetsTested: ["Folder"],
            TargetAppsTested: [viewerName],
            ChecklistIdsCovered: ["REL-OUT-01"],
            ResultSummary: $"{viewerName} HDR validation passed.",
            EvidencePaths: [$"knowledge/validation/evidence/{viewerName}.md"],
            KnownLimitations: [],
            FollowUpIssuesOrStories: [],
            OutputProfileRecords:
            [
                new(
                    OutputProfileKind.Hdr10Pq,
                    [
                        PassingHdrViewer(viewerName),
                    ])
                {
                    FormatContract = CompleteHdr10Contract,
                },
            ])
        {
            TargetAppVersions =
            [
                new OutputValidationTargetAppVersionRecord(
                    viewerName,
                    $"{viewerName} 1.0"),
            ],
            TargetHdrEvidence = CompleteTargetHdrEvidence,
        };

    private static OutputValidationSessionArtifact ArtifactWithIncompleteViewerEvidence(string viewerName) =>
        new(
            Date: "2026-06-21",
            Tester: "QA",
            BuildCommit: "72c3be7",
            WindowsVersion: "Windows 11 24H2",
            Device: "HDR workstation",
            Gpu: "Test GPU",
            DisplaySetup: "HDR primary",
            HdrState: "HDR enabled",
            DpiScales: ["150%"],
            EntryPointsTested: ["Overlay"],
            OutputTargetsTested: ["Folder"],
            TargetAppsTested: [viewerName],
            ChecklistIdsCovered: ["REL-OUT-01"],
            ResultSummary: $"{viewerName} HDR validation is incomplete.",
            EvidencePaths: [$"knowledge/validation/evidence/{viewerName}.md"],
            KnownLimitations: ["Viewer evidence still incomplete."],
            FollowUpIssuesOrStories: ["11-3"],
            OutputProfileRecords:
            [
                new(
                    OutputProfileKind.Hdr10Pq,
                    [
                        PassingHdrViewer(viewerName) with
                        {
                            Hdr10MetadataStatus = OutputCompatibilityEvidenceStatus.NotRun,
                        },
                    ])
                {
                    FormatContract = CompleteHdr10Contract,
                },
            ])
        {
            TargetAppVersions =
            [
                new OutputValidationTargetAppVersionRecord(
                    viewerName,
                    $"{viewerName} 1.0"),
            ],
            TargetHdrEvidence = CompleteTargetHdrEvidence,
        };

    private static OutputViewerCompatibilityEvidence PassingHdrViewer(string name) =>
        new(
            name,
            OutputCompatibilityEvidenceStatus.Pass,
            OutputCompatibilityEvidenceStatus.Pass,
            OutputCompatibilityEvidenceStatus.Pass,
            "Validated HDR viewer.")
        {
            Hdr10MetadataStatus = OutputCompatibilityEvidenceStatus.Pass,
        };

    private static OutputProfileExecutionCapabilities ValidateOnlyHdr10Capabilities(
        IEnumerable<OutputValidationSessionArtifact> artifacts) =>
        OutputProfileExecutionCapabilities.ResolveHdr10JxrReleaseCapabilities(
            ReadyHdr10JxrReadiness,
            artifacts);

    private static Hdr10JxrCodecReadiness ReadyHdr10JxrReadiness { get; } =
        new(
            HasNativeWicJpegXrEncoder: true,
            AcceptsRgba16FloatSource: true,
            WritesAuditMetadata: true,
            HasArtifactAuditMetadataRoundTripEvidence: true,
            HasViewerRecognizedHdr10StaticMetadata: true,
            Hdr10StaticMetadataPolicy: Hdr10StaticMetadataPolicy.Bt2020PqReference1000Nit,
            HasWindowsManualViewerValidation: true,
            Blockers: []);

    private static OutputFormatContract CompleteHdr10Contract { get; } =
        new(
            OutputPixelFormat.R16G16B16A16Float,
            OutputPixelFormat.R16G16B16A16Float,
            OutputTransferFunction.PqSt2084,
            OutputColorPrimaries.Bt2020,
            OutputConversionPolicy.PreserveHdrWithDefinedToneMapping,
            OutputMetadataPolicy.AttachHdr10StaticMetadata,
            OutputTargetAppAssumption.RequiresHdrViewerValidation,
            Hdr10StaticMetadataPolicy.Bt2020PqReference1000Nit);

    private static TargetAwareHdrValidationEvidence CompleteTargetHdrEvidence { get; } =
        new(
            TargetDisplayName: "HDR primary",
            Left: 0,
            Top: 0,
            Width: 3840,
            Height: 2160,
            MatchKind: "DesktopBounds",
            HdrState: "Active",
            ColorSpace: "RgbFullG2084NoneP2020",
            Detail: "Validated target-aware HDR match evidence.");
}
