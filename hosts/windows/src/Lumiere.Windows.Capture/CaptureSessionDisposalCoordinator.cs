using Lumiere.Windows.Interop.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Lumiere.Windows.Capture;

internal static class CaptureSessionDisposalCoordinator
{
    private static readonly ILogger Logger = LumiereLoggerFactory.CreateLogger(LogCategories.Capture);

    public static CaptureSessionDisposalResult DisposeOnce(
        Action unsubscribeFrameHandler,
        Action stopSession,
        Action disposeFramePool,
        Action disposeDevice)
    {
        ArgumentNullException.ThrowIfNull(unsubscribeFrameHandler);
        ArgumentNullException.ThrowIfNull(stopSession);
        ArgumentNullException.ThrowIfNull(disposeFramePool);
        ArgumentNullException.ThrowIfNull(disposeDevice);

        Exception? firstException = null;
        var frameHandlerUnsubscribed = false;
        var sessionStopped = false;
        var framePoolDisposed = false;
        var deviceDisposed = false;

        Logger.LogDebug("operation=CaptureTeardown, stage=1/4, detail=Unsubscribing frame handler");
        try { unsubscribeFrameHandler(); frameHandlerUnsubscribed = true; }
        catch (Exception ex) { Logger.LogError(ex, "operation=CaptureTeardown, stage=1/4, detail=Frame handler unsubscribe failed"); firstException ??= ex; }

        Logger.LogDebug("operation=CaptureTeardown, stage=2/4, detail=Stopping capture session");
        try { stopSession(); sessionStopped = true; }
        catch (Exception ex) { Logger.LogError(ex, "operation=CaptureTeardown, stage=2/4, detail=Capture session stop failed"); firstException ??= ex; }

        Logger.LogDebug("operation=CaptureTeardown, stage=3/4, detail=Disposing frame pool");
        try { disposeFramePool(); framePoolDisposed = true; }
        catch (Exception ex) { Logger.LogError(ex, "operation=CaptureTeardown, stage=3/4, detail=Frame pool dispose failed"); firstException ??= ex; }

        Logger.LogDebug("operation=CaptureTeardown, stage=4/4, detail=Disposing D3D11 device");
        try { disposeDevice(); deviceDisposed = true; }
        catch (Exception ex) { Logger.LogError(ex, "operation=CaptureTeardown, stage=4/4, detail=D3D11 device dispose failed"); firstException ??= ex; }

        if (firstException is not null)
        {
            Logger.LogError(firstException, "operation=CaptureTeardown, stage=Complete, detail=Capture session teardown completed with one or more failures");
        }
        else
        {
            Logger.LogInformation("operation=CaptureTeardown, stage=Complete, detail=Capture session teardown completed: all 4 steps finished in order");
        }

        return new CaptureSessionDisposalResult(
            frameHandlerUnsubscribed,
            sessionStopped,
            framePoolDisposed,
            deviceDisposed,
            firstException);
    }
}
