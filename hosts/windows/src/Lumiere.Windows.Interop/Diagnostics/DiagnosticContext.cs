using Microsoft.Extensions.Logging;

namespace Lumiere.Windows.Interop.Diagnostics;

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

    public static DiagnosticRecord EngineFailure(
        string stage,
        string userFacingState,
        string technicalDetail,
        string? sessionId = null,
        string? correlationId = null,
        Exception? exception = null) => DiagnosticRecord.Create(
        operation: "Graphics",
        stage: stage,
        userFacingState: userFacingState,
        technicalDetail: technicalDetail,
        logLevel: LogLevel.Error,
        sessionId: sessionId,
        correlationId: correlationId,
        exception: exception);

    public static DiagnosticRecord EngineWarning(
        string stage,
        string userFacingState,
        string technicalDetail,
        string? sessionId = null,
        string? correlationId = null,
        Exception? exception = null) => DiagnosticRecord.Create(
        operation: "Graphics",
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

}
