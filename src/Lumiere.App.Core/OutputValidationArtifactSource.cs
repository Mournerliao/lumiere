using Lumiere.Graphics.Output;

namespace Lumiere.App;

public interface IOutputValidationArtifactSource
{
    OutputValidationArtifactSnapshot Load();

    OutputValidationDraftResult CreateDraft(OutputValidationDraftRequest request);

    OutputValidationDraftResult CreateResourceTrendDraft(ResourceTrendValidationDraftRequest request);
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
    string GuidanceDirectoryPath,
    string EvidenceDirectoryPath,
    string GuidanceFilePath,
    string? SampleTemplatePath,
    string? ReleaseChecklistPath,
    string? HdrSdrScenariosPath,
    string? SettingsAccessibilityGuidePath,
    string? ResourceTrendTemplatePath,
    string? ResourceTrendScriptPath,
    IReadOnlyList<OutputValidationWorkspaceIssue> Issues)
{
    public static OutputValidationWorkspaceState Unavailable { get; } =
        new(
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            null,
            null,
            null,
            null,
            null,
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
    IReadOnlyList<string> EntryPointsTested)
{
    public IReadOnlyList<string> SuggestedDisplayTopologies { get; init; } = [];

    public IReadOnlyList<string> SuggestedEntryPoints { get; init; } = [];

    public IReadOnlyList<string> SuggestedOutputTargets { get; init; } = [];

    public IReadOnlyList<string> SuggestedViewerTargets { get; init; } = [];
}

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

public sealed record ResourceTrendValidationDraftRequest(
    string? BuildVersion,
    OutputTarget OutputTarget,
    Lumiere.Capture.CaptureSessionState SessionState,
    int ProcessId,
    string? ResourceTrendCommand = null,
    OutputValidationCurrentSessionHint? CurrentSessionHint = null);

public sealed class FileOutputValidationArtifactSource : IOutputValidationArtifactSource
{
    internal const string WorkspaceReadmeFileName = "README.txt";
    internal const string SampleTemplateFileName = "output-validation-session.schema-v4.sample.json";
    internal const string HdrSdrSessionTemplateFileName = "hdr-sdr-validation-session-template.md";
    internal const string ReleaseChecklistFileName = "release-validation-checklist.md";
    internal const string HdrSdrScenariosFileName = "hdr-sdr-validation-scenarios.md";
    internal const string SettingsAccessibilityGuideFileName = "settings-accessibility-validation.md";
    internal const string ResourceTrendTemplateFileName = "resource-trend-session-template.md";
    internal const string ResourceTrendScriptFileName = "collect-resource-trend-samples.ps1";

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
            var scenarioNotesPath = AllocateScenarioNotesPath(
                workspace.EvidenceDirectoryPath,
                Path.GetFileNameWithoutExtension(filePath));
            var scenarioNotesRelativePath = Path.Combine("evidence", Path.GetFileName(scenarioNotesPath));
            var artifact = draft.Artifact with
            {
                EvidencePaths =
                [
                    scenarioNotesRelativePath,
                ],
            };
            var scenarioTemplatePath = Path.Combine(workspace.TemplatesDirectoryPath, HdrSdrSessionTemplateFileName);
            var scenarioTemplate = readAllText(scenarioTemplatePath);
            writeAllText(
                scenarioNotesPath,
                ScenarioValidationDraftFactory.Create(
                    scenarioTemplate,
                    artifact,
                    Path.GetFileName(filePath)));
            writeAllText(filePath, artifact.ToJson());
            return OutputValidationDraftResult.Success(filePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            return OutputValidationDraftResult.Failed($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    public OutputValidationDraftResult CreateResourceTrendDraft(ResourceTrendValidationDraftRequest request)
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

        if (string.IsNullOrWhiteSpace(workspace.ResourceTrendTemplatePath)
            || !fileExists(workspace.ResourceTrendTemplatePath))
        {
            return OutputValidationDraftResult.Failed("Resource trend session template is not available in the local validation workspace.");
        }

        try
        {
            var template = readAllText(workspace.ResourceTrendTemplatePath);
            var now = getNow();
            var content = ResourceTrendValidationDraftFactory.Create(
                request,
                workspace.DirectoryPath,
                now,
                template,
                SelectLatestResourceTrendSummary(workspace, request.ProcessId));
            var filePath = AllocateResourceTrendDraftPath(workspace.DirectoryPath, now);
            writeAllText(filePath, content);
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
                var evidenceIssues = ValidateWorkspaceLocalEvidencePaths(path, artifact, workspace).ToArray();
                if (evidenceIssues.Length > 0)
                {
                    issues.AddRange(evidenceIssues);
                    continue;
                }

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

    private IEnumerable<OutputValidationArtifactLoadIssue> ValidateWorkspaceLocalEvidencePaths(
        string artifactPath,
        OutputValidationSessionArtifact artifact,
        OutputValidationWorkspaceState workspace)
    {
        if (!workspace.IsReady)
        {
            yield break;
        }

        foreach (var evidencePath in artifact.EvidencePaths)
        {
            if (!TryResolveWorkspaceLocalEvidencePath(workspace, evidencePath, out var resolvedPath, out var issueDetail))
            {
                if (!string.IsNullOrWhiteSpace(issueDetail))
                {
                    yield return new OutputValidationArtifactLoadIssue(artifactPath, issueDetail);
                }

                continue;
            }

            if (!fileExists(resolvedPath))
            {
                yield return new OutputValidationArtifactLoadIssue(
                    artifactPath,
                    $"Workspace-local evidence path is missing: {evidencePath}");
                continue;
            }

            if (IsMarkdownEvidencePath(resolvedPath))
            {
                if (!TryReadWorkspaceLocalMarkdownEvidence(evidencePath, resolvedPath, out var content, out var readIssueDetail))
                {
                    if (!string.IsNullOrWhiteSpace(readIssueDetail))
                    {
                        yield return new OutputValidationArtifactLoadIssue(artifactPath, readIssueDetail);
                    }

                    continue;
                }

                if (IsIncompleteMarkdownEvidence(content))
                {
                    yield return new OutputValidationArtifactLoadIssue(
                        artifactPath,
                        $"Workspace-local markdown evidence is incomplete: {evidencePath}");
                }
            }
        }
    }

    private bool TryReadWorkspaceLocalMarkdownEvidence(
        string evidencePath,
        string resolvedPath,
        out string content,
        out string? issueDetail)
    {
        content = string.Empty;
        issueDetail = null;

        try
        {
            content = readAllText(resolvedPath);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            issueDetail = $"Workspace-local markdown evidence could not be read: {evidencePath}. {ex.GetType().Name}: {ex.Message}";
            return false;
        }
    }

    private static bool TryResolveWorkspaceLocalEvidencePath(
        OutputValidationWorkspaceState workspace,
        string? evidencePath,
        out string resolvedPath,
        out string? issueDetail)
    {
        resolvedPath = string.Empty;
        issueDetail = null;

        var trimmed = evidencePath?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return false;
        }

        try
        {
            var candidate = Path.IsPathFullyQualified(trimmed)
                ? Path.GetFullPath(trimmed)
                : Path.GetFullPath(Path.Combine(workspace.DirectoryPath, trimmed));
            var evidenceDirectory = Path.GetFullPath(workspace.EvidenceDirectoryPath);
            if (!IsPathInsideDirectory(candidate, evidenceDirectory))
            {
                issueDetail = $"Evidence path must stay inside the local validation workspace evidence directory: {trimmed}";
                return false;
            }

            if (!Path.IsPathFullyQualified(trimmed)
                && !StartsWithEvidenceSegment(trimmed))
            {
                issueDetail = $"Evidence path must be workspace-local and start with evidence\\: {trimmed}";
                return false;
            }

            resolvedPath = candidate;
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            issueDetail = $"Workspace-local evidence path is invalid: {trimmed}. {ex.GetType().Name}: {ex.Message}";
            return false;
        }
    }

    private static bool IsPathInsideDirectory(string candidatePath, string directoryPath)
    {
        var directoryWithSeparator = Path.EndsInDirectorySeparator(directoryPath)
            ? directoryPath
            : directoryPath + Path.DirectorySeparatorChar;
        return candidatePath.Equals(directoryPath, StringComparison.OrdinalIgnoreCase)
            || candidatePath.StartsWith(directoryWithSeparator, StringComparison.OrdinalIgnoreCase);
    }

    private static bool StartsWithEvidenceSegment(string path)
    {
        var normalized = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        return normalized.StartsWith($"evidence{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMarkdownEvidencePath(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".md", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".markdown", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsIncompleteMarkdownEvidence(string content) =>
        string.IsNullOrWhiteSpace(content)
        || content.Contains("REPLACE_WITH_", StringComparison.OrdinalIgnoreCase)
        || content.Contains("Template only", StringComparison.OrdinalIgnoreCase);

    private OutputValidationWorkspaceState EnsureWorkspace()
    {
        var templatesDirectoryPath = Path.Combine(directoryPath, "templates");
        var guidanceDirectoryPath = Path.Combine(directoryPath, "guidance");
        var evidenceDirectoryPath = Path.Combine(directoryPath, "evidence");
        var guidanceFilePath = Path.Combine(directoryPath, WorkspaceReadmeFileName);
        var sampleTemplatePath = Path.Combine(templatesDirectoryPath, SampleTemplateFileName);
        var hdrSdrSessionTemplatePath = Path.Combine(templatesDirectoryPath, HdrSdrSessionTemplateFileName);
        var releaseChecklistPath = Path.Combine(guidanceDirectoryPath, ReleaseChecklistFileName);
        var hdrSdrScenariosPath = Path.Combine(guidanceDirectoryPath, HdrSdrScenariosFileName);
        var settingsAccessibilityGuidePath = Path.Combine(guidanceDirectoryPath, SettingsAccessibilityGuideFileName);
        var resourceTrendTemplatePath = Path.Combine(templatesDirectoryPath, ResourceTrendTemplateFileName);
        var resourceTrendScriptPath = Path.Combine(directoryPath, ResourceTrendScriptFileName);
        var issues = new List<OutputValidationWorkspaceIssue>();

        EnsureDirectory(directoryPath, "Validation artifact directory could not be prepared.", issues);
        EnsureDirectory(templatesDirectoryPath, "Template directory could not be prepared.", issues);
        EnsureDirectory(guidanceDirectoryPath, "Guidance directory could not be prepared.", issues);
        EnsureDirectory(evidenceDirectoryPath, "Evidence directory could not be prepared.", issues);

        if (issues.Count == 0)
        {
            EnsureGuidanceFile(guidanceFilePath, issues);
            EnsureSampleTemplate(sampleTemplatePath, issues);
            EnsureSeededTemplate(
                hdrSdrSessionTemplatePath,
                "Lumiere.App.Validation.Output.hdr-sdr-validation-session-template.md",
                "HDR/SDR validation session template source could not be loaded from the current build.",
                "HDR/SDR validation session template could not be seeded.",
                issues);
            EnsureSeededGuidance(
                releaseChecklistPath,
                "Lumiere.App.Validation.Guidance.release-validation-checklist.md",
                "Release validation checklist source could not be loaded from the current build.",
                "Release validation checklist could not be seeded.",
                issues);
            EnsureSeededGuidance(
                hdrSdrScenariosPath,
                "Lumiere.App.Validation.Guidance.hdr-sdr-validation-scenarios.md",
                "HDR/SDR validation scenarios source could not be loaded from the current build.",
                "HDR/SDR validation scenarios could not be seeded.",
                issues);
            EnsureSeededGuidance(
                settingsAccessibilityGuidePath,
                "Lumiere.App.Validation.Guidance.settings-accessibility-validation.md",
                "Settings accessibility validation guide source could not be loaded from the current build.",
                "Settings accessibility validation guide could not be seeded.",
                issues);
            EnsureResourceTrendTemplate(resourceTrendTemplatePath, issues);
            EnsureResourceTrendScript(resourceTrendScriptPath, issues);
        }

        return new OutputValidationWorkspaceState(
            directoryPath,
            templatesDirectoryPath,
            guidanceDirectoryPath,
            evidenceDirectoryPath,
            guidanceFilePath,
            fileExists(sampleTemplatePath) ? sampleTemplatePath : null,
            fileExists(releaseChecklistPath) ? releaseChecklistPath : null,
            fileExists(hdrSdrScenariosPath) ? hdrSdrScenariosPath : null,
            fileExists(settingsAccessibilityGuidePath) ? settingsAccessibilityGuidePath : null,
            fileExists(resourceTrendTemplatePath) ? resourceTrendTemplatePath : null,
            fileExists(resourceTrendScriptPath) ? resourceTrendScriptPath : null,
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

    private void EnsureResourceTrendTemplate(
        string resourceTrendTemplatePath,
        ICollection<OutputValidationWorkspaceIssue> issues)
    {
        if (fileExists(resourceTrendTemplatePath))
        {
            return;
        }

        var templateContent = LoadEmbeddedText("Lumiere.App.Validation.ResourceTrend.resource-trend-session-template.md");
        if (string.IsNullOrWhiteSpace(templateContent))
        {
            issues.Add(new OutputValidationWorkspaceIssue(
                resourceTrendTemplatePath,
                "Resource trend session template source could not be loaded from the current build."));
            return;
        }

        try
        {
            writeAllText(resourceTrendTemplatePath, templateContent);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            issues.Add(new OutputValidationWorkspaceIssue(
                resourceTrendTemplatePath,
                $"Resource trend session template could not be seeded. {ex.GetType().Name}: {ex.Message}"));
        }
    }

    private void EnsureSeededTemplate(
        string path,
        string resourceName,
        string missingSourceDetail,
        string seedFailurePrefix,
        ICollection<OutputValidationWorkspaceIssue> issues) =>
        EnsureSeededGuidance(path, resourceName, missingSourceDetail, seedFailurePrefix, issues);

    private void EnsureSeededGuidance(
        string path,
        string resourceName,
        string missingSourceDetail,
        string seedFailurePrefix,
        ICollection<OutputValidationWorkspaceIssue> issues)
    {
        if (fileExists(path))
        {
            return;
        }

        var content = LoadEmbeddedText(resourceName);
        if (string.IsNullOrWhiteSpace(content))
        {
            issues.Add(new OutputValidationWorkspaceIssue(path, missingSourceDetail));
            return;
        }

        try
        {
            writeAllText(path, content);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            issues.Add(new OutputValidationWorkspaceIssue(
                path,
                $"{seedFailurePrefix} {ex.GetType().Name}: {ex.Message}"));
        }
    }

    private void EnsureResourceTrendScript(
        string resourceTrendScriptPath,
        ICollection<OutputValidationWorkspaceIssue> issues)
    {
        if (fileExists(resourceTrendScriptPath))
        {
            return;
        }

        var scriptContent = LoadEmbeddedText("Lumiere.App.Validation.ResourceTrend.collect-resource-trend-samples.ps1");
        if (string.IsNullOrWhiteSpace(scriptContent))
        {
            issues.Add(new OutputValidationWorkspaceIssue(
                resourceTrendScriptPath,
                "Resource trend sampler script source could not be loaded from the current build."));
            return;
        }

        try
        {
            writeAllText(resourceTrendScriptPath, scriptContent);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            issues.Add(new OutputValidationWorkspaceIssue(
                resourceTrendScriptPath,
                $"Resource trend sampler script could not be seeded. {ex.GetType().Name}: {ex.Message}"));
        }
    }

    private static string? LoadEmbeddedTemplateText()
        => LoadEmbeddedText("Lumiere.App.Validation.Output.output-validation-session.schema-v4.sample.json");

    private static string? LoadEmbeddedText(string resourceName)
    {
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
                "- Seed long-run resource trend validation helpers next to the same workspace so Story 12-3 runs start from the app-local validation surface.",
                "- Seed the current release checklist, HDR/SDR scenario guide, and settings accessibility workflow into guidance\\ for this machine.",
                string.Empty,
                "Workflow:",
                "1. Copy templates\\output-validation-session.schema-v4.sample.json into this output\\ folder.",
                "2. Or use Lumiere's Create draft action to generate a prefilled local draft in this folder.",
                "3. Review guidance\\release-validation-checklist.md, guidance\\hdr-sdr-validation-scenarios.md, and guidance\\settings-accessibility-validation.md before counting Windows manual evidence toward public release.",
                "4. Use templates\\hdr-sdr-validation-session-template.md when recording a focused Story 12-1 manual scenario run.",
                "5. Use templates\\resource-trend-session-template.md plus collect-resource-trend-samples.ps1 for Story 12-3 long-run validation sessions.",
                "6. Rename or copy templates as needed, replace every REPLACE_WITH_* placeholder, and keep manual evidence honest.",
                "7. Reload evidence from Lumiere after recording real observations.",
                "8. Do not treat template files or incomplete sessions as passing release evidence.",
                string.Empty,
                "Seeded local guides:",
                "- guidance\\release-validation-checklist.md",
                "- guidance\\hdr-sdr-validation-scenarios.md",
                "- guidance\\settings-accessibility-validation.md",
                string.Empty,
                "Repo references:",
                "- harness/validation/output-validation.md",
                "- harness/validation/resource-trend-validation.md"]);

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

    private string AllocateResourceTrendDraftPath(string workspaceDirectoryPath, DateTimeOffset now)
    {
        var stem = $"resource-trend-session-{now.ToLocalTime():yyyy-MM-dd}";
        var candidate = Path.Combine(workspaceDirectoryPath, $"{stem}.md");
        if (!fileExists(candidate))
        {
            return candidate;
        }

        for (var suffix = 2; suffix < 1000; suffix++)
        {
            candidate = Path.Combine(workspaceDirectoryPath, $"{stem}-{suffix}.md");
            if (!fileExists(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Could not allocate a unique resource trend draft file name.");
    }

    private ResourceTrendSummaryArtifact? SelectLatestResourceTrendSummary(
        OutputValidationWorkspaceState workspace,
        int processId)
    {
        var resourceTrendDirectoryPath = Path.Combine(workspace.DirectoryPath, "resource-trends");
        if (!directoryExists(resourceTrendDirectoryPath))
        {
            return null;
        }

        ResourceTrendSummaryArtifact? latestAnyProcess = null;
        foreach (var path in enumerateFiles(resourceTrendDirectoryPath, "*-summary.json")
            .OrderDescending(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var parsedSummary = ResourceTrendSummaryArtifact.FromJson(readAllText(path), path);
                var summary = parsedSummary with
                {
                    CsvPathStatus = parsedSummary.HasRecordedCsvPath
                        ? fileExists(parsedSummary.CsvPath)
                            ? ResourceTrendEvidencePathStatus.Present
                            : ResourceTrendEvidencePathStatus.Missing
                        : ResourceTrendEvidencePathStatus.Missing,
                };
                latestAnyProcess ??= summary;
                if (summary.MatchesProcessId(processId))
                {
                    return summary;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or System.Text.Json.JsonException)
            {
                continue;
            }
        }

        return latestAnyProcess;
    }

    private string AllocateScenarioNotesPath(string evidenceDirectoryPath, string draftStem)
    {
        var candidate = Path.Combine(evidenceDirectoryPath, $"{draftStem}-scenario-session.md");
        if (!fileExists(candidate))
        {
            return candidate;
        }

        for (var suffix = 2; suffix < 1000; suffix++)
        {
            candidate = Path.Combine(evidenceDirectoryPath, $"{draftStem}-scenario-session-{suffix}.md");
            if (!fileExists(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Could not allocate a unique scenario validation notes file name.");
    }

    private static OutputValidationDraftSeed? SelectDraftSeed(
        IEnumerable<OutputValidationSessionArtifact> artifacts,
        OutputValidationDraftRequest request)
    {
        ArgumentNullException.ThrowIfNull(artifacts);
        ArgumentNullException.ThrowIfNull(request);

        var artifactArray = artifacts.ToArray();
        var selected = artifactArray
            .OrderByDescending(artifact => ScoreSeedCompatibility(artifact, request))
            .ThenByDescending(artifact => artifact.Date, StringComparer.Ordinal)
            .FirstOrDefault();
        var runPlan = OutputValidationRunPlanner.Create(artifactArray, request.RequestedProfile);

        return new OutputValidationDraftSeed(
            selected?.Tester,
            selected?.WindowsVersion,
            selected?.Device,
            selected?.Gpu,
            selected?.DisplaySetup,
            selected?.DpiScales ?? [],
            selected?.EntryPointsTested ?? [])
        {
            SuggestedDisplayTopologies = runPlan.MissingDisplayTopologies,
            SuggestedEntryPoints = runPlan.MissingEntryPoints,
            SuggestedOutputTargets = runPlan.MissingOutputTargets,
            SuggestedViewerTargets = runPlan.MissingViewerTargets,
        };
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
