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
        Assert.Equal(
            ["C:\\Validation\\a.json", "C:\\Validation\\b.json"],
            snapshot.ArtifactReferences.Select(reference => reference.Path).ToArray());
        var issue = Assert.Single(snapshot.LoadIssues);
        Assert.Equal("C:\\Validation\\bad.json", issue.Path);
        Assert.Contains("JsonException", issue.Detail, StringComparison.Ordinal);
        Assert.True(snapshot.HasArtifacts);
        Assert.True(snapshot.HasLoadIssues);
    }

    [Fact]
    public void Load_PreparesWorkspaceAndSeedsSampleTemplateWhenEnabled()
    {
        var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        const string templateJson = "{ \"schemaVersion\": 4 }";
        var source = new FileOutputValidationArtifactSource(
            "C:\\Validation",
            "*.json",
            directoryExists: directories.Contains,
            fileExists: files.ContainsKey,
            createDirectory: path => directories.Add(path),
            enumerateFiles: (_, _) => Array.Empty<string>(),
            readAllText: path => files[path],
            writeAllText: (path, content) => files[path] = content,
            resolveTemplateSourceText: () => templateJson,
            prepareWorkspace: true);

        var snapshot = source.Load();

        Assert.True(snapshot.Workspace.IsReady);
        Assert.Equal("C:\\Validation", snapshot.Workspace.DirectoryPath);
        Assert.Equal("C:\\Validation\\templates", snapshot.Workspace.TemplatesDirectoryPath);
        Assert.Equal("C:\\Validation\\guidance", snapshot.Workspace.GuidanceDirectoryPath);
        Assert.Equal("C:\\Validation\\evidence", snapshot.Workspace.EvidenceDirectoryPath);
        Assert.Equal("C:\\Validation\\README.txt", snapshot.Workspace.GuidanceFilePath);
        Assert.Equal("C:\\Validation\\templates\\output-validation-session.schema-v4.sample.json", snapshot.Workspace.SampleTemplatePath);
        Assert.Equal("C:\\Validation\\guidance\\release-validation-checklist.md", snapshot.Workspace.ReleaseChecklistPath);
        Assert.Equal("C:\\Validation\\guidance\\hdr-sdr-validation-scenarios.md", snapshot.Workspace.HdrSdrScenariosPath);
        Assert.Equal("C:\\Validation\\guidance\\settings-accessibility-validation.md", snapshot.Workspace.SettingsAccessibilityGuidePath);
        Assert.Equal("C:\\Validation\\templates\\resource-trend-session-template.md", snapshot.Workspace.ResourceTrendTemplatePath);
        Assert.Equal("C:\\Validation\\collect-resource-trend-samples.ps1", snapshot.Workspace.ResourceTrendScriptPath);
        Assert.Equal(templateJson, files[snapshot.Workspace.SampleTemplatePath!]);
        Assert.Contains("Session Metadata", files["C:\\Validation\\templates\\hdr-sdr-validation-session-template.md"], StringComparison.Ordinal);
        Assert.Contains("Public perfect-HDR-fidelity", files[snapshot.Workspace.ReleaseChecklistPath!], StringComparison.Ordinal);
        Assert.Contains("Standard Content Set", files[snapshot.Workspace.HdrSdrScenariosPath!], StringComparison.Ordinal);
        Assert.Contains("Keyboard Validation", files[snapshot.Workspace.SettingsAccessibilityGuidePath!], StringComparison.Ordinal);
        Assert.Contains("Session Metadata", files[snapshot.Workspace.ResourceTrendTemplatePath!], StringComparison.Ordinal);
        Assert.Contains("Collects repeated resource trend samples", files[snapshot.Workspace.ResourceTrendScriptPath!], StringComparison.Ordinal);
        Assert.Contains("templates\\hdr-sdr-validation-session-template.md", files[snapshot.Workspace.GuidanceFilePath], StringComparison.Ordinal);
        Assert.Contains("guidance\\release-validation-checklist.md", files[snapshot.Workspace.GuidanceFilePath], StringComparison.Ordinal);
    }

    [Fact]
    public void Load_ReturnsWorkspaceIssueWhenTemplateCannotBeSeeded()
    {
        var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "C:\\Validation",
            "C:\\Validation\\templates",
            "C:\\Validation\\evidence",
        };
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var source = new FileOutputValidationArtifactSource(
            "C:\\Validation",
            "*.json",
            directoryExists: directories.Contains,
            fileExists: files.ContainsKey,
            createDirectory: path => directories.Add(path),
            enumerateFiles: (_, _) => Array.Empty<string>(),
            readAllText: path => files[path],
            writeAllText: (path, content) => files[path] = content,
            resolveTemplateSourceText: () => null,
            prepareWorkspace: true);

        var snapshot = source.Load();

        Assert.False(snapshot.Workspace.IsReady);
        var issue = Assert.Single(snapshot.Workspace.Issues);
        Assert.Contains("sample template", issue.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(snapshot.Artifacts);
        Assert.Empty(snapshot.LoadIssues);
    }

    [Fact]
    public void CreateDraft_WritesPrefilledDraftIntoWorkspaceRoot()
    {
        var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var source = new FileOutputValidationArtifactSource(
            "C:\\Validation",
            "*.json",
            directoryExists: directories.Contains,
            fileExists: files.ContainsKey,
            createDirectory: path => directories.Add(path),
            enumerateFiles: (_, _) => Array.Empty<string>(),
            readAllText: path => files[path],
            writeAllText: (path, content) => files[path] = content,
            resolveTemplateSourceText: () => "{ \"schemaVersion\": 4 }",
            getNow: () => new DateTimeOffset(2026, 06, 22, 10, 30, 00, TimeSpan.FromHours(8)),
            targetAppVersionPrefillProvider: new StubTargetAppVersionPrefillProvider(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Microsoft Paint"] = "11.2504.451.0",
                    ["Windows Photos"] = "2026.11040.12001.0",
                }),
            prepareWorkspace: true);

        var result = source.CreateDraft(
            new OutputValidationDraftRequest(
                "0.1.0",
                OutputTarget.Folder,
                OutputProfileContract.Hdr10Pq,
                CaptureSessionState.Capturing(
                    CaptureTarget.CreateForTest(
                        new Windows.Graphics.SizeInt32
                        {
                            Width = 3840,
                            Height = 2160,
                        },
                        "HDR Display",
                        CaptureTargetKind.Display,
                        new DisplayOutputIdentity("\\\\.\\DISPLAY1", left: 0, top: 0, width: 3840, height: 2160)),
                    Lumiere.Graphics.Hdr.PreviewReadinessStatus.Ready(
                        "HDR preview path is validated.",
                        "IDXGISwapChain3.SetColorSpace1 set RgbFullG2084NoneP2020; display match=DesktopBounds."))));

        Assert.True(result.IsSuccess);
        Assert.Equal("C:\\Validation\\output-validation-draft-2026-06-22-hdr10-folder.json", result.DraftPath);
        Assert.True(files.ContainsKey(result.DraftPath!));
        var artifact = OutputValidationSessionArtifact.FromJson(files[result.DraftPath!]);
        Assert.Equal(["Folder"], artifact.OutputTargetsTested);
        Assert.Equal("HDR Display", artifact.TargetHdrEvidence!.TargetDisplayName);
        Assert.Contains("REL-HDR-04", artifact.ChecklistIdsCovered);
        Assert.Contains("Suggested next Windows run", artifact.ResultSummary, StringComparison.Ordinal);
        Assert.Contains("suggested next topology", artifact.DisplaySetup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            artifact.TargetAppVersions,
            version => version.Name == "Microsoft Paint"
                && version.Version == "11.2504.451.0");
        Assert.Contains(
            artifact.TargetAppVersions,
            version => version.Name == "Windows Photos"
                && version.Version == "2026.11040.12001.0");
    }

    [Fact]
    public void CreateDraft_FailsWhenWorkspaceIsNotReady()
    {
        var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "C:\\Validation",
            "C:\\Validation\\templates",
            "C:\\Validation\\evidence",
        };
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var source = new FileOutputValidationArtifactSource(
            "C:\\Validation",
            "*.json",
            directoryExists: directories.Contains,
            fileExists: files.ContainsKey,
            createDirectory: path => directories.Add(path),
            enumerateFiles: (_, _) => Array.Empty<string>(),
            readAllText: path => files[path],
            writeAllText: (path, content) => files[path] = content,
            resolveTemplateSourceText: () => null,
            getNow: () => new DateTimeOffset(2026, 06, 22, 10, 30, 00, TimeSpan.FromHours(8)),
            prepareWorkspace: true);

        var result = source.CreateDraft(
            new OutputValidationDraftRequest(
                "0.1.0",
                OutputTarget.Folder,
                OutputProfileContract.Hdr10Pq,
                CaptureSessionState.Idle()));

        Assert.False(result.IsSuccess);
        Assert.Contains("template", result.TechnicalDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Null(result.DraftPath);
    }

    [Fact]
    public void CreateResourceTrendDraft_WritesPrefilledMarkdownIntoWorkspaceRoot()
    {
        var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var source = new FileOutputValidationArtifactSource(
            "C:\\Validation",
            "*.json",
            directoryExists: directories.Contains,
            fileExists: files.ContainsKey,
            createDirectory: path => directories.Add(path),
            enumerateFiles: (_, _) => Array.Empty<string>(),
            readAllText: path => files[path],
            writeAllText: (path, content) => files[path] = content,
            resolveTemplateSourceText: () => "{ \"schemaVersion\": 4 }",
            getNow: () => new DateTimeOffset(2026, 06, 23, 11, 30, 00, TimeSpan.FromHours(8)),
            prepareWorkspace: true);

        var result = source.CreateResourceTrendDraft(
            new ResourceTrendValidationDraftRequest(
                "2.3.4+72c3be7",
                OutputTarget.Both,
                CaptureSessionState.Capturing(
                    CaptureTarget.CreateForTest(
                        new Windows.Graphics.SizeInt32
                        {
                            Width = 3840,
                            Height = 2160,
                        },
                        "HDR Display",
                        CaptureTargetKind.Display),
                    Lumiere.Graphics.Hdr.PreviewReadinessStatus.Ready(
                        "HDR preview path is validated.",
                        "Target-aware readiness passed.")),
                4242,
                "& \"C:\\Validation\\collect-resource-trend-samples.ps1\" -ProcessId 4242 -DurationSeconds 900 -SampleIntervalSeconds 5 -OutputDirectory \"C:\\Validation\\resource-trends\"",
                new OutputValidationCurrentSessionHint(
                    "NVIDIA RTX 5080",
                    ["150%"],
                    "2 displays; active target HDR Display at 0,0 3840x2160")));

        Assert.True(result.IsSuccess);
        Assert.Equal("C:\\Validation\\resource-trend-session-2026-06-23.md", result.DraftPath);
        Assert.True(files.ContainsKey(result.DraftPath!));
        Assert.Contains("- Output configuration: Both", files[result.DraftPath!], StringComparison.Ordinal);
        Assert.Contains("- Lumiere process ID: 4242 (current session)", files[result.DraftPath!], StringComparison.Ordinal);
        Assert.Contains("resource-trend-Lumiere.App-pid4242-REPLACE_WITH_TIMESTAMP.csv", files[result.DraftPath!], StringComparison.Ordinal);
    }

    [Fact]
    public void CreateDraft_CarriesLatestCompatibleArtifactContextIntoManualPlaceholders()
    {
        var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["C:\\Validation\\existing.json"] = (CreateArtifact("2026-06-22", "Windows Photos") with
            {
                Tester = "QA",
                WindowsVersion = "Windows 11 24H2",
                Device = "HDR workstation",
                Gpu = "NVIDIA test GPU",
                DisplaySetup = "HDR primary, SDR secondary",
                DpiScales = ["150%"],
                EntryPointsTested = ["Tray menu"],
            }).ToJson(),
        };
        var source = new FileOutputValidationArtifactSource(
            "C:\\Validation",
            "*.json",
            directoryExists: path => directories.Contains(path) || path == "C:\\Validation",
            fileExists: files.ContainsKey,
            createDirectory: path => directories.Add(path),
            enumerateFiles: (_, _) => ["C:\\Validation\\existing.json"],
            readAllText: path => files[path],
            writeAllText: (path, content) => files[path] = content,
            resolveTemplateSourceText: () => "{ \"schemaVersion\": 4 }",
            getNow: () => new DateTimeOffset(2026, 06, 22, 10, 30, 00, TimeSpan.FromHours(8)),
            prepareWorkspace: true);

        var result = source.CreateDraft(
            new OutputValidationDraftRequest(
                "0.1.0",
                OutputTarget.Folder,
                OutputProfileContract.Hdr10Pq,
                CaptureSessionState.Capturing(
                    CaptureTarget.CreateForTest(
                        new Windows.Graphics.SizeInt32
                        {
                            Width = 3840,
                            Height = 2160,
                        },
                        "HDR Display",
                        CaptureTargetKind.Display),
                    Lumiere.Graphics.Hdr.PreviewReadinessStatus.Ready(
                        "HDR preview path is validated.",
                        "Target-aware readiness passed.")),
                new OutputValidationCurrentSessionHint(
                    "NVIDIA RTX 5080",
                    ["175%"],
                    "2 displays; active target HDR Display at 0,0 3840x2160")));

        Assert.True(result.IsSuccess);
        var artifact = OutputValidationSessionArtifact.FromJson(files[result.DraftPath!]);
        Assert.Equal("REPLACE_WITH_TESTER_NAME (latest local artifact: QA)", artifact.Tester);
        Assert.Contains("Windows 11 24H2", artifact.WindowsVersion, StringComparison.Ordinal);
        Assert.Equal("REPLACE_WITH_DEVICE_MODEL (latest local artifact: HDR workstation)", artifact.Device);
        Assert.Equal(
            "REPLACE_WITH_GPU_MODEL_AND_DRIVER (current session: NVIDIA RTX 5080; latest local artifact: NVIDIA test GPU)",
            artifact.Gpu);
        Assert.Contains(
            "current session: 2 displays; active target HDR Display at 0,0 3840x2160",
            artifact.DisplaySetup,
            StringComparison.Ordinal);
        Assert.Contains("latest local artifact: HDR primary, SDR secondary", artifact.DisplaySetup, StringComparison.Ordinal);
        Assert.Equal(
            ["REPLACE_WITH_DPI_SCALE (current session: 175%; latest local artifact: 150%)"],
            artifact.DpiScales);
        Assert.Equal(
            ["REPLACE_WITH_ENTRY_POINT (for example: Main panel, Tray menu, Global hotkey; latest local artifact: Tray menu; suggested next entry point: Main panel)"],
            artifact.EntryPointsTested);
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
            OutputTarget = OutputTarget.Folder,
        };

        var projection = SettingsPanelProjection.Project(
            settings,
            CaptureSessionState.Idle(),
            source.Load().Artifacts,
            executionCapabilities: OutputProfileExecutionCapabilities.CompatibilityOnly);

        Assert.Contains(projection.Validation.ViewerMatrix, viewer =>
            viewer.Name == "Windows Photos"
            && viewer.Status == ValidationEvidenceStatus.Pass);
        Assert.Equal("Build", projection.MainPanel.OutputProfile.StatusLabel);
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
            OutputTarget = OutputTarget.Folder,
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
        Assert.Equal("harness/validation/output-validation.md", projection.Validation.Record.EvidenceDocumentPath);
        Assert.Equal("%LOCALAPPDATA%\\Lumiere\\validation\\output", projection.Validation.Record.ValidationWorkspacePath);
        Assert.Equal("Build", projection.MainPanel.OutputProfile.StatusLabel);
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
        Assert.Contains("Validation workspace:", projection.Validation.Record.WindowsManualValidationDetail);
        Assert.Contains("Release gates", projection.Validation.Record.WindowsManualValidationDetail);
        Assert.Equal("harness/validation/output-validation.md", projection.Validation.Record.EvidenceDocumentPath);
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

    private sealed class StubTargetAppVersionPrefillProvider(
        IReadOnlyDictionary<string, string> values) : ITargetAppVersionPrefillProvider
    {
        public string? TryGetVersion(string targetAppName) =>
            values.TryGetValue(targetAppName, out var value) ? value : null;
    }
}
