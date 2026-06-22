using Windows.ApplicationModel;
using Windows.Management.Deployment;

namespace Lumiere.App;

public interface ITargetAppVersionPrefillProvider
{
    string? TryGetVersion(string targetAppName);
}

public sealed class WindowsTargetAppVersionPrefillProvider : ITargetAppVersionPrefillProvider
{
    private const string MicrosoftPaintFamilyName = "Microsoft.Paint_8wekyb3d8bbwe";
    private const string WindowsPhotosFamilyName = "Microsoft.Windows.Photos_8wekyb3d8bbwe";

    private readonly Func<string, string?> tryGetPackageVersionByFamilyName;

    public WindowsTargetAppVersionPrefillProvider()
        : this(TryGetInstalledPackageVersionByFamilyName)
    {
    }

    internal WindowsTargetAppVersionPrefillProvider(
        Func<string, string?> tryGetPackageVersionByFamilyName)
    {
        this.tryGetPackageVersionByFamilyName =
            tryGetPackageVersionByFamilyName ?? throw new ArgumentNullException(nameof(tryGetPackageVersionByFamilyName));
    }

    public string? TryGetVersion(string targetAppName)
    {
        if (string.IsNullOrWhiteSpace(targetAppName))
        {
            return null;
        }

        return targetAppName.Trim() switch
        {
            "Microsoft Paint" => tryGetPackageVersionByFamilyName(MicrosoftPaintFamilyName),
            "Windows Photos" => tryGetPackageVersionByFamilyName(WindowsPhotosFamilyName),
            _ => null,
        };
    }

    private static string? TryGetInstalledPackageVersionByFamilyName(string familyName)
    {
        try
        {
            var bestMatch = new PackageManager()
                .FindPackagesForUser(string.Empty)
                .Where(package => string.Equals(
                    package.Id.FamilyName,
                    familyName,
                    StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(package => package.Id.Version.Major)
                .ThenByDescending(package => package.Id.Version.Minor)
                .ThenByDescending(package => package.Id.Version.Build)
                .ThenByDescending(package => package.Id.Version.Revision)
                .FirstOrDefault();

            return bestMatch is null
                ? null
                : FormatVersion(bestMatch.Id.Version);
        }
        catch (Exception ex) when (ex is InvalidOperationException
                                   or UnauthorizedAccessException
                                   or ArgumentException
                                   or System.Runtime.InteropServices.COMException)
        {
            return null;
        }
    }

    private static string FormatVersion(PackageVersion version) =>
        $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
}
