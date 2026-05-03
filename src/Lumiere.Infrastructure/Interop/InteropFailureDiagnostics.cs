namespace Lumiere.Infrastructure.Interop;

public static class InteropFailureDiagnostics
{
    public static string Write(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var diagnosticDetail = exception.ToString();
        var logPath = Path.Combine(Path.GetTempPath(), "lumiere-last-error.txt");

        try
        {
            File.WriteAllText(logPath, diagnosticDetail);
            return $"{diagnosticDetail}{Environment.NewLine}{Environment.NewLine}Log: {logPath}";
        }
        catch
        {
            return diagnosticDetail;
        }
    }
}
