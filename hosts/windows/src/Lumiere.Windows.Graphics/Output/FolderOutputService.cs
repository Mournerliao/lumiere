using Lumiere.Windows.Interop.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Lumiere.Windows.Graphics.Output;

internal sealed class FolderOutputService : IOutputTargetAdapter
{
    private static readonly ILogger Logger = LumiereLoggerFactory.CreateLogger(LogCategories.Graphics);
    private readonly OutputFolderPathPolicy pathPolicy;
    private readonly Func<string, bool> directoryExists;
    private readonly Func<string, bool> fileExists;
    private readonly Func<string, byte[], CancellationToken, Task> writeAllBytesAsync;

    public FolderOutputService(OutputFolderPathPolicy? pathPolicy = null)
        : this(
            pathPolicy ?? new OutputFolderPathPolicy(),
            Directory.Exists,
            File.Exists,
            File.WriteAllBytesAsync)
    {
    }

    internal FolderOutputService(
        OutputFolderPathPolicy pathPolicy,
        Func<string, bool> directoryExists,
        Func<string, bool> fileExists,
        Func<string, byte[], CancellationToken, Task> writeAllBytesAsync)
    {
        this.pathPolicy = pathPolicy ?? throw new ArgumentNullException(nameof(pathPolicy));
        this.directoryExists = directoryExists ?? throw new ArgumentNullException(nameof(directoryExists));
        this.fileExists = fileExists ?? throw new ArgumentNullException(nameof(fileExists));
        this.writeAllBytesAsync = writeAllBytesAsync ?? throw new ArgumentNullException(nameof(writeAllBytesAsync));
    }

    public OutputTarget Target => OutputTarget.Folder;

    public async Task<OutputTargetResult> DeliverAsync(
        OutputRequest request,
        OutputEncodedArtifact artifact,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(artifact);

        if (string.IsNullOrWhiteSpace(request.SaveDirectory)
            || !directoryExists(request.SaveDirectory))
        {
            return Failed("Save folder is not available");
        }

        try
        {
            var artifactPath = pathPolicy.CreateCandidatePath(
                request.SaveDirectory,
                request.TimestampNaming,
                artifact.NormalizedFileExtension,
                fileExists);
            await writeAllBytesAsync(artifactPath, artifact.Bytes, cancellationToken);

            Logger.LogInformation(
                "operation=FolderOutput, stage=Complete, path={Path}, bytes={Bytes}, profile={Profile}",
                artifactPath,
                artifact.Bytes.Length,
                OutputEncodedArtifact.Profile);
            return OutputTargetResult.Success(
                OutputTarget.Folder,
                "Saved to folder",
                $"Folder output success: {artifact.Bytes.Length} bytes",
                artifactPath,
                artifact.Bytes.Length);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            DiagnosticContext.OutputWarning(
                stage: "FolderWrite",
                userFacingState: "Failed to save file",
                technicalDetail: exception.Message,
                exception: exception).LogTo(Logger);
            return Failed("Failed to save file", exception.Message);
        }
    }

    private static OutputTargetResult Failed(string message, string? detail = null) =>
        OutputTargetResult.Failed(OutputTarget.Folder, message, detail);
}
