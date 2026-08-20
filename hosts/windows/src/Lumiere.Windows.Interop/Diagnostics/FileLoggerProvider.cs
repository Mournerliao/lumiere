using Microsoft.Extensions.Logging;

namespace Lumiere.Windows.Interop.Diagnostics;

public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string logDirectory;
    private readonly int maxFileSizeBytes;
    private readonly int retentionDays;

    public FileLoggerProvider(
        string? logDirectory = null,
        int maxFileSizeBytes = 10 * 1024 * 1024,
        int retentionDays = 7)
    {
        this.logDirectory = logDirectory ?? DefaultLogDirectory();
        this.maxFileSizeBytes = maxFileSizeBytes;
        this.retentionDays = retentionDays;
    }

    public ILogger CreateLogger(string categoryName) =>
        new FileLogger(categoryName, logDirectory, maxFileSizeBytes, retentionDays);

    public void Dispose()
    {
        // No unmanaged resources; file handles are opened/closed per-write
    }

    private static string DefaultLogDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "Lumiere", "logs");
    }
}
