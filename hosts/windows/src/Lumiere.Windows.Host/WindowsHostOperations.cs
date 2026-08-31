using Lumiere.Windows.Capture;
using Lumiere.Windows.Graphics.Output;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lumiere.Windows.Host;

public interface IWindowsCaptureEngine : IAsyncDisposable
{
    Task<WindowsCaptureResult> CaptureDisplayAsync(
        WindowsCaptureRequest request,
        CancellationToken cancellationToken = default);

    Task<WindowsCaptureResult> CaptureRegionAsync(
        WindowsCaptureRequest request,
        WindowsTargetCapability target,
        WindowsRegionGeometry geometry,
        CancellationToken cancellationToken = default);
}

public sealed class WindowsHostOperations : IWindowsHostOperations
{
    private readonly object sync = new();
    private readonly Func<IWindowsCaptureEngine> engineFactory;
    private readonly Func<WindowsTargetCapability?> getTargetCapability;
    private readonly Func<string> targetTokenFactory;
    private readonly Func<string> outputDirectory;
    private readonly Action<string> createDirectory;
    private readonly ILogger logger;
    private IWindowsCaptureEngine? engine;
    private IssuedTarget? issuedTarget;
    private int disposed;

    public WindowsHostOperations(
        Func<IWindowsCaptureEngine> engineFactory,
        Func<WindowsTargetCapability?> getTargetCapability,
        Func<string> targetTokenFactory,
        Func<string> outputDirectory,
        Action<string> createDirectory,
        ILogger? logger = null)
    {
        this.engineFactory = engineFactory ?? throw new ArgumentNullException(nameof(engineFactory));
        this.getTargetCapability = getTargetCapability
            ?? throw new ArgumentNullException(nameof(getTargetCapability));
        this.targetTokenFactory = targetTokenFactory
            ?? throw new ArgumentNullException(nameof(targetTokenFactory));
        this.outputDirectory = outputDirectory ?? throw new ArgumentNullException(nameof(outputDirectory));
        this.createDirectory = createDirectory ?? throw new ArgumentNullException(nameof(createDirectory));
        this.logger = logger ?? NullLogger.Instance;
    }

    public static WindowsHostOperations CreateDefault(ILogger? logger = null)
    {
        var capabilityProvider = new WindowsTargetCapabilityProvider();
        return new WindowsHostOperations(
            static () => new WindowsDisplayCaptureEngineAdapter(
                WindowsDisplayCaptureEngine.CreateDefault()),
            capabilityProvider.GetCurrent,
            static () => Guid.NewGuid().ToString("N"),
            WindowsCaptureFolder.GetDefaultPath,
            static path => _ = Directory.CreateDirectory(path),
            logger);
    }

    public HostCapabilities GetCapabilities()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
        var target = getTargetCapability();
        var activeTarget = CreateActiveTarget(target);
        var supportsRegion = target?.SupportsRegionCapture == true && activeTarget is not null;
        lock (sync)
        {
            issuedTarget = supportsRegion
                ? new IssuedTarget(activeTarget!.Id, target!)
                : null;
        }

        return new HostCapabilities(
            PlatformProtocol.ContractVersion,
            "windows",
            "available",
            supportsRegion ? ["region", "display"] : ["display"],
            ["clipboard", "folder"],
            MapHdrCapture(target?.HdrState),
            ["srgb-visual-match"],
            activeTarget);
    }

    public async Task<HostCaptureResult> CaptureAsync(
        string requestId,
        HostCaptureRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestId);
        ArgumentNullException.ThrowIfNull(request);
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);

        if (request.Mode is not ("display" or "region"))
        {
            return Failed(
                "capture-unavailable",
                "The requested capture mode is unavailable.",
                retryable: false);
        }

        IssuedTarget? regionTarget = null;
        WindowsRegionGeometry? regionGeometry = null;
        if (request.Mode == "region")
        {
            if (request.TargetId is null || request.Geometry is null)
            {
                return Failed(
                    "capture-unavailable",
                    "Region capture requires a current target and valid selection.",
                    retryable: false);
            }

            lock (sync)
            {
                if (issuedTarget?.Token == request.TargetId)
                {
                    regionTarget = issuedTarget;
                    issuedTarget = null;
                }
            }

            if (regionTarget is null)
            {
                return Failed(
                    "capture-unavailable",
                    "The capture target changed. Select the region again.",
                    retryable: true);
            }

            regionGeometry = new WindowsRegionGeometry(
                request.Geometry.X,
                request.Geometry.Y,
                request.Geometry.Width,
                request.Geometry.Height);
        }

        if (!TryMapDelivery(request.Delivery, out var delivery))
        {
            return Failed(
                "delivery-unavailable",
                "Windows capture supports clipboard, folder, or both delivery.",
                retryable: false);
        }

        try
        {
            string? directory = null;
            if (delivery is OutputTarget.Folder or OutputTarget.Both)
            {
                directory = request.SaveDirectory ?? outputDirectory();
                if (string.IsNullOrWhiteSpace(directory))
                {
                    throw new InvalidOperationException("The Windows capture folder could not be resolved.");
                }

                try
                {
                    createDirectory(directory);
                }
                catch (Exception exception)
                {
                    logger.LogWarning(
                        exception,
                        "operation=CaptureDirectory, stage=CreateFailed, correlation={CorrelationId}",
                        requestId);
                }
            }

            var captureRequest = new WindowsCaptureRequest(requestId, delivery, directory);
            var captureResult = request.Mode == "region"
                ? await GetOrCreateEngine().CaptureRegionAsync(
                    captureRequest,
                    regionTarget!.Target,
                    regionGeometry!,
                    cancellationToken)
                : await GetOrCreateEngine().CaptureDisplayAsync(
                    captureRequest,
                    cancellationToken);
            return MapCaptureResult(captureResult, delivery, requestId, request.Mode);
        }
        catch (OperationCanceledException)
        {
            return new HostCaptureResult("cancelled");
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "operation=CaptureRequest, stage=Failed, correlation={CorrelationId}",
                requestId);
            return Failed(
                "unexpected-failure",
                "Windows capture failed. Try again.",
                retryable: true);
        }
    }

    private IWindowsCaptureEngine GetOrCreateEngine()
    {
        lock (sync)
        {
            ObjectDisposedException.ThrowIf(disposed != 0, this);
            return engine ??= engineFactory();
        }
    }

    private HostCaptureTarget? CreateActiveTarget(WindowsTargetCapability? target)
    {
        if (target?.LogicalSize is not { } logicalSize)
        {
            return null;
        }

        var token = targetTokenFactory();
        return string.IsNullOrWhiteSpace(token)
            ? null
            : new HostCaptureTarget(
                token,
                new HostLogicalSize(logicalSize.Width, logicalSize.Height));
    }

    private static string MapHdrCapture(WindowsTargetHdrState? state) =>
        state switch
        {
            WindowsTargetHdrState.Active => "supported",
            WindowsTargetHdrState.Inactive => "unavailable",
            _ => "unvalidated",
        };

    private static bool TryMapDelivery(string value, out OutputTarget delivery)
    {
        delivery = value switch
        {
            "clipboard" => OutputTarget.Clipboard,
            "folder" => OutputTarget.Folder,
            "both" => OutputTarget.Both,
            _ => default,
        };
        return value is "clipboard" or "folder" or "both";
    }

    private HostCaptureResult MapCaptureResult(
        WindowsCaptureResult result,
        OutputTarget requestedDelivery,
        string requestId,
        string captureMode)
    {
        if (result.Outcome == WindowsCaptureOutcome.Cancelled)
        {
            return new HostCaptureResult("cancelled");
        }

        if (result.Output is { } output
            && result.Outcome is WindowsCaptureOutcome.Delivered
                or WindowsCaptureOutcome.DeliveryFailed)
        {
            return Completed(
                result.HdrCapability?.IsHdrActive == true ? "hdr" : "sdr",
                RequestedTargets(requestedDelivery)
                    .Select(target => MapDeliveryResult(
                        output,
                        target,
                        result.TechnicalDetail,
                        requestId))
                    .ToArray());
        }

        logger.LogWarning(
            "operation=CaptureResult, stage={Outcome}, correlation={CorrelationId}, detail={Detail}",
            result.Outcome,
            requestId,
            result.TechnicalDetail);
        return result.Outcome switch
        {
            WindowsCaptureOutcome.TimedOut => Failed(
                "capture-unavailable",
                "Windows capture timed out. Try again.",
                retryable: true),
            WindowsCaptureOutcome.Unavailable => Failed(
                "capture-unavailable",
                captureMode == "region"
                    ? "The capture target changed. Select the region again."
                    : "The capture target is unavailable. Try again.",
                retryable: true),
            WindowsCaptureOutcome.Unsupported => Failed(
                "capture-unavailable",
                "Screen capture is unavailable on this Windows system.",
                retryable: false),
            _ => Failed(
                "unexpected-failure",
                "Windows capture failed. Try again.",
                retryable: true),
        };
    }

    private static HostCaptureResult Completed(
        string sourceDynamicRange,
        IReadOnlyList<HostDeliveryResult> deliveries) =>
        new(
            "completed",
            sourceDynamicRange,
            "srgb-visual-match",
            deliveries);

    private static IReadOnlyList<OutputTarget> RequestedTargets(OutputTarget delivery) =>
        delivery switch
        {
            OutputTarget.Clipboard => [OutputTarget.Clipboard],
            OutputTarget.Folder => [OutputTarget.Folder],
            OutputTarget.Both => [OutputTarget.Clipboard, OutputTarget.Folder],
            _ => throw new ArgumentOutOfRangeException(nameof(delivery), delivery, null),
        };

    private HostDeliveryResult MapDeliveryResult(
        OutputResult output,
        OutputTarget target,
        string fallbackDetail,
        string requestId)
    {
        var targetResult = output.Targets.FirstOrDefault(result => result.Target == target);
        var protocolTarget = target == OutputTarget.Clipboard ? "clipboard" : "folder";
        if (targetResult?.Outcome == OutputOutcome.Success
            && (target != OutputTarget.Folder
                || !string.IsNullOrWhiteSpace(targetResult.ArtifactPath)))
        {
            return new HostDeliveryResult(
                protocolTarget,
                "success",
                target == OutputTarget.Folder ? targetResult.ArtifactPath : null);
        }

        var message = targetResult?.TechnicalDetail
            ?? $"{protocolTarget} delivery did not return a result. {fallbackDetail}";
        logger.LogWarning(
            "operation=CaptureDelivery, stage=Failed, correlation={CorrelationId}, target={Target}, detail={Detail}",
            requestId,
            protocolTarget,
            message);
        return new HostDeliveryResult(
            protocolTarget,
            "failed",
            Failure: new PlatformFailure(
                "delivery-failed",
                target == OutputTarget.Clipboard
                    ? "Could not copy the screenshot to the clipboard."
                    : "Could not save the screenshot.",
                Retryable: true));
    }

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

        IWindowsCaptureEngine? ownedEngine;
        lock (sync)
        {
            ownedEngine = engine;
            engine = null;
            issuedTarget = null;
        }

        if (ownedEngine is not null)
        {
            await ownedEngine.DisposeAsync();
        }
    }

    private sealed class WindowsDisplayCaptureEngineAdapter(WindowsDisplayCaptureEngine inner)
        : IWindowsCaptureEngine
    {
        public Task<WindowsCaptureResult> CaptureDisplayAsync(
            WindowsCaptureRequest request,
            CancellationToken cancellationToken = default) =>
            inner.CaptureDisplayAsync(request, cancellationToken);

        public Task<WindowsCaptureResult> CaptureRegionAsync(
            WindowsCaptureRequest request,
            WindowsTargetCapability target,
            WindowsRegionGeometry geometry,
            CancellationToken cancellationToken = default) =>
            inner.CaptureRegionAsync(request, target, geometry, cancellationToken);

        public ValueTask DisposeAsync() => inner.DisposeAsync();
    }

    private sealed record IssuedTarget(string Token, WindowsTargetCapability Target);
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
