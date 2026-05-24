# Deferred Work

Updated: 2026-05-13

This file tracks work intentionally deferred after implementation, review, or retrospective. It is not a graveyard: every unresolved item should either have a target epic/story hint, an accepted-tech-debt label, or a clear reason it remains parked.

Resolved review history belongs in story files or review artifacts. Historical mojibake-heavy review text should not be copied forward unless the meaning is recoverable.

## MVP Blockers or Active Defects

None currently known.

## Active Technical Debt

### Capture command rejection logic is duplicated

- Source: Story 4.2 / 4.3 code review.
- Current shape: `CaptureService.CanAcceptCommand()` decides whether a command is allowed, while `ValidateCommand()` and `TryReserveCommand()` independently classify rejection outcomes.
- Risk: if allowed/rejected states change, outcome classification can drift.
- Target: next capture-session refactor or Epic 7 tray/hotkey command routing.
- Suggested acceptance criterion: Given a capture command is rejected, when the rejection result is produced, then the state allow/deny decision and rejection outcome come from one authoritative mapping.

### `ApplySessionState` reentrancy silently drops updates

- Source: screenshot state reset follow-up.
- Current shape: `MainWindow.ApplySessionState()` returns without applying a state when `applyingSessionState` is already true.
- Risk: future tray/hotkey/output completion paths may depend on a state update that gets dropped.
- Target: before adding tray/hotkey entry points, or during a focused app-state projection refactor.
- Suggested acceptance criterion: Given a state update arrives during another state projection, when projection completes, then the later update is either applied, queued, or deliberately rejected with diagnostics.

### Capture action re-enable path depends on current overlay completion ordering

- Source: Story 4.3 review.
- Current shape: `SetCaptureActionsEnabled(true)` is not called inside the overlay completion handler after `currentCaptureOverlay = null`.
- Risk: this is safe only while overlay completion always flows through session-state reset; a future early-return or alternate completion path could leave capture actions disabled.
- Target: before broadening overlay completion paths or adding tray/hotkey capture triggers.
- Suggested acceptance criterion: Given overlay completion ends a selection session, when the overlay reference is cleared, then capture actions are either re-enabled by the authoritative session-state projection or an explicit diagnostic verifies why no re-enable is needed.

### Disposed-to-idle transition remains awkward

- Source: screenshot state reset follow-up.
- Current shape: some paths need sequential `Disposed` then `Idle` projections to communicate teardown and return-to-ready intent.
- Risk: a future atomic reset path could skip user-visible teardown evidence or leave stale UI state.
- Target: capture/app state cleanup story before tray/hotkey background workflows.
- Suggested acceptance criterion: Given capture teardown finishes and the app should return to ready, when the reset path runs, then teardown evidence and final idle state are represented without relying on fragile sequential UI calls.

### `CaptureSessionState.FromStartResult` contains a dead conditional

- Source: screenshot state reset follow-up.
- Current shape: both branches call `FromReadiness(target, result.Readiness, treatReadyAsCapturing: false)`.
- Risk: hides intended semantics and weakens future maintenance confidence.
- Target: low-risk capture-state cleanup.
- Suggested acceptance criterion: Given `CaptureStartResult` is converted to session state, when start succeeds or does not start, then the method has distinct, tested branches or the dead conditional is removed.

## Future Story Candidates

### BMad workflow: non-code story review exit criteria

- Source: Epic 4 retrospective.
- Current shape: non-code/documentation stories can reach review without a crisp checklist that proves intended artifacts, links, and follow-through were completed.
- Risk: workflow-only work may appear done while losing review findings, status updates, or traceability.
- Target: before the next documentation/planning-heavy story.
- Suggested acceptance criterion: Given a story changes only BMad artifacts, when it enters review, then the story identifies expected artifact changes, preserved decisions, verification method, and review disposition.

### Epic 5: Main window and settings guardrails

- Source: Epic 4 retrospective.
- Status: guardrail document created in `_bmad-output/implementation-artifacts/epic-5-implementation-guardrails.md`.
- Target: include as context in Story 5.1 and later Epic 5 stories.
- Follow-up: if the guardrail proves durable, promote stable rules into `harness/` or `_bmad-output/project-context.md`.

### Epic 5.2 / 5.5: Settings write and persistence plan

- Source: Epic 4 retrospective and Story 4.4 review.
- Current shape: `ISettingsProvider` and `DefaultSettingsProvider` exist, but settings are read-only and only partially consumed.
- Risk: settings UI could create local duplicated state before persistence exists.
- Target: Story 5.2 or Story 5.5.
- Suggested acceptance criterion: Given editable settings are introduced, when the user changes a supported value, then the write path and persisted source of truth are owned by `Lumiere.Settings` and consumed through a shared abstraction.

### Epic 6.1: Output policy type ownership decision

- Source: Epic 4 retrospective.
- Current shape: `CropPixelRect`, `OutputTarget`, and output request types live in `Lumiere.Graphics.Output`, and `Lumiere.Settings` references `OutputTarget`.
- Risk: output policy vocabulary may become too graphics-owned if settings and UI semantics expand.
- Target: Story 6.1.
- Suggested acceptance criterion: Given output target policy is formalized, when ownership is reviewed, then shared output vocabulary has an explicit owning module and no circular or convenience-only dependencies.

### Epic 7: Release-build UI-thread protection for non-main entry points

- Source: Story 4.3 review.
- Current shape: `Debug.Assert` documents UI-thread expectations in `ApplySessionState`, but Release builds do not enforce it.
- Risk: tray/hotkey callbacks may call app state projection from the wrong thread.
- Target: before or during Epic 7 tray/hotkey implementation.
- Suggested acceptance criterion: Given a tray or hotkey command updates app-visible state, when it arrives off the UI thread, then the update is marshalled through `DispatcherQueue` or rejected with diagnostics.

### Future overlay story: InvalidCrop integration tests

- Source: Story 4.6 review and Epic 4 retrospective.
- Missing coverage:
  - InvalidCrop save/apply/timer/restore round trip in `OverlayWindow`.
  - Escape or close during active InvalidCrop feedback.
  - rapid successive invalid crop gestures.
  - confirm click while InvalidCrop feedback is active.
- Target: next overlay-related story or focused test-hardening task.
- Suggested acceptance criterion: Given InvalidCrop feedback is active, when close, retry, timer, or confirm paths run, then prior valid selection and terminal states are preserved correctly.

### Documentation cleanup: mojibake-heavy story review sections

- Source: Epic 4 retrospective.
- Current shape: several story review sections contain encoded/mojibake text.
- Risk: future automation or human review may misread historical findings.
- Target: documentation maintenance only if those records become active source material.
- Suggested acceptance criterion: Given a mojibake-heavy story record is used as planning input, when it is cleaned, then the corrected summary preserves original review meaning and does not rewrite historical conclusions.

## Validation Gaps Carried Forward

### Epic 8.4 / 8.5: Hardware validation gaps from Epic 4

- Source: Story 4.5 and Epic 4 retrospective.
- Gaps:
  - Escape cancel with and without active crop was not fully validated in Story 4.5.
  - Multi-monitor behavior was not validated beyond a single-monitor environment.
  - DPI scales 100%, 125%, and 200% were not validated; 150% was tested.
  - SDR display behavior was not separately validated.
  - Clipboard lock recovery/failure injection was not tested.
- Target: Story 8.4 and Story 8.5.
- Suggested acceptance criterion: Given release validation is executed, when these scenarios are not run or fail, then the release matrix records them as explicit gaps, limitations, or blockers rather than implied support.

## Accepted Decisions / No Current Action

### Sprint-status timestamp formats remain mixed for now

- Source: Epic 4 retrospective follow-through.
- Current shape: `sprint-status.yaml` may contain both date-only and timestamp-with-offset values.
- Decision: no current action; normalize only if a future tooling story needs machine-validated timestamps.
- Rationale: changing historical status metadata for cosmetic consistency adds noise without improving implementation safety.

### `MainWindow` retains a direct `CaptureService` field for now

- Source: Story 4.4 review.
- Decision: accepted as-is for the current foundation. `ICaptureCommandCoordinator` wraps command reservation, while `MainWindow` still needs `CaptureService` for current session projection and existing preview orchestration.
- Revisit trigger: if a future app-state coordinator removes the remaining projection dependency cleanly.

### Constructor-injected graphics resources fail through caller path

- Source: Story 4.5 review.
- Decision: accepted as-is. `GraphicsEngine` construction failure propagates through app startup; this is not a current active defect.
- Revisit trigger: if device-loss recovery or startup diagnostics become an explicit story.

### `ISettingsProvider` injected before full consumption

- Source: Story 4.4 review.
- Decision: accepted temporary seam. It exists so Epic 5 can consume settings through a shared abstraction.
- Revisit trigger: Story 5.2 / 5.5 should replace the stub-only usefulness with real settings shell/persistence behavior.

### `CaptureCommand` permits a null target

- Source: Story 4.2 review.
- Decision: accepted for current command shape because fullscreen/region commands may reserve target resolution for a later step.
- Revisit trigger: if command payload semantics are tightened after tray/hotkey and direct target selection settle.

### `CaptureCommandResult` is a class rather than a record

- Source: Story 4.2 review.
- Decision: accepted technical style debt. No current behavioral defect is known.
- Revisit trigger: if equality semantics become important in tests or command-result caching.

### Default switch rejects future `CaptureSessionStatus` values

- Source: Story 4.2 review.
- Decision: accepted defensive behavior for now.
- Revisit trigger: when adding a new session status, review command acceptance explicitly.

## Recently Closed

- Epic 4 retrospective created `_bmad-output/implementation-artifacts/epic-4-retro-2026-05-13.md`.
- Epic 5 guardrail follow-through created `_bmad-output/implementation-artifacts/epic-5-implementation-guardrails.md`.

## Deferred from: code review of 5-1-build-the-native-v0-main-panel.md (2026-05-17)

- Native close/minimize command affordance is still pending a later shell/tray story. Story 5.1 intentionally represents minimize/background intent without implementing tray/background behavior; revisit when Story 5.2 or Epic 7 owns shell commands.
- Full text scaling, high contrast, mixed-DPI, SDR, and multi-monitor manual validation remains future release-matrix coverage. Story 5.1 validated a single HDR 4K display at 150% DPI only.
- Add deeper automated coverage for HWND/DWM frame suppression helpers. Current Story 5.1 relies on build/manual validation for the native interop path; future coverage can extract pure style-bit planning logic or add boundary tests.

## Deferred from: code review of spec-restore-export-format-segmented-control.md (2026-05-25)

- CreateExportColorOptions allocates new list on every call — Could be static readonly field since data is static.
- "validation-scoped" jargon in accessibility text — Screen reader users won't understand this means "not functional".
- ExportColorDisplayValue hardcoded to "sRGB" — Panel-level automation name always says "Export profile: sRGB" regardless of actual selection.
- No test validates sRGB is default/active segment — Design Notes say "prefer sRGB" but no test explicitly asserts the policy.
