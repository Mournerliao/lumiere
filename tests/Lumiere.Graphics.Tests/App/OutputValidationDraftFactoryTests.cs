using Lumiere.App;
using Lumiere.Capture;
using Lumiere.Graphics.Hdr;
using Lumiere.Graphics.Output;
using Windows.Graphics;
using Xunit;

namespace Lumiere.Graphics.Tests.App;

public sealed class OutputValidationDraftFactoryTests
{
    [Fact]
    public void Create_PrefillsCurrentSessionContextForHdr10FolderValidation()
    {
        var session = CaptureSessionState.Capturing(
            CaptureTarget.CreateForTest(
                new SizeInt32
                {
                    Width = 3840,
                    Height = 2160,
                },
                "HDR Display",
                CaptureTargetKind.Display,
                new DisplayOutputIdentity("\\\\.\\DISPLAY1", left: 0, top: 0, width: 3840, height: 2160)),
            PreviewReadinessStatus.Ready(
                "HDR preview path is validated.",
                "IDXGISwapChain3.CheckColorSpaceSupport returned Present; IDXGISwapChain3.SetColorSpace1 set RgbFullG2084NoneP2020; display match=DesktopBounds."));
        var request = new OutputValidationDraftRequest(
            "0.1.0",
            OutputTarget.Folder,
            OutputProfileContract.Hdr10Pq,
            session);

        var document = OutputValidationDraftFactory.Create(
            request,
            new DateTimeOffset(2026, 06, 22, 10, 30, 00, TimeSpan.FromHours(8)));

        Assert.Equal("output-validation-draft-2026-06-22-hdr10-folder", document.FileNameStem);
        Assert.Equal("2026-06-22", document.Artifact.Date);
        Assert.Equal(["Folder"], document.Artifact.OutputTargetsTested);
        Assert.Contains("REL-OUT-04", document.Artifact.ChecklistIdsCovered);
        Assert.Contains("REL-HDR-04", document.Artifact.ChecklistIdsCovered);
        Assert.Contains("Microsoft Paint", document.Artifact.TargetAppsTested);
        Assert.Contains("Windows Photos", document.Artifact.TargetAppsTested);
        Assert.Contains("Microsoft Edge", document.Artifact.TargetAppsTested);
        Assert.Contains(
            document.Artifact.TargetAppVersions,
            version => version.Name == "Windows Photos"
                && version.Version == "REPLACE_WITH_WINDOWS_PHOTOS_VERSION");
        Assert.Contains("REPLACE_WITH_GIT_COMMIT", document.Artifact.BuildCommit, StringComparison.Ordinal);
        Assert.Contains("Lumiere v0.1.0", document.Artifact.BuildCommit, StringComparison.Ordinal);
        Assert.Equal("HDR Display", document.Artifact.TargetHdrEvidence!.TargetDisplayName);
        Assert.Equal(0, document.Artifact.TargetHdrEvidence.Left);
        Assert.Equal(0, document.Artifact.TargetHdrEvidence.Top);
        Assert.Equal(3840, document.Artifact.TargetHdrEvidence.Width);
        Assert.Equal(2160, document.Artifact.TargetHdrEvidence.Height);
        Assert.Equal("DesktopBounds", document.Artifact.TargetHdrEvidence.MatchKind);
        Assert.Equal("RgbFullG2084NoneP2020", document.Artifact.TargetHdrEvidence.ColorSpace);
        Assert.Contains("REPLACE_WITH_OBSERVED_TARGET_HDR_STATE", document.Artifact.TargetHdrEvidence.HdrState, StringComparison.Ordinal);

        var profileRecord = Assert.Single(document.Artifact.OutputProfileRecords);
        Assert.Equal(OutputProfileKind.Hdr10Pq, profileRecord.ProfileKind);
        Assert.Equal(["Folder"], profileRecord.OutputTargetsCovered);
        Assert.NotNull(profileRecord.FormatContract);
        Assert.Equal(OutputMetadataPolicy.AttachHdr10StaticMetadata, profileRecord.FormatContract!.MetadataPolicy);
        Assert.Equal(OutputTransferFunction.PqSt2084, profileRecord.FormatContract.TransferFunction);
        Assert.All(
            profileRecord.ViewerEvidence,
            viewer =>
            {
                Assert.Equal(OutputCompatibilityEvidenceStatus.NotRun, viewer.ArtifactHandlingStatus);
                Assert.Equal(OutputCompatibilityEvidenceStatus.NotRun, viewer.VisualMatchStatus);
                Assert.Equal(OutputCompatibilityEvidenceStatus.NotRun, viewer.HdrPreservationStatus);
                Assert.Equal(OutputCompatibilityEvidenceStatus.NotRun, viewer.Hdr10MetadataStatus);
            });
    }

    [Fact]
    public void Create_KeepsClipboardSrgbDraftScopedToCompatibilityOutput()
    {
        var request = new OutputValidationDraftRequest(
            "v0.1.0",
            OutputTarget.Clipboard,
            OutputProfileContract.SrgbCompatibilityPng,
            CaptureSessionState.Idle());

        var document = OutputValidationDraftFactory.Create(
            request,
            new DateTimeOffset(2026, 06, 22, 10, 30, 00, TimeSpan.FromHours(8)));

        Assert.Equal(["Clipboard"], document.Artifact.OutputTargetsTested);
        Assert.Contains("REL-OUT-01", document.Artifact.ChecklistIdsCovered);
        Assert.DoesNotContain("REL-HDR-04", document.Artifact.ChecklistIdsCovered);

        var profileRecord = Assert.Single(document.Artifact.OutputProfileRecords);
        Assert.Equal(OutputProfileKind.SrgbCompatibilityPng, profileRecord.ProfileKind);
        Assert.Equal(OutputMetadataPolicy.NoHdrMetadata, profileRecord.FormatContract!.MetadataPolicy);
        Assert.All(
            profileRecord.ViewerEvidence,
            viewer =>
            {
                Assert.Equal(OutputCompatibilityEvidenceStatus.NotRun, viewer.ArtifactHandlingStatus);
                Assert.Equal(OutputCompatibilityEvidenceStatus.NotRun, viewer.VisualMatchStatus);
                Assert.Equal(OutputCompatibilityEvidenceStatus.NotApplicable, viewer.HdrPreservationStatus);
                Assert.Equal(OutputCompatibilityEvidenceStatus.NotApplicable, viewer.Hdr10MetadataStatus);
            });
    }

    [Fact]
    public void Create_PrefillsComparableBuildCommitWhenInformationalVersionCarriesIt()
    {
        var request = new OutputValidationDraftRequest(
            "2.3.4+72c3be7",
            OutputTarget.Folder,
            OutputProfileContract.Hdr10Pq,
            CaptureSessionState.Idle());

        var document = OutputValidationDraftFactory.Create(
            request,
            new DateTimeOffset(2026, 06, 22, 10, 30, 00, TimeSpan.FromHours(8)));

        Assert.StartsWith("72c3be7", document.Artifact.BuildCommit, StringComparison.Ordinal);
        Assert.Contains("Lumiere v2.3.4+72c3be7", document.Artifact.BuildCommit, StringComparison.Ordinal);
        Assert.DoesNotContain("REPLACE_WITH_GIT_COMMIT", document.Artifact.BuildCommit, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_UsesResolvedTargetAppVersionsWhenProviderCanIdentifyKnownApps()
    {
        var request = new OutputValidationDraftRequest(
            "0.1.0",
            OutputTarget.Folder,
            OutputProfileContract.Hdr10Pq,
            CaptureSessionState.Idle());

        var document = OutputValidationDraftFactory.Create(
            request,
            new DateTimeOffset(2026, 06, 22, 10, 30, 00, TimeSpan.FromHours(8)),
            new StubTargetAppVersionPrefillProvider(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Microsoft Paint"] = "11.2504.451.0",
                    ["Windows Photos"] = "2026.11040.12001.0",
                    ["Microsoft Edge"] = "138.0.7204.101",
                }));

        Assert.Contains(
            document.Artifact.TargetAppVersions,
            version => version.Name == "Microsoft Paint"
                && version.Version == "11.2504.451.0");
        Assert.Contains(
            document.Artifact.TargetAppVersions,
            version => version.Name == "Windows Photos"
                && version.Version == "2026.11040.12001.0");
        Assert.Contains(
            document.Artifact.TargetAppVersions,
            version => version.Name == "Microsoft Edge"
                && version.Version == "138.0.7204.101");
    }

    [Fact]
    public void Create_CarriesLatestLocalArtifactHintsWhileKeepingManualPlaceholdersExplicit()
    {
        var request = new OutputValidationDraftRequest(
            "0.1.0",
            OutputTarget.Folder,
            OutputProfileContract.Hdr10Pq,
            CaptureSessionState.Capturing(
                CaptureTarget.CreateForTest(
                    new SizeInt32
                    {
                        Width = 3840,
                        Height = 2160,
                    },
                    "HDR Display",
                    CaptureTargetKind.Display),
                PreviewReadinessStatus.Ready("HDR-ready", "Target-aware readiness passed.")),
            new OutputValidationCurrentSessionHint(
                "NVIDIA RTX 5080",
                ["175%"],
                "2 displays; active target HDR Display at 0,0 3840x2160"));

        var document = OutputValidationDraftFactory.Create(
            request,
            new DateTimeOffset(2026, 06, 22, 10, 30, 00, TimeSpan.FromHours(8)),
            seed: new OutputValidationDraftSeed(
                Tester: "QA",
                WindowsVersion: "Windows 11 24H2",
                Device: "HDR workstation",
                Gpu: "NVIDIA RTX test driver",
                DisplaySetup: "HDR primary, SDR secondary",
                DpiScales: ["150%"],
                EntryPointsTested: ["Main panel", "Tray menu"]));

        Assert.Equal("REPLACE_WITH_TESTER_NAME (latest local artifact: QA)", document.Artifact.Tester);
        Assert.Contains("current session:", document.Artifact.WindowsVersion, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Windows 11 24H2", document.Artifact.WindowsVersion, StringComparison.Ordinal);
        Assert.Equal("REPLACE_WITH_DEVICE_MODEL (latest local artifact: HDR workstation)", document.Artifact.Device);
        Assert.Equal(
            "REPLACE_WITH_GPU_MODEL_AND_DRIVER (current session: NVIDIA RTX 5080; latest local artifact: NVIDIA RTX test driver)",
            document.Artifact.Gpu);
        Assert.Contains("active target: HDR Display", document.Artifact.DisplaySetup, StringComparison.Ordinal);
        Assert.Contains(
            "current session: 2 displays; active target HDR Display at 0,0 3840x2160",
            document.Artifact.DisplaySetup,
            StringComparison.Ordinal);
        Assert.Contains("latest local artifact: HDR primary, SDR secondary", document.Artifact.DisplaySetup, StringComparison.Ordinal);
        Assert.Equal(
            ["REPLACE_WITH_DPI_SCALE (current session: 175%; latest local artifact: 150%)"],
            document.Artifact.DpiScales);
        Assert.Equal(
            ["REPLACE_WITH_ENTRY_POINT (for example: Main panel, Tray menu, Global hotkey; latest local artifact: Main panel, Tray menu)"],
            document.Artifact.EntryPointsTested);
    }

    private sealed class StubTargetAppVersionPrefillProvider(
        IReadOnlyDictionary<string, string> values) : ITargetAppVersionPrefillProvider
    {
        public string? TryGetVersion(string targetAppName) =>
            values.TryGetValue(targetAppName, out var value) ? value : null;
    }
}
