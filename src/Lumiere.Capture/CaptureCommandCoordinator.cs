using Lumiere.Infrastructure.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Lumiere.Capture;

/// <summary>
/// Coordinates capture commands by wrapping CaptureService.TryReserveCommand().
/// Provides a single entry point for UI buttons, tray commands, and global hotkeys.
/// UI-thread marshalling responsibility remains with the caller (MainWindow or future UI coordinator).
/// </summary>
public sealed class CaptureCommandCoordinator : ICaptureCommandCoordinator
{
    private static readonly ILogger Logger = LumiereLoggerFactory.CreateLogger(LogCategories.Capture);
    private readonly CaptureService captureService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureCommandCoordinator"/> class.
    /// </summary>
    /// <param name="captureService">The capture service to coordinate commands for.</param>
    public CaptureCommandCoordinator(CaptureService captureService)
    {
        this.captureService = captureService ?? throw new ArgumentNullException(nameof(captureService));
    }

    /// <inheritdoc/>
    public Task<CaptureCommandResult> ExecuteAsync(CaptureCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        Logger.LogInformation(
            "ExecuteAsync: mode={Mode}, currentStatus={Status}",
            command.Mode, captureService.CurrentSessionState.Status);

        var result = captureService.TryReserveCommand(command);

        if (result.IsAccepted)
        {
            Logger.LogInformation(
                "ExecuteAsync ACCEPTED: mode={Mode}",
                command.Mode);
        }
        else
        {
            var diagnostic = DiagnosticContext.CaptureWarning(
                stage: "CommandValidation",
                userFacingState: "Capture command rejected",
                technicalDetail: $"mode={command.Mode}, outcome={result.Outcome}, reason={result.Readiness?.TechnicalDetail ?? "none"}");
            diagnostic.LogTo(Logger);
        }

        return Task.FromResult(result);
    }
}
