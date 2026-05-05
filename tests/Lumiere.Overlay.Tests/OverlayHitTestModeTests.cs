using Lumiere.Overlay.Windowing;
using Xunit;

namespace Lumiere.Overlay.Tests;

public sealed class OverlayHitTestModeTests
{
    [Fact]
    public void DefaultMode_KeepsOverlayInteractiveForCropInput()
    {
        Assert.Equal(OverlayHitTestMode.Interactive, OverlayHitTestModeDefaults.MvpDefault);
    }

    [Fact]
    public void PresenterApplication_DescribesInteractiveHitTesting()
    {
        var application = OverlayWindowPresenter.CreatePresenterApplication(
            "HDR Display",
            OverlayHitTestModeDefaults.MvpDefault);

        Assert.Equal(OverlayHitTestMode.Interactive, application.HitTestMode);
        Assert.Contains("interactive hit testing", application.TechnicalDetail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("crop input", application.TechnicalDetail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PresenterApplication_DescribesPassThroughWhenExplicitlyRequested()
    {
        var application = OverlayWindowPresenter.CreatePresenterApplication(
            "Reference Window",
            OverlayHitTestMode.PassThrough);

        Assert.Equal(OverlayHitTestMode.PassThrough, application.HitTestMode);
        Assert.Contains("pass-through", application.TechnicalDetail, StringComparison.OrdinalIgnoreCase);
    }
}
