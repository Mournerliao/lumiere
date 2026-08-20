using Lumiere.Windows.Graphics.Output;
using Xunit;

namespace Lumiere.Windows.Graphics.Tests.Output;

public sealed class OutputFolderPathPolicyTests
{
    [Fact]
    public void CreatesTimestampedPngPathAndAvoidsCollision()
    {
        var policy = new OutputFolderPathPolicy(
            () => new DateTimeOffset(2026, 8, 20, 12, 34, 56, 789, TimeSpan.Zero));
        var first = Path.Combine("C:\\captures", "Lumiere-20260820-123456-789.png");

        var path = policy.CreateCandidatePath(
            "C:\\captures",
            timestampNaming: true,
            exists: candidate => candidate == first);

        Assert.Equal(Path.Combine("C:\\captures", "Lumiere-20260820-123456-789-01.png"), path);
    }
}
