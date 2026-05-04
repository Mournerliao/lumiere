namespace Lumiere.Capture;

public sealed record CapturePreviewRecreationRequest
{
    private CapturePreviewRecreationRequest(
        CaptureTarget target,
        CaptureFrameSizeChange sizeChange,
        long generation)
    {
        Target = target;
        SizeChange = sizeChange;
        Generation = generation;
    }

    public CaptureTarget Target { get; }

    public CaptureFrameSizeChange SizeChange { get; }

    public long Generation { get; }

    public static CapturePreviewRecreationRequest Create(
        CaptureTarget target,
        CaptureFrameSizeChange sizeChange,
        long generation)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(sizeChange);

        if (!sizeChange.RequiresRecreation)
        {
            throw new ArgumentException(
                "Preview recreation requests require a frame size mismatch.",
                nameof(sizeChange));
        }

        return new CapturePreviewRecreationRequest(target, sizeChange, generation);
    }

    public bool MatchesGeneration(long currentGeneration) =>
        Generation == currentGeneration;
}
