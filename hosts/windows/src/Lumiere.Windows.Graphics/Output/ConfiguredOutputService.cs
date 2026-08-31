using Lumiere.Windows.Graphics.Clipboard;
using Lumiere.Windows.Graphics.Devices;
using Lumiere.Windows.Graphics.Presentation;

namespace Lumiere.Windows.Graphics.Output;

/// <summary>
/// Routes one explicit output request and shares its encoded artifact across targets.
/// </summary>
internal sealed class ConfiguredOutputService : IOutputService
{
    private static readonly TimeSpan DefaultTargetTimeout = TimeSpan.FromSeconds(10);
    private readonly IOutputPngEncoder encoder;
    private readonly IOutputTargetAdapter clipboardOutput;
    private readonly IOutputTargetAdapter folderOutput;
    private readonly TimeSpan targetTimeout;

    internal ConfiguredOutputService(
        IOutputPngEncoder encoder,
        IOutputTargetAdapter clipboardOutput,
        IOutputTargetAdapter folderOutput,
        TimeSpan? targetTimeout = null)
    {
        this.encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
        this.clipboardOutput = clipboardOutput ?? throw new ArgumentNullException(nameof(clipboardOutput));
        this.folderOutput = folderOutput ?? throw new ArgumentNullException(nameof(folderOutput));
        this.targetTimeout = targetTimeout ?? DefaultTargetTimeout;
        if (this.targetTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(targetTimeout), this.targetTimeout, "Target timeout must be positive.");
        }
    }

    public static ConfiguredOutputService CreateDefault(GraphicsDeviceResources deviceResources)
    {
        ArgumentNullException.ThrowIfNull(deviceResources);
        var encoder = new SrgbVisualMatchPngEncoder(
            new SrgbVisualMatchConverter(
                new CapturedFrameTextureReadback(deviceResources)));
        return new ConfiguredOutputService(
            encoder,
            new ClipboardOutputService(),
            new FolderOutputService());
    }

    public async Task<OutputResult> ExecuteOutputAsync(
        OutputRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var outputs = RequestedOutputs(request).ToArray();
        if (outputs.Length == 0)
        {
            return OutputResult.FromTargets(OutputTargetResult.Skipped(
                request.Delivery,
                "No output target was requested"));
        }

        var artifact = await encoder.EncodeArtifactAsync(
            request.Texture,
            request.CropRegion,
            request.VisualMatchContext,
            cancellationToken);

        var results = new List<OutputTargetResult>(outputs.Length);
        foreach (var output in outputs)
        {
            results.Add(await ExecuteTargetAsync(output, request, artifact, cancellationToken));
        }

        return OutputResult.FromTargets(results);
    }

    public async Task<byte[]> EncodePngAsync(
        CapturedFrameTexture texture,
        CropPixelRect? cropRegion,
        SrgbVisualMatchConversionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(texture);
        var artifact = await encoder.EncodeArtifactAsync(
            texture,
            cropRegion,
            context,
            cancellationToken);
        return artifact.Bytes;
    }

    private IEnumerable<IOutputTargetAdapter> RequestedOutputs(OutputRequest request)
    {
        if (request.ShouldWriteClipboard)
        {
            yield return clipboardOutput;
        }

        if (request.ShouldWriteFolder)
        {
            yield return folderOutput;
        }
    }

    private async Task<OutputTargetResult> ExecuteTargetAsync(
        IOutputTargetAdapter output,
        OutputRequest request,
        OutputEncodedArtifact artifact,
        CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(targetTimeout);
            return await output.DeliverAsync(request, artifact, timeoutSource.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return OutputTargetResult.Failed(output.Target, $"{output.Target} output timed out");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return OutputTargetResult.Failed(
                output.Target,
                $"{output.Target} output failed",
                exception.Message);
        }
    }
}
