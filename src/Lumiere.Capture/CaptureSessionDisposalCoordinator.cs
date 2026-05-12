using Lumiere.Infrastructure.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Lumiere.Capture;

public static class CaptureSessionDisposalCoordinator
{
    private static readonly ILogger Logger = LumiereLoggerFactory.CreateLogger(LogCategories.Capture);

    public static CaptureSessionDisposalEvidence DisposeOnce(
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

        Logger.LogDebug("operation=CaptureTeardown, stage=1/6, detail=Unsubscribing frame handler");
        try { unsubscribeFrameHandler(); }
        catch (Exception ex) { Logger.LogError(ex, "operation=CaptureTeardown, stage=1/6, detail=Frame handler unsubscribe failed"); firstException ??= ex; }
        Logger.LogDebug("operation=CaptureTeardown, stage=1/6, detail=Frame handler unsubscribed");

        Logger.LogDebug("operation=CaptureTeardown, stage=2/6, detail=Stopping capture session");
        try { stopSession(); }
        catch (Exception ex) { Logger.LogError(ex, "operation=CaptureTeardown, stage=2/6, detail=Capture session stop failed"); firstException ??= ex; }
        Logger.LogDebug("operation=CaptureTeardown, stage=2/6, detail=Capture session stopped");

        Logger.LogDebug("operation=CaptureTeardown, stage=3/6, detail=Disposing frame pool");
        try { disposeFramePool(); }
        catch (Exception ex) { Logger.LogError(ex, "operation=CaptureTeardown, stage=3/6, detail=Frame pool dispose failed"); firstException ??= ex; }
        Logger.LogDebug("operation=CaptureTeardown, stage=3/6, detail=Frame pool disposed");

        Logger.LogDebug("operation=CaptureTeardown, stage=4/6, detail=Disposing D3D11 device");
        try { disposeDevice(); }
        catch (Exception ex) { Logger.LogError(ex, "operation=CaptureTeardown, stage=4/6, detail=D3D11 device dispose failed"); firstException ??= ex; }
        Logger.LogDebug("operation=CaptureTeardown, stage=4/6, detail=D3D11 device disposed");

        if (firstException is not null)
        {
            Logger.LogError(firstException, "operation=CaptureTeardown, stage=Complete, detail=Capture session teardown failed with exception");
            throw firstException;
        }

        Logger.LogInformation("operation=CaptureTeardown, stage=Complete, detail=Capture session teardown completed: all 4 steps finished in order");

        return new CaptureSessionDisposalEvidence(
            FrameHandlerUnsubscribed: true,
            SessionStopped: true,
            FramePoolDisposed: true,
            DeviceDisposed: true);
    }
}
