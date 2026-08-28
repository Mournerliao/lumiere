using Lumiere.Windows.Graphics.Devices;
using Lumiere.Windows.Graphics.Hdr;
using Lumiere.Windows.Graphics.Output;
using Lumiere.Windows.Graphics.Presentation;
using Lumiere.Windows.Interop;
using Lumiere.Windows.Interop.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Lumiere.Windows.Capture;

/// <summary>
/// Owns one complete display-capture operation, from target resolution through delivery and teardown.
/// The platform Host maps protocol requests to this interface and never owns native frame resources.
/// </summary>
public sealed class WindowsDisplayCaptureEngine : IAsyncDisposable
{
    private static readonly Lazy<ILogger> LoggerHolder = new(
        () => LumiereLoggerFactory.CreateLogger(LogCategories.Capture));
    private static readonly TimeSpan DefaultFrameTimeout = TimeSpan.FromSeconds(10);

    private readonly Func<CaptureCommand, CaptureCommandResult> reserveCommand;
    private readonly Action<CaptureSessionState> updateSessionState;
    private readonly Func<CancellationToken, Task<CaptureTargetSelectionResult>> selectTargetAsync;
    private readonly Func<CaptureTarget, HdrDisplayCapability> probeHdrCapability;
    private readonly Func<
        CaptureTarget,
        Action<CapturedFrameTexture>,
        Action<EngineReadinessStatus>,
        CaptureStartResult> startCapture;
    private readonly IOutputService output;
    private readonly IDisposable? ownedResources;
    private readonly TimeSpan frameTimeout;
    private readonly SemaphoreSlim operationGate = new(1, 1);
    private readonly CancellationTokenSource lifetimeCancellation = new();
    private int disposed;

    internal WindowsDisplayCaptureEngine(
        Func<CaptureCommand, CaptureCommandResult> reserveCommand,
        Action<CaptureSessionState> updateSessionState,
        Func<CancellationToken, Task<CaptureTargetSelectionResult>> selectTargetAsync,
        Func<CaptureTarget, HdrDisplayCapability> probeHdrCapability,
        Func<CaptureTarget, Action<CapturedFrameTexture>, Action<EngineReadinessStatus>, CaptureStartResult> startCapture,
        IOutputService output,
        IDisposable? ownedResources = null,
        TimeSpan? frameTimeout = null)
    {
        this.reserveCommand = reserveCommand ?? throw new ArgumentNullException(nameof(reserveCommand));
        this.updateSessionState = updateSessionState ?? throw new ArgumentNullException(nameof(updateSessionState));
        this.selectTargetAsync = selectTargetAsync ?? throw new ArgumentNullException(nameof(selectTargetAsync));
        this.probeHdrCapability = probeHdrCapability ?? throw new ArgumentNullException(nameof(probeHdrCapability));
        this.startCapture = startCapture ?? throw new ArgumentNullException(nameof(startCapture));
        this.output = output ?? throw new ArgumentNullException(nameof(output));
        this.ownedResources = ownedResources;
        this.frameTimeout = frameTimeout ?? DefaultFrameTimeout;
        if (this.frameTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(frameTimeout), this.frameTimeout, "Frame timeout must be positive.");
        }
    }

    public static WindowsDisplayCaptureEngine CreateDefault()
    {
        var deviceResources = new GraphicsDeviceProvider().CreateDevice();
        try
        {
            var captureService = new CaptureService(deviceResources);
            var targetSelection = new DirectMonitorCaptureTargetSelectionService(
                MonitorSelectionInterop.GetCurrentMonitorFromCursor,
                WindowsDisplayTargetFactory.Create);
            var output = ConfiguredOutputService.CreateDefault(deviceResources);

            return new WindowsDisplayCaptureEngine(
                captureService.TryReserveCommand,
                captureService.UpdateSessionState,
                targetSelection.SelectTargetAsync,
                WindowsDisplayTargetFactory.ProbeHdrCapability,
                captureService.StartCapture,
                output,
                deviceResources);
        }
        catch
        {
            deviceResources.Dispose();
            throw;
        }
    }

    public static void ConfigureLogging(ILoggerFactory loggerFactory) =>
        LumiereLoggerFactory.Configure(loggerFactory);

    public async Task<WindowsCaptureResult> CaptureDisplayAsync(
        WindowsCaptureRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ThrowIfDisposed();

        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            lifetimeCancellation.Token);
        var token = operationCancellation.Token;

        try
        {
            await operationGate.WaitAsync(token);
        }
        catch (OperationCanceledException)
        {
            return Cancelled("Capture was cancelled before it started.");
        }

        try
        {
            ThrowIfDisposed();
            using var diagnosticScope = SessionDiagnosticScope.Begin(
                LoggerHolder.Value,
                correlationId: request.CorrelationId);

            var command = CaptureCommand.Fullscreen();
            var reservation = reserveCommand(command);
            if (!reservation.IsAccepted)
            {
                return new WindowsCaptureResult(
                    WindowsCaptureOutcome.Failed,
                    "Capture is already active",
                    reservation.Readiness?.TechnicalDetail ?? "The Windows capture engine rejected the request.");
            }

            return await CaptureReservedDisplayAsync(request, token);
        }
        catch (OperationCanceledException)
        {
            return Cancelled("Capture was cancelled by the caller.");
        }
        catch (Exception exception)
        {
            DiagnosticContext.CaptureFailure(
                stage: "DisplayCapture",
                userFacingState: "Capture failed",
                technicalDetail: exception.Message,
                correlationId: request.CorrelationId,
                exception: exception).LogTo(LoggerHolder.Value);
            return new WindowsCaptureResult(
                WindowsCaptureOutcome.Failed,
                "Capture failed",
                $"{exception.GetType().Name}: {exception.Message}");
        }
        finally
        {
            updateSessionState(CaptureSessionState.Idle());
            operationGate.Release();
        }
    }

    private async Task<WindowsCaptureResult> CaptureReservedDisplayAsync(
        WindowsCaptureRequest request,
        CancellationToken cancellationToken)
    {
        var selection = await selectTargetAsync(cancellationToken);
        updateSessionState(CaptureSessionState.FromSelectionResult(selection));
        if (!selection.IsSelected)
        {
            var outcome = selection.IsUnsupported
                ? WindowsCaptureOutcome.Unsupported
                : selection.IsCanceled
                    ? WindowsCaptureOutcome.Cancelled
                    : WindowsCaptureOutcome.Failed;
            return new WindowsCaptureResult(
                outcome,
                selection.Readiness.UserMessage,
                selection.Readiness.TechnicalDetail ?? selection.Readiness.UserMessage);
        }

        var target = selection.Target;
        var hdrCapability = probeHdrCapability(target);
        var frameCompletion = new TaskCompletionSource<FrameCaptureResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var acceptingFrames = 1;

        void ReceiveFrame(CapturedFrameTexture frame)
        {
            if (Volatile.Read(ref acceptingFrames) == 0
                || !frameCompletion.TrySetResult(FrameCaptureResult.Succeeded(frame)))
            {
                frame.Dispose();
            }
        }

        void ReceiveFailure(EngineReadinessStatus readiness) =>
            frameCompletion.TrySetResult(FrameCaptureResult.Failed(readiness));

        using var cancellationRegistration = cancellationToken.Register(() =>
        {
            Volatile.Write(ref acceptingFrames, 0);
            frameCompletion.TrySetCanceled(cancellationToken);
        });

        var startResult = startCapture(target, ReceiveFrame, ReceiveFailure);
        updateSessionState(CaptureSessionState.FromStartResult(target, startResult));
        if (!startResult.Started)
        {
            return new WindowsCaptureResult(
                startResult.Readiness.State == EngineReadinessState.Unsupported
                    ? WindowsCaptureOutcome.Unsupported
                    : WindowsCaptureOutcome.Failed,
                startResult.Readiness.UserMessage,
                startResult.Readiness.TechnicalDetail ?? startResult.Readiness.UserMessage,
                hdrCapability);
        }

        using var sessionResources = startResult.SessionResources!;
        FrameCaptureResult frameResult;
        try
        {
            frameResult = await frameCompletion.Task.WaitAsync(frameTimeout, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Volatile.Write(ref acceptingFrames, 0);
            if (!frameCompletion.TrySetCanceled(cancellationToken)
                && frameCompletion.Task.IsCompletedSuccessfully)
            {
                frameCompletion.Task.Result.Texture?.Dispose();
            }

            throw;
        }
        catch (TimeoutException)
        {
            Volatile.Write(ref acceptingFrames, 0);
            if (!frameCompletion.TrySetCanceled()
                && frameCompletion.Task.IsCompletedSuccessfully)
            {
                frameCompletion.Task.Result.Texture?.Dispose();
            }

            return new WindowsCaptureResult(
                WindowsCaptureOutcome.TimedOut,
                "Capture timed out",
                $"No frame arrived within {frameTimeout}.",
                hdrCapability);
        }
        finally
        {
            Volatile.Write(ref acceptingFrames, 0);
        }

        if (frameResult.Texture is null)
        {
            var readiness = frameResult.Readiness!;
            return new WindowsCaptureResult(
                WindowsCaptureOutcome.Failed,
                readiness.UserMessage,
                readiness.TechnicalDetail ?? readiness.UserMessage,
                hdrCapability);
        }

        using var texture = frameResult.Texture;
        updateSessionState(CaptureSessionState.Capturing(
            target,
            EngineReadinessStatus.Ready(
                "Captured frame is ready for sRGB Visual Match conversion.",
                $"Captured {texture.Width}x{texture.Height} from {target.DisplayName}.")));

        var outputResult = await output.ExecuteOutputAsync(
            new OutputRequest
            {
                Texture = texture,
                Delivery = request.Delivery,
                SaveDirectory = request.SaveDirectory,
                TimestampNaming = request.TimestampNaming,
            },
            cancellationToken);

        return new WindowsCaptureResult(
            outputResult.IsSuccess
                ? WindowsCaptureOutcome.Delivered
                : WindowsCaptureOutcome.DeliveryFailed,
            outputResult.UserMessage,
            outputResult.TechnicalDetail,
            hdrCapability,
            outputResult);
    }

    private static WindowsCaptureResult Cancelled(string detail) =>
        new(WindowsCaptureOutcome.Cancelled, "Capture cancelled", detail);

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
        {
            return;
        }

        await lifetimeCancellation.CancelAsync();
        await operationGate.WaitAsync();
        try
        {
            ownedResources?.Dispose();
        }
        finally
        {
            operationGate.Release();
            operationGate.Dispose();
            lifetimeCancellation.Dispose();
        }
    }

    private sealed record FrameCaptureResult(
        CapturedFrameTexture? Texture,
        EngineReadinessStatus? Readiness)
    {
        public static FrameCaptureResult Succeeded(CapturedFrameTexture texture) =>
            new(texture, null);

        public static FrameCaptureResult Failed(EngineReadinessStatus readiness) =>
            new(null, readiness);
    }
}
