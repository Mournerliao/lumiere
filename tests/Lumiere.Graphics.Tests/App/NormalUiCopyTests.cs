using Lumiere.App;
using Lumiere.Graphics.Output;
using Xunit;

namespace Lumiere.Graphics.Tests.App;

public sealed class NormalUiCopyTests
{
    [Theory]
    [InlineData("src/Lumiere.App/MainWindow.xaml")]
    [InlineData("src/Lumiere.App/TrayMenuWindow.xaml")]
    [InlineData("src/Lumiere.Overlay/OverlayWindow.xaml")]
    public void NormalUiMarkupDoesNotExposeValidationWorkbenchCopy(string relativePath)
    {
        var source = File.ReadAllText(FindRepoFile(relativePath));

        Assert.DoesNotContain("Export option HDR10", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Export option P3", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Selected profile contract", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Loaded evidence", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Create validation draft", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Technical details", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Fidelity claim", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Output profile", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Target-aware state", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Target unresolved", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OutputResultProjectionUsesMvpSafeNormalCopy()
    {
        var projection = OutputResultProjection.Project(
            OutputResult.ClipboardSuccess(1024)
                .WithRequestedProfile(OutputProfileContract.FromSettingsValue("HDR10")));

        Assert.Equal("Clipboard copied as sRGB Visual Match", projection.Detail);
        Assert.Contains("Requested HDR10; using sRGB Visual Match output.", projection.FidelityDetail, StringComparison.Ordinal);
        Assert.DoesNotContain("fidelity claim", projection.FidelityDetail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("profile gate", projection.FidelityDetail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("contract", projection.FidelityDetail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("evidence", projection.FidelityDetail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR-preserved", projection.FidelityDetail, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepoFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate {relativePath} from {AppContext.BaseDirectory}.");
    }
}
