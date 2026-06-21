---
title: 'Validate Mixed HDR SDR And Multi-Monitor Trust States'
type: 'feature'
created: '2026-06-22'
status: 'in-progress'
route: 'validation-asset'
story: '10-3'
---

# Validate Mixed HDR / SDR And Multi-Monitor Trust States

## Intent

`Public perfect-HDR-fidelity` now has target-aware HDR probing and trust projections in code, but Story `10-3` was still under-specified at the manual-validation layer.

The release checklist named the gate, yet another agent or tester still had to improvise:

- which trust surfaces must agree
- which topologies actually matter
- how to prove mixed HDR/SDR switching follows the active capture target

This slice turns Story `10-3` into an explicit Windows validation workflow so real hardware evidence can be collected without re-deriving the procedure from planning prose.

## Delivered In This Slice

1. Added [Target-Aware HDR Validation](../../harness/validation/target-aware-hdr-validation.md) as the focused manual workflow for Story `10-3`.
2. Wired the new workflow into the live validation surface so release-gate readers can find it from:
   - [Validation Index](../../harness/validation/index.md)
   - [Release Validation Checklist](../../harness/validation/release-validation-checklist.md)
   - [HDR / SDR Validation Scenarios](../../harness/validation/hdr-sdr-validation-scenarios.md)
3. Defined the required trust surfaces that must agree during validation:
   - main panel
   - overlay
   - tray
   - settings evidence rows
4. Defined the topology buckets that matter for public claims:
   - single HDR enabled
   - single HDR disabled
   - single SDR
   - mixed HDR + SDR multi-monitor
   - same-DPI multi-monitor
   - mixed-DPI multi-monitor
5. Main-panel, tray, and overlay trust copy now prefix the active target scope directly in user-facing detail text, reducing ambiguity about which display the current HDR state belongs to during mixed-monitor validation.

## Suggested Review Order

1. [Target-Aware HDR Validation](../../harness/validation/target-aware-hdr-validation.md)
2. [Release Validation Checklist](../../harness/validation/release-validation-checklist.md)
3. [HDR / SDR Validation Scenarios](../../harness/validation/hdr-sdr-validation-scenarios.md)
4. [Validation Index](../../harness/validation/index.md)

## Why This Matters

Story `10-3` is currently blocked by missing Windows evidence, not by missing projection code alone.

This workflow closes the process gap between:

- target-aware code support that now exists in capture, HDR probing, and UI projections
- the real mixed-monitor evidence still required before Public perfect-HDR-fidelity can treat Epic 10 as complete

## Remaining Work

Story `10-3` is still `in-progress`, not `done`.

Remaining follow-up:

- Run the workflow on at least one real mixed HDR/SDR multi-monitor machine.
- Record build/commit, display topology, DPI, and observed target-switch behavior in a session artifact.
- Backfill checklist rows `REL-HDR-01` through `REL-HDR-04` with actual evidence instead of plan-only status.
