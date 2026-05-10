namespace Lumiere.Capture;

/// <summary>
/// Represents the outcome of attempting to execute a capture command.
/// </summary>
public enum CaptureCommandOutcome
{
    /// <summary>
    /// The command was accepted and will be executed.
    /// </summary>
    Accepted = 0,

    /// <summary>
    /// The command was rejected because a session is already active (selecting, initializing, capturing, etc.).
    /// </summary>
    RejectedSessionActive = 1,

    /// <summary>
    /// The command was rejected because the session is in a non-recoverable state.
    /// </summary>
    RejectedNonRecoverable = 2,

    /// <summary>
    /// The command execution failed.
    /// </summary>
    Failed = 3,
}
