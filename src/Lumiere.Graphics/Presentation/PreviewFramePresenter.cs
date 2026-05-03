using Lumiere.Graphics.Devices;
using Lumiere.Graphics.Hdr;
using Lumiere.Infrastructure.Interop;

namespace Lumiere.Graphics.Presentation;

public sealed class PreviewFramePresenter
{
    private readonly IPreviewFrameOutput output;

    public PreviewFramePresenter(
        GraphicsDeviceResources deviceResources,
        SwapChainResources swapChainResources)
        : this(new SwapChainFrameOutput(deviceResources, swapChainResources))
    {
    }

    internal PreviewFramePresenter(IPreviewFrameOutput output)
    {
        this.output = output ?? throw new ArgumentNullException(nameof(output));
    }

    public PreviewRenderResult PresentFrame(CapturedFrameTexture frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        try
        {
            output.CopyFrame(frame.Texture);
            output.Present();

            return new PreviewRenderResult(
                PreviewReadinessStatus.Ready(
                    "HDR-ready",
                    $"{frame.SourceDescription} reached the FP16 scRGB swap chain without CPU readback."));
        }
        catch (Exception exception)
        {
            return new PreviewRenderResult(MapFailureToReadiness(exception));
        }
    }

    public static PreviewReadinessStatus MapFailureToReadiness(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return PreviewReadinessStatus.Failed(
            PreviewReadinessStage.Presentation,
            "Preview failed",
            InteropFailureDiagnostics.Write(exception));
    }
}
