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

    public bool HasArtifacts => Artifacts.Count > 0;

    public bool HasLoadIssues => LoadIssues.Count > 0;
}

public sealed record OutputValidationArtifactLoadIssue(
    string Path,
    string Detail);

public sealed class FileOutputValidationArtifactSource : IOutputValidationArtifactSource
{
    private readonly string directoryPath;
    private readonly string searchPattern;
    private readonly Func<string, bool> directoryExists;
    private readonly Func<string, string, IEnumerable<string>> enumerateFiles;
    private readonly Func<string, string> readAllText;

    public FileOutputValidationArtifactSource(string directoryPath, string searchPattern = "*.json")
        : this(
            directoryPath,
            searchPattern,
            Directory.Exists,
            Directory.EnumerateFiles,
            File.ReadAllText)
    {
    }

    public FileOutputValidationArtifactSource(
        string directoryPath,
        string searchPattern,
        Func<string, bool> directoryExists,
        Func<string, string, IEnumerable<string>> enumerateFiles,
        Func<string, string> readAllText)
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
        this.enumerateFiles = enumerateFiles ?? throw new ArgumentNullException(nameof(enumerateFiles));
        this.readAllText = readAllText ?? throw new ArgumentNullException(nameof(readAllText));
    }

    public static FileOutputValidationArtifactSource CreateDefault()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var directory = Path.Combine(localAppData, "Lumiere", "validation", "output");
        return new FileOutputValidationArtifactSource(directory);
    }

    public OutputValidationArtifactSnapshot Load()
    {
        if (!directoryExists(directoryPath))
        {
            return OutputValidationArtifactSnapshot.Empty;
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

        return new OutputValidationArtifactSnapshot(artifacts, issues);
    }
}
