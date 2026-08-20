using Lumiere.Windows.Graphics.Devices;
using Lumiere.Windows.Graphics.Hdr;
using Lumiere.Windows.Interop;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Xunit;

namespace Lumiere.Windows.Graphics.Tests.Devices;

public sealed class GraphicsDeviceProviderTests
{
    [Fact]
    public void DefaultOptionsEnableBgraSupportForWinRtAndDxgiInterop()
    {
        var options = new GraphicsDeviceCreationOptions();

        Assert.True(options.CreationFlags.HasFlag(DeviceCreationFlags.BgraSupport));
    }

    [Fact]
    public void ProviderForcesBgraSupportWhenCustomOptionsDisableIt()
    {
        var provider = new GraphicsDeviceProvider(
            new GraphicsDeviceCreationOptions
            {
                CreationFlags = DeviceCreationFlags.None,
            });

        Assert.True(provider.EffectiveCreationFlags.HasFlag(DeviceCreationFlags.BgraSupport));
    }

    [Fact]
    public void DefaultFeatureLevelsAreOrderedFromHighestToLowest()
    {
        var options = new GraphicsDeviceCreationOptions();

        Assert.Equal(
            new[]
            {
                FeatureLevel.Level_12_1,
                FeatureLevel.Level_12_0,
                FeatureLevel.Level_11_1,
                FeatureLevel.Level_11_0,
                FeatureLevel.Level_10_1,
                FeatureLevel.Level_10_0,
            },
            options.FeatureLevels);
    }

    [Fact]
    public void NativeInteropFailureCarriesOperationStageAndTechnicalDetail()
    {
        var exception = Direct3D11Interop.CreateFailure(
            unchecked((int)0x887A0004),
            "DXGI device pointer was rejected.");

        Assert.Equal("CreateDirect3D11DeviceFromDXGIDevice", exception.OperationName);
        Assert.Equal("Interop", exception.Stage);
        Assert.Equal(unchecked((int)0x887A0004), exception.HResultCode);
        Assert.Contains("0x887A0004", exception.Message, StringComparison.Ordinal);
        Assert.Contains("DXGI device pointer", exception.TechnicalDetail, StringComparison.Ordinal);
        Assert.Contains("Windows-compatible Direct3D device", exception.UserMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void GraphicsFailureMapsToFailedReadinessStatus()
    {
        var provider = new GraphicsDeviceProvider();
        var exception = new InvalidOperationException("D3D11CreateDevice returned E_FAIL.");

        var status = provider.MapFailureToReadiness(exception);

        Assert.Equal(EngineReadinessState.Failed, status.State);
        Assert.Equal(EngineReadinessStage.Graphics, status.Stage);
        Assert.True(status.RequiresUserAttention);
        Assert.Contains("D3D11CreateDevice", status.TechnicalDetail ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void NativeInteropFailureMapsToInteropReadinessStage()
    {
        var provider = new GraphicsDeviceProvider();
        var exception = Direct3D11Interop.CreateFailure(
            unchecked((int)0x80004005),
            "CreateDirect3D11DeviceFromDXGIDevice returned E_FAIL.");

        var status = provider.MapFailureToReadiness(exception);

        Assert.Equal(EngineReadinessState.Failed, status.State);
        Assert.Equal(EngineReadinessStage.Interop, status.Stage);
        Assert.Contains("CreateDirect3D11DeviceFromDXGIDevice", status.TechnicalDetail ?? string.Empty, StringComparison.Ordinal);
    }
}
