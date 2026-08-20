using Lumiere.Windows.Graphics.Output;
using Lumiere.Windows.Graphics.Presentation;
using Xunit;

namespace Lumiere.Windows.Graphics.Tests.Output;

public sealed class ConfiguredOutputServiceTests
{
    [Fact]
    public async Task BothRoutesTheSameRequestCacheToBothTargets()
    {
        var clipboard = new RecordingOutputService(OutputTarget.Clipboard);
        var folder = new RecordingOutputService(OutputTarget.Folder);
        var service = new ConfiguredOutputService(clipboard, folder);
        using var texture = new CapturedFrameTexture(null, 10, 10, "test");

        var result = await service.ExecuteOutputAsync(new OutputRequest
        {
            Texture = texture,
            Delivery = OutputTarget.Both,
            SaveDirectory = "C:\\captures",
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(clipboard.Request!.ArtifactCache);
        Assert.Same(clipboard.Request.ArtifactCache, folder.Request!.ArtifactCache);
    }

    private sealed class RecordingOutputService(OutputTarget target) : IOutputService
    {
        public OutputRequest? Request { get; private set; }

        public Task<OutputResult> ExecuteOutputAsync(
            OutputRequest request,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult(OutputResult.FromTargets(
                OutputTargetResult.Success(target, "complete")));
        }
    }
}
