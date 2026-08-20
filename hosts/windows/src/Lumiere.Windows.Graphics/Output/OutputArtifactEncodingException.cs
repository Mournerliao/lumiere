namespace Lumiere.Windows.Graphics.Output;

public sealed class OutputArtifactEncodingException : InvalidOperationException
{
    public OutputArtifactEncodingException(string message)
        : base(message)
    {
    }
}
