namespace Lumiere.Graphics.Output;

public sealed class ConfiguredOutputService : IOutputService
{
    private readonly IOutputService clipboardOutput;
    private readonly IOutputService folderOutput;

    public ConfiguredOutputService(IOutputService clipboardOutput, IOutputService folderOutput)
    {
        this.clipboardOutput = clipboardOutput ?? throw new ArgumentNullException(nameof(clipboardOutput));
        this.folderOutput = folderOutput ?? throw new ArgumentNullException(nameof(folderOutput));
    }

    public async Task<OutputResult> ExecuteOutputAsync(OutputRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var results = new List<OutputTargetResult>();
        if (request.Policy.Target is OutputTarget.Clipboard)
        {
            results.AddRange((await clipboardOutput.ExecuteOutputAsync(request, cancellationToken)).Targets);
        }
        else if (request.Policy.Target is OutputTarget.Folder)
        {
            results.AddRange((await folderOutput.ExecuteOutputAsync(request, cancellationToken)).Targets);
        }
        else
        {
            results.AddRange((await clipboardOutput.ExecuteOutputAsync(request, cancellationToken)).Targets);
            results.AddRange((await folderOutput.ExecuteOutputAsync(request, cancellationToken)).Targets);
        }

        return OutputResult.FromTargets(results);
    }
}
