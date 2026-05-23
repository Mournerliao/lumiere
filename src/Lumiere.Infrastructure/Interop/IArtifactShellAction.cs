namespace Lumiere.Infrastructure.Interop;

public interface IArtifactShellAction
{
    Task<ArtifactShellActionResult> ExecuteAsync(
        string artifactPath,
        ArtifactShellActionKind action,
        CancellationToken cancellationToken = default);
}

public enum ArtifactShellActionKind
{
    Open = 0,
    Reveal = 1,
}

public sealed record ArtifactShellActionResult(
    bool IsSuccess,
    string? TechnicalDetail)
{
    public static ArtifactShellActionResult Success() => new(true, null);

    public static ArtifactShellActionResult Failed(string technicalDetail) => new(false, technicalDetail);
}
