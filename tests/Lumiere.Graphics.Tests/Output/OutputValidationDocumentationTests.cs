using Lumiere.Graphics.Output;
using Xunit;

namespace Lumiere.Graphics.Tests.Output;

public sealed class OutputValidationDocumentationTests
{
    [Fact]
    public void OutputValidationDocs_RecordFutureFormatAcceptanceFields()
    {
        var repoRoot = LocateRepositoryRoot();
        var document = File.ReadAllText(Path.Combine(repoRoot, "harness", "validation", "output-validation.md"));

        Assert.Contains("not validated HDR-preserving output", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Format choice", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Conversion policy", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Metadata policy", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Target-app assumptions", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Windows manual validation", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Every app-loaded artifact evidence path must resolve inside the same local validation workspace", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Draft status: NOT RUN until", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("specific repair guidance", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("repo-relative references", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("HDR10", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("P3", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sRGB", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Visible design-reference controls", document, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HdrSdrValidationSessionTemplate_CarriesDraftSentinel()
    {
        var repoRoot = LocateRepositoryRoot();
        var templatePath = Path.Combine(
            repoRoot,
            "harness",
            "validation",
            "templates",
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
            "harness",
            "validation",
            "templates",
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
