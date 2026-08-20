namespace Lumiere.Windows.Graphics.Output;

/// <summary>
/// Routes one explicit output request and shares its encoded artifact across targets.
/// </summary>
public sealed class ConfiguredOutputService : IOutputService
{
    private static readonly TimeSpan DefaultTargetTimeout = TimeSpan.FromSeconds(10);
    private readonly IOutputService clipboardOutput;
    private readonly IOutputService folderOutput;
    private readonly TimeSpan targetTimeout;

    public ConfiguredOutputService(
        IOutputService clipboardOutput,
        IOutputService folderOutput,
        TimeSpan? targetTimeout = null)
    {
        this.clipboardOutput = clipboardOutput ?? throw new ArgumentNullException(nameof(clipboardOutput));
        this.folderOutput = folderOutput ?? throw new ArgumentNullException(nameof(folderOutput));
        this.targetTimeout = targetTimeout ?? DefaultTargetTimeout;
        if (this.targetTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(targetTimeout), this.targetTimeout, "Target timeout must be positive.");
        }
    }

    public async Task<OutputResult> ExecuteOutputAsync(
        OutputRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request = request.Delivery is OutputTarget.Both && request.ArtifactCache is null
            ? request with { ArtifactCache = new OutputArtifactCache() }
            : request;

        var results = new List<OutputTargetResult>();
        if (request.Delivery is OutputTarget.Clipboard or OutputTarget.Both)
        {
            results.AddRange(await ExecuteTargetAsync(
                OutputTarget.Clipboard,
                clipboardOutput,
                request,
                cancellationToken));
        }

        if (request.Delivery is OutputTarget.Folder or OutputTarget.Both)
        {
            results.AddRange(await ExecuteTargetAsync(
                OutputTarget.Folder,
                folderOutput,
                request,
                cancellationToken));
        }

        return OutputResult.FromTargets(results);
    }

    private async Task<IReadOnlyList<OutputTargetResult>> ExecuteTargetAsync(
        OutputTarget target,
        IOutputService output,
        OutputRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(targetTimeout);
            return (await output.ExecuteOutputAsync(request, timeoutSource.Token)).Targets;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return [OutputTargetResult.Failed(target, $"{target} output timed out")];
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return [OutputTargetResult.Failed(target, $"{target} output failed", exception.Message)];
        }
    }
}
