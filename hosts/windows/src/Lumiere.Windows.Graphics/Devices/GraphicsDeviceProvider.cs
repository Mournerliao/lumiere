using Lumiere.Windows.Graphics.Hdr;
using Lumiere.Windows.Interop;
using Lumiere.Windows.Interop.Diagnostics;
using Microsoft.Extensions.Logging;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Lumiere.Windows.Graphics.Devices;

internal sealed class GraphicsDeviceProvider
{
    private static readonly ILogger Logger = LumiereLoggerFactory.CreateLogger(LogCategories.Graphics);
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
        IDXGIAdapter? adapter = null;

        try
        {
            var featureLevels = options.FeatureLevels.ToArray();
            var creationFlags = EffectiveCreationFlags;
            device = D3D11.D3D11CreateDevice(DriverType.Hardware, creationFlags, featureLevels);
            var selectedFeatureLevel = device.FeatureLevel;
            immediateContext = device.ImmediateContext;
            dxgiDevice = device.QueryInterface<IDXGIDevice>();
            dxgiDevice.GetAdapter(out adapter);
            var adapterName = adapter?.Description.Description;

            Logger.LogDebug("D3D11Device created: featureLevel={FeatureLevel}, flags={Flags}", selectedFeatureLevel, creationFlags);

            var readiness = EngineReadinessStatus.Ready(
                "Graphics device is ready for HDR-aware capture.",
                $"{OperationName} selected {selectedFeatureLevel} with {creationFlags}.");

            return new GraphicsDeviceResources(
                device,
                immediateContext,
                dxgiDevice,
                adapterName,
                selectedFeatureLevel,
                readiness);
        }
        catch (Exception exception) when (exception is not GraphicsDeviceException)
        {
            adapter?.Dispose();
            dxgiDevice?.Dispose();
            immediateContext?.Dispose();
            device?.Dispose();

            var diagnostic = DiagnosticContext.EngineFailure(
                stage: "DeviceCreation",
                userFacingState: "Graphics device creation failed",
                technicalDetail: $"Operation={OperationName}, Detail={FormatExceptionDetail(exception)}",
                exception: exception);
            diagnostic.LogTo(Logger);

            throw CreateFailure(FormatExceptionDetail(exception), exception);
        }
        finally
        {
            adapter?.Dispose();
        }
    }

    public EngineReadinessStatus MapFailureToReadiness(Exception exception) =>
        EngineReadinessStatus.Failed(
            exception is NativeInteropException ? EngineReadinessStage.Interop : EngineReadinessStage.Graphics,
            "Graphics initialization failed before HDR-aware capture could start.",
            exception.Message);

    private static GraphicsDeviceException CreateFailure(string technicalDetail, Exception? innerException = null) =>
        new(
            OperationName,
            EngineReadinessStage.Graphics,
            "Graphics device creation failed before HDR-aware capture could start.",
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
