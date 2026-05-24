---
status: done
---

# Story 6.3: Implement Folder Output with Save Path and Timestamp Naming

Status: done

## Story

As a screenshot user,
I want captures saved to my selected folder with safe names,
so that file output is reliable and does not overwrite previous captures.

## Acceptance Criteria

1. **Given** folder output is enabled, **when** a valid fullscreen or region capture completes, **then** Lumiere writes the image artifact to the configured save folder.

2. **Given** timestamp naming is enabled, **when** a file is created, **then** the filename uses deterministic invariant formatting and avoids overwriting existing files.

3. **Given** the save path is missing, inaccessible, or permission denied, **when** file output is attempted, **then** the app reports recoverable failure feedback and does not silently drop output.

## Tasks / Subtasks

- [x] **Task 1: Audit current output encoding and file settings** (AC: 1,2,3)
  - [x] Read Story 6.1/6.2 output policy/result contracts and `ClipboardOutputService` PNG encoding path.
  - [x] Read settings save path and timestamp naming persistence tests.
  - [x] Identify the narrowest shared artifact writer or service seam that avoids duplicating conversion code.

- [x] **Task 2: Add deterministic filename and folder validation logic** (AC: 2,3)
  - [x] Implement invariant timestamp naming for file artifacts.
  - [x] Avoid overwriting by appending a deterministic suffix when a target path already exists.
  - [x] Treat missing/blank save path, inaccessible folder, and permission errors as recoverable output failures.

- [x] **Task 3: Implement folder output service path** (AC: 1,3)
  - [x] Attempt folder output only when policy target includes folder.
  - [x] Write PNG image artifacts to configured save path using the approved output conversion path.
  - [x] Return typed per-target success/failure/skipped results including artifact path on success.

- [x] **Task 4: Keep UI state honest for folder output** (AC: 1,2,3)
  - [x] Update settings projection wording so save path and timestamp naming are scoped to implemented folder output semantics.
  - [x] Keep picker/edit controls read-only if no write API exists yet, but do not describe folder output itself as unavailable once service behavior exists.

- [x] **Task 5: Add focused tests** (AC: 1,2,3)
  - [x] Test invariant timestamp filename generation and collision suffixing.
  - [x] Test missing save path and inaccessible folder produce failed folder outcomes.
  - [x] Test folder-disabled policy skips without writing.
  - [x] Test successful folder write reports artifact path and does not overwrite.

- [x] **Task 6: Validate and record limits** (AC: 1,2,3)
  - [x] Run `dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false`.
  - [x] Run `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false`.
  - [x] Run `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false`.
  - [x] Run `dotnet format Lumiere.sln --verify-no-changes --verbosity minimal`.
  - [x] Record that real Windows filesystem permissions, long-path behavior, and HDR fidelity still require Windows manual validation.

### Review Findings

- No code defects found in the Story 6.3 review pass.

## Dev Notes

### Story Scope

Story 6.3 implements file artifact output for folder target and timestamp naming. It does not implement both-target orchestration feedback beyond folder target results, folder picker UI, after-capture open/reveal, export/color options, tray, or hotkeys.

### Previous Story Intelligence

Story 6.1 established `OutputPolicy`, `OutputTargetResult`, and aggregate `OutputResult`. Story 6.2 added a policy-testable clipboard execution seam. Reuse these contracts and avoid another output status model.

### Architecture Compliance

- `Lumiere.Graphics.Output` should own filename policy, folder output contracts, and artifact result semantics.
- `Lumiere.Graphics.Clipboard` currently owns the native FP16-to-BGRA8 PNG conversion path. If sharing conversion is needed, move logic into a `Lumiere.Graphics.Output` helper instead of duplicating it.
- `Lumiere.Settings` remains the source of `SavePath` and `TimestampNaming`.
- Folder output creates user artifacts only; do not add a database or history store.
- Output artifacts are basic PNG usability unless future encoder/metadata policy validates HDR preservation.

### Testing Guidance

Use temp directories for file output tests and pure tests for filename policy. Avoid requiring WGC, real swap-chain textures, HDR displays, or Windows shell UI. Real permission-denied behavior can be tested via abstraction or controlled invalid paths where reliable; do not overclaim manual Windows validation from unit tests.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 6.3`] - Story requirements and acceptance criteria.
- [Source: `_bmad-output/implementation-artifacts/6-1-define-output-target-policy-and-result-model.md`] - Output policy/result model.
- [Source: `_bmad-output/implementation-artifacts/6-2-implement-configured-clipboard-output.md`] - Configured clipboard output seam.
- [Source: `_bmad-output/project-context.md`] - Output validation and HDR claim discipline.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-23: Story created and moved to in-progress after Story 6.2 completed.
- 2026-05-23: Added folder output service, deterministic path policy, shared PNG encoder seam through clipboard output, and configured output composition.
- 2026-05-23: Validation passed: restore up to date; build succeeded with 0 warnings/errors; `Lumiere.Graphics.Tests` passed 242/242; format verification passed.

### Completion Notes List

- Folder target output now writes PNG artifacts to the configured save path when folder output is selected.
- Timestamp naming uses invariant `yyyyMMdd-HHmmss-fff` formatting and appends deterministic suffixes to avoid overwrites.
- Missing save path, unavailable folder, write failure, and disabled folder target return typed recoverable folder outcomes.
- App composition now routes configured output through clipboard and folder services.
- Validation level: Windows CI-pass for hardware-independent filename and folder service behavior. Real Windows filesystem permissions, long paths, and HDR output fidelity still require manual validation.
- Story marked done after review and validation.

### File List

- `_bmad-output/implementation-artifacts/6-3-implement-folder-output-with-save-path-and-timestamp-naming.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Lumiere.App/App.xaml.cs`
- `src/Lumiere.Graphics/Clipboard/ClipboardOutputService.cs`
- `src/Lumiere.Graphics/Output/ConfiguredOutputService.cs`
- `src/Lumiere.Graphics/Output/FolderOutputService.cs`
- `src/Lumiere.Graphics/Output/IOutputPngEncoder.cs`
- `src/Lumiere.Graphics/Output/OutputFolderPathPolicy.cs`
- `src/Lumiere.Graphics/Output/OutputResult.cs`
- `tests/Lumiere.Graphics.Tests/Output/FolderOutputServiceTests.cs`
- `tests/Lumiere.Graphics.Tests/Output/OutputFolderPathPolicyTests.cs`

### Change Log

- 2026-05-23: Created story context and started implementation.
- 2026-05-23: Implemented configured folder output with safe timestamp naming and marked story done.
