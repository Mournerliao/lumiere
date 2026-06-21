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
}
