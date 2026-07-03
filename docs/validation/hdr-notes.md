# HDR Notes

These notes capture the HDR risks that should guide implementation without blocking the first MVP.

## What MVP Means

The MVP is HDR-aware, not HDR-certified. It keeps the native FP16/scRGB preview foundation and reports HDR state honestly, but it does not claim that every output preserves HDR data.

## Key Concepts

- Windows Advanced Color composition uses FP16/scRGB as a practical HDR composition space.
- HDR display behavior depends on the active target display, Windows HDR setting, driver, display capabilities, and color management.
- An artifact being written successfully does not prove visual match or HDR preservation.
- Clipboard compatibility and HDR-preserved file export are different product paths.

## JPEG XR And HDR-Preserved Export

JPEG XR remains a possible future export path because Windows Imaging Component supports high-bit-depth and half-float JPEG XR pixel formats. That is implementation evidence, not a public product claim.

Before any HDR-preserved export is public, record:

- The exact format and extension.
- Source and destination pixel format.
- Transfer function and primaries.
- Conversion or tone-mapping policy.
- Metadata policy.
- Named viewer assumptions.
- Windows manual validation showing artifact handling, visual result, HDR preservation, and viewer recognition where applicable.

## Product Boundary

Until that evidence exists, use compatible-output wording. Keep HDR-preserved export in roadmap or experimental language only.
