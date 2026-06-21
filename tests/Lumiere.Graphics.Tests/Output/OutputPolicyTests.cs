using Lumiere.Graphics.Output;
using Xunit;

namespace Lumiere.Graphics.Tests.Output;

public sealed class OutputPolicyTests
{
    [Fact]
    public void Default_AttemptsClipboardOnly()
    {
        var policy = OutputPolicy.Default;

        Assert.True(policy.ShouldAttemptClipboard);
        Assert.False(policy.ShouldAttemptFolder);
    }

    [Theory]
    [InlineData(OutputTarget.Clipboard, true, true, false)]
    [InlineData(OutputTarget.Clipboard, false, false, false)]
    [InlineData(OutputTarget.Folder, true, false, true)]
    [InlineData(OutputTarget.Folder, false, false, true)]
    [InlineData(OutputTarget.Both, true, true, true)]
    [InlineData(OutputTarget.Both, false, false, true)]
    public void FromSettings_DerivesAttemptPolicy(
        OutputTarget target,
        bool copyAsImage,
        bool shouldAttemptClipboard,
        bool shouldAttemptFolder)
    {
        var policy = OutputPolicy.FromSettings(
            target,
            copyAsImage,
            " C:\\Captures ",
            timestampNaming: true,
            afterCaptureBehavior: "RevealInFolder",
            exportColorFormat: "sRGB");

        Assert.Equal(shouldAttemptClipboard, policy.ShouldAttemptClipboard);
        Assert.Equal(shouldAttemptFolder, policy.ShouldAttemptFolder);
        Assert.Equal("C:\\Captures", policy.SavePath);
        Assert.Equal("RevealInFolder", policy.AfterCaptureBehavior);
        Assert.Equal(OutputAfterCaptureAction.Reveal, policy.AfterCaptureAction);
        Assert.Equal("sRGB", policy.RequestedProfile.Label);
        Assert.Equal("sRGB", policy.EffectiveProfile.Label);
        Assert.False(policy.UsesCompatibilityProfileFallback);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromSettings_NormalizesBlankOptionalValues(string? value)
    {
        var policy = OutputPolicy.FromSettings(
            OutputTarget.Folder,
            copyAsImage: true,
            value,
            timestampNaming: false,
            afterCaptureBehavior: value);

        Assert.Null(policy.SavePath);
        Assert.Null(policy.AfterCaptureBehavior);
        Assert.Equal(OutputAfterCaptureAction.None, policy.AfterCaptureAction);
    }

    [Theory]
    [InlineData("Open", OutputAfterCaptureAction.Open)]
    [InlineData("Reveal", OutputAfterCaptureAction.Reveal)]
    [InlineData("RevealInFolder", OutputAfterCaptureAction.Reveal)]
    [InlineData("Unsupported", OutputAfterCaptureAction.None)]
    public void FromSettings_MapsSupportedAfterCaptureActions(
        string value,
        OutputAfterCaptureAction expectedAction)
    {
        var policy = OutputPolicy.FromSettings(
            OutputTarget.Folder,
            copyAsImage: true,
            savePath: "C:\\Captures",
            timestampNaming: true,
            afterCaptureBehavior: value);

        Assert.Equal(expectedAction, policy.AfterCaptureAction);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("sRGB")]
    [InlineData("unknown")]
    public void FromSettings_UsesSrgbCompatibilityProfileForExecutableOutput(string? exportColorFormat)
    {
        var policy = OutputPolicy.FromSettings(
            OutputTarget.Clipboard,
            copyAsImage: true,
            savePath: null,
            timestampNaming: true,
            afterCaptureBehavior: null,
            exportColorFormat);

        Assert.Equal(OutputProfileKind.SrgbCompatibilityPng, policy.RequestedProfile.Kind);
        Assert.Equal(OutputProfileKind.SrgbCompatibilityPng, policy.EffectiveProfile.Kind);
        Assert.Equal(OutputFidelityMode.SdrCompatible, policy.EffectiveProfile.FidelityMode);
        Assert.True(policy.EffectiveProfile.IsExecutable);
        Assert.Contains("No HDR metadata", policy.EffectiveProfile.MetadataPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.False(policy.EffectiveProfile.AllowsHdrPreservedClaim);
    }

    [Theory]
    [InlineData("HDR10", OutputProfileKind.Hdr10Pq)]
    [InlineData("P3", OutputProfileKind.DisplayP3)]
    [InlineData("wide", OutputProfileKind.DisplayP3)]
    public void FromSettings_KeepsUnsupportedProfilesNonExecutableAndFallsBackToSrgb(
        string exportColorFormat,
        OutputProfileKind requestedKind)
    {
        var policy = OutputPolicy.FromSettings(
            OutputTarget.Clipboard,
            copyAsImage: true,
            savePath: null,
            timestampNaming: true,
            afterCaptureBehavior: null,
            exportColorFormat);

        Assert.Equal(requestedKind, policy.RequestedProfile.Kind);
        Assert.False(policy.RequestedProfile.IsExecutable);
        Assert.Equal(OutputFidelityMode.Unvalidated, policy.RequestedProfile.FidelityMode);
        Assert.False(policy.RequestedProfile.AllowsHdrPreservedClaim);
        Assert.Equal(OutputProfileKind.SrgbCompatibilityPng, policy.EffectiveProfile.Kind);
        Assert.True(policy.UsesCompatibilityProfileFallback);
        Assert.True(policy.ShouldAttemptClipboard);
    }

    [Fact]
    public void UsesCompatibilityProfileFallback_WhenRequestedProfileIsExecutableButFormatContractIsIncomplete()
    {
        var policy = OutputPolicy.Default with
        {
            RequestedProfile = OutputProfileContract.Hdr10Pq with
            {
                IsExecutable = true,
                FidelityMode = OutputFidelityMode.HdrPreserved,
            },
        };

        Assert.False(policy.RequestedProfile.HasCompleteFormatContract);
        Assert.Equal(OutputProfileKind.SrgbCompatibilityPng, policy.EffectiveProfile.Kind);
        Assert.True(policy.UsesCompatibilityProfileFallback);
    }

    [Fact]
    public void FromSettings_AppliesValidationArtifactsToRequestedProfileWithoutEnablingUnsupportedRuntimeProfile()
    {
        var policy = OutputPolicy.FromSettings(
            OutputTarget.Folder,
            copyAsImage: true,
            savePath: "C:\\Captures",
            timestampNaming: true,
            afterCaptureBehavior: null,
            exportColorFormat: "HDR10",
            validationArtifacts: [CompleteHdr10Artifact()]);

        Assert.Equal(OutputProfileKind.Hdr10Pq, policy.RequestedProfile.Kind);
        Assert.True(policy.RequestedProfile.HasCompleteFormatContract);
        Assert.False(policy.RequestedProfile.IsExecutable);
        Assert.Equal(OutputProfileKind.SrgbCompatibilityPng, policy.EffectiveProfile.Kind);
        Assert.True(policy.UsesCompatibilityProfileFallback);
    }

    [Fact]
    public void FromSettings_KeepsValidatedHdrProfileFallbackWhenRuntimeEncoderIsNotImplemented()
    {
        var policy = OutputPolicy.FromSettings(
            OutputTarget.Folder,
            copyAsImage: true,
            savePath: "C:\\Captures",
            timestampNaming: true,
            afterCaptureBehavior: null,
            exportColorFormat: "HDR10",
            validationArtifacts: [CompleteHdr10Artifact()],
            executionCapabilities: OutputProfileExecutionCapabilities.Create(
                OutputProfileExecutionCapability.SrgbCompatibility,
                OutputProfileExecutionCapability.Hdr10PreservedPendingArtifactEncoder));

        Assert.Equal(OutputProfileKind.Hdr10Pq, policy.RequestedProfile.Kind);
        Assert.True(policy.RequestedProfile.HasCompleteFormatContract);
        Assert.Equal(OutputProfileKind.SrgbCompatibilityPng, policy.EffectiveProfile.Kind);
        Assert.True(policy.UsesCompatibilityProfileFallback);
    }

    [Fact]
    public void FromSettings_UsesValidatedHdrProfileOnlyWhenRuntimeEncoderCapabilityIsImplemented()
    {
        var policy = OutputPolicy.FromSettings(
            OutputTarget.Folder,
            copyAsImage: true,
            savePath: "C:\\Captures",
            timestampNaming: true,
            afterCaptureBehavior: null,
            exportColorFormat: "HDR10",
            validationArtifacts: [CompleteHdr10Artifact()],
            executionCapabilities: OutputProfileExecutionCapabilities.Create(
                OutputProfileExecutionCapability.SrgbCompatibility,
                OutputProfileExecutionCapability.Hdr10PreservedImplementedArtifactEncoder));

        Assert.Equal(OutputProfileKind.Hdr10Pq, policy.EffectiveProfile.Kind);
        Assert.True(policy.EffectiveProfile.IsExecutable);
        Assert.Equal(OutputFidelityMode.HdrPreserved, policy.EffectiveProfile.FidelityMode);
        Assert.Equal(OutputTransferFunction.PqSt2084, policy.EffectiveProfile.FormatContract.TransferFunction);
        Assert.False(policy.UsesCompatibilityProfileFallback);
    }

    [Fact]
    public void FromHdr10JxrCodecReadiness_KeepsCompatibilityOnlyWhenCodecIsPending()
    {
        var capabilities = OutputProfileExecutionCapabilities.FromHdr10JxrCodecReadiness(
            Hdr10JxrCodecReadiness.PendingNativeWicImplementation);
        var policy = OutputPolicy.FromSettings(
            OutputTarget.Folder,
            copyAsImage: true,
            savePath: "C:\\Captures",
            timestampNaming: true,
            afterCaptureBehavior: null,
            exportColorFormat: "HDR10",
            validationArtifacts: [CompleteHdr10Artifact()],
            executionCapabilities: capabilities);

        Assert.Equal(OutputProfileKind.Hdr10Pq, policy.RequestedProfile.Kind);
        Assert.True(policy.RequestedProfile.HasCompleteFormatContract);
        Assert.Equal(OutputProfileKind.SrgbCompatibilityPng, policy.EffectiveProfile.Kind);
        Assert.True(policy.UsesCompatibilityProfileFallback);
    }

    [Fact]
    public void FromHdr10JxrCodecReadiness_EnablesHdr10OnlyWhenCodecReadinessIsComplete()
    {
        var capabilities = OutputProfileExecutionCapabilities.FromHdr10JxrCodecReadiness(ReadyHdr10JxrReadiness);
        var policy = OutputPolicy.FromSettings(
            OutputTarget.Folder,
            copyAsImage: true,
            savePath: "C:\\Captures",
            timestampNaming: true,
            afterCaptureBehavior: null,
            exportColorFormat: "HDR10",
            validationArtifacts: [CompleteHdr10Artifact()],
            executionCapabilities: capabilities);

        Assert.Equal(OutputProfileKind.Hdr10Pq, policy.EffectiveProfile.Kind);
        Assert.True(policy.EffectiveProfile.IsExecutable);
        Assert.Equal(OutputFidelityMode.HdrPreserved, policy.EffectiveProfile.FidelityMode);
        Assert.Equal(OutputArtifactEncoderImplementation.Implemented, capabilities.Profiles.Single(
            profile => profile.ProfileKind is OutputProfileKind.Hdr10Pq).ArtifactEncoderImplementation);
    }

    [Fact]
    public void FromHdr10JxrCodecReadiness_KeepsCompatibilityOnlyWhenMetadataPolicyIsNotAuditable()
    {
        var readiness = ReadyHdr10JxrReadiness with
        {
            Hdr10StaticMetadataPolicy = Hdr10StaticMetadataPolicy.Undefined,
        };

        var capabilities = OutputProfileExecutionCapabilities.FromHdr10JxrCodecReadiness(readiness);

        Assert.DoesNotContain(capabilities.Profiles, profile => profile.ProfileKind is OutputProfileKind.Hdr10Pq);
    }

    [Fact]
    public void FromHdr10JxrCodecReadiness_KeepsCompatibilityOnlyWhenOnlyAuditMetadataRoundTripIsProven()
    {
        var readiness = ReadyHdr10JxrReadiness with
        {
            HasViewerRecognizedHdr10StaticMetadata = false,
            HasWindowsManualViewerValidation = false,
            Blockers =
            [
                "Viewer-recognized HDR10 static metadata is not implemented or validated for the JPEG XR container.",
                "Windows manual viewer validation for the emitted JXR artifact has not passed.",
            ],
        };

        var capabilities = OutputProfileExecutionCapabilities.FromHdr10JxrCodecReadiness(readiness);

        Assert.DoesNotContain(capabilities.Profiles, profile => profile.ProfileKind is OutputProfileKind.Hdr10Pq);
    }

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
            EvidencePaths: ["docs/validation/evidence/hdr10-output.md"],
            KnownLimitations: [],
            FollowUpIssuesOrStories: [],
            OutputProfileRecords:
            [
                new(
                    OutputProfileKind.Hdr10Pq,
                    [
                        PassingHdrViewer("Microsoft Paint"),
                        PassingHdrViewer("Windows Photos"),
                        PassingHdrViewer("Chromium browsers"),
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
}
