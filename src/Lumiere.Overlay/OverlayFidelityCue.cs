namespace Lumiere.Overlay;

public sealed record OverlayFidelityCue(
    OverlayFidelityClaimKind Kind,
    string Label,
    string Detail)
{
    public static OverlayFidelityCue Unvalidated { get; } =
        new(
            OverlayFidelityClaimKind.Unvalidated,
            "Output unvalidated",
            "No output fidelity claim is made until validation evidence exists.");

    public static OverlayFidelityCue Converted { get; } =
        new(
            OverlayFidelityClaimKind.Converted,
            "Converted output",
            "Output may be compatibility-converted and must not be treated as HDR-preserved.");

    public static OverlayFidelityCue VisualMatch { get; } =
        new(
            OverlayFidelityClaimKind.VisualMatch,
            "Visual match",
            "Output is expected to match visually, pending target-app compatibility evidence.");

    public static OverlayFidelityCue HdrPreserved { get; } =
        new(
            OverlayFidelityClaimKind.HdrPreserved,
            "HDR-preserved",
            "Supported output path has target-aware validation evidence.");

    public static OverlayFidelityCue FromClaim(OverlayFidelityClaimKind kind) =>
        kind switch
        {
            OverlayFidelityClaimKind.Converted => Converted,
            OverlayFidelityClaimKind.VisualMatch => VisualMatch,
            OverlayFidelityClaimKind.HdrPreserved => HdrPreserved,
            _ => Unvalidated,
        };
}

public enum OverlayFidelityClaimKind
{
    Unvalidated = 0,
    Converted,
    VisualMatch,
    HdrPreserved,
}
