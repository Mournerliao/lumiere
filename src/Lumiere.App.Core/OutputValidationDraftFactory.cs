using Lumiere.Capture;
using Lumiere.Graphics.Hdr;
using Lumiere.Graphics.Output;

namespace Lumiere.App;

public static class OutputValidationDraftFactory
{
    public static OutputValidationDraftDocument Create(
        OutputValidationDraftRequest request,
        DateTimeOffset now,
        ITargetAppVersionPrefillProvider? targetAppVersionPrefillProvider = null,
        OutputValidationDraftSeed? seed = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var localNow = now.ToLocalTime();
        var date = localNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var profile = request.RequestedProfile ?? throw new ArgumentNullException(nameof(request.RequestedProfile));
        var outputTarget = request.OutputTarget;
        var targetApps = profile.ViewerEvidence
            .Select(viewer => viewer.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var buildLabel = NormalizeBuildVersion(request.BuildVersion);
        var buildCommit = ExtractBuildCommit(request.BuildVersion);
        var artifact = new OutputValidationSessionArtifact(
            Date: date,
            Tester: CreateTesterPlaceholder(seed),
            BuildCommit: buildCommit is null
                ? $"REPLACE_WITH_GIT_COMMIT (app version {buildLabel})"
                : $"{buildCommit} (app version {buildLabel})",
            WindowsVersion: CreateWindowsVersionPlaceholder(seed),
            Device: CreateDevicePlaceholder(seed),
            Gpu: CreateGpuPlaceholder(request.CurrentSessionHint, seed),
            DisplaySetup: CreateDisplaySetupPlaceholder(request.SessionState.Target, request.CurrentSessionHint, seed),
            HdrState: CreateHdrStatePlaceholder(request.SessionState.Readiness),
            DpiScales: CreateDpiScalePlaceholders(request.CurrentSessionHint, seed),
            EntryPointsTested: CreateEntryPointPlaceholders(seed),
            OutputTargetsTested:
            [
                FormatOutputTarget(outputTarget),
            ],
            TargetAppsTested: targetApps,
            ChecklistIdsCovered: CreateSuggestedChecklistIds(outputTarget, profile.Kind),
            ResultSummary: $"REPLACE_WITH_VALIDATION_RESULT_SUMMARY. Draft for {profile.Label} on {FormatOutputTarget(outputTarget)} generated from {buildLabel}.",
            EvidencePaths:
            [
                $@"evidence\{CreateFileNameStem(date, profile.Label, outputTarget)}-REPLACE_WITH_SESSION_NOTES.md",
            ],
            KnownLimitations:
            [
                "Draft created from current Lumiere session context. Replace this line with observed limitations after Windows manual validation.",
            ],
            FollowUpIssuesOrStories:
            [
                "11-3",
                "12-1",
                "10-3",
                "13-2",
            ],
            OutputProfileRecords:
            [
                CreateProfileRecord(profile, outputTarget),
            ])
        {
            TargetAppVersions = CreateTargetAppVersionSuggestions(
                targetApps,
                targetAppVersionPrefillProvider),
            TargetHdrEvidence = CreateTargetHdrEvidence(request.SessionState),
        };

        return new OutputValidationDraftDocument(
            artifact,
            CreateFileNameStem(date, profile.Label, outputTarget));
    }

    private static OutputProfileValidationRecord CreateProfileRecord(
        OutputProfileContract profile,
        OutputTarget outputTarget)
    {
        var formatContract = profile.Kind switch
        {
            OutputProfileKind.Hdr10Pq => new OutputFormatContract(
                OutputPixelFormat.R16G16B16A16Float,
                OutputPixelFormat.R16G16B16A16Float,
                OutputTransferFunction.PqSt2084,
                OutputColorPrimaries.Bt2020,
                OutputConversionPolicy.PreserveHdrWithDefinedToneMapping,
                OutputMetadataPolicy.AttachHdr10StaticMetadata,
                OutputTargetAppAssumption.RequiresHdrViewerValidation,
                Hdr10StaticMetadataPolicy.Bt2020PqReference1000Nit),
            OutputProfileKind.SrgbCompatibilityPng => OutputFormatContract.SrgbCompatibility,
            _ => null,
        };

        return new OutputProfileValidationRecord(
            profile.Kind,
            profile.ViewerEvidence)
        {
            FormatContract = formatContract,
            OutputTargetsCovered =
            [
                FormatOutputTarget(outputTarget),
            ],
        };
    }

    private static TargetAwareHdrValidationEvidence CreateTargetHdrEvidence(CaptureSessionState sessionState)
    {
        ArgumentNullException.ThrowIfNull(sessionState);

        var target = sessionState.Target;
        var displayIdentity = target?.DisplayIdentity;
        return new TargetAwareHdrValidationEvidence(
            target?.DisplayName ?? "REPLACE_WITH_TARGET_DISPLAY_NAME",
            displayIdentity?.Left,
            displayIdentity?.Top,
            displayIdentity?.Width ?? target?.Size.Width ?? 0,
            displayIdentity?.Height ?? target?.Size.Height ?? 0,
            CreateMatchKind(displayIdentity),
            CreateObservedHdrStatePlaceholder(sessionState.Readiness),
            CreateObservedColorSpace(sessionState.Readiness),
            CreateTargetDetailPlaceholder(sessionState.Readiness, target));
    }

    private static string[] CreateSuggestedChecklistIds(
        OutputTarget outputTarget,
        OutputProfileKind profileKind)
    {
        string[] checklistIds = outputTarget switch
        {
            OutputTarget.Clipboard => ["REL-OUT-01", "REL-OUT-02", "REL-OUT-03"],
            OutputTarget.Folder => ["REL-OUT-04"],
            OutputTarget.Both => ["REL-OUT-05"],
            _ => ["REL-OUT-04"],
        };

        if (profileKind is OutputProfileKind.Hdr10Pq or OutputProfileKind.DisplayP3)
        {
            return [.. checklistIds, "REL-HDR-04"];
        }

        return checklistIds;
    }

    private static string CreateDisplaySetupPlaceholder(CaptureTarget? target)
        => CreateDisplaySetupPlaceholder(target, null);

    private static string CreateDisplaySetupPlaceholder(
        CaptureTarget? target,
        OutputValidationDraftSeed? seed)
    {
        return CreateDisplaySetupPlaceholder(target, currentSessionHint: null, seed);
    }

    private static string CreateDisplaySetupPlaceholder(
        CaptureTarget? target,
        OutputValidationCurrentSessionHint? currentSessionHint,
        OutputValidationDraftSeed? seed)
    {
        var currentSession = NormalizeHint(currentSessionHint?.DisplaySetup);
        var hint = NormalizeHint(seed?.DisplaySetup);
        if (target is null)
        {
            return CreateDisplaySetupPlaceholder(
                "REPLACE_WITH_FULL_DISPLAY_SETUP",
                currentSession,
                hint);
        }

        return CreateDisplaySetupPlaceholder(
            $"REPLACE_WITH_FULL_DISPLAY_SETUP (active target: {target.DisplayName})",
            currentSession,
            hint);
    }

    private static string CreateHdrStatePlaceholder(PreviewReadinessStatus readiness)
    {
        ArgumentNullException.ThrowIfNull(readiness);
        return $"REPLACE_WITH_OBSERVED_WINDOWS_HDR_STATE (current session: {readiness.UserMessage})";
    }

    private static string CreateObservedHdrStatePlaceholder(PreviewReadinessStatus readiness)
    {
        ArgumentNullException.ThrowIfNull(readiness);
        return $"REPLACE_WITH_OBSERVED_TARGET_HDR_STATE (current session: {readiness.UserMessage})";
    }

    private static string? CreateObservedColorSpace(PreviewReadinessStatus readiness)
    {
        ArgumentNullException.ThrowIfNull(readiness);

        var detail = readiness.TechnicalDetail;
        if (string.IsNullOrWhiteSpace(detail))
        {
            return null;
        }

        var marker = "set ";
        var markerIndex = detail.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return null;
        }

        var tail = detail[(markerIndex + marker.Length)..];
        var colorSpace = new string(tail.TakeWhile(char.IsLetterOrDigit).ToArray());
        return string.IsNullOrWhiteSpace(colorSpace) ? null : colorSpace;
    }

    private static string CreateTargetDetailPlaceholder(
        PreviewReadinessStatus readiness,
        CaptureTarget? target)
    {
        ArgumentNullException.ThrowIfNull(readiness);

        var targetLabel = target?.DisplayName ?? "selected target";
        var technicalDetail = string.IsNullOrWhiteSpace(readiness.TechnicalDetail)
            ? "No additional session detail was available."
            : readiness.TechnicalDetail.Trim();
        return $"REPLACE_WITH_TARGET_HDR_VALIDATION_DETAIL. Current session for {targetLabel}: {readiness.UserMessage} {technicalDetail}";
    }

    private static string CreateMatchKind(DisplayOutputIdentity? displayIdentity) =>
        displayIdentity switch
        {
            { Left: not null, Top: not null } => "DesktopBounds",
            not null => "REPLACE_WITH_TARGET_MATCH_KIND (current session matched a display target)",
            _ => "REPLACE_WITH_TARGET_MATCH_KIND",
        };

    private static string NormalizeBuildVersion(string? buildVersion)
    {
        var trimmed = buildVersion?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return "Lumiere unknown build";
        }

        return trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase)
            ? $"Lumiere {trimmed}"
            : $"Lumiere v{trimmed}";
    }

    private static string? ExtractBuildCommit(string? buildVersion)
    {
        var trimmed = buildVersion?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return null;
        }

        var plusIndex = trimmed.IndexOf('+');
        if (plusIndex >= 0 && plusIndex < trimmed.Length - 1)
        {
            return NormalizeCommitToken(trimmed[(plusIndex + 1)..]);
        }

        return null;
    }

    private static string? NormalizeCommitToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var filtered = new string(value.Trim().Where(Uri.IsHexDigit).ToArray());
        return filtered.Length >= 7
            ? filtered.ToLowerInvariant()
            : null;
    }

    private static IReadOnlyList<OutputValidationTargetAppVersionRecord> CreateTargetAppVersionSuggestions(
        IReadOnlyList<string> targetApps,
        ITargetAppVersionPrefillProvider? targetAppVersionPrefillProvider) =>
        targetApps
            .Select(targetApp => new OutputValidationTargetAppVersionRecord(
                targetApp,
                ResolveSuggestedTargetAppVersion(targetApp, targetAppVersionPrefillProvider)))
            .ToArray();

    private static string ResolveSuggestedTargetAppVersion(
        string targetApp,
        ITargetAppVersionPrefillProvider? targetAppVersionPrefillProvider)
    {
        var resolvedVersion = targetAppVersionPrefillProvider?.TryGetVersion(targetApp)?.Trim();
        return string.IsNullOrWhiteSpace(resolvedVersion)
            ? $"REPLACE_WITH_{SanitizeIdentifier(targetApp)}_VERSION"
            : resolvedVersion;
    }

    private static string CreateFileNameStem(
        string date,
        string profileLabel,
        OutputTarget outputTarget) =>
        $"output-validation-draft-{date}-{SanitizeSegment(profileLabel)}-{SanitizeSegment(FormatOutputTarget(outputTarget))}";

    private static string FormatOutputTarget(OutputTarget outputTarget) =>
        outputTarget switch
        {
            OutputTarget.Clipboard => "Clipboard",
            OutputTarget.Folder => "Folder",
            OutputTarget.Both => "Both",
            _ => "Folder",
        };

    private static string SanitizeSegment(string value)
    {
        var trimmed = value.Trim().ToLowerInvariant();
        var characters = trimmed
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray();
        var sanitized = new string(characters).Trim('-');
        return string.IsNullOrWhiteSpace(sanitized) ? "draft" : sanitized;
    }

    private static string SanitizeIdentifier(string value)
    {
        var trimmed = value.Trim().ToUpperInvariant();
        var characters = trimmed
            .Select(character => char.IsLetterOrDigit(character) ? character : '_')
            .ToArray();
        var sanitized = new string(characters).Trim('_');
        return string.IsNullOrWhiteSpace(sanitized) ? "APP" : sanitized;
    }

    private static string CreateDisplaySetupPlaceholder(
        string placeholder,
        string? currentSession,
        string? latestArtifact)
    {
        if (currentSession is null && latestArtifact is null)
        {
            return placeholder;
        }

        if (currentSession is not null && latestArtifact is not null)
        {
            return $"{placeholder} (current session: {currentSession}; latest local artifact: {latestArtifact})";
        }

        return currentSession is not null
            ? $"{placeholder} (current session: {currentSession})"
            : $"{placeholder} (latest local artifact: {latestArtifact})";
    }

    private static string CreateTesterPlaceholder(OutputValidationDraftSeed? seed) =>
        AppendLatestArtifactHint("REPLACE_WITH_TESTER_NAME", seed?.Tester);

    private static string CreateWindowsVersionPlaceholder(OutputValidationDraftSeed? seed)
    {
        var currentSession = $"current session: {Environment.OSVersion.VersionString}";
        var latestArtifact = NormalizeHint(seed?.WindowsVersion);
        return latestArtifact is null
            ? $"REPLACE_WITH_WINDOWS_VERSION ({currentSession})"
            : $"REPLACE_WITH_WINDOWS_VERSION ({currentSession}; latest local artifact: {latestArtifact})";
    }

    private static string CreateDevicePlaceholder(OutputValidationDraftSeed? seed) =>
        AppendLatestArtifactHint("REPLACE_WITH_DEVICE_MODEL", seed?.Device);

    private static string CreateGpuPlaceholder(
        OutputValidationCurrentSessionHint? currentSessionHint,
        OutputValidationDraftSeed? seed)
    {
        var currentSession = NormalizeHint(currentSessionHint?.Gpu);
        var latestArtifact = NormalizeHint(seed?.Gpu);
        if (currentSession is null && latestArtifact is null)
        {
            return "REPLACE_WITH_GPU_MODEL_AND_DRIVER";
        }

        if (currentSession is not null && latestArtifact is not null)
        {
            return $"REPLACE_WITH_GPU_MODEL_AND_DRIVER (current session: {currentSession}; latest local artifact: {latestArtifact})";
        }

        return currentSession is not null
            ? $"REPLACE_WITH_GPU_MODEL_AND_DRIVER (current session: {currentSession})"
            : $"REPLACE_WITH_GPU_MODEL_AND_DRIVER (latest local artifact: {latestArtifact})";
    }

    private static IReadOnlyList<string> CreateDpiScalePlaceholders(
        OutputValidationCurrentSessionHint? currentSessionHint,
        OutputValidationDraftSeed? seed)
    {
        var currentSession = JoinMeaningfulValues(currentSessionHint?.DpiScales);
        var latestArtifact = JoinMeaningfulValues(seed?.DpiScales);
        return
        [
            CreateDpiScalePlaceholder(currentSession, latestArtifact),
        ];
    }

    private static string CreateDpiScalePlaceholder(string? currentSession, string? latestArtifact)
    {
        if (currentSession is null && latestArtifact is null)
        {
            return "REPLACE_WITH_DPI_SCALE";
        }

        if (currentSession is not null && latestArtifact is not null)
        {
            return $"REPLACE_WITH_DPI_SCALE (current session: {currentSession}; latest local artifact: {latestArtifact})";
        }

        return currentSession is not null
            ? $"REPLACE_WITH_DPI_SCALE (current session: {currentSession})"
            : $"REPLACE_WITH_DPI_SCALE (latest local artifact: {latestArtifact})";
    }

    private static IReadOnlyList<string> CreateEntryPointPlaceholders(OutputValidationDraftSeed? seed)
    {
        var latestArtifact = JoinMeaningfulValues(seed?.EntryPointsTested);
        return
        [
            latestArtifact is null
                ? "REPLACE_WITH_ENTRY_POINT (for example: Main panel, Tray menu, Global hotkey)"
                : $"REPLACE_WITH_ENTRY_POINT (for example: Main panel, Tray menu, Global hotkey; latest local artifact: {latestArtifact})",
        ];
    }

    private static string AppendLatestArtifactHint(string placeholder, string? hint)
    {
        var normalized = NormalizeHint(hint);
        return normalized is null
            ? placeholder
            : $"{placeholder} (latest local artifact: {normalized})";
    }

    private static string? JoinMeaningfulValues(IEnumerable<string>? values)
    {
        if (values is null)
        {
            return null;
        }

        var normalized = values
            .Select(NormalizeHint)
            .Where(value => value is not null)
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return normalized.Length == 0
            ? null
            : string.Join(", ", normalized);
    }

    private static string? NormalizeHint(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return null;
        }

        return trimmed.Contains("REPLACE_WITH_", StringComparison.OrdinalIgnoreCase)
               || trimmed.StartsWith("Template only", StringComparison.OrdinalIgnoreCase)
            ? null
            : trimmed;
    }
}

public sealed record OutputValidationDraftDocument(
    OutputValidationSessionArtifact Artifact,
    string FileNameStem);
