using Lumiere.App;
using Lumiere.Graphics.Output;
using Xunit;

namespace Lumiere.Graphics.Tests.App;

public sealed class OutputValidationRunPlannerTests
{
    [Fact]
    public void Create_UsesProfileRecordOutputTargetScopeForMissingOutputTargets()
    {
        var artifact = Hdr10Artifact(
            outputTargetsTested: ["Both"],
            outputTargetsCovered: ["Clipboard"]);

        var plan = OutputValidationRunPlanner.Create(
            [artifact],
            OutputProfileContract.Hdr10Pq);

        Assert.Contains("Folder", plan.MissingOutputTargets);
        Assert.Contains("Both", plan.MissingOutputTargets);
        Assert.DoesNotContain("Clipboard", plan.MissingOutputTargets);
        var recommendation = Assert.IsType<string>(plan.CreateNextWindowsRunRecommendation());
        Assert.Contains("validate Folder output", recommendation);
    }

    [Fact]
    public void Create_TreatsSessionOutputTargetAsProfileScopeWhenRecordDoesNotNarrowIt()
    {
        var artifact = Hdr10Artifact(
            outputTargetsTested: ["Both"],
            outputTargetsCovered: []);

        var plan = OutputValidationRunPlanner.Create(
            [artifact],
            OutputProfileContract.Hdr10Pq);

        Assert.Empty(plan.MissingOutputTargets);
    }

    [Fact]
    public void Create_KeepsViewerTargetMissingUntilRequiredHdr10EvidencePasses()
    {
        var artifact = Hdr10Artifact(
            outputTargetsTested: ["Folder"],
            outputTargetsCovered: ["Folder"],
            viewerEvidence:
            [
                PassingHdrViewer("Microsoft Paint"),
                PassingHdrViewer("Windows Photos") with
                {
                    Hdr10MetadataStatus = OutputCompatibilityEvidenceStatus.NotRun,
                },
                PassingHdrViewer("Microsoft Edge"),
            ]);

        var plan = OutputValidationRunPlanner.Create(
            [artifact],
            OutputProfileContract.Hdr10Pq);

        Assert.Equal(["Windows Photos"], plan.MissingViewerTargets);
        var recommendation = Assert.IsType<string>(plan.CreateNextWindowsRunRecommendation());
        Assert.Contains("Windows Photos", recommendation);
    }

    private static OutputValidationSessionArtifact Hdr10Artifact(
        IReadOnlyList<string> outputTargetsTested,
        IReadOnlyList<string> outputTargetsCovered,
        IReadOnlyList<OutputViewerCompatibilityEvidence>? viewerEvidence = null) =>
        new(
            Date: "2026-06-29",
            Tester: "QA",
            BuildCommit: "d54155c",
            WindowsVersion: "Windows 11 24H2",
            Device: "HDR workstation",
            Gpu: "Test GPU",
            DisplaySetup: "Topology: Single HDR-capable display with Windows HDR enabled",
            HdrState: "HDR enabled",
            DpiScales: ["150%"],
            EntryPointsTested: ["Main panel", "Tray menu", "Global hotkey"],
            OutputTargetsTested: outputTargetsTested,
            TargetAppsTested: ["Microsoft Paint", "Windows Photos", "Microsoft Edge"],
            ChecklistIdsCovered: ["REL-OUT-04", "REL-HDR-04"],
            ResultSummary: "HDR10 validation evidence rehearsal.",
            EvidencePaths: ["evidence\\hdr10-session.md"],
            KnownLimitations: [],
            FollowUpIssuesOrStories: [],
            OutputProfileRecords:
            [
                new(
                    OutputProfileKind.Hdr10Pq,
                    viewerEvidence ??
                    [
                        PassingHdrViewer("Microsoft Paint"),
                        PassingHdrViewer("Windows Photos"),
                        PassingHdrViewer("Microsoft Edge"),
                    ])
                {
                    OutputTargetsCovered = outputTargetsCovered,
                },
            ])
        {
            TargetAppVersions =
            [
                new OutputValidationTargetAppVersionRecord("Microsoft Paint", "Microsoft Paint 1.0"),
                new OutputValidationTargetAppVersionRecord("Windows Photos", "Windows Photos 1.0"),
                new OutputValidationTargetAppVersionRecord("Microsoft Edge", "Microsoft Edge 1.0"),
            ],
            TargetHdrEvidence = CompleteTargetHdrEvidence,
        };

    private static OutputViewerCompatibilityEvidence PassingHdrViewer(string name) =>
        new(
            name,
            OutputCompatibilityEvidenceStatus.Pass,
            OutputCompatibilityEvidenceStatus.Pass,
            OutputCompatibilityEvidenceStatus.Pass,
            "Validated HDR10 viewer.")
        {
            Hdr10MetadataStatus = OutputCompatibilityEvidenceStatus.Pass,
        };

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
