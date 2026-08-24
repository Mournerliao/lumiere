using System.Globalization;

namespace Lumiere.Windows.Interop;

internal readonly record struct NativePoint(int X, int Y)
{
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{X},{Y}");
}

internal readonly record struct NativeRect(int X, int Y, int Width, int Height)
{
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{X},{Y},{Width}x{Height}");
}
