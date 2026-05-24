---
status: done
---

# Story 6.1: Define Output Target Policy and Result Model

Status: done

## Story

As a screenshot user,
I want capture output to follow a clear target policy,
so that clipboard, folder, and both-target behavior is predictable from settings.

## Acceptance Criteria

1. **Given** output target settings are enabled, **when** capture confirmation produces a valid image payload, **then** the output pipeline reads the shared persisted output target and attempts only the configured targets.

2. **Given** output target settings are not yet supported by the output pipeline, **when** the settings UI is reviewed, **then** the corresponding controls remain hidden, disabled, or explicitly scoped until this story enables them.

3. **Given** one or more output targets are attempted, **when** they complete, **then** the result model reports per-target success, failure, skipped, and user-facing message state.

4. **Given** output semantics are reviewed, **when** HDR preservation has not been validated, **then** the result model and UI copy describe basic usability, not HDR-preserving output.

## Tasks / Subtasks

- [x] **Task 1: Audit current output and settings seams** (AC: 1,2,3,4)
  - [x] Read `ISettingsProvider`, `LocalSettingsSnapshot`, `LocalSettingsStore`, output model files, clipboard service, capture command/release flow, and settings projection.
  - [x] Identify any duplicated output settings stubs or placeholder copy that conflicts with Epic 6 ownership.
  - [x] Confirm which controls can become active in this story and which remain scoped to later 6.x stories.

- [x] **Task 2: Define a shared output policy model from persisted settings** (AC: 1,2,4)
  - [x] Replace or retire `OutputTargetSettings` placeholder usage so output policy is derived from `ISettingsProvider` / `LocalSettingsSnapshot` values.
  - [x] Keep `Lumiere.Settings` as the owner of persisted preferences and `Lumiere.Graphics.Output` as the owner of output policy/result contracts.
  - [x] Model copy-as-image, folder target, both target, save path, timestamp naming, and after-capture inputs without implementing folder write or after-capture actions in this story.
  - [x] Preserve clipboard output as basic SDR-compatible bitmap usability and avoid HDR-preserving language.

- [x] **Task 3: Strengthen per-target output result semantics** (AC: 3,4)
  - [x] Provide a typed per-target result shape that records target, outcome, user-facing message, diagnostic detail, and artifact path only when applicable.
  - [x] Support success, failed, skipped, and partial-success aggregation without requiring string parsing.
  - [x] Ensure skipped targets are explicit when a configured setting disables that target or later story capability is not implemented.

- [x] **Task 4: Wire policy into the current output service seam conservatively** (AC: 1,3,4)
  - [x] Update `IOutputService` / `OutputRequest` consumers so configured target policy can be evaluated before attempting target work.
  - [x] Keep current clipboard implementation callable, but do not let it write when the policy says clipboard is disabled or copy-as-image is false.
  - [x] Do not implement folder file writes, folder picker, after-capture open/reveal, tray, hotkeys, or new image formats in this story.

- [x] **Task 5: Enable only honest settings UI state for Story 6.1** (AC: 2,4)
  - [x] Update `SettingsPanelProjection` so output target and copy-as-image no longer say all output behavior is pending when the model consumes those settings.
  - [x] Keep save path selection, timestamp behavior, export/color format, and after-capture controls read-only or explicitly scoped until their 6.x stories implement behavior.
  - [x] Keep UI copy clear that clipboard image output is basic usability, not validated HDR preservation.

- [x] **Task 6: Add focused hardware-independent tests** (AC: 1,2,3,4)
  - [x] Test policy derivation for clipboard, folder, both, copy-as-image false, missing save path, and later-story skipped states.
  - [x] Test output result aggregation for success, skipped, failed, and partial success.
  - [x] Test clipboard service or a pure policy wrapper skips clipboard writes when settings disable clipboard/copy-as-image.
  - [x] Test settings projection copy does not claim HDR-preserving output and only enables controls backed by Story 6.1 semantics.

- [x] **Task 7: Validate and record limits** (AC: 1,2,3,4)
  - [x] Run `dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false`.
  - [x] Run `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false`.
  - [x] Run `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false`.
  - [x] Run `dotnet format Lumiere.sln --verify-no-changes --verbosity minimal`.
  - [x] Record that real Windows clipboard behavior, folder output, after-capture behavior, and HDR output fidelity still require later stories and Windows manual validation.

### Review Findings

- No code defects found in the Story 6.1 review pass.

## Dev Notes

### Story Scope

Story 6.1 starts Epic 6 by turning output settings into a real policy/result contract. It should make output behavior predictable from shared persisted settings, but it should not finish every output target. Folder writing is Story 6.3, both-target orchestration/feedback is Story 6.4, export/color option scoping is Story 6.5, and after-capture behavior is Story 6.6.

This story may activate policy consumption for output target and copy-as-image because those are required to decide whether clipboard output should be attempted. It must keep save path selection, timestamp naming behavior, folder artifact creation, export/color choices, and after-capture open/reveal honest and scoped until their stories implement the actual behavior.

### Current Implementation State

`Lumiere.Settings` already owns persisted settings through `ISettingsProvider`, `LocalSettingsSnapshot`, `DefaultSettingsProvider`, and `LocalSettingsStore`. Defaults are clipboard target, timestamp naming enabled, copy-as-image enabled, HDR alerts enabled, no save path, and `AfterCaptureBehavior.None`.

`Lumiere.Graphics.Output` already contains `OutputTarget`, `OutputRequest`, `OutputResult`, `IOutputService`, and `CropPixelRect`. `OutputRequest` still has an `OutputTargetSettings` placeholder that says real settings would arrive in Story 5.5; this is now stale and should be replaced or retired.

`ClipboardOutputService` implements `IOutputService` and currently executes clipboard conversion/write for any valid request. It returns a simple `OutputResult` with `ClipboardOutcome`, `FolderOutcome`, `IsSuccess`, and messages. The service performs FP16 to BGRA8 PNG conversion for basic clipboard usability; this is not validated HDR-preserving output.

`SettingsPanelProjection.ReadOnly` currently marks all output controls as read-only with `"Output behavior arrives in Epic 6"`. Story 6.1 should update that wording where policy is real, while keeping controls unavailable where execution semantics are still absent.

### Architecture Compliance

- `Lumiere.Settings` owns local preference persistence, defaults, validation, and migration semantics.
- `Lumiere.Graphics.Output` owns output policy/result contracts and output conversion policy vocabulary.
- `Lumiere.Graphics.Clipboard` owns the current Windows clipboard output implementation.
- `Lumiere.App.Core` may project settings/output state for UI, but it must not own output conversion, folder write, clipboard interop, D3D resources, or native lifetimes.
- Do not add web, WPF, WinForms, cross-platform screenshot, database, telemetry, or cloud dependencies.
- Use `ILogger` via existing logging patterns; never add `Console.WriteLine`.
- Do not log captured pixels, raw frame data, or screenshot content.

### Testing Guidance

Keep new tests hardware-independent where possible and place them under `tests/Lumiere.Graphics.Tests` following the existing pattern. Prefer pure tests for policy derivation, result aggregation, settings projection, and skip/failure semantics. Real Windows clipboard, folder filesystem permissions, HDR fidelity, WGC/DXGI timing, and target-app compatibility remain Windows manual validation evidence, not unit-test claims.

### Regression Risks

- Do not silently keep using `OutputTargetSettings.Default` and thereby ignore persisted settings.
- Do not make folder/both UI look fully supported before folder writes and both-target orchestration exist.
- Do not treat a skipped target as success unless the aggregate result clearly reports the skip and why.
- Do not describe PNG clipboard output as HDR-preserving.
- Do not introduce a parallel settings source inside `Lumiere.App` or `Lumiere.Graphics`.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 6.1`] - Story requirements and acceptance criteria.
- [Source: `_bmad-output/planning-artifacts/prd.md#Functional Requirements`] - FR22, FR24, FR25, FR28, FR29 output requirements.
- [Source: `_bmad-output/planning-artifacts/architecture.md#Integration Points`] - Output consumes crop/frame payloads, settings, and validation-aware conversion semantics.
- [Source: `_bmad-output/project-context.md`] - HDR/output claim discipline, module boundaries, and validation rules.
- [Source: `_bmad-output/implementation-artifacts/5-5-persist-local-settings-across-launches.md`] - Settings persistence and shared source-of-truth context.
- [Source: `_bmad-output/implementation-artifacts/5-6-show-native-about-and-version-information.md`] - Latest Epic 5 status and honest HDR copy precedent.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-23: Story created from Epic 6 backlog after confirming sprint status had no ready-for-dev 6.x story files.
- 2026-05-23: Existing output contracts, clipboard service, settings provider/store, settings projection, PRD, architecture, and previous Epic 5 story context reviewed for story creation.
- 2026-05-23: Story moved to in-progress for BMad dev-story execution.
- 2026-05-23: Added `OutputPolicy`, per-target `OutputTargetResult`, aggregate `OutputResult` semantics, settings-derived output request policy, and clipboard policy skip handling.
- 2026-05-23: Updated settings projection copy so policy-backed output target/copy-as-image no longer reads as wholly pending while later 6.x behavior remains scoped.
- 2026-05-23: Validation passed after restoring with network approval: restore succeeded; build succeeded with 0 warnings/errors; `Lumiere.Graphics.Tests` passed 228/228; format verification passed. A parallel build/test attempt briefly failed with an `obj` file lock, then the test was rerun alone and passed.
- 2026-05-23: Review pass found no code defects. Copy wording was polished and `Lumiere.Graphics.Tests` plus format verification were rerun successfully.

### Completion Notes List

- Story context created with explicit guardrails for shared settings consumption, typed per-target results, honest output copy, and scoped later-story behavior.
- Output requests now carry a pure `OutputPolicy` derived from shared settings at the app boundary without adding a Graphics -> Settings dependency.
- Clipboard output now skips without writing when configured policy disables clipboard or copy-as-image.
- Output results now expose typed per-target outcomes and aggregate partial-success/skipped/failure messages.
- Folder artifacts, timestamped file naming, both-target orchestration, export/color formats, and after-capture open/reveal remain explicitly scoped to later Epic 6 stories.
- Validation level: Windows CI-pass for hardware-independent output policy/result/projection behavior. Real Windows clipboard behavior and HDR output fidelity still need manual validation.
- Story marked done after review and final validation.

### File List

- `_bmad-output/implementation-artifacts/6-1-define-output-target-policy-and-result-model.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Lumiere.App.Core/SettingsPanelProjection.cs`
- `src/Lumiere.App/MainWindow.xaml.cs`
- `src/Lumiere.Graphics/Clipboard/ClipboardOutputService.cs`
- `src/Lumiere.Graphics/Output/OutputRequest.cs`
- `src/Lumiere.Graphics/Output/OutputResult.cs`
- `tests/Lumiere.Graphics.Tests/App/SettingsPanelProjectionTests.cs`
- `tests/Lumiere.Graphics.Tests/Output/OutputPolicyTests.cs`
- `tests/Lumiere.Graphics.Tests/Output/OutputResultTests.cs`

### Change Log

- 2026-05-23: Created ready-for-dev story context for Story 6.1.
- 2026-05-23: Started implementation.
- 2026-05-23: Implemented output target policy/result model and moved story to review.
- 2026-05-23: Review found no defects and story was marked done.
