using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Lumiere.Infrastructure.Diagnostics;

internal sealed class FileLogger : ILogger
{
    private static readonly object Sync = new();
    private readonly string category;
    private readonly string logDirectory;
    private readonly int maxFileSizeBytes;
    private readonly int retentionDays;

    internal FileLogger(string category, string logDirectory, int maxFileSizeBytes, int retentionDays)
    {
        this.category = category;
        this.logDirectory = logDirectory;
        this.maxFileSizeBytes = maxFileSizeBytes;
        this.retentionDays = retentionDays;
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

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        var level = FormatLogLevel(logLevel);
        var shortCategory = ShortenCategory(category);

        var line = exception is null
            ? $"[{timestamp}] [{level}] [{shortCategory}] {message}"
            : $"[{timestamp}] [{level}] [{shortCategory}] {message}{Environment.NewLine}    {exception}";

        WriteToFile(line);
    }

    private void WriteToFile(string line)
    {
        lock (Sync)
        {
            try
            {
                Directory.CreateDirectory(logDirectory);
                var logPath = GetLogFilePath();
                EnsureFileSizeLimit(logPath);
                File.AppendAllText(logPath, line + Environment.NewLine);
                CleanupOldFiles();
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine($"[FileLogger] Write failed: {line.Trim()}");
            }
        }
    }

    private string GetLogFilePath()
    {
        var date = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return Path.Combine(logDirectory, $"lumiere-{date}.log");
    }

    private void EnsureFileSizeLimit(string logPath)
    {
        if (!File.Exists(logPath))
        {
            return;
        }

        var fileInfo = new FileInfo(logPath);
        if (fileInfo.Length <= maxFileSizeBytes)
        {
            return;
        }

        try
        {
            var lines = File.ReadAllLines(logPath);
            var keepFrom = lines.Length / 2;
            File.WriteAllLines(logPath, lines.Skip(keepFrom));
        }
        catch
        {
            // If truncation fails, continue — the next write will append
        }
    }

    private void CleanupOldFiles()
    {
        try
        {
            var cutoff = DateTime.Now.AddDays(-retentionDays);
            foreach (var file in Directory.GetFiles(logDirectory, "lumiere-*.log"))
            {
                if (File.GetLastWriteTime(file) < cutoff)
                {
                    File.Delete(file);
                }
            }
        }
        catch
        {
            // Cleanup failures are non-fatal
        }
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
