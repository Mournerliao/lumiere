using Lumiere.Graphics.Output;
using Xunit;

namespace Lumiere.Graphics.Tests.Output;

public sealed class Hdr10JxrViewerValidationEvidenceTests
{
    [Fact]
    public void FromArtifacts_BlocksWhenNoOutputValidationArtifactsAreLoaded()
    {
        var evidence = Hdr10JxrViewerValidationEvidence.FromArtifacts([]);

        Assert.False(evidence.IsComplete);
        Assert.False(evidence.HasArtifacts);
        Assert.True(evidence.HasCurrentBuildAlignment);
        Assert.False(evidence.HasCompleteTargetAwareHdrEvidence);
        Assert.False(evidence.HasCompleteTargetAppVersionEvidence);
        Assert.False(evidence.HasCompleteFormatContract);
        Assert.False(evidence.HasViewerRecognizedHdr10StaticMetadata);
        Assert.False(evidence.HasWindowsManualViewerValidation);
        Assert.Contains(evidence.Blockers, blocker => blocker.Contains("No folder-output validation artifacts", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FromArtifacts_BlocksWhenNamedViewersAreMissingHdr10MetadataEvidence()
    {
        var evidence = Hdr10JxrViewerValidationEvidence.FromArtifacts(
            [
                Hdr10Artifact(
                    "Windows Photos",
                    metadataStatus: OutputCompatibilityEvidenceStatus.NotRun,
                    includeFormatContract: true),
            ]);

        Assert.False(evidence.IsComplete);
        Assert.True(evidence.HasCompleteTargetAwareHdrEvidence);
        Assert.True(evidence.HasCompleteTargetAppVersionEvidence);
        Assert.True(evidence.HasCompleteFormatContract);
        Assert.False(evidence.HasViewerRecognizedHdr10StaticMetadata);
        Assert.False(evidence.HasWindowsManualViewerValidation);
        Assert.Contains(evidence.Blockers, blocker =>
            blocker.Contains("Windows Photos", StringComparison.OrdinalIgnoreCase)
            && blocker.Contains("Microsoft Paint", StringComparison.OrdinalIgnoreCase)
            && blocker.Contains("Microsoft Edge", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FromArtifacts_TreatsAutomatedViewerEvidenceAsIncompleteForReadiness()
    {
        var artifacts = RequiredHdrViewers
            .Select(viewer => Hdr10Artifact(
                viewer,
                metadataStatus: OutputCompatibilityEvidenceStatus.Pass,
                includeFormatContract: true,
                evidenceSource: OutputValidationEvidenceSource.Automated))
            .ToArray();

        var evidence = Hdr10JxrViewerValidationEvidence.FromArtifacts(artifacts);

        Assert.False(evidence.IsComplete);
        Assert.True(evidence.HasCompleteTargetAppVersionEvidence);
        Assert.False(evidence.HasCompleteFormatContract);
        Assert.False(evidence.HasViewerRecognizedHdr10StaticMetadata);
        Assert.False(evidence.HasWindowsManualViewerValidation);
        Assert.Contains(evidence.Blockers, blocker => blocker.Contains("format contract", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(evidence.Blockers, blocker => blocker.Contains("Windows manual viewer validation", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FromArtifacts_AllowsCompleteManualViewerMetadataEvidenceToSatisfyJxrViewerGates()
    {
        var artifacts = RequiredHdrViewers
            .Select((viewer, index) => Hdr10Artifact(
                viewer,
                metadataStatus: OutputCompatibilityEvidenceStatus.Pass,
                includeFormatContract: index == 0))
            .ToArray();

        var evidence = Hdr10JxrViewerValidationEvidence.FromArtifacts(artifacts);

        Assert.True(evidence.IsComplete);
        Assert.True(evidence.HasArtifacts);
        Assert.True(evidence.HasCompleteTargetAwareHdrEvidence);
        Assert.True(evidence.HasCompleteTargetAppVersionEvidence);
        Assert.True(evidence.HasCompleteFormatContract);
        Assert.True(evidence.HasViewerRecognizedHdr10StaticMetadata);
        Assert.True(evidence.HasWindowsManualViewerValidation);
        Assert.Empty(evidence.Blockers);
    }

    [Fact]
    public void FromArtifacts_WithCurrentBuildVersion_AllowsCompleteEvidenceOnlyWhenBuildMatches()
    {
        var artifacts = RequiredHdrViewers
            .Select((viewer, index) => Hdr10Artifact(
                viewer,
                metadataStatus: OutputCompatibilityEvidenceStatus.Pass,
                includeFormatContract: index == 0))
            .ToArray();

        var evidence = Hdr10JxrViewerValidationEvidence.FromArtifacts(artifacts, "2.3.4+d54155c");

        Assert.True(evidence.IsComplete);
        Assert.True(evidence.HasCurrentBuildAlignment);
        Assert.Empty(evidence.Blockers);
    }

    [Fact]
    public void FromArtifacts_WithCurrentBuildVersion_BlocksStaleEvidence()
    {
        var artifacts = RequiredHdrViewers
            .Select((viewer, index) => Hdr10Artifact(
                viewer,
                metadataStatus: OutputCompatibilityEvidenceStatus.Pass,
                includeFormatContract: index == 0))
            .ToArray();

        var evidence = Hdr10JxrViewerValidationEvidence.FromArtifacts(artifacts, "2.3.4+deadbee");

        Assert.False(evidence.IsComplete);
        Assert.False(evidence.HasCurrentBuildAlignment);
        Assert.Contains(evidence.Blockers, blocker => blocker.Contains("stale", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FromArtifacts_IgnoresClipboardOnlyArtifactsForHdr10JxrFolderReleaseGate()
    {
        var artifacts = RequiredHdrViewers
            .Select((viewer, index) => Hdr10Artifact(
                viewer,
                metadataStatus: OutputCompatibilityEvidenceStatus.Pass,
                includeFormatContract: index == 0,
                outputTarget: "Clipboard"))
            .ToArray();

        var evidence = Hdr10JxrViewerValidationEvidence.FromArtifacts(artifacts);

        Assert.False(evidence.IsComplete);
        Assert.False(evidence.HasArtifacts);
        Assert.Contains(evidence.Blockers, blocker => blocker.Contains("folder-output", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FromArtifacts_BlocksWhenTargetAppVersionsAreMissingForNamedViewers()
    {
        var artifacts = RequiredHdrViewers
            .Select((viewer, index) => Hdr10Artifact(
                viewer,
                metadataStatus: OutputCompatibilityEvidenceStatus.Pass,
                includeFormatContract: index == 0,
                includeTargetAppVersion: viewer == "Windows Photos"))
            .ToArray();

        var evidence = Hdr10JxrViewerValidationEvidence.FromArtifacts(artifacts);

        Assert.False(evidence.IsComplete);
        Assert.False(evidence.HasCompleteTargetAppVersionEvidence);
        Assert.Contains(evidence.Blockers, blocker =>
            blocker.Contains("Target app version evidence", StringComparison.OrdinalIgnoreCase)
            && blocker.Contains("Microsoft Paint", StringComparison.OrdinalIgnoreCase)
            && blocker.Contains("Microsoft Edge", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FromArtifacts_UsesRecordLevelOutputTargetsWhenArtifactSessionCoversBoth()
    {
        var artifacts = RequiredHdrViewers
            .Select((viewer, index) => Hdr10Artifact(
                viewer,
                metadataStatus: OutputCompatibilityEvidenceStatus.Pass,
                includeFormatContract: index == 0,
                outputTarget: "Both",
                recordOutputTargetsCovered: ["Clipboard"]))
            .ToArray();

        var evidence = Hdr10JxrViewerValidationEvidence.FromArtifacts(artifacts);

        Assert.False(evidence.IsComplete);
        Assert.False(evidence.HasArtifacts);
        Assert.Contains(evidence.Blockers, blocker => blocker.Contains("folder-output", StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<string> RequiredHdrViewers { get; } =
        ["Microsoft Paint", "Windows Photos", "Microsoft Edge"];

    private static OutputValidationSessionArtifact Hdr10Artifact(
        string viewerName,
        OutputCompatibilityEvidenceStatus metadataStatus,
        bool includeFormatContract,
        OutputValidationEvidenceSource evidenceSource = OutputValidationEvidenceSource.WindowsManual,
        string outputTarget = "Folder",
        IReadOnlyList<string>? recordOutputTargetsCovered = null,
        bool includeTargetAppVersion = true) =>
        new(
            Date: "2026-06-22",
            Tester: "QA",
            BuildCommit: "d54155c",
            WindowsVersion: "Windows 11 24H2",
            Device: "HDR workstation",
            Gpu: "Test GPU",
            DisplaySetup: "HDR primary",
            HdrState: "HDR enabled",
            DpiScales: ["150%"],
            EntryPointsTested: ["Settings panel"],
            OutputTargetsTested: [outputTarget],
            TargetAppsTested: [viewerName],
            ChecklistIdsCovered: ["REL-OUT-04", "REL-HDR-04"],
            ResultSummary: $"{viewerName} HDR10 JXR validation passed.",
            EvidencePaths: [$"docs/validation/evidence/{viewerName}.md"],
            KnownLimitations: [],
            FollowUpIssuesOrStories: [],
            OutputProfileRecords:
            [
                new(
                    OutputProfileKind.Hdr10Pq,
                    [
                        new(
                            viewerName,
                            OutputCompatibilityEvidenceStatus.Pass,
                            OutputCompatibilityEvidenceStatus.Pass,
                            OutputCompatibilityEvidenceStatus.Pass,
                            "Validated HDR10 JXR viewer.")
                        {
                            Hdr10MetadataStatus = metadataStatus,
                        },
                    ])
                {
                    EvidenceSource = evidenceSource,
                    FormatContract = includeFormatContract ? CompleteHdr10Contract : null,
                    OutputTargetsCovered = recordOutputTargetsCovered ?? [],
                },
            ])
        {
            TargetAppVersions = includeTargetAppVersion
                ?
                [
                    new OutputValidationTargetAppVersionRecord(
                        viewerName,
                        $"{viewerName} 1.0"),
                ]
                : [],
            TargetHdrEvidence = CompleteTargetHdrEvidence,
        };

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
