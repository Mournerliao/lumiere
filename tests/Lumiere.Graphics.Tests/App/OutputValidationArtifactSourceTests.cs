using Lumiere.App;
using Lumiere.Capture;
using Lumiere.Graphics.Output;
using Lumiere.Settings;
using Xunit;

namespace Lumiere.Graphics.Tests.App;

public sealed class OutputValidationArtifactSourceTests
{
    [Fact]
    public void Load_ReturnsEmptySnapshotWhenDirectoryIsMissing()
    {
        var source = new FileOutputValidationArtifactSource(
            "C:\\Missing",
            "*.json",
            directoryExists: _ => false,
            enumerateFiles: (_, _) => throw new InvalidOperationException("Directory should not be enumerated."),
            readAllText: _ => throw new InvalidOperationException("Files should not be read."));

        var snapshot = source.Load();

        Assert.Empty(snapshot.Artifacts);
        Assert.Empty(snapshot.LoadIssues);
        Assert.False(snapshot.HasArtifacts);
        Assert.False(snapshot.HasLoadIssues);
    }

    [Fact]
    public void Load_LoadsValidArtifactsInStablePathOrderAndReportsInvalidJson()
    {
        var jsonByPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["C:\\Validation\\b.json"] = CreateArtifact("2026-06-22", "Windows Photos").ToJson(),
            ["C:\\Validation\\a.json"] = CreateArtifact("2026-06-21", "Microsoft Paint").ToJson(),
            ["C:\\Validation\\bad.json"] = "{ not valid json",
        };
        var source = new FileOutputValidationArtifactSource(
            "C:\\Validation",
            "*.json",
            directoryExists: _ => true,
            enumerateFiles: (_, _) => ["C:\\Validation\\b.json", "C:\\Validation\\bad.json", "C:\\Validation\\a.json"],
            readAllText: path => jsonByPath[path]);

        var snapshot = source.Load();

        Assert.Equal(["2026-06-21", "2026-06-22"], snapshot.Artifacts.Select(artifact => artifact.Date).ToArray());
        var issue = Assert.Single(snapshot.LoadIssues);
        Assert.Equal("C:\\Validation\\bad.json", issue.Path);
        Assert.Contains("JsonException", issue.Detail, StringComparison.Ordinal);
        Assert.True(snapshot.HasArtifacts);
        Assert.True(snapshot.HasLoadIssues);
    }

    [Fact]
    public void LoadedArtifactsCanFeedSettingsProjectionWithoutBypassingRuntimeCapabilities()
    {
        var source = new FileOutputValidationArtifactSource(
            "C:\\Validation",
            "*.json",
            directoryExists: _ => true,
            enumerateFiles: (_, _) => ["C:\\Validation\\hdr10.json"],
            readAllText: _ => CreateArtifact("2026-06-21", "Windows Photos").ToJson());
        var settings = new TestSettingsProvider
        {
            ExportColorFormat = "HDR10",
        };

        var projection = SettingsPanelProjection.Project(
            settings,
            CaptureSessionState.Idle(),
            source.Load().Artifacts,
            executionCapabilities: OutputProfileExecutionCapabilities.CompatibilityOnly);

        Assert.Contains(projection.Validation.ViewerMatrix, viewer =>
            viewer.Name == "Windows Photos"
            && viewer.Status == ValidationEvidenceStatus.Pass);
        Assert.Equal("Fallback", projection.MainPanel.OutputProfile.StatusLabel);
        Assert.Equal(FidelityClaimKind.Converted, projection.MainPanel.FidelityClaim.Kind);
        Assert.DoesNotContain("HDR-preserved", projection.MainPanel.FidelityClaim.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadedSnapshotSurfacesArtifactLoadIssuesInSettingsValidationRecord()
    {
        var snapshot = new OutputValidationArtifactSnapshot(
            [CreateArtifact("2026-06-21", "Windows Photos")],
            [new("C:\\Validation\\bad.json", "JsonException: invalid JSON")]);
        var settings = new TestSettingsProvider
        {
            ExportColorFormat = "HDR10",
        };

        var projection = SettingsPanelProjection.Project(
            settings,
            CaptureSessionState.Idle(),
            snapshot,
            executionCapabilities: OutputProfileExecutionCapabilities.CompatibilityOnly);

        Assert.Equal(ValidationEvidenceStatus.Limited, projection.Validation.Record.WindowsManualValidationStatus);
        Assert.Contains("1 output validation artifact", projection.Validation.Record.WindowsManualValidationDetail);
        Assert.Contains("1 file", projection.Validation.Record.WindowsManualValidationDetail);
        Assert.Contains("bad.json", projection.Validation.Record.WindowsManualValidationDetail);
        Assert.Contains("JsonException", projection.Validation.Record.WindowsManualValidationDetail);
        Assert.Equal("docs/validation/output-validation.md", projection.Validation.Record.EvidenceDocumentPath);
        Assert.Equal("Fallback", projection.MainPanel.OutputProfile.StatusLabel);
        Assert.Equal(FidelityClaimKind.Converted, projection.MainPanel.FidelityClaim.Kind);
    }

    [Fact]
    public void LoadedSnapshotWithoutIssuesSurfacesArtifactCountAsLimitedManualEvidence()
    {
        var snapshot = new OutputValidationArtifactSnapshot(
            [
                CreateArtifact("2026-06-21", "Microsoft Paint"),
                CreateArtifact("2026-06-22", "Windows Photos"),
            ],
            []);

        var projection = SettingsPanelProjection.Project(
            new TestSettingsProvider(),
            CaptureSessionState.Idle(),
            snapshot,
            executionCapabilities: OutputProfileExecutionCapabilities.CompatibilityOnly);

        Assert.Equal(ValidationEvidenceStatus.Limited, projection.Validation.Record.WindowsManualValidationStatus);
        Assert.Contains("2 output validation artifact", projection.Validation.Record.WindowsManualValidationDetail);
        Assert.Contains("Release gates", projection.Validation.Record.WindowsManualValidationDetail);
        Assert.Equal("docs/validation/output-validation.md", projection.Validation.Record.EvidenceDocumentPath);
    }

    private static OutputValidationSessionArtifact CreateArtifact(string date, string viewerName) =>
        new(
            Date: date,
            Tester: "QA",
            BuildCommit: "31d400c",
            WindowsVersion: "Windows 11 24H2",
            Device: "HDR workstation",
            Gpu: "Test GPU",
            DisplaySetup: "HDR primary",
            HdrState: "HDR enabled",
            DpiScales: ["150%"],
            EntryPointsTested: ["Settings panel"],
            OutputTargetsTested: ["Folder"],
            TargetAppsTested: [viewerName],
            ChecklistIdsCovered: ["REL-OUT-04"],
            ResultSummary: $"{viewerName} validation passed.",
            EvidencePaths: [$"docs/validation/evidence/{viewerName}.md"],
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

    private sealed class TestSettingsProvider : ISettingsProvider
    {
        public OutputTarget OutputTarget { get; init; } = OutputTarget.Clipboard;

        public string? SavePath { get; init; }

        public bool TimestampNaming { get; init; } = true;

        public bool CopyAsImage { get; init; } = true;

        public bool HdrAlertsEnabled { get; init; } = true;

        public string FullscreenShortcut { get; init; } = string.Empty;

        public string RegionShortcut { get; init; } = string.Empty;

        public AfterCaptureBehavior AfterCaptureBehavior { get; init; } = AfterCaptureBehavior.None;

        public string ExportColorFormat { get; init; } = "sRGB";
    }
}
