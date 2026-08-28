using Lumiere.Windows.Capture;
using Lumiere.Windows.Graphics.Output;

namespace Lumiere.Windows.Host;

public interface IWindowsDisplayCaptureEngine : IAsyncDisposable
{
    Task<WindowsCaptureResult> CaptureDisplayAsync(
        WindowsCaptureRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class WindowsHostOperations : IWindowsHostOperations
{
    private readonly object sync = new();
    private readonly Func<IWindowsDisplayCaptureEngine> engineFactory;
    private readonly Func<string> outputDirectory;
    private readonly Action<string> createDirectory;
    private IWindowsDisplayCaptureEngine? engine;
    private int disposed;

    public WindowsHostOperations(
        Func<IWindowsDisplayCaptureEngine> engineFactory,
        Func<string> outputDirectory,
        Action<string> createDirectory)
    {
        this.engineFactory = engineFactory ?? throw new ArgumentNullException(nameof(engineFactory));
        this.outputDirectory = outputDirectory ?? throw new ArgumentNullException(nameof(outputDirectory));
        this.createDirectory = createDirectory ?? throw new ArgumentNullException(nameof(createDirectory));
    }

    public static WindowsHostOperations CreateDefault() =>
        new(
            static () => new WindowsDisplayCaptureEngineAdapter(
                WindowsDisplayCaptureEngine.CreateDefault()),
            WindowsCaptureFolder.GetDefaultPath,
            static path => _ = Directory.CreateDirectory(path));

    public HostCapabilities GetCapabilities() =>
        new(
            PlatformProtocol.ContractVersion,
            "windows",
            "available",
            ["display"],
            ["folder"],
            "unvalidated",
            ["srgb-visual-match"]);

    public async Task<HostCaptureResult> CaptureAsync(
        string requestId,
        HostCaptureRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestId);
        ArgumentNullException.ThrowIfNull(request);
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);

        if (request.Mode != "display")
        {
            return Failed(
                "capture-unavailable",
                "Region capture is not available on Windows yet.",
                retryable: false);
        }

        if (request.Delivery != "folder")
        {
            return Failed(
                "delivery-unavailable",
                "Windows capture currently supports folder delivery only.",
                retryable: false);
        }

        try
        {
            var directory = outputDirectory();
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new InvalidOperationException("The Windows capture folder could not be resolved.");
            }

            createDirectory(directory);
            var captureResult = await GetOrCreateEngine().CaptureDisplayAsync(
                new WindowsCaptureRequest(requestId, OutputTarget.Folder, directory),
                cancellationToken);
            return MapCaptureResult(captureResult);
        }
        catch (OperationCanceledException)
        {
            return new HostCaptureResult("cancelled");
        }
        catch (Exception exception)
        {
            return Failed(
                "unexpected-failure",
                $"Windows capture failed: {exception.Message}",
                retryable: true);
        }
    }

    private IWindowsDisplayCaptureEngine GetOrCreateEngine()
    {
        lock (sync)
        {
            ObjectDisposedException.ThrowIf(disposed != 0, this);
            return engine ??= engineFactory();
        }
    }

    private static HostCaptureResult MapCaptureResult(WindowsCaptureResult result)
    {
        if (result.Outcome == WindowsCaptureOutcome.Cancelled)
        {
            return new HostCaptureResult("cancelled");
        }

        if (result.Output is { } output
            && result.Outcome is WindowsCaptureOutcome.Delivered
                or WindowsCaptureOutcome.DeliveryFailed)
        {
            var folder = output.Targets.FirstOrDefault(target => target.Target == OutputTarget.Folder);
            if (folder?.Outcome == OutputOutcome.Success
                && !string.IsNullOrWhiteSpace(folder.ArtifactPath))
            {
                return Completed(
                    result.HdrCapability?.IsHdrActive == true ? "hdr" : "sdr",
                    new HostDeliveryResult("folder", "success", folder.ArtifactPath));
            }

            var message = folder?.TechnicalDetail ?? result.TechnicalDetail;
            return Completed(
                result.HdrCapability?.IsHdrActive == true ? "hdr" : "sdr",
                new HostDeliveryResult(
                    "folder",
                    "failed",
                    Failure: new PlatformFailure("delivery-failed", message, Retryable: true)));
        }

        return result.Outcome switch
        {
            WindowsCaptureOutcome.TimedOut => Failed(
                "capture-unavailable",
                result.TechnicalDetail,
                retryable: true),
            WindowsCaptureOutcome.Unsupported => Failed(
                "capture-unavailable",
                result.TechnicalDetail,
                retryable: false),
            _ => Failed("unexpected-failure", result.TechnicalDetail, retryable: true),
        };
    }

    private static HostCaptureResult Completed(
        string sourceDynamicRange,
        HostDeliveryResult delivery) =>
        new(
            "completed",
            sourceDynamicRange,
            "srgb-visual-match",
            [delivery]);

    private static HostCaptureResult Failed(
        string code,
        string message,
        bool retryable) =>
        new("failed", Failure: new PlatformFailure(code, message, retryable));

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
        {
            return;
        }

        IWindowsDisplayCaptureEngine? ownedEngine;
        lock (sync)
        {
            ownedEngine = engine;
            engine = null;
        }

        if (ownedEngine is not null)
        {
            await ownedEngine.DisposeAsync();
        }
    }

    private sealed class WindowsDisplayCaptureEngineAdapter(WindowsDisplayCaptureEngine inner)
        : IWindowsDisplayCaptureEngine
    {
        public Task<WindowsCaptureResult> CaptureDisplayAsync(
            WindowsCaptureRequest request,
            CancellationToken cancellationToken = default) =>
            inner.CaptureDisplayAsync(request, cancellationToken);

        public ValueTask DisposeAsync() => inner.DisposeAsync();
    }
}

internal static class WindowsCaptureFolder
{
    public static string GetDefaultPath()
    {
        var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        if (string.IsNullOrWhiteSpace(pictures))
        {
            var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrWhiteSpace(profile))
            {
                throw new InvalidOperationException("The Windows user profile could not be resolved.");
            }

            pictures = Path.Combine(profile, "Pictures");
        }

        return Path.Combine(pictures, "Lumiere");
    }
}
