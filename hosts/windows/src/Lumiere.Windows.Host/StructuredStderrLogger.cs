using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Lumiere.Windows.Host;

internal sealed class StructuredStderrLogger(TextWriter writer) : ILogger
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        if (state is HostDiagnostic diagnostic)
        {
            writer.WriteLine(JsonSerializer.Serialize(
                new
                {
                    level = logLevel.ToString().ToLowerInvariant(),
                    @event = diagnostic.Event,
                    requestID = diagnostic.RequestId,
                    code = diagnostic.Failure.Code,
                    message = diagnostic.Failure.Message,
                    retryable = diagnostic.Failure.Retryable,
                },
                SerializerOptions));
            writer.Flush();
            return;
        }

        writer.WriteLine(JsonSerializer.Serialize(
            new
            {
                level = logLevel.ToString().ToLowerInvariant(),
                @event = eventId.Name ?? "host-log",
                message = formatter(state, exception),
            },
            SerializerOptions));
        writer.Flush();
    }
}
