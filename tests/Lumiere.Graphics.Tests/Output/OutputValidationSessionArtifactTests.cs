using Lumiere.Graphics.Output;
using Xunit;

namespace Lumiere.Graphics.Tests.Output;

public sealed class OutputValidationSessionArtifactTests
{
    [Fact]
    public void JsonRoundTrip_PreservesManualValidationSessionAndViewerEvidence()
    {
        var artifact = new OutputValidationSessionArtifact(
            Date: "2026-06-21",
            Tester: "QA",
            BuildCommit: "04a8dd6",
            WindowsVersion: "Windows 11 24H2",
            Device: "HDR workstation",
            Gpu: "Test GPU",
            DisplaySetup: "HDR primary, SDR secondary",
            HdrState: "HDR enabled on primary capture target",
            DpiScales: ["150%"],
            EntryPointsTested: ["Main panel", "Tray menu"],
            OutputTargetsTested: ["Clipboard", "Folder"],
            TargetAppsTested: ["Windows Photos"],
            ChecklistIdsCovered: ["REL-OUT-01"],
            ResultSummary: "Windows Photos preserved the HDR10 output path.",
            EvidencePaths: ["docs/validation/evidence/photos-hdr10.md"],
            KnownLimitations: ["Paint not yet validated"],
            FollowUpIssuesOrStories: ["Validate Paint and Chromium viewers"],
            OutputProfileRecords:
            [
                new(
                    OutputProfileKind.Hdr10Pq,
                    [
                        new(
                            "Windows Photos",
                            OutputCompatibilityEvidenceStatus.Pass,
                            OutputCompatibilityEvidenceStatus.Pass,
                            OutputCompatibilityEvidenceStatus.Pass,
                            "Manual HDR validation passed in Windows Photos."),
                    ]),
            ]);

        var roundTripped = OutputValidationSessionArtifact.FromJson(artifact.ToJson());

        Assert.Equal("2026-06-21", roundTripped.Date);
        Assert.Equal("04a8dd6", roundTripped.BuildCommit);
        Assert.Equal(["REL-OUT-01"], roundTripped.ChecklistIdsCovered);
        Assert.Equal(["docs/validation/evidence/photos-hdr10.md"], roundTripped.EvidencePaths);
        Assert.Equal(OutputValidationEvidenceSource.WindowsManual, roundTripped.OutputProfileRecords[0].EvidenceSource);
        var viewer = Assert.Single(roundTripped.OutputProfileRecords[0].ViewerEvidence);
        Assert.Equal("Windows Photos", viewer.Name);
        Assert.Equal(OutputCompatibilityEvidenceStatus.Pass, viewer.HdrPreservationStatus);
    }

    [Fact]
    public void ApplyTo_UpdatesMatchingProfileAndLeavesMissingViewersBlockingClaims()
    {
        var artifact = CreateArtifact(
            [
                new(
                    OutputProfileKind.Hdr10Pq,
                    [
                        new(
                            "Windows Photos",
                            OutputCompatibilityEvidenceStatus.Pass,
                            OutputCompatibilityEvidenceStatus.Pass,
                            OutputCompatibilityEvidenceStatus.Pass,
                            "Manual HDR validation passed in Windows Photos."),
                    ]),
                new(
                    OutputProfileKind.DisplayP3,
                    [
                        new(
                            "Microsoft Paint",
                            OutputCompatibilityEvidenceStatus.Pass,
                            OutputCompatibilityEvidenceStatus.Pass,
                            OutputCompatibilityEvidenceStatus.Pass,
                            "Manual P3 validation passed in Paint."),
                    ]),
            ]);
        var contract = OutputProfileContract.Hdr10Pq with
        {
            IsExecutable = true,
            FidelityMode = OutputFidelityMode.HdrPreserved,
        };

        var updated = artifact.ApplyTo(contract);

        Assert.Contains(updated.ViewerEvidence, viewer =>
            viewer.Name == "Windows Photos"
            && viewer.HdrPreservationStatus == OutputCompatibilityEvidenceStatus.Pass);
        Assert.Contains(updated.ViewerEvidence, viewer =>
            viewer.Name == "Microsoft Paint"
            && viewer.HdrPreservationStatus == OutputCompatibilityEvidenceStatus.NotRun);
        Assert.False(updated.EvaluateEvidence().AllowsHdrPreservedClaim);
    }

    [Fact]
    public void FromJson_RejectsUnsupportedSchemaVersion()
    {
        var json = CreateArtifact([]).ToJson().Replace("\"schemaVersion\": 1", "\"schemaVersion\": 99", StringComparison.Ordinal);

        var exception = Assert.Throws<InvalidOperationException>(() => OutputValidationSessionArtifact.FromJson(json));

        Assert.Contains("schema", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static OutputValidationSessionArtifact CreateArtifact(
        IReadOnlyList<OutputProfileValidationRecord> records) =>
        new(
            Date: "2026-06-21",
            Tester: "QA",
            BuildCommit: "04a8dd6",
            WindowsVersion: "Windows 11 24H2",
            Device: "HDR workstation",
            Gpu: "Test GPU",
            DisplaySetup: "HDR primary",
            HdrState: "HDR enabled",
            DpiScales: ["150%"],
            EntryPointsTested: ["Main panel"],
            OutputTargetsTested: ["Clipboard"],
            TargetAppsTested: ["Windows Photos"],
            ChecklistIdsCovered: ["REL-OUT-01"],
            ResultSummary: "Manual validation session.",
            EvidencePaths: ["docs/validation/evidence/session.md"],
            KnownLimitations: [],
            FollowUpIssuesOrStories: [],
            OutputProfileRecords: records);
}
