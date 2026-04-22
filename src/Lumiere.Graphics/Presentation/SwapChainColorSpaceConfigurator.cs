using Lumiere.Graphics.Hdr;
using Lumiere.Infrastructure.Interop;
using Vortice.DXGI;

namespace Lumiere.Graphics.Presentation;

public static class SwapChainColorSpaceConfigurator
{
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
                return PreviewReadinessStatus.Degraded(
                    PreviewReadinessStage.Presentation,
                    "Preview cannot be proven HDR-correct on the current display path.",
                    $"{CheckOperationName} returned {support} for {colorSpace}.");
            }

            controller.SetColorSpace1(colorSpace);

            return PreviewReadinessStatus.Initializing(
                PreviewReadinessStage.Presentation,
                "Preview presentation is configured for HDR; live capture still needs validation.",
                $"{CheckOperationName} returned {support}; {SetOperationName} set {colorSpace}.");
        }
        catch (SwapChainPresentationException exception)
        {
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

            return PreviewReadinessStatus.Failed(
                PreviewReadinessStage.Presentation,
                "Preview color space setup failed before HDR correctness could be validated.",
                $"{SetOperationName}:{hResult} {exception.Message}");
        }
    }
}
