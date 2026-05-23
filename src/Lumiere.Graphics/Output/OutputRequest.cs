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
    string? AfterCaptureBehavior)
{
    /// <summary>
    /// Gets the default output policy.
    /// </summary>
    public static readonly OutputPolicy Default = new(
        OutputTarget.Clipboard,
        CopyAsImage: true,
        SavePath: null,
        TimestampNaming: true,
        AfterCaptureBehavior: null);

    /// <summary>
    /// Gets whether clipboard image output should be attempted.
    /// </summary>
    public bool ShouldAttemptClipboard => (Target is OutputTarget.Clipboard or OutputTarget.Both) && CopyAsImage;

    /// <summary>
    /// Gets whether folder output should be attempted by a service that supports file artifacts.
    /// </summary>
    public bool ShouldAttemptFolder => Target is OutputTarget.Folder or OutputTarget.Both;

    /// <summary>
    /// Creates a policy from raw settings values without taking a dependency on the settings module.
    /// </summary>
    public static OutputPolicy FromSettings(
        OutputTarget target,
        bool copyAsImage,
        string? savePath,
        bool timestampNaming,
        string? afterCaptureBehavior) =>
        new(
            target,
            copyAsImage,
            string.IsNullOrWhiteSpace(savePath) ? null : savePath.Trim(),
            timestampNaming,
            string.IsNullOrWhiteSpace(afterCaptureBehavior) ? null : afterCaptureBehavior.Trim());
}
