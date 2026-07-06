# Product Roadmap

This roadmap keeps Lumiere focused on a useful MVP first, then grows toward stronger HDR guarantees.

## Phase 1: HDR-Aware MVP

Goal: ship the core Windows screenshot tool with one official output path: sRGB Visual Match.

Execution checklist: `knowledge/engineering/mvp-development-plan.md`.

- Fast region and fullscreen capture.
- Main window, tray, and shortcut entry points.
- Clipboard and folder output.
- FP16/scRGB-oriented preview path.
- Honest HDR state and output copy.
- HDR-to-sRGB visual-match conversion that avoids obvious overexposure, washed-out output, or gray output in common supported paths.
- P3 and HDR10 remain planned profiles in the model and roadmap, but they are not normal MVP UI choices or supported MVP modes.
- Lightweight Windows validation for the supported paths.

## Phase 2: Single HDR-Preserved Export Path

Goal: add one narrow, validated HDR-preserved file export path.

- Pick one format and viewer target before making public claims.
- Define source format, destination format, transfer function, primaries, conversion policy, metadata policy, and viewer assumptions.
- Validate that the artifact opens and is recognized as HDR by the named viewer.
- Keep clipboard and unknown viewers scoped as compatible or unvalidated unless separately proven.

JPEG XR may be a candidate because Windows Imaging Component supports high-bit-depth JPEG XR pixel formats, including half-float paths. That still does not make JPEG XR a public HDR-preserved feature until a real encoded artifact and named viewer behavior are validated.

## Phase 3: Broader Compatibility

Goal: expand confidence without overwhelming the MVP.

- Additional viewers and target apps.
- Mixed HDR/SDR monitor scenarios.
- DPI and accessibility hardening.
- Longer capture/output stability runs.
- More explicit release notes and support matrix.

## Non-Goals

- Becoming a general image editor.
- Replacing professional HDR mastering tools.
- Claiming display-independent visual identity across all HDR monitors.
- Supporting non-native production UI stacks.
