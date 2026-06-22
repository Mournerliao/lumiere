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

    public static string PrefixOutputDetail(CaptureTarget? target, string? detail)
    {
        var trimmedDetail = detail?.Trim();
        var prefix = DescribeOutputTarget(target);

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

    private static string DescribeOutputTarget(CaptureTarget? target)
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
            CaptureTargetKind.Display when target.DisplayIdentity is { Left: { } left, Top: { } top } identity =>
                $"Captured display: {label} ({identity.DeviceName}) at desktop bounds {left},{top} {identity.Width}x{identity.Height}.",
            CaptureTargetKind.Display when target.DisplayIdentity is not null =>
                $"Captured display: {label} ({target.DisplayIdentity.DeviceName}) at {target.DisplayIdentity.Width}x{target.DisplayIdentity.Height}.",
            CaptureTargetKind.Display =>
                $"Captured display: {label}.",
            CaptureTargetKind.Window =>
                $"Captured window: {label}.",
            _ =>
                $"Captured target: {label}.",
        };
    }
}
