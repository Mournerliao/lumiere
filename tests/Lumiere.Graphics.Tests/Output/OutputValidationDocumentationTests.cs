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
