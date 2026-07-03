# 0003: Shared sRGB Visual Match Conversion

Date: 2026-07-03

## Decision

The MVP sRGB Visual Match conversion must be implemented as a shared output component used by clipboard, folder, and both-target output. Clipboard and folder services may differ in delivery mechanics, but they should not own separate HDR/scRGB to SDR/sRGB tone-mapping behavior.

The shared component should produce a unified RGBA8/sRGB visual-match bitmap or artifact from the FP16/scRGB capture source. Clipboard and folder services should deliver that result to their targets; they should not own independent visual-match conversion logic.

The first implementation should use a simple, fixed-parameter, explainable tone mapper. It should keep ordinary SDR-range content visually stable where practical, smoothly compress HDR highlights above the SDR range instead of hard-clamping them, then output sRGB-compatible pixels.

The conversion should not introduce a user-visible HDR-highlight detection mode or toggle. The same default conversion should run for the MVP output path: ordinary SDR-range content remains stable, and HDR highlights are naturally compressed when present.

## Context

Lumiere's MVP promises one official output path across clipboard and file workflows. If each target implements its own conversion, visual output can drift, validation expands, and tuning the default conversion becomes target-specific.

## Consequences

- Tone-mapping fixes should be made once in the shared output conversion path.
- Clipboard and folder validation should compare against the same sRGB Visual Match semantics.
- The shared conversion component should not take responsibility for clipboard APIs, file-system writes, or target-specific delivery mechanics.
- The first tone mapper should be easy to regression-test before pursuing more complex adaptive or local tone mapping.
- The UI should not expose HDR-highlight detection state as an output mode; detection and highlight handling are internal conversion behavior.
- Target-specific app behavior can still be recorded as a limitation, but target-specific conversion drift is a Lumiere bug.
