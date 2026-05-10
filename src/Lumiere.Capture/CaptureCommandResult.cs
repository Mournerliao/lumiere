using Lumiere.Graphics.Hdr;

namespace Lumiere.Capture;

/// <summary>
/// Represents the result of attempting to execute a capture command.
/// Follows the established CaptureStartResult pattern for consistency.
/// </summary>
public sealed class CaptureCommandResult
{
    private CaptureCommandResult(
        CaptureCommandOutcome outcome,
        CaptureCommand command,
        CaptureSessionState? sessionState,
        PreviewReadinessStatus? readiness)
    {
        Outcome = outcome;
        Command = command;
        SessionState = sessionState;
        Readiness = readiness;
    }

    /// <summary>
    /// Gets the outcome of the command execution attempt.
    /// </summary>
    public CaptureCommandOutcome Outcome { get; }

    /// <summary>
    /// Gets the command that was attempted.
    /// </summary>
    public CaptureCommand Command { get; }

    /// <summary>
    /// Gets the current session state at the time of rejection, if available.
    /// Useful for UI feedback about why command was rejected.
    /// </summary>
    public CaptureSessionState? SessionState { get; }

    /// <summary>
    /// Gets the readiness status, if available.
    /// </summary>
    public PreviewReadinessStatus? Readiness { get; }

    /// <summary>
    /// Returns true if the command was accepted and will be executed.
    /// </summary>
    public bool IsAccepted => Outcome == CaptureCommandOutcome.Accepted;

    /// <summary>
    /// Returns true if the command was rejected because a session is already active.
    /// </summary>
    public bool IsRejectedSessionActive => Outcome == CaptureCommandOutcome.RejectedSessionActive;

    /// <summary>
    /// Returns true if the command was rejected because the session is in a non-recoverable state.
    /// </summary>
    public bool IsRejectedNonRecoverable => Outcome == CaptureCommandOutcome.RejectedNonRecoverable;

    /// <summary>
    /// Returns true if the command execution failed.
    /// </summary>
    public bool IsFailed => Outcome == CaptureCommandOutcome.Failed;

    /// <summary>
    /// Creates a result indicating the command was accepted.
    /// </summary>
    /// <param name="command">The accepted command.</param>
    /// <param name="readiness">Optional readiness status.</param>
    /// <returns>A new CaptureCommandResult indicating acceptance.</returns>
    public static CaptureCommandResult Accepted(
        CaptureCommand command,
        PreviewReadinessStatus? readiness = null) =>
        new(
            CaptureCommandOutcome.Accepted,
            command ?? throw new ArgumentNullException(nameof(command)),
            null,
            readiness);

    /// <summary>
    /// Creates a result indicating the command was rejected because a session is already active.
    /// </summary>
    /// <param name="command">The rejected command.</param>
    /// <param name="currentSessionState">The current session state that caused rejection.</param>
    /// <param name="readiness">Optional readiness status with rejection reason.</param>
    /// <returns>A new CaptureCommandResult indicating rejection.</returns>
    public static CaptureCommandResult RejectedSessionActive(
        CaptureCommand command,
        CaptureSessionState currentSessionState,
        PreviewReadinessStatus? readiness = null) =>
        new(
            CaptureCommandOutcome.RejectedSessionActive,
            command ?? throw new ArgumentNullException(nameof(command)),
            currentSessionState ?? throw new ArgumentNullException(nameof(currentSessionState)),
            readiness);

    /// <summary>
    /// Creates a result indicating the command was rejected because the session is in a non-recoverable state.
    /// </summary>
    /// <param name="command">The rejected command.</param>
    /// <param name="currentSessionState">The current session state that caused rejection.</param>
    /// <param name="readiness">Optional readiness status with rejection reason.</param>
    /// <returns>A new CaptureCommandResult indicating rejection.</returns>
    public static CaptureCommandResult RejectedNonRecoverable(
        CaptureCommand command,
        CaptureSessionState currentSessionState,
        PreviewReadinessStatus? readiness = null) =>
        new(
            CaptureCommandOutcome.RejectedNonRecoverable,
            command ?? throw new ArgumentNullException(nameof(command)),
            currentSessionState ?? throw new ArgumentNullException(nameof(currentSessionState)),
            readiness);

    /// <summary>
    /// Creates a result indicating the command execution failed.
    /// </summary>
    /// <param name="command">The failed command.</param>
    /// <param name="readiness">Readiness status with failure details.</param>
    /// <returns>A new CaptureCommandResult indicating failure.</returns>
    public static CaptureCommandResult Failed(
        CaptureCommand command,
        PreviewReadinessStatus readiness) =>
        new(
            CaptureCommandOutcome.Failed,
            command ?? throw new ArgumentNullException(nameof(command)),
            null,
            readiness ?? throw new ArgumentNullException(nameof(readiness)));
}
