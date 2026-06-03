using Microsoft.Extensions.Logging;

namespace Lumiere.Infrastructure.Diagnostics;

public static class DiagnosticContext
{
    public static DiagnosticRecord CaptureFailure(
        string stage,
        string userFacingState,
        string technicalDetail,
        string? sessionId = null,
        string? correlationId = null,
        Exception? exception = null) => DiagnosticRecord.Create(
        operation: "Capture",
        stage: stage,
        userFacingState: userFacingState,
        technicalDetail: technicalDetail,
        logLevel: LogLevel.Error,
        sessionId: sessionId,
        correlationId: correlationId,
        exception: exception);

    public static DiagnosticRecord CaptureWarning(
        string stage,
        string userFacingState,
        string technicalDetail,
        string? sessionId = null,
        string? correlationId = null,
        Exception? exception = null) => DiagnosticRecord.Create(
        operation: "Capture",
        stage: stage,
        userFacingState: userFacingState,
        technicalDetail: technicalDetail,
        logLevel: LogLevel.Warning,
        sessionId: sessionId,
        correlationId: correlationId,
        exception: exception);

    public static DiagnosticRecord PreviewFailure(
        string stage,
        string userFacingState,
        string technicalDetail,
        string? sessionId = null,
        string? correlationId = null,
        Exception? exception = null) => DiagnosticRecord.Create(
        operation: "Preview",
        stage: stage,
        userFacingState: userFacingState,
        technicalDetail: technicalDetail,
        logLevel: LogLevel.Error,
        sessionId: sessionId,
        correlationId: correlationId,
        exception: exception);

    public static DiagnosticRecord PreviewWarning(
        string stage,
        string userFacingState,
        string technicalDetail,
        string? sessionId = null,
        string? correlationId = null,
        Exception? exception = null) => DiagnosticRecord.Create(
        operation: "Preview",
        stage: stage,
        userFacingState: userFacingState,
        technicalDetail: technicalDetail,
        logLevel: LogLevel.Warning,
        sessionId: sessionId,
        correlationId: correlationId,
        exception: exception);

    public static DiagnosticRecord OutputFailure(
        string stage,
        string userFacingState,
        string technicalDetail,
        string? sessionId = null,
        string? correlationId = null,
        Exception? exception = null) => DiagnosticRecord.Create(
        operation: "Output",
        stage: stage,
        userFacingState: userFacingState,
        technicalDetail: technicalDetail,
        logLevel: LogLevel.Error,
        sessionId: sessionId,
        correlationId: correlationId,
        exception: exception);

    public static DiagnosticRecord OutputWarning(
        string stage,
        string userFacingState,
        string technicalDetail,
        string? sessionId = null,
        string? correlationId = null,
        Exception? exception = null) => DiagnosticRecord.Create(
        operation: "Output",
        stage: stage,
        userFacingState: userFacingState,
        technicalDetail: technicalDetail,
        logLevel: LogLevel.Warning,
        sessionId: sessionId,
        correlationId: correlationId,
        exception: exception);

    public static DiagnosticRecord InteropFailure(
        string stage,
        string userFacingState,
        string technicalDetail,
        string? sessionId = null,
        string? correlationId = null,
        Exception? exception = null) => DiagnosticRecord.Create(
        operation: "Interop",
        stage: stage,
        userFacingState: userFacingState,
        technicalDetail: technicalDetail,
        logLevel: LogLevel.Error,
        sessionId: sessionId,
        correlationId: correlationId,
        exception: exception);

    public static DiagnosticRecord TrayWarning(
        string stage,
        string userFacingState,
        string technicalDetail,
        string? sessionId = null,
        string? correlationId = null,
        Exception? exception = null) => DiagnosticRecord.Create(
        operation: "Tray",
        stage: stage,
        userFacingState: userFacingState,
        technicalDetail: technicalDetail,
        logLevel: LogLevel.Warning,
        sessionId: sessionId,
        correlationId: correlationId,
        exception: exception);

    public static DiagnosticRecord TrayFailure(
        string stage,
        string userFacingState,
        string technicalDetail,
        string? sessionId = null,
        string? correlationId = null,
        Exception? exception = null) => DiagnosticRecord.Create(
        operation: "Tray",
        stage: stage,
        userFacingState: userFacingState,
        technicalDetail: technicalDetail,
        logLevel: LogLevel.Error,
        sessionId: sessionId,
        correlationId: correlationId,
        exception: exception);

    public static DiagnosticRecord HotkeyFailure(
        string stage,
        string userFacingState,
        string technicalDetail,
        string? sessionId = null,
        string? correlationId = null,
        Exception? exception = null) => DiagnosticRecord.Create(
        operation: "Hotkey",
        stage: stage,
        userFacingState: userFacingState,
        technicalDetail: technicalDetail,
        logLevel: LogLevel.Error,
        sessionId: sessionId,
        correlationId: correlationId,
        exception: exception);
}
