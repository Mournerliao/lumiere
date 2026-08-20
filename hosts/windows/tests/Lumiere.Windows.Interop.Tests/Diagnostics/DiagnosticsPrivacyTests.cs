using Lumiere.Windows.Interop;
using Lumiere.Windows.Interop.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Lumiere.Windows.Interop.Tests;

public sealed class DiagnosticsPrivacyTests
{
    private static readonly string[] SensitivePatterns =
    [
        "pixel",
        "frame dump",
        "screenshot",
        "screen content",
        "raw frame",
        "texture data",
        "GPU resource dump",
        "bitmap data",
        "image data",
        "RGB",
        "RGBA",
        "BGRA",
    ];

    [Fact]
    public void DiagnosticRecord_TechnicalDetail_DoesNotContainSensitivePatterns()
    {
        var record = DiagnosticRecord.Create(
            operation: "Capture",
            stage: "FrameCapture",
            userFacingState: "Capture failed",
            technicalDetail: "D3D11 resource creation failed: E_ACCESSDENIED",
            logLevel: LogLevel.Error);

        foreach (var pattern in SensitivePatterns)
        {
            Assert.DoesNotContain(pattern, record.TechnicalDetail, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void DiagnosticContext_CaptureFailure_DoesNotAcceptPixelData()
    {
        var record = DiagnosticContext.CaptureFailure(
            stage: "FrameCapture",
            userFacingState: "Capture failed",
            technicalDetail: "D3D11 resource creation failed");

        foreach (var pattern in SensitivePatterns)
        {
            Assert.DoesNotContain(pattern, record.TechnicalDetail, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void DiagnosticContext_EngineFailure_DoesNotAcceptPixelData()
    {
        var record = DiagnosticContext.EngineFailure(
            stage: "DeviceCreation",
            userFacingState: "Capture failed",
            technicalDetail: "Swap chain creation failed");

        foreach (var pattern in SensitivePatterns)
        {
            Assert.DoesNotContain(pattern, record.TechnicalDetail, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void DiagnosticContext_OutputFailure_DoesNotAcceptPixelData()
    {
        var record = DiagnosticContext.OutputFailure(
            stage: "FolderWrite",
            userFacingState: "Failed to save file",
            technicalDetail: "IOException: access denied");

        foreach (var pattern in SensitivePatterns)
        {
            Assert.DoesNotContain(pattern, record.TechnicalDetail, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void DiagnosticContext_InteropFailure_DoesNotAcceptPixelData()
    {
        var record = DiagnosticContext.InteropFailure(
            stage: "MonitorResolution",
            userFacingState: "Monitor detection failed",
            technicalDetail: "EnumDisplayDevices returned FALSE");

        foreach (var pattern in SensitivePatterns)
        {
            Assert.DoesNotContain(pattern, record.TechnicalDetail, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void NativeInteropException_DoesNotCaptureScreenContent()
    {
        var exception = new NativeInteropException(
            "CreateD3D11Device",
            "DeviceCreation",
            unchecked((int)0x80070005),
            "Access denied when creating the D3D11 device",
            "Failed to initialize capture");

        foreach (var pattern in SensitivePatterns)
        {
            Assert.DoesNotContain(pattern, exception.TechnicalDetail, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(pattern, exception.UserMessage, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void DiagnosticRecord_UserFacingState_DoesNotClaimHdrPreservation()
    {
        var record = DiagnosticContext.OutputFailure(
            stage: "FolderWrite",
            userFacingState: "Output failed",
            technicalDetail: "IOException: access denied");

        Assert.DoesNotContain("HDR-preserving", record.UserFacingState, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HDR output", record.UserFacingState, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DiagnosticRecord_UserFacingState_SeparatesFromTechnicalDetail()
    {
        var record = DiagnosticRecord.Create(
            operation: "Output",
            stage: "ClipboardWrite",
            userFacingState: "Failed to copy to clipboard",
            technicalDetail: "COM error 0x800401D0",
            logLevel: LogLevel.Error);

        Assert.NotEqual(record.UserFacingState, record.TechnicalDetail);
    }

    [Fact]
    public void DiagnosticRecord_WithSessionId_DoesNotExposeSensitiveData()
    {
        var record = DiagnosticRecord.Create(
            operation: "Capture",
            stage: "FramePool",
            userFacingState: "Capture failed",
            technicalDetail: "Frame pool creation failed",
            logLevel: LogLevel.Error,
            sessionId: "session-123",
            correlationId: "corr-456");

        Assert.DoesNotContain("pixel", record.SessionId, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("frame", record.SessionId, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pixel", record.CorrelationId, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("frame", record.CorrelationId, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InteropFailureDiagnostics_Write_DoesNotCaptureScreenContent()
    {
        var exception = new NativeInteropException(
            "CreateD3D11Device",
            "DXGIPresentation",
            unchecked((int)0x80070005),
            "Access denied when creating swap chain",
            "Failed to initialize capture");

        var result = InteropFailureDiagnostics.LogAndFormat(exception);

        foreach (var pattern in SensitivePatterns)
        {
            Assert.DoesNotContain(pattern, result, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void InteropFailureDiagnostics_LogAndFormat_TechnicalDetailIsConcise()
    {
        Exception realException;
        try
        {
            throw new InvalidOperationException("test error");
        }
        catch (Exception ex)
        {
            realException = ex;
        }

        var record = DiagnosticContext.InteropFailure(
            stage: "Test",
            userFacingState: "Test failed",
            technicalDetail: $"{realException.GetType().Name}: {realException.Message}",
            exception: realException);

        Assert.DoesNotContain("at ", record.TechnicalDetail);
        Assert.DoesNotContain(".cs:line", record.TechnicalDetail);
        Assert.Contains("InvalidOperationException", record.TechnicalDetail);
        Assert.Contains("test error", record.TechnicalDetail);
    }
}
