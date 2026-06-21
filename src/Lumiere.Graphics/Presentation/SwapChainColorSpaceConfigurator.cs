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
        ColorSpaceType colorSpace,
        HdrDisplayCapability? displayCapability = null,
        bool requireTargetedDisplayCapability = false)
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

            if (displayCapability is { State: HdrDisplayState.Inactive })
            {
                Logger.LogInformation(
                    "ColorSpace check passed but display HDR is inactive (colorSpace={DisplayColorSpace}, device={DeviceName}); marking degraded.",
                    displayCapability.DisplayColorSpace, displayCapability.DeviceName);
                return PreviewReadinessStatus.Degraded(
                    PreviewReadinessStage.Presentation,
                    "Enable HDR in Windows Display settings for best capture quality.",
                    $"Display color space is {displayCapability.DisplayColorSpace} (device: {displayCapability.DeviceName}); HDR is not active.");
            }

            if (requireTargetedDisplayCapability && displayCapability is { State: HdrDisplayState.Unknown })
            {
                Logger.LogInformation(
                    "ColorSpace check passed but target-aware display capability could not be resolved; marking degraded.");
                return PreviewReadinessStatus.Degraded(
                    PreviewReadinessStage.Presentation,
                    "HDR readiness is unvalidated for the selected capture target.",
                    "Target-aware display capability could not be matched to a DXGI output.");
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
            var diagnostic = DiagnosticContext.PreviewFailure(
                stage: "ColorSpaceConfiguration",
                userFacingState: "Preview color space setup failed",
                technicalDetail: $"Operation={SetOperationName}, Detail={exception.Message}",
                exception: exception);
            diagnostic.LogTo(Logger);

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

            var diagnostic = DiagnosticContext.PreviewFailure(
                stage: "ColorSpaceConfiguration",
                userFacingState: "Preview color space setup failed",
                technicalDetail: $"Operation={SetOperationName}, {hResult} {exception.Message}",
                exception: exception);
            diagnostic.LogTo(Logger);

            return PreviewReadinessStatus.Failed(
                PreviewReadinessStage.Presentation,
                "Preview color space setup failed before HDR correctness could be validated.",
                $"{SetOperationName}:{hResult} {exception.Message}");
        }
    }
}
