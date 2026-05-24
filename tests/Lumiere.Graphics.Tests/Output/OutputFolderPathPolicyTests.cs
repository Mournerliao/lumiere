using Lumiere.Graphics.Output;
using Xunit;

namespace Lumiere.Graphics.Tests.Output;

public sealed class OutputFolderPathPolicyTests
{
    [Fact]
    public void CreateCandidatePath_UsesInvariantTimestamp()
    {
        var policy = new OutputFolderPathPolicy(() => new DateTimeOffset(2026, 5, 23, 9, 8, 7, 123, TimeSpan.Zero));
        var outputPolicy = OutputPolicy.FromSettings(
            OutputTarget.Folder,
            copyAsImage: true,
            "C:\\Captures",
            timestampNaming: true,
            afterCaptureBehavior: null);

        var path = policy.CreateCandidatePath(outputPolicy, _ => false);

        Assert.Equal("C:\\Captures\\Lumiere-20260523-090807-123.png", path);
    }

    [Fact]
    public void CreateCandidatePath_AppendsDeterministicSuffixForExistingPath()
    {
        var policy = new OutputFolderPathPolicy(() => new DateTimeOffset(2026, 5, 23, 9, 8, 7, 123, TimeSpan.Zero));
        var outputPolicy = OutputPolicy.FromSettings(
            OutputTarget.Folder,
            copyAsImage: true,
            "C:\\Captures",
            timestampNaming: true,
            afterCaptureBehavior: null);

        var path = policy.CreateCandidatePath(
            outputPolicy,
            candidate => candidate.EndsWith("Lumiere-20260523-090807-123.png", StringComparison.Ordinal)
                || candidate.EndsWith("Lumiere-20260523-090807-123-01.png", StringComparison.Ordinal));

        Assert.Equal("C:\\Captures\\Lumiere-20260523-090807-123-02.png", path);
    }

    [Fact]
    public void CreateCandidatePath_ThrowsForMissingSavePath()
    {
        var policy = new OutputFolderPathPolicy();

        Assert.Throws<InvalidOperationException>(() => policy.CreateCandidatePath(
            OutputPolicy.FromSettings(
                OutputTarget.Folder,
                copyAsImage: true,
                savePath: null,
                timestampNaming: true,
                afterCaptureBehavior: null)));
    }
}
