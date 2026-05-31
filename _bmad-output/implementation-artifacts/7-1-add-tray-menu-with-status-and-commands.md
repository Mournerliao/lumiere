---
status: done
---

# Story 7.1: Add Tray Menu with Status and Commands

Status: done

## Story

As a screenshot user,
I want a compact tray menu with Lumiere status and capture commands,
so that I can use Lumiere without bringing the main window forward.

## Acceptance Criteria

1. **Given** Lumiere is running, **when** the user opens the tray menu, **then** the menu shows Lumiere identity, HDR status summary, fullscreen capture, region capture, shortcut labels, open main window, settings, and quit.

2. **Given** capture is active, **when** the tray menu is opened, **then** capture commands reflect the active or disabled state and cannot start a conflicting session.

3. **Given** tray UI is implemented, **when** native ownership is reviewed, **then** Win32 tray details remain in infrastructure boundaries and command routing remains in app orchestration.

## Tasks / Subtasks

- [x] **Task 1: Add tray menu projection** (AC: 1,2)
  - [x] Project Lumiere identity, HDR status summary, capture commands, shortcut labels, open-window/settings, and quit.
  - [x] Reuse existing `MainPanelProjection` readiness/action vocabulary instead of adding parallel status strings.
  - [x] Disable and mark capture commands active while a capture session is selecting, initializing, capturing, or degraded.

- [x] **Task 2: Keep Win32 tray ownership inside infrastructure** (AC: 1,3)
  - [x] Add a narrow `ITrayMenu` abstraction with typed command events and immutable menu snapshots.
  - [x] Implement native `Shell_NotifyIcon` and popup menu details in `Lumiere.Infrastructure.Interop`.
  - [x] Keep HWND/message-window/popup-menu ownership out of `Lumiere.App.Core` and low-level capture/graphics modules.

- [x] **Task 3: Route tray capture commands through app orchestration** (AC: 2,3)
  - [x] Wire tray commands to the existing main-window capture command path.
  - [x] Dispatch tray commands to the WinUI UI thread before mutating session/UI state.
  - [x] Update tray snapshots whenever session state changes.

- [x] **Task 4: Add focused tests and validation** (AC: 1,2,3)
  - [x] Test idle tray projection includes identity, HDR status, capture commands, shortcut labels, navigation, and quit.
  - [x] Test active capture projection disables capture commands without disabling navigation commands.
  - [x] Run story validation gates and record manual-validation limits.

## Dev Notes

### Story Scope

Story 7.1 adds tray status and command presentation plus typed command routing. It does not implement global hotkeys, minimize-to-tray close policy, or the final quit-cleanup validation matrix reserved for later Epic 7 stories.

### Architecture Guardrails

- Win32 tray details belong in `Lumiere.Infrastructure.Interop`.
- Menu state projection belongs in `Lumiere.App.Core` and should reuse existing typed capture/readiness state.
- `Lumiere.App` may orchestrate command routing but must not own raw native tray implementation details.
- Real tray behavior requires Windows manual validation before release claims.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.1`] - Story requirements and acceptance criteria.
- [Source: `_bmad-output/project-context.md`] - Module boundary, diagnostics, and validation-level rules.
- [Source: `harness/design/v0-mvp-reference/components/lumiere/tray-context-menu.tsx`] - UX reference for compact command-first tray menu.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-25: Story created from Epic 7 backlog because no 7.1 story file existed.
- 2026-05-25: Added tray projection and focused tests for idle and active capture states.
- 2026-05-25: Added `ITrayMenu`, native Win32 tray menu implementation, and app wiring.
- 2026-05-25: `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false` passed with 0 warnings/errors before full validation.
- 2026-05-25: Full validation passed: restore succeeded; build succeeded with 0 warnings/errors; `Lumiere.Graphics.Tests` passed 265/265; format verification passed after CRLF normalization.
- 2026-05-25: Fixed native tray icon initialization by binding `LoadIconW` explicitly instead of the non-exported neutral `LoadIcon` entry point.

### Completion Notes List

- Tray menu state now includes Lumiere identity, HDR status, fullscreen/region capture commands with shortcut labels, open main window, settings, and quit.
- Native tray icon initialization now uses the correct Unicode Win32 entry point.
- Active capture states disable capture commands through the same session-state projection used by the main panel.
- Native tray icon, hidden message window, and popup menu ownership are isolated in `Lumiere.Infrastructure.Interop`.
- Validation level: Windows CI-pass for projection/routing/build guardrails. Real tray icon display, popup placement, shell behavior, and interaction require Windows manual validation.
- **Manual validation completed:** 2026-05-26. Tray icon display, popup menu behavior, and shell interaction verified on Windows hardware.

### File List

- `_bmad-output/implementation-artifacts/7-1-add-tray-menu-with-status-and-commands.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Lumiere.App.Core/TrayMenuProjection.cs`
- `src/Lumiere.App/App.xaml.cs`
- `src/Lumiere.App/MainWindow.xaml.cs`
- `src/Lumiere.Infrastructure/Interop/ITrayMenu.cs`
- `src/Lumiere.Infrastructure/Interop/TrayMenuSnapshot.cs`
- `src/Lumiere.Infrastructure/Interop/WindowsTrayMenu.cs`
- `tests/Lumiere.Graphics.Tests/App/TrayMenuProjectionTests.cs`

### Change Log

- 2026-05-25: Created story context and implemented tray menu projection, infrastructure, app routing, and focused tests.
