# Sprint Change Proposal: Enhance Story 8.1 HDR Detection

**Date:** 2026-06-03
**Triggered by:** Windows manual validation of Story 8.2
**Scope:** Minor

## Issue Summary

HDR status detection relies solely on `IDXGISwapChain3::CheckColorSpaceSupport(RgbFullG10NoneP709)`. On many Windows systems, even when HDR is disabled in Display settings, the swap chain still reports scRGB as supported (hardware capability + Windows tone mapping). This causes the app to show "HDR Ready" when HDR is actually off.

**Evidence:** User tested Story 8.2 on Windows with HDR disabled. Expected "Enable HDR" trust label, observed "HDR Ready". Story 8.2 alert system (InfoBar, tray hint, overlay message) never triggers because the readiness state never becomes `Degraded`.

## Impact Analysis

| Artifact | Impact |
|---|---|
| Story 8.1 (done) | AC states "the app evaluates display, system HDR, capture, preview, and output evidence" — but no system HDR query exists |
| Story 8.2 (review) | Alert logic is correct but receives wrong input state; most common HDR alert scenario is unreachable |
| Epic 8 trust foundation | Users cannot trust HDR status display; undermines the "trust" promise of Epic 8 |

**No impact on:** PRD scope, Architecture module boundaries, UX design specification (the UX intent is correct; the detection mechanism is the gap).

## Recommended Approach

### Direct Adjustment: Enhance Story 8.1

Add DXGI output description query to detect actual HDR capability before swap chain color space probing.

**Current flow:**
```
Create swap chain → CheckColorSpaceSupport(scRGB) → infer state
```

**Enhanced flow:**
```
Enumerate DXGI adapters → Enumerate outputs → IDXGIOutput6.GetDesc1()
  → Check DXGI_OUTPUT_DESC1.ColorSpace for Advanced Color support
  → Combine with CheckColorSpaceSupport result
  → Determine accurate readiness state
```

**Key API:**
- `IDXGIFactory1.EnumAdapters1()` — enumerate GPU adapters
- `IDXGIAdapter.EnumOutputs()` — enumerate displays
- `IDXGIOutput.QueryInterface<IDXGIOutput6>()` — get advanced output info
- `IDXGIOutput6.GetDesc1()` → `DXGI_OUTPUT_DESC1.ColorSpace` — HDR capability indicator

**ColorSpace values:**
- `DXGI_COLOR_SPACE_RGB_FULL_G2084_NONE_P2020` (HDR10) — display supports HDR
- `DXGI_COLOR_SPACE_RGB_FULL_G22_NONE_P709` (sRGB) — standard display

**Where to add:**
- New type in `Lumiere.Graphics.Hdr` (e.g., `HdrDisplayCapability`) to hold the probe result
- New probing logic in `Lumiere.Graphics.Presentation` or `Lumiere.Graphics.Devices`
- Integrate into `MainWindow.StartPreview` or `SwapChainManager.CreateCompositionSwapChain` flow
- Probe result feeds into `PresentationEvidence` alongside existing `CheckColorSpaceSupport`

**Fallback:** If DXGI output enumeration fails (e.g., headless, remote desktop), fall back to existing `CheckColorSpaceSupport` behavior with `Degraded` as safe default.

## Detailed Change Proposals

### Story 8.1 Enhancement

**Story:** 8.1 — Complete Evidence-Based HDR State Mapping
**Section:** Tasks/Subtasks

**Add new subtask:**

```
- [ ] Subtask X.1: Add `HdrDisplayCapability` probe type in `Lumiere.Graphics.Hdr`
      that enumerates DXGI outputs and queries `IDXGIOutput6.GetDesc1()` for
      `DXGI_OUTPUT_DESC1.ColorSpace` to determine if the display supports HDR
- [ ] Subtask X.2: Integrate HDR display probe into the swap chain creation flow
      so `PresentationEvidence` reflects both display capability and swap chain
      color space support
- [ ] Subtask X.3: When HDR display probe indicates HDR is not enabled but
      `CheckColorSpaceSupport` reports present, prefer the display probe result
      and mark readiness as `Degraded` with user message "Enable HDR in Windows
      Display settings for best capture quality"
- [ ] Subtask X.4: Add unit tests for `HdrDisplayCapability` mapping logic
      (hardware-independent, test the mapping from ColorSpace values to
      PreviewReadinessState)
- [ ] Subtask X.5: Run full validation: restore, build, tests, format verification
```

### Sprint Status Update

**Current:** `8-1-complete-evidence-based-hdr-state-mapping: done`
**Proposed:** Reopen to `in-progress` for enhancement, or track as follow-up subtasks

## Implementation Handoff

**Classification:** Minor — can be implemented directly by Developer agent

**Deliverables:**
- `HdrDisplayCapability` probe type
- Integration into swap chain creation flow
- Unit tests for capability mapping
- Updated Story 8.1 task list

**Dependencies:** Vortice.Windows DXGI bindings (already in project as dependency)
