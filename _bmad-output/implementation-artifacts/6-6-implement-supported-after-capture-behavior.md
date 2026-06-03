---
status: done
---

# Story 6.6: Implement Supported After-Capture Behavior

Status: done

## Story

As a screenshot user,
I want after-capture behavior to apply only when there is an artifact to open or reveal,
so that clipboard-only captures do not trigger confusing no-op actions.

## Acceptance Criteria

1. **Given** folder output creates a file artifact, **when** supported after-capture behavior is enabled, **then** Lumiere opens or reveals the output according to the implemented setting.

2. **Given** output target is clipboard-only, **when** after-capture behavior is evaluated, **then** the app performs no unsupported open/reveal action and communicates completion through normal feedback.

3. **Given** after-capture action fails, **when** the failure is handled, **then** output success remains separate from open/reveal failure in the result model.

## Tasks / Subtasks

- [x] **Task 1: Model after-capture behavior in output results** (AC: 1,2,3)
  - [x] Add a typed after-capture outcome that is distinct from output target success/failure.
  - [x] Map persisted setting values to supported output actions without taking a dependency on `Lumiere.Settings` from low-level output code.
  - [x] Keep the default and clipboard-only path as no action.

- [x] **Task 2: Execute after-capture only for file artifacts** (AC: 1,2)
  - [x] Add a narrow shell/open abstraction so platform launching details stay outside app orchestration.
  - [x] Run open/reveal only when output produced a folder artifact path and the setting requests a supported action.
  - [x] Preserve normal output feedback for clipboard-only or missing-artifact results.

- [x] **Task 3: Preserve output success when after-capture fails** (AC: 3)
  - [x] Capture shell action failure as an after-capture failure, not as a failed folder output.
  - [x] Include technical diagnostics without screenshot payloads.
  - [x] Ensure aggregate output `IsSuccess` still reflects target output success.

- [x] **Task 4: Enable honest settings projection** (AC: 1,2)
  - [x] Update settings projection/UI copy so after-capture behavior is no longer pending when output artifacts are supported.
  - [x] Keep settings editing read-only until a future writer/picker story implements changes.
  - [x] Ensure clipboard-only help text does not imply an open/reveal action will run.

- [x] **Task 5: Add focused tests and validation** (AC: 1,2,3)
  - [x] Test open and reveal actions run for folder artifacts.
  - [x] Test clipboard-only output performs no after-capture action.
  - [x] Test after-capture action failure remains separate from output success.
  - [x] Run `dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false`.
  - [x] Run `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false`.
  - [x] Run `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false`.
  - [x] Run `dotnet format Lumiere.sln --verify-no-changes --verbosity minimal`.

### Review Findings

- No code defects found in the Story 6.6 review pass.

## Dev Notes

### Validation Level

**Windows CI-pass** — Automated gates pass on Windows. After-capture routing tested. Known gap: Explorer reveal/open behavior not validated on Windows hardware.

### Story Scope

Story 6.6 implements the supported post-output action for file artifacts only. It must not add a gallery, editor workflow, folder picker, output format conversion, tray behavior, or global hotkeys.

### Previous Story Intelligence

Stories 6.1-6.4 established settings-backed output policy, per-target results, folder artifacts with `ArtifactPath`, and both-target aggregate feedback. Story 6.5 kept advanced color/export behavior unavailable and documented that current output is basic PNG/clipboard/file usability, not validated HDR preservation.

### Architecture Guardrails

- Keep native shell launching behind a narrow infrastructure boundary.
- Keep output orchestration and result modeling in `Lumiere.Graphics.Output`.
- Do not reference `Lumiere.Settings` from graphics output types; map raw persisted values into output-owned behavior.
- Preserve cancellation semantics and deterministic output result reporting.
- Use `ILogger` via `LumiereLoggerFactory`; do not write to console.

### Validation Notes

Automated tests can validate routing, result modeling, and failure separation. Real Windows shell open/reveal behavior requires Windows manual validation before release claims.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 6.6`] - Story requirements and acceptance criteria.
- [Source: `_bmad-output/project-context.md`] - Module boundary, logging, and validation-level rules.
- [Source: `_bmad-output/implementation-artifacts/6-5-scope-export-and-color-format-options-honestly.md`] - Current output fidelity and validation limits.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-23: Story created from Epic 6 backlog and moved to ready-for-dev.
- 2026-05-23: Story moved to in-progress and after-capture result/service design started.
- 2026-05-23: Implemented `AfterCaptureOutputService`, typed after-capture result state, Windows shell artifact abstraction, App wiring, and settings projection updates.
- 2026-05-23: Validation passed: restore succeeded with network approval after sandbox denial; build succeeded with 0 warnings/errors; `Lumiere.Graphics.Tests` passed 260/260; format verification passed.

### Completion Notes List

- Supported after-capture behavior now runs only when folder output produces a file artifact and the policy requests Open or Reveal.
- Clipboard-only output records a skipped after-capture result without invoking shell actions.
- Shell action failure is represented as `AfterCaptureOutcome.Failed` and does not change target output success.
- Settings projection and static UI copy now describe supported artifact-scoped behavior instead of pending Epic 6 behavior.
- Validation level: Windows CI-pass for routing/result/projection guardrails. Real Windows shell open/reveal behavior still requires Windows manual validation evidence.

### File List

- `_bmad-output/implementation-artifacts/6-6-implement-supported-after-capture-behavior.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Lumiere.App.Core/SettingsPanelProjection.cs`
- `src/Lumiere.App/App.xaml.cs`
- `src/Lumiere.App/MainWindow.xaml.cs`
- `src/Lumiere.Graphics/Output/AfterCaptureOutputService.cs`
- `src/Lumiere.Graphics/Output/OutputRequest.cs`
- `src/Lumiere.Graphics/Output/OutputResult.cs`
- `src/Lumiere.Infrastructure/Interop/IArtifactShellAction.cs`
- `src/Lumiere.Infrastructure/Interop/WindowsArtifactShellAction.cs`
- `tests/Lumiere.Graphics.Tests/App/SettingsPanelProjectionTests.cs`
- `tests/Lumiere.Graphics.Tests/Output/AfterCaptureOutputServiceTests.cs`
- `tests/Lumiere.Graphics.Tests/Output/OutputPolicyTests.cs`

### Change Log

- 2026-05-23: Created story context for supported after-capture behavior.
- 2026-05-23: Started implementation.
- 2026-05-23: Implemented supported after-capture behavior and marked story done.
