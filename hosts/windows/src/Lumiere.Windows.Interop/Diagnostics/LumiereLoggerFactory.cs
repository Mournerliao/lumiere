using Microsoft.Extensions.Logging;

namespace Lumiere.Windows.Interop.Diagnostics;

internal static class LumiereLoggerFactory
{
    public static readonly LogLevel DefaultMinimumLevel = LogLevel.Information;
    public static readonly int DefaultMaxFileSizeBytes = 10 * 1024 * 1024;
    public static readonly int DefaultRetentionDays = 7;

    private static ILoggerFactory? factory;
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

    /// <summary>
    /// Configures the process-owned logger factory before any native module creates a logger.
    /// The future platform Host uses this seam to route structured diagnostics to stderr.
    /// </summary>
    public static void Configure(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        lock (Sync)
        {
            if (initialized)
            {
                throw new InvalidOperationException(
                    "Logging is already initialized. Configure logging before creating Windows engine modules.");
            }

            factory = loggerFactory;
            initialized = true;
        }
    }

    public static void InitializeWithHeader(LogLevel minimumLevel, params string[] headerLines)
    {
        Initialize(minimumLevel);

        var logger = Instance.CreateLogger(LogCategories.Interop);
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
        private readonly FileLoggerProvider fileProvider;
#if DEBUG
        private readonly DebugLoggerProvider debugProvider;
#endif

        internal SimpleLoggerFactory(LogLevel minimumLevel)
        {
            this.minimumLevel = minimumLevel;
            fileProvider = new FileLoggerProvider(
                maxFileSizeBytes: DefaultMaxFileSizeBytes,
                retentionDays: DefaultRetentionDays);
#if DEBUG
            debugProvider = new DebugLoggerProvider();
#endif
        }

        public ILogger CreateLogger(string categoryName)
        {
            var fileLogger = fileProvider.CreateLogger(categoryName);
#if DEBUG
            var debugLogger = debugProvider.CreateLogger(categoryName);
            var compositeLogger = new CompositeLogger(fileLogger, debugLogger);
            return new FilteredLogger(compositeLogger, minimumLevel);
#else
            return new FilteredLogger(fileLogger, minimumLevel);
#endif
        }

        public void AddProvider(ILoggerProvider provider)
        {
            throw new NotSupportedException(
                "The fallback logger has fixed providers. Configure a process-owned ILoggerFactory before engine creation.");
        }

        public void Dispose()
        {
            fileProvider.Dispose();
#if DEBUG
            debugProvider.Dispose();
#endif
        }
    }

#if DEBUG
    private sealed class CompositeLogger : ILogger
    {
        private readonly ILogger primary;
        private readonly ILogger secondary;

        internal CompositeLogger(ILogger primary, ILogger secondary)
        {
            this.primary = primary;
            this.secondary = secondary;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return new CompositeScope(primary.BeginScope(state), secondary.BeginScope(state));
        }

        private sealed class CompositeScope(IDisposable? primaryScope, IDisposable? secondaryScope) : IDisposable
        {
            public void Dispose()
            {
                secondaryScope?.Dispose();
                primaryScope?.Dispose();
            }
        }

        public bool IsEnabled(LogLevel logLevel) =>
            primary.IsEnabled(logLevel) || secondary.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            primary.Log(logLevel, eventId, state, exception, formatter);
            secondary.Log(logLevel, eventId, state, exception, formatter);
        }
    }
#endif

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
