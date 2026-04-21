using Lumiere.Graphics.Hdr;
using Lumiere.Infrastructure.Interop;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Lumiere.Graphics.Devices;

public sealed class GraphicsDeviceProvider
{
    private const string OperationName = "D3D11CreateDevice";

    private readonly GraphicsDeviceCreationOptions options;

    public GraphicsDeviceProvider(GraphicsDeviceCreationOptions? options = null)
    {
        this.options = options ?? new GraphicsDeviceCreationOptions();
    }

    public GraphicsDeviceCreationOptions Options => options;

    public DeviceCreationFlags EffectiveCreationFlags =>
        options.CreationFlags | DeviceCreationFlags.BgraSupport;

    public GraphicsDeviceResources CreateDevice()
    {
        ID3D11Device? device = null;
        ID3D11DeviceContext? immediateContext = null;
        IDXGIDevice? dxgiDevice = null;

        try
        {
            var featureLevels = options.FeatureLevels.ToArray();
            var creationFlags = EffectiveCreationFlags;
            device = D3D11.D3D11CreateDevice(DriverType.Hardware, creationFlags, featureLevels);
            var selectedFeatureLevel = device.FeatureLevel;
            immediateContext = device.ImmediateContext;
            dxgiDevice = device.QueryInterface<IDXGIDevice>();

            var evidence = PreviewReadinessStatus.Initializing(
                "Graphics device creation succeeded; HDR preview still needs presentation validation.",
                $"{OperationName} selected {selectedFeatureLevel} with {creationFlags}.");

            return new GraphicsDeviceResources(
                device,
                immediateContext,
                dxgiDevice,
                selectedFeatureLevel,
                evidence);
        }
        catch (Exception exception) when (exception is not GraphicsDeviceException)
        {
            dxgiDevice?.Dispose();
            immediateContext?.Dispose();
            device?.Dispose();

            throw CreateFailure(FormatExceptionDetail(exception), exception);
        }
    }

    public PreviewReadinessStatus MapFailureToReadiness(Exception exception) =>
        PreviewReadinessStatus.Failed(
            exception is NativeInteropException ? PreviewReadinessStage.Interop : PreviewReadinessStage.Graphics,
            "Graphics initialization failed before HDR preview could be validated.",
            exception.Message);

    private static GraphicsDeviceException CreateFailure(string technicalDetail, Exception? innerException = null) =>
        new(
            OperationName,
            PreviewReadinessStage.Graphics,
            "Graphics device creation failed before HDR preview could be validated.",
            technicalDetail,
            innerException);

    private static string FormatExceptionDetail(Exception exception)
    {
        var hResult = exception.HResult == 0
            ? string.Empty
            : $" HRESULT {NativeInteropException.FormatHResult(exception.HResult)}.";

        return $"{exception.GetType().Name}:{hResult} {exception.Message}";
    }
}
