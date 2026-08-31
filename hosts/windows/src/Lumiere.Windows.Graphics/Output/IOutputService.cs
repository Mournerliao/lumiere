using Lumiere.Windows.Graphics.Presentation;

namespace Lumiere.Windows.Graphics.Output;

/// <summary>
/// Abstraction for output operations (clipboard, file, or both).
/// Concrete implementations handle the actual output target logic.
/// </summary>
internal interface IOutputService
{
    /// <summary>
    /// Executes an output operation for the given captured frame.
    /// </summary>
    /// <param name="request">The output request containing the frame, crop region, and settings.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>An OutputResult indicating success, failure, or skipped for each target.</returns>
    Task<OutputResult> ExecuteOutputAsync(OutputRequest request, CancellationToken cancellationToken = default);

    Task<byte[]> EncodePngAsync(
        CapturedFrameTexture texture,
        CropPixelRect? cropRegion,
        SrgbVisualMatchConversionContext context,
        CancellationToken cancellationToken = default);
}
