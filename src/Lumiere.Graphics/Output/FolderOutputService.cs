using Lumiere.Infrastructure.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Lumiere.Graphics.Output;

public sealed class FolderOutputService : IOutputService
{
    private static readonly ILogger Logger = LumiereLoggerFactory.CreateLogger(LogCategories.Graphics);
    private readonly IOutputPngEncoder encoder;
    private readonly OutputFolderPathPolicy pathPolicy;
    private readonly Func<string, bool> directoryExists;
    private readonly Func<string, bool> fileExists;
    private readonly Func<string, byte[], CancellationToken, Task> writeAllBytesAsync;

    public FolderOutputService(
        IOutputPngEncoder encoder,
        OutputFolderPathPolicy? pathPolicy = null)
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

    public async Task<OutputResult> ExecuteOutputAsync(OutputRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.Policy.ShouldAttemptFolder)
        {
            return OutputResult.FromTargets(OutputTargetResult.Skipped(
                OutputTarget.Folder,
                "Folder output skipped by settings",
                "Folder target is not configured."))
                .WithRequestedProfile(request.Policy.RequestedProfile);
        }

        if (request.Texture is null)
        {
            return Failed(request, "No captured frame texture available");
        }

        if (string.IsNullOrWhiteSpace(request.Policy.SavePath))
        {
            return Failed(request, "Save path is not configured");
        }

        if (!directoryExists(request.Policy.SavePath))
        {
            return Failed(request, "Save folder is not available");
        }

        try
        {
            var artifactPath = pathPolicy.CreateCandidatePath(request.Policy, fileExists);
            var pngBytes = await encoder.EncodePngAsync(request.Texture, request.CropRegion, cancellationToken);
            await writeAllBytesAsync(artifactPath, pngBytes, cancellationToken);

            Logger.LogInformation(
                "operation=FolderOutput, stage=Complete, path={Path}, bytes={Bytes}",
                artifactPath,
                pngBytes.Length);

            return OutputResult.FromTargets(OutputTargetResult.Success(
                OutputTarget.Folder,
                "Saved to folder",
                $"Folder output success: {pngBytes.Length} bytes",
                artifactPath))
                .WithRequestedProfile(request.Policy.RequestedProfile);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            var diagnostic = DiagnosticContext.OutputWarning(
                stage: "FolderWrite",
                userFacingState: "Failed to save file",
                technicalDetail: $"operation=FolderOutput, stage=Write, exception={ex.GetType().Name}: {ex.Message}",
                exception: ex);
            diagnostic.LogTo(Logger);

            return Failed(request, "Failed to save file", ex.Message);
        }
    }

    private static OutputResult Failed(OutputRequest request, string userMessage, string? technicalDetail = null) =>
        OutputResult.FromTargets(OutputTargetResult.Failed(
            OutputTarget.Folder,
            userMessage,
            technicalDetail ?? userMessage))
            .WithRequestedProfile(request.Policy.RequestedProfile);
}
