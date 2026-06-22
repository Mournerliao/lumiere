using Lumiere.Graphics.Output;

namespace Lumiere.App;

public interface IOutputValidationArtifactSource
{
    OutputValidationArtifactSnapshot Load();

    OutputValidationDraftResult CreateDraft(OutputValidationDraftRequest request);
}

public sealed record OutputValidationArtifactSnapshot(
    IReadOnlyList<OutputValidationSessionArtifact> Artifacts,
    IReadOnlyList<OutputValidationArtifactLoadIssue> LoadIssues)
{
    public static OutputValidationArtifactSnapshot Empty { get; } =
        new([], []);

    public IReadOnlyList<OutputValidationArtifactReference> ArtifactReferences { get; init; } =
        [];

    public OutputValidationWorkspaceState Workspace { get; init; } =
        OutputValidationWorkspaceState.Unavailable;

    public bool HasArtifacts => Artifacts.Count > 0;

    public bool HasLoadIssues => LoadIssues.Count > 0;
}

public sealed record OutputValidationArtifactReference(
    string Path,
    OutputValidationSessionArtifact Artifact);

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

public sealed record OutputValidationDraftRequest(
    string? BuildVersion,
    OutputTarget OutputTarget,
    OutputProfileContract RequestedProfile,
    Lumiere.Capture.CaptureSessionState SessionState,
    OutputValidationCurrentSessionHint? CurrentSessionHint = null);

public sealed record OutputValidationCurrentSessionHint(
    string? Gpu,
    IReadOnlyList<string> DpiScales,
    string? DisplaySetup = null);

public sealed record OutputValidationDraftSeed(
    string? Tester,
    string? WindowsVersion,
    string? Device,
    string? Gpu,
    string? DisplaySetup,
    IReadOnlyList<string> DpiScales,
    IReadOnlyList<string> EntryPointsTested);

public sealed record OutputValidationDraftResult(
    bool IsSuccess,
    string? DraftPath,
    string? TechnicalDetail)
{
    public static OutputValidationDraftResult Success(string draftPath) =>
        new(true, draftPath, null);

    public static OutputValidationDraftResult Failed(string technicalDetail) =>
        new(false, null, technicalDetail);
}

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
    private readonly Func<DateTimeOffset> getNow;
    private readonly ITargetAppVersionPrefillProvider? targetAppVersionPrefillProvider;
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
            () => DateTimeOffset.Now,
            new WindowsTargetAppVersionPrefillProvider(),
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
            () => DateTimeOffset.Now,
            null,
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
        Func<DateTimeOffset>? getNow = null,
        ITargetAppVersionPrefillProvider? targetAppVersionPrefillProvider = null,
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
        this.getNow = getNow ?? (() => DateTimeOffset.Now);
        this.targetAppVersionPrefillProvider = targetAppVersionPrefillProvider;
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

        return LoadArtifactsFromWorkspace(workspace);
    }

    public OutputValidationDraftResult CreateDraft(OutputValidationDraftRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var workspace = prepareWorkspace
            ? EnsureWorkspace()
            : OutputValidationWorkspaceState.Unavailable;
        if (!workspace.IsReady)
        {
            var detail = workspace.Issues.Count == 0
                ? "Validation workspace is not ready on this machine."
                : string.Join(" ", workspace.Issues.Select(issue => issue.Detail));
            return OutputValidationDraftResult.Failed(detail);
        }

        try
        {
            var snapshot = LoadArtifactsFromWorkspace(workspace);
            var now = getNow();
            var draft = OutputValidationDraftFactory.Create(
                request,
                now,
                targetAppVersionPrefillProvider,
                SelectDraftSeed(snapshot.Artifacts, request));
            var filePath = AllocateDraftPath(workspace.DirectoryPath, draft.FileNameStem);
            writeAllText(filePath, draft.Artifact.ToJson());
            return OutputValidationDraftResult.Success(filePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            return OutputValidationDraftResult.Failed($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private OutputValidationArtifactSnapshot LoadArtifactsFromWorkspace(OutputValidationWorkspaceState workspace)
    {
        if (!directoryExists(directoryPath))
        {
            return OutputValidationArtifactSnapshot.Empty with
            {
                Workspace = workspace,
            };
        }

        var artifacts = new List<OutputValidationSessionArtifact>();
        var artifactReferences = new List<OutputValidationArtifactReference>();
        var issues = new List<OutputValidationArtifactLoadIssue>();
        foreach (var path in enumerateFiles(directoryPath, searchPattern)
            .Order(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var artifact = OutputValidationSessionArtifact.FromJson(readAllText(path));
                artifacts.Add(artifact);
                artifactReferences.Add(new OutputValidationArtifactReference(path, artifact));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or System.Text.Json.JsonException)
            {
                issues.Add(new OutputValidationArtifactLoadIssue(path, $"{ex.GetType().Name}: {ex.Message}"));
            }
        }

        return new OutputValidationArtifactSnapshot(artifacts, issues)
        {
            ArtifactReferences = artifactReferences,
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
                "2. Or use Lumiere's Create draft action to generate a prefilled local draft in this folder.",
                "3. Rename it for the session if needed, replace every REPLACE_WITH_* placeholder, and keep viewer evidence honest.",
                "4. Reload evidence from Lumiere after recording real observations.",
                "5. Do not treat template files or incomplete sessions as passing release evidence.",
                string.Empty,
                "Reference docs:",
                "- harness/validation/output-validation.md",
                "- harness/validation/release-validation-checklist.md"]);

    private string AllocateDraftPath(string workspaceDirectoryPath, string fileNameStem)
    {
        var candidate = Path.Combine(workspaceDirectoryPath, $"{fileNameStem}.json");
        if (!fileExists(candidate))
        {
            return candidate;
        }

        for (var suffix = 2; suffix < 1000; suffix++)
        {
            candidate = Path.Combine(workspaceDirectoryPath, $"{fileNameStem}-{suffix}.json");
            if (!fileExists(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Could not allocate a unique validation draft file name.");
    }

    private static OutputValidationDraftSeed? SelectDraftSeed(
        IEnumerable<OutputValidationSessionArtifact> artifacts,
        OutputValidationDraftRequest request)
    {
        ArgumentNullException.ThrowIfNull(artifacts);
        ArgumentNullException.ThrowIfNull(request);

        var selected = artifacts
            .OrderByDescending(artifact => ScoreSeedCompatibility(artifact, request))
            .ThenByDescending(artifact => artifact.Date, StringComparer.Ordinal)
            .FirstOrDefault();

        return selected is null
            ? null
            : new OutputValidationDraftSeed(
                selected.Tester,
                selected.WindowsVersion,
                selected.Device,
                selected.Gpu,
                selected.DisplaySetup,
                selected.DpiScales,
                selected.EntryPointsTested);
    }

    private static int ScoreSeedCompatibility(
        OutputValidationSessionArtifact artifact,
        OutputValidationDraftRequest request)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(request);

        var score = 0;
        if (artifact.CoversOutputTarget(request.OutputTarget))
        {
            score += 2;
        }

        if (artifact.OutputProfileRecords.Any(record => record.ProfileKind == request.RequestedProfile.Kind))
        {
            score += 2;
        }

        return score;
    }
}
