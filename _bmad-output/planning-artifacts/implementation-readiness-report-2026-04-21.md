---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
includedFiles:
  prd:
    - /Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/prd.md
  architecture:
    - /Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/architecture.md
  epics:
    - /Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/epics.md
  ux:
    - /Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/ux-design-specification.md
---

# Implementation Readiness Assessment Report

**Date:** 2026-04-21
**Project:** lumiere

## Step 1: Document Discovery

### PRD Files Found

**Whole Documents:**
- prd.md (28,244 bytes, modified 2026-04-20 16:41:55 CST)

**Sharded Documents:**
- None found

### Architecture Files Found

**Whole Documents:**
- architecture.md (35,335 bytes, modified 2026-04-20 16:54:21 CST)

**Sharded Documents:**
- None found

### Epics & Stories Files Found

**Whole Documents:**
- epics.md (38,632 bytes, modified 2026-04-20 17:22:46 CST)

**Sharded Documents:**
- None found

### UX Design Files Found

**Whole Documents:**
- ux-design-specification.md (64,161 bytes, modified 2026-04-20 20:05:36 CST)

**Sharded Documents:**
- None found

### Issues Found

- No duplicate whole/sharded document conflicts found.
- No required document types missing.

## Current Reassessment Status

### Overall Readiness Status

READY FOR SPRINT PLANNING

This current status supersedes the earlier `NEEDS WORK` result in this same file. The earlier result was produced before the approved `epics.md` correction was applied.

### Closed Previous Blockers

- `epics.md` now references the standalone UX specification.
- Epic 5 and Story 5.2 no longer claim implemented coverage for FR37.
- Epic 6 and Stories 6.1-6.4 are explicitly marked roadmap/post-MVP or blocked by future semantics.

### Remaining Action

Proceed to `bmad-sprint-planning` using Epics 1-5 as the MVP implementation lane. Exclude FR37 and Epic 6 from MVP sprint planning.

**Assessor:** Codex using `bmad-check-implementation-readiness`
**Current Reassessment Completed:** 2026-04-21

### Step 2: PRD Analysis

PRD requirements were re-read from `prd.md`; the PRD has not changed since the prior readiness run.

### Functional Requirements

- Total FRs: 42
- FR1-FR38 define MVP and MVP-adjacent behavior.
- FR37 remains explicitly conditional: cursor capture applies "when that option is implemented."
- FR39-FR42 remain post-MVP output/workflow capabilities.

### Non-Functional Requirements

- Total NFRs: 28
- NFRs remain unchanged from the prior run.

### Additional Requirements

Key additional requirements remain unchanged: FP16/scRGB primary preview, no SDR/GDI/bitmap main preview path, WinUI UI-thread attachment, deterministic WGC/D3D/DXGI/COM disposal, explicit degraded/unsupported states, Windows capture consent, local/offline MVP operation, and manual HDR validation.

### PRD Completeness Assessment

The PRD remains complete enough for implementation readiness validation. Its MVP/post-MVP boundary is clear; the reassessment focus is whether the corrected epics now respect that boundary.

### Step 3: Epic Coverage Validation

### Epic FR Coverage Extracted

- FR1-FR5: Covered in Epic 2
- FR6-FR10: Covered in Epic 1
- FR11-FR21: Covered in Epic 3
- FR22-FR26: Covered in Epic 4
- FR27-FR31: Covered in Epic 2
- FR32-FR33: Covered in Epic 1
- FR34: Covered in Epic 2
- FR35: Covered in Epic 4
- FR36: Covered in Epic 5
- FR37: Deferred/Post-MVP until cursor capture semantics are implemented
- FR38: Covered in Epic 5
- FR39-FR42: Covered in Epic 6, which is explicitly Roadmap / Not ready for MVP implementation

### Coverage Matrix

| FR Number | Epic Coverage | Status |
| --- | --- | --- |
| FR1 | Epic 2 | Covered |
| FR2 | Epic 2 | Covered |
| FR3 | Epic 2 | Covered |
| FR4 | Epic 2 | Covered |
| FR5 | Epic 2 | Covered |
| FR6 | Epic 1 | Covered |
| FR7 | Epic 1 | Covered |
| FR8 | Epic 1 | Covered |
| FR9 | Epic 1 | Covered |
| FR10 | Epic 1 | Covered |
| FR11 | Epic 3 | Covered |
| FR12 | Epic 3 | Covered |
| FR13 | Epic 3 | Covered |
| FR14 | Epic 3 | Covered |
| FR15 | Epic 3 | Covered |
| FR16 | Epic 3 | Covered |
| FR17 | Epic 3 | Covered |
| FR18 | Epic 3 | Covered |
| FR19 | Epic 3 | Covered |
| FR20 | Epic 3 | Covered |
| FR21 | Epic 3 | Covered |
| FR22 | Epic 4 | Covered |
| FR23 | Epic 4 | Covered |
| FR24 | Epic 4 | Covered |
| FR25 | Epic 4 | Covered |
| FR26 | Epic 4 | Covered |
| FR27 | Epic 2 | Covered |
| FR28 | Epic 2 | Covered |
| FR29 | Epic 2 | Covered |
| FR30 | Epic 2 | Covered |
| FR31 | Epic 2 | Covered |
| FR32 | Epic 1 | Covered |
| FR33 | Epic 1 | Covered |
| FR34 | Epic 2 | Covered |
| FR35 | Epic 4 | Covered |
| FR36 | Epic 5 | Covered |
| FR37 | Deferred/Post-MVP | Deferred with explicit path |
| FR38 | Epic 5 | Covered |
| FR39 | Epic 6 | Roadmap/Post-MVP |
| FR40 | Epic 6 | Roadmap/Post-MVP |
| FR41 | Epic 6 | Roadmap/Post-MVP |
| FR42 | Epic 6 | Roadmap/Post-MVP |

### Missing Requirements

No MVP FR coverage gaps were found.

FR37 is not counted as an MVP implementation gap because both the PRD and corrected epics explicitly condition it on future cursor capture semantics.

FR39-FR42 are covered in Epic 6 as roadmap/post-MVP work and are explicitly excluded from MVP sprint planning.

### Coverage Statistics

- Total PRD FRs: 42
- MVP / MVP-adjacent FRs covered in Epics 1-5: 37 (FR1-FR36 and FR38)
- Deferred FRs with explicit path: 1 (FR37)
- Roadmap/Post-MVP FRs: 4 (FR39-FR42)
- Unresolved missing FRs: 0

### Step 4: UX Alignment Assessment

### UX Document Status

Found: `ux-design-specification.md`.

### Alignment Issues

No unresolved UX alignment issues were found in the reassessment.

The previous stale issue has been corrected: `epics.md` now states that the standalone UX Design document exists at `_bmad-output/planning-artifacts/ux-design-specification.md` and must be used as an implementation input alongside PRD and Architecture.

### PRD / UX / Architecture Alignment

- UX target users and journeys still match PRD user journeys.
- UX overlay requirements are reflected in epics: fullscreen overlay, HDR readiness/trust states, degraded/unsupported/failed recovery messages, crop interaction, keyboard cancellation, diagnostics disclosure, target context, accessibility, and layout stability.
- Architecture supports these needs through `OverlayUI`, `SwapChainPanel`, XAML `Canvas`, crop coordinate mapping, `DiagnosticsService`, `DispatcherQueue`, and no preview-surface resizing on loading/degraded UI.

### Warnings

No missing-UX warning remains.

### Step 5: Epic Quality Review

### Critical Violations

None found.

### Major Issues

None remaining in the reassessment.

The three major issues from the previous readiness run have been addressed:

1. `epics.md` now references the standalone UX specification.
2. Epic 5 and Story 5.2 no longer claim implemented coverage for FR37.
3. Epic 6 and Stories 6.1-6.4 are now explicitly marked roadmap/post-MVP or blocked by future semantics.

### Minor Concerns

- Epic 6 remains in `epics.md` as roadmap material. This is acceptable because it is clearly marked "Roadmap / Not ready for MVP implementation" and the MVP readiness note excludes it from MVP sprint planning.
- Some technical/developer stories remain in Epic 1 and validation epics. This is acceptable for this product because the PRD defines HDR pipeline proof, native resource lifecycle, and validation as core user-trust enablers.

### Best Practices Compliance Checklist

| Epic | Delivers User Value | Independent in Sequence | Stories Sized | No Forward Dependencies | Clear ACs | Traceability |
| --- | --- | --- | --- | --- | --- | --- |
| Epic 1 | Pass | Pass | Pass | Pass | Pass | Pass |
| Epic 2 | Pass | Pass | Pass | Pass | Pass | Pass |
| Epic 3 | Pass | Pass | Pass | Pass | Pass | Pass |
| Epic 4 | Pass | Pass | Pass | Pass | Pass | Pass |
| Epic 5 | Pass | Pass | Pass | Pass | Pass | Pass |
| Epic 6 | Roadmap/Post-MVP | Pass as roadmap | Not MVP implementation scope | Explicitly blocked | Clear for roadmap | Pass as deferred scope |

### Quality Review Conclusion

The MVP epic sequence is now implementation-ready for sprint planning. Epics 1-5 form the MVP implementation lane. FR37 and Epic 6 are explicitly deferred and should not be selected for MVP sprint planning.

### Step 6: Final Reassessment Summary and Recommendations

### Overall Readiness Status

READY FOR SPRINT PLANNING

The corrected planning set is ready to proceed into Phase 4 sprint planning. PRD, UX, Architecture, and Epics are aligned for MVP implementation. Epics 1-5 define the MVP implementation lane. FR37 and Epic 6 are explicitly deferred/post-MVP and should not be pulled into MVP sprint planning.

### Critical Issues Requiring Immediate Action

None.

### Major Issues Requiring Immediate Action

None.

### Remaining Notes

- Epic 6 remains in the epics document as roadmap/post-MVP context. This is acceptable and useful, but sprint planning must exclude it from MVP implementation.
- FR37 remains intentionally deferred until cursor capture behavior is specified.
- Phase 0 HDR pipeline validation on real HDR hardware remains an implementation risk/gate, not a planning-readiness blocker.

### Recommended Next Steps

1. Run `bmad-sprint-planning` to generate the implementation sprint plan from corrected Epics 1-5.
2. Start implementation planning with Epic 1 Story 1.1: scaffold the native WinUI 3 `.NET 10` foundation.
3. Preserve Epic 6 as roadmap/post-MVP material and do not create MVP sprint tasks for export, clipboard, hotkey/tray, or annotation until separate design/research is approved.

### Issue Count

This reassessment identified 0 issues requiring correction before sprint planning:

- 0 critical issues
- 0 major issues
- 0 unresolved MVP FR coverage gaps

### Final Note

The previously approved correction was successfully applied to `epics.md`. The readiness blockers from the earlier run are closed. The project is ready for sprint planning with a clear MVP boundary.

**Assessor:** Codex using `bmad-check-implementation-readiness`
**Reassessment Completed:** 2026-04-21

## PRD Analysis

### Functional Requirements

FR1: Users can initiate a new screen capture session from the desktop application.
FR2: Users can choose a display or window as the capture target.
FR3: Users can cancel capture target selection before a capture session begins.
FR4: The system can report when screen capture is unsupported on the current device or Windows configuration.
FR5: The system can distinguish between normal, degraded, and unsupported capture states.
FR6: Users can view a live preview of the selected capture target before confirming a crop.
FR7: The system can preserve HDR-oriented capture data in the primary preview workflow.
FR8: The system can validate that the primary preview path is using the required HDR-capable capture and presentation configuration.
FR9: The system can notify users when the preview cannot be trusted as HDR-correct.
FR10: Users can compare the app's preview state against a clear status indicator for HDR readiness.
FR11: Users can create a crop selection by dragging over the full-screen preview.
FR12: Users can adjust or recreate the crop selection before confirmation.
FR13: Users can confirm the selected capture region.
FR14: Users can cancel the capture overlay and return to the prior desktop state.
FR15: Users can see the active crop region and non-selected area clearly while selecting.
FR16: Users can complete the MVP crop workflow without configuring advanced settings.
FR17: Users can interact with a full-screen overlay that displays the capture preview and crop controls.
FR18: The system can keep preview rendering and interaction overlays visually layered in the correct order.
FR19: The system can handle transparent or borderless overlay behavior required for screenshot selection.
FR20: The system can manage overlay hit testing so crop selection remains possible.
FR21: The system can close or dismiss the overlay reliably after confirm, cancel, or failure.
FR22: Users can see concise diagnostic information when HDR capture or preview setup fails.
FR23: Advanced users can inspect whether the app is using the intended capture format, preview format, and color-space state.
FR24: The system can detect and report target display or monitor capability differences relevant to HDR preview correctness.
FR25: The system can report graphics initialization failures with enough context to support troubleshooting.
FR26: The system can surface degraded output warnings instead of silently presenting SDR fallback as valid.
FR27: The system can start, stop, and restart capture sessions without requiring app restart.
FR28: The system can release capture, preview, and graphics resources when a session ends.
FR29: The system can recreate capture and preview resources when target size or capture target changes.
FR30: The system can detach preview presentation resources before graphics teardown.
FR31: The system can prevent stale capture frames or invalid graphics surfaces from being reused after their valid lifetime.
FR32: Developers can run a minimal HDR pipeline spike independent of later product features.
FR33: Developers can verify the app's key HDR constants and capture/preview states.
FR34: Developers can repeat capture start/stop flows to check resource stability.
FR35: Developers can test capture behavior across HDR enabled, HDR disabled, SDR monitor, and multi-monitor scenarios.
FR36: Users can access minimal local preferences needed for capture behavior once those preferences exist.
FR37: Users can choose whether future capture sessions include cursor capture when that option is implemented.
FR38: Users can enable or disable advanced diagnostics when diagnostic UI is available.
FR39: Users can export or copy capture output after HDR/SDR output semantics are defined.
FR40: Users can choose between HDR-preserving output and SDR tone-mapped output when export support exists.
FR41: Users can use global hotkey or tray workflows when post-MVP desktop integration is implemented.
FR42: Users can add lightweight annotations when post-MVP annotation support is implemented.

Total FRs: 42

### Non-Functional Requirements

NFR1: The primary preview pipeline must preserve FP16/scRGB capture data and must not silently downgrade to SDR.
NFR2: The system must expose a visible degraded or unsupported state when HDR preview correctness cannot be established.
NFR3: MVP validation must include side-by-side comparison against ordinary SDR screenshot output on real HDR hardware.
NFR4: HDR-related constants and configuration must be testable and centrally verifiable.
NFR5: Crop interaction must remain responsive during live preview under normal capture conditions.
NFR6: The live preview path must avoid CPU readback or bitmap conversion for routine frame presentation.
NFR7: Frame processing must release WGC frame objects promptly enough to avoid frame pool starvation during normal use.
NFR8: Overlay startup should feel immediate enough for screenshot use; any noticeable delay must be attributable to explicit target selection or graphics initialization.
NFR9: Repeated capture start, cancel, confirm, and restart flows must not produce unbounded GPU memory growth.
NFR10: All WGC, WinRT, COM, D3D11, DXGI, frame pool, session, texture, render target, and swap-chain resources must have deterministic teardown paths.
NFR11: The preview swap chain must be detached before graphics device teardown.
NFR12: Wrong-thread WinUI access must be prevented by design, not handled as a recoverable runtime error.
NFR13: Device/resource initialization failures must leave the application in a recoverable state.
NFR14: The MVP targets Windows desktop with `.NET 10 LTS` and `net10.0-windows10.0.19041.0`.
NFR15: The MVP targets `x64` first and must not rely on `Any CPU`.
NFR16: The application must run without network access for core capture workflows.
NFR17: The application must handle HDR and SDR monitor configurations without presenting misleading output.
NFR18: The application must use Windows capture consent and capability mechanisms.
NFR19: The MVP must not upload screenshots, telemetry, or display content to any remote service.
NFR20: Any future diagnostics must avoid capturing or exposing screenshot content unless explicitly user-approved.
NFR21: Borderless capture behavior must only be used with the required Windows capability and user consent.
NFR22: Core capture controls must be understandable without requiring graphics API knowledge.
NFR23: Error and degraded-state messages must be actionable for non-developer users while allowing advanced diagnostics for power users.
NFR24: Overlay controls should be keyboard-reachable where practical for MVP and must not trap users without a cancel path.
NFR25: Native interop code must be isolated behind narrow APIs.
NFR26: Diagnostics must identify capture stage, graphics initialization stage, and presentation stage failures separately.
NFR27: Package versions and target framework decisions must be recorded in project files once scaffolding begins.
NFR28: MVP code must preserve the module boundaries between capture, graphics rendering, and overlay UI.

Total NFRs: 28

### Additional Requirements

- Use Windows.Graphics.Capture consent and capability mechanisms; do not bypass OS capture permission, picker, or border behavior.
- If borderless capture is pursued post-MVP, request the required borderless capture access and declare the appropriate package capability.
- Avoid misleading users about capture fidelity; if the app cannot preserve HDR preview correctness, show a degraded or unsupported state.
- The primary capture and preview pipeline must preserve FP16/scRGB data and must not use SDR bitmap/GDI paths.
- WinUI objects and `SwapChainPanel` attachment must be manipulated on the UI thread.
- WGC frame callbacks and graphics rendering must be coordinated without retaining invalid frame or surface references.
- Direct3D/DXGI/WinRT/COM resources must have explicit owners and deterministic disposal.
- Multi-monitor behavior must account for HDR/SDR capability differences, target display changes, and scaling differences.
- The app must be architecture-specific rather than `Any CPU` because Windows App SDK and graphics dependencies include native components.
- Integrate WGC `Direct3D11CaptureFramePool` with a D3D11 device exposed as a WinRT `IDirect3DDevice`.
- Convert or access captured `IDirect3DSurface` content as GPU resources usable by the rendering layer.
- Create a DXGI composition swap chain and attach it to WinUI through `ISwapChainPanelNative`.
- Use `IDXGISwapChain3.SetColorSpace1` to set the scRGB color space.
- Use a transparent/full-screen WinUI overlay with controlled hit testing for crop selection.
- Phase 0 spike must demonstrate WGC -> FP16 D3D11 texture -> scRGB swap chain -> `SwapChainPanel` preview on HDR hardware.
- MVP preview path must have no 8-bit SDR, GDI, `BitmapImage`, or `SoftwareBitmap` dependency in the main live preview path.
- Manual HDR validation must cover HDR enabled, HDR disabled, SDR monitor, and at least one multi-monitor configuration.
- Resource teardown tests or manual diagnostics must show stable GPU memory across repeated capture sessions.
- The app must use `.NET 10 LTS`, TargetFramework `net10.0-windows10.0.19041.0`, WinUI 3, Windows App SDK 1.8 stable, WGC, Direct3D 11, DXGI, and Vortice.
- The capture frame pool must use `DirectXPixelFormat.R16G16B16A16Float`.
- The swap chain must use `DXGI_FORMAT_R16G16B16A16_FLOAT` and `DXGI_COLOR_SPACE_RGB_FULL_G10_NONE_P709`.
- UI-thread and capture-thread boundaries must be explicit; frame callbacks must not mutate WinUI state directly.

### PRD Completeness Assessment

The PRD is complete enough for traceability validation: it has explicit FR and NFR numbering, a clear MVP boundary, platform and graphics constraints, integration requirements, validation scenarios, and post-MVP scope. The main readiness risk to validate in later steps is whether epics and stories separate MVP commitments from post-MVP items as cleanly as the PRD does.

## Epic Coverage Validation

### Epic FR Coverage Extracted

FR1: Covered in Epic 2
FR2: Covered in Epic 2
FR3: Covered in Epic 2
FR4: Covered in Epic 2
FR5: Covered in Epic 2
FR6: Covered in Epic 1
FR7: Covered in Epic 1
FR8: Covered in Epic 1
FR9: Covered in Epic 1
FR10: Covered in Epic 1
FR11: Covered in Epic 3
FR12: Covered in Epic 3
FR13: Covered in Epic 3
FR14: Covered in Epic 3
FR15: Covered in Epic 3
FR16: Covered in Epic 3
FR17: Covered in Epic 3
FR18: Covered in Epic 3
FR19: Covered in Epic 3
FR20: Covered in Epic 3
FR21: Covered in Epic 3
FR22: Covered in Epic 4
FR23: Covered in Epic 4
FR24: Covered in Epic 4
FR25: Covered in Epic 4
FR26: Covered in Epic 4
FR27: Covered in Epic 2
FR28: Covered in Epic 2
FR29: Covered in Epic 2
FR30: Covered in Epic 2
FR31: Covered in Epic 2
FR32: Covered in Epic 1
FR33: Covered in Epic 1
FR34: Covered in Epic 2
FR35: Covered in Epic 4
FR36: Covered in Epic 5
FR37: Covered in Epic 5
FR38: Covered in Epic 5
FR39: Covered in Epic 6
FR40: Covered in Epic 6
FR41: Covered in Epic 6
FR42: Covered in Epic 6

Total FRs in epics: 42

### Coverage Matrix

| FR Number | PRD Requirement | Epic Coverage | Status |
| --- | --- | --- | --- |
| FR1 | Users can initiate a new screen capture session from the desktop application. | Epic 2 | Covered |
| FR2 | Users can choose a display or window as the capture target. | Epic 2 | Covered |
| FR3 | Users can cancel capture target selection before a capture session begins. | Epic 2 | Covered |
| FR4 | The system can report when screen capture is unsupported on the current device or Windows configuration. | Epic 2 | Covered |
| FR5 | The system can distinguish between normal, degraded, and unsupported capture states. | Epic 2 | Covered |
| FR6 | Users can view a live preview of the selected capture target before confirming a crop. | Epic 1 | Covered |
| FR7 | The system can preserve HDR-oriented capture data in the primary preview workflow. | Epic 1 | Covered |
| FR8 | The system can validate that the primary preview path is using the required HDR-capable capture and presentation configuration. | Epic 1 | Covered |
| FR9 | The system can notify users when the preview cannot be trusted as HDR-correct. | Epic 1 | Covered |
| FR10 | Users can compare the app's preview state against a clear status indicator for HDR readiness. | Epic 1 | Covered |
| FR11 | Users can create a crop selection by dragging over the full-screen preview. | Epic 3 | Covered |
| FR12 | Users can adjust or recreate the crop selection before confirmation. | Epic 3 | Covered |
| FR13 | Users can confirm the selected capture region. | Epic 3 | Covered |
| FR14 | Users can cancel the capture overlay and return to the prior desktop state. | Epic 3 | Covered |
| FR15 | Users can see the active crop region and non-selected area clearly while selecting. | Epic 3 | Covered |
| FR16 | Users can complete the MVP crop workflow without configuring advanced settings. | Epic 3 | Covered |
| FR17 | Users can interact with a full-screen overlay that displays the capture preview and crop controls. | Epic 3 | Covered |
| FR18 | The system can keep preview rendering and interaction overlays visually layered in the correct order. | Epic 3 | Covered |
| FR19 | The system can handle transparent or borderless overlay behavior required for screenshot selection. | Epic 3 | Covered |
| FR20 | The system can manage overlay hit testing so crop selection remains possible. | Epic 3 | Covered |
| FR21 | The system can close or dismiss the overlay reliably after confirm, cancel, or failure. | Epic 3 | Covered |
| FR22 | Users can see concise diagnostic information when HDR capture or preview setup fails. | Epic 4 | Covered |
| FR23 | Advanced users can inspect whether the app is using the intended capture format, preview format, and color-space state. | Epic 4 | Covered |
| FR24 | The system can detect and report target display or monitor capability differences relevant to HDR preview correctness. | Epic 4 | Covered |
| FR25 | The system can report graphics initialization failures with enough context to support troubleshooting. | Epic 4 | Covered |
| FR26 | The system can surface degraded output warnings instead of silently presenting SDR fallback as valid. | Epic 4 | Covered |
| FR27 | The system can start, stop, and restart capture sessions without requiring app restart. | Epic 2 | Covered |
| FR28 | The system can release capture, preview, and graphics resources when a session ends. | Epic 2 | Covered |
| FR29 | The system can recreate capture and preview resources when target size or capture target changes. | Epic 2 | Covered |
| FR30 | The system can detach preview presentation resources before graphics teardown. | Epic 2 | Covered |
| FR31 | The system can prevent stale capture frames or invalid graphics surfaces from being reused after their valid lifetime. | Epic 2 | Covered |
| FR32 | Developers can run a minimal HDR pipeline spike independent of later product features. | Epic 1 | Covered |
| FR33 | Developers can verify the app's key HDR constants and capture/preview states. | Epic 1 | Covered |
| FR34 | Developers can repeat capture start/stop flows to check resource stability. | Epic 2 | Covered |
| FR35 | Developers can test capture behavior across HDR enabled, HDR disabled, SDR monitor, and multi-monitor scenarios. | Epic 4 | Covered |
| FR36 | Users can access minimal local preferences needed for capture behavior once those preferences exist. | Epic 5 | Covered |
| FR37 | Users can choose whether future capture sessions include cursor capture when that option is implemented. | Epic 5 | Covered |
| FR38 | Users can enable or disable advanced diagnostics when diagnostic UI is available. | Epic 5 | Covered |
| FR39 | Users can export or copy capture output after HDR/SDR output semantics are defined. | Epic 6 | Covered |
| FR40 | Users can choose between HDR-preserving output and SDR tone-mapped output when export support exists. | Epic 6 | Covered |
| FR41 | Users can use global hotkey or tray workflows when post-MVP desktop integration is implemented. | Epic 6 | Covered |
| FR42 | Users can add lightweight annotations when post-MVP annotation support is implemented. | Epic 6 | Covered |

### Missing Requirements

No uncovered PRD FRs were found in the epics FR coverage map.

No FRs were found in the epics coverage map that do not exist in the PRD.

### Coverage Statistics

- Total PRD FRs: 42
- FRs covered in epics: 42
- Coverage percentage: 100%

## UX Alignment Assessment

### UX Document Status

Found: `ux-design-specification.md` (64,161 bytes, modified 2026-04-20 20:05:36 CST).

### UX ↔ PRD Alignment

The UX specification aligns with the PRD's core MVP scope:

- PRD target users are reflected in UX target users: HDR creators/colorists, HDR gamers, Windows power users, and developers/testers.
- PRD journeys map directly to UX journey flows: creator reference capture, gamer HDR scene capture, power-user capability diagnosis, and developer lifecycle validation.
- PRD MVP capabilities are reflected in UX mechanics: start capture, Windows-supported target selection, fullscreen overlay, live HDR preview, preview trust status, drag crop, adjust/recreate crop, confirm/cancel, degraded/unsupported/failed states, diagnostics disclosure, and deterministic teardown.
- PRD post-MVP boundaries are preserved: export, clipboard, annotations, global hotkey, tray integration, and history remain outside the MVP capture overlay.
- PRD accessibility/usability requirements are expanded by UX: keyboard Escape, visible focus for controls, non-color-only status, readable messages, crop boundary visibility, and no full-screen trap.

No UX requirements were found that clearly contradict the PRD.

### UX ↔ Architecture Alignment

The architecture supports the UX specification's core implementation needs:

- Fullscreen overlay and crop UX are mapped to `OverlayUI`, `Lumiere.Overlay`, XAML `Canvas`, crop components, coordinate mapping, hit testing, toolbar controls, and keyboard/mouse interaction.
- HDR preview UX is supported by `SwapChainPanel`, `GraphicsEngine`, D3D11/DXGI swap chain ownership, FP16/scRGB constants, and visible degraded/unsupported status.
- Diagnostic UX is supported by `DiagnosticsService`, typed result/status objects, stage-specific diagnostics, and advanced details separated from basic user-facing messages.
- Layout stability requirements are supported by architecture rules that prevent status/diagnostics UI from resizing the preview surface or changing crop coordinate mapping.
- Threading and recovery UX are supported by `DispatcherQueue` rules, explicit capture states, deterministic teardown, and `SetSwapChain(null)` before graphics resource release.

### Alignment Issues

- The epics document contains a stale statement: "No standalone UX Design document was found." This is incorrect because `ux-design-specification.md` exists and is complete. This does not break FR coverage, but it can mislead implementation agents and should be corrected in `epics.md`.

### Warnings

- UX is present and detailed, so there is no missing-UX warning.
- Architecture support is strong for MVP UX, but implementation readiness still depends on stories preserving the UX's state-specific behavior: HDR-ready, degraded, unsupported, failed, retry, details, cancel, and keyboard escape must be represented in acceptance criteria where relevant.
- The UX references design directions in `_bmad-output/planning-artifacts/ux-design-directions.html`; this readiness pass did not validate that auxiliary HTML artifact because Step 4 focuses on UX/PRD/Architecture alignment.

## Epic Quality Review

### Overall Epic Structure Validation

The epic set is mostly well structured and traceable:

- Epic 1 provides a technically necessary but product-critical HDR preview proof. It is not merely arbitrary infrastructure because the PRD makes preview fidelity the product's core user value.
- Epic 2 can build on Epic 1 and adds capture target/session lifecycle value.
- Epic 3 can build on Epic 1 and Epic 2 and adds the fullscreen crop workflow.
- Epic 4 can build on the preview/session foundation and adds user/developer trust, diagnostics, and validation.
- Epic 5 adds local preferences and diagnostics controls, but is thin and contains one story-scope issue.
- Epic 6 is correctly labeled post-MVP, but its stories are not all implementation-ready and should not be treated as Phase 4 MVP work.

### Critical Violations

No critical violations found.

No epic has an obvious forbidden forward dependency on a later epic for its own basic function. No story explicitly depends on a future story with a higher sequence number in a way that blocks completion.

### Major Issues

#### Major Issue 1: Epics document contains stale UX discovery guidance

**Location:** `epics.md`, Requirements Inventory, "UX Design Requirements"

**Issue:** The epics document states: "No standalone UX Design document was found." This is false. `ux-design-specification.md` exists and is complete.

**Why it matters:** Implementation agents may ignore the standalone UX specification and derive overlay behavior only from PRD/Architecture, losing detailed UX requirements for trust states, recovery messages, target context, diagnostics disclosure, toolbar behavior, accessibility, and layout stability.

**Recommendation:** Update `epics.md` to reference `ux-design-specification.md` as an input and replace the stale UX warning with a short summary of UX requirements that stories must honor.

#### Major Issue 2: Story 5.2 mixes implemented diagnostics visibility with future cursor behavior

**Location:** Epic 5, Story 5.2: "Control Advanced Diagnostics Visibility"

**Issue:** Story 5.2 lists `FR37` and `FR38`, but its acceptance criteria allow cursor capture to be omitted or marked as future behavior. That means the story claims FR37 coverage while not requiring an implemented cursor-capture choice.

**Why it matters:** This creates a traceability illusion. The story can pass while FR37 remains intentionally unimplemented. That is acceptable only if FR37 is explicitly post-MVP or placeholder scope, but Epic 5 is not labeled as post-MVP in the same way Epic 6 is.

**Recommendation:** Split or clarify scope:

- Keep Story 5.2 focused on FR38 advanced diagnostics visibility.
- Move FR37 cursor capture preference into a future/post-MVP story, or add concrete acceptance criteria for a disabled-but-visible preference only if the product intentionally wants that placeholder.
- Update the FR coverage map to distinguish implemented coverage from deferred/placeholder coverage.

#### Major Issue 3: Epic 6 contains post-MVP placeholders that are not implementation-ready

**Location:** Epic 6: "Post-MVP Capture Output and Workflow Expansion"

**Issue:** Epic 6 is correctly described as a post-MVP holding epic, but Stories 6.2, 6.3, and 6.4 imply implementation of export/copy, hotkey/tray, and annotation features before separate design/research resolves semantics and architecture. Story 6.1 is research/specification work rather than a user-valuable implementation slice.

**Why it matters:** These stories are useful roadmap placeholders, but they should not enter implementation without further refinement. They depend on future decisions that the PRD and architecture explicitly defer.

**Recommendation:** Keep Epic 6 out of MVP implementation readiness, or convert it into a post-MVP planning epic with clear "not ready for implementation" status until export, clipboard, hotkey/tray, and annotation semantics are specified.

### Minor Concerns

#### Minor Concern 1: Several technical/developer stories need careful framing during implementation

**Examples:** Story 1.1 scaffold, Story 1.3 interop bridge, Story 2.4 lifecycle stability, Story 4.4 manual HDR validation.

**Assessment:** These are acceptable in this project because the PRD and architecture make technical proof, native resource lifecycle, and HDR validation core to user trust. However, they are not ordinary end-user slices and should remain tightly acceptance-test driven.

**Recommendation:** Preserve explicit acceptance criteria and avoid expanding these into broad infrastructure tasks beyond the listed requirements.

#### Minor Concern 2: Acceptance criteria are mostly Given/When/Then, but not always independently measurable

**Examples:** "Overlay startup should feel immediate enough" is represented indirectly; "clear normal or HDR-ready status" and "message is actionable" are testable only if copy/state expectations are later made concrete.

**Recommendation:** When creating implementation story files, tighten subjective wording into concrete expected states, labels, and observable behavior, especially for status text, recovery actions, and diagnostics fields.

### Best Practices Compliance Checklist

| Epic | Delivers User Value | Independent in Sequence | Stories Sized | No Forward Dependencies | Clear ACs | Traceability |
| --- | --- | --- | --- | --- | --- | --- |
| Epic 1 | Pass | Pass | Pass with technical-foundation caution | Pass | Pass | Pass |
| Epic 2 | Pass | Pass | Pass | Pass | Pass | Pass |
| Epic 3 | Pass | Pass | Pass | Pass | Pass | Pass |
| Epic 4 | Pass | Pass | Pass | Pass | Pass | Pass |
| Epic 5 | Partial | Pass | Needs correction for FR37/FR38 split | Pass | Partial | Partial |
| Epic 6 | Post-MVP only | Pass as roadmap | Not implementation-ready | Pass with deferred-decision caution | Partial | Partial |

### Quality Review Conclusion

The MVP epic sequence is broadly implementable after correcting the stale UX reference and clarifying Epic 5's cursor-capture placeholder. Epic 6 should be treated as roadmap/post-MVP planning, not as ready implementation work.

## Summary and Recommendations

### Overall Readiness Status

NEEDS WORK

The planning set is close to implementation-ready for the MVP, but it should not be treated as cleanly ready until the identified major issues are corrected. The core PRD, architecture, UX, and MVP epic sequence are strong; the problems are mostly artifact consistency and scope clarity rather than fundamental product or architecture gaps.

### Critical Issues Requiring Immediate Action

No critical issues were found.

### Major Issues Requiring Correction

1. `epics.md` incorrectly states that no standalone UX Design document was found, even though `ux-design-specification.md` exists and is complete.
2. Epic 5 Story 5.2 claims coverage for FR37 and FR38, but the acceptance criteria only truly implement diagnostics visibility and allow cursor capture to remain omitted or future behavior.
3. Epic 6 is a post-MVP holding epic, not implementation-ready work. Stories 6.1-6.4 need separate research/design refinement before they should enter an implementation sprint.

### Recommended Next Steps

1. Update `epics.md` to reference `ux-design-specification.md` and summarize the UX requirements that implementation stories must honor.
2. Split or clarify Epic 5 Story 5.2 so FR38 diagnostics visibility and FR37 cursor capture preference are not falsely treated as one implemented story.
3. Mark Epic 6 explicitly as post-MVP/not-ready-for-implementation, or move it into a roadmap section until output, clipboard, hotkey/tray, and annotation semantics are specified.
4. When generating implementation story files, tighten subjective acceptance criteria into observable labels, states, actions, and diagnostics fields.
5. Proceed with MVP implementation only after the above artifact corrections are made, starting with Epic 1 Story 1.1 and the Phase 0 HDR pipeline spike.

### Issue Count

This assessment identified 6 issues across 3 categories:

- 0 critical violations
- 3 major issues
- 3 minor warnings/concerns

### Final Note

The implementation plan has a solid foundation: all PRD FRs are covered, architecture supports the UX and MVP flow, and the epic sequence is mostly coherent. The readiness gap is precision. Fix the stale UX reference, clarify deferred FR coverage, and keep post-MVP roadmap stories out of the MVP implementation lane.

**Assessor:** Codex using `bmad-check-implementation-readiness`
**Completed:** 2026-04-21

## Reassessment Run: 2026-04-21 After Epics Correction

### Step 1: Document Discovery

### PRD Files Found

**Whole Documents:**
- prd.md (28,244 bytes, modified 2026-04-20 16:41:55 CST)

**Sharded Documents:**
- None found

### Architecture Files Found

**Whole Documents:**
- architecture.md (35,335 bytes, modified 2026-04-20 16:54:21 CST)

**Sharded Documents:**
- None found

### Epics & Stories Files Found

**Whole Documents:**
- epics.md (39,916 bytes, modified 2026-04-21 11:26:17 CST)

**Sharded Documents:**
- None found

### UX Design Files Found

**Whole Documents:**
- ux-design-specification.md (64,161 bytes, modified 2026-04-20 20:05:36 CST)

**Sharded Documents:**
- None found

### Issues Found

- No duplicate whole/sharded document conflicts found.
- No required document types missing.
