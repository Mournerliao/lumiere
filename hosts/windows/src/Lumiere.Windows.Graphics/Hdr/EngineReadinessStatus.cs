namespace Lumiere.Windows.Graphics.Hdr;

internal sealed record EngineReadinessStatus
{
    private EngineReadinessStatus(
        EngineReadinessState state,
        EngineReadinessStage stage,
        string userMessage,
        string? technicalDetail = null,
        EngineReadinessReason reason = EngineReadinessReason.None)
    {
        State = state;
        Stage = stage;
        UserMessage = userMessage;
        TechnicalDetail = technicalDetail;
        Reason = reason;
    }

    public EngineReadinessState State { get; }

    public EngineReadinessStage Stage { get; }

    public string UserMessage { get; }

    public string? TechnicalDetail { get; }

    public EngineReadinessReason Reason { get; }

    public bool IsReady => State == EngineReadinessState.Ready;

    public bool RequiresUserAttention =>
        State is EngineReadinessState.Degraded or EngineReadinessState.Unsupported or EngineReadinessState.Failed;

    public static EngineReadinessStatus Initializing(
        string userMessage = "HDR-aware engine is initializing.",
        string? technicalDetail = null) =>
        new(EngineReadinessState.Initializing, EngineReadinessStage.Unknown, userMessage, technicalDetail);

    public static EngineReadinessStatus Initializing(
        EngineReadinessStage stage,
        string userMessage,
        string? technicalDetail = null) =>
        new(EngineReadinessState.Initializing, stage, userMessage, technicalDetail);

    public static EngineReadinessStatus Ready(
        string userMessage = "HDR-aware engine is ready.",
        string? technicalDetail = null) =>
        new(EngineReadinessState.Ready, EngineReadinessStage.Graphics, userMessage, technicalDetail);

    public static EngineReadinessStatus Degraded(
        EngineReadinessStage stage,
        string userMessage,
        string? technicalDetail = null,
        EngineReadinessReason reason = EngineReadinessReason.None) =>
        new(EngineReadinessState.Degraded, stage, userMessage, technicalDetail, reason);

    public static EngineReadinessStatus Unsupported(
        EngineReadinessStage stage,
        string userMessage,
        string? technicalDetail = null) =>
        new(EngineReadinessState.Unsupported, stage, userMessage, technicalDetail);

    public static EngineReadinessStatus Failed(
        EngineReadinessStage stage,
        string userMessage,
        string? technicalDetail = null) =>
        new(EngineReadinessState.Failed, stage, userMessage, technicalDetail);
}

internal enum EngineReadinessReason
{
    None = 0,
    TargetDisplayUnresolved,
}
