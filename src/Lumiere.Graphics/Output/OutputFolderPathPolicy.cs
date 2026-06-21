using System.Globalization;

namespace Lumiere.Graphics.Output;

public sealed class OutputFolderPathPolicy
{
    private const string FilePrefix = "Lumiere";
    private readonly Func<DateTimeOffset> nowProvider;

    public OutputFolderPathPolicy(Func<DateTimeOffset>? nowProvider = null)
    {
        this.nowProvider = nowProvider ?? (() => DateTimeOffset.Now);
    }

    public string CreateCandidatePath(
        OutputPolicy policy,
        Func<string, bool>? exists) =>
        CreateCandidatePath(policy, fileExtension: "png", exists);

    public string CreateCandidatePath(
        OutputPolicy policy,
        string fileExtension = "png",
        Func<string, bool>? exists = null)
    {
        ArgumentNullException.ThrowIfNull(policy);
        if (string.IsNullOrWhiteSpace(policy.SavePath))
        {
            throw new InvalidOperationException("Save path is not configured.");
        }

        exists ??= File.Exists;
        var directory = policy.SavePath.Trim();
        var extension = NormalizeExtension(fileExtension);
        var baseName = policy.TimestampNaming
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
