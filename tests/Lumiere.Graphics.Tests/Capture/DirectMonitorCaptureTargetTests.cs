using Lumiere.Capture;
using Lumiere.Graphics.Hdr;
using Lumiere.Infrastructure.Interop;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Xunit;

namespace Lumiere.Graphics.Tests.Capture;

public sealed class DirectMonitorCaptureTargetTests
{
    [Fact]
    public void FromDisplayItemSetsKindToDisplay()
    {
        var target = CaptureTarget.CreateForTest(
            new SizeInt32 { Width = 1920, Height = 1080 },
            "DISPLAY1",
            CaptureTargetKind.Display);

        Assert.Equal(CaptureTargetKind.Display, target.Kind);
    }

    [Fact]
    public void FromDisplayItemUsesProvidedDisplayName()
    {
        var target = CaptureTarget.CreateForTest(
            new SizeInt32 { Width = 1920, Height = 1080 },
            "My Monitor",
            CaptureTargetKind.Display);

        Assert.Equal("My Monitor", target.DisplayName);
    }

    [Fact]
    public void FromDisplayItemDefaultsDisplayNameToCaptureTarget()
    {
        var target = CaptureTarget.CreateForTest(
            new SizeInt32 { Width = 1920, Height = 1080 },
            "",
            CaptureTargetKind.Display);

        Assert.Equal("Capture target", target.DisplayName);
    }

    [Fact]
    public void FromDisplayItemValidatesPositiveSize()
    {
        Assert.Throws<ArgumentException>(
            () => CaptureTarget.CreateForTest(
                new SizeInt32 { Width = 0, Height = 1080 },
                "DISPLAY1",
                CaptureTargetKind.Display));
    }

    [Fact]
    public void FromDisplayItemValidatesMaxTextureDimension()
    {
        Assert.Throws<ArgumentException>(
            () => CaptureTarget.CreateForTest(
                new SizeInt32 { Width = 16_385, Height = 1080 },
                "DISPLAY1",
                CaptureTargetKind.Display));
    }

    [Fact]
    public void FromDisplayItemAcceptsValidDisplaySizes()
    {
        var target1080p = CaptureTarget.CreateForTest(
            new SizeInt32 { Width = 1920, Height = 1080 },
            "DISPLAY1",
            CaptureTargetKind.Display);

        var target4k = CaptureTarget.CreateForTest(
            new SizeInt32 { Width = 3840, Height = 2160 },
            "DISPLAY2",
            CaptureTargetKind.Display);

        Assert.Equal(1920, target1080p.Size.Width);
        Assert.Equal(1080, target1080p.Size.Height);
        Assert.Equal(3840, target4k.Size.Width);
        Assert.Equal(2160, target4k.Size.Height);
    }
}

public sealed class DirectMonitorCaptureTargetSelectionServiceTests
{
    [Fact]
    public async Task DirectSelectionReturnsSelectedWithDisplayTarget()
    {
        var fakeMonitor = new MonitorHandle(new IntPtr(12345), "DISPLAY1");
        var service = new DirectMonitorCaptureTargetSelectionService(
            () => fakeMonitor,
            monitor => CreateFakeDisplayTarget(monitor, 1920, 1080),
            () => true);

        var result = await service.SelectDirectMonitorTargetAsync();

        Assert.Equal(SelectionOutcome.Selected, result.Outcome);
        Assert.NotNull(result.Target);
        Assert.Equal(CaptureTargetKind.Display, result.Target.Kind);
        Assert.Equal(1920, result.Target.Size.Width);
        Assert.Equal(1080, result.Target.Size.Height);
        Assert.Equal("DISPLAY1", result.Target.DisplayIdentity?.DeviceName);
        Assert.Equal(1920, result.Target.DisplayIdentity?.Width);
        Assert.Equal(1080, result.Target.DisplayIdentity?.Height);
    }

    [Fact]
    public async Task DirectSelectionReturnsUnsupportedWhenCaptureNotSupported()
    {
        var fakeMonitor = new MonitorHandle(new IntPtr(12345), "DISPLAY1");
        var service = new DirectMonitorCaptureTargetSelectionService(
            () => fakeMonitor,
            _ => throw new InvalidOperationException("Should not be called"),
            () => false);

        var result = await service.SelectDirectMonitorTargetAsync();

        Assert.Equal(SelectionOutcome.Unsupported, result.Outcome);
        Assert.Null(result.Target);
        Assert.Equal(PreviewReadinessState.Unsupported, result.Readiness.State);
        Assert.Contains("IsSupported returned false", result.Readiness.TechnicalDetail);
    }

    [Fact]
    public async Task DirectSelectionDoesNotCallPickerWhenSuccessful()
    {
        var fakeMonitor = new MonitorHandle(new IntPtr(12345), "DISPLAY1");
        var pickerCalled = false;
        var fakePicker = new ThrowingFakeCaptureTargetPicker(() => pickerCalled = true);
        var service = new DirectMonitorCaptureTargetSelectionService(
            () => fakeMonitor,
            monitor => CreateFakeDisplayTarget(monitor, 1920, 1080),
            () => true,
            fakePicker);

        var result = await service.SelectDirectMonitorTargetAsync();

        Assert.Equal(SelectionOutcome.Selected, result.Outcome);
        Assert.False(pickerCalled, "Picker should not be called during direct monitor selection.");
    }

    [Fact]
    public async Task DirectOnlyFactoryDoesNotCarryFallbackPicker()
    {
        var service = DirectMonitorCaptureTargetSelectionService.CreateDirectOnly(
            () => new MonitorHandle(new IntPtr(12345), "DISPLAY1"),
            _ => throw new NotSupportedException("CreateForMonitor not supported"),
            () => true);

        var result = await service.SelectDirectMonitorTargetAsync();

        Assert.False(service.HasFallbackPicker);
        Assert.Equal(SelectionOutcome.Unsupported, result.Outcome);
        Assert.Null(result.Target);
    }

    [Fact]
    public async Task DirectSelectionMapsNativeInteropExceptionToFailed()
    {
        var service = new DirectMonitorCaptureTargetSelectionService(
            () => throw new NativeInteropException(
                "MonitorFromPoint",
                "Capture",
                -2147467259,
                "Test interop failure",
                "Could not determine the target display."),
            _ => throw new InvalidOperationException("Should not be called"),
            () => true);

        var result = await service.SelectDirectMonitorTargetAsync();

        Assert.Equal(SelectionOutcome.Failed, result.Outcome);
        Assert.Null(result.Target);
        Assert.Equal(PreviewReadinessState.Failed, result.Readiness.State);
    }

    [Fact]
    public async Task DirectSelectionMapsNotSupportedExceptionToUnsupported()
    {
        var service = new DirectMonitorCaptureTargetSelectionService(
            () => new MonitorHandle(new IntPtr(12345), "DISPLAY1"),
            _ => throw new NotSupportedException("CreateForMonitor not supported"),
            () => true);

        var result = await service.SelectDirectMonitorTargetAsync();

        Assert.Equal(SelectionOutcome.Unsupported, result.Outcome);
        Assert.Null(result.Target);
        Assert.Equal(PreviewReadinessState.Unsupported, result.Readiness.State);
    }

    [Fact]
    public async Task DirectSelectionMapsArgumentExceptionToFailed()
    {
        var service = new DirectMonitorCaptureTargetSelectionService(
            () => new MonitorHandle(new IntPtr(12345), "DISPLAY1"),
            _ => throw new ArgumentException("Invalid monitor handle"),
            () => true);

        var result = await service.SelectDirectMonitorTargetAsync();

        Assert.Equal(SelectionOutcome.Failed, result.Outcome);
        Assert.Null(result.Target);
        Assert.Equal(PreviewReadinessState.Failed, result.Readiness.State);
    }

    [Fact]
    public async Task DirectSelectionMapsGenericExceptionToFailed()
    {
        var service = new DirectMonitorCaptureTargetSelectionService(
            () => new MonitorHandle(new IntPtr(12345), "DISPLAY1"),
            _ => throw new InvalidOperationException("Unexpected error"),
            () => true);

        var result = await service.SelectDirectMonitorTargetAsync();

        Assert.Equal(SelectionOutcome.Failed, result.Outcome);
        Assert.Null(result.Target);
        Assert.Equal(PreviewReadinessState.Failed, result.Readiness.State);
    }

    [Fact]
    public async Task FallbackProviderReturnsSelectedWhenTargetProvided()
    {
        var service = new DirectMonitorCaptureTargetSelectionService(
            () => throw new InvalidOperationException("Should not be called"),
            _ => throw new InvalidOperationException("Should not be called"),
            () => true,
            () => Task.FromResult<CaptureTarget?>(
                CaptureTarget.CreateForTest(
                    new SizeInt32 { Width = 800, Height = 600 },
                    "Picked target")));

        var result = await service.SelectWithFallbackPickerAsync();

        Assert.Equal(SelectionOutcome.Selected, result.Outcome);
        Assert.NotNull(result.Target);
        Assert.Equal(CaptureTargetKind.Unknown, result.Target.Kind);
    }

    [Fact]
    public async Task FallbackPickerReturnsCanceledWhenPickerReturnsNull()
    {
        var fakePicker = new FakeCaptureTargetPicker((GraphicsCaptureItem?)null);
        var service = new DirectMonitorCaptureTargetSelectionService(
            () => throw new InvalidOperationException("Should not be called"),
            _ => throw new InvalidOperationException("Should not be called"),
            () => true,
            fakePicker);

        var result = await service.SelectWithFallbackPickerAsync();

        Assert.Equal(SelectionOutcome.Canceled, result.Outcome);
        Assert.Null(result.Target);
    }

    [Fact]
    public async Task FallbackPickerReturnsFailedWhenNoPickerConfigured()
    {
        var service = new DirectMonitorCaptureTargetSelectionService(
            () => new MonitorHandle(new IntPtr(12345), "DISPLAY1"),
            _ => throw new InvalidOperationException("Should not be called"),
            () => true);

        var result = await service.SelectWithFallbackPickerAsync();

        Assert.Equal(SelectionOutcome.Failed, result.Outcome);
        Assert.Null(result.Target);
        Assert.Contains("no fallback picker was configured", result.Readiness.TechnicalDetail);
    }

    [Fact]
    public async Task FallbackPickerReturnsUnsupportedWhenCaptureNotSupported()
    {
        var fakePicker = new FakeCaptureTargetPicker(
            new InvalidOperationException("Should not be called"));
        var service = new DirectMonitorCaptureTargetSelectionService(
            () => throw new InvalidOperationException("Should not be called"),
            _ => throw new InvalidOperationException("Should not be called"),
            () => false,
            fakePicker);

        var result = await service.SelectWithFallbackPickerAsync();

        Assert.Equal(SelectionOutcome.Unsupported, result.Outcome);
        Assert.Null(result.Target);
    }

    [Fact]
    public void HasFallbackPickerReturnsTrueWhenPickerProvided()
    {
        var fakePicker = new FakeCaptureTargetPicker((GraphicsCaptureItem?)null);
        var service = new DirectMonitorCaptureTargetSelectionService(
            () => new MonitorHandle(IntPtr.Zero, "Display"),
            _ => throw new InvalidOperationException(),
            fallbackPicker: fakePicker);

        Assert.True(service.HasFallbackPicker);
    }

    [Fact]
    public void HasFallbackPickerReturnsFalseWhenNoPickerProvided()
    {
        var service = new DirectMonitorCaptureTargetSelectionService(
            () => new MonitorHandle(IntPtr.Zero, "Display"),
            _ => throw new InvalidOperationException());

        Assert.False(service.HasFallbackPicker);
    }

    private static CaptureTarget CreateFakeDisplayTarget(
        MonitorHandle monitor,
        int width,
        int height)
    {
        return CaptureTarget.CreateForTest(
            new SizeInt32 { Width = width, Height = height },
            monitor.DisplayName,
            CaptureTargetKind.Display);
    }

    private sealed class FakeCaptureTargetPicker : ICaptureTargetPicker
    {
        private readonly GraphicsCaptureItem? item;
        private readonly Exception? exception;

        public FakeCaptureTargetPicker(GraphicsCaptureItem? item)
        {
            this.item = item;
        }

        public FakeCaptureTargetPicker(Exception exception)
        {
            this.exception = exception;
        }

        public Task<GraphicsCaptureItem?> PickSingleItemAsync()
        {
            if (exception is not null)
            {
                return Task.FromException<GraphicsCaptureItem?>(exception);
            }

            return Task.FromResult(item);
        }
    }

    private sealed class ThrowingFakeCaptureTargetPicker : ICaptureTargetPicker
    {
        private readonly Action onCalled;

        public ThrowingFakeCaptureTargetPicker(Action onCalled)
        {
            this.onCalled = onCalled;
        }

        public Task<GraphicsCaptureItem?> PickSingleItemAsync()
        {
            onCalled();
            return Task.FromException<GraphicsCaptureItem?>(
                new InvalidOperationException("Picker should not be called."));
        }
    }
}
