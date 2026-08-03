using Lumiere.App;
using Lumiere.Capture;
using Lumiere.Graphics.Hdr;
using Lumiere.Graphics.Output;
using Windows.Graphics;
using Xunit;

namespace Lumiere.Graphics.Tests.App;

public sealed class OutputResultProjectionTests
{
    [Fact]
    public void Project_WithoutOutputShowsReadyState()
    {
        var fidelity = HdrAwareOutputProjection.ProjectOutputProfile("HDR10").FidelityClaim;

        var projection = OutputResultProjection.Project(null, fidelity);

        Assert.Equal("Ready", projection.Title);
        Assert.Equal("No capture output has completed yet.", projection.Detail);
        Assert.Equal(OutputResultProjectionSeverity.Neutral, projection.Severity);
        Assert.Contains("no completed output yet", projection.FidelityDetail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Project_ClipboardSuccessSeparatesArtifactSuccessFromFidelityClaim()
    {
        var output = OutputResult.ClipboardSuccess(1024)
            .WithRequestedProfile(OutputProfileContract.FromSettingsValue("HDR10"));
        var fidelity = HdrAwareOutputProjection.ProjectOutputProfile("HDR10").FidelityClaim;

        var projection = OutputResultProjection.Project(output, fidelity);

        Assert.Equal("Copied", projection.Title);
        Assert.Equal("Clipboard copied as sRGB Visual Match", projection.Detail);
        Assert.Equal(OutputResultProjectionSeverity.Success, projection.Severity);
        Assert.Contains("Requested HDR10", projection.FidelityDetail, StringComparison.Ordinal);
        Assert.Contains("using sRGB Visual Match output", projection.FidelityDetail, StringComparison.Ordinal);
        Assert.Contains("no completed output yet", projection.FidelityDetail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR-preserved", projection.Title, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR-preserved", projection.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR-preserved", projection.FidelityDetail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Project_UsesOutputResultEffectiveProfileWhenFidelityClaimIsNotProvided()
    {
        var output = OutputResult.ClipboardSuccess(1024)
            .WithRequestedProfile(OutputProfileContract.FromSettingsValue("P3"));

        var projection = OutputResultProjection.Project(output);

        Assert.Equal("Copied", projection.Title);
        Assert.Contains("Requested P3", projection.FidelityDetail, StringComparison.Ordinal);
        Assert.Contains("using sRGB Visual Match output", projection.FidelityDetail, StringComparison.Ordinal);
        Assert.Contains("compatibility output", projection.FidelityDetail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR-preserved", projection.FidelityDetail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Project_FidelityDetailOmitsViewerEvidenceGapFromNormalCopy()
    {
        var output = OutputResult.ClipboardSuccess(1024)
            .WithRequestedProfile(OutputProfileContract.FromSettingsValue("sRGB"));

        var projection = OutputResultProjection.Project(output);

        Assert.DoesNotContain("viewer evidence", projection.FidelityDetail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Microsoft Paint", projection.FidelityDetail, StringComparison.Ordinal);
        Assert.DoesNotContain("Windows Photos", projection.FidelityDetail, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft Edge", projection.FidelityDetail, StringComparison.Ordinal);
        Assert.DoesNotContain("HDR-preserved", projection.FidelityDetail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Project_FidelityDetailIncludesEffectiveTypedFormatContract()
    {
        var output = OutputResult.ClipboardSuccess(1024)
            .WithRequestedProfile(OutputProfileContract.FromSettingsValue("HDR10"));

        var projection = OutputResultProjection.Project(output);

        Assert.Contains("Requested HDR10", projection.FidelityDetail, StringComparison.Ordinal);
        Assert.Contains("using sRGB Visual Match output", projection.FidelityDetail, StringComparison.Ordinal);
        Assert.Contains("RGBA8 sRGB", projection.FidelityDetail, StringComparison.Ordinal);
        Assert.Contains("Transfer: sRGB", projection.FidelityDetail, StringComparison.Ordinal);
        Assert.Contains("Primaries: BT.709", projection.FidelityDetail, StringComparison.Ordinal);
        Assert.Contains("No HDR metadata", projection.FidelityDetail, StringComparison.Ordinal);
    }

    [Fact]
    public void Project_WithSelectedProfilePreservesRequestedGateWhenRuntimeFallsBack()
    {
        OutputValidationSessionArtifact[] artifacts =
        [
            ArtifactFor("Microsoft Paint"),
            ArtifactFor("Windows Photos"),
            ArtifactFor("Microsoft Edge"),
        ];
        var selectedProfile = HdrAwareOutputProjection.ProjectOutputProfile(
            OutputProfileContract.FromSettingsValue("HDR10"),
            artifacts,
            readiness: null,
            executionCapabilities: ValidateOnlyHdr10Capabilities(artifacts));
        var output = OutputResult.ClipboardSuccess(1024)
            .WithRequestedProfile(OutputProfileContract.FromSettingsValue("HDR10"));

        var projection = OutputResultProjection.Project(output, selectedProfile);

        Assert.Contains("Requested HDR10", projection.FidelityDetail, StringComparison.Ordinal);
        Assert.DoesNotContain("Validate", projection.FidelityDetail, StringComparison.Ordinal);
        Assert.DoesNotContain("Windows manual viewer evidence", projection.FidelityDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("using sRGB Visual Match output", projection.FidelityDetail, StringComparison.Ordinal);
        Assert.Contains("compatibility output", projection.FidelityDetail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Project_FolderSuccessShowsSavedArtifact()
    {
        var output = OutputResult.FromTargets(
            OutputTargetResult.Success(OutputTarget.Folder, "Saved", artifactPath: "C:\\Captures\\a.png"));
        var fidelity = HdrAwareOutputProjection.ProjectOutputProfile("sRGB").FidelityClaim;

        var projection = OutputResultProjection.Project(output, fidelity);

        Assert.Equal("Saved", projection.Title);
        Assert.Equal("File saved as sRGB Visual Match", projection.Detail);
        Assert.Equal(OutputResultProjectionSeverity.Success, projection.Severity);
    }

    [Fact]
    public void Project_PartialSuccessShowsWarningAndBothTargets()
    {
        var output = OutputResult.FromTargets(
            OutputTargetResult.Success(OutputTarget.Clipboard, "Copied"),
            OutputTargetResult.Failed(OutputTarget.Folder, "Folder unavailable"));
        var fidelity = HdrAwareOutputProjection.ProjectOutputProfile("sRGB").FidelityClaim;

        var projection = OutputResultProjection.Project(output, fidelity);

        Assert.Equal("Output partially complete", projection.Title);
        Assert.Equal(OutputResultProjectionSeverity.Warning, projection.Severity);
        Assert.Equal("Clipboard copied as sRGB Visual Match | Folder unavailable", projection.Detail);
    }

    [Fact]
    public void Project_BothTargetMixedProfilesCallsOutPerTargetFidelity()
    {
        var requested = OutputProfileContract.Hdr10Pq with
        {
            FormatContract = CompleteHdr10Contract,
        };
        var output = OutputResult.FromTargets(
                OutputTargetResult.Success(OutputTarget.Clipboard, "Copied"),
                OutputTargetResult.Success(OutputTarget.Folder, "Saved", artifactPath: "C:\\Captures\\frame.jxr"))
            .WithOutputPolicy(OutputPolicy.FromSettings(
                OutputTarget.Both,
                copyAsImage: true,
                savePath: "C:\\Captures",
                timestampNaming: true,
                afterCaptureBehavior: null,
                exportColorFormat: "HDR10",
                validationArtifacts: [CompleteHdr10Artifact()],
                executionCapabilities: OutputProfileExecutionCapabilities.Create(
                    OutputProfileExecutionCapability.SrgbCompatibility,
                    OutputProfileExecutionCapability.Hdr10PreservedImplementedArtifactEncoder)) with
            {
                RequestedProfile = requested,
            });

        var projection = OutputResultProjection.Project(output);

        Assert.Contains("Output modes:", projection.FidelityDetail, StringComparison.Ordinal);
        Assert.Contains("Clipboard sRGB", projection.FidelityDetail, StringComparison.Ordinal);
        Assert.Contains("Folder HDR10", projection.FidelityDetail, StringComparison.Ordinal);
        Assert.Contains("Formats:", projection.FidelityDetail, StringComparison.Ordinal);
        Assert.DoesNotContain("viewer evidence", projection.FidelityDetail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Project_FidelityDetailIncludesCapturedDisplayContextWhenProvided()
    {
        var output = OutputResult.ClipboardSuccess(1024)
            .WithRequestedProfile(OutputProfileContract.FromSettingsValue("HDR10"));
        var target = CaptureTarget.CreateForTest(
            new SizeInt32
            {
                Width = 3840,
                Height = 2160,
            },
            "HDR Display",
            CaptureTargetKind.Display,
            new DisplayOutputIdentity("\\\\.\\DISPLAY2", left: 3840, top: 0, width: 3840, height: 2160));

        var projection = OutputResultProjection.Project(output, target);

        Assert.Contains("Captured display: HDR Display", projection.FidelityDetail, StringComparison.Ordinal);
        Assert.Contains("\\\\.\\DISPLAY2", projection.FidelityDetail, StringComparison.Ordinal);
        Assert.Contains("desktop bounds 3840,0 3840x2160", projection.FidelityDetail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Project_FailedOutputUsesWarningSeverity()
    {
        var output = OutputResult.ClipboardFailed("Clipboard write denied.");
        var fidelity = HdrAwareOutputProjection.ProjectOutputProfile("sRGB").FidelityClaim;

        var projection = OutputResultProjection.Project(output, fidelity);

        Assert.Equal("Failed to copy to clipboard", projection.Title);
        Assert.Equal(OutputResultProjectionSeverity.Warning, projection.Severity);
        Assert.Equal("Failed to copy to clipboard", projection.Detail);
    }

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

    private static OutputValidationSessionArtifact ArtifactFor(string viewerName) =>
        new(
            Date: "2026-06-21",
            Tester: "QA",
            BuildCommit: "485bc31",
            WindowsVersion: "Windows 11 24H2",
            Device: "HDR workstation",
            Gpu: "Test GPU",
            DisplaySetup: "HDR primary",
            HdrState: "HDR enabled",
            DpiScales: ["150%"],
            EntryPointsTested: ["Main panel"],
            OutputTargetsTested: ["Folder"],
            TargetAppsTested: [viewerName],
            ChecklistIdsCovered: ["REL-OUT-01"],
            ResultSummary: $"{viewerName} HDR validation passed.",
            EvidencePaths: [$"knowledge/evidence/{viewerName}.md"],
            KnownLimitations: [],
            FollowUpIssuesOrStories: [],
            OutputProfileRecords:
            [
                new(
                    OutputProfileKind.Hdr10Pq,
                    [
                        PassingHdrViewer(viewerName),
                    ]),
            ]);

    private static OutputValidationSessionArtifact CompleteHdr10Artifact() =>
        new(
            Date: "2026-06-21",
            Tester: "QA",
            BuildCommit: "485bc31",
            WindowsVersion: "Windows 11 24H2",
            Device: "HDR workstation",
            Gpu: "Test GPU",
            DisplaySetup: "HDR primary",
            HdrState: "HDR enabled",
            DpiScales: ["150%"],
            EntryPointsTested: ["Main panel"],
            OutputTargetsTested: ["Folder"],
            TargetAppsTested: ["Windows Photos"],
            ChecklistIdsCovered: ["REL-OUT-04"],
            ResultSummary: "HDR10 output profile validation passed.",
            EvidencePaths: ["knowledge/evidence/hdr10-output.md"],
            KnownLimitations: [],
            FollowUpIssuesOrStories: [],
            OutputProfileRecords:
            [
                new(
                    OutputProfileKind.Hdr10Pq,
                    [
                        PassingHdrViewer("Microsoft Paint"),
                        PassingHdrViewer("Windows Photos"),
                        PassingHdrViewer("Microsoft Edge"),
                    ])
                {
                    FormatContract = CompleteHdr10Contract,
                },
            ]);

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
}
