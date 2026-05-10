---
stepsCompleted:
  - step-01-requirements-extracted
  - step-02-epics-approved
  - step-03-stories-generated
  - step-04-final-validation
inputDocuments:
  - _bmad-output/planning-artifacts/prd.md
  - _bmad-output/planning-artifacts/architecture.md
  - harness/planning/project-plan.md
  - harness/planning/mvp-feature-list.md
  - harness/design/v0-mvp-reference/README.md
  - harness/design/v0-mvp-reference/app/page.tsx
  - harness/design/v0-mvp-reference/components/lumiere/app-shell.tsx
  - harness/design/v0-mvp-reference/components/lumiere/main-panel.tsx
  - harness/design/v0-mvp-reference/components/lumiere/settings-panel.tsx
  - harness/design/v0-mvp-reference/components/lumiere/tray-context-menu.tsx
  - harness/design/v0-mvp-reference/components/lumiere/prototype-state.ts
planningConstraints:
  - Preserve Epic 1-3 code implementation and validation documents as historical foundation work from the pre-MVP-rebaseline route.
  - When recreating epics for the updated MVP route, keep Epic 1-3 and begin rework or continued implementation from Epic 4.
---

# lumiere - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for lumiere, decomposing the requirements from the PRD, UX Design if it exists, and Architecture requirements into implementable stories.

## Requirements Inventory

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

### NonFunctional Requirements

NFR1: Capture entry responsiveness SHALL be validated on Windows reference hardware with p50 and p95 trigger-to-capture-active timings, and p95 SHALL NOT regress beyond the documented prior baseline without explicit acceptance rationale.
NFR2: Region selection pointer feedback SHALL remain visually continuous during drag, resize, invalid-crop, and release-to-capture interactions on supported Windows hardware, with pass/fail validation across the manual test plan DPI scales.
NFR3: Overlay status, crop visuals, and completion feedback SHALL NOT resize, rescale, displace, or destabilize the HDR preview surface during a capture session.
NFR4: Clipboard or file output, including slow or failing writes, SHALL NOT leave the overlay, WGC session, or graphics resources active indefinitely; validation SHALL confirm return to a defined idle or disposed state within a bounded timeout.
NFR5: Repeated capture cycles across start, cancel, restart, release-to-output, and quit SHALL NOT produce monotonic growth beyond documented noise thresholds in selected resource indicators.
NFR6: The primary capture and preview path SHALL preserve HDR-first invariants: FP16 WGC frames, FP16 DXGI swap-chain presentation, scRGB readiness evidence, and GPU-resident preview.
NFR7: The authoritative live HDR preview SHALL NOT be replaced by BitmapImage, SoftwareBitmap, GDI, WIC, CPU bitmap readback, SDR texture fallback, or ordinary XAML bitmap Image presentation.
NFR8: Clipboard or file output SHALL NOT be described as HDR-preserving unless a written record exists covering format choice, conversion or metadata policy, target-app assumptions, and Windows manual validation results.
NFR9: Export or color-format options SHALL be hidden, disabled, or explicitly scoped when fidelity semantics are undefined.
NFR10: HDR readiness and trust states SHALL be backed by capability, preview, and output evidence; degraded, unvalidated, unsupported, or failed states SHALL NOT use success or completed language.
NFR11: Capture cancellation, failure, restart, main-window close, and app quit SHALL deterministically dispose or hand off WGC session, frame pool, frames, swap chain, overlay, tray, hotkeys, and related native resources.
NFR12: Preview teardown SHALL detach presentation from the UI surface before releasing DXGI swap-chain resources.
NFR13: Capture callbacks, output completion handlers, diagnostics, and overlay updates SHALL be generation-scoped or equivalently session-token-scoped so stale async work cannot mutate UI or session state after a newer capture begins.
NFR14: Failed capture startup, failed direct monitor resolution, failed overlay creation, failed clipboard write, and failed file write SHALL leave the application in a recoverable idle state with explicit user-facing failure feedback.
NFR15: Ordinary stop or restart of capture SHALL NOT dispose the shared graphics device unless the application is shutting down or executing a documented device-loss recovery path.
NFR16: MVP operation SHALL be fully local: capture, preview, settings, and output SHALL NOT require account login, cloud upload, remote processing, telemetry collection endpoints, or network availability.
NFR17: Logs and diagnostics SHALL NOT include screenshot pixel data, raw frame dumps, or other screen content payloads.
NFR18: File output SHALL respect the configured save location and SHALL surface permission, missing path, or write failures without silent drop.
NFR19: Clipboard output SHALL follow the user's configured output targets and SHALL accurately represent behavior under normal Windows clipboard semantics.
NFR20: Users SHALL have a reliable cancel path during capture, including keyboard Escape whenever the overlay can safely close.
NFR21: HDR, degraded, unsupported, failed, and completed states SHALL be distinguishable without relying on color alone.
NFR22: Main window, tray, settings, and overlay controls SHALL use concise, native-feeling language during capture.
NFR23: Tray and global shortcut workflows SHALL support completing the default capture flows without opening the main window.
NFR24: Settings SHALL NOT present options as fully supported capabilities when underlying semantics are absent.
NFR25: The shipping product SHALL remain Windows-only and aligned to .NET 10, net10.0-windows10.0.19041.0 minimum, x64, WinUI 3, Windows App SDK, WGC, D3D11, DXGI, and WinRT/COM interop.
NFR26: Tray, hotkeys, monitor-targeted capture, overlay windowing, clipboard, and picker integrations SHALL keep raw HWND, HMONITOR, COM, and DXGI ownership inside narrow platform boundary layers.
NFR27: Release claims about multi-monitor placement, HDR/SDR mixed setups, common DPI scaling values, fullscreen or disruptive cases, and display topology changes SHALL be supported by recorded Windows manual validation.
NFR28: MVP SHALL NOT take architectural dependencies on web UI stacks, Electron/Tauri shells, cross-platform UI frameworks, cloud sync services, gallery or annotation suites, or SDR-first screenshot libraries called out as out of scope.
NFR29: The codebase SHALL preserve strict separation of concerns among application shell, capture session lifecycle, graphics and presentation, overlay interaction, native interop and diagnostics, and local settings persistence.
NFR30: Platform interop failures SHALL be diagnosable with structured context including operation, stage, mapped user-facing status, and technical detail sufficient for engineering triage.
NFR31: HDR constants and readiness mapping SHALL have a single authoritative source of truth and SHALL be protected by automated tests.
NFR32: The Windows integration pipeline SHALL execute the repository's agreed automated quality gates end-to-end without unapproved waivers.
NFR33: Behavior that cannot be proven in non-hardware automation, including real HDR displays, WGC timing, tray/global hotkeys, and multi-monitor geometry, SHALL carry an explicit validation level in implementation records.

### Additional Requirements

- Use the existing brownfield native WinUI 3 solution scaffold as the starter; do not re-run or replace the starter template.
- Preserve Epic 1-3 as historical foundation and begin the rebaselined MVP implementation plan from Epic 4.
- Keep production implementation Windows-only on .NET 10, net10.0-windows10.0.19041.0, x64, WinUI 3, Windows App SDK, WGC, D3D11, DXGI, Vortice, WinRT/COM, and Win32 interop where required.
- Treat harness/design/v0-mvp-reference as UX reference only; do not introduce React, Tailwind, shadcn, Radix, Electron, Tauri, or web UI dependencies into production code.
- Use direct monitor capture through monitor-targeted WGC interop as the default MVP path; picker behavior may remain fallback or debug only.
- Treat current clipboard output as basic bitmap usability, not HDR-preserving output, until output semantics and Windows validation evidence prove otherwise.
- Keep all WGC, D3D11, DXGI, WinRT, COM, HWND, HMONITOR, tray, hotkey, clipboard, and file/folder picker details inside narrow platform boundaries.
- Keep one shared capture/session state model across main window, overlay, tray, hotkeys, settings, and output.
- Use typed result objects, state enums, events, and immutable payloads instead of unstructured tuples, magic strings, or duplicated status vocabularies.
- Keep local settings persistence in Lumiere.Settings, covering shortcut preferences, output target, save path, timestamp naming, clipboard image option, HDR alert preference, and version/about metadata.
- Do not introduce a database, account system, REST/GraphQL/local HTTP API, cloud upload, telemetry dependency, or remote processing for MVP.
- Map native interop failures to structured diagnostics with operation, stage, user-facing status, technical detail, and optional session/correlation identity; never include captured pixels or frame dumps.
- Generation-scope capture callbacks, output completion handlers, diagnostics, and overlay updates so stale async work cannot mutate active UI or session state.
- Automated gates include restore, build, graphics tests, overlay tests, and format verification; real WGC/DXGI/HDR/tray/hotkey/multi-monitor/DPI behavior requires Windows manual validation.
- Packaging, signing, installer, auto-update, advanced diagnostics UI, advanced export profiles, gallery, history, annotation, onboarding, and editor-like workflows remain deferred unless explicitly pulled back into MVP scope.

### UX Design Requirements

UX-DR1: The main window must present Lumiere branding, a settings entry, fullscreen capture, region capture, current shortcut labels, HDR status summary, and minimize/background intent in a compact native WinUI/Fluent surface.
UX-DR2: Fullscreen and region capture buttons must show capture-in-progress state, prevent duplicate trigger while capture is active, and use lifecycle-driven status rather than a fixed simulated delay.
UX-DR3: Region capture must support the v0 release-to-capture intent: a valid drag release submits capture/output directly, while invalid or too-small selection remains recoverable and produces no output.
UX-DR4: The default capture UX must avoid a picker-first interruption; direct monitor or current-display capture is the primary MVP flow.
UX-DR5: HDR status must be visible in the main panel with concise, actionable language and icon/text discrimination that does not rely on color alone.
UX-DR6: The tray menu must show Lumiere identity, HDR status, fullscreen capture, region capture, shortcut labels, open main window, open settings, and quit commands in a compact command-first shape.
UX-DR7: Tray capture commands must mirror main-window availability and disabled/active state so tray, shortcuts, and main window cannot start conflicting sessions.
UX-DR8: Settings must include separate configurable shortcuts for fullscreen capture and region capture.
UX-DR9: Shortcut editing must handle invalid combinations, conflicts, registration failure, and recovery/default behavior in the native implementation.
UX-DR10: Settings must include an HDR alerts preference that governs user-facing prompts when HDR is unavailable, degraded, unsupported, or failed.
UX-DR11: Export/color format choices shown in the v0 reference, such as HDR10, P3, and sRGB, must be hidden, disabled, or validation-scoped until real encoding, metadata, conversion policy, and Windows validation exist.
UX-DR12: Settings must provide output destination selection for clipboard, folder, or both, and every capture entry point must obey the same persisted output preference.
UX-DR13: When folder output is enabled, settings must expose save path selection through native Windows UI and surface missing path, permission, or write failures clearly.
UX-DR14: Settings must include supported after-capture behavior only for output targets that produce an artifact that can be opened or revealed.
UX-DR15: Settings must include timestamp naming preference and map it to a deterministic filename policy that avoids overwriting existing files.
UX-DR16: Clipboard settings must include copy-as-image behavior when clipboard output is enabled, while keeping clipboard image usability separate from HDR-preserving claims.
UX-DR17: Settings must include about/version information sourced from app/build metadata rather than fragile hardcoded copy.
UX-DR18: Main panel, tray, settings, hotkeys, output, and HDR status must share one settings/state source rather than separate UI-local state.
UX-DR19: The native implementation must translate prototype layout, density, information hierarchy, and wording intent into WinUI/Fluent patterns without copying web component implementation.
UX-DR20: The capture experience must remain low-interruption and exclude onboarding, gallery/history, annotation-heavy overlay, and extended export wizard behavior from MVP unless a later story explicitly reintroduces them.

### FR Coverage Map

FR1: Epic 5 - Native v0 main window provides fullscreen capture entry.
FR2: Epic 5 - Native v0 main window provides region capture entry.
FR3: Epic 7 - Global hotkeys start fullscreen and region capture.
FR4: Epic 7 - Tray commands start fullscreen and region capture.
FR5: Epic 7 - Background/tray workflow keeps Lumiere available after leaving the main window.
FR6: Epic 4 - Transition cutover preserves cancel behavior and recoverable idle state from the existing lifecycle.
FR7: Epic 4 - Transition cutover preserves the single-session guard and prevents conflicting sessions.
FR8: Epic 4 - Transition cutover verifies startup failure recovery and no stranded resources.
FR9: Epic 5 - Main window exposes concise HDR status summary in the v0-aligned surface.
FR10: Epic 7 - Tray menu exposes concise HDR status summary.
FR11: Epic 8 - HDR trust model distinguishes ready, available-not-enabled, unavailable, degraded, unsupported, failed, completed, and output-failed states.
FR12: Epic 8 - HDR alerts provide actionable feedback for unavailable, degraded, unsupported, and failed states.
FR13: Epic 5 - Settings expose the HDR alert preference.
FR14: Epic 8 - Capture and preview trust are represented as typed evidence-backed states.
FR15: Epic 4 - Transition cutover keeps direct monitor capture as the default no-picker MVP path.
FR16: Epic 4 - Transition cutover preserves drag-to-select overlay region selection.
FR17: Epic 4 - Transition cutover preserves release-to-capture for valid regions.
FR18: Epic 4 - Transition cutover preserves Escape and cancel paths.
FR19: Epic 4 - Transition cutover preserves invalid or too-small crop recovery without output.
FR20: Epic 4 - Transition cutover fixes invalid-crop-stays-active behavior; Epic 8 - Overlay and status feedback use the approved state vocabulary for active, invalid, completed, canceled, degraded, unsupported, and failed states.
FR21: Epic 4 - Transition cutover preserves interactive overlay crop input while status and cancel affordances remain available.
FR22: Epic 6 - Output settings support clipboard, folder, or both.
FR23: Epic 6 - Folder output includes save folder selection and change behavior.
FR24: Epic 4 - Transition cutover adds "Copied to clipboard" completion feedback; Epic 6 - Completion feedback identifies which configured output targets succeeded.
FR25: Epic 6 - Recoverable failure feedback identifies which output target failed and what correction is needed.
FR26: Epic 6 - Timestamp naming preference is implemented in output behavior.
FR27: Epic 6 - Clipboard image output preference controls clipboard output behavior.
FR28: Epic 6 - Output settings apply consistently across main window, tray, shortcut, fullscreen, and region flows.
FR29: Epic 6 - Export and color format options are only exposed where implementation semantics exist.
FR30: Epic 5 - Settings can be opened from the main window.
FR31: Epic 7 - Settings can be opened from the tray menu.
FR32: Epic 5 - Settings expose fullscreen and region shortcut configuration.
FR33: Epic 7 - Hotkey registration handles invalid, conflicting, or unregistered shortcut choices and recovery.
FR34: Epic 5 - Settings expose output target preferences.
FR35: Epic 5 - Settings expose save path preferences.
FR36: Epic 6 - After-capture behavior is implemented only for output targets with openable or revealable artifacts.
FR37: Epic 5 - Settings show application name, version, and product description.
FR38: Epic 5 - Settings persist locally and are reused across launches.
FR39: Epic 7 - Tray menu remains available while Lumiere runs in the background.
FR40: Epic 7 - Tray can open the main Lumiere window.
FR41: Epic 7 - Tray capture commands route through the shared capture state.
FR42: Epic 7 - Tray can quit Lumiere.
FR43: Epic 7 - Quit releases capture, overlay, tray, hotkey, and graphics resources.
FR44: Epic 8 - Implementation records carry validation level: Mac edit, Windows CI-pass, or Windows manual-pass.
FR45: Epic 8 - Repeated lifecycle validation covers start, cancel, restart, failure, and output flows.
FR46: Epic 4 - Transition cutover validates direct monitor capture without picker on Windows hardware.
FR47: Epic 4 - Transition cutover validates overlay behavior across HDR/SDR displays, multi-monitor placement, and DPI scales.
FR48: Epic 6 - Clipboard and file output validation follows configured settings.
FR49: Epic 8 - Structured diagnostics retain operation, stage, mapped user-facing state, and technical detail.

## Historical Foundation Baseline

The following historical epics are retained for traceability and evidence only. They are not active Phase 4 implementation backlog and should not be selected by sprint planning as new work:

- Historical Epic 1: HDR Preview Foundation
- Historical Epic 2: Direct Capture Lifecycle
- Historical Epic 3: Region Overlay Release-to-Capture

These records document existing implementation and validation evidence from the pre-MVP-rebaseline route. Active MVP implementation begins with Epic 4.

## Active MVP Epic List

### Epic 4: MVP Rebaseline Transition and Foundation Cutover
Users can rely on the existing Epic 1-3 capture foundation as the new MVP baseline rather than as a pre-rebaseline prototype path. This epic audits and cuts over the current implementation by preserving the HDR/capture/overlay assets that still match the MVP, demoting or removing stale picker/debug/dashboard/confirm-first/hardcoded-status assumptions from the default path, establishing app-facing seams for settings/output/tray/hotkeys, fixing overlay UX deviations discovered during Epic 3 validation, adding diagnostic observability for lifecycle verification, and validating direct monitor capture, overlay behavior, basic clipboard, and lifecycle behavior before UI and product claims build on them.
**FRs covered:** FR6, FR7, FR8, FR15, FR16, FR17, FR18, FR19, FR20, FR21, FR24, FR44, FR46, FR47, FR49.

### Epic 5: Native v0 Main Window and Settings Experience
Users can operate Lumiere through a native WinUI experience that matches the v0 MVP reference intent: compact Lumiere branding, fullscreen and region capture actions, shortcut labels, HDR status summary, settings entry, minimize/background intent, and settings sections for currently supported preferences. Settings for output, shortcuts, and after-capture behavior must be read-only, disabled, validation-scoped, or explicitly marked pending until the corresponding behavior is implemented in Epic 6 or Epic 7.
**FRs covered:** FR1, FR2, FR9, FR13, FR30, FR32, FR34, FR35, FR37, FR38.

### Epic 6: Configured Output Users Can Trust
Users can configure where screenshots go and trust that captures obey those settings. This epic turns clipboard, folder, and both-target output into explicit behavior with save path handling, timestamp naming, copy-as-image behavior, supported after-capture behavior, per-target completion and failure feedback, and honest export/color semantics that do not claim HDR preservation without validation evidence.
**FRs covered:** FR22, FR23, FR24, FR25, FR26, FR27, FR28, FR29, FR36, FR48.

### Epic 7: Tray, Hotkeys, and Background Capture
Users can keep Lumiere out of the way while still capturing through global shortcuts or the tray. This epic adds tray status and commands, global hotkey registration and recovery, background availability, open-main-window, open-settings, quit, and shared capture/session routing so tray, hotkeys, and the main window cannot start conflicting sessions.
**FRs covered:** FR3, FR4, FR5, FR10, FR31, FR33, FR39, FR40, FR41, FR42, FR43.

### Epic 8: HDR Trust, Recovery, and Release Validation
Users and developers can trust what Lumiere says about capture fidelity and release readiness. This epic is a trust-hardening and release-validation gate that depends on the relevant MVP surfaces existing before final validation can complete. It completes the evidence-backed HDR state model, actionable HDR alerts, degraded/unsupported/failed/completed language, structured diagnostics, validation-level records, repeated lifecycle evidence, output validation evidence, and Windows manual validation gates for HDR displays, WGC/DXGI behavior, tray/hotkeys, multi-monitor behavior, DPI scaling, and resource trends.
**FRs covered:** FR11, FR12, FR14, FR20, FR44, FR45, FR49.

## Epic 1: Historical HDR Preview Foundation

Preserve the existing native WinUI/.NET foundation, FP16 Windows Graphics Capture path, D3D11/DXGI interop, FP16 scRGB swap-chain preview, and preview readiness vocabulary as historical foundation work. This epic is retained for traceability and should not be recreated as new MVP story work.

### Story 1.1: Retain Native Windows App Foundation

As a Lumiere developer,
I want the existing native Windows app scaffold retained as the baseline,
So that future MVP work builds on the validated Windows-only foundation instead of reintroducing starter or web-stack risk.

**Requirements Covered:** NFR25, NFR28, NFR29; Additional Requirements 1, 3, 4.

**Acceptance Criteria:**

**Given** the rebaselined MVP plan
**When** Epic 1 is reviewed
**Then** `Lumiere.sln`, module boundaries, .NET 10 Windows x64 targeting, WinUI 3, Windows App SDK, and central package/build conventions are treated as retained historical foundation
**And** this story is marked historical/retained rather than new MVP implementation work.

**Given** future stories need UI or app startup work
**When** those stories are created
**Then** they reference the existing native scaffold and do not recreate a starter template.

**Given** the v0 reference is used for UX direction
**When** implementation decisions are made
**Then** React, Tailwind, shadcn, Radix, Electron, Tauri, and web UI dependencies remain out of production code.

### Story 1.2: Retain HDR Constants and Readiness Vocabulary

As a Lumiere developer,
I want the existing HDR constants and preview readiness vocabulary retained,
So that later MVP work keeps a single source of truth for HDR capability and trust states.

**Requirements Covered:** FR14, FR49, NFR10, NFR31, NFR33.

**Acceptance Criteria:**

**Given** future capture, preview, output, or UI stories need HDR state information
**When** they reference readiness behavior
**Then** they reuse the existing HDR constants and readiness models instead of inventing parallel status strings.

**Given** degraded, unsupported, failed, or ready preview states are presented
**When** user-facing labels are derived
**Then** the implementation preserves evidence-backed state mapping and avoids success language for unverified states.

**Given** HDR constants are changed in future work
**When** automated validation runs
**Then** existing constants and readiness tests must be updated or fail the quality gate.

### Story 1.3: Retain D3D11 Device and WinRT/DXGI Interop Bridge

As a Lumiere developer,
I want the existing D3D11 device and WinRT/DXGI interop bridge retained,
So that future capture and output work can use the native GPU path without duplicating COM or device ownership.

**Requirements Covered:** FR49, NFR25, NFR26, NFR29, NFR30; Additional Requirements 7, 12.

**Acceptance Criteria:**

**Given** future stories require WGC, D3D11, DXGI, WinRT, or COM access
**When** ownership boundaries are reviewed
**Then** native interop remains inside the approved graphics and infrastructure modules.

**Given** a future story needs to add interop behavior
**When** it introduces new native handles or COM pointers
**Then** it documents ownership, disposal, diagnostics, and validation level.

**Given** implementation code is reviewed
**When** UI code attempts to own D3D11 devices, DXGI swap chains, WGC frame pools, COM pointers, HWND, or HMONITOR values directly
**Then** the review flags the change as a boundary violation.

### Story 1.4: Retain FP16 scRGB Swap-Chain Preview

As a screenshot user,
I want Lumiere's live preview foundation to preserve the FP16 scRGB path,
So that later MVP capture flows do not regress into SDR bitmap preview behavior.

**Requirements Covered:** FR14, NFR6, NFR7, NFR12, NFR31.

**Acceptance Criteria:**

**Given** the preview surface is created
**When** future stories modify capture or UI presentation
**Then** the authoritative live preview remains backed by the FP16 DXGI swap-chain path.

**Given** a future implementation proposes `BitmapImage`, `SoftwareBitmap`, GDI, WIC, CPU readback, or ordinary XAML image presentation
**When** it affects the authoritative live preview
**Then** the story must reject it as an SDR fallback unless a separate approved exception is documented outside the main HDR preview path.

**Given** preview teardown occurs
**When** resources are released
**Then** swap-chain presentation must detach from the UI surface before DXGI resources are disposed.

### Story 1.5: Retain Minimal WGC FP16 Capture to Live Preview Proof

As a Lumiere developer,
I want the existing WGC FP16 live preview proof retained with its validation evidence,
So that the MVP rebaseline can trust the core HDR capture path while clearly marking its validation scope.

**Requirements Covered:** FR14, FR44, FR49, NFR6, NFR33.

**Acceptance Criteria:**

**Given** Epic 1 validation records are reviewed
**When** release readiness is discussed
**Then** the WGC FP16 frame to D3D11 texture to FP16 scRGB swap-chain proof is treated as Windows manual-validated for its historical scope.

**Given** future stories build on this proof
**When** they make broader claims about output, tray, hotkey, multi-monitor, or repeated lifecycle behavior
**Then** they must record their own validation level and cannot inherit broader claims from Story 1.5.

**Given** structured diagnostics are retained
**When** preview startup or interop fails
**Then** operation, stage, mapped user-facing state, and technical detail remain available for engineering triage.

## Epic 2: Historical Direct Capture Lifecycle

Preserve the existing direct capture lifecycle foundation: typed target selection, explicit session state, cancellation, restart, deterministic teardown, generation guarding, and direct monitor capture without picker. This epic is retained for traceability, while known Windows manual validation gaps are carried into Epic 4 and Epic 8.

### Story 2.1: Retain Typed Capture Target Selection

As a screenshot user,
I want capture target selection to produce explicit success, cancel, unsupported, or failure outcomes,
So that Lumiere can recover predictably instead of treating every selection path as a successful capture.

**Requirements Covered:** FR6, FR8, FR15, NFR14; Additional Requirements 5.

**Acceptance Criteria:**

**Given** target selection is invoked
**When** a target is selected, canceled, unsupported, or fails
**Then** the retained target selection result model maps the outcome into explicit capture session state.

**Given** a future story changes target selection
**When** the default MVP path is evaluated
**Then** direct monitor capture remains the primary path and picker behavior remains fallback or debug-only unless reapproved.

**Given** target selection fails
**When** the UI is updated
**Then** the app returns to recoverable idle or failed state without leaving active WGC or overlay resources.

### Story 2.2: Retain Explicit Capture Session State

As a Lumiere developer,
I want the existing capture session state model retained,
So that main window, overlay, output, tray, hotkeys, and validation speak the same lifecycle vocabulary.

**Requirements Covered:** FR7, FR14, FR45, NFR10, NFR13; Additional Requirements 8, 9.

**Acceptance Criteria:**

**Given** future UI surfaces display capture state
**When** they need status values
**Then** they project from the retained session model rather than creating new state enums or ad hoc strings.

**Given** a capture session enters idle, selecting, initializing, capturing, degraded, unsupported, failed, or disposed state
**When** user-facing copy is generated
**Then** the copy remains consistent with the typed lifecycle state and readiness evidence.

**Given** stale async capture work completes after a newer session begins
**When** it attempts to update UI or output state
**Then** generation or session token checks prevent stale mutation.

### Story 2.3: Retain Stop, Restart, and Resource Recreation Behavior

As a screenshot user,
I want capture stop, restart, and frame-size recreation to be predictable,
So that repeated capture attempts do not leak resources or strand the app in an unusable state.

**Requirements Covered:** FR6, FR7, FR8, FR45, NFR11, NFR12, NFR13, NFR15.

**Acceptance Criteria:**

**Given** capture is stopped, restarted, or recreated after frame-size change
**When** retained lifecycle code runs
**Then** capture session, frame pool, frame presenter, and swap-chain resources are deterministically disposed or replaced.

**Given** a new capture begins after an older capture
**When** callbacks from the older capture arrive
**Then** generation checks prevent them from mutating the active session.

**Given** ordinary stop or restart occurs
**When** resources are cleaned up
**Then** the shared graphics device is not disposed unless app shutdown or documented device-loss recovery is in progress.

### Story 2.4: Retain Lifecycle Validation Evidence

As a Lumiere developer,
I want repeated capture lifecycle validation evidence retained,
So that the MVP rebaseline can identify which lifecycle behaviors are already automated and which require Windows manual validation.

**Requirements Covered:** FR44, FR45, NFR5, NFR11, NFR27, NFR33.

**Acceptance Criteria:**

**Given** lifecycle validation records are reviewed
**When** future stories depend on cancellation, restart, failure recovery, or teardown
**Then** they reference the retained automated evidence and explicitly identify any remaining Windows manual validation gap.

**Given** repeated lifecycle behavior is validated
**When** resource trend checks are recorded
**Then** private bytes, handles, GPU allocator trends, or documented equivalents are compared against defined noise thresholds.

**Given** a future story changes lifecycle sequencing
**When** it is accepted
**Then** lifecycle validation documentation is updated or the validation gap is carried forward explicitly.

### Story 2.5: Retain Direct Monitor Capture Without Picker

As a screenshot user,
I want Lumiere's default capture path to start from the current monitor without a system picker,
So that the MVP capture flow remains low-interruption.

**Requirements Covered:** FR15, FR46, NFR23, NFR27; UX-DR4.

**Acceptance Criteria:**

**Given** the user starts the default capture flow
**When** the rebaselined MVP path is used
**Then** Lumiere attempts direct monitor capture without a picker-first interruption.

**Given** direct monitor resolution fails
**When** the app handles the failure
**Then** it returns to a recoverable state with explicit failure feedback and no stranded overlay or capture resources.

**Given** Windows manual validation is performed
**When** direct monitor capture is tested across real display setups
**Then** results are recorded separately from Mac edit or Windows CI-pass status.

## Epic 3: Historical Region Overlay Release-to-Capture

Preserve the existing fullscreen overlay, crop geometry, adjustment, invalid crop handling, Escape/cancel, release-to-capture behavior, and basic clipboard output as historical foundation work. This epic is retained for traceability; output semantics and validation are completed in later MVP epics.

### Story 3.1: Retain Fullscreen Overlay Above HDR Preview

As a screenshot user,
I want a fullscreen overlay above the HDR preview,
So that I can select the capture region directly over the content I am capturing.

**Requirements Covered:** FR16, FR21, FR47, NFR3, NFR27.

**Acceptance Criteria:**

**Given** region capture starts
**When** the retained overlay is shown
**Then** it is placed fullscreen for the capture target and remains above the preview for crop interaction.

**Given** overlay placement is applied
**When** multi-monitor or DPI conditions vary
**Then** placement requirements are carried into Epic 4 and Epic 8 validation rather than silently assumed complete.

**Given** the overlay is excluded from capture where supported
**When** capture runs
**Then** overlay windowing remains owned by the overlay and infrastructure boundaries.

### Story 3.2: Retain Crop Selection by Dragging

As a screenshot user,
I want to draw a crop region by dragging,
So that I can quickly select the part of the screen I need.

**Requirements Covered:** FR16, FR19, FR21, NFR2, NFR20; UX-DR3.

**Acceptance Criteria:**

**Given** the overlay is active and crop input is enabled
**When** the user presses, drags, and releases the pointer
**Then** the retained crop controller creates a valid selection when geometry meets the minimum size.

**Given** the drag is too small or invalid
**When** the gesture commits
**Then** no output is produced and the overlay remains recoverable.

**Given** crop visuals update during drag
**When** the pointer moves
**Then** feedback remains visually continuous without changing preview surface geometry.

### Story 3.3: Retain Crop Adjustment and Recreation

As a screenshot user,
I want to adjust or redraw the selected region,
So that I can correct the crop before capture is finalized.

**Requirements Covered:** FR16, FR19, FR21, NFR2.

**Acceptance Criteria:**

**Given** a crop selection is active
**When** the user drags an edge, handle, or replacement region
**Then** the retained crop controller updates or recreates the selection according to existing geometry rules.

**Given** an adjustment produces invalid geometry
**When** the gesture commits
**Then** the previous valid selection is preserved or the invalid attempt is ignored according to the retained crop state machine.

**Given** future MVP UX reduces visible chrome
**When** crop adjustment behavior is reviewed
**Then** the underlying crop interaction remains available unless explicitly removed by a rebaseline story.

### Story 3.4: Retain Confirm and Cancel Overlay Paths

As a screenshot user,
I want confirmation and cancel paths to remain distinct,
So that completing capture and abandoning capture cannot race or collapse into ambiguous behavior.

**Requirements Covered:** FR17, FR18, FR20, NFR20.

**Acceptance Criteria:**

**Given** a crop is confirmable
**When** confirmation occurs
**Then** the overlay emits a typed confirmed capture payload.

**Given** the user cancels through Escape or an available cancel path
**When** cancellation occurs
**Then** the overlay emits close/cancel behavior without emitting capture confirmation.

**Given** release-to-capture and cancel happen close together
**When** the retained closing guard is evaluated
**Then** only one terminal path is processed.

### Story 3.5: Retain Hit Testing and Keyboard Escape

As a screenshot user,
I want overlay pointer and keyboard input to behave predictably,
So that I can select, adjust, or cancel capture without fighting the overlay.

**Requirements Covered:** FR18, FR20, FR21, NFR20, NFR21.

**Acceptance Criteria:**

**Given** crop input is enabled
**When** the user interacts with the overlay canvas
**Then** hit testing routes pointer input to create, adjust, or replace crop selection as retained.

**Given** Escape is pressed while overlay can safely close
**When** the retained keyboard route handles it
**Then** capture is canceled and resources are torn down through the normal lifecycle.

**Given** overlay state is unsupported, failed, closing, or disposed
**When** crop input availability is applied
**Then** hit testing is disabled for crop operations.

### Story 3.6: Retain Release-to-Capture and Basic Clipboard Output

As a screenshot user,
I want releasing a valid crop to finish capture quickly,
So that the MVP flow remains fast and familiar.

**Requirements Covered:** FR17, FR24, FR48, NFR4, NFR8, NFR19; UX-DR3, UX-DR16.

**Acceptance Criteria:**

**Given** the overlay is active and the user releases a valid crop
**When** the retained release-to-capture behavior runs
**Then** capture confirmation is emitted without requiring a separate Confirm button in the default successful path.

**Given** basic clipboard output is attempted
**When** the crop texture is copied and converted
**Then** a usable clipboard image may be produced without altering the FP16/scRGB live preview path.

**Given** clipboard output succeeds or fails
**When** the overlay closes
**Then** capture resources are torn down and later epics must define configured output semantics and honest completion/failure feedback.

## Epic 4: MVP Rebaseline Transition and Foundation Cutover

Users can rely on the existing Epic 1-3 capture foundation as the new MVP baseline rather than as a pre-rebaseline prototype path. This epic audits and cuts over the current implementation by preserving the HDR/capture/overlay assets that still match the MVP, demoting or removing stale picker/debug/dashboard/confirm-first/hardcoded-status assumptions from the default path, establishing app-facing seams for settings/output/tray/hotkeys, fixing overlay UX deviations discovered during Epic 3 validation, adding diagnostic observability for lifecycle verification, and validating direct monitor capture, overlay behavior, basic clipboard, and lifecycle behavior before UI and product claims build on them.

### Story 4.1: Classify Existing Foundation for MVP Cutover

As a Lumiere product owner,
I want the Epic 1-3 implementation classified as retained, reworked, deferred, or removed,
So that the rebaselined MVP starts from a deliberate foundation instead of accidental historical behavior.

**Requirements Covered:** FR44, FR49, NFR24, NFR29; Additional Requirements 2, 14.

**Acceptance Criteria:**

**Given** the existing app, capture, graphics, overlay, clipboard, settings, and validation artifacts
**When** the cutover audit is completed
**Then** each major capability is classified as retained, reworked, deferred, or removed for the MVP route.

**Given** a historical feature remains useful
**When** it is retained
**Then** the record explains which FR, NFR, UX-DR, or architecture rule it supports.

**Given** a historical behavior conflicts with the v0 MVP direction
**When** it is reworked, deferred, or removed
**Then** the record states the product reason and the follow-up epic or story that owns the replacement.

### Story 4.2: Cut Over Capture Commands to the MVP Session Contract

As a screenshot user,
I want fullscreen and region capture commands to route through one MVP session contract,
So that main window, future tray commands, and future hotkeys cannot start conflicting capture flows.

**Requirements Covered:** FR6, FR7, FR8, FR15, NFR13, NFR14; UX-DR2, UX-DR18.

**Acceptance Criteria:**

**Given** a capture command is invoked from any app-facing entry point
**When** a session is already selecting, initializing, capturing, outputting, closing, or failed in a non-recoverable way
**Then** the command is rejected or queued according to an explicit MVP rule and no second active WGC session is created.

**Given** fullscreen and region capture modes are represented
**When** the app routes the command
**Then** the mode is explicit in typed state or command payloads rather than inferred from a button name.

**Given** capture startup fails
**When** the session contract handles it
**Then** the app returns to a recoverable idle or failed state and releases overlay, WGC, and graphics resources as appropriate.

### Story 4.3: Demote Legacy Picker and Dashboard Behavior from the Default Path

As a screenshot user,
I want the default MVP path to avoid legacy picker-first and dashboard-only behavior,
So that capture starts from the low-interruption workflow promised by the rebaseline.

**Requirements Covered:** FR15, NFR22, NFR23; UX-DR4, UX-DR20.

**Acceptance Criteria:**

**Given** the current app still contains dashboard-era labels, debug-oriented commands, or picker fallback assumptions
**When** the MVP cutover is implemented
**Then** those behaviors are removed from the default user path, demoted behind explicit debug/fallback access, or documented as deferred.

**Given** direct monitor capture is available
**When** region capture starts
**Then** the no-picker direct monitor path remains the default.

**Given** a fallback path is retained
**When** it is exposed
**Then** the UI and documentation do not present it as the primary MVP workflow.

### Story 4.4: Establish App-Facing Seams for Settings, Output, Tray, and Hotkeys

As a Lumiere developer,
I want stable app-facing seams for settings, output, tray, and hotkeys,
So that later MVP epics can connect UI and system integration without adding more native ownership to `MainWindow.xaml.cs`.

**Requirements Covered:** FR28, FR41, FR49, NFR26, NFR29; Additional Requirements 7, 8, 10.

**Acceptance Criteria:**

**Given** future settings, output, tray, and hotkey stories need to interact with capture
**When** this transition story is complete
**Then** they can call narrow app-facing services or command interfaces instead of directly manipulating WGC, D3D11, DXGI, overlay windows, or COM pointers.

**Given** `MainWindow.xaml.cs` currently owns capture orchestration
**When** seams are introduced
**Then** the story reduces or fences orchestration growth without forcing speculative abstractions beyond the immediate MVP needs.

**Given** native ownership remains required
**When** it crosses module boundaries
**Then** ownership and disposal responsibilities stay in capture, graphics, overlay, infrastructure, or settings modules as appropriate.

### Story 4.5: Validate Foundation Cutover on Windows Hardware

As a Lumiere developer,
I want the retained foundation validated under the rebaselined MVP path,
So that UI and output work does not build on unverified direct capture, overlay, or lifecycle assumptions.

**Requirements Covered:** FR44, FR45, FR46, FR47, FR48, FR49, NFR5, NFR27, NFR32, NFR33.

**Acceptance Criteria:**

**Given** direct monitor capture, overlay crop, release-to-capture, and basic clipboard output are retained
**When** Windows manual validation runs
**Then** results are recorded for no-picker capture, overlay placement, valid crop release, invalid crop recovery, Escape cancel, clipboard attempt, repeated lifecycle, multi-monitor, HDR/SDR displays, and common DPI scales.

**Given** validation cannot be completed for a scenario
**When** the story is closed
**Then** the gap is recorded with validation level and carried into Epic 8 rather than hidden.

**Given** automated gates are run
**When** they complete
**Then** restore, build, relevant tests, and format verification outcomes are recorded separately from Windows manual validation.

### Story 4.6: Fix Overlay UX Deviations from Epic 3 Validation

As a screenshot user,
I want the overlay to follow the UX specification for cancel affordance, completion feedback, and invalid crop behavior,
So that the capture experience matches the intended MVP interaction model.

**Requirements Covered:** FR18, FR19, FR20, FR21, FR24, NFR3, NFR20; UX-DR3.

**Acceptance Criteria:**

**Given** the overlay is open and capture is in progress
**When** the user looks for a way to cancel
**Then** a visible cancel affordance (button or equivalent control) is present in the overlay in addition to Escape, matching the UX specification's "reliable cancel affordances" requirement.

**Given** a valid crop is released and clipboard output succeeds
**When** the overlay shows completion feedback
**Then** a lightweight "Copied to clipboard" message is displayed in the closing state before the overlay disappears, matching the UX specification's per-target feedback requirement.

**Given** the user drags a crop that is too small or invalid
**When** the gesture commits
**Then** the overlay remains active and the user can retry the selection, rather than the overlay closing with an error message. No output is produced for invalid crops.

**Given** the cancel button, completion feedback, or invalid crop behavior is updated
**When** the changes are reviewed
**Then** they follow existing overlay UI patterns: native WinUI controls, no preview surface displacement, no crop coordinate mapping disruption, and status messages that do not rely on color alone.

**Given** the fixes are implemented
**When** automated tests run
**Then** existing overlay, crop, confirm/cancel, and lifecycle tests continue to pass, and new tests cover the visible cancel affordance, completion feedback message, and invalid-crop-stays-active behavior.

### Story 4.7: Add Diagnostic Observability for Capture and Overlay Lifecycle

As a Lumiere developer,
I want structured logging for capture resource release, stale callback rejection, and repeated capture stability,
So that lifecycle correctness can be verified from logs rather than relying solely on UI appearance or manual inspection.

**Requirements Covered:** FR44, FR49, NFR5, NFR11, NFR30; Additional Requirements 14.

**Acceptance Criteria:**

**Given** the user presses Escape to close the overlay
**When** capture teardown runs
**Then** a structured log entry records each teardown step: frame handler unsubscribe, session stop/dispose, frame pool dispose, preview detach (`SetSwapChain(null)`), and DXGI swap-chain release. The log confirms teardown completed in the expected order.

**Given** a stale callback arrives after a newer capture generation has started
**When** the `previewGeneration` guard rejects it
**Then** a structured log entry records the rejection with the stale generation ID and the current active generation ID.

**Given** the user performs repeated capture cycles (start, stop, start, stop)
**When** each cycle completes
**Then** structured log entries confirm each teardown completed fully, and no resources from a previous cycle are still held when the next cycle starts.

**Given** a clipboard write fails because the clipboard is locked by another application
**When** the failure is handled
**Then** a structured diagnostic log entry records the failure with operation, stage, and technical detail, and the overlay still closes with capture resources torn down.

**Given** the logging is implemented
**When** the code is reviewed
**Then** log entries use `ILogger` through `LumiereLoggerFactory`, include operation/stage/detail context, and do not include screenshot pixels, frame dumps, or captured screen content.

**Given** the logging is implemented
**When** automated tests run
**Then** existing capture, overlay, and lifecycle tests continue to pass, and logging does not introduce observable delays or resource holds in the teardown path.

## Epic 5: Native v0 Main Window and Settings Experience

Users can operate Lumiere through a native WinUI experience that matches the v0 MVP reference intent: compact Lumiere branding, fullscreen and region capture actions, shortcut labels, HDR status summary, settings entry, minimize/background intent, and settings sections for currently supported preferences. Settings for output, shortcuts, and after-capture behavior must be read-only, disabled, validation-scoped, or explicitly marked pending until the corresponding behavior is implemented in Epic 6 or Epic 7.

### Story 5.1: Build the Native v0 Main Panel

As a screenshot user,
I want the main window to match the compact v0 capture-first layout,
So that Lumiere feels like a focused screenshot utility rather than a dashboard prototype.

**Requirements Covered:** FR1, FR2, FR9, FR30, NFR21, NFR22; UX-DR1, UX-DR2, UX-DR5, UX-DR19, UX-DR20.

**Acceptance Criteria:**

**Given** the main window opens
**When** the v0-aligned panel is displayed
**Then** it shows Lumiere identity, settings entry, fullscreen capture, region capture, shortcut labels, HDR status summary, and minimize/background intent in a compact native WinUI layout.

**Given** the user triggers fullscreen or region capture
**When** capture is active
**Then** capture actions show lifecycle-driven active or disabled state and prevent duplicate triggers.

**Given** the UI is reviewed against `harness/design/v0-mvp-reference`
**When** differences are found
**Then** production code follows WinUI/Fluent conventions while preserving layout intent, density, wording hierarchy, and information architecture.

### Story 5.2: Implement Settings Navigation and Shell

As a screenshot user,
I want to open and close settings from the main panel,
So that I can configure capture behavior without leaving the native app experience.

**Requirements Covered:** FR30, NFR22; UX-DR1, UX-DR19.

**Acceptance Criteria:**

**Given** the main panel is visible
**When** the user activates the settings entry
**Then** the app displays a native settings panel or page with a clear path back to the main panel.

**Given** settings are open
**When** the user closes or navigates back
**Then** the main panel state remains coherent and any active capture session is not disrupted unless explicitly canceled.

**Given** settings UI is implemented
**When** it is reviewed
**Then** it uses native WinUI controls and does not copy web component code from the v0 reference.

### Story 5.3: Add Shortcut and HDR Alert Settings UI

As a screenshot user,
I want to configure fullscreen/region shortcuts and HDR alert preference,
So that Lumiere matches my workflow and warning tolerance.

**Requirements Covered:** FR13, FR32, FR38, NFR24; UX-DR8, UX-DR10, UX-DR18.

**Acceptance Criteria:**

**Given** settings are open
**When** the shortcuts section is displayed
**Then** separate fullscreen and region shortcut controls are visible with current configured values.

**Given** global hotkey registration is not yet implemented
**When** shortcut controls are displayed in settings
**Then** they are read-only, disabled, or explicitly labeled as pending registration support, and the UI does not imply that changed shortcuts are active.

**Given** Epic 7 implements global hotkey registration
**When** shortcut editing is enabled
**Then** shortcut changes are persisted through shared settings state and registration failure or conflict recovery is handled by the Epic 7 hotkey story.

**Given** the HDR alerts setting is changed
**When** the preference is saved
**Then** later HDR unavailable, degraded, unsupported, or failed prompts honor that preference.

### Story 5.4: Add Output Preference Settings UI

As a screenshot user,
I want settings for output target, save path, timestamp naming, copy-as-image, and supported after-capture behavior,
So that I can configure capture output once before using any entry point.

**Requirements Covered:** FR34, FR35, FR36, NFR24; UX-DR11, UX-DR12, UX-DR13, UX-DR14, UX-DR15, UX-DR16, UX-DR18.

**Acceptance Criteria:**

**Given** configured output behavior is not yet implemented
**When** the output section is displayed
**Then** output target, save path, timestamp naming, copy-as-image, and after-capture controls are hidden, disabled, read-only, or explicitly scoped as pending Epic 6 behavior.

**Given** Epic 6 implements configured output behavior
**When** output controls are enabled
**Then** each enabled setting is consumed by the output pipeline and reflected in per-target completion or recoverable failure feedback.

**Given** Epic 6 has enabled output preference controls and clipboard output is part of the selected target
**When** clipboard settings are displayed
**Then** copy-as-image is visible and does not imply HDR-preserving clipboard semantics.

**Given** after-capture and timestamp preferences are visible after Epic 6 enables output controls
**When** unsupported behavior lacks implementation semantics
**Then** it is hidden, disabled, or clearly scoped until Epic 6 implements it.

### Story 5.5: Persist Local Settings Across Launches

As a screenshot user,
I want Lumiere to remember my settings,
So that capture entry points obey the same preferences every time I launch the app.

**Requirements Covered:** FR28, FR38, NFR18, NFR19; UX-DR18.

**Acceptance Criteria:**

**Given** the user changes supported settings
**When** the app is closed and reopened
**Then** fullscreen shortcut, region shortcut, HDR alert preference, output target, save path, timestamp naming, copy-as-image, and supported after-capture preferences are restored.

**Given** settings data is missing, invalid, or from an older schema
**When** settings load
**Then** Lumiere falls back to safe defaults and records diagnostics without blocking app startup.

**Given** main panel, future tray, future hotkeys, and output pipeline need preferences
**When** they read settings
**Then** they consume the same local settings source rather than maintaining UI-local state.

### Story 5.6: Show Native About and Version Information

As a screenshot user,
I want to see Lumiere's name, version, and brief HDR-first description,
So that I can identify the app and understand its purpose.

**Requirements Covered:** FR37, NFR8; UX-DR17.

**Acceptance Criteria:**

**Given** settings are open
**When** the About section is displayed
**Then** it shows Lumiere name, version, and concise HDR-first product description.

**Given** version information is shown
**When** the app is built or packaged
**Then** the displayed version comes from app/build metadata or a single authoritative source rather than fragile hardcoded copy.

**Given** product description mentions HDR
**When** validation is incomplete for a path
**Then** the copy avoids unsupported claims about HDR-preserving output.

## Epic 6: Configured Output Users Can Trust

Users can configure where screenshots go and trust that captures obey those settings. This epic turns clipboard, folder, and both-target output into explicit behavior with save path handling, timestamp naming, copy-as-image behavior, supported after-capture behavior, per-target completion and failure feedback, and honest export/color semantics that do not claim HDR preservation without validation evidence.

### Story 6.1: Define Output Target Policy and Result Model

As a screenshot user,
I want capture output to follow a clear target policy,
So that clipboard, folder, and both-target behavior is predictable from settings.

**Requirements Covered:** FR22, FR24, FR25, FR28, FR29, NFR8, NFR9.

**Acceptance Criteria:**

**Given** output target settings are enabled
**When** capture confirmation produces a valid image payload
**Then** the output pipeline reads the shared persisted output target and attempts only the configured targets.

**Given** output target settings are not yet supported by the output pipeline
**When** the settings UI is reviewed
**Then** the corresponding controls remain hidden, disabled, or explicitly scoped until this story enables them.

**Given** one or more output targets are attempted
**When** they complete
**Then** the result model reports per-target success, failure, skipped, and user-facing message state.

**Given** output semantics are reviewed
**When** HDR preservation has not been validated
**Then** the result model and UI copy describe basic usability, not HDR-preserving output.

### Story 6.2: Implement Configured Clipboard Output

As a screenshot user,
I want clipboard output to obey my copy-as-image preference,
So that captures copied to the clipboard behave consistently with settings.

**Requirements Covered:** FR22, FR24, FR25, FR27, FR28, FR48, NFR4, NFR8, NFR19; UX-DR16.

**Acceptance Criteria:**

**Given** clipboard output is enabled and copy-as-image is enabled
**When** a valid fullscreen or region capture completes
**Then** Lumiere writes a usable image to the Windows clipboard through the approved output path.

**Given** clipboard output is disabled or copy-as-image is off
**When** capture output is processed
**Then** no clipboard image is written and the output result records the target as skipped.

**Given** clipboard write fails
**When** the failure is handled
**Then** the app reports recoverable failure feedback and tears down capture resources without claiming success.

### Story 6.3: Implement Folder Output with Save Path and Timestamp Naming

As a screenshot user,
I want captures saved to my selected folder with safe names,
So that file output is reliable and does not overwrite previous captures.

**Requirements Covered:** FR22, FR23, FR24, FR25, FR26, FR28, FR48, NFR18; UX-DR13, UX-DR15.

**Acceptance Criteria:**

**Given** folder output is enabled
**When** a valid fullscreen or region capture completes
**Then** Lumiere writes the image artifact to the configured save folder.

**Given** timestamp naming is enabled
**When** a file is created
**Then** the filename uses deterministic invariant formatting and avoids overwriting existing files.

**Given** the save path is missing, inaccessible, or permission denied
**When** file output is attempted
**Then** the app reports recoverable failure feedback and does not silently drop output.

### Story 6.4: Implement Both-Target Output and Completion Feedback

As a screenshot user,
I want captures sent to both clipboard and folder when configured,
So that one capture can support quick sharing and durable storage.

**Requirements Covered:** FR22, FR24, FR25, FR28, FR48, NFR4.

**Acceptance Criteria:**

**Given** output target is both
**When** capture output completes
**Then** clipboard and folder targets are attempted independently and the final feedback identifies which targets succeeded.

**Given** one target succeeds and another fails
**When** feedback is shown
**Then** the message reports partial success and the specific recoverable failure without retrying indefinitely.

**Given** output processing is slow or failing
**When** bounded timeout or failure handling occurs
**Then** overlay, WGC session, and graphics resources do not remain active indefinitely.

### Story 6.5: Scope Export and Color Format Options Honestly

As a screenshot user,
I want export and color options to reflect real implementation support,
So that I am not misled about HDR preservation.

**Requirements Covered:** FR29, NFR8, NFR9, NFR24; UX-DR11.

**Acceptance Criteria:**

**Given** HDR10, P3, sRGB, or similar color/export options are considered
**When** implementation semantics are incomplete
**Then** those controls are hidden, disabled, or explicitly labeled as unavailable until real encoder, metadata, conversion policy, and validation evidence exist.

**Given** an output path is described in UI or docs
**When** HDR preservation has not been validated
**Then** copy avoids language that implies validated HDR-preserving output.

**Given** future output formats are enabled
**When** they are accepted
**Then** validation records include format choice, conversion or metadata policy, target-app assumptions, and Windows manual validation results.

### Story 6.6: Implement Supported After-Capture Behavior

As a screenshot user,
I want after-capture behavior to apply only when there is an artifact to open or reveal,
So that clipboard-only captures do not trigger confusing no-op actions.

**Requirements Covered:** FR36, NFR24; UX-DR14.

**Acceptance Criteria:**

**Given** folder output creates a file artifact
**When** supported after-capture behavior is enabled
**Then** Lumiere opens or reveals the output according to the implemented setting.

**Given** output target is clipboard-only
**When** after-capture behavior is evaluated
**Then** the app performs no unsupported open/reveal action and communicates completion through normal feedback.

**Given** after-capture action fails
**When** the failure is handled
**Then** output success remains separate from open/reveal failure in the result model.

## Epic 7: Tray, Hotkeys, and Background Capture

Users can keep Lumiere out of the way while still capturing through global shortcuts or the tray. This epic adds tray status and commands, global hotkey registration and recovery, background availability, open-main-window, open-settings, quit, and shared capture/session routing so tray, hotkeys, and the main window cannot start conflicting sessions.

### Story 7.1: Add Tray Menu with Status and Commands

As a screenshot user,
I want a compact tray menu with Lumiere status and capture commands,
So that I can use Lumiere without bringing the main window forward.

**Requirements Covered:** FR4, FR10, FR39, FR41, NFR23; UX-DR6, UX-DR7.

**Acceptance Criteria:**

**Given** Lumiere is running
**When** the user opens the tray menu
**Then** the menu shows Lumiere identity, HDR status summary, fullscreen capture, region capture, shortcut labels, open main window, settings, and quit.

**Given** capture is active
**When** the tray menu is opened
**Then** capture commands reflect the active or disabled state and cannot start a conflicting session.

**Given** tray UI is implemented
**When** native ownership is reviewed
**Then** Win32 tray details remain in infrastructure boundaries and command routing remains in app orchestration.

### Story 7.2: Open Main Window and Settings from Tray

As a screenshot user,
I want tray commands to open Lumiere or settings,
So that background operation still gives me access to configuration.

**Requirements Covered:** FR31, FR40, FR41; UX-DR6, UX-DR18.

**Acceptance Criteria:**

**Given** the tray menu is open
**When** the user selects Open Lumiere
**Then** the main window is shown or activated without creating a second app state.

**Given** the tray menu is open
**When** the user selects Settings
**Then** the settings surface opens through the same settings state used by the main window.

**Given** a capture session is active
**When** tray open-window or open-settings commands run
**Then** they do not interrupt the session unless the user explicitly cancels.

### Story 7.3: Register Global Capture Hotkeys

As a screenshot user,
I want global shortcuts for fullscreen and region capture,
So that I can trigger Lumiere from my current workflow.

**Requirements Covered:** FR3, FR33, FR41, NFR23; UX-DR7, UX-DR9.

**Acceptance Criteria:**

**Given** shortcut settings are available
**When** Lumiere starts or settings change
**Then** fullscreen and region hotkeys are registered with Windows where possible.

**Given** a registered fullscreen or region hotkey is pressed
**When** no conflicting capture session is active
**Then** the corresponding capture command routes through the shared session contract.

**Given** a shortcut is invalid, conflicts, or cannot register
**When** registration is attempted
**Then** Lumiere records the failure, provides recoverable feedback, and preserves or restores a safe shortcut state.

### Story 7.4: Support Background and Minimize-to-Tray Workflow

As a screenshot user,
I want Lumiere to remain available after leaving the main window,
So that capture stays low-interruption.

**Requirements Covered:** FR5, FR39, FR41, NFR23; UX-DR6, UX-DR20.

**Acceptance Criteria:**

**Given** the main window offers minimize/background intent
**When** the user minimizes or closes according to the MVP policy
**Then** Lumiere remains available through tray and hotkeys where configured.

**Given** the app is in background/tray mode
**When** a tray or hotkey capture command starts
**Then** capture runs without requiring the main window to be visible.

**Given** background operation is disabled or unavailable
**When** the user attempts it
**Then** the app communicates the limitation without losing capture or settings state.

### Story 7.5: Quit Cleanly from Tray

As a screenshot user,
I want Quit to close Lumiere cleanly,
So that background operation does not leave native resources behind.

**Requirements Covered:** FR42, FR43, NFR11, NFR26.

**Acceptance Criteria:**

**Given** the tray menu is open
**When** the user selects Quit
**Then** Lumiere unregisters hotkeys, disposes tray resources, cancels active capture if needed, closes overlay, releases capture and graphics resources, and exits.

**Given** output or capture is active during quit
**When** shutdown begins
**Then** the app follows a deterministic cancel/teardown path and records diagnostics for incomplete work.

**Given** quit cleanup is validated
**When** Windows manual validation runs
**Then** resource cleanup is recorded separately from automated test results.

## Epic 8: HDR Trust, Recovery, and Release Validation

Users and developers can trust what Lumiere says about capture fidelity and release readiness. This epic is a trust-hardening and release-validation gate that depends on the relevant MVP surfaces existing before final validation can complete. It completes the evidence-backed HDR state model, actionable HDR alerts, degraded/unsupported/failed/completed language, structured diagnostics, validation-level records, repeated lifecycle evidence, output validation evidence, and Windows manual validation gates for HDR displays, WGC/DXGI behavior, tray/hotkeys, multi-monitor behavior, DPI scaling, and resource trends.

### Story 8.1: Complete Evidence-Based HDR State Mapping

As a screenshot user,
I want HDR readiness messages to reflect real system and capture evidence,
So that I know when a capture can be trusted.

**Requirements Covered:** FR11, FR14, FR20, NFR10, NFR21; UX-DR5.

**Acceptance Criteria:**

**Given** the app evaluates display, system HDR, capture, preview, and output evidence
**When** state is projected to UI
**Then** users can distinguish HDR ready, enable HDR, HDR unavailable, degraded preview, unsupported capture, preview failed, output complete, and output failed states.

**Given** a state is degraded, unsupported, unvalidated, or failed
**When** user-facing text is shown
**Then** it does not use success or completed language.

**Given** state is displayed in main window, tray, overlay, or output feedback
**When** UI is reviewed
**Then** the status is distinguishable without relying on color alone.

### Story 8.2: Implement Actionable HDR Alerts

As a screenshot user,
I want concise alerts when HDR is unavailable, degraded, unsupported, or failed,
So that I understand what happened without reading diagnostics during capture.

**Requirements Covered:** FR12, FR13, FR20, NFR14, NFR22; UX-DR10.

**Acceptance Criteria:**

**Given** HDR alerts are enabled
**When** HDR unavailable, degraded, unsupported, or failed state occurs
**Then** Lumiere shows concise actionable feedback appropriate to the surface.

**Given** HDR alerts are disabled
**When** a non-critical HDR warning occurs
**Then** Lumiere suppresses optional alert chrome while preserving status and diagnostics.

**Given** capture cannot continue safely
**When** failure is surfaced
**Then** the overlay or session returns to idle/failed state without stranded topmost windows or active WGC resources.

### Story 8.3: Strengthen Structured Diagnostics and Failure Mapping

As a Lumiere developer,
I want failures mapped to structured diagnostics,
So that interop, preview, output, and lifecycle issues can be triaged without leaking captured content.

**Requirements Covered:** FR49, NFR17, NFR30; Additional Requirements 12.

**Acceptance Criteria:**

**Given** capture, preview, output, tray, hotkey, or interop failure occurs
**When** diagnostics are recorded
**Then** logs include operation, stage, mapped user-facing state, technical detail, and optional session/correlation identity.

**Given** logs are sampled after capture scenarios
**When** privacy review is performed
**Then** logs contain no screenshot pixel data, raw frame dumps, or captured screen content payloads.

**Given** user-facing feedback is shown
**When** technical diagnostics exist
**Then** concise user text and detailed engineering diagnostics remain separate.

### Story 8.4: Record Validation Level for Every MVP Capability

As a Lumiere developer,
I want each implemented capability labeled with its validation level,
So that release claims do not outrun evidence.

**Requirements Covered:** FR44, FR45, FR46, FR47, FR48, NFR27, NFR33.

**Acceptance Criteria:**

**Given** a story implements or changes an MVP capability
**When** it is marked complete
**Then** its record identifies Mac edit, Windows CI-pass, Windows manual-pass, or explicit validation gap.

**Given** a feature involves WGC, DXGI, HDR display behavior, tray, hotkeys, multi-monitor geometry, DPI scaling, or output compatibility
**When** only non-hardware validation exists
**Then** the story cannot claim Windows manual-pass.

**Given** product or release copy is prepared
**When** it mentions HDR fidelity, direct monitor behavior, output preservation, or display compatibility
**Then** it cites or aligns with recorded validation evidence.

### Story 8.5: Run MVP Release Validation Matrix

As a Lumiere release owner,
I want a final release validation matrix for the implemented MVP capture loop,
So that the team can decide whether Lumiere is ready for early users based on explicit evidence and documented gaps.

**Requirements Covered:** FR44, FR45, FR46, FR47, FR48, NFR1, NFR2, NFR5, NFR27, NFR32, NFR33.

**Acceptance Criteria:**

**Given** the MVP implementation includes main window, settings, output, tray, hotkeys, overlay, direct capture, and HDR trust states
**When** the release validation matrix is executed
**Then** it records results for trigger-to-active responsiveness, repeated start/cancel/restart/release/quit, direct monitor capture, overlay behavior, clipboard/file output, HDR/SDR displays, multi-monitor placement, DPI scales, and resource trends.

**Given** a validation scenario fails or is not run
**When** release readiness is assessed
**Then** the gap is documented as a limitation, blocker, or deferred risk instead of being implied as supported.

**Given** automated gates are part of release readiness
**When** final validation is recorded
**Then** restore, build, tests, and format verification are listed separately from Windows manual validation results.
