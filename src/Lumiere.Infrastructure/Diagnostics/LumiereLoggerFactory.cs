using Microsoft.Extensions.Logging;

namespace Lumiere.Infrastructure.Diagnostics;

public static class LumiereLoggerFactory
{
    private static SimpleLoggerFactory? factory;
    private static readonly object Sync = new();
    private static bool initialized;

    public static ILoggerFactory Instance
    {
        get
        {
            EnsureInitialized();
            return factory!;
        }
    }

    public static ILogger<T> CreateLogger<T>() => Instance.CreateLogger<T>();

    public static ILogger CreateLogger(string categoryName) => Instance.CreateLogger(categoryName);

    public static void Initialize(LogLevel minimumLevel = LogLevel.Information)
    {
        lock (Sync)
        {
            if (initialized)
            {
                return;
            }

            factory = new SimpleLoggerFactory(minimumLevel);
            initialized = true;
        }
    }

    public static void InitializeWithHeader(LogLevel minimumLevel, params string[] headerLines)
    {
        Initialize(minimumLevel);

        var logger = factory!.CreateLogger(LogCategories.App);
        foreach (var line in headerLines)
        {
            logger.LogInformation("{Line}", line);
        }
    }

    private static void EnsureInitialized()
    {
        if (!initialized)
        {
            lock (Sync)
            {
                if (!initialized)
                {
                    Initialize();
                }
            }
        }
    }

    private sealed class SimpleLoggerFactory : ILoggerFactory
    {
        private readonly LogLevel minimumLevel;
        private readonly FileLoggerProvider provider;

        internal SimpleLoggerFactory(LogLevel minimumLevel)
        {
            this.minimumLevel = minimumLevel;
            provider = new FileLoggerProvider();
        }

        public ILogger CreateLogger(string categoryName)
        {
            var innerLogger = provider.CreateLogger(categoryName);
            return new FilteredLogger(innerLogger, minimumLevel);
        }

        public void AddProvider(ILoggerProvider provider)
        {
            // No-op: we only support our built-in FileLoggerProvider
        }

        public void Dispose()
        {
            provider.Dispose();
        }
    }

    private sealed class FilteredLogger : ILogger
    {
        private readonly ILogger inner;
        private readonly LogLevel minimumLevel;

        internal FilteredLogger(ILogger inner, LogLevel minimumLevel)
        {
            this.inner = inner;
            this.minimumLevel = minimumLevel;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => inner.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) =>
            logLevel >= minimumLevel && logLevel != LogLevel.None && inner.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                inner.Log(logLevel, eventId, state, exception, formatter);
            }
        }
    }
}
