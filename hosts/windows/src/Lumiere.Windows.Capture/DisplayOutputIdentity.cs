namespace Lumiere.Windows.Capture;

internal sealed record DisplayOutputIdentity
{
    public DisplayOutputIdentity(string deviceName, int width, int height)
        : this(deviceName, left: null, top: null, width, height)
    {
    }

    public DisplayOutputIdentity(string deviceName, int left, int top, int width, int height)
        : this(deviceName, (int?)left, (int?)top, width, height)
    {
    }

    private DisplayOutputIdentity(string deviceName, int? left, int? top, int width, int height)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceName);
        ValidateSize(width, height);

        DeviceName = deviceName.Trim();
        Left = left;
        Top = top;
        Width = width;
        Height = height;
    }

    public string DeviceName { get; init; }

    public int? Left { get; init; }

    public int? Top { get; init; }

    public int Width { get; init; }

    public int Height { get; init; }

    public static DisplayOutputIdentity FromMonitorDisplayName(
        string displayName,
        int width,
        int height)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ValidateSize(width, height);

        return new DisplayOutputIdentity(displayName.Trim(), width, height);
    }

    public static DisplayOutputIdentity FromMonitorDisplayName(
        string displayName,
        int left,
        int top,
        int width,
        int height)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ValidateSize(width, height);

        return new DisplayOutputIdentity(displayName.Trim(), left, top, width, height);
    }

    public DisplayOutputIdentity WithSize(int width, int height)
    {
        ValidateSize(width, height);
        return this with { Width = width, Height = height };
    }

    private static void ValidateSize(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width),
                $"Display output identity requires a positive size, got {width}x{height}.");
        }
    }
}
