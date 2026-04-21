using System.Globalization;

namespace Lumiere.Infrastructure.Interop;

public sealed class NativeInteropException : InvalidOperationException
{
    public NativeInteropException(
        string operationName,
        string stage,
        int hResult,
        string technicalDetail,
        string userMessage,
        Exception? innerException = null)
        : base(BuildMessage(operationName, stage, hResult, technicalDetail), innerException)
    {
        OperationName = operationName;
        Stage = stage;
        HResultCode = hResult;
        TechnicalDetail = technicalDetail;
        UserMessage = userMessage;
    }

    public string OperationName { get; }

    public string Stage { get; }

    public int HResultCode { get; }

    public string TechnicalDetail { get; }

    public string UserMessage { get; }

    public static string FormatHResult(int hResult) =>
        string.Create(CultureInfo.InvariantCulture, $"0x{hResult:X8}");

    private static string BuildMessage(
        string operationName,
        string stage,
        int hResult,
        string technicalDetail) =>
        $"{operationName} failed during {stage}: HRESULT {FormatHResult(hResult)}. {technicalDetail}";
}
