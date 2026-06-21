using Lumiere.App;
using Lumiere.Graphics.Output;
using Xunit;

namespace Lumiere.Graphics.Tests.App;

public sealed class OutputResultProjectionTests
{
    [Fact]
    public void Project_WithoutOutputShowsReadyState()
    {
        var fidelity = PerfectHdrFidelityProjection.ProjectOutputProfile("HDR10").FidelityClaim;

        var projection = OutputResultProjection.Project(null, fidelity);

        Assert.Equal("Ready", projection.Title);
        Assert.Equal("No capture output has completed yet.", projection.Detail);
        Assert.Equal(OutputResultProjectionSeverity.Neutral, projection.Severity);
        Assert.Contains("Fidelity claim: Unvalidated", projection.FidelityDetail);
    }

    [Fact]
    public void Project_ClipboardSuccessSeparatesArtifactSuccessFromFidelityClaim()
    {
        var output = OutputResult.ClipboardSuccess(1024);
        var fidelity = PerfectHdrFidelityProjection.ProjectOutputProfile("HDR10").FidelityClaim;

        var projection = OutputResultProjection.Project(output, fidelity);

        Assert.Equal("Copied", projection.Title);
        Assert.Equal("Clipboard copied", projection.Detail);
        Assert.Equal(OutputResultProjectionSeverity.Success, projection.Severity);
        Assert.Contains("Fidelity claim: Unvalidated", projection.FidelityDetail);
        Assert.DoesNotContain("HDR-preserved", projection.Title, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR-preserved", projection.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR-preserved", projection.FidelityDetail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Project_FolderSuccessShowsSavedArtifact()
    {
        var output = OutputResult.FromTargets(
            OutputTargetResult.Success(OutputTarget.Folder, "Saved", artifactPath: "C:\\Captures\\a.png"));
        var fidelity = PerfectHdrFidelityProjection.ProjectOutputProfile("sRGB").FidelityClaim;

        var projection = OutputResultProjection.Project(output, fidelity);

        Assert.Equal("Saved", projection.Title);
        Assert.Equal("File saved", projection.Detail);
        Assert.Equal(OutputResultProjectionSeverity.Success, projection.Severity);
    }

    [Fact]
    public void Project_PartialSuccessShowsWarningAndBothTargets()
    {
        var output = OutputResult.FromTargets(
            OutputTargetResult.Success(OutputTarget.Clipboard, "Copied"),
            OutputTargetResult.Failed(OutputTarget.Folder, "Folder unavailable"));
        var fidelity = PerfectHdrFidelityProjection.ProjectOutputProfile("sRGB").FidelityClaim;

        var projection = OutputResultProjection.Project(output, fidelity);

        Assert.Equal("Output partially complete", projection.Title);
        Assert.Equal(OutputResultProjectionSeverity.Warning, projection.Severity);
        Assert.Equal("Clipboard copied | Folder unavailable", projection.Detail);
    }

    [Fact]
    public void Project_FailedOutputUsesWarningSeverity()
    {
        var output = OutputResult.ClipboardFailed("Clipboard write denied.");
        var fidelity = PerfectHdrFidelityProjection.ProjectOutputProfile("sRGB").FidelityClaim;

        var projection = OutputResultProjection.Project(output, fidelity);

        Assert.Equal("Failed to copy to clipboard", projection.Title);
        Assert.Equal(OutputResultProjectionSeverity.Warning, projection.Severity);
        Assert.Equal("Failed to copy to clipboard", projection.Detail);
    }
}
