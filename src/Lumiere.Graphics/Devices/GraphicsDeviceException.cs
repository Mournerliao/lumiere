using Lumiere.Graphics.Hdr;

namespace Lumiere.Graphics.Devices;

public sealed class GraphicsDeviceException : InvalidOperationException
{
    public GraphicsDeviceException(
        string operationName,
        PreviewReadinessStage stage,
        string userMessage,
        string technicalDetail,
        Exception? innerException = null)
        : base($"{operationName} failed during {stage}: {technicalDetail}", innerException)
    {
        OperationName = operationName;
        Stage = stage;
        UserMessage = userMessage;
        TechnicalDetail = technicalDetail;
        ReadinessStatus = PreviewReadinessStatus.Failed(stage, userMessage, technicalDetail);
    }

    public string OperationName { get; }

    public PreviewReadinessStage Stage { get; }

    public string UserMessage { get; }

    public string TechnicalDetail { get; }

    public PreviewReadinessStatus ReadinessStatus { get; }
}
