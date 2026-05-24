using Lumiere.Infrastructure.Diagnostics;
using Lumiere.Infrastructure.Interop;
using Microsoft.Extensions.Logging;

namespace Lumiere.Graphics.Output;

/// <summary>
/// Decorates output execution with supported post-capture file artifact actions.
/// </summary>
public sealed class AfterCaptureOutputService : IOutputService
{
    private static readonly ILogger Logger = LumiereLoggerFactory.CreateLogger(LogCategories.Graphics);
    private readonly IOutputService innerOutputService;
    private readonly IArtifactShellAction artifactShellAction;

    public AfterCaptureOutputService(
        IOutputService innerOutputService,
        IArtifactShellAction artifactShellAction)
    {
        this.innerOutputService = innerOutputService ?? throw new ArgumentNullException(nameof(innerOutputService));
        this.artifactShellAction = artifactShellAction ?? throw new ArgumentNullException(nameof(artifactShellAction));
    }

    public async Task<OutputResult> ExecuteOutputAsync(OutputRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var outputResult = await innerOutputService.ExecuteOutputAsync(request, cancellationToken);
        var action = request.Policy.AfterCaptureAction;

        if (action == OutputAfterCaptureAction.None)
        {
            return outputResult.WithAfterCapture(AfterCaptureResult.NotRequested());
        }

        var artifactPath = outputResult.Targets
            .Where(target => target.Target == OutputTarget.Folder && target.Outcome == OutputOutcome.Success)
            .Select(target => target.ArtifactPath)
            .FirstOrDefault(path => !string.IsNullOrWhiteSpace(path));

        if (string.IsNullOrWhiteSpace(artifactPath))
        {
            return outputResult.WithAfterCapture(AfterCaptureResult.Skipped(
                action,
                "No file artifact available for after-capture action"));
        }

        try
        {
            var shellResult = await artifactShellAction.ExecuteAsync(
                artifactPath,
                MapAction(action),
                cancellationToken);

            if (shellResult.IsSuccess)
            {
                Logger.LogInformation(
                    "operation=AfterCapture, stage=Complete, action={Action}, path={Path}",
                    action,
                    artifactPath);
                return outputResult.WithAfterCapture(AfterCaptureResult.Success(action, artifactPath));
            }

            Logger.LogWarning(
                "operation=AfterCapture, stage=ShellAction, action={Action}, path={Path}, detail={Detail}",
                action,
                artifactPath,
                shellResult.TechnicalDetail);
            return outputResult.WithAfterCapture(AfterCaptureResult.Failed(
                action,
                artifactPath,
                shellResult.TechnicalDetail));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(
                ex,
                "operation=AfterCapture, stage=Exception, action={Action}, path={Path}",
                action,
                artifactPath);
            return outputResult.WithAfterCapture(AfterCaptureResult.Failed(action, artifactPath, ex.Message));
        }
    }

    private static ArtifactShellActionKind MapAction(OutputAfterCaptureAction action) =>
        action switch
        {
            OutputAfterCaptureAction.Open => ArtifactShellActionKind.Open,
            OutputAfterCaptureAction.Reveal => ArtifactShellActionKind.Reveal,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unsupported after-capture action."),
        };
}
