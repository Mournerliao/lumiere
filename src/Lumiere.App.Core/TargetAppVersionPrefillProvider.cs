using Windows.ApplicationModel;
using Windows.Management.Deployment;
using System.Diagnostics;

namespace Lumiere.App;

public interface ITargetAppVersionPrefillProvider
{
    string? TryGetVersion(string targetAppName);
}

public sealed class WindowsTargetAppVersionPrefillProvider : ITargetAppVersionPrefillProvider
{
    private const string MicrosoftPaintFamilyName = "Microsoft.Paint_8wekyb3d8bbwe";
    private const string WindowsPhotosFamilyName = "Microsoft.Windows.Photos_8wekyb3d8bbwe";
    private const string MicrosoftEdgeExecutableName = "msedge.exe";

    private readonly Func<string, string?> tryGetPackageVersionByFamilyName;
    private readonly Func<string, string?> tryGetExecutableVersionByName;

    public WindowsTargetAppVersionPrefillProvider()
        : this(TryGetInstalledPackageVersionByFamilyName, TryGetInstalledExecutableVersionByName)
    {
    }

    internal WindowsTargetAppVersionPrefillProvider(
        Func<string, string?> tryGetPackageVersionByFamilyName,
        Func<string, string?> tryGetExecutableVersionByName)
    {
        this.tryGetPackageVersionByFamilyName =
            tryGetPackageVersionByFamilyName ?? throw new ArgumentNullException(nameof(tryGetPackageVersionByFamilyName));
        this.tryGetExecutableVersionByName =
            tryGetExecutableVersionByName ?? throw new ArgumentNullException(nameof(tryGetExecutableVersionByName));
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
            "Microsoft Edge" => tryGetExecutableVersionByName(MicrosoftEdgeExecutableName),
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

    private static string? TryGetInstalledExecutableVersionByName(string executableName)
    {
        foreach (var path in EnumerateKnownExecutablePaths(executableName))
        {
            try
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                var version = FileVersionInfo.GetVersionInfo(path).FileVersion?.Trim();
                if (!string.IsNullOrWhiteSpace(version))
                {
                    return version;
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException
                                       or UnauthorizedAccessException
                                       or ArgumentException
                                       or FileNotFoundException
                                       or System.ComponentModel.Win32Exception)
            {
                continue;
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateKnownExecutablePaths(string executableName)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        return
        [
            Path.Combine(programFilesX86, "Microsoft", "Edge", "Application", executableName),
            Path.Combine(programFiles, "Microsoft", "Edge", "Application", executableName),
            Path.Combine(localAppData, "Microsoft", "Edge", "Application", executableName),
        ];
    }
}
