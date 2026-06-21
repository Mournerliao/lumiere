using System.Diagnostics;
using Lumiere.Infrastructure.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Lumiere.Infrastructure.Interop;

public sealed class WindowsArtifactShellAction : IArtifactShellAction
{
    private static readonly ILogger Logger = LumiereLoggerFactory.CreateLogger(LogCategories.Infrastructure);
    private readonly Func<ProcessStartInfo, Process?> startProcess;
    private readonly Func<string, bool> fileExists;
    private readonly Func<string, bool> directoryExists;

    public WindowsArtifactShellAction()
        : this(Process.Start, File.Exists, Directory.Exists)
    {
    }

    internal WindowsArtifactShellAction(
        Func<ProcessStartInfo, Process?> startProcess,
        Func<string, bool> fileExists,
        Func<string, bool> directoryExists)
    {
        this.startProcess = startProcess ?? throw new ArgumentNullException(nameof(startProcess));
        this.fileExists = fileExists ?? throw new ArgumentNullException(nameof(fileExists));
        this.directoryExists = directoryExists ?? throw new ArgumentNullException(nameof(directoryExists));
    }

    public Task<ArtifactShellActionResult> ExecuteAsync(
        string artifactPath,
        ArtifactShellActionKind action,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(artifactPath))
        {
            return Task.FromResult(ArtifactShellActionResult.Failed("Artifact path is empty."));
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (!fileExists(artifactPath) && !directoryExists(artifactPath))
        {
            return Task.FromResult(ArtifactShellActionResult.Failed("Artifact path does not exist."));
        }

        try
        {
            using var process = startProcess(CreateStartInfo(artifactPath, action));
            if (process is null)
            {
                return Task.FromResult(ArtifactShellActionResult.Failed("Windows shell did not start a process."));
            }

            Logger.LogInformation(
                "operation=ArtifactShellAction, stage=Started, action={Action}, path={Path}",
                action,
                artifactPath);
            return Task.FromResult(ArtifactShellActionResult.Success());
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            Logger.LogWarning(
                ex,
                "operation=ArtifactShellAction, stage=Start, action={Action}, path={Path}",
                action,
                artifactPath);
            return Task.FromResult(ArtifactShellActionResult.Failed(ex.Message));
        }
    }

    private static ProcessStartInfo CreateStartInfo(string artifactPath, ArtifactShellActionKind action) =>
        action switch
        {
            ArtifactShellActionKind.Open => new ProcessStartInfo
            {
                FileName = artifactPath,
                UseShellExecute = true,
            },
            ArtifactShellActionKind.Reveal => new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{artifactPath.Replace("\"", "\\\"", StringComparison.Ordinal)}\"",
                UseShellExecute = true,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unsupported shell action."),
        };
}
