namespace Lumiere.Graphics.Output;

/// <summary>
/// Computes whether loaded validation artifacts can be aligned to the current build token.
/// This stays intentionally strict: if Lumiere cannot prove a match, the result is not treated
/// as current-build release evidence.
/// </summary>
public sealed record ValidationArtifactBuildAlignment(
    ValidationArtifactBuildAlignmentStatus Status,
    string Detail,
    string? ExpectedBuildCommit,
    string? LatestArtifactBuildCommit)
{
    public bool MatchesCurrentBuild => Status is ValidationArtifactBuildAlignmentStatus.MatchedCurrentBuild;

    public static ValidationArtifactBuildAlignment NotChecked { get; } =
        new(
            ValidationArtifactBuildAlignmentStatus.NotChecked,
            "No loaded evidence is available yet, so current-build alignment cannot be checked.",
            null,
            null);

    public static ValidationArtifactBuildAlignment Evaluate(
        string? buildVersion,
        IEnumerable<OutputValidationSessionArtifact> artifacts)
    {
        ArgumentNullException.ThrowIfNull(artifacts);

        var artifactArray = artifacts.ToArray();
        if (artifactArray.Length == 0)
        {
            return NotChecked;
        }

        var latestArtifact = artifactArray
            .OrderByDescending(artifact => ParseArtifactDate(artifact.Date))
            .ThenByDescending(artifact => NormalizeEvidenceField(artifact.BuildCommit, "unknown build"))
            .First();
        var expectedCommit = ExtractBuildCommitToken(buildVersion);
        var artifactCommit = ExtractArtifactCommitToken(latestArtifact.BuildCommit);

        if (string.IsNullOrWhiteSpace(expectedCommit))
        {
            return new ValidationArtifactBuildAlignment(
                ValidationArtifactBuildAlignmentStatus.Unknown,
                "The current app build does not expose a commit token, so Lumiere cannot prove whether the loaded evidence matches this exact build.",
                NormalizeBuildVersion(buildVersion),
                NormalizeEvidenceField(latestArtifact.BuildCommit, "unknown build"));
        }

        if (string.IsNullOrWhiteSpace(artifactCommit))
        {
            return new ValidationArtifactBuildAlignment(
                ValidationArtifactBuildAlignmentStatus.Unknown,
                $"Loaded evidence does not record a comparable build commit, so current-build alignment remains unknown. Current build token: {expectedCommit}. Evidence build field: {NormalizeEvidenceField(latestArtifact.BuildCommit, "unknown build")}.",
                expectedCommit,
                NormalizeEvidenceField(latestArtifact.BuildCommit, "unknown build"));
        }

        if (string.Equals(expectedCommit, artifactCommit, StringComparison.OrdinalIgnoreCase))
        {
            return new ValidationArtifactBuildAlignment(
                ValidationArtifactBuildAlignmentStatus.MatchedCurrentBuild,
                $"Loaded evidence matches the current build token ({expectedCommit}).",
                expectedCommit,
                artifactCommit);
        }

        return new ValidationArtifactBuildAlignment(
            ValidationArtifactBuildAlignmentStatus.StaleForCurrentBuild,
            $"Loaded evidence is stale for the current build. Current build token: {expectedCommit}; latest evidence token: {artifactCommit}.",
            expectedCommit,
            artifactCommit);
    }

    private static DateOnly ParseArtifactDate(string? value) =>
        DateOnly.TryParse(value, out var parsed)
            ? parsed
            : DateOnly.MinValue;

    private static string NormalizeBuildVersion(string? buildVersion)
    {
        var trimmed = buildVersion?.Trim();
        return string.IsNullOrWhiteSpace(trimmed)
            ? "unknown build"
            : trimmed;
    }

    private static string NormalizeEvidenceField(string? value, string fallback)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed)
            || trimmed.Contains("REPLACE_WITH_", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("Template only", StringComparison.OrdinalIgnoreCase)
                ? fallback
                : trimmed;
    }

    private static string? ExtractBuildCommitToken(string? buildVersion)
    {
        var normalized = NormalizeBuildVersion(buildVersion);
        if (string.Equals(normalized, "unknown build", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var plusIndex = normalized.IndexOf('+');
        if (plusIndex >= 0 && plusIndex < normalized.Length - 1)
        {
            return NormalizeCommitToken(normalized[(plusIndex + 1)..]);
        }

        var pieces = normalized.Split([' ', '-', '.'], StringSplitOptions.RemoveEmptyEntries);
        return pieces
            .Select(NormalizeCommitToken)
            .FirstOrDefault(token => token is not null);
    }

    private static string? ExtractArtifactCommitToken(string? buildCommit)
    {
        var normalized = NormalizeEvidenceField(buildCommit, "unknown build");
        return string.Equals(normalized, "unknown build", StringComparison.OrdinalIgnoreCase)
            ? null
            : NormalizeCommitToken(normalized);
    }

    private static string? NormalizeCommitToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        var filtered = new string(trimmed.Where(Uri.IsHexDigit).ToArray());
        return filtered.Length >= 7
            ? filtered.ToLowerInvariant()
            : null;
    }
}

public enum ValidationArtifactBuildAlignmentStatus
{
    NotChecked = 0,
    Unknown,
    MatchedCurrentBuild,
    StaleForCurrentBuild,
}
