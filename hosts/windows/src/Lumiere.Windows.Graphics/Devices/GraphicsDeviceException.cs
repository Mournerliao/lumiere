using Lumiere.Windows.Graphics.Hdr;

namespace Lumiere.Windows.Graphics.Devices;

public sealed class GraphicsDeviceException : InvalidOperationException
{
    public GraphicsDeviceException(
        string operationName,
        EngineReadinessStage stage,
        string userMessage,
        string technicalDetail,
        Exception? innerException = null)
        : base($"{operationName} failed during {stage}: {technicalDetail}", innerException)
    {
        OperationName = operationName;
        Stage = stage;
        UserMessage = userMessage;
        TechnicalDetail = technicalDetail;
        ReadinessStatus = EngineReadinessStatus.Failed(stage, userMessage, technicalDetail);
    }

    public string OperationName { get; }

    public EngineReadinessStage Stage { get; }

    public string UserMessage { get; }

    public string TechnicalDetail { get; }

    public EngineReadinessStatus ReadinessStatus { get; }
}
