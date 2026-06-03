using Lumiere.Infrastructure.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Lumiere.Infrastructure.Interop;

public static class InteropFailureDiagnostics
{
    private static readonly ILogger DefaultLogger = LumiereLoggerFactory.CreateLogger(LogCategories.Infrastructure);

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

        return exception.ToString();
    }
}
