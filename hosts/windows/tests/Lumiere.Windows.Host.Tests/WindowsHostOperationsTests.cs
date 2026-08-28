using Lumiere.Windows.Capture;
using Lumiere.Windows.Graphics.Hdr;
using Lumiere.Windows.Graphics.Output;
using Xunit;

namespace Lumiere.Windows.Host.Tests;

public sealed class WindowsHostOperationsTests
{
    [Fact]
    public async Task CaptureAsync_CreatesFolderAndForwardsCorrelatedFolderRequest()
    {
        var engine = new StubCaptureEngine
        {
            Result = TestCaptureResults.FolderSuccess("C:\\Pictures\\Lumiere\\capture.png"),
        };
        string? createdDirectory = null;
        await using var operations = new WindowsHostOperations(
            () => engine,
            () => "C:\\Pictures\\Lumiere",
            path => createdDirectory = path);

        var result = await operations.CaptureAsync(
            "capture-1",
            new HostCaptureRequest("display", "folder"));

        Assert.Equal("completed", result.Status);
        Assert.Equal("C:\\Pictures\\Lumiere", createdDirectory);
        Assert.Equal("capture-1", engine.Request?.CorrelationId);
        Assert.Equal(OutputTarget.Folder, engine.Request?.Delivery);
        Assert.Equal("C:\\Pictures\\Lumiere", engine.Request?.SaveDirectory);
    }

    [Fact]
    public async Task CaptureAsync_MapsHdrFolderSuccessWithoutClaimingHdrOutput()
    {
        var engine = new StubCaptureEngine
        {
            Result = TestCaptureResults.FolderSuccess(
                "C:\\Pictures\\Lumiere\\capture.png",
                HdrDisplayCapability.Unknown() with { State = HdrDisplayState.Active }),
        };
        await using var operations = CreateOperations(engine);

        var result = await operations.CaptureAsync(
            "capture-1",
            new HostCaptureRequest("display", "folder"));

        Assert.Equal("hdr", result.SourceDynamicRange);
        Assert.Equal("srgb-visual-match", result.OutputProfile);
        Assert.Equal("success", Assert.Single(result.Deliveries!).Status);
    }

    [Theory]
    [InlineData("region", "folder", "capture-unavailable")]
    [InlineData("display", "clipboard", "delivery-unavailable")]
    [InlineData("display", "both", "delivery-unavailable")]
    public async Task CaptureAsync_RejectsUnimplementedSliceWithoutCreatingEngine(
        string mode,
        string delivery,
        string expectedCode)
    {
        var factoryCalls = 0;
        await using var operations = new WindowsHostOperations(
            () =>
            {
                factoryCalls++;
                return new StubCaptureEngine();
            },
            () => "C:\\Pictures\\Lumiere",
            _ => { });

        var result = await operations.CaptureAsync(
            "capture-1",
            new HostCaptureRequest(mode, delivery));

        Assert.Equal("failed", result.Status);
        Assert.Equal(expectedCode, result.Failure?.Code);
        Assert.Equal(0, factoryCalls);
    }

    [Fact]
    public async Task CaptureAsync_MapsDeliveryFailureAsCompletedAcquisition()
    {
        var engine = new StubCaptureEngine
        {
            Result = new WindowsCaptureResult(
                WindowsCaptureOutcome.DeliveryFailed,
                "Output failed",
                "The folder is read-only.",
                Output: OutputResult.FromTargets(
                    OutputTargetResult.Failed(
                        OutputTarget.Folder,
                        "Failed to save",
                        "The folder is read-only."))),
        };
        await using var operations = CreateOperations(engine);

        var result = await operations.CaptureAsync(
            "capture-1",
            new HostCaptureRequest("display", "folder"));

        Assert.Equal("completed", result.Status);
        var delivery = Assert.Single(result.Deliveries!);
        Assert.Equal("failed", delivery.Status);
        Assert.Equal("delivery-failed", delivery.Failure?.Code);
    }

    [Fact]
    public async Task RepeatedCaptures_ReuseAndDisposeEngineOnce()
    {
        var engine = new StubCaptureEngine
        {
            Result = TestCaptureResults.FolderSuccess("C:\\Pictures\\Lumiere\\capture.png"),
        };
        var factoryCalls = 0;
        var operations = new WindowsHostOperations(
            () =>
            {
                factoryCalls++;
                return engine;
            },
            () => "C:\\Pictures\\Lumiere",
            _ => { });

        await operations.CaptureAsync(
            "capture-1",
            new HostCaptureRequest("display", "folder"));
        await operations.CaptureAsync(
            "capture-2",
            new HostCaptureRequest("display", "folder"));
        await operations.DisposeAsync();
        await operations.DisposeAsync();

        Assert.Equal(1, factoryCalls);
        Assert.Equal(2, engine.CaptureCalls);
        Assert.Equal(1, engine.DisposeCalls);
    }

    private static WindowsHostOperations CreateOperations(StubCaptureEngine engine) =>
        new(
            () => engine,
            () => "C:\\Pictures\\Lumiere",
            _ => { });
}

internal sealed class StubCaptureEngine : IWindowsDisplayCaptureEngine
{
    public WindowsCaptureResult Result { get; set; } =
        new(WindowsCaptureOutcome.Failed, "Capture failed", "No result configured.");

    public WindowsCaptureRequest? Request { get; private set; }

    public int CaptureCalls { get; private set; }

    public int DisposeCalls { get; private set; }

    public Task<WindowsCaptureResult> CaptureDisplayAsync(
        WindowsCaptureRequest request,
        CancellationToken cancellationToken = default)
    {
        Request = request;
        CaptureCalls++;
        return Task.FromResult(Result);
    }

    public ValueTask DisposeAsync()
    {
        DisposeCalls++;
        return ValueTask.CompletedTask;
    }
}

internal static class TestCaptureResults
{
    public static WindowsCaptureResult FolderSuccess(
        string path,
        HdrDisplayCapability? hdrCapability = null) =>
        new(
            WindowsCaptureOutcome.Delivered,
            "Saved",
            "Saved capture.",
            hdrCapability,
            OutputResult.FromTargets(
                OutputTargetResult.Success(
                    OutputTarget.Folder,
                    "Saved",
                    artifactPath: path)));
}
