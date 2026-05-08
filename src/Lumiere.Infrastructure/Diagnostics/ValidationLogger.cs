using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Lumiere.Infrastructure.Diagnostics;

public static class ValidationLogger
{
    private static readonly object Sync = new();
    private static string? logPath;
    private static bool initialized;
    private static ILogger? bridgeLogger;

    public static string LogPath
    {
        get
        {
            EnsureInitialized();
            return logPath!;
        }
    }

    public static void SetBridgeLogger(ILogger logger)
    {
        bridgeLogger = logger;
    }

    public static void Log(string epic, string message)
    {
        if (bridgeLogger is not null)
        {
            var logLevel = InferLogLevel(message);
            bridgeLogger.Log(logLevel, "[{Epic}] {Message}", epic, message);
            return;
        }

        EnsureInitialized();

        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
        var line = $"[{timestamp}] [{epic}] {message}{Environment.NewLine}";

        lock (Sync)
        {
            try
            {
                File.AppendAllText(logPath!, line);
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine($"ValidationLogger append failed: {line.Trim()}");
            }
        }
    }

    public static void LogHeader(params string[] lines)
    {
        if (bridgeLogger is not null)
        {
            foreach (var line in lines)
            {
                bridgeLogger.LogInformation("[HEADER] {Line}", line);
            }
            return;
        }

        EnsureInitialized();

        lock (Sync)
        {
            try
            {
                using var writer = new StreamWriter(logPath!, append: false);
                foreach (var line in lines)
                {
                    writer.WriteLine($"[HEADER] {line}");
                }
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("ValidationLogger header write failed.");
            }
        }
    }

    private static void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        lock (Sync)
        {
            if (initialized)
            {
                return;
            }

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dir = Path.Combine(appData, "Lumiere");
            Directory.CreateDirectory(dir);
            logPath = Path.Combine(dir, "validation.log");
            initialized = true;
        }
    }

    private static LogLevel InferLogLevel(string message)
    {
        if (message.Contains("FAILED", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("EXCEPTION", StringComparison.OrdinalIgnoreCase))
        {
            return LogLevel.Error;
        }

        if (message.Contains("BLOCKED", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("NOT SUPPORTED", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("SKIPPED", StringComparison.OrdinalIgnoreCase))
        {
            return LogLevel.Warning;
        }

        if (message.Contains("SUCCESS", StringComparison.OrdinalIgnoreCase))
        {
            return LogLevel.Information;
        }

        return LogLevel.Debug;
    }
}
