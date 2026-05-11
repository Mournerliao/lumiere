namespace Lumiere.Graphics.Output;

/// <summary>
/// Represents the result of an output operation, with per-target success/failure/skipped state.
/// </summary>
public sealed record OutputResult
{
    /// <summary>
    /// Gets whether the overall output operation succeeded (at least one target succeeded).
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the clipboard output outcome.
    /// </summary>
    public OutputOutcome ClipboardOutcome { get; init; } = OutputOutcome.Skipped;

    /// <summary>
    /// Gets the folder output outcome.
    /// </summary>
    public OutputOutcome FolderOutcome { get; init; } = OutputOutcome.Skipped;

    /// <summary>
    /// Gets a user-facing message summarizing the output result.
    /// </summary>
    public string? UserMessage { get; init; }

    /// <summary>
    /// Gets a technical detail for diagnostics logging.
    /// </summary>
    public string? TechnicalDetail { get; init; }

    /// <summary>
    /// Creates a successful clipboard output result.
    /// </summary>
    public static OutputResult ClipboardSuccess(int bytesCopied) =>
        new()
        {
            IsSuccess = true,
            ClipboardOutcome = OutputOutcome.Success,
            UserMessage = "Copied to clipboard",
            TechnicalDetail = $"Clipboard output success: {bytesCopied} bytes"
        };

    /// <summary>
    /// Creates a failed clipboard output result.
    /// </summary>
    public static OutputResult ClipboardFailed(string? detail = null) =>
        new()
        {
            IsSuccess = false,
            ClipboardOutcome = OutputOutcome.Failed,
            UserMessage = "Failed to copy to clipboard",
            TechnicalDetail = detail ?? "Clipboard output failed"
        };

    /// <summary>
    /// Creates a result indicating the output was skipped (no valid region or target).
    /// </summary>
    public static OutputResult Skipped(string reason) =>
        new()
        {
            IsSuccess = false,
            UserMessage = reason,
            TechnicalDetail = reason
        };
}

/// <summary>
/// Represents the outcome of a single output target operation.
/// </summary>
public enum OutputOutcome
{
    /// <summary>
    /// The target was not requested or not applicable.
    /// </summary>
    Skipped = 0,

    /// <summary>
    /// The target output succeeded.
    /// </summary>
    Success = 1,

    /// <summary>
    /// The target output failed.
    /// </summary>
    Failed = 2
}
