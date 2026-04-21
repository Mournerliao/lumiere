using Lumiere.Graphics.Hdr;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Lumiere.Graphics.Devices;

public sealed class GraphicsDeviceResources : IDisposable
{
    private bool disposed;

    public GraphicsDeviceResources(
        ID3D11Device device,
        ID3D11DeviceContext immediateContext,
        IDXGIDevice dxgiDevice,
        FeatureLevel featureLevel,
        PreviewReadinessStatus initializationEvidence)
    {
        Device = device ?? throw new ArgumentNullException(nameof(device));
        ImmediateContext = immediateContext ?? throw new ArgumentNullException(nameof(immediateContext));
        DxgiDevice = dxgiDevice ?? throw new ArgumentNullException(nameof(dxgiDevice));
        FeatureLevel = featureLevel;
        InitializationEvidence = initializationEvidence ?? throw new ArgumentNullException(nameof(initializationEvidence));
    }

    public ID3D11Device Device { get; }

    public ID3D11DeviceContext ImmediateContext { get; }

    public IDXGIDevice DxgiDevice { get; }

    public FeatureLevel FeatureLevel { get; }

    public PreviewReadinessStatus InitializationEvidence { get; }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        DxgiDevice.Dispose();
        ImmediateContext.Dispose();
        Device.Dispose();
    }
}
