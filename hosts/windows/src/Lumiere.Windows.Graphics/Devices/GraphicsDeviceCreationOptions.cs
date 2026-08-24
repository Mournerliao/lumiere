using Vortice.Direct3D;
using Vortice.Direct3D11;

namespace Lumiere.Windows.Graphics.Devices;

internal sealed record GraphicsDeviceCreationOptions
{
    public DeviceCreationFlags CreationFlags { get; init; } = DeviceCreationFlags.BgraSupport;

    public IReadOnlyList<FeatureLevel> FeatureLevels { get; init; } =
        new[]
        {
            FeatureLevel.Level_12_1,
            FeatureLevel.Level_12_0,
            FeatureLevel.Level_11_1,
            FeatureLevel.Level_11_0,
            FeatureLevel.Level_10_1,
            FeatureLevel.Level_10_0,
        };
}
