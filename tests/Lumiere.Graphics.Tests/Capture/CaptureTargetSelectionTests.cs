using Lumiere.Capture;
using Lumiere.Graphics.Hdr;
using Lumiere.Infrastructure.Interop;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Xunit;

namespace Lumiere.Graphics.Tests.Capture;

public sealed class CaptureTargetSelectionTests
{
    [Fact]
    public void SelectedResultExposesTargetAndReadyForSession()
    {
        var target = CaptureTarget.CreateForTest(
            new SizeInt32 { Width = 1920, Height = 1080 },
            "Test Display");

        var result = CaptureTargetSelectionResult.Selected(
            target,
            PreviewReadinessStatus.Ready("Target selected."));

        Assert.Equal(SelectionOutcome.Selected, result.Outcome);
        Assert.NotNull(result.Target);
        Assert.Equal("Test Display", result.Target.DisplayName);
        Assert.Equal(1920, result.Target.Size.Width);
        Assert.Equal(1080, result.Target.Size.Height);
        Assert.True(result.IsSelected);
        Assert.False(result.IsCanceled);
        Assert.False(result.IsFailed);
        Assert.False(result.IsUnsupported);
    }

    [Fact]
    public void CanceledResultHasNoTargetAndIdleReadiness()
    {
        var readiness = PreviewReadinessStatus.Initializing(
            PreviewReadinessStage.Capture,
            "Choose a display or window to start the minimal HDR preview.",
            "GraphicsCapturePicker was canceled.");

        var result = CaptureTargetSelectionResult.Canceled(readiness);

        Assert.Equal(SelectionOutcome.Canceled, result.Outcome);
        Assert.Null(result.Target);
        Assert.True(result.IsCanceled);
        Assert.False(result.IsSelected);
        Assert.False(result.IsFailed);
        Assert.False(result.IsUnsupported);
    }

    [Fact]
    public void UnsupportedResultHasNoTargetAndUnsupportedReadiness()
    {
        var readiness = PreviewReadinessStatus.Unsupported(
            PreviewReadinessStage.Capture,
            "Unsupported capture",
            "GraphicsCaptureSession.IsSupported returned false.");

        var result = CaptureTargetSelectionResult.Unsupported(readiness);

        Assert.Equal(SelectionOutcome.Unsupported, result.Outcome);
        Assert.Null(result.Target);
        Assert.True(result.IsUnsupported);
        Assert.False(result.IsSelected);
        Assert.False(result.IsCanceled);
        Assert.False(result.IsFailed);
    }

    [Fact]
    public void FailedResultCarriesDiagnosticAndNoTarget()
    {
        var readiness = PreviewReadinessStatus.Failed(
            PreviewReadinessStage.Interop,
            "Preview failed",
            "Picker initialization threw ArgumentNullException.");

        var result = CaptureTargetSelectionResult.Failed(readiness);

        Assert.Equal(SelectionOutcome.Failed, result.Outcome);
        Assert.Null(result.Target);
        Assert.True(result.IsFailed);
        Assert.False(result.IsSelected);
        Assert.False(result.IsCanceled);
        Assert.False(result.IsUnsupported);
        Assert.Equal(PreviewReadinessState.Failed, result.Readiness.State);
    }

    [Fact]
    public void CanceledDoesNotAllocateCaptureResources()
    {
        var readiness = PreviewReadinessStatus.Initializing(
            PreviewReadinessStage.Capture,
            "Choose a display or window.");

        var result = CaptureTargetSelectionResult.Canceled(readiness);

        Assert.Null(result.Target);
        Assert.False(result.IsSelected);
    }

    [Fact]
    public async Task FakePickerCanceledReturnsCanceledOutcome()
    {
        var fakePicker = new FakeCaptureTargetPicker((GraphicsCaptureItem?)null);
        var service = new CaptureTargetSelectionService(fakePicker, () => true);

        var result = await service.SelectTargetAsync();

        Assert.Equal(SelectionOutcome.Canceled, result.Outcome);
        Assert.Null(result.Target);
        Assert.False(result.IsSelected);
        Assert.True(result.IsCanceled);
    }

    [Fact]
    public async Task FakePickerExceptionReturnsFailedOutcome()
    {
        var fakePicker = new FakeCaptureTargetPicker(
            new InvalidOperationException("Picker initialization failed."));
        var service = new CaptureTargetSelectionService(fakePicker, () => true);

        var result = await service.SelectTargetAsync();

        Assert.Equal(SelectionOutcome.Failed, result.Outcome);
        Assert.Null(result.Target);
        Assert.Equal(PreviewReadinessState.Failed, result.Readiness.State);
        Assert.Contains("Picker initialization failed.", result.Readiness.TechnicalDetail);
    }

    [Fact]
    public async Task FakePickerNotSupportedExceptionReturnsUnsupportedOutcome()
    {
        var fakePicker = new FakeCaptureTargetPicker(
            new NotSupportedException("Graphics capture is not supported on this system."));
        var service = new CaptureTargetSelectionService(fakePicker, () => true);

        var result = await service.SelectTargetAsync();

        Assert.Equal(SelectionOutcome.Unsupported, result.Outcome);
        Assert.Null(result.Target);
        Assert.Equal(PreviewReadinessState.Unsupported, result.Readiness.State);
    }

    [Fact]
    public async Task UnsupportedCaptureReturnsUnsupportedBeforePickerRuns()
    {
        var fakePicker = new FakeCaptureTargetPicker(
            new InvalidOperationException("Picker should not run."));
        var service = new CaptureTargetSelectionService(fakePicker, () => false);

        var result = await service.SelectTargetAsync();

        Assert.Equal(SelectionOutcome.Unsupported, result.Outcome);
        Assert.Null(result.Target);
        Assert.Equal(PreviewReadinessState.Unsupported, result.Readiness.State);
        Assert.Contains("IsSupported returned false", result.Readiness.TechnicalDetail);
    }

    [Fact]
    public async Task SupportCheckNotSupportedExceptionReturnsUnsupported()
    {
        var fakePicker = new FakeCaptureTargetPicker(
            new InvalidOperationException("Picker should not run."));
        var service = new CaptureTargetSelectionService(
            fakePicker,
            () => throw new NotSupportedException("Support check failed."));

        var result = await service.SelectTargetAsync();

        Assert.Equal(SelectionOutcome.Unsupported, result.Outcome);
        Assert.Null(result.Target);
        Assert.Equal(PreviewReadinessState.Unsupported, result.Readiness.State);
        Assert.Contains("Support check failed.", result.Readiness.TechnicalDetail);
    }

    [Fact]
    public void SelectedResultRejectsNullTarget()
    {
        var readiness = PreviewReadinessStatus.Ready("Ready.");

        Assert.Throws<ArgumentNullException>(
            () => CaptureTargetSelectionResult.Selected(null!, readiness));
    }

    [Fact]
    public void SelectedResultRejectsNullReadiness()
    {
        var target = CaptureTarget.CreateForTest(
            new SizeInt32 { Width = 800, Height = 600 },
            "Test");

        Assert.Throws<ArgumentNullException>(
            () => CaptureTargetSelectionResult.Selected(target, null!));
    }

    [Fact]
    public void FactoryMethodsRejectNullReadiness()
    {
        Assert.Throws<ArgumentNullException>(
            () => CaptureTargetSelectionResult.Canceled(null!));
        Assert.Throws<ArgumentNullException>(
            () => CaptureTargetSelectionResult.Unsupported(null!));
        Assert.Throws<ArgumentNullException>(
            () => CaptureTargetSelectionResult.Failed(null!));
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
}
