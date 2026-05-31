---
status: done
---

# Story 7.4: Support Background and Minimize-to-Tray Workflow

Status: done

## Story

As a screenshot user,
I want Lumiere to remain available after leaving the main window,
so that capture stays low-interruption.

## Acceptance Criteria

1. **Given** the main window offers minimize/background intent, **when** the user minimizes or closes according to the MVP policy, **then** Lumiere remains available through tray and hotkeys where configured.

2. **Given** the app is in background/tray mode, **when** a tray or hotkey capture command starts, **then** capture runs without requiring the main window to be visible.

3. **Given** background operation is disabled or unavailable, **when** the user attempts it, **then** the app communicates the limitation without losing capture or settings state.

## Tasks / Subtasks

- [x] **Task 1: Intercept close and minimize for background mode** (AC: 1)
  - [x] Cancel ordinary close when tray or hotkey background availability exists.
  - [x] Hide the AppWindow on close or minimize instead of disposing app state.
  - [x] Preserve explicit shutdown as a separate path.

- [x] **Task 2: Keep background commands routed through existing state** (AC: 2)
  - [x] Allow tray capture commands to run while the main window is hidden.
  - [x] Allow global hotkeys to run while the main window is hidden.
  - [x] Reuse existing capture/session/output/settings state instead of creating background-local state.

- [x] **Task 3: Surface unavailable background behavior safely** (AC: 3)
  - [x] Let ordinary close proceed if neither tray nor hotkeys are available.
  - [x] Log the unavailable background path as recoverable limitation.
  - [x] Keep settings and capture state untouched when background mode is unavailable.

- [x] **Task 4: Validate and record limits** (AC: 1,2,3)
  - [x] Build against WinUI `AppWindow.Closing` and `AppWindow.Changed` behavior.
  - [x] Confirm automated tests cover the shared projection/state guardrails.
  - [x] Record that real minimize/close shell behavior requires Windows manual validation.

## Dev Notes

### Story Scope

Story 7.4 makes ordinary close/minimize hide Lumiere into the background when tray or hotkeys are available. It does not add onboarding, a background settings toggle, or installer-level startup behavior.

### Architecture Guardrails

- Background mode must preserve the existing `MainWindow`, settings provider, capture coordinator, tray, hotkey, and graphics resources.
- Background commands must route through the same command paths as visible-window commands.
- Explicit Quit remains the deterministic teardown path.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.4`] - Story requirements and acceptance criteria.
- [Source: `_bmad-output/project-context.md`] - Shared state, lifecycle, and validation-level rules.
- [Source: `_bmad-output/implementation-artifacts/7-2-open-main-window-and-settings-from-tray.md`] - Existing-window activation path.
- [Source: `_bmad-output/implementation-artifacts/7-3-register-global-capture-hotkeys.md`] - Background hotkey route.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-25: Story created from Epic 7 backlog because no 7.4 story file existed.
- 2026-05-25: Added AppWindow close/minimize interception to hide into background when tray or hotkeys are available.
- 2026-05-25: Preserved explicit shutdown so tray Quit can close normally.
- 2026-05-25: Kept tray and hotkey capture commands routed to existing capture state while the main window is hidden.
- 2026-05-25: Full validation passed: restore succeeded; build succeeded with 0 warnings/errors; `Lumiere.Graphics.Tests` passed 273/273; format verification passed after CRLF normalization.
- 2026-05-25: Fixed startup visibility regression by enabling close/minimize background handlers only after the first window activation and by responding only to presenter-state changes.
- 2026-05-25: Moved tray and hotkey attachment after main-window activation so background integrations cannot prevent first-window display.

### Completion Notes List

- Closing or minimizing now hides Lumiere into the background when tray/hotkey infrastructure is available.
- Startup now leaves the main window visible; background close/minimize handlers are armed only after the first activation.
- App launch now activates the main window before attaching tray and global hotkey integrations.
- Tray and global hotkey capture commands can run without requiring the main window to be visible.
- If background infrastructure is unavailable, the app logs the limitation and closes normally rather than stranding state.
- Validation level: Windows CI-pass for build/routing guardrails. Real close/minimize shell behavior and hidden-window capture behavior require Windows manual validation.

### File List

- `_bmad-output/implementation-artifacts/7-4-support-background-and-minimize-to-tray-workflow.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Lumiere.App/App.xaml.cs`
- `src/Lumiere.App/MainWindow.xaml.cs`

### Change Log

- 2026-05-25: Created story context and implemented background/minimize-to-tray workflow.
