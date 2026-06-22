using Lumiere.App;
using Lumiere.Graphics.Hdr;
using Lumiere.Graphics.Output;
using Xunit;

namespace Lumiere.Graphics.Tests.App;

public sealed class PerfectHdrFidelityProjectionTests
{
    [Theory]
    [InlineData(null, "sRGB")]
    [InlineData("", "sRGB")]
    [InlineData("  ", "sRGB")]
    [InlineData("srgb", "sRGB")]
    [InlineData("HDR10", "HDR10")]
    [InlineData("hdr10", "HDR10")]
    [InlineData("P3", "P3")]
    [InlineData("wide", "P3")]
    public void NormalizeExportColorFormat_MapsKnownProfilesAndFallsBackToSrgb(string? input, string expected)
    {
        var normalized = PerfectHdrFidelityProjection.NormalizeExportColorFormat(input);

        Assert.Equal(expected, normalized);
    }

    [Fact]
    public void ProjectOutputProfile_Hdr10IsValidationScopedAndUnvalidated()
    {
        var profile = PerfectHdrFidelityProjection.ProjectOutputProfile("HDR10");

        Assert.Equal("HDR10", profile.Label);
        Assert.Equal("Validate", profile.StatusLabel);
        Assert.True(profile.IsReadOnly);
        Assert.Equal(FidelityClaimKind.Unvalidated, profile.FidelityClaim.Kind);
        Assert.Equal("Unvalidated", profile.FidelityClaim.Label);
        Assert.Contains("No fidelity claim", profile.FidelityClaim.Detail);
        Assert.Equal("FP16/scRGB capture source", profile.Contract.SourcePolicy);
        Assert.Equal("HDR10 output contract pending implementation", profile.Contract.DestinationPolicy);
        Assert.Contains("tone", profile.Contract.ConversionPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("metadata", profile.Contract.MetadataPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("target-app", profile.Contract.ViewerCompatibilityPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR-preserved", profile.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectOutputProfile_SrgbIsCompatibilityConvertedFallback()
    {
        var profile = PerfectHdrFidelityProjection.ProjectOutputProfile("sRGB");

        Assert.Equal("sRGB", profile.Label);
        Assert.Equal("Compat", profile.StatusLabel);
        Assert.False(profile.IsReadOnly);
        Assert.Equal(FidelityClaimKind.Converted, profile.FidelityClaim.Kind);
        Assert.Equal("Converted", profile.FidelityClaim.Label);
        Assert.Contains("compatibility", profile.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Compatibility-converted sRGB artifact", profile.Contract.DestinationPolicy);
        Assert.Contains("no HDR metadata", profile.Contract.MetadataPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("public release target", profile.FidelityClaim.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectOutputProfile_SurfacesTypedFormatContractLabels()
    {
        var profile = PerfectHdrFidelityProjection.ProjectOutputProfile("sRGB");

        Assert.Equal("R16G16B16A16 float", profile.Contract.SourcePixelFormatLabel);
        Assert.Equal("RGBA8 sRGB", profile.Contract.DestinationPixelFormatLabel);
        Assert.Equal("sRGB", profile.Contract.TransferFunctionLabel);
        Assert.Equal("BT.709", profile.Contract.ColorPrimariesLabel);
        Assert.Equal("SDR tone-mapped", profile.Contract.ConversionPolicyLabel);
        Assert.Equal("No HDR metadata", profile.Contract.MetadataPolicyLabel);
        Assert.Equal("Compatibility-first target apps", profile.Contract.TargetAppAssumptionLabel);
    }

    [Fact]
    public void ProjectOutputProfile_UsesOutputContractAsSourceOfTruth()
    {
        var contract = OutputProfileContract.FromSettingsValue("HDR10");

        var profile = PerfectHdrFidelityProjection.ProjectOutputProfile(contract);

        Assert.Equal(contract.Label, profile.Label);
        Assert.Equal(contract.SourceFormatPolicy, profile.Contract.SourcePolicy);
        Assert.Equal(contract.DestinationFormatPolicy, profile.Contract.DestinationPolicy);
        Assert.Equal(contract.ConversionPolicy, profile.Contract.ConversionPolicy);
        Assert.Equal(contract.MetadataPolicy, profile.Contract.MetadataPolicy);
        Assert.Equal(contract.ViewerCompatibilityPolicy, profile.Contract.ViewerCompatibilityPolicy);
        Assert.Equal(FidelityClaimKind.Unvalidated, profile.FidelityClaim.Kind);
    }

    [Fact]
    public void ProjectOutputProfile_CompleteHdr10ContractUsesValidateWordingBeforeManualViewerEvidencePasses()
    {
        var artifacts =
        new[]
        {
            ArtifactWithIncompleteViewerEvidence("Microsoft Paint"),
            ArtifactWithIncompleteViewerEvidence("Windows Photos"),
            ArtifactWithIncompleteViewerEvidence("Microsoft Edge"),
        };

        var profile = PerfectHdrFidelityProjection.ProjectOutputProfile(
            OutputProfileContract.Hdr10Pq,
            artifacts,
            readiness: null,
            ValidateOnlyHdr10Capabilities(artifacts));

        Assert.Equal("Validate", profile.StatusLabel);
        Assert.Equal("HDR10 output contract is defined, but this session is still waiting for Windows manual viewer evidence.", profile.Contract.DestinationPolicy);
        Assert.Contains("defined for the HDR10 path", profile.Contract.ConversionPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("viewer evidence is still incomplete", profile.Contract.MetadataPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Windows manual viewer evidence", profile.Contract.ViewerCompatibilityPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pending implementation", profile.Contract.DestinationPolicy, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectOutputProfile_DoesNotClaimHdrPreservedWithoutViewerEvidence()
    {
        var contract = OutputProfileContract.Hdr10Pq with
        {
            IsExecutable = true,
            FidelityMode = OutputFidelityMode.HdrPreserved,
            FormatContract = CompleteHdr10Contract,
        };

        var profile = PerfectHdrFidelityProjection.ProjectOutputProfile(contract);

        Assert.Equal(FidelityClaimKind.Unvalidated, profile.FidelityClaim.Kind);
        Assert.Contains("blocked", profile.FidelityClaim.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("validated HDR-preserved", profile.FidelityClaim.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectOutputProfile_ClaimsHdrPreservedOnlyWhenEvidenceGatePasses()
    {
        var contract = OutputProfileContract.Hdr10Pq with
        {
            IsExecutable = true,
            FidelityMode = OutputFidelityMode.HdrPreserved,
            FormatContract = CompleteHdr10Contract,
            ViewerEvidence =
            [
                PassingHdrViewer("Microsoft Paint"),
                PassingHdrViewer("Windows Photos"),
                PassingHdrViewer("Microsoft Edge"),
            ],
        };

        var profile = PerfectHdrFidelityProjection.ProjectOutputProfile(contract);

        Assert.Equal(FidelityClaimKind.HdrPreserved, profile.FidelityClaim.Kind);
        Assert.Contains("validated HDR-preserved", profile.FidelityClaim.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectOutputProfile_BlocksHdrPreservedClaimWhenTargetAwareReadinessIsUnresolved()
    {
        var contract = OutputProfileContract.Hdr10Pq with
        {
            IsExecutable = true,
            FidelityMode = OutputFidelityMode.HdrPreserved,
            FormatContract = CompleteHdr10Contract,
            ViewerEvidence =
            [
                PassingHdrViewer("Microsoft Paint"),
                PassingHdrViewer("Windows Photos"),
                PassingHdrViewer("Microsoft Edge"),
            ],
        };
        var readiness = PreviewReadinessStatus.Degraded(
            PreviewReadinessStage.Presentation,
            "HDR readiness is unvalidated for the selected capture target.",
            "Target-aware display capability could not be matched to a DXGI output.",
            PreviewReadinessReason.TargetDisplayUnresolved);

        var profile = PerfectHdrFidelityProjection.ProjectOutputProfile(contract, readiness);

        Assert.Equal(FidelityClaimKind.Unvalidated, profile.FidelityClaim.Kind);
        Assert.Equal("Unvalidated", profile.FidelityClaim.Label);
        Assert.Contains("target-aware HDR readiness", profile.FidelityClaim.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("validated HDR-preserved", profile.FidelityClaim.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectValidation_RequiresPublicHdrFidelityEvidenceBeforeRelease()
    {
        var validation = PerfectHdrFidelityProjection.ProjectValidation();

        Assert.Equal(PerfectHdrFidelityProjection.ReleaseTarget, validation.ReleaseTarget);
        Assert.Contains("evidence", validation.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("sRGB", validation.OutputProfileGate.ProfileLabel);
        Assert.Equal("Compat", validation.OutputProfileGate.StatusLabel);
        Assert.Equal(ValidationEvidenceStatus.Limited, validation.OutputProfileGate.Status);
        Assert.Contains(validation.Rows, row => row.Label == "Target-aware HDR" && row.Status == ValidationEvidenceStatus.NotRun);
        Assert.Contains(validation.Rows, row => row.Label == "Visual-match output" && row.Status == ValidationEvidenceStatus.NotRun);
        Assert.Contains(validation.Rows, row => row.Label == "HDR-preserved profile" && row.Status == ValidationEvidenceStatus.NotRun);
        Assert.Contains(validation.Rows, row => row.Label == "Target app matrix" && row.Status == ValidationEvidenceStatus.NotRun);
        Assert.Contains(validation.Rows, row => row.Label == "Target app versions" && row.Status == ValidationEvidenceStatus.NotRun);
    }

    [Fact]
    public void ProjectValidation_IncludesNamedViewerCompatibilityMatrix()
    {
        var validation = PerfectHdrFidelityProjection.ProjectValidation();

        Assert.Contains("Named viewers", validation.ViewerMatrixSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(validation.ViewerMatrix, viewer => viewer.Name == "Microsoft Paint" && viewer.Status == ValidationEvidenceStatus.NotRun);
        Assert.Contains(validation.ViewerMatrix, viewer => viewer.Name == "Windows Photos" && viewer.Status == ValidationEvidenceStatus.NotRun);
        Assert.Contains(validation.ViewerMatrix, viewer => viewer.Name == "Microsoft Edge" && viewer.Status == ValidationEvidenceStatus.NotRun);
        Assert.All(
            validation.ViewerMatrix,
            viewer =>
            {
                Assert.Contains("artifact", viewer.Detail, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("fidelity", viewer.Detail, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("HDR-preserved", viewer.Detail, StringComparison.OrdinalIgnoreCase);
            });
    }

    [Fact]
    public void ProjectValidation_ViewerMatrixSeparatesArtifactVisualAndHdrEvidence()
    {
        var validation = PerfectHdrFidelityProjection.ProjectValidation(OutputProfileContract.SrgbCompatibilityPng);

        Assert.All(
            validation.ViewerMatrix,
            viewer =>
            {
                Assert.Equal(ValidationEvidenceStatus.NotRun, viewer.ArtifactHandlingStatus);
                Assert.Equal(ValidationEvidenceStatus.NotRun, viewer.VisualMatchStatus);
                Assert.Equal(ValidationEvidenceStatus.NotApplicable, viewer.HdrPreservationStatus);
                Assert.Equal(ValidationEvidenceStatus.NotApplicable, viewer.Hdr10MetadataStatus);
                Assert.Contains("Artifact", viewer.Detail, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("visual", viewer.Detail, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("HDR preservation: N/A", viewer.Detail, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("HDR10 metadata: N/A", viewer.Detail, StringComparison.OrdinalIgnoreCase);
            });
    }

    [Fact]
    public void ProjectValidation_ReflectsAppliedViewerValidationRecord()
    {
        var contract = (OutputProfileContract.Hdr10Pq with
        {
            IsExecutable = true,
            FidelityMode = OutputFidelityMode.HdrPreserved,
            FormatContract = CompleteHdr10Contract,
        }).ApplyValidationRecord(new OutputProfileValidationRecord(
            OutputProfileKind.Hdr10Pq,
            [
                PassingHdrViewer("Windows Photos"),
            ]));

        var validation = PerfectHdrFidelityProjection.ProjectValidation(contract);

        Assert.Contains(validation.ViewerMatrix, viewer =>
            viewer.Name == "Windows Photos"
            && viewer.ArtifactHandlingStatus == ValidationEvidenceStatus.Pass
            && viewer.VisualMatchStatus == ValidationEvidenceStatus.Pass
            && viewer.HdrPreservationStatus == ValidationEvidenceStatus.Pass
            && viewer.Hdr10MetadataStatus == ValidationEvidenceStatus.Pass
            && viewer.Status == ValidationEvidenceStatus.Pass);
        Assert.Contains(validation.ViewerMatrix, viewer =>
            viewer.Name == "Microsoft Paint"
            && viewer.Status == ValidationEvidenceStatus.NotRun);
        Assert.Equal(FidelityClaimKind.Unvalidated, PerfectHdrFidelityProjection.ProjectOutputProfile(contract).FidelityClaim.Kind);
    }

    [Fact]
    public void ProjectValidation_AppliesValidationSessionArtifactToViewerMatrix()
    {
        var contract = OutputProfileContract.Hdr10Pq with
        {
            IsExecutable = true,
            FidelityMode = OutputFidelityMode.HdrPreserved,
            FormatContract = CompleteHdr10Contract,
        };
        var artifact = new OutputValidationSessionArtifact(
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
            OutputTargetsTested: ["Folder"],
            TargetAppsTested: ["Windows Photos"],
            ChecklistIdsCovered: ["REL-OUT-01"],
            ResultSummary: "Windows Photos HDR validation passed.",
            EvidencePaths: ["docs/validation/evidence/photos.md"],
            KnownLimitations: ["Paint and Microsoft Edge not yet validated"],
            FollowUpIssuesOrStories: ["Validate remaining viewers"],
            OutputProfileRecords:
            [
                new(
                    OutputProfileKind.Hdr10Pq,
                    [
                        PassingHdrViewer("Windows Photos"),
                    ]),
            ])
        {
            TargetAppVersions =
            [
                new OutputValidationTargetAppVersionRecord(
                    "Windows Photos",
                    "2026.11040.12001.0"),
            ],
            TargetHdrEvidence = CompleteTargetHdrEvidence,
        };

        var validation = PerfectHdrFidelityProjection.ProjectValidation(contract, artifact);
        var profile = PerfectHdrFidelityProjection.ProjectOutputProfile(artifact.ApplyTo(contract));

        Assert.Contains(validation.ViewerMatrix, viewer =>
            viewer.Name == "Windows Photos"
            && viewer.Status == ValidationEvidenceStatus.Pass);
        Assert.Contains(validation.ViewerMatrix, viewer =>
            viewer.Name == "Microsoft Edge"
            && viewer.Status == ValidationEvidenceStatus.NotRun);
        Assert.Equal(FidelityClaimKind.Unvalidated, profile.FidelityClaim.Kind);
    }

    [Fact]
    public void ProjectValidation_AppliesMultipleValidationSessionArtifacts()
    {
        var contract = OutputProfileContract.Hdr10Pq with
        {
            IsExecutable = true,
            FidelityMode = OutputFidelityMode.HdrPreserved,
            FormatContract = CompleteHdr10Contract,
        };

        var validation = PerfectHdrFidelityProjection.ProjectValidation(
            contract,
            [
                ArtifactFor("Microsoft Paint"),
                ArtifactFor("Windows Photos"),
                ArtifactFor("Microsoft Edge"),
            ]);

        Assert.All(validation.ViewerMatrix, viewer => Assert.Equal(ValidationEvidenceStatus.Pass, viewer.Status));
    }

    [Fact]
    public void ProjectValidation_ReportsTargetAppMatrixAndHdrProfilePassedWhenAllHdrViewerEvidencePasses()
    {
        var contract = OutputProfileContract.Hdr10Pq with
        {
            IsExecutable = true,
            FidelityMode = OutputFidelityMode.HdrPreserved,
            FormatContract = CompleteHdr10Contract,
        };

        var validation = PerfectHdrFidelityProjection.ProjectValidation(
            contract,
            [
                ArtifactFor("Microsoft Paint"),
                ArtifactFor("Windows Photos"),
                ArtifactFor("Microsoft Edge"),
            ]);

        var profileRow = Assert.Single(
            validation.Rows,
            row => row.Label == "HDR-preserved profile");
        var matrixRow = Assert.Single(
            validation.Rows,
            row => row.Label == "Target app matrix");
        var versionRow = Assert.Single(
            validation.Rows,
            row => row.Label == "Target app versions");

        Assert.Equal(ValidationEvidenceStatus.Pass, profileRow.Status);
        Assert.Contains("HDR10 metadata", profileRow.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(ValidationEvidenceStatus.Pass, matrixRow.Status);
        Assert.Contains("All named target apps", matrixRow.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(ValidationEvidenceStatus.Pass, versionRow.Status);
        Assert.Contains("concrete recorded app versions", versionRow.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectValidation_ReportsTargetAppMatrixLimitedWhenNamedViewerEvidenceIsIncomplete()
    {
        var validation = PerfectHdrFidelityProjection.ProjectValidation(
            OutputProfileContract.SrgbCompatibilityPng,
            [
                SdrArtifactFor("Microsoft Paint"),
            ]);
        var matrixRow = Assert.Single(
            validation.Rows,
            row => row.Label == "Target app matrix");
        var versionRow = Assert.Single(
            validation.Rows,
            row => row.Label == "Target app versions");

        Assert.Equal(ValidationEvidenceStatus.Limited, matrixRow.Status);
        Assert.Contains("Windows Photos", matrixRow.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Microsoft Edge", matrixRow.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(ValidationEvidenceStatus.Pass, versionRow.Status);
    }

    [Fact]
    public void ProjectValidation_ReportsTargetAppVersionsLimitedWhenNamedVersionEvidenceIsMissing()
    {
        var validation = PerfectHdrFidelityProjection.ProjectValidation(
            OutputProfileContract.Hdr10Pq,
            [
                ArtifactFor("Microsoft Paint") with
                {
                    TargetAppVersions = [],
                },
                ArtifactFor("Windows Photos"),
                ArtifactFor("Microsoft Edge"),
            ]);
        var versionRow = Assert.Single(
            validation.Rows,
            row => row.Label == "Target app versions");

        Assert.Equal(ValidationEvidenceStatus.Limited, versionRow.Status);
        Assert.Contains("Microsoft Paint", versionRow.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectValidation_ReportsFailedRowsWhenAnyNamedHdrViewerFails()
    {
        var contract = OutputProfileContract.Hdr10Pq with
        {
            IsExecutable = true,
            FidelityMode = OutputFidelityMode.HdrPreserved,
            FormatContract = CompleteHdr10Contract,
            ViewerEvidence =
            [
                PassingHdrViewer("Microsoft Paint"),
                PassingHdrViewer("Windows Photos") with
                {
                    Hdr10MetadataStatus = OutputCompatibilityEvidenceStatus.Fail,
                },
                PassingHdrViewer("Microsoft Edge"),
            ],
        };

        var validation = PerfectHdrFidelityProjection.ProjectValidation(contract);
        var profileRow = Assert.Single(
            validation.Rows,
            row => row.Label == "HDR-preserved profile");
        var matrixRow = Assert.Single(
            validation.Rows,
            row => row.Label == "Target app matrix");

        Assert.Equal(ValidationEvidenceStatus.Fail, profileRow.Status);
        Assert.Contains("failed", profileRow.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(ValidationEvidenceStatus.Fail, matrixRow.Status);
        Assert.Contains("Windows Photos", matrixRow.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectValidation_ReportsVisualMatchPassedWhenAllNamedViewerEvidencePasses()
    {
        var validation = PerfectHdrFidelityProjection.ProjectValidation(
            OutputProfileContract.SrgbCompatibilityPng,
            [
                SdrArtifactFor("Microsoft Paint"),
                SdrArtifactFor("Windows Photos"),
                SdrArtifactFor("Microsoft Edge"),
            ]);
        var visualRow = Assert.Single(
            validation.Rows,
            row => row.Label == "Visual-match output");

        Assert.Equal(ValidationEvidenceStatus.Pass, visualRow.Status);
        Assert.Contains("visual-match evidence passed", visualRow.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectValidation_ReportsVisualMatchLimitedWhenSomeNamedViewerEvidenceIsMissing()
    {
        var validation = PerfectHdrFidelityProjection.ProjectValidation(
            OutputProfileContract.SrgbCompatibilityPng,
            [
                SdrArtifactFor("Microsoft Paint"),
            ]);
        var visualRow = Assert.Single(
            validation.Rows,
            row => row.Label == "Visual-match output");

        Assert.Equal(ValidationEvidenceStatus.Limited, visualRow.Status);
        Assert.Contains("missing", visualRow.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Windows Photos", visualRow.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Microsoft Edge", visualRow.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectValidation_SurfacesManualFormatContractEvidenceWithoutClaimingHdrPreserved()
    {
        var validation = PerfectHdrFidelityProjection.ProjectValidation(
            OutputProfileContract.Hdr10Pq,
            [
                ArtifactWithFormatContract("Windows Photos"),
            ]);
        var profileRow = Assert.Single(
            validation.Rows,
            row => row.Label == "HDR-preserved profile");

        Assert.Equal(ValidationEvidenceStatus.Limited, profileRow.Status);
        Assert.Contains("format contract evidence", profileRow.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Windows manual", profileRow.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("viewer", profileRow.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("passed", profileRow.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectValidation_DoesNotSurfaceFormatContractEvidenceFromIncompleteManualSession()
    {
        var artifact = ArtifactWithFormatContract("Windows Photos") with
        {
            EvidencePaths = [],
        };
        var validation = PerfectHdrFidelityProjection.ProjectValidation(
            OutputProfileContract.Hdr10Pq,
            [
                artifact,
            ]);
        var profileRow = Assert.Single(
            validation.Rows,
            row => row.Label == "HDR-preserved profile");

        Assert.Equal(ValidationEvidenceStatus.NotRun, profileRow.Status);
        Assert.Contains("At least one supported profile", profileRow.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("format contract evidence", profileRow.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectOutputProfile_AppliesValidationSessionArtifactsBeforeClaimGate()
    {
        var contract = OutputProfileContract.Hdr10Pq with
        {
            IsExecutable = true,
            FidelityMode = OutputFidelityMode.HdrPreserved,
            FormatContract = CompleteHdr10Contract,
        };

        var profile = PerfectHdrFidelityProjection.ProjectOutputProfile(
            contract,
            [
                ArtifactFor("Microsoft Paint"),
                ArtifactFor("Windows Photos"),
                ArtifactFor("Microsoft Edge"),
            ]);

        Assert.Equal(FidelityClaimKind.HdrPreserved, profile.FidelityClaim.Kind);
    }

    [Fact]
    public void ProjectOutputProfile_RuntimeCapabilitiesBlockHdrPreservedClaimEvenWithCompleteArtifacts()
    {
        var profile = PerfectHdrFidelityProjection.ProjectOutputProfile(
            OutputProfileContract.Hdr10Pq,
            [
                ArtifactFor("Microsoft Paint"),
                ArtifactFor("Windows Photos"),
                ArtifactFor("Microsoft Edge"),
            ],
            readiness: null,
            OutputProfileExecutionCapabilities.CompatibilityOnly);

        Assert.Equal("HDR10", profile.Label);
        Assert.Equal("Build", profile.StatusLabel);
        Assert.Equal(FidelityClaimKind.Converted, profile.FidelityClaim.Kind);
        Assert.Contains("compatibility fallback", profile.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("implementation prerequisites", profile.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("validated HDR-preserved", profile.FidelityClaim.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectOutputProfile_ClipboardTargetKeepsHdr10OnCompatibilityPathEvenWithCompleteArtifacts()
    {
        OutputValidationSessionArtifact[] artifacts =
        [
            ArtifactWithFormatContract("Microsoft Paint"),
            ArtifactWithFormatContract("Windows Photos"),
            ArtifactWithFormatContract("Microsoft Edge"),
        ];

        var profile = PerfectHdrFidelityProjection.ProjectOutputProfile(
            OutputProfileContract.Hdr10Pq,
            artifacts,
            readiness: null,
            ValidateOnlyHdr10Capabilities(artifacts),
            OutputTarget.Clipboard);

        Assert.Equal("HDR10", profile.Label);
        Assert.Equal("Compat", profile.StatusLabel);
        Assert.True(profile.IsReadOnly);
        Assert.Equal(FidelityClaimKind.Converted, profile.FidelityClaim.Kind);
        Assert.Contains("clipboard output stays on sRGB compatibility output", profile.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("do not promote the clipboard target", profile.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectOutputProfile_BothTargetKeepsOverallFidelityConvertedEvenWhenFolderHdr10IsReady()
    {
        OutputValidationSessionArtifact[] artifacts =
        [
            ArtifactWithFormatContract("Microsoft Paint"),
            ArtifactWithFormatContract("Windows Photos"),
            ArtifactWithFormatContract("Microsoft Edge"),
        ];

        var profile = PerfectHdrFidelityProjection.ProjectOutputProfile(
            OutputProfileContract.Hdr10Pq,
            artifacts,
            readiness: null,
            ValidateOnlyHdr10Capabilities(artifacts),
            OutputTarget.Both);

        Assert.Equal("HDR10", profile.Label);
        Assert.Equal("Ready", profile.StatusLabel);
        Assert.Equal(FidelityClaimKind.Converted, profile.FidelityClaim.Kind);
        Assert.Contains("Both-target output still keeps clipboard on sRGB compatibility fallback", profile.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("folder artifacts separately", profile.FidelityClaim.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectOutputProfile_ClaimsHdrPreservedOnlyWhenArtifactsAndRuntimeCapabilitiesPass()
    {
        var profile = PerfectHdrFidelityProjection.ProjectOutputProfile(
            OutputProfileContract.Hdr10Pq,
            [
                ArtifactWithFormatContract("Microsoft Paint"),
                ArtifactWithFormatContract("Windows Photos"),
                ArtifactWithFormatContract("Microsoft Edge"),
            ],
            readiness: null,
            ValidateOnlyHdr10Capabilities(
                [
                    ArtifactWithFormatContract("Microsoft Paint"),
                    ArtifactWithFormatContract("Windows Photos"),
                    ArtifactWithFormatContract("Microsoft Edge"),
                ]));

        Assert.Equal("HDR10", profile.Label);
        Assert.Equal("Ready", profile.StatusLabel);
        Assert.Equal("Validated HDR10-preserved artifact contract is active for this session.", profile.Contract.DestinationPolicy);
        Assert.Contains("validated HDR-preserved path", profile.Contract.ConversionPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validated for the active HDR-preserved path", profile.Contract.MetadataPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("compatibility evidence passed", profile.Contract.ViewerCompatibilityPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(FidelityClaimKind.HdrPreserved, profile.FidelityClaim.Kind);
        Assert.Contains("validated HDR-preserved", profile.FidelityClaim.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectValidation_RuntimeCapabilitiesKeepCompleteHdrArtifactsLimitedUntilExecutable()
    {
        var validation = PerfectHdrFidelityProjection.ProjectValidation(
            OutputProfileContract.Hdr10Pq,
            [
                ArtifactWithFormatContract("Microsoft Paint"),
                ArtifactWithFormatContract("Windows Photos"),
                ArtifactWithFormatContract("Microsoft Edge"),
            ],
            OutputProfileExecutionCapabilities.CompatibilityOnly);
        var profileRow = Assert.Single(
            validation.Rows,
            row => row.Label == "HDR-preserved profile");
        var matrixRow = Assert.Single(
            validation.Rows,
            row => row.Label == "Target app matrix");

        Assert.Equal("HDR10", validation.OutputProfileGate.ProfileLabel);
        Assert.Equal("Build", validation.OutputProfileGate.StatusLabel);
        Assert.Contains("implementation prerequisites", validation.OutputProfileGate.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(ValidationEvidenceStatus.Limited, profileRow.Status);
        Assert.Contains("executable output", profileRow.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(ValidationEvidenceStatus.Pass, matrixRow.Status);
    }

    [Fact]
    public void ProjectValidation_RuntimeCapabilitiesSurfaceValidateGateBeforeExecutableHdr10()
    {
        OutputValidationSessionArtifact[] artifacts =
        [
            ArtifactFor("Microsoft Paint"),
            ArtifactFor("Windows Photos"),
            ArtifactFor("Microsoft Edge"),
        ];

        var validation = PerfectHdrFidelityProjection.ProjectValidation(
            OutputProfileContract.Hdr10Pq,
            artifacts,
            ValidateOnlyHdr10Capabilities(artifacts));

        Assert.Equal("HDR10", validation.OutputProfileGate.ProfileLabel);
        Assert.Equal("Validate", validation.OutputProfileGate.StatusLabel);
        Assert.Equal(ValidationEvidenceStatus.Limited, validation.OutputProfileGate.Status);
        Assert.Contains("viewer evidence", validation.OutputProfileGate.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectValidation_RuntimeCapabilitiesSurfaceReadyGateWhenExecutableHdr10Passes()
    {
        var validation = PerfectHdrFidelityProjection.ProjectValidation(
            OutputProfileContract.Hdr10Pq,
            [
                ArtifactWithFormatContract("Microsoft Paint"),
                ArtifactWithFormatContract("Windows Photos"),
                ArtifactWithFormatContract("Microsoft Edge"),
            ],
            OutputProfileExecutionCapabilities.Create(
                OutputProfileExecutionCapability.SrgbCompatibility,
                OutputProfileExecutionCapability.Hdr10PreservedImplementedArtifactEncoder));

        Assert.Equal("HDR10", validation.OutputProfileGate.ProfileLabel);
        Assert.Equal("Ready", validation.OutputProfileGate.StatusLabel);
        Assert.Equal(ValidationEvidenceStatus.Pass, validation.OutputProfileGate.Status);
        Assert.Contains("validated session", validation.OutputProfileGate.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectValidationRecord_UsesBuildVersionAndKeepsManualValidationNotRun()
    {
        var record = PerfectHdrFidelityProjection.ProjectValidationRecord("v2.3.4");

        Assert.Equal("Build v2.3.4", record.BuildLabel);
        Assert.Equal(ValidationEvidenceStatus.Limited, record.AutomatedEvidenceStatus);
        Assert.Equal(ValidationEvidenceStatus.NotRun, record.WindowsManualValidationStatus);
        Assert.Contains("Windows CI", record.AutomatedEvidenceDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("manual validation", record.WindowsManualValidationDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("harness/validation/release-validation-checklist.md", record.EvidenceDocumentPath);
        Assert.DoesNotContain("HDR-preserved", record.AutomatedEvidenceDetail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR-preserved", record.WindowsManualValidationDetail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectValidationRecord_WithWorkspaceOnlySurfacesSeededValidationWorkspace()
    {
        var snapshot = new OutputValidationArtifactSnapshot([], [])
        {
            Workspace = new OutputValidationWorkspaceState(
                "C:\\Users\\Tester\\AppData\\Local\\Lumiere\\validation\\output",
                "C:\\Users\\Tester\\AppData\\Local\\Lumiere\\validation\\output\\templates",
                "C:\\Users\\Tester\\AppData\\Local\\Lumiere\\validation\\output\\evidence",
                "C:\\Users\\Tester\\AppData\\Local\\Lumiere\\validation\\output\\README.txt",
                "C:\\Users\\Tester\\AppData\\Local\\Lumiere\\validation\\output\\templates\\output-validation-session.schema-v4.sample.json",
                "C:\\Users\\Tester\\AppData\\Local\\Lumiere\\validation\\output\\templates\\resource-trend-session-template.md",
                "C:\\Users\\Tester\\AppData\\Local\\Lumiere\\validation\\output\\collect-resource-trend-samples.ps1",
                []),
        };

        var record = PerfectHdrFidelityProjection.ProjectValidationRecord("v2.3.4", snapshot);

        Assert.Equal(ValidationEvidenceStatus.Limited, record.WindowsManualValidationStatus);
        Assert.Contains("Validation workspace:", record.WindowsManualValidationDetail);
        Assert.Contains("seeded sample", record.WindowsManualValidationDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No output validation artifact is loaded", record.WindowsManualValidationDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("C:\\Users\\Tester\\AppData\\Local\\Lumiere\\validation\\output", record.ValidationWorkspacePath);
        Assert.Equal("C:\\Users\\Tester\\AppData\\Local\\Lumiere\\validation\\output\\templates\\output-validation-session.schema-v4.sample.json", record.ValidationTemplatePath);
        Assert.Equal("C:\\Users\\Tester\\AppData\\Local\\Lumiere\\validation\\output\\templates\\resource-trend-session-template.md", record.ResourceTrendTemplatePath);
        Assert.Equal("C:\\Users\\Tester\\AppData\\Local\\Lumiere\\validation\\output\\collect-resource-trend-samples.ps1", record.ResourceTrendScriptPath);
        Assert.True(record.CanOpenResourceTrendTemplate);
        Assert.True(record.CanOpenResourceTrendScript);
        Assert.True(record.CanCopyResourceTrendCommand);
    }

    [Fact]
    public void ProjectValidationRecord_WithWorkspaceFailureSurfacesSetupProblemWithoutClaimingManualPass()
    {
        var snapshot = new OutputValidationArtifactSnapshot([], [])
        {
            Workspace = new OutputValidationWorkspaceState(
                "C:\\Users\\Tester\\AppData\\Local\\Lumiere\\validation\\output",
                "C:\\Users\\Tester\\AppData\\Local\\Lumiere\\validation\\output\\templates",
                "C:\\Users\\Tester\\AppData\\Local\\Lumiere\\validation\\output\\evidence",
                "C:\\Users\\Tester\\AppData\\Local\\Lumiere\\validation\\output\\README.txt",
                null,
                "C:\\Users\\Tester\\AppData\\Local\\Lumiere\\validation\\output\\templates\\resource-trend-session-template.md",
                "C:\\Users\\Tester\\AppData\\Local\\Lumiere\\validation\\output\\collect-resource-trend-samples.ps1",
                [new OutputValidationWorkspaceIssue(
                    "C:\\Users\\Tester\\AppData\\Local\\Lumiere\\validation\\output\\templates\\output-validation-session.schema-v4.sample.json",
                    "Validation sample template source could not be loaded from the current build.")]),
        };

        var record = PerfectHdrFidelityProjection.ProjectValidationRecord("v2.3.4", snapshot);

        Assert.Equal(ValidationEvidenceStatus.Limited, record.WindowsManualValidationStatus);
        Assert.Contains("workspace is not ready", record.WindowsManualValidationDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sample template source", record.WindowsManualValidationDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("harness/validation/output-validation.md", record.EvidenceDocumentPath);
        Assert.Equal("C:\\Users\\Tester\\AppData\\Local\\Lumiere\\validation\\output", record.ValidationWorkspacePath);
        Assert.Null(record.ValidationTemplatePath);
        Assert.Equal("C:\\Users\\Tester\\AppData\\Local\\Lumiere\\validation\\output\\templates\\resource-trend-session-template.md", record.ResourceTrendTemplatePath);
        Assert.Equal("C:\\Users\\Tester\\AppData\\Local\\Lumiere\\validation\\output\\collect-resource-trend-samples.ps1", record.ResourceTrendScriptPath);
        Assert.True(record.CanOpenResourceTrendTemplate);
        Assert.True(record.CanOpenResourceTrendScript);
        Assert.True(record.CanCopyResourceTrendCommand);
    }

    [Fact]
    public void ProjectValidationRecord_WithMismatchedBuildAlignmentCallsOutStaleEvidence()
    {
        var snapshot = new OutputValidationArtifactSnapshot(
            [ArtifactFor("Windows Photos")],
            []);

        var record = PerfectHdrFidelityProjection.ProjectValidationRecord("2.3.4+deadbee", snapshot);

        Assert.Equal(ValidationEvidenceStatus.Limited, record.WindowsManualValidationStatus);
        Assert.Contains("not aligned with the current build", record.WindowsManualValidationDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("deadbee", record.WindowsManualValidationDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("485bc31", record.WindowsManualValidationDetail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectValidationEvidenceSummary_WithArtifactsSurfacesLatestCoverageAndFollowUp()
    {
        var summary = PerfectHdrFidelityProjection.ProjectValidationEvidenceSummary(
            [
                ArtifactFor("Microsoft Paint") with
                {
                    Date = "2026-06-21",
                    ResultSummary = "Paint validation passed.",
                },
                ArtifactFor("Windows Photos") with
                {
                    Date = "2026-06-22",
                    OutputTargetsTested = ["Folder", "Both"],
                    TargetAppsTested = ["Windows Photos", "Microsoft Edge"],
                    TargetAppVersions =
                    [
                        new OutputValidationTargetAppVersionRecord("Windows Photos", "2026.11040.12001.0"),
                        new OutputValidationTargetAppVersionRecord("Microsoft Edge", "138.0.7204.101"),
                    ],
                    ChecklistIdsCovered = ["REL-OUT-01", "REL-HDR-04"],
                    KnownLimitations = ["Microsoft Edge metadata recognition is still pending."],
                    FollowUpIssuesOrStories = ["11-3", "12-1"],
                    ResultSummary = "Windows Photos validation passed with pending Microsoft Edge follow-up.",
                },
            ]);

        Assert.Equal(ValidationEvidenceStatus.Limited, summary.Status);
        Assert.Contains("2 artifact", summary.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2026-06-22", summary.Summary, StringComparison.Ordinal);
        Assert.Contains("Windows Photos validation passed", summary.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("targets Folder, Both", summary.CoverageDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Windows Photos", summary.CoverageDetail, StringComparison.Ordinal);
        Assert.Contains("2026.11040.12001.0", summary.CoverageDetail, StringComparison.Ordinal);
        Assert.Contains("REL-HDR-04", summary.CoverageDetail, StringComparison.Ordinal);
        Assert.Contains("Known limitations", summary.GapDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Microsoft Edge metadata recognition", summary.GapDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Follow-up: 11-3, 12-1", summary.GapDetail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectValidationEvidenceSummary_CallsOutMissingTargetAppVersions()
    {
        var summary = PerfectHdrFidelityProjection.ProjectValidationEvidenceSummary(
            [
                ArtifactFor("Windows Photos") with
                {
                    TargetAppVersions = [],
                },
            ]);

        Assert.Contains("Target app versions are still missing for Windows Photos", summary.GapDetail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectValidationEvidenceSummary_WithMatchingCurrentBuildSurfacesPassAlignment()
    {
        var snapshot = new OutputValidationArtifactSnapshot(
            [ArtifactFor("Windows Photos")],
            [])
        {
            ArtifactReferences =
            [
                new OutputValidationArtifactReference(
                    "C:\\Validation\\windows-photos.json",
                    ArtifactFor("Windows Photos")),
            ],
        };

        var summary = PerfectHdrFidelityProjection.ProjectValidationEvidenceSummary(snapshot, "2.3.4+485bc31");

        Assert.Equal(ValidationEvidenceStatus.Pass, summary.BuildAlignment.Status);
        Assert.Equal("Matched current build", summary.BuildAlignment.StatusLabel);
        Assert.Contains("matches the current build token", summary.BuildAlignment.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("485bc31", summary.BuildAlignment.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectValidationEvidenceSummary_WithMismatchedCurrentBuildSurfacesStaleAlignment()
    {
        var snapshot = new OutputValidationArtifactSnapshot(
            [ArtifactFor("Windows Photos")],
            [])
        {
            ArtifactReferences =
            [
                new OutputValidationArtifactReference(
                    "C:\\Validation\\windows-photos.json",
                    ArtifactFor("Windows Photos")),
            ],
        };

        var summary = PerfectHdrFidelityProjection.ProjectValidationEvidenceSummary(snapshot, "2.3.4+deadbee");

        Assert.Equal(ValidationEvidenceStatus.Limited, summary.BuildAlignment.Status);
        Assert.Equal("Stale for current build", summary.BuildAlignment.StatusLabel);
        Assert.Contains("stale for the current build", summary.BuildAlignment.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("deadbee", summary.BuildAlignment.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("485bc31", summary.BuildAlignment.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectValidationEvidenceSummary_WithSnapshotIssuesCallsOutIgnoredFiles()
    {
        var snapshot = new OutputValidationArtifactSnapshot(
            [ArtifactFor("Windows Photos")],
            [new("C:\\Validation\\bad.json", "JsonException: invalid JSON")]);
        snapshot = snapshot with
        {
            ArtifactReferences =
            [
                new OutputValidationArtifactReference(
                    "C:\\Validation\\windows-photos.json",
                    snapshot.Artifacts[0]),
            ],
        };

        var summary = PerfectHdrFidelityProjection.ProjectValidationEvidenceSummary(snapshot);

        Assert.Equal(ValidationEvidenceStatus.Limited, summary.Status);
        Assert.Contains("1 file", summary.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("bad.json", summary.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Ignored files must be fixed", summary.GapDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("C:\\Validation\\windows-photos.json", summary.LatestArtifactPath);
        Assert.True(summary.CanOpenLatestArtifact);
    }

    [Fact]
    public void ProjectValidationEvidenceSummary_WithWorkspaceFailureKeepsCoverageEmpty()
    {
        var snapshot = new OutputValidationArtifactSnapshot([], [])
        {
            Workspace = new OutputValidationWorkspaceState(
                "C:\\Users\\Tester\\AppData\\Local\\Lumiere\\validation\\output",
                "C:\\Users\\Tester\\AppData\\Local\\Lumiere\\validation\\output\\templates",
                "C:\\Users\\Tester\\AppData\\Local\\Lumiere\\validation\\output\\evidence",
                "C:\\Users\\Tester\\AppData\\Local\\Lumiere\\validation\\output\\README.txt",
                null,
                null,
                null,
                [new OutputValidationWorkspaceIssue(
                    "C:\\Users\\Tester\\AppData\\Local\\Lumiere\\validation\\output\\templates\\output-validation-session.schema-v4.sample.json",
                    "Validation sample template source could not be loaded from the current build.")]),
        };

        var summary = PerfectHdrFidelityProjection.ProjectValidationEvidenceSummary(snapshot);

        Assert.Equal(ValidationEvidenceStatus.NotRun, summary.Status);
        Assert.Contains("workspace is not ready", summary.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Coverage: none yet.", summary.CoverageDetail);
        Assert.Contains("fix the local validation workspace", summary.GapDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sample template source", summary.GapDetail, StringComparison.OrdinalIgnoreCase);
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
            EvidencePaths: [$"docs/validation/evidence/{viewerName}.md"],
            KnownLimitations: [],
            FollowUpIssuesOrStories: [],
            OutputProfileRecords:
            [
                new(
                    OutputProfileKind.Hdr10Pq,
                    [
                        PassingHdrViewer(viewerName),
                    ]),
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

    private static OutputValidationSessionArtifact ArtifactWithFormatContract(string viewerName) =>
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

    private static OutputValidationSessionArtifact ArtifactWithIncompleteViewerEvidence(string viewerName) =>
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
            ResultSummary: $"{viewerName} HDR validation is incomplete.",
            EvidencePaths: [$"docs/validation/evidence/{viewerName}.md"],
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

    private static OutputValidationSessionArtifact SdrArtifactFor(string viewerName) =>
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
            OutputTargetsTested: ["Clipboard"],
            TargetAppsTested: [viewerName],
            ChecklistIdsCovered: ["REL-OUT-01"],
            ResultSummary: $"{viewerName} visual-match validation passed.",
            EvidencePaths: [$"docs/validation/evidence/{viewerName}.md"],
            KnownLimitations: [],
            FollowUpIssuesOrStories: [],
            OutputProfileRecords:
            [
                new(
                    OutputProfileKind.SrgbCompatibilityPng,
                    [
                        new(
                            viewerName,
                            OutputCompatibilityEvidenceStatus.Pass,
                            OutputCompatibilityEvidenceStatus.Pass,
                            OutputCompatibilityEvidenceStatus.NotApplicable,
                            "Validated SDR visual-match viewer."),
                    ]),
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
}
