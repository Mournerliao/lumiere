using Windows.Foundation.Metadata;
using Windows.Graphics.Capture;
using Windows.Security.Authorization.AppCapabilityAccess;

namespace Lumiere.Windows.Capture;

internal static class CaptureBorderAccess
{
    private const string GraphicsCaptureAccessTypeName =
        "Windows.Graphics.Capture.GraphicsCaptureAccess";

    public static async Task<CaptureBorderOptions> ResolveAsync(
        CancellationToken cancellationToken)
    {
        if (!ApiInformation.IsTypePresent(GraphicsCaptureAccessTypeName))
        {
            return CaptureBorderOptions.RequireSystemBorder();
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var status = await GraphicsCaptureAccess.RequestAccessAsync(
                GraphicsCaptureAccessKind.Borderless);
            cancellationToken.ThrowIfCancellationRequested();
            return status == AppCapabilityAccessStatus.Allowed
                ? CaptureBorderOptions.TryBorderless()
                : CaptureBorderOptions.RequireSystemBorder();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return CaptureBorderOptions.RequireSystemBorder();
        }
    }
}
