namespace Lumiere.Windows.Graphics.Output;

internal interface IOutputTargetAdapter
{
    OutputTarget Target { get; }

    Task<OutputTargetResult> DeliverAsync(
        OutputRequest request,
        OutputEncodedArtifact artifact,
        CancellationToken cancellationToken);
}
