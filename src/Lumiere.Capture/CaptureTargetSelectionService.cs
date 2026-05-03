using Lumiere.Graphics.Hdr;
using Lumiere.Infrastructure.Interop;
using Windows.Graphics.Capture;

namespace Lumiere.Capture;

public sealed class CaptureTargetSelectionService
{
    private readonly ICaptureTargetPicker picker;

    public CaptureTargetSelectionService(ICaptureTargetPicker picker)
    {
        this.picker = picker ?? throw new ArgumentNullException(nameof(picker));
    }

    public async Task<CaptureTargetSelectionResult> SelectTargetAsync()
    {
        try
        {
            if (!GraphicsCaptureSession.IsSupported())
            {
                return CaptureTargetSelectionResult.Unsupported(
                    PreviewReadinessStatus.Unsupported(
                        PreviewReadinessStage.Capture,
                        "Unsupported capture",
                        "GraphicsCaptureSession.IsSupported returned false."));
            }

            var item = await picker.PickSingleItemAsync();

            if (item is null)
            {
                return CaptureTargetSelectionResult.Canceled(
                    PreviewReadinessStatus.Initializing(
                        PreviewReadinessStage.Capture,
                        "Choose a display or window to start the minimal HDR preview.",
                        "GraphicsCapturePicker was canceled."));
            }

            var target = CaptureTarget.FromItem(item);
            return CaptureTargetSelectionResult.Selected(
                target,
                PreviewReadinessStatus.Initializing(
                    PreviewReadinessStage.Capture,
                    "Target selected. Starting preview.",
                    $"Selected: {target.DisplayName} ({target.Size.Width}x{target.Size.Height})."));
        }
        catch (NotSupportedException exception)
        {
            return CaptureTargetSelectionResult.Unsupported(
                PreviewReadinessStatus.Unsupported(
                    PreviewReadinessStage.Capture,
                    "Unsupported capture",
                    InteropFailureDiagnostics.Write(exception)));
        }
        catch (ArgumentException exception)
        {
            return CaptureTargetSelectionResult.Failed(
                PreviewReadinessStatus.Failed(
                    PreviewReadinessStage.Capture,
                    "Preview failed",
                    InteropFailureDiagnostics.Write(exception)));
        }
        catch (Exception exception)
        {
            return CaptureTargetSelectionResult.Failed(
                PreviewReadinessStatus.Failed(
                    PreviewReadinessStage.Interop,
                    "Preview failed",
                    InteropFailureDiagnostics.Write(exception)));
        }
    }
}
