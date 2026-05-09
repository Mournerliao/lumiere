---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
includedFiles:
  prd: _bmad-output/planning-artifacts/prd.md
  architecture: _bmad-output/planning-artifacts/architecture.md
  epics: _bmad-output/planning-artifacts/epics.md
  ux: _bmad-output/planning-artifacts/ux-design.md
---

# Implementation Readiness Assessment Report

**Date:** 2026-05-09
**Project:** lumiere

## Document Inventory

### PRD Files Found

**Whole Documents:**
- `_bmad-output/planning-artifacts/prd.md` (49,040 bytes, modified 2026-05-09 17:15:15)

**Sharded Documents:**
- None found

### Architecture Files Found

**Whole Documents:**
- `_bmad-output/planning-artifacts/architecture.md` (43,265 bytes, modified 2026-05-09 17:15:16)

**Sharded Documents:**
- None found

### Epics & Stories Files Found

**Whole Documents:**
- `_bmad-output/planning-artifacts/epics.md` (71,531 bytes, modified 2026-05-09 17:23:45)

**Sharded Documents:**
- None found

### UX Design Files Found

**Whole Documents:**
- `_bmad-output/planning-artifacts/ux-design.md` (4,491 bytes, modified 2026-05-09 17:23:51)

**Sharded Documents:**
- None found

### Discovery Issues

- No duplicate whole/sharded document formats found.
- `project-context.md` was not found under the repository (workflow `persistent_facts`); optional AI/project context is missing unless restored elsewhere.

## PRD Analysis

### Functional Requirements

FR1: Users can start a fullscreen capture from the main window.

FR2: Users can start a region capture from the main window.

FR3: Users can start fullscreen and region capture through global shortcuts.

FR4: Users can start fullscreen and region capture from the system tray.

FR5: Users can keep Lumiere available through a background or tray-oriented workflow after leaving the main window.

FR6: Users can cancel an active capture flow and return to a recoverable idle state.

FR7: The system prevents conflicting capture sessions from running at the same time.

FR8: The system can recover from capture startup failure without leaving active capture resources or stranded overlay windows.

FR9: Users can see a concise HDR status summary from the main window.

FR10: Users can see a concise HDR status summary from the tray menu.

FR11: Users can distinguish HDR ready, HDR available but not enabled, HDR unavailable, degraded preview, unsupported capture, preview failed, and output completion or failure states.

FR12: Users can receive actionable HDR-related alerts when HDR is unavailable, degraded, unsupported, or failed.

FR13: Users can disable or enable HDR-related alerts in settings.

FR14: The system can represent capture and preview trust as typed states instead of treating all successful starts as trustworthy HDR capture.

FR15: Users can enter the default region capture flow without first choosing a target through a system picker.

FR16: Users can select a region by dragging over a fullscreen overlay.

FR17: Users can complete a valid region capture by releasing the pointer.

FR18: Users can cancel region capture with Escape or an available cancel path.

FR19: Users can attempt a new region selection after an invalid or too-small crop without producing output.

FR20: Users can distinguish active, invalid-region, completed, canceled, degraded, unsupported, and failed region-capture states through overlay or status feedback.

FR21: The overlay can remain interactive for crop input while displaying status and cancellation controls.

FR22: Users can choose whether captures output to clipboard, folder, or both.

FR23: Users can choose or change the save folder when file output is enabled.

FR24: Users can receive completion feedback that identifies which configured output targets succeeded.

FR25: Users can receive recoverable failure feedback that identifies which configured output target failed and whether retry or settings correction is needed.

FR26: Users can enable or disable timestamp-based file naming.

FR27: Users can enable or disable clipboard image output when clipboard output is part of the selected output target.

FR28: The system can apply output settings consistently across main window, tray, shortcut, fullscreen, and region capture flows.

FR29: The system can present export or color format options only where the product has defined implementation semantics for them.

FR30: Users can open settings from the main window.

FR31: Users can open settings from the tray menu.

FR32: Users can configure fullscreen capture and region capture shortcuts.

FR33: Users can restore or recover from invalid, conflicting, or unregistered shortcut choices.

FR34: Users can configure output target preferences.

FR35: Users can configure save path preferences.

FR36: Users can configure supported after-capture behavior for opening or revealing an output artifact when the selected output target produces one.

FR37: Users can view application name, version, and brief product description.

FR38: The system persists settings locally and reuses them across app launches.

FR39: Users can open the tray menu while Lumiere is running in the background.

FR40: Users can open the main Lumiere window from the tray.

FR41: Users can start capture commands from the tray without duplicating capture state.

FR42: Users can quit Lumiere from the tray.

FR43: The system releases capture, overlay, tray, hotkey, and graphics resources when quitting.

FR44: Developers can record validation level for each implemented capability as Mac edit, Windows CI-pass, or Windows manual-pass.

FR45: Developers can validate repeated capture lifecycle behavior across start, cancel, restart, failure, and output flows.

FR46: Developers can validate direct monitor capture without picker on Windows hardware.

FR47: Developers can validate overlay behavior across HDR/SDR displays, multi-monitor placement, and common DPI scaling values.

FR48: Developers can validate clipboard and file output behavior against configured settings.

FR49: The system can retain structured diagnostic context for capture, preview, output, and interop failures, including operation, stage, mapped user-facing state, and technical detail needed for engineering triage.

Total FRs: 49

### Non-Functional Requirements

NFR1: Capture entry responsiveness SHALL be validated on Windows reference hardware: elapsed time from user trigger through shortcut, tray, or main window to capture-active state SHALL be recorded at p50 and p95, and p95 SHALL NOT regress beyond the documented prior baseline without an explicit acceptance rationale.

NFR2: Region selection pointer feedback SHALL remain visually continuous during drag, resize, invalid-crop, and release-to-capture interactions on supported Windows hardware; validation SHALL record pass/fail across the DPI scales listed in the manual test plan.

NFR3: Overlay status, crop visuals, and completion feedback SHALL NOT resize, rescale, displace, or destabilize the HDR preview surface during a capture session; visual validation SHALL confirm stable preview framing while chrome updates.

NFR4: Clipboard or file output, including slow or failing writes, SHALL NOT leave the overlay, WGC session, or graphics resources active indefinitely; validation SHALL confirm the session returns to a defined idle or disposed state within a bounded timeout documented by the test plan.

NFR5: Repeated capture cycles across start, cancel, restart, release-to-output, and quit SHALL NOT produce monotonic growth beyond documented noise thresholds in selected resource indicators such as private bytes, handles, or GPU allocator trends; Windows validation SHALL compare baseline and post-cycle metrics across a defined cycle count.

NFR6: The primary capture and preview path SHALL preserve HDR-first invariants: FP16 WGC frames, FP16 DXGI swap-chain presentation, scRGB readiness evidence, and GPU-resident preview; review or automated checks SHALL verify configured formats and presentation path alignment.

NFR7: The authoritative live HDR preview SHALL NOT be replaced by `BitmapImage`, `SoftwareBitmap`, GDI, WIC, CPU bitmap readback, SDR texture fallback, or ordinary XAML bitmap `Image` presentation; exceptions SHALL be explicitly documented and SHALL NOT be marketed as HDR-equivalent preview.

NFR8: Clipboard or file output SHALL NOT be described as HDR-preserving unless a written record exists for that path covering format choice, conversion or metadata policy, target-app assumptions where relevant, and Windows manual validation results.

NFR9: Export or color-format options SHALL be hidden, disabled, or explicitly scoped when fidelity semantics are undefined; UI review SHALL confirm users cannot select options that imply validated HDR preservation without evidence.

NFR10: HDR readiness and trust states SHALL be backed by capability, preview, and output evidence per the product state model; degraded, unvalidated, unsupported, or failed states SHALL NOT use success or completed language.

NFR11: Capture cancellation, failure, restart, main-window close, and app quit SHALL deterministically dispose or hand off WGC session, frame pool, frames, swap chain, overlay, tray, hotkeys, and related native resources; Windows validation SHALL include teardown checks after each scenario class.

NFR12: Preview teardown SHALL detach presentation from the UI surface before releasing DXGI swap-chain resources; ordering SHALL be enforced by review and covered by targeted lifecycle tests or inspections where feasible.

NFR13: Capture callbacks, output completion handlers, diagnostics, and overlay updates SHALL be generation-scoped or equivalently session-token-scoped so stale async work cannot mutate UI or session state after a newer capture begins; automated tests SHALL cover stale completion rejection.

NFR14: Failed capture startup, failed direct monitor resolution, failed overlay creation, failed clipboard write, and failed file write SHALL leave the application in a recoverable idle state with explicit user-facing failure feedback; validation SHALL include scripted failure injections for each class.

NFR15: Ordinary stop or restart of capture SHALL NOT dispose the shared graphics device unless the application is shutting down or executing a documented device-loss recovery path; code review SHALL confirm capture recycling does not recreate the device per session by default.

NFR16: MVP operation SHALL be fully local: capture, preview, settings, and output SHALL NOT require account login, cloud upload, remote processing, telemetry collection endpoints, or general network availability; validation SHALL demonstrate core flows with network disconnected.

NFR17: Logs and diagnostics SHALL NOT include screenshot pixel data, raw frame dumps, or other screen content payloads; spot checks of generated logs during capture scenarios SHALL confirm absence of content payloads.

NFR18: File output SHALL respect the configured save location and SHALL surface permission, missing path, or write failures without silent drop; tests SHALL include invalid paths and permission-denied cases where practical.

NFR19: Clipboard output SHALL follow the user's configured output targets and SHALL accurately represent behavior under normal Windows clipboard semantics; settings UI SHALL NOT imply private vault storage beyond the OS clipboard model.

NFR20: Users SHALL have a reliable cancel path during capture, including keyboard Escape whenever the overlay can safely close; Windows manual validation SHALL verify cancel behavior for region capture and related flows.

NFR21: HDR, degraded, unsupported, failed, and completed states SHALL be distinguishable without relying on color alone; UX review SHALL validate text and/or icon discrimination using a rendered state inventory.

NFR22: Main window, tray, settings, and overlay controls SHALL use concise, native-feeling language during capture; primary capture surfaces SHALL NOT require reading long diagnostic paragraphs to understand next actions.

NFR23: Tray and global shortcut workflows SHALL support completing the default capture flows without opening the main window; journey validation SHALL include tray-only and shortcut-only happy paths.

NFR24: Settings SHALL NOT present options as fully supported capabilities when underlying semantics are absent; release QA SHALL cross-check controls against the implemented behavior matrix.

NFR25: The shipping product SHALL remain Windows-only and aligned to the approved desktop stack: `.NET 10`, `net10.0-windows10.0.19041.0` targeting minimum, x64, WinUI 3, Windows App SDK, WGC, D3D11, DXGI, and WinRT/COM interop; release packaging metadata SHALL match these constraints.

NFR26: Tray, hotkeys, monitor-targeted capture, overlay windowing, clipboard, and picker integrations SHALL keep raw HWND, HMONITOR, COM, and DXGI ownership inside narrow platform boundary layers; UI orchestration SHALL depend on facades or interfaces rather than owning native lifetimes directly.

NFR27: Release claims about multi-monitor placement, HDR/SDR mixed setups, common DPI scaling values, fullscreen or disruptive cases, and display topology changes SHALL be supported by recorded Windows manual validation against an explicit scenario list; gaps SHALL be documented as limitations rather than implied guarantees.

NFR28: MVP SHALL NOT take architectural dependencies on web UI stacks, Electron/Tauri shells, cross-platform UI frameworks, cloud sync services, gallery or annotation suites, or SDR-first screenshot libraries called out as out of scope; dependency review SHALL be part of release readiness.

NFR29: The codebase SHALL preserve strict separation of concerns among application shell and workflow orchestration, capture session lifecycle, graphics and presentation, overlay interaction, native interop and diagnostics, and local settings persistence, such that UI layers do not directly own WGC, D3D, DXGI, COM resource lifetimes, or low-level monitor handles.

NFR30: Platform interop failures SHALL be diagnosable with structured context including operation, stage, mapped user-facing status, and technical detail sufficient for engineering triage; sampling of failure logs SHALL confirm required fields are populated for representative failures.

NFR31: HDR constants and readiness mapping SHALL have a single authoritative source of truth and SHALL be protected by automated tests; changes to constants or mapping SHALL update tests or fail the automated gate.

NFR32: The Windows integration pipeline SHALL execute the repository's agreed automated quality gates end-to-end without unapproved waivers; mainline health SHALL be defined as passing those gates, with the exact gate set and runner configuration documented outside this PRD.

NFR33: Behavior that cannot be proven in non-hardware automation, including real HDR displays, WGC timing, tray/global hotkeys, and multi-monitor geometry, SHALL carry an explicit validation level in implementation records; public-facing HDR and display fidelity claims SHALL only reference Windows hardware-level validation evidence.

Total NFRs: 33

### Additional Requirements

- Product-claim discipline: the app must not claim HDR preservation, HDR readiness, or output fidelity unless Windows manual validation evidence exists for the relevant path.
- Windows-only MVP targeting native Windows APIs: WinUI 3, Windows App SDK, Windows Graphics Capture, Direct3D 11, DXGI, WinRT/COM interop, x64 .NET.
- Primary capture and preview path must preserve FP16 WGC frames, FP16 DXGI swap chain, scRGB color space, GPU-resident preview, typed readiness states, and no silent SDR bitmap preview fallback.
- WGC, D3D11, DXGI, WinRT, COM, HWND, HMONITOR, tray, and global hotkey details must stay behind narrow module boundaries.
- Clipboard/file output requires explicit fidelity semantics; basic clipboard bitmap usability must not be described as HDR-preserving without concrete format, conversion policy, metadata strategy, compatibility checks, and Windows manual validation.
- Settings must be persisted locally and consumed by main window, tray, hotkeys, output pipeline, and HDR alert behavior.
- MVP must be fully local/offline with no account login, cloud upload, remote processing, telemetry dependency, or network requirement.
- Existing Epic 1-3 implementation and validation artifacts must be preserved as historical foundation; updated MVP implementation planning begins from Epic 4.
- Out-of-scope for MVP: gallery, annotation-heavy editing, onboarding, advanced export workflows, history, full auto-update system, installer/signing/release channel unless needed for validation or early distribution.

### PRD Completeness Assessment

The PRD is requirement-rich and explicit about product scope, platform constraints, MVP boundaries, technical invariants, output-claim discipline, validation levels, and module ownership. FR/NFR coverage is well numbered and test-oriented. `ux-design.md` now includes table-style state guidance for main panel, settings, tray, overlay, and status/copy inventory aligned to NFR21/NFR24, but it is still not a pixel-level or full interaction specification; validation should continue to tie back to PRD journeys, `UX-DR1`–`UX-DR20`, architecture, and `harness/design/v0-mvp-reference`.

## Epic Coverage Validation

### Epic FR Coverage Extracted

FR1: Covered in Epic 5.
FR2: Covered in Epic 5.
FR3: Covered in Epic 7.
FR4: Covered in Epic 7.
FR5: Covered in Epic 7.
FR6: Covered in Epic 4.
FR7: Covered in Epic 4.
FR8: Covered in Epic 4.
FR9: Covered in Epic 5.
FR10: Covered in Epic 7.
FR11: Covered in Epic 8.
FR12: Covered in Epic 8.
FR13: Covered in Epic 5.
FR14: Covered in Epic 8.
FR15: Covered in Epic 4.
FR16: Covered in Epic 4.
FR17: Covered in Epic 4.
FR18: Covered in Epic 4.
FR19: Covered in Epic 4.
FR20: Covered in Epic 8.
FR21: Covered in Epic 4.
FR22: Covered in Epic 6.
FR23: Covered in Epic 6.
FR24: Covered in Epic 6.
FR25: Covered in Epic 6.
FR26: Covered in Epic 6.
FR27: Covered in Epic 6.
FR28: Covered in Epic 6.
FR29: Covered in Epic 6.
FR30: Covered in Epic 5.
FR31: Covered in Epic 7.
FR32: Covered in Epic 5.
FR33: Covered in Epic 7.
FR34: Covered in Epic 5.
FR35: Covered in Epic 5.
FR36: Covered in Epic 6.
FR37: Covered in Epic 5.
FR38: Covered in Epic 5.
FR39: Covered in Epic 7.
FR40: Covered in Epic 7.
FR41: Covered in Epic 7.
FR42: Covered in Epic 7.
FR43: Covered in Epic 7.
FR44: Covered in Epic 8.
FR45: Covered in Epic 8.
FR46: Covered in Epic 4.
FR47: Covered in Epic 4.
FR48: Covered in Epic 6.
FR49: Covered in Epic 8.

Total FRs in epics: 49

### Coverage Matrix

| FR Number | PRD Requirement | Epic Coverage | Status |
| --------- | --------------- | ------------- | ------ |
| FR1 | Users can start a fullscreen capture from the main window. | Epic 5 | Covered |
| FR2 | Users can start a region capture from the main window. | Epic 5 | Covered |
| FR3 | Users can start fullscreen and region capture through global shortcuts. | Epic 7 | Covered |
| FR4 | Users can start fullscreen and region capture from the system tray. | Epic 7 | Covered |
| FR5 | Users can keep Lumiere available through a background or tray-oriented workflow after leaving the main window. | Epic 7 | Covered |
| FR6 | Users can cancel an active capture flow and return to a recoverable idle state. | Epic 4 | Covered |
| FR7 | The system prevents conflicting capture sessions from running at the same time. | Epic 4 | Covered |
| FR8 | The system can recover from capture startup failure without leaving active capture resources or stranded overlay windows. | Epic 4 | Covered |
| FR9 | Users can see a concise HDR status summary from the main window. | Epic 5 | Covered |
| FR10 | Users can see a concise HDR status summary from the tray menu. | Epic 7 | Covered |
| FR11 | Users can distinguish HDR ready, HDR available but not enabled, HDR unavailable, degraded preview, unsupported capture, preview failed, and output completion or failure states. | Epic 8 | Covered |
| FR12 | Users can receive actionable HDR-related alerts when HDR is unavailable, degraded, unsupported, or failed. | Epic 8 | Covered |
| FR13 | Users can disable or enable HDR-related alerts in settings. | Epic 5 | Covered |
| FR14 | The system can represent capture and preview trust as typed states instead of treating all successful starts as trustworthy HDR capture. | Epic 8 | Covered |
| FR15 | Users can enter the default region capture flow without first choosing a target through a system picker. | Epic 4 | Covered |
| FR16 | Users can select a region by dragging over a fullscreen overlay. | Epic 4 | Covered |
| FR17 | Users can complete a valid region capture by releasing the pointer. | Epic 4 | Covered |
| FR18 | Users can cancel region capture with Escape or an available cancel path. | Epic 4 | Covered |
| FR19 | Users can attempt a new region selection after an invalid or too-small crop without producing output. | Epic 4 | Covered |
| FR20 | Users can distinguish active, invalid-region, completed, canceled, degraded, unsupported, and failed region-capture states through overlay or status feedback. | Epic 8 | Covered |
| FR21 | The overlay can remain interactive for crop input while displaying status and cancellation controls. | Epic 4 | Covered |
| FR22 | Users can choose whether captures output to clipboard, folder, or both. | Epic 6 | Covered |
| FR23 | Users can choose or change the save folder when file output is enabled. | Epic 6 | Covered |
| FR24 | Users can receive completion feedback that identifies which configured output targets succeeded. | Epic 6 | Covered |
| FR25 | Users can receive recoverable failure feedback that identifies which configured output target failed and whether retry or settings correction is needed. | Epic 6 | Covered |
| FR26 | Users can enable or disable timestamp-based file naming. | Epic 6 | Covered |
| FR27 | Users can enable or disable clipboard image output when clipboard output is part of the selected output target. | Epic 6 | Covered |
| FR28 | The system can apply output settings consistently across main window, tray, shortcut, fullscreen, and region capture flows. | Epic 6 | Covered |
| FR29 | The system can present export or color format options only where the product has defined implementation semantics for them. | Epic 6 | Covered |
| FR30 | Users can open settings from the main window. | Epic 5 | Covered |
| FR31 | Users can open settings from the tray menu. | Epic 7 | Covered |
| FR32 | Users can configure fullscreen capture and region capture shortcuts. | Epic 5 | Covered |
| FR33 | Users can restore or recover from invalid, conflicting, or unregistered shortcut choices. | Epic 7 | Covered |
| FR34 | Users can configure output target preferences. | Epic 5 | Covered |
| FR35 | Users can configure save path preferences. | Epic 5 | Covered |
| FR36 | Users can configure supported after-capture behavior for opening or revealing an output artifact when the selected output target produces one. | Epic 6 | Covered |
| FR37 | Users can view application name, version, and brief product description. | Epic 5 | Covered |
| FR38 | The system persists settings locally and reuses them across app launches. | Epic 5 | Covered |
| FR39 | Users can open the tray menu while Lumiere is running in the background. | Epic 7 | Covered |
| FR40 | Users can open the main Lumiere window from the tray. | Epic 7 | Covered |
| FR41 | Users can start capture commands from the tray without duplicating capture state. | Epic 7 | Covered |
| FR42 | Users can quit Lumiere from the tray. | Epic 7 | Covered |
| FR43 | The system releases capture, overlay, tray, hotkey, and graphics resources when quitting. | Epic 7 | Covered |
| FR44 | Developers can record validation level for each implemented capability as Mac edit, Windows CI-pass, or Windows manual-pass. | Epic 8 | Covered |
| FR45 | Developers can validate repeated capture lifecycle behavior across start, cancel, restart, failure, and output flows. | Epic 8 | Covered |
| FR46 | Developers can validate direct monitor capture without picker on Windows hardware. | Epic 4 | Covered |
| FR47 | Developers can validate overlay behavior across HDR/SDR displays, multi-monitor placement, and common DPI scaling values. | Epic 4 | Covered |
| FR48 | Developers can validate clipboard and file output behavior against configured settings. | Epic 6 | Covered |
| FR49 | The system can retain structured diagnostic context for capture, preview, output, and interop failures, including operation, stage, mapped user-facing state, and technical detail needed for engineering triage. | Epic 8 | Covered |

### Missing Requirements

No missing PRD FR coverage found. All PRD FR1-FR49 are explicitly mapped in the epics document.

No FR numbers were found in the epics coverage map that are absent from the PRD.

### Coverage Statistics

- Total PRD FRs: 49
- FRs covered in epics: 49
- Coverage percentage: 100%

## UX Alignment Assessment

### UX Document Status

Found: `_bmad-output/planning-artifacts/ux-design.md` (MVP UX specification — structured outline with state tables).

The document anchors to PRD journeys, NFR21/NFR22/NFR24, `UX-DR1`–`UX-DR20`, architecture guidance for WinUI/Fluent, and `harness/design/v0-mvp-reference` as reference-only. It adds Markdown tables for main panel modes, settings pending vs active (Epic 6/7), tray states, overlay phases, and a status/copy inventory with non-color cue notes.

### Alignment Issues

- **Residual depth gap:** Tables define states and roles but not layout density, exact Fluent control types, or full micro-copy strings for every edge case. Implementation detail still flows from PRD, `UX-DR` rows, architecture, and the v0 reference prototype.
- **PRD ↔ UX:** Surfaces and trust vocabulary align with PRD scope; no direct contradiction observed.
- **Architecture ↔ UX:** Module boundaries support the named surfaces; lifecycle and performance NFRs are consistent with the UX tables.

### Warnings

- Warning: NFR21 is best closed with a **rendered** state walk (screenshots or manual checklist) per surface—not only the markdown tables.
- Warning: `project-context.md` remains absent repo-wide; rely on `AGENTS.md`, `harness/README.md`, and architecture unless restored.

## Epic Quality Review

### Critical Violations

#### Historical Epics Are Technical/Traceability Containers, Not User-Value Epics

Epic 1, Epic 2, and Epic 3 are titled and described as historical foundation preservation:

- Epic 1: Historical HDR Preview Foundation
- Epic 2: Historical Direct Capture Lifecycle
- Epic 3: Historical Region Overlay Release-to-Capture

These epics are useful for brownfield traceability, but under strict create-epics-and-stories standards they are not clean executable MVP epics. They preserve or classify prior technical foundation rather than delivering new standalone user value in the current implementation phase.

Impact:

- If treated as active Phase 4 implementation epics, they violate the "epics deliver user value, not technical milestones" standard.
- They also blur implementation readiness because "retained historical foundation" stories are not sized like normal work items and may not represent independently completable future work.

Recommendation:

- Keep Epic 1-3 as a clearly separate "Historical Foundation / Baseline Traceability" appendix, not as active implementation epics.
- Ensure sprint execution starts from Epic 4, as the planning constraints already state.
- In sprint status and story creation, mark Epic 1-3 as retained/baseline evidence only, not backlog work.

### Major Issues

#### Epic 4 Banner vs FR Coverage Map — Remediated

Epic 4’s active MVP `**FRs covered:**` line in `epics.md` now matches the **FR Coverage Map** (FR6, FR7, FR8, FR15, FR16, FR17, FR18, FR19, FR21, FR46, FR47). No further banner/map conflict was found in this pass.

#### Epic 5 Forward Dependencies — Mitigated in Stories; Review Discipline Still Required

Stories **5.3** and **5.4** include explicit Given/When/Then clauses that disable, hide, read-only scope, or label pending behavior until Epic 7 (hotkeys) and Epic 6 (output) land. Residual risk is **process**: accepting Epic 5 without enforcing those ACs could still ship misleading controls.

Recommendation: Keep Epic 5 review checklists tied to those ACs; do not mark stories done if controls imply active hotkeys or configured output before the consuming epics exist.

#### Epic 8 Is a Release Gate More Than an Independent Product Epic

Epic 8 covers trust, diagnostics, validation levels, and release validation. It has strong user/developer value, but it depends on most prior MVP surfaces existing before its release validation matrix can be meaningful.

Impact:

- Epic 8 is acceptable as a final hardening/release-readiness epic, but it should not be described as independently functional in the same way as feature epics.
- Story 8.5 cannot be completed until main window, settings, output, tray, hotkeys, overlay, direct capture, and HDR trust states exist.

Recommendation:

- Label Epic 8 as "release validation and trust hardening" and make its dependency on prior MVP surfaces explicit.
- Keep Story 8.5 as a release gate/checklist story, not as a feature story expected to stand alone.

### Minor Concerns

#### Acceptance Criteria Are Mostly BDD/Testable, With Some Language Needing Tightening

Most stories use clear Given/When/Then acceptance criteria. A few criteria still rely on interpretive language:

- "safe defaults"
- "concise actionable feedback"
- "native-feeling language"
- "compact native WinUI layout"
- "safe shortcut state"

Impact:

- These are not blockers, but they can create ambiguous implementation and review outcomes unless tied to concrete state inventories, copy guidelines, or validation records.

Recommendation:

- Define "safe defaults," "actionable feedback," "native-feeling language," and "compact layout" through a UI state inventory, UX reference checklist, or story-specific examples.
- Tie copy/state criteria to `UX-DR` requirements and NFR21/NFR22.

#### UX Specification Depth

`ux-design.md` now includes state tables and non-color cue guidance; `UX-DR1` through `UX-DR20` remain in `epics.md`.

Impact:

- Authors have stronger planning UX signal, but reviewers still benefit from a rendered walkthrough (screenshots) for NFR21 and for exact copy under stress paths.

Recommendation:

- Add story-level or appendix screenshots for critical states (HDR degraded, output partial failure, pending settings) when approaching Epic 5–8 acceptance.

### Dependency Analysis

- Epic 1-3: Not valid as normal forward-moving user-value epics, but acceptable as retained historical traceability if excluded from active execution.
- Epic 4: Good brownfield transition epic. It intentionally depends on historical Epic 1-3 assets, which matches the rebaseline constraint and does not reference future epics as prerequisites.
- Epic 5: Forward-dependency risk is **mitigated by Story 5.3/5.4 ACs**; enforcement remains a review concern.
- Epic 6: Depends on settings and capture foundation from prior epics; no forbidden forward dependency found.
- Epic 7: Depends on settings and shared capture/session routing from prior epics; no forbidden forward dependency found.
- Epic 8: Depends on prior MVP surfaces as a release validation/hardening epic; acceptable if explicitly treated as release gate, not standalone feature delivery.

No database/entity creation concerns apply to this Windows desktop app.

No starter-template violation found. The architecture and epics correctly state this is brownfield and must preserve the existing native WinUI scaffold rather than rerunning or replacing a starter template.

### Best Practices Compliance Checklist

| Epic | Delivers User Value | Independent | Stories Sized | No Forward Dependencies | Clear ACs | FR Traceability |
| ---- | ------------------- | ----------- | ------------- | ----------------------- | --------- | --------------- |
| Epic 1 | Concern | Concern | Concern | Pass | Pass | Pass |
| Epic 2 | Concern | Concern | Concern | Pass | Pass | Pass |
| Epic 3 | Concern | Concern | Concern | Pass | Pass | Pass |
| Epic 4 | Pass | Pass | Pass | Pass | Pass | Pass |
| Epic 5 | Pass | Concern | Pass | Pass | Minor Concern | Pass |
| Epic 6 | Pass | Pass | Pass | Pass | Pass | Pass |
| Epic 7 | Pass | Pass | Pass | Pass | Pass | Pass |
| Epic 8 | Pass | Concern | Pass | Pass | Minor Concern | Pass |

### Quality Review Summary

The epic set is strong on traceability and requirement coverage. Remaining quality emphasis:

- Epic 1-3 should stay historical baseline traceability, not active backlog.
- Epic 5 depends on disciplined acceptance of pending/disabled settings ACs until Epic 6/7.
- Epic 8 should be framed as release validation/hardening with explicit prior-surface dependencies.

## Summary and Recommendations

### Overall Readiness Status

NEEDS WORK — **narrowed**

The planning set is strong on requirements: PRD FR/NFR text is explicit, NFRs are test-oriented, the **FR Coverage Map** covers all PRD FR1–FR49 (100% in this matrix), and Epic 4’s epic-level `**FRs covered:**` banner now matches that map. Remaining gaps are **process and evidence**: keep historical epics out of active backlog, enforce Epic 5 pending-settings ACs on review, add rendered evidence for NFR21 as UI lands, treat Epic 8 as a release gate, and optionally restore `project-context.md`.

### Critical Issues Requiring Immediate Action

1. **Historical Epic 1–3** must stay **out of active Phase 4 backlog**—retain as traceability/baseline only.

2. **Epic 5 review discipline:** Do not accept settings stories unless pending/disabled/read-only rules in 5.3/5.4 are demonstrably satisfied in builds.

3. **NFR21 evidence:** Plan screenshots or a manual rendered-state checklist in addition to `ux-design.md` tables before declaring UI complete for trust states.

### Recommended Next Steps

1. Keep Epic 1–3 explicitly historical in sprint tooling and `sprint-status.yaml` (if not already).

2. Gate Epic 5 QA on Story 5.3/5.4 ACs whenever Epic 6/7 are not yet done.

3. Add optional `project-context.md` (or equivalent) for agent/workflow consistency.

4. Label Epic 8 as trust/validation/release hardening; keep checklist stories as gates.

5. Tie vague AC phrases to examples or checklists where stories still use interpretive wording.

### Final Note

This pass refreshed document inventory (including larger `ux-design.md` and updated `epics.md` mtimes), revalidated PRD FR/NFR extraction (49 / 33), confirmed 100% FR coverage, and verified Epic 4 banner/map alignment. Remaining readiness work is lighter than the prior assessment: mostly enforcement, rendered UX evidence, and optional project context restoration.

**Assessor:** Cursor Agent (BMad Implementation Readiness workflow)  
**Assessment Date:** 2026-05-09
