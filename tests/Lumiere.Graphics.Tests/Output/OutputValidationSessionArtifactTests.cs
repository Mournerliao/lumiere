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
            EvidencePaths: ["knowledge/evidence/photos-hdr10.md"],
            KnownLimitations: ["Paint not yet validated"],
            FollowUpIssuesOrStories: ["Validate Paint and Microsoft Edge viewers"],
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
                            "Manual HDR validation passed in Windows Photos.")
                        {
                            Hdr10MetadataStatus = OutputCompatibilityEvidenceStatus.Pass,
                        },
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

        var roundTripped = OutputValidationSessionArtifact.FromJson(artifact.ToJson());

        Assert.Equal("2026-06-21", roundTripped.Date);
        Assert.Equal(4, roundTripped.SchemaVersion);
        Assert.Equal("04a8dd6", roundTripped.BuildCommit);
        var targetAppVersion = Assert.Single(roundTripped.TargetAppVersions);
        Assert.Equal("Windows Photos", targetAppVersion.Name);
        Assert.Equal("2026.11040.12001.0", targetAppVersion.Version);
        Assert.Equal(["REL-OUT-01"], roundTripped.ChecklistIdsCovered);
        Assert.Equal(["knowledge/evidence/photos-hdr10.md"], roundTripped.EvidencePaths);
        Assert.NotNull(roundTripped.TargetHdrEvidence);
        Assert.Equal("HDR primary", roundTripped.TargetHdrEvidence.TargetDisplayName);
        Assert.Equal("DesktopBounds", roundTripped.TargetHdrEvidence.MatchKind);
        Assert.Equal("Active", roundTripped.TargetHdrEvidence.HdrState);
        Assert.Equal(OutputValidationEvidenceSource.WindowsManual, roundTripped.OutputProfileRecords[0].EvidenceSource);
        var viewer = Assert.Single(roundTripped.OutputProfileRecords[0].ViewerEvidence);
        Assert.Equal("Windows Photos", viewer.Name);
        Assert.Equal(OutputCompatibilityEvidenceStatus.Pass, viewer.HdrPreservationStatus);
        Assert.Equal(OutputCompatibilityEvidenceStatus.Pass, viewer.Hdr10MetadataStatus);
    }

    [Fact]
    public void JsonRoundTrip_PreservesTargetAwareHdrEvidence()
    {
        var artifact = CreateArtifact([]);

        var roundTripped = OutputValidationSessionArtifact.FromJson(artifact.ToJson());

        Assert.NotNull(roundTripped.TargetHdrEvidence);
        Assert.Equal("HDR primary", roundTripped.TargetHdrEvidence.TargetDisplayName);
        Assert.Equal(0, roundTripped.TargetHdrEvidence.Left);
        Assert.Equal(0, roundTripped.TargetHdrEvidence.Top);
        Assert.Equal(3840, roundTripped.TargetHdrEvidence.Width);
        Assert.Equal(2160, roundTripped.TargetHdrEvidence.Height);
        Assert.Equal("DesktopBounds", roundTripped.TargetHdrEvidence.MatchKind);
        Assert.Equal("RgbFullG2084NoneP2020", roundTripped.TargetHdrEvidence.ColorSpace);
        Assert.Contains("target-aware", roundTripped.TargetHdrEvidence.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void JsonRoundTrip_PreservesProfileFormatContractEvidence()
    {
        var artifact = CreateArtifact(
            [
                new(
                    OutputProfileKind.Hdr10Pq,
                    [
                        PassingHdrViewer("Microsoft Paint"),
                    ])
                {
                    FormatContract = CompleteHdr10Contract,
                },
            ]);

        var roundTripped = OutputValidationSessionArtifact.FromJson(artifact.ToJson());
        var record = Assert.Single(roundTripped.OutputProfileRecords);

        Assert.NotNull(record.FormatContract);
        Assert.Equal(OutputPixelFormat.R16G16B16A16Float, record.FormatContract.SourcePixelFormat);
        Assert.Equal(OutputTransferFunction.PqSt2084, record.FormatContract.TransferFunction);
        Assert.Equal(OutputMetadataPolicy.AttachHdr10StaticMetadata, record.FormatContract.MetadataPolicy);
    }

    [Fact]
    public void CoversOutputTarget_TreatsBothAsCoveringClipboardAndFolder()
    {
        var artifact = CreateArtifact([]) with
        {
            OutputTargetsTested = ["Both"],
        };

        Assert.True(artifact.CoversOutputTarget(OutputTarget.Both));
        Assert.True(artifact.CoversOutputTarget(OutputTarget.Clipboard));
        Assert.True(artifact.CoversOutputTarget(OutputTarget.Folder));
    }

    [Fact]
    public void CoversOutputTarget_DoesNotTreatClipboardOnlyEvidenceAsFolderCoverage()
    {
        var artifact = CreateArtifact([]);

        Assert.True(artifact.CoversOutputTarget(OutputTarget.Clipboard));
        Assert.False(artifact.CoversOutputTarget(OutputTarget.Folder));
    }

    [Fact]
    public void CoversProfileOutputTarget_UsesRecordLevelTargetsWhenProvided()
    {
        var artifact = CreateArtifact(
            [
                new(
                    OutputProfileKind.Hdr10Pq,
                    [
                        PassingHdrViewer("Windows Photos"),
                    ])
                {
                    OutputTargetsCovered = ["Clipboard"],
                },
            ]) with
        {
            OutputTargetsTested = ["Both"],
        };

        Assert.True(artifact.CoversOutputTarget(OutputTarget.Folder));
        Assert.True(artifact.CoversProfileOutputTarget(OutputProfileKind.Hdr10Pq, OutputTarget.Clipboard));
        Assert.False(artifact.CoversProfileOutputTarget(OutputProfileKind.Hdr10Pq, OutputTarget.Folder));
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
            FormatContract = CompleteHdr10Contract,
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
    public void ApplyTo_AppliesCompleteManualFormatContractToMatchingProfile()
    {
        var artifact = CreateArtifact(
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

        var updated = artifact.ApplyTo(OutputProfileContract.Hdr10Pq);

        Assert.True(updated.HasCompleteFormatContract);
        Assert.Equal(OutputTransferFunction.PqSt2084, updated.FormatContract.TransferFunction);
        Assert.Equal(OutputMetadataPolicy.AttachHdr10StaticMetadata, updated.FormatContract.MetadataPolicy);
    }

    [Fact]
    public void ApplyTo_WithTarget_DoesNotApplyRecordThatDoesNotCoverRequestedOutputTarget()
    {
        var artifact = CreateArtifact(
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
                    OutputTargetsCovered = ["Clipboard"],
                },
            ]) with
        {
            OutputTargetsTested = ["Both"],
        };

        var folderUpdated = artifact.ApplyTo(OutputProfileContract.Hdr10Pq, OutputTarget.Folder);
        var clipboardUpdated = artifact.ApplyTo(OutputProfileContract.Hdr10Pq, OutputTarget.Clipboard);

        Assert.False(folderUpdated.HasCompleteFormatContract);
        Assert.True(clipboardUpdated.HasCompleteFormatContract);
    }

    [Fact]
    public void ApplyTo_DoesNotApplyFormatContractFromIncompleteManualSession()
    {
        var artifact = CreateArtifact(
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
            ]) with
        {
            EvidencePaths = [],
        };

        var updated = artifact.ApplyTo(OutputProfileContract.Hdr10Pq);

        Assert.False(updated.HasCompleteFormatContract);
        Assert.Equal(OutputTransferFunction.NotDefined, updated.FormatContract.TransferFunction);
    }

    [Fact]
    public void FromJson_RejectsUnsupportedSchemaVersion()
    {
        var json = CreateArtifact([]).ToJson().Replace("\"schemaVersion\": 4", "\"schemaVersion\": 99", StringComparison.Ordinal);

        var exception = Assert.Throws<InvalidOperationException>(() => OutputValidationSessionArtifact.FromJson(json));

        Assert.Contains("schema", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FromJson_AcceptsLegacySchemaVersionOneArtifactsWithoutFormatContract()
    {
        var json = CreateArtifact([])
            .ToJson()
            .Replace("\"schemaVersion\": 4", "\"schemaVersion\": 1", StringComparison.Ordinal);

        var artifact = OutputValidationSessionArtifact.FromJson(json);

        Assert.Equal(1, artifact.SchemaVersion);
        Assert.Empty(artifact.OutputProfileRecords);
    }

    [Fact]
    public void ApplyTo_TreatsIncompleteManualSessionAsLimitedEvidence()
    {
        var artifact = CreateArtifact(
            [
                new(
                    OutputProfileKind.Hdr10Pq,
                    [
                        PassingHdrViewer("Microsoft Paint"),
                        PassingHdrViewer("Windows Photos"),
                        PassingHdrViewer("Microsoft Edge"),
                    ]),
            ]) with
        {
            WindowsVersion = "",
            EvidencePaths = [],
        };
        var contract = OutputProfileContract.Hdr10Pq with
        {
            IsExecutable = true,
            FidelityMode = OutputFidelityMode.HdrPreserved,
            FormatContract = CompleteHdr10Contract,
        };

        var updated = artifact.ApplyTo(contract);
        var summary = updated.EvaluateEvidence();

        Assert.All(
            updated.ViewerEvidence,
            viewer =>
            {
                Assert.Equal(OutputCompatibilityEvidenceStatus.Limited, viewer.ArtifactHandlingStatus);
                Assert.Equal(OutputCompatibilityEvidenceStatus.Limited, viewer.VisualMatchStatus);
                Assert.Equal(OutputCompatibilityEvidenceStatus.Limited, viewer.HdrPreservationStatus);
                Assert.Contains("Validation session incomplete", viewer.Detail, StringComparison.OrdinalIgnoreCase);
            });
        Assert.False(summary.AllowsVisualMatchClaim);
        Assert.False(summary.AllowsHdrPreservedClaim);
    }

    [Fact]
    public void ApplyTo_TreatsMissingTargetAwareHdrEvidenceAsIncompleteManualSession()
    {
        var artifact = CreateArtifact(
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
            ]) with
        {
            TargetHdrEvidence = null,
        };
        var contract = OutputProfileContract.Hdr10Pq with
        {
            IsExecutable = true,
            FidelityMode = OutputFidelityMode.HdrPreserved,
        };

        var updated = artifact.ApplyTo(contract);

        Assert.False(updated.HasCompleteFormatContract);
        Assert.All(
            updated.ViewerEvidence,
            viewer =>
            {
                Assert.Equal(OutputCompatibilityEvidenceStatus.Limited, viewer.ArtifactHandlingStatus);
                Assert.Contains("target-aware HDR evidence", viewer.Detail, StringComparison.OrdinalIgnoreCase);
            });
    }

    [Fact]
    public void ApplyTo_TreatsMissingTargetAwareColorSpaceAsIncompleteManualSession()
    {
        var artifact = CreateArtifact(
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
            ]) with
        {
            TargetHdrEvidence = CompleteTargetHdrEvidence with
            {
                ColorSpace = "REPLACE_WITH_OBSERVED_TARGET_COLOR_SPACE",
            },
        };
        var contract = OutputProfileContract.Hdr10Pq with
        {
            IsExecutable = true,
            FidelityMode = OutputFidelityMode.HdrPreserved,
        };

        var updated = artifact.ApplyTo(contract);

        Assert.False(updated.HasCompleteFormatContract);
        Assert.All(
            updated.ViewerEvidence,
            viewer =>
            {
                Assert.Equal(OutputCompatibilityEvidenceStatus.Limited, viewer.ArtifactHandlingStatus);
                Assert.Contains("target-aware HDR evidence color space", viewer.Detail, StringComparison.OrdinalIgnoreCase);
            });
    }

    [Fact]
    public void ApplyTo_TreatsMissingTargetAppVersionsAsIncompleteManualSession()
    {
        var artifact = CreateArtifact(
            [
                new(
                    OutputProfileKind.Hdr10Pq,
                    [
                        PassingHdrViewer("Windows Photos"),
                    ])
                {
                    FormatContract = CompleteHdr10Contract,
                },
            ]) with
        {
            TargetAppVersions = [],
        };
        var contract = OutputProfileContract.Hdr10Pq with
        {
            IsExecutable = true,
            FidelityMode = OutputFidelityMode.HdrPreserved,
        };

        var updated = artifact.ApplyTo(contract);

        Assert.False(updated.HasCompleteFormatContract);
        var viewer = Assert.Single(updated.ViewerEvidence, evidence => evidence.Name == "Windows Photos");
        Assert.Equal(OutputCompatibilityEvidenceStatus.Limited, viewer.ArtifactHandlingStatus);
        Assert.Contains("target app version for Windows Photos", viewer.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplyTo_AllowsCompleteManualSessionToSatisfyEvidenceGate()
    {
        var artifact = CreateArtifact(
            [
                new(
                    OutputProfileKind.Hdr10Pq,
                    [
                        PassingHdrViewer("Microsoft Paint"),
                        PassingHdrViewer("Windows Photos"),
                        PassingHdrViewer("Microsoft Edge"),
                    ]),
            ]);
        var contract = OutputProfileContract.Hdr10Pq with
        {
            IsExecutable = true,
            FidelityMode = OutputFidelityMode.HdrPreserved,
            FormatContract = CompleteHdr10Contract,
        };

        var updated = artifact.ApplyTo(contract);
        var summary = updated.EvaluateEvidence();

        Assert.True(summary.AllowsVisualMatchClaim);
        Assert.True(summary.AllowsHdrPreservedClaim);
    }

    [Fact]
    public void ApplyAllTo_DoesNotLetIncompleteSessionDowngradeCompleteManualEvidence()
    {
        var complete = CreateArtifact(
            [
                new(
                    OutputProfileKind.Hdr10Pq,
                    [
                        PassingHdrViewer("Microsoft Paint"),
                        PassingHdrViewer("Windows Photos"),
                        PassingHdrViewer("Microsoft Edge"),
                    ]),
            ]);
        var incomplete = CreateArtifact(
            [
                new(
                    OutputProfileKind.Hdr10Pq,
                    [
                        PassingHdrViewer("Windows Photos"),
                    ]),
            ]) with
        {
            EvidencePaths = [],
        };
        var contract = OutputProfileContract.Hdr10Pq with
        {
            IsExecutable = true,
            FidelityMode = OutputFidelityMode.HdrPreserved,
            FormatContract = CompleteHdr10Contract,
        };

        var updated = OutputValidationSessionArtifact.ApplyAllTo(contract, [complete, incomplete]);

        var photos = Assert.Single(updated.ViewerEvidence, viewer => viewer.Name == "Windows Photos");
        Assert.Equal(OutputCompatibilityEvidenceStatus.Pass, photos.ArtifactHandlingStatus);
        Assert.Equal(OutputCompatibilityEvidenceStatus.Pass, photos.VisualMatchStatus);
        Assert.Equal(OutputCompatibilityEvidenceStatus.Pass, photos.HdrPreservationStatus);
        Assert.Equal(OutputCompatibilityEvidenceStatus.Pass, photos.Hdr10MetadataStatus);
        Assert.True(updated.EvaluateEvidence().AllowsHdrPreservedClaim);
    }

    [Fact]
    public void ApplyAllTo_KeepsAnyFailedViewerEvidenceBlockingReleaseClaims()
    {
        var complete = CreateArtifact(
            [
                new(
                    OutputProfileKind.Hdr10Pq,
                    [
                        PassingHdrViewer("Microsoft Paint"),
                        PassingHdrViewer("Windows Photos"),
                        PassingHdrViewer("Microsoft Edge"),
                    ]),
            ]);
        var failed = CreateArtifact(
            [
                new(
                    OutputProfileKind.Hdr10Pq,
                    [
                        new(
                            "Windows Photos",
                            OutputCompatibilityEvidenceStatus.Fail,
                            OutputCompatibilityEvidenceStatus.Pass,
                            OutputCompatibilityEvidenceStatus.Pass,
                            "Artifact failed to open in Windows Photos."),
                    ]),
            ]);
        var contract = OutputProfileContract.Hdr10Pq with
        {
            IsExecutable = true,
            FidelityMode = OutputFidelityMode.HdrPreserved,
            FormatContract = CompleteHdr10Contract,
        };

        var updated = OutputValidationSessionArtifact.ApplyAllTo(contract, [complete, failed]);
        var summary = updated.EvaluateEvidence();

        var photos = Assert.Single(updated.ViewerEvidence, viewer => viewer.Name == "Windows Photos");
        Assert.Equal(OutputCompatibilityEvidenceStatus.Fail, photos.ArtifactHandlingStatus);
        Assert.False(summary.AllowsVisualMatchClaim);
        Assert.False(summary.AllowsHdrPreservedClaim);
        Assert.Contains("Windows Photos", summary.HdrPreservedGateDetail, StringComparison.OrdinalIgnoreCase);
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
            EvidencePaths: ["knowledge/evidence/session.md"],
            KnownLimitations: [],
            FollowUpIssuesOrStories: [],
            OutputProfileRecords: records)
        {
            TargetAppVersions =
            [
                new OutputValidationTargetAppVersionRecord(
                    "Windows Photos",
                    "2026.11040.12001.0"),
            ],
            TargetHdrEvidence = CompleteTargetHdrEvidence,
        };

    private static OutputViewerCompatibilityEvidence PassingHdrViewer(string name) =>
        new(
            name,
            OutputCompatibilityEvidenceStatus.Pass,
            OutputCompatibilityEvidenceStatus.Pass,
            OutputCompatibilityEvidenceStatus.Pass,
            $"Manual HDR validation passed in {name}.")
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
}
