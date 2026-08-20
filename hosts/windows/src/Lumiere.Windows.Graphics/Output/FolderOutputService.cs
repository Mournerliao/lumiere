using Lumiere.Windows.Interop.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Lumiere.Windows.Graphics.Output;

public sealed class FolderOutputService : IOutputService
{
    private static readonly ILogger Logger = LumiereLoggerFactory.CreateLogger(LogCategories.Graphics);
    private readonly IOutputPngEncoder encoder;
    private readonly OutputFolderPathPolicy pathPolicy;
    private readonly Func<string, bool> directoryExists;
    private readonly Func<string, bool> fileExists;
    private readonly Func<string, byte[], CancellationToken, Task> writeAllBytesAsync;

    public FolderOutputService(IOutputPngEncoder encoder, OutputFolderPathPolicy? pathPolicy = null)
        : this(
            encoder,
            pathPolicy ?? new OutputFolderPathPolicy(),
            Directory.Exists,
            File.Exists,
            File.WriteAllBytesAsync)
    {
    }

    internal FolderOutputService(
        IOutputPngEncoder encoder,
        OutputFolderPathPolicy pathPolicy,
        Func<string, bool> directoryExists,
        Func<string, bool> fileExists,
        Func<string, byte[], CancellationToken, Task> writeAllBytesAsync)
    {
        this.encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
        this.pathPolicy = pathPolicy ?? throw new ArgumentNullException(nameof(pathPolicy));
        this.directoryExists = directoryExists ?? throw new ArgumentNullException(nameof(directoryExists));
        this.fileExists = fileExists ?? throw new ArgumentNullException(nameof(fileExists));
        this.writeAllBytesAsync = writeAllBytesAsync ?? throw new ArgumentNullException(nameof(writeAllBytesAsync));
    }

    public async Task<OutputResult> ExecuteOutputAsync(
        OutputRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.ShouldWriteFolder)
        {
            return OutputResult.FromTargets(OutputTargetResult.Skipped(
                OutputTarget.Folder,
                "Folder output was not requested"));
        }

        if (string.IsNullOrWhiteSpace(request.SaveDirectory)
            || !directoryExists(request.SaveDirectory))
        {
            return Failed("Save folder is not available");
        }

        try
        {
            var artifact = await encoder.EncodeArtifactAsync(
                request.Texture,
                request.CropRegion,
                cancellationToken,
                request.ArtifactCache);
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
            return OutputResult.FromTargets(OutputTargetResult.Success(
                OutputTarget.Folder,
                "Saved to folder",
                $"Folder output success: {artifact.Bytes.Length} bytes",
                artifactPath,
                artifact.Bytes.Length));
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

    private static OutputResult Failed(string message, string? detail = null) =>
        OutputResult.FromTargets(OutputTargetResult.Failed(OutputTarget.Folder, message, detail));
}
