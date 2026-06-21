using Lumiere.Graphics.Output;
using Xunit;

namespace Lumiere.Graphics.Tests.Output;

public sealed class OutputProfileContractTests
{
    [Fact]
    public void SrgbCompatibilityProfileNamesRequiredViewerEvidence()
    {
        var contract = OutputProfileContract.SrgbCompatibilityPng;

        Assert.Equal(["Microsoft Paint", "Windows Photos", "Chromium browsers"], contract.ViewerEvidence.Select(viewer => viewer.Name).ToArray());
        Assert.All(
            contract.ViewerEvidence,
            viewer =>
            {
                Assert.Equal(OutputCompatibilityEvidenceStatus.NotRun, viewer.ArtifactHandlingStatus);
                Assert.Equal(OutputCompatibilityEvidenceStatus.NotRun, viewer.VisualMatchStatus);
                Assert.Equal(OutputCompatibilityEvidenceStatus.NotApplicable, viewer.HdrPreservationStatus);
                Assert.Contains("artifact", viewer.Detail, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("visual", viewer.Detail, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("HDR-preserved", viewer.Detail, StringComparison.OrdinalIgnoreCase);
            });
    }

    [Fact]
    public void SrgbCompatibilityProfileHasCompleteTypedOutputContract()
    {
        var contract = OutputProfileContract.SrgbCompatibilityPng;

        Assert.True(contract.HasCompleteFormatContract);
        Assert.Equal(OutputPixelFormat.Rgba8UnsignedNormalized, contract.FormatContract.DestinationPixelFormat);
        Assert.Equal(OutputTransferFunction.Srgb, contract.FormatContract.TransferFunction);
        Assert.Equal(OutputColorPrimaries.Bt709, contract.FormatContract.ColorPrimaries);
        Assert.Equal(OutputMetadataPolicy.NoHdrMetadata, contract.FormatContract.MetadataPolicy);
        Assert.Equal(OutputConversionPolicy.SdrToneMapped, contract.FormatContract.ConversionPolicy);
        Assert.Equal(OutputTargetAppAssumption.CompatibilityFirst, contract.FormatContract.TargetAppAssumption);
    }

    [Fact]
    public void Hdr10ProfileRequiresHdrPreservationEvidenceBeforeClaim()
    {
        var contract = OutputProfileContract.Hdr10Pq;

        Assert.False(contract.IsExecutable);
        Assert.False(contract.AllowsHdrPreservedClaim);
        Assert.All(
            contract.ViewerEvidence,
            viewer =>
            {
                Assert.Equal(OutputCompatibilityEvidenceStatus.NotRun, viewer.ArtifactHandlingStatus);
                Assert.Equal(OutputCompatibilityEvidenceStatus.NotRun, viewer.VisualMatchStatus);
                Assert.Equal(OutputCompatibilityEvidenceStatus.NotRun, viewer.HdrPreservationStatus);
                Assert.Contains("HDR preservation", viewer.Detail, StringComparison.OrdinalIgnoreCase);
            });
    }

    [Fact]
    public void Hdr10ProfileRemainsValidationScopedUntilTypedOutputContractIsComplete()
    {
        var contract = OutputProfileContract.Hdr10Pq;

        Assert.False(contract.HasCompleteFormatContract);
        Assert.Equal(OutputPixelFormat.R16G16B16A16Float, contract.FormatContract.SourcePixelFormat);
        Assert.Equal(OutputPixelFormat.NotDefined, contract.FormatContract.DestinationPixelFormat);
        Assert.Equal(OutputTransferFunction.NotDefined, contract.FormatContract.TransferFunction);
        Assert.Equal(OutputColorPrimaries.NotDefined, contract.FormatContract.ColorPrimaries);
        Assert.Equal(OutputMetadataPolicy.RequiredButUndefined, contract.FormatContract.MetadataPolicy);
        Assert.Equal(OutputConversionPolicy.RequiredButUndefined, contract.FormatContract.ConversionPolicy);
        Assert.Equal(OutputTargetAppAssumption.RequiresHdrViewerValidation, contract.FormatContract.TargetAppAssumption);
    }

    [Fact]
    public void EvidenceSummary_DoesNotAllowClaimsWhenViewerEvidenceIsMissing()
    {
        var summary = OutputProfileContract.Hdr10Pq.EvaluateEvidence();

        Assert.False(summary.AllowsVisualMatchClaim);
        Assert.False(summary.AllowsHdrPreservedClaim);
        Assert.Contains("NOT RUN", summary.VisualMatchGateDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("HDR preservation", summary.HdrPreservedGateDetail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EvidenceSummary_AllowsVisualMatchOnlyWhenArtifactAndVisualEvidencePass()
    {
        var contract = OutputProfileContract.SrgbCompatibilityPng with
        {
            ViewerEvidence =
            [
                PassingSdrViewer("Microsoft Paint"),
                PassingSdrViewer("Windows Photos"),
                PassingSdrViewer("Chromium browsers"),
            ],
        };

        var summary = contract.EvaluateEvidence();

        Assert.True(summary.AllowsVisualMatchClaim);
        Assert.False(summary.AllowsHdrPreservedClaim);
        Assert.Contains("visual-match evidence passed", summary.VisualMatchGateDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not an HDR-preserved profile", summary.HdrPreservedGateDetail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EvidenceSummary_AllowsHdrPreservedOnlyWhenAllHdrEvidencePasses()
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
                PassingHdrViewer("Chromium browsers"),
            ],
        };

        var summary = contract.EvaluateEvidence();

        Assert.True(summary.AllowsVisualMatchClaim);
        Assert.True(summary.AllowsHdrPreservedClaim);
        Assert.Contains("HDR preservation evidence passed", summary.HdrPreservedGateDetail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EvidenceSummary_BlocksHdrPreservedWhenFormatContractIsIncomplete()
    {
        var contract = OutputProfileContract.Hdr10Pq with
        {
            IsExecutable = true,
            FidelityMode = OutputFidelityMode.HdrPreserved,
            ViewerEvidence =
            [
                PassingHdrViewer("Microsoft Paint"),
                PassingHdrViewer("Windows Photos"),
                PassingHdrViewer("Chromium browsers"),
            ],
        };

        var summary = contract.EvaluateEvidence();

        Assert.False(contract.HasCompleteFormatContract);
        Assert.False(summary.AllowsHdrPreservedClaim);
        Assert.Contains("format contract", summary.HdrPreservedGateDetail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplyValidationRecord_UpdatesNamedViewerEvidenceWithoutPromotingMissingViewers()
    {
        var record = new OutputProfileValidationRecord(
            OutputProfileKind.Hdr10Pq,
            [
                new(
                    "Windows Photos",
                    OutputCompatibilityEvidenceStatus.Pass,
                    OutputCompatibilityEvidenceStatus.Pass,
                    OutputCompatibilityEvidenceStatus.Pass,
                    "Validated on HDR display with Windows Photos."),
            ]);
        var contract = OutputProfileContract.Hdr10Pq with
        {
            IsExecutable = true,
            FidelityMode = OutputFidelityMode.HdrPreserved,
            FormatContract = CompleteHdr10Contract,
        };

        var updated = contract.ApplyValidationRecord(record);

        var photos = Assert.Single(updated.ViewerEvidence, viewer => viewer.Name == "Windows Photos");
        Assert.Equal(OutputCompatibilityEvidenceStatus.Pass, photos.ArtifactHandlingStatus);
        Assert.Equal(OutputCompatibilityEvidenceStatus.Pass, photos.VisualMatchStatus);
        Assert.Equal(OutputCompatibilityEvidenceStatus.Pass, photos.HdrPreservationStatus);
        var summary = updated.EvaluateEvidence();
        Assert.False(summary.AllowsHdrPreservedClaim);
        Assert.Contains("Microsoft Paint", summary.HdrPreservedGateDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Chromium browsers", summary.HdrPreservedGateDetail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Windows Photos", summary.HdrPreservedGateDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(updated.ViewerEvidence, viewer =>
            viewer.Name == "Microsoft Paint"
            && viewer.ArtifactHandlingStatus == OutputCompatibilityEvidenceStatus.NotRun);
    }

    [Fact]
    public void ApplyValidationRecord_IgnoresEvidenceForDifferentOutputProfile()
    {
        var record = new OutputProfileValidationRecord(
            OutputProfileKind.DisplayP3,
            [
                PassingHdrViewer("Windows Photos"),
            ]);

        var updated = OutputProfileContract.Hdr10Pq.ApplyValidationRecord(record);

        Assert.Equal(OutputProfileContract.Hdr10Pq.ViewerEvidence, updated.ViewerEvidence);
        Assert.False(updated.EvaluateEvidence().AllowsVisualMatchClaim);
        Assert.False(updated.EvaluateEvidence().AllowsHdrPreservedClaim);
    }

    [Fact]
    public void ApplyValidationRecord_TreatsAutomatedEvidenceAsLimitedUntilWindowsManualValidationExists()
    {
        var record = new OutputProfileValidationRecord(
            OutputProfileKind.SrgbCompatibilityPng,
            [
                PassingSdrViewer("Microsoft Paint"),
                PassingSdrViewer("Windows Photos"),
                PassingSdrViewer("Chromium browsers"),
            ])
        {
            EvidenceSource = OutputValidationEvidenceSource.Automated,
        };

        var updated = OutputProfileContract.SrgbCompatibilityPng.ApplyValidationRecord(record);

        Assert.All(
            updated.ViewerEvidence,
            viewer =>
            {
                Assert.Equal(OutputCompatibilityEvidenceStatus.Limited, viewer.ArtifactHandlingStatus);
                Assert.Equal(OutputCompatibilityEvidenceStatus.Limited, viewer.VisualMatchStatus);
                Assert.Equal(OutputCompatibilityEvidenceStatus.NotApplicable, viewer.HdrPreservationStatus);
                Assert.Contains("Windows manual validation", viewer.Detail, StringComparison.OrdinalIgnoreCase);
            });
        Assert.False(updated.EvaluateEvidence().AllowsVisualMatchClaim);
    }

    private static OutputViewerCompatibilityEvidence PassingSdrViewer(string name) =>
        new(
            name,
            OutputCompatibilityEvidenceStatus.Pass,
            OutputCompatibilityEvidenceStatus.Pass,
            OutputCompatibilityEvidenceStatus.NotApplicable,
            "Validated compatibility viewer.");

    private static OutputViewerCompatibilityEvidence PassingHdrViewer(string name) =>
        new(
            name,
            OutputCompatibilityEvidenceStatus.Pass,
            OutputCompatibilityEvidenceStatus.Pass,
            OutputCompatibilityEvidenceStatus.Pass,
            "Validated HDR viewer.");

    private static OutputFormatContract CompleteHdr10Contract { get; } =
        new(
            OutputPixelFormat.R16G16B16A16Float,
            OutputPixelFormat.R16G16B16A16Float,
            OutputTransferFunction.PqSt2084,
            OutputColorPrimaries.Bt2020,
            OutputConversionPolicy.PreserveHdrWithDefinedToneMapping,
            OutputMetadataPolicy.AttachHdr10StaticMetadata,
            OutputTargetAppAssumption.RequiresHdrViewerValidation);
}
