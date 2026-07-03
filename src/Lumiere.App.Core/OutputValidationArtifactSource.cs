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
    string GuidanceDirectoryPath,
    string EvidenceDirectoryPath,
    string GuidanceFilePath,
    string? SampleTemplatePath,
    string? ReleaseChecklistPath,
    string? HdrSdrScenariosPath,
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

public sealed class FileOutputValidationArtifactSource : IOutputValidationArtifactSource
{
    internal const string WorkspaceReadmeFileName = "README.txt";
    internal const string SampleTemplateFileName = "output-validation-session.schema-v4.sample.json";
    internal const string HdrSdrSessionTemplateFileName = "hdr-sdr-validation-session-template.md";
    internal const string MvpChecklistFileName = "mvp-checklist.md";
    internal const string HdrNotesFileName = "hdr-notes.md";

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

                if (TryCreateIncompleteMarkdownEvidenceDetail(evidencePath, content, out var incompleteDetail))
                {
                    yield return new OutputValidationArtifactLoadIssue(
                        artifactPath,
                        incompleteDetail);
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

    private static bool TryCreateIncompleteMarkdownEvidenceDetail(
        string evidencePath,
        string content,
        out string detail)
    {
        var fixes = CreateIncompleteMarkdownEvidenceFixes(content);
        if (fixes.Count == 0)
        {
            detail = string.Empty;
            return false;
        }

        detail = $"Workspace-local markdown evidence is incomplete: {evidencePath}. {string.Join(" ", fixes)}";
        return true;
    }

    private static IReadOnlyList<string> CreateIncompleteMarkdownEvidenceFixes(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return ["Record observed Windows manual validation notes before using this file as evidence."];
        }

        var fixes = new List<string>();
        if (content.Contains("REPLACE_WITH_", StringComparison.OrdinalIgnoreCase))
        {
            fixes.Add("Replace every REPLACE_WITH_* placeholder.");
        }

        if (content.Contains("Template only", StringComparison.OrdinalIgnoreCase))
        {
            fixes.Add("Replace template-only language with observed validation results.");
        }

        if (content.Contains("Draft status: NOT RUN until", StringComparison.OrdinalIgnoreCase))
        {
            fixes.Add("Remove the draft NOT RUN sentinel after recording observed Windows manual results.");
        }

        if (content.Contains("PASS / PASS with limitation / FAIL / NOT RUN", StringComparison.OrdinalIgnoreCase))
        {
            fixes.Add("Replace unresolved result choices with one observed status for each scenario.");
        }

        return fixes;
    }

    private OutputValidationWorkspaceState EnsureWorkspace()
    {
        var templatesDirectoryPath = Path.Combine(directoryPath, "templates");
        var guidanceDirectoryPath = Path.Combine(directoryPath, "guidance");
        var evidenceDirectoryPath = Path.Combine(directoryPath, "evidence");
        var guidanceFilePath = Path.Combine(directoryPath, WorkspaceReadmeFileName);
        var sampleTemplatePath = Path.Combine(templatesDirectoryPath, SampleTemplateFileName);
        var hdrSdrSessionTemplatePath = Path.Combine(templatesDirectoryPath, HdrSdrSessionTemplateFileName);
        var releaseChecklistPath = Path.Combine(guidanceDirectoryPath, MvpChecklistFileName);
        var hdrSdrScenariosPath = Path.Combine(guidanceDirectoryPath, HdrNotesFileName);
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
                "Lumiere.App.Validation.Guidance.mvp-checklist.md",
                "MVP validation checklist source could not be loaded from the current build.",
                "MVP validation checklist could not be seeded.",
                issues);
            EnsureSeededGuidance(
                hdrSdrScenariosPath,
                "Lumiere.App.Validation.Guidance.hdr-notes.md",
                "HDR notes source could not be loaded from the current build.",
                "HDR notes could not be seeded.",
                issues);
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
                "- Seed the current MVP checklist and HDR notes into guidance\\ for this machine.",
                string.Empty,
                "Workflow:",
                "1. Copy templates\\output-validation-session.schema-v4.sample.json into this output\\ folder.",
                "2. Or use Lumiere's Create draft action to generate a prefilled local draft in this folder.",
                "3. Review guidance\\mvp-checklist.md and guidance\\hdr-notes.md before counting Windows manual evidence toward the MVP.",
                "4. Use templates\\hdr-sdr-validation-session-template.md only when recording deeper HDR notes for future export work.",
                "5. Rename or copy templates as needed, replace every REPLACE_WITH_* placeholder, and keep manual evidence honest.",
                "6. Reload evidence from Lumiere after recording real observations.",
                "7. Do not treat template files or incomplete sessions as passing MVP evidence.",
                string.Empty,
                "Seeded local guides:",
                "- guidance\\mvp-checklist.md",
                "- guidance\\hdr-notes.md",
                string.Empty,
                "Repo references:",
                "- docs/validation/mvp-checklist.md",
                "- docs/validation/hdr-notes.md"]);

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
