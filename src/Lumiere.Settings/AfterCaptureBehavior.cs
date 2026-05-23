namespace Lumiere.Settings;

/// <summary>
/// User preference for supported post-capture behavior.
/// Actual artifact actions are implemented by later output stories.
/// </summary>
public enum AfterCaptureBehavior
{
    None = 0,
    Open = 1,
    Reveal = 2,
}
