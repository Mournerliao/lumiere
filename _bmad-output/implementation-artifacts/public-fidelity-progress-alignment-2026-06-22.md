# Public Fidelity Progress Alignment - 2026-06-22

This note aligns `sprint-status.yaml` with the code and validation artifacts that landed after the 2026-06-21 Perfect HDR Fidelity course correction. The BMad status file had remained on the planning view where Epics 10-13 were backlog, while implementation commits already advanced several stories.

## Alignment Rules

- `done` means the code/documentation support for the story acceptance criteria exists and is covered by automated tests or committed release-gate documentation.
- `in-progress` means implementation support exists, but public-release completion still depends on Windows manual validation, target-app evidence, or long-run evidence.
- `backlog` means there is no focused implementation or validation evidence yet beyond earlier MVP/private-preview groundwork.

## Epic 10: Target-Aware HDR Detection and Trust Mapping

### 10-1 Map Capture Targets to Display Output Identity - done

Evidence:

- `DisplayOutputIdentity` exists in `Lumiere.Capture`.
- Direct monitor target selection carries device name and display bounds through typed contracts.
- Tests cover capture target display identity and direct monitor target evidence.

Remaining release work is tracked under 10-3, not 10-1.

### 10-2 Probe HDR Capability for the Active Capture Target - done

Evidence:

- `HdrDisplayCapability` supports target-aware selection by display name, desktop bounds, or unambiguous size.
- `SwapChainManager` and readiness mapping surface target match evidence.
- Ambiguous or unresolved target evidence is degraded/unvalidated instead of becoming `HDR Ready`.
- Tests cover target-aware HDR probe selection, ambiguous matches, readiness, and projection behavior.

Remaining release work is hardware validation, tracked under 10-3.

### 10-3 Validate Mixed HDR/SDR and Multi-Monitor Trust States - in-progress

Evidence:

- Code can represent target-aware match evidence and unresolved target states.
- Settings, main panel, tray, overlay, and validation projections expose target-aware evidence.
- `harness/validation/target-aware-hdr-validation.md` now defines the focused Windows manual workflow for proving that trust state follows the active capture target across single-display and mixed-display topologies.
- Main-panel, tray, and overlay trust detail now prefix the active capture target directly, so mixed-monitor validation is no longer forced to infer which display the current HDR state refers to.

Remaining blockers:

- Real Windows mixed HDR/SDR and multi-monitor validation must be recorded before this can be `done`.
- `docs/validation/release-validation-checklist.md` still has the public target-aware HDR gate pending evidence.

## Epic 11: HDR Output Semantics and Format Pipeline

### 11-1 Define the HDR Fidelity Contract - done

Evidence:

- `OutputProfileContract` distinguishes SDR-compatible, visual-match, HDR-preserved, and unvalidated fidelity modes.
- Output result, main panel, overlay, tray, and settings projections avoid collapsing artifact success into HDR preservation.
- Public release docs define that copied/saved/converted/HDR-preserved are separate claims.

### 11-2 Define and Implement the First Supported Output Profile - in-progress

Evidence:

- Runtime output profile capabilities gate unsupported profile claims.
- Folder output routes through artifact encoders.
- HDR10 JXR codec seams, WIC JPEG XR adapter, FP16 source readback, audit metadata write/read, and codec readiness blockers exist.
- `Hdr10JxrCodecReadiness` now covers implementation-level HDR10 JXR readiness, while runtime HDR10 execution is additionally gated on loaded manual validation artifacts.
- `OutputProfileExecutionCapabilities.ResolveHdr10JxrReleaseCapabilities(...)` now keeps HDR10 on `sRGB` fallback unless both implementation readiness and complete manual output evidence pass.
- Output-profile UI now distinguishes `Build`, `Validate`, and `Ready` states instead of collapsing every non-executable HDR10 path into one generic fallback label.
- Validation surfaces now expose the current output-profile gate directly, so testers do not need to infer `Build` / `Validate` / `Ready` only from the lower evidence rows.
- Selected output-contract surfaces now follow the same runtime distinction, so a complete HDR10 format contract no longer keeps saying `pending implementation` once the app has moved into `Validate` or `Ready`.
- Mixed `Both` output sessions now keep per-target execution semantics intact instead of collapsing clipboard and folder into one synthetic runtime profile. Clipboard result evidence stays `sRGB` compatibility-first, while folder result evidence can independently report `HDR10` artifact execution.
- HDR10 JXR runtime evidence now also requires the manual validation artifact to cover `Folder` output explicitly. Clipboard-only artifacts no longer count toward the first HDR-preserved file-output path.
- Output validation artifacts can now narrow target coverage per profile record through `outputTargetsCovered`, so one manual session can honestly say "session covered Both, but the HDR10 record only proves Folder" without over-claiming clipboard evidence as file-output release proof.
- Runtime output policy now uses the same target-aware artifact-scope seam as the UI projections. Requested-profile evidence no longer applies broader folder-side HDR10 validation to clipboard sessions before runtime fallback is resolved.
- Folder output execution now also consumes the folder-specific effective profile in `Both` sessions. This closes the gap where the UI/result model could describe a mixed clipboard+folder session honestly, but the file artifact encoder was still being driven by the aggregate `sRGB` fallback profile.

Remaining blockers:

- The repo still lacks real Windows manual HDR10 output validation artifacts, so HDR10 remains disabled in ordinary sessions.
- A supported HDR-preserved output profile is not yet a public-release path.

### 11-3 Validate Target-App Compatibility for Supported Output - in-progress

Evidence:

- Viewer compatibility evidence is modeled separately for artifact handling, visual match, HDR preservation, and HDR10 metadata recognition.
- Output validation artifacts can apply named viewer evidence to output contracts.
- `Hdr10JxrViewerValidationEvidence` now participates in the runtime gate that decides whether HDR10 can become executable for the current validated session.
- Settings and main-panel export-profile projection now surface whether HDR10 is blocked by implementation work or by missing Windows manual evidence.
- Settings and main-panel selected-contract text now mirrors that same split, so testers can tell whether HDR10 is still blocked by build/runtime prerequisites or only by missing Windows manual viewer evidence.
- Output-result projection now inherits the selected session gate as well, so successful copy/save feedback can still say the requested HDR10 path is at `Build` or `Validate` while runtime output falls back to `sRGB`.
- Output-result evidence now also calls out per-target fidelity in mixed `Both` sessions, so target-app compatibility work is no longer hidden behind a single aggregate profile when clipboard and folder take different output paths.
- The HDR10 target-app compatibility gate now ignores clipboard-only artifacts for the JXR path, keeping runtime readiness aligned with the real file-based artifact path rather than any generic viewer record.
- The same gate now also respects record-level target coverage when a mixed session validates different target semantics for different profiles, reducing ambiguity before real Windows manual artifacts are recorded.
- Selected-profile projections now also respect the active output target. Folder-side HDR10 evidence can still advance `Folder` and `Both` sessions, but it no longer causes `Clipboard` sessions to present `Build` / `Validate` / `Ready` as though the clipboard path itself had become HDR-preserved.

Remaining blockers:

- Real target-app/viewer validation artifacts must be recorded with Windows manual evidence before this can be `done`.

## Epic 12: HDR Fidelity Validation Suite and Public Release Evidence

### 12-1 Establish Standard HDR/SDR Validation Content and Scenarios - in-progress

Evidence:

- Public release checklist defines the required HDR/SDR, mixed-display, target-app, DPI, and output evidence categories.
- Output validation artifact schema/template defines required session evidence fields.
- The local validation workspace can now generate a prefilled draft artifact from the current session, reducing setup friction before real Windows manual evidence is recorded.
- Settings validation now also surfaces a compact loaded-evidence summary so testers can review the latest loaded artifact, current coverage, known limitations, follow-up stories, and ignored-file warnings without leaving the app.
- `harness/validation/hdr-sdr-validation-scenarios.md` now defines the standard content families, topology matrix, and session execution flow.
- `harness/validation/settings-accessibility-validation.md` now gives Story 13-2 a focused Windows validation workflow tied back to the release checklist.
- `harness/validation/templates/hdr-sdr-validation-session-template.md` now gives future sessions a consistent metadata/result record shape.

Remaining blockers:

- Actual executed Windows validation sessions and evidence files are still missing.

### 12-2 Expand the Release Checklist into a Public Fidelity Gate - done

Evidence:

- `docs/validation/release-validation-checklist.md` separates Private Preview / Early Validation from Perfect HDR Fidelity Public Release gates.
- Output validation docs and schema template explain how manual evidence is recorded and why invalid artifacts are surfaced.

### 12-3 Record Long-Run Capture and Output Resource Trends - in-progress

Evidence:

- `harness/validation/scripts/collect-resource-trend-samples.ps1` now provides repeatable CSV and summary-JSON sampling for process and GPU memory metrics.
- `harness/validation/resource-trend-validation.md` now defines the public-release long-run workflow and classification rules.
- `harness/validation/templates/resource-trend-session-template.md` now defines the reusable record shape for Story 12-3 sessions.
- `harness/validation/lifecycle-validation.md`, `harness/validation/release-validation-checklist.md`, and `harness/validation/index.md` now route long-run resource evidence through the new workflow.

Remaining blockers:

- No focused real Windows `50+` or `100+` cycle evidence has been recorded yet.
- Release readiness still depends on actual sampler artifacts plus pass/fail/limitation judgement from a live run.

## Epic 13: Fidelity Confidence UX and Accessibility Hardening

### 13-1 Clarify Fidelity State Copy Across Main, Tray, Overlay, and Output - done

Evidence:

- Main panel, tray, overlay, settings, and output result projections distinguish artifact completion from fidelity claims.
- Tests assert that unvalidated paths do not claim HDR-preserved behavior.
- Validation panel wording keeps public release claims behind evidence gates.
- Tray surfaces now also carry explicit output-profile gate state (`Build`, `Validate`, `Ready`, `Compat`) instead of relying only on the fidelity-claim line to imply runtime status.
- Overlay fidelity cue projection now uses the same target-aware readiness, validation artifacts, and runtime output-capability gate that the main panel uses before surfacing `HDR-preserved` status.
- Overlay fidelity copy now also exposes the selected output-profile gate directly, so overlay users are no longer limited to a generic converted/unvalidated label when the requested HDR path is still blocked at `Build` or `Validate`.

### 13-2 Harden Native Settings and Accessibility Semantics - in-progress

Evidence:

- Settings now use native `ToggleSwitch` semantics for live binary preferences instead of custom button-shaped switch visuals.
- Output destination now uses a native single-choice control path instead of custom segmented buttons.
- Export profile selection now uses native radio-button semantics, and the supported `sRGB` compatibility profile can be re-selected through the settings UI.
- Export profile projection now keeps radio-button availability and gate-state copy aligned with real runtime capability rather than a static design-only projection.
- Persisted validation-scoped export profiles now remain keyboard-focusable and screen-reader-readable as locked current-session choices instead of disappearing into a generic disabled state.
- Export profile helper copy and automation text now avoid internal `validation-scoped` jargon in favor of clearer user-facing availability language while keeping the same release-gate honesty.
- Shortcut capture and save-path browsing rows now use standard `Button` activation rather than `Tapped`-only surfaces.
- A focused implementation record exists at `_bmad-output/implementation-artifacts/13-2-harden-native-settings-and-accessibility-semantics.md`.
- Main-window shell sizing now uses a typed layout projection and reacts to alert visibility, reducing the risk that long HDR status text or `InfoBar` states squeeze compact utility content before manual DPI/text-scaling validation is run.
- The compact main panel now uses a scroll boundary for its body content while keeping header, alert, and footer structure fixed, so layout pressure falls onto secondary summary content before primary capture actions become unreachable.
- Settings > Validation now includes a compact evidence-summary block that stays native and reviewable while making the currently loaded validation scope legible to keyboard and screen-reader users.

Remaining blockers:

- Windows manual accessibility validation for keyboard, screen reader, high contrast, text scaling, and DPI is still required.
- Export profile selected-disabled behavior still needs Windows manual accessibility validation before public release.

## Resulting Sprint Status

After this alignment:

- Epic 10: `in-progress`
- Epic 11: `in-progress`
- Epic 12: `in-progress`
- Epic 13: `in-progress`

The project has moved beyond backlog for the new public-fidelity direction, but public release remains blocked by Windows manual validation evidence, supported HDR-preserved output validation, target-app compatibility, executed long-run resource trend evidence, and accessibility hardening.
