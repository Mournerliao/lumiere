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
}
