# HDR Validation Scenarios

These fixed scenes are lightweight regression anchors for sRGB Visual Match. They
do not certify universal viewer, topology, or HDR-preserved behavior.

## Bright HDR Scene

Use HDR video, a game, or web content with strong highlights. Check for obvious
overexposure, large dead-white regions, washed-out/gray output, and lost highlight shape.

## Dark Scene

Use dark UI, a night scene, or low-key media. Check that shadow detail remains usable
and that the image is not crushed into black or collapsed in contrast.

## Everyday Desktop Scene

Use a normal desktop, browser, or app UI. Check text, white surfaces, colors, ordinary
SDR-range stability, and natural overall contrast.

## Output Comparison

Exercise clipboard, folder, and both-target output where configured. Compare the same
capture for visual drift caused by Lumiere. If Lumiere's file is correct but a receiving
app compresses or recolors it, record that separately with the app and version.

The blocking rules and public claim boundary live in `knowledge/contracts/claims.md`.
