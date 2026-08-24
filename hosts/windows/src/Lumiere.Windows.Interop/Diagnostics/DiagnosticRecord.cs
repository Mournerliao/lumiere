using Microsoft.Extensions.Logging;

namespace Lumiere.Windows.Interop.Diagnostics;

internal sealed record DiagnosticRecord
{
    public required string Operation { get; init; }
    public required string Stage { get; init; }
    public required string UserFacingState { get; init; }
    public required string TechnicalDetail { get; init; }
    public string? SessionId { get; init; }
    public string? CorrelationId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required LogLevel LogLevel { get; init; }
    public Exception? Exception { get; init; }

    public static DiagnosticRecord Create(
        string operation,
        string stage,
        string userFacingState,
        string technicalDetail,
        LogLevel logLevel,
        string? sessionId = null,
        string? correlationId = null,
        Exception? exception = null)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(userFacingState);
        ArgumentNullException.ThrowIfNull(technicalDetail);

        return new()
        {
            Operation = operation,
            Stage = stage,
            UserFacingState = userFacingState,
            TechnicalDetail = technicalDetail,
            LogLevel = logLevel,
            SessionId = sessionId,
            CorrelationId = correlationId,
            Timestamp = DateTimeOffset.UtcNow,
            Exception = exception,
        };
    }

    private const string LogTemplate =
        "operation={Operation}, stage={Stage}, state={State}, detail={Detail}, session={Session}, correlation={Correlation}";

    public void LogTo(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        switch (LogLevel)
        {
            case LogLevel.None:
                return;
            case LogLevel.Critical:
                logger.LogCritical(Exception, LogTemplate, Operation, Stage, UserFacingState, TechnicalDetail, SessionId, CorrelationId);
                break;
            case LogLevel.Error:
                logger.LogError(Exception, LogTemplate, Operation, Stage, UserFacingState, TechnicalDetail, SessionId, CorrelationId);
                break;
            case LogLevel.Warning:
                logger.LogWarning(Exception, LogTemplate, Operation, Stage, UserFacingState, TechnicalDetail, SessionId, CorrelationId);
                break;
            case LogLevel.Information:
                logger.LogInformation(Exception, LogTemplate, Operation, Stage, UserFacingState, TechnicalDetail, SessionId, CorrelationId);
                break;
            case LogLevel.Debug:
                logger.LogDebug(Exception, LogTemplate, Operation, Stage, UserFacingState, TechnicalDetail, SessionId, CorrelationId);
                break;
            case LogLevel.Trace:
                logger.LogTrace(Exception, LogTemplate, Operation, Stage, UserFacingState, TechnicalDetail, SessionId, CorrelationId);
                break;
            default:
                logger.LogInformation(Exception, LogTemplate, Operation, Stage, UserFacingState, TechnicalDetail, SessionId, CorrelationId);
                break;
        }
    }
}
