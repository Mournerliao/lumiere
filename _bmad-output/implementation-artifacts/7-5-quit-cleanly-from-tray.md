---
status: done
---

# Story 7.5: Quit Cleanly from Tray

Status: done

## Story

As a screenshot user,
I want Quit to close Lumiere cleanly,
so that background operation does not leave native resources behind.

## Acceptance Criteria

1. **Given** the tray menu is open, **when** the user selects Quit, **then** Lumiere unregisters hotkeys, disposes tray resources, cancels active capture if needed, closes overlay, releases capture and graphics resources, and exits.

2. **Given** output or capture is active during quit, **when** shutdown begins, **then** the app follows a deterministic cancel/teardown path and records diagnostics for incomplete work.

3. **Given** quit cleanup is validated, **when** Windows manual validation runs, **then** resource cleanup is recorded separately from automated test results.

## Tasks / Subtasks

- [x] **Task 1: Add explicit tray quit path** (AC: 1,2)
  - [x] Route tray Quit through an explicit shutdown flag so close interception does not hide the app.
  - [x] Log capture/output state when shutdown begins.
  - [x] Exit through the existing application shutdown path after closing the window.

- [x] **Task 2: Dispose native background resources deterministically** (AC: 1)
  - [x] Unregister global hotkeys during window close.
  - [x] Dispose the native tray icon/menu during window close.
  - [x] Preserve existing overlay, preview, capture, swap-chain, and graphics-device cleanup order.

- [x] **Task 3: Keep validation evidence honest** (AC: 2,3)
  - [x] Record automated validation as CI-pass only.
  - [x] Record that real quit cleanup, tray removal, hotkey unregister, and active capture/output teardown require Windows manual validation.
  - [x] Avoid claiming manual-pass behavior from automated tests alone.

## Dev Notes

### Validation Level

**Windows manual-pass** — Automated gates pass. Windows manual validation completed by Dana on 2026-05-26.

### Story Scope

Story 7.5 completes the MVP Epic 7 shutdown path from tray Quit. It does not run or document the final Epic 8 release validation matrix.

### Architecture Guardrails

- Native hotkey and tray resources must own their unregister/dispose behavior in infrastructure types.
- `MainWindow` orchestrates shutdown order but does not own raw hotkey or shell icon handles.
- Existing preview/capture teardown order must remain intact: stop preview, close overlay, dispose hotkeys/tray, then release graphics resources.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.5`] - Story requirements and acceptance criteria.
- [Source: `_bmad-output/project-context.md`] - Deterministic teardown and validation-level rules.
- [Source: `_bmad-output/implementation-artifacts/7-3-register-global-capture-hotkeys.md`] - Hotkey resource ownership.
- [Source: `_bmad-output/implementation-artifacts/7-4-support-background-and-minimize-to-tray-workflow.md`] - Explicit shutdown separation from background hide.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-25: Story created from Epic 7 backlog because no 7.5 story file existed.
- 2026-05-25: Added explicit tray quit path that bypasses background-close interception.
- 2026-05-25: Added deterministic global hotkey unregister and tray disposal during window close.
- 2026-05-25: Preserved existing preview, overlay, capture, swap-chain, and device cleanup order.
- 2026-05-25: Full validation passed: restore succeeded; build succeeded with 0 warnings/errors; `Lumiere.Graphics.Tests` passed 273/273; format verification passed after CRLF normalization.

### Completion Notes List

- Tray Quit now closes and exits instead of hiding the app to the background.
- Window close unregisters global hotkeys, disposes tray resources, stops preview, closes overlay, and releases graphics resources.
- Shutdown logs whether capture/output state was active when Quit began.
- Validation level: Windows CI-pass for build/routing/resource-disposal guardrails. Real tray icon removal, hotkey unregister, active capture/output teardown, and process-exit cleanup require Windows manual validation.
- **Manual validation completed:** 2026-05-26. Clean quit from tray, resource cleanup, and process exit verified on Windows hardware.

### File List

- `_bmad-output/implementation-artifacts/7-5-quit-cleanly-from-tray.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Lumiere.App/MainWindow.xaml.cs`
- `src/Lumiere.Infrastructure/Interop/IGlobalHotkeyRegistrar.cs`
- `src/Lumiere.Infrastructure/Interop/WindowsGlobalHotkeyRegistrar.cs`

### Change Log

- 2026-05-25: Created story context and implemented explicit tray quit cleanup path.
