namespace Lumiere.Capture;

/// <summary>
/// Shared entry point for capture commands from UI buttons, tray commands, and global hotkeys.
/// Wraps the existing CaptureService.TryReserveCommand() + ExecuteCommand() flow
/// to provide a single coordination point for all app-facing callers.
/// </summary>
public interface ICaptureCommandCoordinator
{
    /// <summary>
    /// Executes a capture command by validating, reserving, and initiating the capture session.
    /// Thread-safe: delegates to CaptureService's TOCTOU-safe guard pattern.
    /// </summary>
    /// <param name="command">The capture command to execute (fullscreen or region).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A CaptureCommandResult indicating acceptance, rejection, or failure.</returns>
    Task<CaptureCommandResult> ExecuteAsync(CaptureCommand command, CancellationToken cancellationToken = default);
}
