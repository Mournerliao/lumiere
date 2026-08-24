using System.Globalization;

namespace Lumiere.Windows.Graphics.Output;

internal sealed class OutputFolderPathPolicy
{
    private const string FilePrefix = "Lumiere";
    private readonly Func<DateTimeOffset> nowProvider;

    public OutputFolderPathPolicy(Func<DateTimeOffset>? nowProvider = null)
    {
        this.nowProvider = nowProvider ?? (() => DateTimeOffset.Now);
    }

    public string CreateCandidatePath(
        string saveDirectory,
        bool timestampNaming,
        string fileExtension = "png",
        Func<string, bool>? exists = null)
    {
        if (string.IsNullOrWhiteSpace(saveDirectory))
        {
            throw new ArgumentException("Save directory must be provided.", nameof(saveDirectory));
        }

        exists ??= File.Exists;
        var directory = saveDirectory.Trim();
        var extension = NormalizeExtension(fileExtension);
        var baseName = timestampNaming
            ? $"{FilePrefix}-{nowProvider():yyyyMMdd-HHmmss-fff}"
            : FilePrefix;
        var candidate = Path.Combine(directory, $"{baseName}.{extension}");
        var suffix = 1;

        while (exists(candidate))
        {
            candidate = Path.Combine(directory, string.Create(
                CultureInfo.InvariantCulture,
                $"{baseName}-{suffix:00}.{extension}"));
            suffix++;
        }

        return candidate;
    }

    private static string NormalizeExtension(string? fileExtension) =>
        string.IsNullOrWhiteSpace(fileExtension)
            ? "bin"
            : fileExtension.Trim().TrimStart('.').ToLowerInvariant();
}
