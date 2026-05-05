using Windows.System;

namespace Lumiere.Overlay.Input;

public static class OverlayKeyboardInputRouter
{
    public static bool ShouldRequestCancel(VirtualKey key) =>
        key is VirtualKey.Escape;
}
