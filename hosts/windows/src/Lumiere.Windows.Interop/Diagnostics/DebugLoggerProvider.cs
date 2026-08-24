using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Lumiere.Windows.Interop.Diagnostics;

internal sealed class DebugLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) =>
        new DebugLogger(categoryName);

    public void Dispose()
    {
    }

    private sealed class DebugLogger : ILogger
    {
        private readonly string category;

        internal DebugLogger(string category)
        {
            this.category = category;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

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

            var message = formatter(state, exception);
            if (string.IsNullOrEmpty(message) && exception is null)
            {
                return;
            }

            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
            var level = FormatLogLevel(logLevel);
            var shortCategory = ShortenCategory(category);

            var line = exception is null
                ? $"[{timestamp}] [{level}] [{shortCategory}] {message}"
                : $"[{timestamp}] [{level}] [{shortCategory}] {message}{Environment.NewLine}    {exception}";

            Debug.WriteLine(line);
        }

        private static string FormatLogLevel(LogLevel logLevel) => logLevel switch
        {
            LogLevel.Trace => "TRC",
            LogLevel.Debug => "DBG",
            LogLevel.Information => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "FTL",
            _ => "???",
        };

        private static string ShortenCategory(string category)
        {
            var lastDot = category.LastIndexOf('.');
            return lastDot >= 0 ? category[(lastDot + 1)..] : category;
        }
    }
}
