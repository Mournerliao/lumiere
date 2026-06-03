---
status: done
---

# Story 5.5: Persist Local Settings Across Launches

Status: done

## Story

As a screenshot user,
I want Lumiere to remember my settings,
so that capture entry points obey the same preferences every time I launch the app.

## Acceptance Criteria

1. **Given** the user changes supported settings, **when** the app is closed and reopened, **then** fullscreen shortcut, region shortcut, HDR alert preference, output target, save path, timestamp naming, copy-as-image, and supported after-capture preferences are restored.

2. **Given** settings data is missing, invalid, or from an older schema, **when** settings load, **then** Lumiere falls back to safe defaults and records diagnostics without blocking app startup.

3. **Given** main panel, future tray, future hotkeys, and output pipeline need preferences, **when** they read settings, **then** they consume the same local settings source rather than maintaining UI-local state.

## Tasks / Subtasks

- [x] **Task 1: Audit current settings ownership and consumers** (AC: 1,2,3)
  - [x] Read `ISettingsProvider`, `DefaultSettingsProvider`, `IHdrAlertSettingsWriter`, App composition, settings projection, and current output request/settings types.
  - [x] Confirm current HDR alert writes are in-session only and must become durable without moving persistence into `MainWindow`.
  - [x] Confirm output, shortcut, and after-capture preferences remain settings-owned even where later epics consume behavior.

- [x] **Task 2: Add durable settings snapshot and storage in `Lumiere.Settings`** (AC: 1,2)
  - [x] Define a schema-versioned local settings snapshot covering output target, save path, timestamp naming, copy-as-image, HDR alerts, fullscreen shortcut, region shortcut, and after-capture behavior.
  - [x] Store settings under a local app data path by default, with testable path injection.
  - [x] Load safe defaults when the file is missing, invalid JSON, invalid enum data, or an unsupported schema version is encountered.
  - [x] Record structured diagnostics for fallback and save failures without blocking startup.

- [x] **Task 3: Add a deliberate write/persistence counterpart** (AC: 1,3)
  - [x] Keep `ISettingsProvider` as the read source.
  - [x] Keep HDR alert writes behind the existing writer seam while backing it with durable storage.
  - [x] Avoid adding persistence, validation, or file IO to `MainWindow.xaml.cs`.

- [x] **Task 4: Wire App startup to the persisted provider** (AC: 1,3)
  - [x] Compose the persisted settings provider in `App.xaml.cs`.
  - [x] Ensure main panel shortcuts and settings projections read from the same provider instance.
  - [x] Keep output pipeline policy unchanged; configured output behavior remains Epic 6.

- [x] **Task 5: Add focused hardware-independent tests** (AC: 1,2,3)
  - [x] Verify defaults are safe when no settings file exists.
  - [x] Verify a changed HDR alert preference is saved and restored by a new provider instance.
  - [x] Verify all persisted fields round-trip through the store.
  - [x] Verify invalid JSON, invalid enum data, and unsupported schema versions fall back to safe defaults with diagnostics.
  - [x] Verify the provider still implements the shared read/write settings seams.

- [x] **Task 6: Validate and record limits** (AC: 1,2,3)
  - [x] Run `dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false`.
  - [x] Run `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false`.
  - [x] Run `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false`.
  - [x] Run `dotnet format Lumiere.sln --verify-no-changes --verbosity minimal`.
  - [x] Record that Windows manual validation is still needed for actual app relaunch behavior, rendered settings UX, and future tray/hotkey/output consumers.

### Review Findings

- [x] [Review][Patch] Add provider-level round-trip coverage for all persisted settings fields; store-level round-trip alone did not prove `ISettingsProvider` restores every field.

## Dev Notes

### Validation Level

**Windows CI-pass** — Automated gates pass on Windows. JSON persistence logic tested; app relaunch persistence not manually validated.

### Story Scope

Story 5.5 replaces the in-session settings stub with a local persisted settings source. It does not implement shortcut editing, global hotkey registration, configured output behavior, folder writes, save-path picking, timestamp file naming, copy-as-image execution, or after-capture actions.

Persistence belongs in `Lumiere.Settings`. `MainWindow.xaml.cs` may project settings and route existing toggle events, but it must not own settings file paths, schema migration, validation, or defaults.

### Current Implementation State

The current `DefaultSettingsProvider` returns hardcoded values and keeps `HdrAlertsEnabled` only in memory. `MainWindow` receives the same instance as both `ISettingsProvider` and `IHdrAlertSettingsWriter`, so durable HDR alert writes can be introduced without changing the UI event boundary.

`SettingsPanelProjection` already reads shortcuts, HDR alerts, output target, save path, timestamp naming, and copy-as-image from `ISettingsProvider`. Story 5.5 should keep that projection pure.

`OutputRequest.OutputTargetSettings` is a placeholder and configured output behavior is deferred to Epic 6. Do not make the current clipboard service obey folder/both preferences in this story.

### Architecture Compliance

- `Lumiere.Settings` owns defaults, persistence, validation, and fallback diagnostics.
- `Lumiere.App` composes and reads the provider only.
- `Lumiere.Graphics.Output` keeps output target/result models; Epic 6 owns behavior.
- No WGC, D3D11, DXGI, overlay, tray, hotkey, or low-level file/folder picker logic should change.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.5`] - Story requirements and acceptance criteria.
- [Source: `_bmad-output/implementation-artifacts/epic-5-implementation-guardrails.md#Story 5.5: Persist Local Settings Across Launches`] - Persistence ownership guardrails.
- [Source: `_bmad-output/implementation-artifacts/5-4-add-output-preference-settings-ui.md`] - Previous story projection and pending-output patterns.
- [Source: `_bmad-output/project-context.md`] - Settings source-of-truth, boundary, logging, and validation rules.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-23: Story created from Epic 5 backlog and moved to in-progress for BMad dev-story execution.
- 2026-05-23: RED tests added for settings store fallback, round-trip persistence, and durable HDR alert writes; initial compile failed because store/snapshot types did not exist.
- 2026-05-23: Implemented `LocalSettingsStore`, `LocalSettingsSnapshot`, after-capture preference enum, durable `DefaultSettingsProvider`, settings logger category, and App startup wiring.
- 2026-05-23: Narrow settings tests passed: 17 tests, 0 failed.
- 2026-05-23: Full validation passed after CRLF formatting: `dotnet restore` passed with network permission; `dotnet build` passed with 0 warnings/errors; `Lumiere.Graphics.Tests` passed 209/209; `dotnet format --verify-no-changes --no-restore` passed.
- 2026-05-23: Code-review patch added provider-level full persisted settings coverage. Re-validation passed: `dotnet build` 0 warnings/errors, `Lumiere.Graphics.Tests` 210/210, and `dotnet format --verify-no-changes --no-restore`.

### Completion Notes List

- Story context created; implementation in progress.
- Implemented schema-versioned local JSON settings persistence under `%LOCALAPPDATA%\Lumiere\settings.json` by default, with injectable paths for tests.
- Missing, invalid, and unsupported settings files fall back to safe defaults and report structured fallback diagnostics.
- Existing HDR alert writer now persists across provider instances without adding settings persistence to `MainWindow`.
- Validation level: Windows CI-pass for hardware-independent settings persistence tests. Windows manual validation remains needed for actual app relaunch behavior and rendered settings UX.
- Code-review pass found no remaining defects after adding provider-level full-field persistence coverage.

### File List

- `_bmad-output/implementation-artifacts/5-5-persist-local-settings-across-launches.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Lumiere.App/App.xaml.cs`
- `src/Lumiere.Infrastructure/Diagnostics/LogCategories.cs`
- `src/Lumiere.Settings/AfterCaptureBehavior.cs`
- `src/Lumiere.Settings/DefaultSettingsProvider.cs`
- `src/Lumiere.Settings/ISettingsProvider.cs`
- `src/Lumiere.Settings/LocalSettingsLoadResult.cs`
- `src/Lumiere.Settings/LocalSettingsSnapshot.cs`
- `src/Lumiere.Settings/LocalSettingsStore.cs`
- `src/Lumiere.Settings/Lumiere.Settings.csproj`
- `tests/Lumiere.Graphics.Tests/App/SettingsPanelProjectionTests.cs`
- `tests/Lumiere.Graphics.Tests/Settings/DefaultSettingsProviderTests.cs`
- `tests/Lumiere.Graphics.Tests/Settings/LocalSettingsStoreTests.cs`

### Change Log

- 2026-05-23: Created ready-for-dev story context and started implementation.
- 2026-05-23: Added durable local settings persistence and focused tests.
- 2026-05-23: Moved story to review after all tasks and validation gates completed.
- 2026-05-23: Addressed review coverage gap and marked story done.
