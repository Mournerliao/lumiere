using Lumiere.Graphics.Output;
using Xunit;

namespace Lumiere.Graphics.Tests.Output;

public sealed class OutputValidationDocumentationTests
{
    [Fact]
    public void OutputValidationDocs_RecordFutureFormatAcceptanceFields()
    {
        var repoRoot = LocateRepositoryRoot();
        var document = File.ReadAllText(Path.Combine(repoRoot, "docs", "validation", "output-validation.md"));

        Assert.Contains("not validated HDR-preserving output", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Format choice", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Conversion policy", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Metadata policy", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Target-app assumptions", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Windows manual validation", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("HDR10", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("P3", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sRGB", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Visible design-reference controls", document, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OutputValidationSessionTemplate_ParsesAsSchemaVersionFourAndDoesNotPassClaims()
    {
        var repoRoot = LocateRepositoryRoot();
        var templatePath = Path.Combine(
            repoRoot,
            "docs",
            "validation",
            "templates",
            "output-validation-session.schema-v4.sample.json");
        var artifact = OutputValidationSessionArtifact.FromJson(File.ReadAllText(templatePath));

        Assert.Equal(4, artifact.SchemaVersion);
        Assert.NotNull(artifact.TargetHdrEvidence);
        Assert.Contains("REPLACE_WITH_TARGET_DISPLAY_NAME", artifact.TargetHdrEvidence.TargetDisplayName);
        Assert.Equal(
            ["Microsoft Paint", "Windows Photos", "Chromium browsers"],
            artifact.OutputProfileRecords.Single().ViewerEvidence.Select(viewer => viewer.Name).ToArray());

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
