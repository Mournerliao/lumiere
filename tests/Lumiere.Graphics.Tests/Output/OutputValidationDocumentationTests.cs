using Lumiere.App;
using Lumiere.Graphics.Output;
using Xunit;

namespace Lumiere.Graphics.Tests.Output;

public sealed class OutputValidationDocumentationTests
{
    [Fact]
    public void OutputClaimsContract_RecordsFutureFormatAcceptanceFields()
    {
        var repoRoot = LocateRepositoryRoot();
        var document = File.ReadAllText(Path.Combine(repoRoot, "knowledge", "contracts", "claims.md"));

        Assert.Contains("HDR-aware, not HDR-certified", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("exact format and extension", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("source and destination pixel formats", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("transfer function", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tone/gamut policy", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("metadata policy", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("named viewer assumptions", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Windows hardware evidence", document, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Lumiere.App.Validation.Guidance.mvp-release-evidence-template.md", "# MVP Release Evidence Template")]
    [InlineData("Lumiere.App.Validation.Guidance.hdr-validation-scenarios.md", "# HDR Validation Scenarios")]
    public void OutputValidationGuidance_IsAvailableThroughStableResourceName(
        string resourceName,
        string expectedHeading)
    {
        using var stream = typeof(FileOutputValidationArtifactSource).Assembly.GetManifestResourceStream(resourceName);

        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);
        var document = reader.ReadToEnd();

        Assert.Contains(expectedHeading, document, StringComparison.Ordinal);
    }

    [Fact]
    public void HdrSdrValidationSessionTemplate_CarriesDraftSentinel()
    {
        var repoRoot = LocateRepositoryRoot();
        var templatePath = Path.Combine(
            repoRoot,
            "src",
            "Lumiere.App.Core",
            "Validation",
            "Output",
            "hdr-sdr-validation-session-template.md");
        var template = File.ReadAllText(templatePath);

        Assert.Contains("Draft status: NOT RUN until", template, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PASS / PASS with limitation / FAIL / NOT RUN", template, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OutputValidationSessionTemplate_ParsesAsSchemaVersionFourAndDoesNotPassClaims()
    {
        var repoRoot = LocateRepositoryRoot();
        var templatePath = Path.Combine(
            repoRoot,
            "src",
            "Lumiere.App.Core",
            "Validation",
            "Output",
            "output-validation-session.schema-v4.sample.json");
        var artifact = OutputValidationSessionArtifact.FromJson(File.ReadAllText(templatePath));

        Assert.Equal(4, artifact.SchemaVersion);
        Assert.NotNull(artifact.TargetHdrEvidence);
        Assert.Contains("REPLACE_WITH_TARGET_DISPLAY_NAME", artifact.TargetHdrEvidence.TargetDisplayName);
        Assert.Equal(
            ["Microsoft Paint", "Windows Photos", "Microsoft Edge"],
            artifact.OutputProfileRecords.Single().ViewerEvidence.Select(viewer => viewer.Name).ToArray());
        Assert.Equal(["evidence\\REPLACE_WITH_SCENARIO_SESSION_RECORD.md"], artifact.EvidencePaths);
        var targetHdrEvidence = artifact.TargetHdrEvidence
            ?? throw new InvalidOperationException("Template must include target-aware HDR evidence.");
        Assert.Contains("REPLACE_WITH_OBSERVED_TARGET_HDR_STATE", targetHdrEvidence.HdrState, StringComparison.Ordinal);
        Assert.Contains(
            "REPLACE_WITH_OBSERVED_TARGET_COLOR_SPACE",
            targetHdrEvidence.ColorSpace ?? string.Empty,
            StringComparison.Ordinal);
        Assert.Contains("REPLACE_WITH_TARGET_HDR_VALIDATION_DETAIL", targetHdrEvidence.Detail, StringComparison.Ordinal);

        var updated = artifact.ApplyTo(OutputProfileContract.Hdr10Pq with
        {
            IsExecutable = true,
            FidelityMode = OutputFidelityMode.HdrPreserved,
        });
        var summary = updated.EvaluateEvidence();

        Assert.False(updated.HasCompleteFormatContract);
        Assert.Contains("Validation session incomplete", updated.ViewerEvidence[0].Detail, StringComparison.OrdinalIgnoreCase);
        Assert.False(summary.AllowsVisualMatchClaim);
        Assert.False(summary.AllowsHdrPreservedClaim);
    }

    private static string LocateRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lumiere.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root could not be located.");
    }
}
