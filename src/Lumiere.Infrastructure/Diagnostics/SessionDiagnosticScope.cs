using Microsoft.Extensions.Logging;

namespace Lumiere.Infrastructure.Diagnostics;

public sealed class SessionDiagnosticScope : IDisposable
{
    private readonly IDisposable? scope;
    private bool disposed;

    private SessionDiagnosticScope(IDisposable? scope, string sessionId, string? correlationId)
    {
        this.scope = scope;
        SessionId = sessionId;
        CorrelationId = correlationId;
    }

    public string SessionId { get; }
    public string? CorrelationId { get; }

    public static SessionDiagnosticScope Begin(
        ILogger logger,
        string? sessionId = null,
        string? correlationId = null)
    {
        ArgumentNullException.ThrowIfNull(logger);

        var effectiveSessionId = string.IsNullOrWhiteSpace(sessionId)
            ? Guid.NewGuid().ToString("N")[..8]
            : sessionId;
        var effectiveCorrelationId = string.IsNullOrWhiteSpace(correlationId)
            ? Guid.NewGuid().ToString("N")[..8]
            : correlationId;

        var scope = logger.BeginScope(new Dictionary<string, string>
        {
            ["SessionId"] = effectiveSessionId,
            ["CorrelationId"] = effectiveCorrelationId,
        });

        return new SessionDiagnosticScope(scope, effectiveSessionId, effectiveCorrelationId);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        scope?.Dispose();
    }
}
