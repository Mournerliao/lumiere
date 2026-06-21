using Lumiere.App;
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
        Assert.DoesNotContain("public release target", profile.FidelityClaim.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectValidation_RequiresPublicHdrFidelityEvidenceBeforeRelease()
    {
        var validation = PerfectHdrFidelityProjection.ProjectValidation();

        Assert.Equal(PerfectHdrFidelityProjection.ReleaseTarget, validation.ReleaseTarget);
        Assert.Contains("evidence", validation.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(validation.Rows, row => row.Label == "Target-aware HDR" && row.Status == ValidationEvidenceStatus.NotRun);
        Assert.Contains(validation.Rows, row => row.Label == "Visual-match output" && row.Status == ValidationEvidenceStatus.Limited);
        Assert.Contains(validation.Rows, row => row.Label == "HDR-preserved profile" && row.Status == ValidationEvidenceStatus.NotRun);
        Assert.Contains(validation.Rows, row => row.Label == "Target app matrix" && row.Status == ValidationEvidenceStatus.NotRun);
    }
}
