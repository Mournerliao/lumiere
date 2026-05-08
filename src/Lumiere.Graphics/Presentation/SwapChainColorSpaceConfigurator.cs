using Lumiere.Graphics.Hdr;
using Lumiere.Infrastructure.Diagnostics;
using Lumiere.Infrastructure.Interop;
using Microsoft.Extensions.Logging;
using Vortice.DXGI;

namespace Lumiere.Graphics.Presentation;

public static class SwapChainColorSpaceConfigurator
{
    private static readonly ILogger Logger = LumiereLoggerFactory.CreateLogger(LogCategories.Graphics);
    private const string CheckOperationName = "IDXGISwapChain3.CheckColorSpaceSupport";
    private const string SetOperationName = "IDXGISwapChain3.SetColorSpace1";

    public static PreviewReadinessStatus Configure(
        ISwapChainColorSpaceController controller,
        ColorSpaceType colorSpace)
    {
        ArgumentNullException.ThrowIfNull(controller);

        try
        {
            var support = controller.CheckColorSpaceSupport(colorSpace);
            if (!support.HasFlag(SwapChainColorSpaceSupportFlags.Present))
            {
                Logger.LogWarning("ColorSpace check: {ColorSpace} NOT supported (returned {Support}), degraded", colorSpace, support);
                return PreviewReadinessStatus.Degraded(
                    PreviewReadinessStage.Presentation,
                    "Preview cannot be proven HDR-correct on the current display path.",
                    $"{CheckOperationName} returned {support} for {colorSpace}.");
            }

            controller.SetColorSpace1(colorSpace);

            Logger.LogDebug("ColorSpace configured: {ColorSpace}, support={Support}", colorSpace, support);

            return PreviewReadinessStatus.Initializing(
                PreviewReadinessStage.Presentation,
                "Preview presentation is configured for HDR; live capture still needs validation.",
                $"{CheckOperationName} returned {support}; {SetOperationName} set {colorSpace}.");
        }
        catch (SwapChainPresentationException exception)
        {
            Logger.LogError(exception, "ColorSpace FAILED (SwapChainPresentation)");
            return PreviewReadinessStatus.Failed(
                PreviewReadinessStage.Presentation,
                "Preview color space setup failed before HDR correctness could be validated.",
                exception.Message);
        }
        catch (Exception exception)
        {
            var hResult = exception.HResult == 0
                ? string.Empty
                : $" HRESULT {NativeInteropException.FormatHResult(exception.HResult)}.";

            Logger.LogError(exception, "ColorSpace FAILED: {Operation}:{HResult} {Message}", SetOperationName, hResult, exception.Message);
            return PreviewReadinessStatus.Failed(
                PreviewReadinessStage.Presentation,
                "Preview color space setup failed before HDR correctness could be validated.",
                $"{SetOperationName}:{hResult} {exception.Message}");
        }
    }
}
