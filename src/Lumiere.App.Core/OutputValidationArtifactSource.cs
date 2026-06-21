using Lumiere.Graphics.Output;

namespace Lumiere.App;

public interface IOutputValidationArtifactSource
{
    OutputValidationArtifactSnapshot Load();
}

public sealed record OutputValidationArtifactSnapshot(
    IReadOnlyList<OutputValidationSessionArtifact> Artifacts,
    IReadOnlyList<OutputValidationArtifactLoadIssue> LoadIssues)
{
    public static OutputValidationArtifactSnapshot Empty { get; } =
        new([], []);

    public OutputValidationWorkspaceState Workspace { get; init; } =
        OutputValidationWorkspaceState.Unavailable;

    public bool HasArtifacts => Artifacts.Count > 0;

    public bool HasLoadIssues => LoadIssues.Count > 0;
}

public sealed record OutputValidationArtifactLoadIssue(
    string Path,
    string Detail);

public sealed record OutputValidationWorkspaceState(
    string DirectoryPath,
    string TemplatesDirectoryPath,
    string EvidenceDirectoryPath,
    string GuidanceFilePath,
    string? SampleTemplatePath,
    IReadOnlyList<OutputValidationWorkspaceIssue> Issues)
{
    public static OutputValidationWorkspaceState Unavailable { get; } =
        new(
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            null,
            []);

    public bool IsReady => Issues.Count == 0 && !string.IsNullOrWhiteSpace(DirectoryPath);

    public bool HasSampleTemplate => !string.IsNullOrWhiteSpace(SampleTemplatePath);

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(DirectoryPath)
        || !string.IsNullOrWhiteSpace(SampleTemplatePath)
        || Issues.Count > 0;
}

public sealed record OutputValidationWorkspaceIssue(
    string Path,
    string Detail);

public sealed class FileOutputValidationArtifactSource : IOutputValidationArtifactSource
{
    internal const string WorkspaceReadmeFileName = "README.txt";
    internal const string SampleTemplateFileName = "output-validation-session.schema-v4.sample.json";

    private readonly string directoryPath;
    private readonly string searchPattern;
    private readonly Func<string, bool> directoryExists;
    private readonly Func<string, bool> fileExists;
    private readonly Action<string> createDirectory;
    private readonly Func<string, string, IEnumerable<string>> enumerateFiles;
    private readonly Func<string, string> readAllText;
    private readonly Action<string, string> writeAllText;
    private readonly Func<string?> resolveTemplateSourceText;
    private readonly bool prepareWorkspace;

    public FileOutputValidationArtifactSource(string directoryPath, string searchPattern = "*.json")
        : this(
            directoryPath,
            searchPattern,
            Directory.Exists,
            File.Exists,
            path => Directory.CreateDirectory(path),
            Directory.EnumerateFiles,
            File.ReadAllText,
            File.WriteAllText,
            LoadEmbeddedTemplateText,
            prepareWorkspace: true)
    {
    }

    public FileOutputValidationArtifactSource(
        string directoryPath,
        string searchPattern,
        Func<string, bool> directoryExists,
        Func<string, string, IEnumerable<string>> enumerateFiles,
        Func<string, string> readAllText)
        : this(
            directoryPath,
            searchPattern,
            directoryExists,
            File.Exists,
            _ => { },
            enumerateFiles,
            readAllText,
            (_, _) => { },
            () => null,
            prepareWorkspace: false)
    {
    }

    public FileOutputValidationArtifactSource(
        string directoryPath,
        string searchPattern,
        Func<string, bool> directoryExists,
        Func<string, bool> fileExists,
        Action<string> createDirectory,
        Func<string, string, IEnumerable<string>> enumerateFiles,
        Func<string, string> readAllText,
        Action<string, string> writeAllText,
        Func<string?>? resolveTemplateSourceText = null,
        bool prepareWorkspace = false)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new ArgumentException("Validation artifact directory path must be provided.", nameof(directoryPath));
        }

        if (string.IsNullOrWhiteSpace(searchPattern))
        {
            throw new ArgumentException("Validation artifact search pattern must be provided.", nameof(searchPattern));
        }

        this.directoryPath = directoryPath.Trim();
        this.searchPattern = searchPattern.Trim();
        this.directoryExists = directoryExists ?? throw new ArgumentNullException(nameof(directoryExists));
        this.fileExists = fileExists ?? throw new ArgumentNullException(nameof(fileExists));
        this.createDirectory = createDirectory ?? throw new ArgumentNullException(nameof(createDirectory));
        this.enumerateFiles = enumerateFiles ?? throw new ArgumentNullException(nameof(enumerateFiles));
        this.readAllText = readAllText ?? throw new ArgumentNullException(nameof(readAllText));
        this.writeAllText = writeAllText ?? throw new ArgumentNullException(nameof(writeAllText));
        this.resolveTemplateSourceText = resolveTemplateSourceText ?? LoadEmbeddedTemplateText;
        this.prepareWorkspace = prepareWorkspace;
    }

    public static FileOutputValidationArtifactSource CreateDefault()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var directory = Path.Combine(localAppData, "Lumiere", "validation", "output");
        return new FileOutputValidationArtifactSource(directory);
    }

    public OutputValidationArtifactSnapshot Load()
    {
        var workspace = prepareWorkspace
            ? EnsureWorkspace()
            : OutputValidationWorkspaceState.Unavailable;
        if (prepareWorkspace && !workspace.IsReady)
        {
            return OutputValidationArtifactSnapshot.Empty with
            {
                Workspace = workspace,
            };
        }

        if (!directoryExists(directoryPath))
        {
            return OutputValidationArtifactSnapshot.Empty with
            {
                Workspace = workspace,
            };
        }

        var artifacts = new List<OutputValidationSessionArtifact>();
        var issues = new List<OutputValidationArtifactLoadIssue>();
        foreach (var path in enumerateFiles(directoryPath, searchPattern)
            .Order(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                artifacts.Add(OutputValidationSessionArtifact.FromJson(readAllText(path)));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or System.Text.Json.JsonException)
            {
                issues.Add(new OutputValidationArtifactLoadIssue(path, $"{ex.GetType().Name}: {ex.Message}"));
            }
        }

        return new OutputValidationArtifactSnapshot(artifacts, issues)
        {
            Workspace = workspace,
        };
    }

    private OutputValidationWorkspaceState EnsureWorkspace()
    {
        var templatesDirectoryPath = Path.Combine(directoryPath, "templates");
        var evidenceDirectoryPath = Path.Combine(directoryPath, "evidence");
        var guidanceFilePath = Path.Combine(directoryPath, WorkspaceReadmeFileName);
        var sampleTemplatePath = Path.Combine(templatesDirectoryPath, SampleTemplateFileName);
        var issues = new List<OutputValidationWorkspaceIssue>();

        EnsureDirectory(directoryPath, "Validation artifact directory could not be prepared.", issues);
        EnsureDirectory(templatesDirectoryPath, "Template directory could not be prepared.", issues);
        EnsureDirectory(evidenceDirectoryPath, "Evidence directory could not be prepared.", issues);

        if (issues.Count == 0)
        {
            EnsureGuidanceFile(guidanceFilePath, issues);
            EnsureSampleTemplate(sampleTemplatePath, issues);
        }

        return new OutputValidationWorkspaceState(
            directoryPath,
            templatesDirectoryPath,
            evidenceDirectoryPath,
            guidanceFilePath,
            fileExists(sampleTemplatePath) ? sampleTemplatePath : null,
            issues);
    }

    private void EnsureDirectory(
        string path,
        string failurePrefix,
        ICollection<OutputValidationWorkspaceIssue> issues)
    {
        if (directoryExists(path))
        {
            return;
        }

        try
        {
            createDirectory(path);
            if (!directoryExists(path))
            {
                issues.Add(new OutputValidationWorkspaceIssue(path, failurePrefix));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            issues.Add(new OutputValidationWorkspaceIssue(path, $"{failurePrefix} {ex.GetType().Name}: {ex.Message}"));
        }
    }

    private void EnsureGuidanceFile(
        string guidanceFilePath,
        ICollection<OutputValidationWorkspaceIssue> issues)
    {
        if (fileExists(guidanceFilePath))
        {
            return;
        }

        try
        {
            writeAllText(guidanceFilePath, CreateWorkspaceGuidance());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            issues.Add(new OutputValidationWorkspaceIssue(
                guidanceFilePath,
                $"Validation workspace guidance file could not be written. {ex.GetType().Name}: {ex.Message}"));
        }
    }

    private void EnsureSampleTemplate(
        string sampleTemplatePath,
        ICollection<OutputValidationWorkspaceIssue> issues)
    {
        if (fileExists(sampleTemplatePath))
        {
            return;
        }

        var templateContent = resolveTemplateSourceText();
        if (string.IsNullOrWhiteSpace(templateContent))
        {
            issues.Add(new OutputValidationWorkspaceIssue(
                sampleTemplatePath,
                "Validation sample template source could not be loaded from the current build."));
            return;
        }

        try
        {
            writeAllText(sampleTemplatePath, templateContent);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            issues.Add(new OutputValidationWorkspaceIssue(
                sampleTemplatePath,
                $"Validation sample template could not be seeded. {ex.GetType().Name}: {ex.Message}"));
        }
    }

    private static string? LoadEmbeddedTemplateText()
    {
        const string resourceName = "Lumiere.App.Validation.Output.output-validation-session.schema-v4.sample.json";
        using var stream = typeof(FileOutputValidationArtifactSource).Assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string CreateWorkspaceGuidance() =>
        string.Join(
            Environment.NewLine,
            [
                "Lumiere output validation workspace",
                string.Empty,
                "Purpose:",
                "- Store real Windows manual output validation artifacts for the current machine.",
                "- Keep draft templates under templates\\ so the runtime loader does not count them as evidence.",
                "- Save supporting notes, screenshots, or logs under evidence\\ as needed.",
                string.Empty,
                "Workflow:",
                "1. Copy templates\\output-validation-session.schema-v4.sample.json into this output\\ folder.",
                "2. Rename it for the session, replace every REPLACE_WITH_* placeholder, and keep viewer evidence honest.",
                "3. Restart Lumiere or reopen the settings validation panel to reload the artifact.",
                "4. Do not treat template files or incomplete sessions as passing release evidence.",
                string.Empty,
                "Reference docs:",
                "- harness/validation/output-validation.md",
                "- harness/validation/release-validation-checklist.md"]);
}
