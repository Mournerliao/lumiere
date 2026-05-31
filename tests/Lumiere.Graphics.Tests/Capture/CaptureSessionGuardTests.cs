using Lumiere.Capture;
using Lumiere.Graphics.Devices;
using Lumiere.Graphics.Hdr;
using Windows.Graphics;
using Xunit;

namespace Lumiere.Graphics.Tests.Capture;

public sealed class CaptureSessionGuardTests
{
    [Fact]
    public void CanAcceptCommand_AcceptsWhenIdle()
    {
        var state = CaptureSessionState.Idle();
        var command = CaptureCommand.Fullscreen();

        var canAccept = CaptureService.CanAcceptCommand(state, command, out var rejectionReason);

        Assert.True(canAccept);
        Assert.Null(rejectionReason);
    }

    [Fact]
    public void CanAcceptCommand_RejectsWhenDisposed()
    {
        var state = CaptureSessionState.Disposed();
        var command = CaptureCommand.Region();

        var canAccept = CaptureService.CanAcceptCommand(state, command, out var rejectionReason);

        Assert.False(canAccept);
        Assert.NotNull(rejectionReason);
        Assert.Contains("Disposed", rejectionReason.TechnicalDetail);
    }

    [Fact]
    public void CanAcceptCommand_AcceptsWhenFailed()
    {
        var state = CaptureSessionState.Failed(
            null,
            PreviewReadinessStatus.Failed(
                PreviewReadinessStage.Capture,
                "Preview failed",
                "Test failure."));
        var command = CaptureCommand.Fullscreen();

        var canAccept = CaptureService.CanAcceptCommand(state, command, out var rejectionReason);

        Assert.True(canAccept);
        Assert.Null(rejectionReason);
    }

    [Fact]
    public void CanAcceptCommand_RejectsWhenSelectingTarget()
    {
        var state = CaptureSessionState.SelectingTarget();
        var command = CaptureCommand.Fullscreen();

        var canAccept = CaptureService.CanAcceptCommand(state, command, out var rejectionReason);

        Assert.False(canAccept);
        Assert.NotNull(rejectionReason);
        Assert.Contains("SelectingTarget", rejectionReason.TechnicalDetail);
    }

    [Fact]
    public void CanAcceptCommand_RejectsWhenInitializing()
    {
        var target = CreateTarget();
        var state = CaptureSessionState.Initializing(
            target,
            PreviewReadinessStatus.Initializing(
                PreviewReadinessStage.Capture,
                "Initializing preview",
                "Test initialization."));
        var command = CaptureCommand.Fullscreen();

        var canAccept = CaptureService.CanAcceptCommand(state, command, out var rejectionReason);

        Assert.False(canAccept);
        Assert.NotNull(rejectionReason);
        Assert.Contains("Initializing", rejectionReason.TechnicalDetail);
    }

    [Fact]
    public void CanAcceptCommand_RejectsWhenCapturing()
    {
        var target = CreateTarget();
        var state = CaptureSessionState.Capturing(
            target,
            PreviewReadinessStatus.Ready(
                "HDR-ready",
                "Test capture."));
        var command = CaptureCommand.Region();

        var canAccept = CaptureService.CanAcceptCommand(state, command, out var rejectionReason);

        Assert.False(canAccept);
        Assert.NotNull(rejectionReason);
        Assert.Contains("Capturing", rejectionReason.TechnicalDetail);
    }

    [Fact]
    public void CanAcceptCommand_RejectsWhenDegraded()
    {
        var target = CreateTarget();
        var state = CaptureSessionState.Degraded(
            target,
            PreviewReadinessStatus.Degraded(
                PreviewReadinessStage.Presentation,
                "Degraded preview",
                "Test degradation."));
        var command = CaptureCommand.Fullscreen();

        var canAccept = CaptureService.CanAcceptCommand(state, command, out var rejectionReason);

        Assert.False(canAccept);
        Assert.NotNull(rejectionReason);
        Assert.Contains("Degraded", rejectionReason.TechnicalDetail);
    }

    [Fact]
    public void CanAcceptCommand_AcceptsWhenUnsupported()
    {
        var state = CaptureSessionState.Unsupported(
            PreviewReadinessStatus.Unsupported(
                PreviewReadinessStage.Capture,
                "Unsupported capture",
                "Test unsupported."));
        var command = CaptureCommand.Region();

        var canAccept = CaptureService.CanAcceptCommand(state, command, out var rejectionReason);

        Assert.True(canAccept);
        Assert.Null(rejectionReason);
    }

    [Fact]
    public void CanAcceptCommand_AcceptsNullTargetWhenIdle()
    {
        var state = CaptureSessionState.Idle();
        var command = CaptureCommand.Fullscreen();

        var canAccept = CaptureService.CanAcceptCommand(state, command, out var rejectionReason);

        Assert.True(canAccept);
        Assert.Null(rejectionReason);
        Assert.Null(command.Target);
    }

    [Fact]
    public void CaptureCommand_FullscreenCreatesCorrectMode()
    {
        var command = CaptureCommand.Fullscreen();

        Assert.Equal(CaptureCommandMode.Fullscreen, command.Mode);
        Assert.Null(command.Target);
    }

    [Fact]
    public void CaptureCommand_RegionCreatesCorrectMode()
    {
        var command = CaptureCommand.Region();

        Assert.Equal(CaptureCommandMode.Region, command.Mode);
        Assert.Null(command.Target);
    }

    [Fact]
    public void CaptureCommand_FullscreenWithTargetPreservesTarget()
    {
        var target = CreateTarget();
        var command = CaptureCommand.Fullscreen(target);

        Assert.Equal(CaptureCommandMode.Fullscreen, command.Mode);
        Assert.Same(target, command.Target);
    }

    [Fact]
    public void CaptureCommandResult_AcceptedHasCorrectOutcome()
    {
        var command = CaptureCommand.Fullscreen();
        var result = CaptureCommandResult.Accepted(command);

        Assert.True(result.IsAccepted);
        Assert.False(result.IsRejectedSessionActive);
        Assert.False(result.IsRejectedNonRecoverable);
        Assert.False(result.IsFailed);
        Assert.Same(command, result.Command);
    }

    [Fact]
    public void CaptureCommandResult_RejectedSessionActiveHasCorrectOutcome()
    {
        var command = CaptureCommand.Region();
        var state = CaptureSessionState.Capturing(
            CreateTarget(),
            PreviewReadinessStatus.Ready("HDR-ready", "Test"));
        var result = CaptureCommandResult.RejectedSessionActive(command, state);

        Assert.False(result.IsAccepted);
        Assert.True(result.IsRejectedSessionActive);
        Assert.False(result.IsRejectedNonRecoverable);
        Assert.False(result.IsFailed);
        Assert.Same(command, result.Command);
        Assert.Same(state, result.SessionState);
    }

    [Fact]
    public void CaptureCommandResult_RejectedNonRecoverableHasCorrectOutcome()
    {
        var command = CaptureCommand.Fullscreen();
        var state = CaptureSessionState.Unsupported(
            PreviewReadinessStatus.Unsupported(
                PreviewReadinessStage.Capture,
                "Unsupported capture",
                "Test unsupported."));
        var result = CaptureCommandResult.RejectedNonRecoverable(command, state);

        Assert.False(result.IsAccepted);
        Assert.False(result.IsRejectedSessionActive);
        Assert.True(result.IsRejectedNonRecoverable);
        Assert.False(result.IsFailed);
        Assert.Same(command, result.Command);
        Assert.Same(state, result.SessionState);
    }

    [Fact]
    public void CaptureCommandResult_FailedHasCorrectOutcome()
    {
        var command = CaptureCommand.Region();
        var readiness = PreviewReadinessStatus.Failed(
            PreviewReadinessStage.Capture,
            "Capture failed",
            "Test failure.");
        var result = CaptureCommandResult.Failed(command, readiness);

        Assert.False(result.IsAccepted);
        Assert.False(result.IsRejectedSessionActive);
        Assert.False(result.IsRejectedNonRecoverable);
        Assert.True(result.IsFailed);
        Assert.Same(command, result.Command);
        Assert.Same(readiness, result.Readiness);
    }

    [Fact]
    public void CaptureCommandMode_HasCorrectValues()
    {
        Assert.Equal(0, (int)CaptureCommandMode.Fullscreen);
        Assert.Equal(1, (int)CaptureCommandMode.Region);
    }

    [Fact]
    public void CaptureCommandOutcome_HasCorrectValues()
    {
        Assert.Equal(0, (int)CaptureCommandOutcome.Accepted);
        Assert.Equal(1, (int)CaptureCommandOutcome.RejectedSessionActive);
        Assert.Equal(2, (int)CaptureCommandOutcome.RejectedNonRecoverable);
        Assert.Equal(3, (int)CaptureCommandOutcome.Failed);
    }

    [Fact]
    public void ValidateCommand_And_TryReserveCommand_ProduceConsistentRejection_ForSelectingTarget()
    {
        var deviceProvider = new GraphicsDeviceProvider();
        using var deviceResources = deviceProvider.CreateDevice();
        var service = new CaptureService(deviceResources);
        var state = CaptureSessionState.SelectingTarget();
        service.UpdateSessionState(state);
        var command = CaptureCommand.Fullscreen();

        var validateResult = service.ValidateCommand(command);
        service.UpdateSessionState(state); // Reset for TryReserveCommand
        var reserveResult = service.TryReserveCommand(command);

        Assert.True(validateResult.IsRejectedSessionActive);
        Assert.True(reserveResult.IsRejectedSessionActive);
    }

    [Fact]
    public void ValidateCommand_And_TryReserveCommand_ProduceConsistentRejection_ForCapturing()
    {
        var deviceProvider = new GraphicsDeviceProvider();
        using var deviceResources = deviceProvider.CreateDevice();
        var service = new CaptureService(deviceResources);
        var target = CreateTarget();
        var state = CaptureSessionState.Capturing(
            target,
            PreviewReadinessStatus.Ready("HDR-ready", "Test"));
        service.UpdateSessionState(state);
        var command = CaptureCommand.Region();

        var validateResult = service.ValidateCommand(command);
        service.UpdateSessionState(state); // Reset for TryReserveCommand
        var reserveResult = service.TryReserveCommand(command);

        Assert.True(validateResult.IsRejectedSessionActive);
        Assert.True(reserveResult.IsRejectedSessionActive);
    }

    [Fact]
    public void ValidateCommand_And_TryReserveCommand_ProduceConsistentRejection_ForDisposed()
    {
        var deviceProvider = new GraphicsDeviceProvider();
        using var deviceResources = deviceProvider.CreateDevice();
        var service = new CaptureService(deviceResources);
        var state = CaptureSessionState.Disposed();
        service.UpdateSessionState(state);
        var command = CaptureCommand.Fullscreen();

        var validateResult = service.ValidateCommand(command);
        var reserveResult = service.TryReserveCommand(command);

        Assert.True(validateResult.IsRejectedNonRecoverable);
        Assert.True(reserveResult.IsRejectedNonRecoverable);
    }

    private static CaptureTarget CreateTarget() =>
        CaptureTarget.CreateForTest(
            new SizeInt32 { Width = 1920, Height = 1080 },
            "Test Display");
}
