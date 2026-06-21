using Lumiere.Capture;

namespace Lumiere.App;

public static class CaptureTargetScopeProjection
{
    public static string PrefixDetail(CaptureTarget? target, string? detail)
    {
        var trimmedDetail = detail?.Trim();
        var prefix = DescribeTarget(target);

        if (string.IsNullOrWhiteSpace(prefix))
        {
            return trimmedDetail ?? string.Empty;
        }

        return string.IsNullOrWhiteSpace(trimmedDetail)
            ? prefix
            : $"{prefix} {trimmedDetail}";
    }

    private static string DescribeTarget(CaptureTarget? target)
    {
        if (target is null)
        {
            return string.Empty;
        }

        var label = string.IsNullOrWhiteSpace(target.DisplayName)
            ? "Capture target"
            : target.DisplayName.Trim();

        return target.Kind switch
        {
            CaptureTargetKind.Display => $"Selected display: {label}.",
            CaptureTargetKind.Window => $"Selected window: {label}.",
            _ => $"Selected target: {label}.",
        };
    }
}
