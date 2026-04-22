using Lumiere.Infrastructure.Interop;

namespace Lumiere.Graphics.Presentation;

public sealed class SwapChainPresentationException : InvalidOperationException
{
    public SwapChainPresentationException(
        string operationName,
        int hResult,
        string technicalDetail,
        Exception? innerException = null)
        : base(BuildMessage(operationName, hResult, technicalDetail), innerException)
    {
        OperationName = operationName;
        HResultCode = hResult;
        TechnicalDetail = technicalDetail;
    }

    public string OperationName { get; }

    public int HResultCode { get; }

    public string TechnicalDetail { get; }

    private static string BuildMessage(
        string operationName,
        int hResult,
        string technicalDetail) =>
        $"{operationName} failed during Presentation: HRESULT {NativeInteropException.FormatHResult(hResult)}. {technicalDetail}";
}
