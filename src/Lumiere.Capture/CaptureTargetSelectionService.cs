using Lumiere.Graphics.Hdr;
using Lumiere.Infrastructure.Interop;
using Windows.Graphics.Capture;

namespace Lumiere.Capture;

/// <summary>
/// Picker-first capture target selection service.
/// This is a fallback/debug-only path retained for scenarios where direct monitor capture
/// is unavailable or needs manual target selection. The default current-baseline path uses
/// <see cref="DirectMonitorCaptureTargetSelectionService.CreateDirectOnly"/> instead.
/// </summary>
public sealed class CaptureTargetSelectionService
{
    private readonly ICaptureTargetPicker picker;
    private readonly Func<bool> isCaptureSupported;

    public CaptureTargetSelectionService(
        ICaptureTargetPicker picker,
        Func<bool>? isCaptureSupported = null)
    {
        this.picker = picker ?? throw new ArgumentNullException(nameof(picker));
        this.isCaptureSupported = isCaptureSupported ?? GraphicsCaptureSession.IsSupported;
    }

    public async Task<CaptureTargetSelectionResult> SelectTargetAsync()
    {
        try
        {
            if (!isCaptureSupported())
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
                    InteropFailureDiagnostics.LogAndFormat(exception)));
        }
        catch (ArgumentException exception)
        {
            return CaptureTargetSelectionResult.Failed(
                PreviewReadinessStatus.Failed(
                    PreviewReadinessStage.Capture,
                    "Preview failed",
                    InteropFailureDiagnostics.LogAndFormat(exception)));
        }
        catch (Exception exception)
        {
            return CaptureTargetSelectionResult.Failed(
                PreviewReadinessStatus.Failed(
                    PreviewReadinessStage.Interop,
                    "Preview failed",
                    InteropFailureDiagnostics.LogAndFormat(exception)));
        }
    }
}
