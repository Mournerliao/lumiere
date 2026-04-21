namespace Lumiere.Graphics.Hdr;

public sealed record PreviewReadinessStatus
{
    private PreviewReadinessStatus(
        PreviewReadinessState state,
        PreviewReadinessStage stage,
        string userMessage,
        string? technicalDetail = null)
    {
        State = state;
        Stage = stage;
        UserMessage = userMessage;
        TechnicalDetail = technicalDetail;
    }

    public PreviewReadinessState State { get; }

    public PreviewReadinessStage Stage { get; }

    public string UserMessage { get; }

    public string? TechnicalDetail { get; }

    public bool IsReady => State == PreviewReadinessState.Ready;

    public bool RequiresUserAttention =>
        State is PreviewReadinessState.Degraded or PreviewReadinessState.Unsupported or PreviewReadinessState.Failed;

    public static PreviewReadinessStatus Initializing(
        string userMessage = "HDR preview readiness is being validated.",
        string? technicalDetail = null) =>
        new(PreviewReadinessState.Initializing, PreviewReadinessStage.Unknown, userMessage, technicalDetail);

    public static PreviewReadinessStatus Ready(
        string userMessage = "HDR preview path is validated.",
        string? technicalDetail = null) =>
        new(PreviewReadinessState.Ready, PreviewReadinessStage.Presentation, userMessage, technicalDetail);

    public static PreviewReadinessStatus Degraded(
        PreviewReadinessStage stage,
        string userMessage,
        string? technicalDetail = null) =>
        new(PreviewReadinessState.Degraded, stage, userMessage, technicalDetail);

    public static PreviewReadinessStatus Unsupported(
        PreviewReadinessStage stage,
        string userMessage,
        string? technicalDetail = null) =>
        new(PreviewReadinessState.Unsupported, stage, userMessage, technicalDetail);

    public static PreviewReadinessStatus Failed(
        PreviewReadinessStage stage,
        string userMessage,
        string? technicalDetail = null) =>
        new(PreviewReadinessState.Failed, stage, userMessage, technicalDetail);
}
