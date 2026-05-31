---
status: done
---

# Story 7.3: Register Global Capture Hotkeys

Status: done

## Story

As a screenshot user,
I want global shortcuts for fullscreen and region capture,
so that I can trigger Lumiere from my current workflow.

## Acceptance Criteria

1. **Given** shortcut settings are available, **when** Lumiere starts or settings change, **then** fullscreen and region hotkeys are registered with Windows where possible.

2. **Given** a registered fullscreen or region hotkey is pressed, **when** no conflicting capture session is active, **then** the corresponding capture command routes through the shared session contract.

3. **Given** a shortcut is invalid, conflicts, or cannot register, **when** registration is attempted, **then** Lumiere records the failure, provides recoverable feedback, and preserves or restores a safe shortcut state.

## Tasks / Subtasks

- [x] **Task 1: Add pure hotkey registration planning** (AC: 1,3)
  - [x] Parse persisted shortcut strings into typed global hotkey gestures.
  - [x] Skip empty, invalid, unsupported, and conflicting shortcuts safely.
  - [x] Keep shortcut registration status visible through settings projection.

- [x] **Task 2: Add native Windows global hotkey registration boundary** (AC: 1,3)
  - [x] Add a narrow `IGlobalHotkeyRegistrar` abstraction.
  - [x] Implement native `RegisterHotKey`/`UnregisterHotKey` and message-window ownership in `Lumiere.Infrastructure.Interop`.
  - [x] Return per-command registration results for structured diagnostics instead of failing app startup.

- [x] **Task 3: Route hotkeys through shared capture commands** (AC: 2)
  - [x] Dispatch hotkey events onto the WinUI UI thread.
  - [x] Reuse the existing fullscreen/region capture command path.
  - [x] Preserve the existing single-session guard so hotkeys cannot start conflicting sessions.

- [x] **Task 4: Add focused tests and validation** (AC: 1,2,3)
  - [x] Test valid shortcut parsing and virtual-key mapping.
  - [x] Test empty, invalid, and conflicting shortcuts are skipped safely.
  - [x] Run repository validation gates and record Windows manual-validation limits.

## Dev Notes

### Story Scope

Story 7.3 registers global capture hotkeys from existing persisted shortcut settings. Shortcut editing remains read-only in this MVP slice; empty default settings mean no hotkeys are registered until settings contain valid shortcut values.

### Architecture Guardrails

- Raw Win32 hotkey registration and message-window ownership stay in `Lumiere.Infrastructure.Interop`.
- Pure shortcut parsing and conflict detection stay in `Lumiere.App.Core`.
- `Lumiere.App` routes hotkey intent through the existing capture command coordinator.
- Real global hotkey behavior requires Windows manual validation.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.3`] - Story requirements and acceptance criteria.
- [Source: `_bmad-output/project-context.md`] - Boundary, shared state, and validation-level rules.
- [Source: `_bmad-output/implementation-artifacts/7-1-add-tray-menu-with-status-and-commands.md`] - Shared command routing foundation.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-25: Story created from Epic 7 backlog because no 7.3 story file existed.
- 2026-05-25: Added `GlobalHotkeyRegistrationPlan`, pure shortcut parsing, conflict detection, and settings projection updates.
- 2026-05-25: Added `IGlobalHotkeyRegistrar` and `WindowsGlobalHotkeyRegistrar` using a message-only window and native `RegisterHotKey`.
- 2026-05-25: Wired global hotkey events to the existing main-window capture command path on the UI dispatcher.
- 2026-05-25: Full validation passed: restore succeeded; build succeeded with 0 warnings/errors; `Lumiere.Graphics.Tests` passed 273/273; format verification passed after CRLF normalization.

### Completion Notes List

- Valid persisted shortcuts such as `Ctrl+Shift+F` and `Alt+F12` can be registered with Windows.
- Empty, invalid, unsupported, conflicting, or OS-rejected shortcuts are skipped and logged without blocking startup.
- Hotkey-triggered captures reuse the same single-session guard as main-window and tray captures.
- Validation level: Windows CI-pass for parsing/projection/build/test guardrails. Real OS-level hotkey registration, conflict behavior, and keypress routing require Windows manual validation.

### File List

- `_bmad-output/implementation-artifacts/7-3-register-global-capture-hotkeys.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Lumiere.App.Core/GlobalHotkeyRegistrationPlan.cs`
- `src/Lumiere.App.Core/SettingsPanelProjection.cs`
- `src/Lumiere.App/App.xaml.cs`
- `src/Lumiere.App/MainWindow.xaml.cs`
- `src/Lumiere.Infrastructure/Interop/IGlobalHotkeyRegistrar.cs`
- `src/Lumiere.Infrastructure/Interop/WindowsGlobalHotkeyRegistrar.cs`
- `tests/Lumiere.Graphics.Tests/App/GlobalHotkeyRegistrationPlanTests.cs`
- `tests/Lumiere.Graphics.Tests/App/SettingsPanelProjectionTests.cs`

### Change Log

- 2026-05-25: Created story context and implemented global hotkey planning, native registration, routing, and tests.
