namespace Lumiere.Windows.Graphics.Output;

internal sealed class OutputArtifactEncodingException : InvalidOperationException
{
    public OutputArtifactEncodingException(string message)
        : base(message)
    {
    }
}
