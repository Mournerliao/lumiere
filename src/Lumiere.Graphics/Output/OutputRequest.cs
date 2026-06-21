using Lumiere.Graphics.Presentation;

namespace Lumiere.Graphics.Output;

/// <summary>
/// Represents a request to output a captured frame to one or more targets.
/// </summary>
public sealed record OutputRequest
{
    /// <summary>
    /// Gets the captured frame texture to output.
    /// </summary>
    public required CapturedFrameTexture Texture { get; init; }

    /// <summary>
    /// Gets the optional crop region to apply before output.
    /// Null means output the entire frame.
    /// </summary>
    public CropPixelRect? CropRegion { get; init; }

    /// <summary>
    /// Gets the configured output policy for this request.
    /// </summary>
    public OutputPolicy Policy { get; init; } = OutputPolicy.Default;
}

/// <summary>
/// Immutable output policy derived from the shared persisted settings source.
/// </summary>
public sealed record OutputPolicy(
    OutputTarget Target,
    bool CopyAsImage,
    string? SavePath,
    bool TimestampNaming,
    string? AfterCaptureBehavior,
    OutputProfileContract RequestedProfile,
    OutputProfileExecutionCapabilities ExecutionCapabilities)
{
    /// <summary>
    /// Gets the default output policy.
    /// </summary>
    public static readonly OutputPolicy Default = new(
        OutputTarget.Clipboard,
        CopyAsImage: true,
        SavePath: null,
        TimestampNaming: true,
        AfterCaptureBehavior: null,
        OutputProfileContract.SrgbCompatibilityPng,
        OutputProfileExecutionCapabilities.CompatibilityOnly);

    /// <summary>
    /// Gets whether clipboard image output should be attempted.
    /// </summary>
    public bool ShouldAttemptClipboard => (Target is OutputTarget.Clipboard or OutputTarget.Both) && CopyAsImage;

    /// <summary>
    /// Gets whether folder output should be attempted by a service that supports file artifacts.
    /// </summary>
    public bool ShouldAttemptFolder => Target is OutputTarget.Folder or OutputTarget.Both;

    /// <summary>
    /// Gets the supported after-capture action represented by the raw settings value.
    /// </summary>
    public OutputAfterCaptureAction AfterCaptureAction => OutputAfterCaptureActionParser.Parse(AfterCaptureBehavior);

    /// <summary>
    /// Gets the executable output profile currently used by the output pipeline.
    /// </summary>
    public OutputProfileContract EffectiveProfile => ResolveAggregateEffectiveProfile();

    /// <summary>
    /// Gets whether a requested non-executable profile falls back to the compatibility profile.
    /// </summary>
    public bool UsesCompatibilityProfileFallback => RequestedProfile.Kind != EffectiveProfile.Kind;

    /// <summary>
    /// Gets the executable output profile used by a concrete output target.
    /// </summary>
    public OutputProfileContract EffectiveProfileFor(OutputTarget target) =>
        target switch
        {
            OutputTarget.Clipboard => OutputProfileContract.SrgbCompatibilityPng,
            OutputTarget.Folder => ExecutionCapabilities.SelectEffectiveProfile(RequestedProfile),
            OutputTarget.Both => ResolveAggregateEffectiveProfile(),
            _ => OutputProfileContract.SrgbCompatibilityPng,
        };

    /// <summary>
    /// Gets whether a concrete target uses a compatibility fallback for the requested profile.
    /// </summary>
    public bool UsesCompatibilityProfileFallbackFor(OutputTarget target) =>
        RequestedProfile.Kind != EffectiveProfileFor(target).Kind;

    /// <summary>
    /// Creates a policy from raw settings values without taking a dependency on the settings module.
    /// </summary>
    public static OutputPolicy FromSettings(
        OutputTarget target,
        bool copyAsImage,
        string? savePath,
        bool timestampNaming,
        string? afterCaptureBehavior,
        string? exportColorFormat = null,
        IEnumerable<OutputValidationSessionArtifact>? validationArtifacts = null,
        OutputProfileExecutionCapabilities? executionCapabilities = null)
    {
        var requestedProfile = OutputProfileContract.FromSettingsValue(exportColorFormat);
        if (validationArtifacts is not null)
        {
            requestedProfile = OutputProfileTargetScope.ApplyValidationArtifacts(
                requestedProfile,
                validationArtifacts,
                target);
        }

        return new(
            target,
            copyAsImage,
            string.IsNullOrWhiteSpace(savePath) ? null : savePath.Trim(),
            timestampNaming,
            string.IsNullOrWhiteSpace(afterCaptureBehavior) ? null : afterCaptureBehavior.Trim(),
            requestedProfile,
            executionCapabilities ?? OutputProfileExecutionCapabilities.CompatibilityOnly);
    }

    private OutputProfileContract ResolveAggregateEffectiveProfile()
    {
        var attemptedProfiles = new List<OutputProfileContract>(capacity: 2);
        if (ShouldAttemptClipboard)
        {
            attemptedProfiles.Add(EffectiveProfileFor(OutputTarget.Clipboard));
        }

        if (ShouldAttemptFolder)
        {
            attemptedProfiles.Add(EffectiveProfileFor(OutputTarget.Folder));
        }

        if (attemptedProfiles.Count == 0)
        {
            return OutputProfileContract.SrgbCompatibilityPng;
        }

        var first = attemptedProfiles[0];
        return attemptedProfiles.All(profile => profile.Kind == first.Kind)
            ? first
            : OutputProfileContract.SrgbCompatibilityPng;
    }
}

/// <summary>
/// Output-owned representation of supported post-capture artifact actions.
/// </summary>
public enum OutputAfterCaptureAction
{
    None = 0,
    Open = 1,
    Reveal = 2,
}

internal static class OutputAfterCaptureActionParser
{
    public static OutputAfterCaptureAction Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return OutputAfterCaptureAction.None;
        }

        return value.Trim() switch
        {
            nameof(OutputAfterCaptureAction.Open) => OutputAfterCaptureAction.Open,
            nameof(OutputAfterCaptureAction.Reveal) => OutputAfterCaptureAction.Reveal,
            "RevealInFolder" => OutputAfterCaptureAction.Reveal,
            _ => OutputAfterCaptureAction.None,
        };
    }
}
