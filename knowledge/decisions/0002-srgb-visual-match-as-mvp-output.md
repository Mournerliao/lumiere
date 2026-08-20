# 0002: sRGB Visual Match As MVP Output

Date: 2026-07-03

## Decision

Lumiere's MVP will release one official output path: sRGB Visual Match. P3 and HDR-preserved export remain roadmap milestones only; they do not exist as dormant runtime profiles or user-selectable modes.

## Context

The product goal is for HDR screenshots to look normal after capture in common clipboard and file workflows, avoiding obvious overexposure, washed-out output, or gray output. Releasing P3 or HDR10 as supported MVP modes would require format, conversion, metadata, target-app, and viewer-validation work that belongs to later HDR-preserved export milestones.

## Consequences

- MVP engineering should prioritize HDR/scRGB to SDR/sRGB tone mapping and visual-match validation.
- UI and release copy must not imply that P3 or HDR10 are supported output modes.
- P3 and HDR10 remain future profiles, not hidden promises, normal MVP UI choices, or first-release blockers.
