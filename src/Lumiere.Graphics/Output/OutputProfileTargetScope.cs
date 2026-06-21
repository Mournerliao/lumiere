namespace Lumiere.Graphics.Output;

/// <summary>
/// Resolves which validation-artifact scope is allowed to influence a requested output profile.
/// </summary>
public static class OutputProfileTargetScope
{
    public static OutputProfileContract ApplyValidationArtifacts(
        OutputProfileContract contract,
        IEnumerable<OutputValidationSessionArtifact> artifacts,
        OutputTarget outputTarget)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(artifacts);

        return OutputValidationSessionArtifact.ApplyAllTo(
            contract,
            artifacts,
            ResolveValidationTarget(contract, outputTarget));
    }

    public static OutputTarget ResolveValidationTarget(
        OutputProfileContract contract,
        OutputTarget outputTarget)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return outputTarget switch
        {
            OutputTarget.Clipboard => OutputTarget.Clipboard,
            OutputTarget.Both when contract.Kind is OutputProfileKind.SrgbCompatibilityPng => OutputTarget.Both,
            OutputTarget.Both => OutputTarget.Folder,
            _ => OutputTarget.Folder,
        };
    }
}
