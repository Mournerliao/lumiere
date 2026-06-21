using Lumiere.Graphics.Clipboard;
using Lumiere.Graphics.Output;
using Lumiere.Graphics.Presentation;
using Xunit;

namespace Lumiere.Graphics.Tests.Output;

public sealed class ClipboardOutputServicePolicyTests
{
    [Fact]
    public async Task ExecuteOutputAsync_EnabledClipboardPolicyInvokesExecutionSeam()
    {
        var calls = 0;
        using var frame = new CapturedFrameTexture(null, 16, 16, "Test frame");
        var service = new ClipboardOutputService((request, _) =>
        {
            calls++;
            Assert.Same(frame, request.Texture);
            return Task.FromResult(OutputResult.ClipboardSuccess(128));
        });

        var result = await service.ExecuteOutputAsync(new OutputRequest
        {
            Texture = frame,
            Policy = OutputPolicy.FromSettings(
                OutputTarget.Clipboard,
                copyAsImage: true,
                savePath: null,
                timestampNaming: true,
                afterCaptureBehavior: null),
        });

        Assert.Equal(1, calls);
        Assert.True(result.IsSuccess);
        Assert.Equal(OutputOutcome.Success, result.ClipboardOutcome);
    }

    [Theory]
    [InlineData(OutputTarget.Folder, true)]
    [InlineData(OutputTarget.Clipboard, false)]
    [InlineData(OutputTarget.Both, false)]
    public async Task ExecuteOutputAsync_DisabledClipboardPolicySkipsWithoutInvokingExecutionSeam(
        OutputTarget target,
        bool copyAsImage)
    {
        var calls = 0;
        using var frame = new CapturedFrameTexture(null, 16, 16, "Test frame");
        var service = new ClipboardOutputService((_, _) =>
        {
            calls++;
            return Task.FromResult(OutputResult.ClipboardSuccess(128));
        });

        var result = await service.ExecuteOutputAsync(new OutputRequest
        {
            Texture = frame,
            Policy = OutputPolicy.FromSettings(
                target,
                copyAsImage,
                savePath: null,
                timestampNaming: true,
                afterCaptureBehavior: null),
        });

        Assert.Equal(0, calls);
        Assert.False(result.IsSuccess);
        Assert.Equal(OutputOutcome.Skipped, result.ClipboardOutcome);
        Assert.Equal("Clipboard output skipped by settings", result.UserMessage);
    }

    [Fact]
    public async Task ExecuteOutputAsync_ExecutionFailureReturnsRecoverableFailure()
    {
        using var frame = new CapturedFrameTexture(null, 16, 16, "Test frame");
        var service = new ClipboardOutputService((_, _) =>
            throw new InvalidOperationException("Clipboard unavailable"));

        var result = await service.ExecuteOutputAsync(new OutputRequest
        {
            Texture = frame,
            Policy = OutputPolicy.Default,
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(OutputOutcome.Failed, result.ClipboardOutcome);
        Assert.Equal("Failed to copy to clipboard", result.UserMessage);
        Assert.Contains("Clipboard unavailable", result.TechnicalDetail);
    }

    [Fact]
    public async Task EncodeArtifactAsync_RejectsHdrProfileBecausePngEncoderIsCompatibilityOnly()
    {
        using var frame = new CapturedFrameTexture(null, 16, 16, "Test frame");
        using var service = new ClipboardOutputService((_, _) =>
            Task.FromResult(OutputResult.ClipboardSuccess(128)));
        var hdrProfile = OutputProfileContract.Hdr10Pq with
        {
            IsExecutable = true,
            FidelityMode = OutputFidelityMode.HdrPreserved,
            FormatContract = CompleteHdr10Contract,
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.EncodeArtifactAsync(frame, cropRegion: null, hdrProfile));

        Assert.Contains("cannot create HDR10", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static OutputFormatContract CompleteHdr10Contract { get; } =
        new(
            OutputPixelFormat.R16G16B16A16Float,
            OutputPixelFormat.R16G16B16A16Float,
            OutputTransferFunction.PqSt2084,
            OutputColorPrimaries.Bt2020,
            OutputConversionPolicy.PreserveHdrWithDefinedToneMapping,
            OutputMetadataPolicy.AttachHdr10StaticMetadata,
            OutputTargetAppAssumption.RequiresHdrViewerValidation);
}
