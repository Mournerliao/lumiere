namespace Lumiere.Overlay.Input;

public sealed class OverlayCancelRequestGate
{
    public bool IsCancelRequested { get; private set; }

    public bool TryRequestCancel()
    {
        if (IsCancelRequested)
        {
            return false;
        }

        IsCancelRequested = true;
        return true;
    }
}
