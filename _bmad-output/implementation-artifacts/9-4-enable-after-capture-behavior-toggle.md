Status: done

# Story 9.4: Enable After-Capture Behavior Toggle

## Story

As a screenshot user,
I want to configure after-capture behavior in settings,
so that Lumiere can automatically open or reveal the saved file after folder output.

## Requirements Covered

FR36, FR38, UX-DR14, UX-DR18

## Acceptance Criteria

1. **Given** settings are open and folder output is selected, **when** the after-capture section is displayed, **then** the after-capture toggle is enabled and reflects the current persisted preference.

2. **Given** the user toggles after-capture behavior, **when** the preference is saved, **then** the change is persisted through `IAfterCaptureSettingsWriter.SetAfterCaptureBehavior()`.

3. **Given** output target is clipboard-only, **when** after-capture behavior is evaluated, **then** the control is disabled with helper text explaining clipboard-only has no file artifact.

## Tasks / Subtasks

- [x] Task 1: Create `IAfterCaptureSettingsWriter` interface (AC: 2)
  - [x] Subtask 1.1: Create `IAfterCaptureSettingsWriter.cs` in `Lumiere.Settings` with `void SetAfterCaptureBehavior(AfterCaptureBehavior behavior)` method
  - [x] Subtask 1.2: Implement `IAfterCaptureSettingsWriter` in `DefaultSettingsProvider`

- [x] Task 2: Enable the after-capture toggle in the settings UI (AC: 1, 3)
  - [x] Subtask 2.1: Change `IsAfterCaptureReadOnly` from `true` to `false` in `OutputSettingsProjection.ReadOnly()`
  - [x] Subtask 2.2: Wire up `SettingsOpenAfterCaptureButton.IsEnabled` dynamically based on output target
  - [x] Subtask 2.3: Keep disabled when output target is clipboard-only (already handled in projection)

- [x] Task 3: Wire up click handler (AC: 2)
  - [x] Subtask 3.1: Add `OnSettingsAfterCaptureButtonClick` handler that cycles through None/Open/Reveal
  - [x] Subtask 3.2: Wire XAML `Click` event
  - [x] Subtask 3.3: Inject `IAfterCaptureSettingsWriter` via constructor

- [x] Task 4: Update tests and validate (AC: all)
  - [x] Subtask 4.1: Update `SettingsPanelProjectionTests` to verify `IsAfterCaptureReadOnly` is `false`
  - [x] Subtask 4.2: Run full validation: restore, build, tests, format verification

## Dev Notes

### Validation Level

**Windows CI-pass** — Toggle enablement and writer wiring.

### Files to Create/Modify

| File | Change |
|---|---|
| `src/Lumiere.Settings/IAfterCaptureSettingsWriter.cs` | **NEW** — writer interface |
| `src/Lumiere.Settings/DefaultSettingsProvider.cs` | Implement `IAfterCaptureSettingsWriter` |
| `src/Lumiere.App.Core/SettingsPanelProjection.cs` | Change `IsAfterCaptureReadOnly` to `false` |
| `src/Lumiere.App/MainWindow.xaml` | Add `Click` event to after-capture button |
| `src/Lumiere.App/MainWindow.xaml.cs` | Add click handler, inject writer |
| `src/Lumiere.App/App.xaml.cs` | Pass writer to `MainWindow` |
| `tests/Lumiere.Graphics.Tests/App/SettingsPanelProjectionTests.cs` | Update assertions |

## Dev Agent Record

### File List
