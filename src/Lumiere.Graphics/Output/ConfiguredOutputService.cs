namespace Lumiere.Graphics.Output;

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

    public async Task<OutputResult> ExecuteOutputAsync(OutputRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var results = new List<OutputTargetResult>();
        if (request.Policy.Target is OutputTarget.Clipboard)
        {
            results.AddRange(await ExecuteTargetAsync(OutputTarget.Clipboard, clipboardOutput, request, cancellationToken));
        }
        else if (request.Policy.Target is OutputTarget.Folder)
        {
            results.AddRange(await ExecuteTargetAsync(OutputTarget.Folder, folderOutput, request, cancellationToken));
        }
        else
        {
            results.AddRange(await ExecuteTargetAsync(OutputTarget.Clipboard, clipboardOutput, request, cancellationToken));
            results.AddRange(await ExecuteTargetAsync(OutputTarget.Folder, folderOutput, request, cancellationToken));
        }

        return OutputResult.FromTargets(results)
            .WithOutputPolicy(request.Policy);
    }

    private async Task<IReadOnlyList<OutputTargetResult>> ExecuteTargetAsync(
        OutputTarget target,
        IOutputService service,
        OutputRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(targetTimeout);
            return (await service.ExecuteOutputAsync(request, timeoutSource.Token)).Targets;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return [OutputTargetResult.Failed(target, $"{FormatTarget(target)} output timed out", $"{target} output exceeded {targetTimeout}.")];
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return [OutputTargetResult.Failed(target, $"{FormatTarget(target)} output failed", ex.Message)];
        }
    }

    private static string FormatTarget(OutputTarget target) =>
        target switch
        {
            OutputTarget.Folder => "Folder",
            _ => "Clipboard",
        };
}
