# Claims And Output Contract

## Public Boundary

Lumiere is HDR-aware, not HDR-certified. The native FP16/scRGB-oriented preview
foundation and target-aware HDR state do not prove that an output artifact preserves HDR.

Approved language:

- "HDR-aware Windows screenshots."
- "Native capture and preview with an HDR-first graphics pipeline."
- "sRGB output tuned for visual match in everyday clipboard and file use."
- "HDR-preserved export is not yet a supported public path."

Prohibited without named hardware and viewer evidence:

- "Universal HDR fidelity."
- "Three supported color modes."
- "HDR preserved in every output."
- "HDR10/JXR supported."

## MVP Output Semantics

sRGB Visual Match is the only official MVP path. Clipboard and folder delivery
must consume the same shared conversion result; target-specific tone mapping drift
is a Lumiere defect.

The default conversion must:

- keep ordinary SDR-range content visually stable where practical;
- smoothly compress HDR highlights rather than hard-clamping them;
- preserve usable shadows and overall contrast;
- emit compatible RGBA8/sRGB pixels;
- remain fixed and regression-testable instead of exposing capture-time tuning.

## Blocking Visual Failures

The MVP must not ship when Lumiere causes any of these in supported scenarios:

- obvious overexposure, large dead-white regions, washed-out output, or gray output;
- ordinary desktop UI that is visibly too dark, color-shifted, or collapsed in contrast;
- obvious clipboard/folder visual drift for the same converted capture.

Narrow target-app recoloring/compression and loss of fine detail in an extreme HDR
scene may be recorded as limitations only when Lumiere's source artifact is valid.

## Future HDR-Preserved Export Gate

Before publishing one HDR-preserved path, record its exact format and extension,
source and destination pixel formats, transfer function, primaries, tone/gamut policy,
metadata policy, named viewer assumptions, target-aware display state, and observed
Windows hardware evidence. A codec or high-bit-depth pixel format existing is only
implementation evidence, never product evidence.
