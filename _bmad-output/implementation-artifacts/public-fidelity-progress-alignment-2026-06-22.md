# Public Fidelity Progress Alignment - 2026-06-22

This note aligns `sprint-status.yaml` with the code and validation artifacts that landed after the 2026-06-21 Perfect HDR Fidelity course correction. The BMad status file had remained on the planning view where Epics 10-13 were backlog, while implementation commits already advanced several stories.

Addendum: 2026-06-23 implementation slices continue to update this note when they materially change the real evidence/review surface without yet completing the public-release gate.

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
- Settings > Validation `Target-aware HDR` row now also names the current runtime target directly, including display identity / desktop bounds when available, so mixed-monitor review no longer depends on inferring which active display the validation row refers to.
- A focused implementation record exists at `_bmad-output/implementation-artifacts/10-3-surface-active-target-context-in-validation-row.md`.
- Output feedback now also preserves the captured target context after the live session returns to `Idle`, so post-capture fidelity/result review no longer drops target identity exactly when the artifact result is shown.
- A focused implementation record exists at `_bmad-output/implementation-artifacts/10-3-carry-captured-target-context-into-output-feedback.md`.

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
- HDR10 JXR runtime gating now also checks whether the loaded manual evidence aligns to the current build token before treating that evidence as executable release support. Stale evidence can remain reviewable, but it no longer unlocks `Ready`.
- HDR10 JXR runtime gating now also requires recorded target-app versions for the named viewers under test. Viewer-status rows without matching app-version evidence no longer count as complete manual release proof.
- Settings > Validation now also surfaces target-app version evidence as a dedicated validation row, so validators do not need to infer missing viewer/app versions only from the compact loaded-evidence summary.
- The draft-generation workflow can now also prefill known Windows packaged target-app versions for supported viewers such as Microsoft Paint and Windows Photos, reducing target-app evidence friction before the manual session is finalized.
- The browser-side validation target is now explicitly `Microsoft Edge` instead of a generic Chromium bucket, so runtime gates, templates, UI wording, and target-app version evidence all refer to one concrete browser target.
- That same draft-generation workflow can now also resolve the local Microsoft Edge version through the `msedge.exe` executable path, keeping the named browser target as concrete evidence instead of a broad browser-family placeholder.
- Settings > Validation viewer rows now also separate aggregate viewer status from category-by-category evidence breakdown text, so target-app review no longer depends on parsing one dense paragraph to understand artifact handling, visual match, HDR preservation, and HDR10 metadata state.
- Those same viewer rows now also carry target-app version evidence inline, including recorded version detail or missing-version blockers for the named viewer under review instead of leaving app-version context only in a summary row.
- A focused implementation record now exists at `_bmad-output/implementation-artifacts/11-3-carry-target-app-version-evidence-into-viewer-rows.md`.
- Those same viewer rows now also state the output target scope they actually prove, preferring record-level `outputTargetsCovered` over broad session-level scope so reviewers can see whether the viewer evidence applies to `Folder`, `Clipboard`, or `Both` without cross-checking JSON.
- A focused implementation record now exists at `_bmad-output/implementation-artifacts/11-3-carry-output-target-scope-into-viewer-rows.md`.

Remaining blockers:

- Real target-app/viewer validation artifacts must be recorded with Windows manual evidence before this can be `done`.

## Epic 12: HDR Fidelity Validation Suite and Public Release Evidence

### 12-1 Establish Standard HDR/SDR Validation Content and Scenarios - in-progress

Evidence:

- Public release checklist defines the required HDR/SDR, mixed-display, target-app, DPI, and output evidence categories.
- Output validation artifact schema/template defines required session evidence fields.
- The local validation workspace can now generate a prefilled draft artifact from the current session, reducing setup friction before real Windows manual evidence is recorded.
- When the app informational version exposes a comparable build commit token, that same draft now pre-fills the current build commit instead of leaving the build field fully manual.
- Output validation artifacts, templates, and generated drafts can now also carry target-app version records, and the in-app loaded-evidence summary can surface them directly.
- Missing target-app versions now also downgrade the artifact to incomplete manual evidence, so the review summary warning and the runtime/manual-evidence semantics stay aligned.
- The validation panel now also exposes that same target-app-version completeness as a first-class row, keeping the review surface aligned with the stricter runtime/manual evidence rules.
- Generated drafts now also attempt to prefill known Windows packaged target-app versions when the current machine can identify them, while leaving unsupported or unknown viewers on explicit placeholders.
- The current browser-side validation scope is now also fixed to `Microsoft Edge`, and the seeded sample/draft placeholders use that explicit target instead of a broad Chromium family label.
- Draft generation can now also reuse stable local environment hints from the latest compatible artifact, keeping tester / Windows / device / GPU / DPI / entry-point context close at hand without silently converting older evidence into current proof.
- Draft generation can now also carry current-session GPU and DPI hints directly from the active app session, so the validator sees both "current session" and "latest local artifact" context without losing the manual replacement prompt.
- Draft generation can now also carry a current-session display-topology hint, giving the validator a session-local reminder of single-display vs multi-display context and active target bounds before real mixed-monitor evidence is recorded.
- Settings validation now also surfaces a compact loaded-evidence summary so testers can review the latest loaded artifact, current coverage, known limitations, follow-up stories, and ignored-file warnings without leaving the app.
- That same surface now also links directly to the latest loaded evidence file, reducing the gap between in-app review and the durable JSON artifact that release validation depends on.
- The same validation surface now also calls out whether the latest loaded evidence matches the current build, is stale for the current build, or cannot yet be aligned to a comparable build token.
- `harness/validation/hdr-sdr-validation-scenarios.md` now defines the standard content families, topology matrix, and session execution flow.
- `harness/validation/settings-accessibility-validation.md` now gives Story 13-2 a focused Windows validation workflow tied back to the release checklist.
- `harness/validation/templates/hdr-sdr-validation-session-template.md` now gives future sessions a consistent metadata/result record shape.
- The app-local validation workspace now also seeds the current release checklist, HDR/SDR scenario guide, and settings accessibility workflow into a local `guidance` folder instead of leaving those core manual-validation references repo-only.
- The app-local validation workspace now also seeds `templates/hdr-sdr-validation-session-template.md`, keeping the Story `12-1` scenario record template beside the JSON output-evidence sample without adding another settings-page button.
- Settings > Validation keeps evidence rows primary and moves secondary workspace, checklist, scenario, accessibility, and trend helpers into a compact native command surface instead of expanding into a standalone button grid.
- A focused implementation record now exists at `_bmad-output/implementation-artifacts/12-1-seed-public-validation-guides-into-local-workspace.md`.
- A focused implementation record now exists at `_bmad-output/implementation-artifacts/12-1-seed-scenario-session-template-into-local-workspace.md`.
- The same loaded-evidence summary now also carries entry-point, DPI, display-setup, and HDR-state coverage plus the public release checklist groups still missing from the loaded evidence, reducing manual cross-checking during Windows validation prep.
- That same summary now also recommends the next native guide or action for those missing public-gate groups, reducing another round of guesswork before a validator starts the next Windows run.
- A focused implementation record now exists at `_bmad-output/implementation-artifacts/12-1-surface-public-gate-evidence-gaps-in-validation-summary.md`.
- The loaded-evidence summary now also names covered display topology buckets, missing topology buckets, missing HDR10 named viewer targets, and a concrete next Windows run combining entry point, topology, output target, and viewer scope.
- A focused implementation record now exists at `_bmad-output/implementation-artifacts/12-1-surface-missing-topology-and-viewer-runs.md`.
- Generated output-validation drafts now also carry that missing-run scope as placeholder guidance, so the durable JSON draft and Settings > Validation summary point validators at the same next Windows run.
- A focused implementation record now exists at `_bmad-output/implementation-artifacts/12-1-carry-next-run-scope-into-validation-drafts.md`.
- `Create draft` now also writes a companion Story `12-1` scenario-session markdown draft under the local workspace `evidence\` folder and points the JSON draft's `evidencePaths` at it, closing the handoff gap between scenario notes and the runtime-loaded output-validation artifact.
- The output-validation loader now requires workspace-local `evidence\...` paths to exist and workspace-local markdown evidence to be filled in before loading the JSON artifact, so a broken or still-template companion scenario-session link cannot advance the current evidence summary or runtime gates.
- A focused implementation record now exists at `_bmad-output/implementation-artifacts/12-1-require-workspace-local-scenario-evidence.md`.

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
- The app-local validation workspace now also seeds the same resource-trend session template and sampler script next to the output-validation workflow instead of keeping Story `12-3` only in repo docs.
- Settings > Validation now exposes native `Trend template`, `Trend script`, and `Copy trend cmd` actions so a Windows validator can open the seeded helpers and copy a current-process sampler command without manually rebuilding the PowerShell invocation.
- Settings > Validation now also exposes `Create trend draft`, which writes a session-local long-run markdown record prefilled with the current process ID, output configuration, current-session hints, and workspace-local sampler command instead of leaving Story `12-3` at template-only readiness.
- `ResourceTrendValidationCommandProjection` now centralizes that copied command behind one narrow seam, keeping the current PID, workspace path, output folder, and default duration/interval policy out of the view layer.
- `ResourceTrendValidationDraftFactory` now centralizes long-run session draft content behind one narrow seam, keeping the WinUI layer limited to request assembly and file-open behavior.
- `ResourceTrendSummaryArtifact` now parses the sampler `*-summary.json` shape, and `Create trend draft` imports the latest readable workspace-local summary into the markdown draft so CSV/summary paths and metric rows do not need to be copied by hand.
- Imported sampler summaries still leave pass/fail/limitation classification as an explicit human review field, preserving the public-release gate boundary between raw telemetry and validated evidence.
- Resource-trend summary import now prefers summaries whose PID matches the current Lumiere process and flags fallback imports with an explicit scope warning, reducing the chance that a stale or unrelated sampler run is mistaken for current release evidence.
- A focused implementation record now exists at `_bmad-output/implementation-artifacts/12-3-surface-resource-trend-workflow-in-settings.md`.
- A focused implementation record now exists at `_bmad-output/implementation-artifacts/12-3-create-resource-trend-draft-from-current-session.md`.
- A focused implementation record now exists at `_bmad-output/implementation-artifacts/12-3-import-resource-trend-summary-into-draft.md`.
- A focused implementation record now exists at `_bmad-output/implementation-artifacts/12-3-scope-resource-trend-summary-imports.md`.

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
- Settings > Validation now also exposes a direct `Open latest evidence` path so keyboard and screen-reader users can move from the summary surface to the current loaded artifact without manually browsing the workspace.
- Settings > Validation now also surfaces `Current build evidence` as a first-class row, so validators do not need to infer stale-versus-current evidence by manually comparing JSON and About metadata.
- The settings validation panel now also has enough native row capacity to render the full projected evidence set, so `Target app versions` and `Current build evidence` no longer compete for one truncated row slot in the desktop UI.
- That same validation surface now also carries current active target context inside the `Target-aware HDR` row, reducing ambiguity for keyboard, screen-reader, and manual-validation review flows when mixed-monitor setups are under test.
- Settings > Validation viewer rows now also render an explicit category-by-category evidence breakdown ahead of the narrative guidance detail, reducing dependence on color and improving screen-reader/long-text review for target-app compatibility states.
- Those same viewer rows now also keep target-app version evidence inline with the named viewer, reducing cross-row inference during keyboard, screen-reader, and long-text validation review.
- Those same viewer rows now also keep output target scope inline with the named viewer evidence, reducing another cross-row inference step during keyboard, screen-reader, and long-text validation review.
- Main-panel output feedback now also keeps captured-target context visible after teardown, reducing ambiguity for post-capture review flows without inventing new non-native UI affordances.
- Settings > Validation now also exposes direct native access to the seeded release checklist, scenario guide, and settings accessibility workflow, reducing the amount of manual repo navigation required during keyboard and screen-reader validation sessions.
- That same validation summary now also calls out which public release checklist groups remain uncovered by the loaded evidence, improving long-text and screen-reader review of what still needs Windows validation.
- That same validation summary now also points to the next guide or action to run for those missing groups, keeping the accessibility review path action-oriented instead of inference-heavy.
- The same loaded-evidence summary now also calls out missing topology buckets, missing HDR10 viewer targets, and the next Windows validation run in readable text, reducing another cross-document inference step for keyboard, screen-reader, and long-text review flows.
- The `Create draft` workflow now carries the same suggested topology, entry point, output target, and viewer scope into the generated artifact placeholders, reducing another handoff gap between the native validation surface and the edited JSON evidence file.
- A focused implementation record exists at `_bmad-output/implementation-artifacts/13-2-structure-viewer-compatibility-evidence-for-accessibility.md`.

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
