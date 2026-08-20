using Lumiere.Windows.Interop.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Lumiere.Windows.Interop;

public static class InteropFailureDiagnostics
{
    private static readonly ILogger DefaultLogger = LumiereLoggerFactory.CreateLogger(LogCategories.Interop);

    public static string LogAndFormat(Exception exception, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var effectiveLogger = logger ?? DefaultLogger;
        var technicalDetail = $"{exception.GetType().Name}: {exception.Message}";
        var diagnostic = DiagnosticContext.InteropFailure(
            stage: "InteropException",
            userFacingState: "Operation failed",
            technicalDetail: technicalDetail,
            exception: exception);
        diagnostic.LogTo(effectiveLogger);

        var full = exception.ToString();
        return full.Length > 2048 ? full[..2048] + "...[truncated]" : full;
    }
}
