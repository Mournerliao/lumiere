using Lumiere.Infrastructure.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Lumiere.Graphics.Tests.Diagnostics;

public sealed class DiagnosticRecordTests
{
    [Fact]
    public void Create_WithRequiredFields_SetsAllProperties()
    {
        var record = DiagnosticRecord.Create(
            operation: "Capture",
            stage: "FramePool",
            userFacingState: "Capture failed",
            technicalDetail: "Access denied creating frame pool",
            logLevel: LogLevel.Error);

        Assert.Equal("Capture", record.Operation);
        Assert.Equal("FramePool", record.Stage);
        Assert.Equal("Capture failed", record.UserFacingState);
        Assert.Equal("Access denied creating frame pool", record.TechnicalDetail);
        Assert.Equal(LogLevel.Error, record.LogLevel);
        Assert.Null(record.SessionId);
        Assert.Null(record.CorrelationId);
        Assert.True(record.Timestamp > DateTimeOffset.MinValue);
    }

    [Fact]
    public void Create_WithSessionAndCorrelationId_SetsOptionalFields()
    {
        var record = DiagnosticRecord.Create(
            operation: "Output",
            stage: "FolderWrite",
            userFacingState: "Failed to save file",
            technicalDetail: "IOException: disk full",
            logLevel: LogLevel.Error,
            sessionId: "session-123",
            correlationId: "corr-456");

        Assert.Equal("session-123", record.SessionId);
        Assert.Equal("corr-456", record.CorrelationId);
    }

    [Fact]
    public void Create_SeparatesUserFacingAndTechnicalDetail()
    {
        var record = DiagnosticRecord.Create(
            operation: "Output",
            stage: "ClipboardWrite",
            userFacingState: "Failed to copy to clipboard",
            technicalDetail: "COM error 0x800401D0",
            logLevel: LogLevel.Error);

        Assert.NotEqual(record.UserFacingState, record.TechnicalDetail);
        Assert.DoesNotContain("0x800401D0", record.UserFacingState);
        Assert.DoesNotContain("Failed to copy to clipboard", record.TechnicalDetail);
    }

    [Fact]
    public void Create_WithWarningLevel_SetsWarningLogLevel()
    {
        var record = DiagnosticRecord.Create(
            operation: "Capture",
            stage: "Validation",
            userFacingState: "Capture started with degraded settings",
            technicalDetail: "FP16 not supported on this display",
            logLevel: LogLevel.Warning);

        Assert.Equal(LogLevel.Warning, record.LogLevel);
    }

    [Fact]
    public void CaptureFailure_UsesCaptureOperationAndErrorLevel()
    {
        var record = DiagnosticContext.CaptureFailure(
            stage: "Startup",
            userFacingState: "Failed to start capture",
            technicalDetail: "GraphicsCaptureSession creation failed");

        Assert.Equal("Capture", record.Operation);
        Assert.Equal("Startup", record.Stage);
        Assert.Equal(LogLevel.Error, record.LogLevel);
    }

    [Fact]
    public void CaptureWarning_UsesCaptureOperationAndWarningLevel()
    {
        var record = DiagnosticContext.CaptureWarning(
            stage: "FramePool",
            userFacingState: "Capture started with degraded settings",
            technicalDetail: "FP16 frame pool not available");

        Assert.Equal("Capture", record.Operation);
        Assert.Equal(LogLevel.Warning, record.LogLevel);
    }

    [Fact]
    public void PreviewFailure_UsesPreviewOperationAndErrorLevel()
    {
        var record = DiagnosticContext.PreviewFailure(
            stage: "SwapChain",
            userFacingState: "Preview initialization failed",
            technicalDetail: "DXGI swap chain creation failed: E_ACCESSDENIED");

        Assert.Equal("Preview", record.Operation);
        Assert.Equal(LogLevel.Error, record.LogLevel);
    }

    [Fact]
    public void OutputFailure_UsesOutputOperationAndErrorLevel()
    {
        var record = DiagnosticContext.OutputFailure(
            stage: "FolderWrite",
            userFacingState: "Failed to save file",
            technicalDetail: "PathTooLongException");

        Assert.Equal("Output", record.Operation);
        Assert.Equal(LogLevel.Error, record.LogLevel);
    }

    [Fact]
    public void OutputWarning_UsesOutputOperationAndWarningLevel()
    {
        var record = DiagnosticContext.OutputWarning(
            stage: "ClipboardWrite",
            userFacingState: "Clipboard copy partially failed",
            technicalDetail: "Some formats not supported");

        Assert.Equal("Output", record.Operation);
        Assert.Equal(LogLevel.Warning, record.LogLevel);
    }

    [Fact]
    public void InteropFailure_UsesInteropOperationAndErrorLevel()
    {
        var record = DiagnosticContext.InteropFailure(
            stage: "MonitorResolution",
            userFacingState: "Monitor detection failed",
            technicalDetail: "EnumDisplayDevices returned FALSE");

        Assert.Equal("Interop", record.Operation);
        Assert.Equal(LogLevel.Error, record.LogLevel);
    }

    [Fact]
    public void TrayFailure_UsesTrayOperationAndErrorLevel()
    {
        var record = DiagnosticContext.TrayFailure(
            stage: "Initialization",
            userFacingState: "Tray icon could not be created",
            technicalDetail: "NOTIFYICONDATA shell_NotifyIcon failed");

        Assert.Equal("Tray", record.Operation);
        Assert.Equal(LogLevel.Error, record.LogLevel);
    }

    [Fact]
    public void HotkeyFailure_UsesHotkeyOperationAndErrorLevel()
    {
        var record = DiagnosticContext.HotkeyFailure(
            stage: "Registration",
            userFacingState: "Global hotkey could not be registered",
            technicalDetail: "RegisterHotKey returned FALSE: hotkey already in use");

        Assert.Equal("Hotkey", record.Operation);
        Assert.Equal(LogLevel.Error, record.LogLevel);
    }

    [Fact]
    public void FactoryMethods_PreserveSessionAndCorrelationIds()
    {
        var record = DiagnosticContext.CaptureFailure(
            stage: "Startup",
            userFacingState: "Failed to start capture",
            technicalDetail: "D3D11 device creation failed",
            sessionId: "s-1",
            correlationId: "c-1");

        Assert.Equal("s-1", record.SessionId);
        Assert.Equal("c-1", record.CorrelationId);
    }

    [Fact]
    public void DiagnosticRecord_DoesNotAcceptPixelDataParameters()
    {
        var record = DiagnosticRecord.Create(
            operation: "Capture",
            stage: "FrameCapture",
            userFacingState: "Capture failed",
            technicalDetail: "D3D11 resource creation failed",
            logLevel: LogLevel.Error);

        Assert.DoesNotContain("pixel", record.TechnicalDetail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("frame dump", record.TechnicalDetail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("screenshot", record.TechnicalDetail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DiagnosticContext_FactoryMethods_DoNotAcceptPixelDataParameters()
    {
        var record = DiagnosticContext.CaptureFailure(
            stage: "FrameCapture",
            userFacingState: "Capture failed",
            technicalDetail: "D3D11 resource creation failed");

        Assert.DoesNotContain("pixel", record.TechnicalDetail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("frame dump", record.TechnicalDetail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UserFacingState_DoesNotClaimHdrPreservationForUnvalidatedPaths()
    {
        var record = DiagnosticContext.OutputFailure(
            stage: "FolderWrite",
            userFacingState: "Output failed",
            technicalDetail: "IOException: access denied");

        Assert.DoesNotContain("HDR-preserving", record.UserFacingState, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR output failed", record.UserFacingState, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DiagnosticRecord_IsImmutable()
    {
        var record = DiagnosticRecord.Create(
            operation: "Capture",
            stage: "Test",
            userFacingState: "Test state",
            technicalDetail: "Test detail",
            logLevel: LogLevel.Error);

        var copy = record with { Operation = "Modified" };

        Assert.Equal("Capture", record.Operation);
        Assert.Equal("Modified", copy.Operation);
    }
}
