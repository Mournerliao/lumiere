using System.Runtime.InteropServices.WindowsRuntime;
using Lumiere.Windows.Graphics.Output;
using Lumiere.Windows.Interop.Diagnostics;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;

namespace Lumiere.Windows.Graphics.Clipboard;

internal sealed class ClipboardOutputService : IOutputTargetAdapter
{
    private static readonly ILogger Logger = LumiereLoggerFactory.CreateLogger(LogCategories.Graphics);

    internal ClipboardOutputService()
        : this(WriteToClipboardAsync)
    {
    }

    internal ClipboardOutputService(Func<byte[], CancellationToken, Task> writeAsync)
    {
        this.writeAsync = writeAsync ?? throw new ArgumentNullException(nameof(writeAsync));
    }

    private readonly Func<byte[], CancellationToken, Task> writeAsync;

    public OutputTarget Target => OutputTarget.Clipboard;

    public async Task<OutputTargetResult> DeliverAsync(
        OutputRequest request,
        OutputEncodedArtifact artifact,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(artifact);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await writeAsync(artifact.Bytes, cancellationToken);
            Logger.LogInformation(
                "operation=ClipboardOutput, stage=Complete, bytes={Bytes}, profile={Profile}",
                artifact.Bytes.Length,
                OutputEncodedArtifact.Profile);
            return OutputTargetResult.Success(
                OutputTarget.Clipboard,
                "Copied to clipboard",
                $"Clipboard output success: {artifact.Bytes.Length} bytes",
                bytesWritten: artifact.Bytes.Length);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            DiagnosticContext.OutputFailure(
                stage: "ClipboardWrite",
                userFacingState: "Failed to copy to clipboard",
                technicalDetail: exception.Message,
                exception: exception).LogTo(Logger);
            return OutputTargetResult.Failed(
                OutputTarget.Clipboard,
                "Failed to copy to clipboard",
                exception.Message);
        }
    }

    private static async Task WriteToClipboardAsync(
        byte[] pngBytes,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = new InMemoryRandomAccessStream();
        await stream.WriteAsync(pngBytes.AsBuffer());
        cancellationToken.ThrowIfCancellationRequested();
        stream.Seek(0);

        var reference = RandomAccessStreamReference.CreateFromStream(stream);
        var dataPackage = new DataPackage();
        dataPackage.SetBitmap(reference);
        global::Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        global::Windows.ApplicationModel.DataTransfer.Clipboard.Flush();
    }
}
