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
            NoTargetCapability,
            static () => "target-token",
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

    [Fact]
    public async Task CaptureAsync_ForwardsCustomSaveDirectoryWithoutResolvingDefault()
    {
        var engine = new StubCaptureEngine
        {
            Result = TestCaptureResults.FolderSuccess("D:\\Screenshots\\capture.png"),
        };
        var defaultDirectoryCalls = 0;
        string? createdDirectory = null;
        await using var operations = new WindowsHostOperations(
            () => engine,
            NoTargetCapability,
            static () => "target-token",
            () =>
            {
                defaultDirectoryCalls++;
                return "C:\\Pictures\\Lumiere";
            },
            path => createdDirectory = path);

        var result = await operations.CaptureAsync(
            "capture-custom",
            new HostCaptureRequest(
                "display",
                "folder",
                SaveDirectory: "D:\\Screenshots"));

        Assert.Equal("completed", result.Status);
        Assert.Equal(0, defaultDirectoryCalls);
        Assert.Equal("D:\\Screenshots", createdDirectory);
        Assert.Equal("D:\\Screenshots", engine.Request?.SaveDirectory);
    }

    [Fact]
    public async Task CaptureAsync_ForwardsClipboardWithoutCreatingFolder()
    {
        var engine = new StubCaptureEngine
        {
            Result = TestCaptureResults.ClipboardSuccess(),
        };
        var directoryCalls = 0;
        var createDirectoryCalls = 0;
        await using var operations = new WindowsHostOperations(
            () => engine,
            NoTargetCapability,
            static () => "target-token",
            () =>
            {
                directoryCalls++;
                return "C:\\Pictures\\Lumiere";
            },
            _ => createDirectoryCalls++);

        var result = await operations.CaptureAsync(
            "capture-clipboard",
            new HostCaptureRequest("display", "clipboard"));

        Assert.Equal("completed", result.Status);
        Assert.Equal(OutputTarget.Clipboard, engine.Request?.Delivery);
        Assert.Null(engine.Request?.SaveDirectory);
        Assert.Equal(0, directoryCalls);
        Assert.Equal(0, createDirectoryCalls);
        var delivery = Assert.Single(result.Deliveries!);
        Assert.Equal("clipboard", delivery.Target);
        Assert.Equal("success", delivery.Status);
        Assert.Null(delivery.FilePath);
    }

    [Fact]
    public async Task CaptureAsync_MapsBothDeliveryOutcomesIndependently()
    {
        var engine = new StubCaptureEngine
        {
            Result = TestCaptureResults.BothPartialSuccess("C:\\Pictures\\Lumiere\\capture.png"),
        };
        await using var operations = CreateOperations(engine);

        var result = await operations.CaptureAsync(
            "capture-both",
            new HostCaptureRequest("display", "both"));

        Assert.Equal("completed", result.Status);
        Assert.Equal(OutputTarget.Both, engine.Request?.Delivery);
        Assert.Collection(
            result.Deliveries!,
            clipboard =>
            {
                Assert.Equal("clipboard", clipboard.Target);
                Assert.Equal("failed", clipboard.Status);
                Assert.Equal("delivery-failed", clipboard.Failure?.Code);
            },
            folder =>
            {
                Assert.Equal("folder", folder.Target);
                Assert.Equal("success", folder.Status);
                Assert.Equal("C:\\Pictures\\Lumiere\\capture.png", folder.FilePath);
            });
    }

    [Theory]
    [InlineData("region", "folder", "capture-unavailable")]
    [InlineData("display", "other", "delivery-unavailable")]
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
            NoTargetCapability,
            static () => "target-token",
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
        Assert.Equal("Could not save the screenshot.", delivery.Failure?.Message);
        Assert.DoesNotContain("read-only", delivery.Failure?.Message);
    }

    [Theory]
    [InlineData(WindowsCaptureOutcome.TimedOut, "capture-unavailable", "Windows capture timed out. Try again.")]
    [InlineData(WindowsCaptureOutcome.Unavailable, "capture-unavailable", "The capture target is unavailable. Try again.")]
    [InlineData(WindowsCaptureOutcome.Unsupported, "capture-unavailable", "Screen capture is unavailable on this Windows system.")]
    [InlineData(WindowsCaptureOutcome.Failed, "unexpected-failure", "Windows capture failed. Try again.")]
    public async Task CaptureAsync_DoesNotExposeNativeFailureDetails(
        WindowsCaptureOutcome outcome,
        string expectedCode,
        string expectedMessage)
    {
        var engine = new StubCaptureEngine
        {
            Result = new WindowsCaptureResult(
                outcome,
                "Capture failed",
                "COMException at C:\\private\\source.cs:42"),
        };
        await using var operations = CreateOperations(engine);

        var result = await operations.CaptureAsync(
            "capture-failure",
            new HostCaptureRequest("display", "folder"));

        Assert.Equal("failed", result.Status);
        Assert.Equal(expectedCode, result.Failure?.Code);
        Assert.Equal(expectedMessage, result.Failure?.Message);
        Assert.DoesNotContain("COMException", result.Failure?.Message);
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
            NoTargetCapability,
            static () => "target-token",
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

    [Fact]
    public async Task DisposeAsync_DelegatesInFlightCancellationToOwnedEngine()
    {
        var engine = new BlockingCaptureEngine();
        var operations = new WindowsHostOperations(
            () => engine,
            NoTargetCapability,
            static () => "target-token",
            () => "C:\\Pictures\\Lumiere",
            _ => { });

        var capture = operations.CaptureAsync(
            "capture-in-flight",
            new HostCaptureRequest("display", "folder"));
        await engine.CaptureStarted;
        await operations.DisposeAsync();
        var result = await capture;

        Assert.Equal("cancelled", result.Status);
        Assert.Equal(1, engine.DisposeCalls);
    }

    [Theory]
    [InlineData(WindowsTargetHdrState.Active, "supported")]
    [InlineData(WindowsTargetHdrState.Inactive, "unavailable")]
    [InlineData(WindowsTargetHdrState.Unknown, "unvalidated")]
    public async Task GetCapabilities_ProjectsCurrentTarget(
        WindowsTargetHdrState hdrState,
        string expectedHdrCapture)
    {
        await using var operations = new WindowsHostOperations(
            () => new StubCaptureEngine(),
            () => new WindowsTargetCapability(
                hdrState,
                new WindowsTargetLogicalSize(2560, 1440)),
            static () => "target-token-17",
            () => "C:\\Pictures\\Lumiere",
            _ => { });

        var capabilities = operations.GetCapabilities();

        Assert.Equal(expectedHdrCapture, capabilities.HdrCapture);
        Assert.Equal("target-token-17", capabilities.ActiveTarget?.Id);
        Assert.Equal(2560, capabilities.ActiveTarget?.LogicalSize.Width);
        Assert.Equal(1440, capabilities.ActiveTarget?.LogicalSize.Height);
        Assert.Equal(["display"], capabilities.CaptureModes);
        Assert.Equal(["clipboard", "folder"], capabilities.DeliveryTargets);
    }

    [Fact]
    public async Task GetCapabilities_OmitsTargetWhenResolutionIsUnavailable()
    {
        await using var operations = CreateOperations(new StubCaptureEngine());

        var capabilities = operations.GetCapabilities();

        Assert.Equal("unvalidated", capabilities.HdrCapture);
        Assert.Null(capabilities.ActiveTarget);
    }

    [Fact]
    public async Task RegionCapture_ConsumesIssuedTargetAndForwardsLogicalGeometry()
    {
        var engine = new StubCaptureEngine
        {
            Result = TestCaptureResults.ClipboardSuccess(),
        };
        var target = CreateRegionCapability();
        await using var operations = new WindowsHostOperations(
            () => engine,
            () => target,
            static () => "region-target-17",
            () => "C:\\Pictures\\Lumiere",
            _ => { });
        var capabilities = operations.GetCapabilities();

        var result = await operations.CaptureAsync(
            "capture-region",
            new HostCaptureRequest(
                "region",
                "clipboard",
                capabilities.ActiveTarget!.Id,
                new HostCaptureGeometry(12.5, 20, 300, 200)));
        var repeated = await operations.CaptureAsync(
            "capture-region-repeat",
            new HostCaptureRequest(
                "region",
                "clipboard",
                capabilities.ActiveTarget.Id,
                new HostCaptureGeometry(12.5, 20, 300, 200)));

        Assert.Equal(["region", "display"], capabilities.CaptureModes);
        Assert.Equal("completed", result.Status);
        Assert.Same(target, engine.RegionTarget);
        Assert.Equal(new WindowsRegionGeometry(12.5, 20, 300, 200), engine.RegionGeometry);
        Assert.Equal("failed", repeated.Status);
        Assert.Equal("capture-unavailable", repeated.Failure?.Code);
    }

    private static WindowsHostOperations CreateOperations(StubCaptureEngine engine) =>
        new(
            () => engine,
            NoTargetCapability,
            static () => "target-token",
            () => "C:\\Pictures\\Lumiere",
            _ => { });

    private static WindowsTargetCapability? NoTargetCapability() => null;

    internal static WindowsTargetCapability CreateRegionCapability()
    {
        var captureTarget = CaptureTarget.CreateForTest(
            new global::Windows.Graphics.SizeInt32 { Width = 3840, Height = 2160 },
            "test display",
            CaptureTargetKind.Display);
        return WindowsTargetCapability.CreateForTest(
            WindowsTargetHdrState.Active,
            new WindowsTargetLogicalSize(2560, 1440),
            captureTarget);
    }
}

internal sealed class StubCaptureEngine : IWindowsCaptureEngine
{
    public WindowsCaptureResult Result { get; set; } =
        new(WindowsCaptureOutcome.Failed, "Capture failed", "No result configured.");

    public WindowsCaptureRequest? Request { get; private set; }

    public WindowsTargetCapability? RegionTarget { get; private set; }

    public WindowsRegionGeometry? RegionGeometry { get; private set; }

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

    public Task<WindowsCaptureResult> CaptureRegionAsync(
        WindowsCaptureRequest request,
        WindowsTargetCapability target,
        WindowsRegionGeometry geometry,
        CancellationToken cancellationToken = default)
    {
        Request = request;
        RegionTarget = target;
        RegionGeometry = geometry;
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
    public static WindowsCaptureResult ClipboardSuccess() =>
        new(
            WindowsCaptureOutcome.Delivered,
            "Copied",
            "Copied capture.",
            Output: OutputResult.FromTargets(
                OutputTargetResult.Success(
                    OutputTarget.Clipboard,
                    "Copied",
                    bytesWritten: 42)));

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

    public static WindowsCaptureResult BothPartialSuccess(string path) =>
        new(
            WindowsCaptureOutcome.Delivered,
            "Output partially complete",
            "Clipboard failed; folder succeeded.",
            Output: OutputResult.FromTargets(
                OutputTargetResult.Failed(
                    OutputTarget.Clipboard,
                    "Failed to copy",
                    "The clipboard is busy."),
                OutputTargetResult.Success(
                    OutputTarget.Folder,
                    "Saved",
                    artifactPath: path)));
}

internal sealed class BlockingCaptureEngine : IWindowsCaptureEngine
{
    private readonly TaskCompletionSource captureStarted = new(
        TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<WindowsCaptureResult> captureCompletion = new(
        TaskCreationOptions.RunContinuationsAsynchronously);

    public Task CaptureStarted => captureStarted.Task;

    public int DisposeCalls { get; private set; }

    public Task<WindowsCaptureResult> CaptureDisplayAsync(
        WindowsCaptureRequest request,
        CancellationToken cancellationToken = default)
    {
        captureStarted.SetResult();
        return captureCompletion.Task;
    }

    public Task<WindowsCaptureResult> CaptureRegionAsync(
        WindowsCaptureRequest request,
        WindowsTargetCapability target,
        WindowsRegionGeometry geometry,
        CancellationToken cancellationToken = default) =>
        CaptureDisplayAsync(request, cancellationToken);

    public ValueTask DisposeAsync()
    {
        DisposeCalls++;
        captureCompletion.TrySetResult(new WindowsCaptureResult(
            WindowsCaptureOutcome.Cancelled,
            "Capture cancelled",
            "Engine lifetime ended."));
        return ValueTask.CompletedTask;
    }
}
