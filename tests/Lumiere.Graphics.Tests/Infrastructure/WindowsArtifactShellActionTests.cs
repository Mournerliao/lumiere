using System.Diagnostics;
using Lumiere.Infrastructure.Interop;
using Xunit;

namespace Lumiere.Graphics.Tests.Infrastructure;

public sealed class WindowsArtifactShellActionTests
{
    [Fact]
    public async Task ExecuteAsync_OpenDirectoryPathUsesShellWhenDirectoryExists()
    {
        ProcessStartInfo? captured = null;
        var action = new WindowsArtifactShellAction(
            info =>
            {
                captured = info;
                return Process.GetCurrentProcess();
            },
            _ => false,
            path => path == "C:\\Validation");

        var result = await action.ExecuteAsync("C:\\Validation", ArtifactShellActionKind.Open);

        Assert.True(result.IsSuccess);
        Assert.NotNull(captured);
        Assert.Equal("C:\\Validation", captured!.FileName);
        Assert.True(captured.UseShellExecute);
    }

    [Fact]
    public async Task ExecuteAsync_FailsWhenNeitherFileNorDirectoryExists()
    {
        var action = new WindowsArtifactShellAction(
            _ => throw new InvalidOperationException("Should not start."),
            _ => false,
            _ => false);

        var result = await action.ExecuteAsync("C:\\Missing", ArtifactShellActionKind.Open);

        Assert.False(result.IsSuccess);
        Assert.Contains("does not exist", result.TechnicalDetail, StringComparison.OrdinalIgnoreCase);
    }
}
