# Target-Aware HDR Validation

Updated: 2026-06-22

This document is the focused Windows manual validation workflow for Epic 10 / Story 10-3. It verifies that Lumiere's HDR trust state follows the active capture target rather than a generic desktop or first-output assumption.

Use it together with:

- `release-validation-checklist.md`
- `hdr-sdr-validation-scenarios.md`
- `overlay-validation.md`
- `settings-accessibility-validation.md` when target-aware status copy or export-profile honesty changes

## Scope

This workflow covers:

- `REL-HDR-01`
- `REL-HDR-02`
- `REL-HDR-03`
- `REL-HDR-04`
- related trust-surface checks in `REL-CAP-01` through `REL-CAP-08`

## Required Trust Surfaces

For every topology that can be tested, validate at least these surfaces:

1. Main panel target HDR pill
2. Main panel fidelity/trust summary
3. Overlay status copy and fidelity cue
4. Tray status summary
5. Settings validation rows for target-aware evidence

## Required Display Topologies

Prefer covering all of the following. If a topology is unavailable, record it explicitly as `NOT RUN`.

1. Single HDR-capable display with Windows HDR enabled
2. Single HDR-capable display with Windows HDR disabled
3. Single SDR-only display
4. Mixed HDR + SDR multi-monitor desktop
5. Multi-monitor same-DPI
6. Multi-monitor mixed-DPI

## Pre-Session Setup

1. Record Windows version, GPU, device, display arrangement, and DPI scale.
2. Record which display is HDR-capable, which display currently has Windows HDR enabled, and which display will be used as the active capture target.
3. Prepare at least one bright-highlight scene and one dark-scene target from `hdr-sdr-validation-scenarios.md`.
4. Confirm whether capture will be entered from the main panel, tray, hotkey, or all three.

## Core Validation Flow

Run this flow once per topology and once per target display that matters.

1. Start capture against the intended display target.
2. Confirm the main panel trust state matches the target display rather than another monitor.
3. Open region capture and confirm the overlay opens on the intended target display.
4. Confirm overlay status text and fidelity cue remain honest for the active target:
   - HDR-ready target may show HDR-ready trust only when the target is actually HDR-ready
   - unresolved or mismatched target evidence must stay unvalidated/degraded rather than promoting to `HDR Ready`
5. If tray is available, compare the tray trust summary against the same active target.
6. Open settings and confirm target-aware evidence rows describe the same target state and limitation.
7. Repeat capture from at least one alternate entry point when available:
   - main panel
   - tray
   - hotkey
8. Record whether the trust state stayed aligned after:
   - canceling capture
   - reopening capture
   - switching target display
   - moving between HDR and SDR targets on a mixed desktop

## Mixed HDR / SDR Multi-Monitor Checks

These checks are the critical public-release blocker for Story 10-3.

1. Place an HDR-capable target on one monitor and an SDR-only or HDR-disabled target on another.
2. Start capture on the HDR target and record the trust state across main panel, overlay, tray, and settings.
3. Start capture on the SDR or HDR-disabled target and record the same surfaces again.
4. Confirm the state changes with the target, not just with the desktop-wide presence of any HDR display.
5. If target-aware matching cannot be proven, record the exact unvalidated/degraded wording rather than treating it as a pass.
6. If only one side of the mixed setup is testable, record the missing half as a limitation, not as implied coverage.

Expected result:

- Lumiere follows the active capture target.
- Mixed-monitor ambiguity is surfaced honestly.
- No surface claims `HDR Ready` for the wrong display.

## DPI And Placement Checks

At each available DPI scale:

1. Confirm the overlay opens on the intended target display.
2. Confirm trust/status text remains readable and does not overflow critical layout.
3. Confirm switching between main panel and settings does not hide target-aware evidence or compress it into unreadable copy.
4. Confirm main-panel alert states still leave trust surfaces visible when target-aware status is degraded or unvalidated.

Expected result:

- Target-aware trust remains understandable at the tested DPI.
- Layout pressure does not hide which display the state belongs to.

## Result Recording Notes

Record findings separately for:

- `HDR target pass`
- `HDR-disabled target pass`
- `SDR target pass`
- `mixed-monitor target switch pass`
- `target-aware limitation`
- `wrong-display trust defect`
- `layout/readability defect`

Use the fidelity vocabulary from `hdr-sdr-validation-scenarios.md` and do not reduce results to a single generic "works" statement.
