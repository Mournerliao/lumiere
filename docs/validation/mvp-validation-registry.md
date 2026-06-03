# MVP Validation Registry

Generated: 2026-06-03
Story: 8.4 Record Validation Level for Every MVP Capability

This registry records the validation level for every MVP capability implemented in Epic 4 through Epic 8. It ensures release claims do not outrun evidence and that validation gaps are explicitly tracked.

## Validation Level Assignment Rules

| Level | Definition |
|-------|-----------|
| **Mac edit** | Pure code, test, or documentation changes with no Windows hardware dependencies. |
| **Windows CI-pass** | Automated gates pass on Windows (restore, build, tests, format verification) but no real hardware/platform behavior was manually exercised. |
| **Windows manual-pass** | Real WGC, DXGI, D3D11, HDR display, tray, hotkey, multi-monitor, DPI, clipboard/file output, or resource trend behavior was manually validated on Windows hardware with results recorded. |
| **Validation gap** | Capability exists but validation level is unknown or unrecorded. |

### Capabilities Requiring Windows Manual Validation

The following capability categories cannot be validated from unit tests alone (per NFR27, NFR33):

- WGC capture timing and frame pool behavior
- DXGI swap-chain presentation on real displays
- HDR display behavior (FP16/scRGB rendering, HDR-ready detection)
- Tray icon display and popup menu interaction
- Global hotkey registration and dispatch
- Multi-monitor overlay placement and capture targeting
- DPI scaling at 100%, 125%, 150%, 200%
- Clipboard output compatibility with target applications
- File output to real filesystem paths (permissions, long paths, missing paths)
- Resource trend monitoring (GPU memory, handles, private bytes) across repeated capture cycles
- SDR display fallback behavior
- Display topology changes during capture
- Active capture teardown during app quit

## Capability Registry by Module

### Foundation / Capture Cutover (Epic 4)

| Capability | FR/NFR | Story | Validation Level | Evidence | Known Gaps |
|-----------|--------|-------|-----------------|----------|------------|
| Foundation classification and module audit | FR44, FR49, NFR24, NFR29 | 4.1 | Mac edit | Documentation/audit only; no runtime changes | None |
| Shared capture session contract and command routing | FR6, FR7, FR8, FR15, NFR13, NFR14 | 4.2 | Windows CI-pass | Automated gates pass; `CaptureService.TryReserveCommand()` tests | No manual multi-entry-point stress testing |
| Legacy picker/dashboard demotion; direct monitor default | FR15, NFR22, NFR23 | 4.3 | Windows CI-pass | Automated gates pass; direct monitor path confirmed by code review | No manual picker fallback testing |
| App-facing seams (ICaptureCommandCoordinator, IOutputService, ISettingsProvider) | FR28, FR41, FR49, NFR26, NFR29 | 4.4 | Windows CI-pass | Automated gates pass; interface contracts tested | None |
| Direct monitor capture without picker | FR46 | 4.5 | Windows CI-pass + partial manual | Automated gates pass; direct monitor capture validated on Windows hardware | Escape cancel, multi-monitor, DPI 100%/125%/200%, SDR display, clipboard lock recovery not validated |
| Overlay behavior: placement, crop, cancel, feedback | FR47, FR18, FR19, FR20, FR21, FR24, NFR3, NFR20 | 4.5, 4.6 | Windows CI-pass + partial manual | Automated overlay/crop tests pass; partial manual on single HDR 4K at 150% DPI | Multi-monitor placement, DPI scales beyond 150%, SDR display |
| Basic clipboard output | FR48 | 4.5 | Windows CI-pass + partial manual | Clipboard write validated manually; paste-to-Paint confirmed | Clipboard lock recovery/failure injection not tested |
| Repeated capture lifecycle behavior | FR45, NFR5, NFR11 | 4.5 | Windows CI-pass + partial manual | Automated lifecycle tests pass; lifecycle checklist partially validated | Resource trend monitoring across repeated cycles not validated |
| Diagnostic observability for capture/overlay lifecycle | FR44, FR49, NFR5, NFR11, NFR30 | 4.7 | Windows CI-pass | Automated gates pass; structured logging verified by code review and tests | No manual log inspection on Windows hardware |

### Main Window and Settings (Epic 5)

| Capability | FR/NFR | Story | Validation Level | Evidence | Known Gaps |
|-----------|--------|-------|-----------------|----------|------------|
| Native v0 main panel: capture actions, HDR status, branding | FR1, FR2, FR9, FR30, NFR21, NFR22 | 5.1 | Windows CI-pass + partial manual | Automated gates pass; single HDR 4K at 150% DPI validated | Text scaling, high contrast, mixed-DPI, SDR, multi-monitor not validated |
| Settings navigation and shell | FR30, NFR22 | 5.2 | Windows CI-pass | Automated gates pass; projection logic tested | Rendered settings UI not manually validated |
| Shortcut and HDR alert settings UI | FR13, FR32, FR38, NFR24 | 5.3 | Windows CI-pass | Automated gates pass; settings projection tested | Keyboard navigation, screen reader not validated |
| Output preference settings UI (pending Epic 6) | FR34, FR35, FR36, NFR24 | 5.4 | Windows CI-pass | Automated gates pass; pending controls confirmed disabled/read-only | None (intentionally pending) |
| Local settings persistence across launches | FR28, FR38, NFR18, NFR19 | 5.5 | Windows CI-pass | Automated gates pass; JSON persistence tested | App relaunch persistence not manually validated |
| About and version information | FR37, NFR8 | 5.6 | Windows CI-pass | Automated gates pass; assembly metadata projection tested | Packaged version display not manually validated |

### Configured Output (Epic 6)

| Capability | FR/NFR | Story | Validation Level | Evidence | Known Gaps |
|-----------|--------|-------|-----------------|----------|------------|
| Output target policy and result model | FR22, FR24, FR25, FR28, FR29, NFR8, NFR9 | 6.1 | Windows CI-pass | Automated gates pass; result model tested | None |
| Configured clipboard output | FR22, FR24, FR25, FR27, FR28, FR48, NFR4, NFR8, NFR19 | 6.2 | Windows CI-pass | Automated gates pass; clipboard routing tested | Real clipboard compatibility with target apps not validated |
| Folder output with save path and timestamp naming | FR22, FR23, FR24, FR25, FR26, FR28, FR48, NFR18 | 6.3 | Windows CI-pass | Automated gates pass; path/naming logic tested | Protected folders, long paths, filesystem edge cases not validated |
| Both-target output and completion feedback | FR22, FR24, FR25, FR28, FR48, NFR4 | 6.4 | Windows CI-pass | Automated gates pass; both-target orchestration tested | Slow OS behavior, resource teardown not validated |
| Export and color format options (validation-scoped) | FR29, NFR8, NFR9, NFR24 | 6.5 | Windows CI-pass | Automated gates pass; controls confirmed disabled/scoped | None (intentionally validation-scoped) |
| Supported after-capture behavior | FR36, NFR24 | 6.6 | Windows CI-pass | Automated gates pass; open/reveal routing tested | Explorer reveal/open behavior not validated |

### Tray, Hotkeys, and Background (Epic 7)

| Capability | FR/NFR | Story | Validation Level | Evidence | Known Gaps |
|-----------|--------|-------|-----------------|----------|------------|
| Tray menu with status and commands | FR4, FR10, FR39, FR41, NFR23 | 7.1 | Windows manual-pass | Automated gates pass; Dana validated on Windows hardware 2026-05-26 | None |
| Open main window and settings from tray | FR31, FR40, FR41 | 7.2 | Windows manual-pass | Automated gates pass; Dana validated on Windows hardware 2026-05-26 | None |
| Global capture hotkeys | FR3, FR33, FR41, NFR23 | 7.3 | Windows manual-pass | Automated gates pass; Dana validated on Windows hardware 2026-05-26 | None |
| Background and minimize-to-tray workflow | FR5, FR39, FR41, NFR23 | 7.4 | Windows manual-pass | Automated gates pass; Dana validated on Windows hardware 2026-05-26 | None |
| Quit cleanly from tray with resource cleanup | FR42, FR43, NFR11, NFR26 | 7.5 | Windows manual-pass | Automated gates pass; Dana validated on Windows hardware 2026-05-26 | None |
| Capture state technical debt resolution | FR44, NFR10 | 7.6 | Windows CI-pass | Automated gates pass; code refactoring and tests | No manual validation required (internal refactoring) |

### HDR Trust, Recovery, and Diagnostics (Epic 8)

| Capability | FR/NFR | Story | Validation Level | Evidence | Known Gaps |
|-----------|--------|-------|-----------------|----------|------------|
| Evidence-based HDR state model (7 states) | FR11, FR14, FR20, NFR10, NFR21 | 8.1 | Windows CI-pass | Automated gates pass; projection tests cover all states | HDR display behavior on real HDR/SDR displays not validated; 7 vs 8 states (see deferred) |
| Actionable HDR alerts with user preference | FR12, FR13, FR20, NFR14, NFR22 | 8.2 | Windows CI-pass | Automated gates pass; alert projection tested | Alert display on real HDR/SDR displays not validated |
| Structured diagnostics and failure mapping | FR49, NFR17, NFR30 | 8.3 | Windows CI-pass | Automated gates pass; diagnostic records tested | No manual log inspection on Windows hardware |
| Validation level recording per capability | FR44, FR45, FR46, FR47, FR48, NFR27, NFR33 | 8.4 | Windows CI-pass | This registry document; automated gates pass | None (documentation story) |

## Validation Gaps

The following capabilities require Windows manual validation but currently only have Mac edit or Windows CI-pass evidence.

### Hardware Validation Gaps from Epic 4 (carried from deferred-work.md)

| Gap | Source Story | Impact | Status |
|-----|-------------|--------|--------|
| Escape cancel with and without active crop | 4.5 | Capability validation incomplete | Open gap |
| Multi-monitor behavior beyond single-monitor | 4.5 | NFR27 requirement not met | Open gap |
| DPI scales 100%, 125%, 200% (only 150% tested) | 4.5 | NFR27 requirement not met | Open gap |
| SDR display behavior not separately validated | 4.5 | HDR/SDR mixed setup claims unsupported | Open gap |
| Clipboard lock recovery/failure injection | 4.5 | NFR4 requirement not fully validated | Open gap |

### Settings and UI Validation Gaps from Epic 5

| Gap | Source Story | Impact | Status |
|-----|-------------|--------|--------|
| Text scaling and high contrast accessibility | 5.1 | Accessibility requirements not validated | Open gap |
| Mixed-DPI multi-monitor settings rendering | 5.1 | NFR27 requirement not met | Open gap |
| Keyboard navigation in settings | 5.3 | Accessibility requirements not validated | Open gap |
| Screen reader exposure for settings | 5.3 | Accessibility requirements not validated | Open gap |
| App relaunch persistence (packaged app) | 5.5 | FR38 not fully validated in packaged context | Open gap |

### Output Validation Gaps from Epic 6

| Gap | Source Story | Impact | Status |
|-----|-------------|--------|--------|
| Real clipboard compatibility (Paint, Photos, Chromium) | 6.2 | NFR19 requirement not fully validated | Open gap |
| Folder output to protected/inaccessible paths | 6.3 | NFR18 requirement not fully validated | Open gap |
| Explorer reveal/open behavior | 6.6 | After-capture behavior not validated | Open gap |
| Both-target partial failure with slow OS behavior | 6.4 | NFR4 requirement not fully validated | Open gap |

### HDR Display Validation Gaps from Epic 8

| Gap | Source Story | Impact | Status |
|-----|-------------|--------|--------|
| HDR state display on real HDR display | 8.1 | UX-DR5 not fully validated | Open gap |
| HDR state display on SDR display | 8.1 | HDR/SDR discrimination not validated | Open gap |
| Alert display behavior on HDR/SDR | 8.2 | Alert UX not validated on real displays | Open gap |

### Validation Gap Summary

| Category | Gap Count | Blocker for Release? |
|----------|-----------|---------------------|
| Hardware/platform behavior (Epic 4) | 5 | No — all gaps classified as limitation or deferred risk |
| Settings/accessibility (Epic 5) | 5 | No — accessibility gaps are limitations for early release |
| Output behavior (Epic 6) | 4 | No — clipboard/folder behavior classified as limitation |
| HDR display (Epic 8) | 4 | No — HDR trust gaps classified as deferred risk |
| Performance/stability (NFR1, NFR5) | 3 | No — responsiveness and resource gaps classified as limitation |
| **Total** | **21** | **No blockers — see release validation matrix for authoritative assessment** |

## Story-Level Validation Evidence Map

| Epic | Stories | Validation Level | Notes |
|------|---------|-----------------|-------|
| Epic 4 | 4.1 | Mac edit | Documentation/audit only |
| Epic 4 | 4.2, 4.3, 4.4, 4.6, 4.7 | Windows CI-pass | Code and test changes; no Windows manual validation docs in story files |
| Epic 4 | 4.5 | Windows CI-pass + partial manual | Story ran automated gates; manual validation gaps recorded in deferred-work.md |
| Epic 5 | 5.2, 5.3, 5.4, 5.5, 5.6 | Windows CI-pass | UI and settings work; no manual validation |
| Epic 5 | 5.1 | Windows CI-pass + partial manual | Single HDR 4K at 150% DPI only |
| Epic 6 | 6.1, 6.2, 6.3, 6.4, 6.5, 6.6 | Windows CI-pass | Output pipeline; manual validation required per output-validation.md but not recorded |
| Epic 7 | 7.1, 7.2, 7.3, 7.4, 7.5 | Windows manual-pass | Epic 7 retro confirms Windows manual validation completed by Dana on 2026-05-26 |
| Epic 7 | 7.6 | Windows CI-pass | Technical debt cleanup; no hardware dependencies |
| Epic 8 | 8.1, 8.2, 8.3 | Windows CI-pass | HDR state mapping, alerts, diagnostics — all code/test work |
| Epic 8 | 8.4 | Windows CI-pass | This story — documentation/audit with diagnostic framework |
| Epic 8 | 8.5 | Windows CI-pass | Automated gates executed; release validation matrix created; manual scenarios catalogued as not-run |

## Existing Validation Documentation

| Document | Path | Coverage | Validation Level |
|----------|------|----------|-----------------|
| **MVP Release Validation Matrix** | `docs/validation/mvp-release-validation-matrix.md` | All FR/NFR — **authoritative release-readiness document** | Automated gates executed; 43 manual scenarios catalogued (not-run pending human tester) |
| Lifecycle Validation | `docs/validation/lifecycle-validation.md` | FR45, NFR5, NFR11 | Checklist defined; partial manual execution from Story 4.5 |
| Overlay Validation | `docs/validation/overlay-validation.md` | FR47, NFR3, NFR27 | Checklist defined; partial manual execution from Story 4.5 |
| Output Validation | `docs/validation/output-validation.md` | FR48, NFR8, NFR19 | Scope table defined; manual validation required but not executed |

## How to Use This Registry

1. **Before release claims**: Check that any capability mentioned in release copy has at least the required validation level. Consult `docs/validation/mvp-release-validation-matrix.md` for the authoritative release-readiness assessment.
2. **Before new stories**: Reference the gap list to identify validation work that should accompany implementation.
3. **After Windows manual validation**: Update the relevant row with evidence, date, tester, and device/display configuration. Also update the release validation matrix.
4. **For release readiness**: The release validation matrix (`docs/validation/mvp-release-validation-matrix.md`) is the authoritative document. This registry feeds into it.
