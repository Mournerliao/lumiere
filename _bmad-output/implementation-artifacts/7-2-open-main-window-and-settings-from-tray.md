---
status: done
---

# Story 7.2: Open Main Window and Settings from Tray

Status: done

## Story

As a screenshot user,
I want tray commands to open Lumiere or settings,
so that background operation still gives me access to configuration.

## Acceptance Criteria

1. **Given** the tray menu is open, **when** the user selects Open Lumiere, **then** the main window is shown or activated without creating a second app state.

2. **Given** the tray menu is open, **when** the user selects Settings, **then** the settings surface opens through the same settings state used by the main window.

3. **Given** a capture session is active, **when** tray open-window or open-settings commands run, **then** they do not interrupt the session unless the user explicitly cancels.

## Tasks / Subtasks

- [x] **Task 1: Route tray navigation commands to the existing window** (AC: 1,2)
  - [x] Handle `Open Lumiere` through the existing `MainWindow` instance.
  - [x] Handle `Settings` through the existing settings shell projection and provider state.
  - [x] Avoid creating a second `MainWindow`, settings provider, or capture coordinator.

- [x] **Task 2: Preserve active capture state while opening surfaces** (AC: 3)
  - [x] Restore and activate the app window without stopping preview or closing the overlay.
  - [x] Open settings by applying the same `AppShellView.Settings` route used by the main settings button.
  - [x] Keep capture cancellation explicit and separate from tray navigation commands.

- [x] **Task 3: Keep command delivery thread-safe** (AC: 1,2,3)
  - [x] Dispatch tray command handling onto the WinUI dispatcher before mutating app state.
  - [x] Log dropped dispatcher work through structured logging.
  - [x] Preserve deterministic tray disposal during main-window close.

- [x] **Task 4: Validate shared projection behavior** (AC: 1,2,3)
  - [x] Reuse `AppShellProjection` and `SettingsPanelProjection` rather than creating tray-local settings state.
  - [x] Confirm active capture tray projection keeps navigation enabled.
  - [x] Run story validation gates and record manual-validation limits.

## Dev Notes

### Validation Level

**Windows manual-pass** — Automated gates pass. Windows manual validation completed by Dana on 2026-05-26.

### Story Scope

Story 7.2 wires tray navigation commands to the already-running main window and settings shell. It does not implement minimize-to-tray policy, global hotkey registration, or final quit cleanup; those remain in later Epic 7 stories.

### Previous Story Intelligence

Story 7.1 introduced a narrow tray abstraction and typed command events. Story 7.2 consumes those events in `MainWindow` and keeps state changes on the UI dispatcher.

### Architecture Guardrails

- Settings state must continue to come from the shared `ISettingsProvider`.
- Open-window/open-settings commands must not stop capture, dispose overlay resources, or recreate capture/session services.
- Win32 tray details remain in infrastructure; app orchestration handles command intent only.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.2`] - Story requirements and acceptance criteria.
- [Source: `_bmad-output/project-context.md`] - Shared settings, UI-thread, and validation-level rules.
- [Source: `_bmad-output/implementation-artifacts/7-1-add-tray-menu-with-status-and-commands.md`] - Tray command abstraction created for this epic.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-25: Story created from Epic 7 backlog because no 7.2 story file existed.
- 2026-05-25: Added tray command handling for open-main, open-settings, capture commands, and quit on the existing window.
- 2026-05-25: Ensured tray navigation restores/activates the window and applies the existing shell view without interrupting capture state.
- 2026-05-25: `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false` passed with 0 warnings/errors before full validation.
- 2026-05-25: Full validation passed: restore succeeded; build succeeded with 0 warnings/errors; `Lumiere.Graphics.Tests` passed 265/265; format verification passed after CRLF normalization.

### Completion Notes List

- `Open Lumiere` restores and activates the existing main window and applies the main shell view.
- `Settings` restores and activates the existing window, then opens the existing settings shell backed by the shared settings provider.
- Tray navigation commands are dispatched to the WinUI UI thread and do not call preview stop, overlay close, or capture cancellation.
- Validation level: Windows CI-pass for projection/routing/build guardrails. Real tray activation, minimized-window restoration, focus behavior, and active-capture interaction require Windows manual validation.
- **Manual validation completed:** 2026-05-26. Tray activation, window restoration, and focus behavior verified on Windows hardware.

### File List

- `_bmad-output/implementation-artifacts/7-2-open-main-window-and-settings-from-tray.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Lumiere.App.Core/TrayMenuProjection.cs`
- `src/Lumiere.App/App.xaml.cs`
- `src/Lumiere.App/MainWindow.xaml.cs`
- `src/Lumiere.Infrastructure/Interop/ITrayMenu.cs`
- `src/Lumiere.Infrastructure/Interop/TrayMenuSnapshot.cs`
- `src/Lumiere.Infrastructure/Interop/WindowsTrayMenu.cs`
- `tests/Lumiere.Graphics.Tests/App/TrayMenuProjectionTests.cs`

### Change Log

- 2026-05-25: Created story context and implemented tray open-main/open-settings routing through the existing window and settings state.
