using Lumiere.Overlay.Input;
using Windows.System;
using Xunit;

namespace Lumiere.Overlay.Tests;

public sealed class OverlayInputTests
{
    [Fact]
    public void ShouldRequestCancel_ReturnsTrueOnlyForEscape()
    {
        Assert.True(OverlayKeyboardInputRouter.ShouldRequestCancel(VirtualKey.Escape));
        Assert.False(OverlayKeyboardInputRouter.ShouldRequestCancel(VirtualKey.Enter));
        Assert.False(OverlayKeyboardInputRouter.ShouldRequestCancel(VirtualKey.Space));
    }

    [Fact]
    public void CancelRequestGate_AllowsOnlyOneRequest()
    {
        var gate = new OverlayCancelRequestGate();

        Assert.True(gate.TryRequestCancel());
        Assert.False(gate.TryRequestCancel());
        Assert.True(gate.IsCancelRequested);
    }
}
