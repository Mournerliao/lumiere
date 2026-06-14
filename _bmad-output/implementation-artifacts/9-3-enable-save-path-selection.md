Status: done

# Story 9.3: Enable Save Path Selection

## Story

As a screenshot user,
I want to select or change the save folder for file output in settings,
so that captures are saved to my preferred location.

## Requirements Covered

FR35, FR38, UX-DR13, UX-DR18

## Acceptance Criteria

1. **Given** settings are open and folder output is selected, **when** the save path section is displayed, **then** the save path shows the current configured path with an editable selection control.

2. **Given** the user activates the save path control, **when** a native Windows folder picker is shown, **then** the user can select a new folder and the path is persisted through `ISavePathSettingsWriter.SetSavePath()`.

3. **Given** the selected path is invalid, inaccessible, or permission denied, **when** the path is validated, **then** the settings UI shows inline recovery guidance near the path control.

4. **Given** the save path is changed, **when** subsequent captures use folder output, **then** files are saved to the new configured path.

## Tasks / Subtasks

- [x] Task 1: Create `ISavePathSettingsWriter` interface (AC: 2)
  - [x] Subtask 1.1: Create `ISavePathSettingsWriter.cs` in `Lumiere.Settings` with `void SetSavePath(string? path)` method
  - [x] Subtask 1.2: Implement `ISavePathSettingsWriter` in `DefaultSettingsProvider`

- [x] Task 2: Enable the save path control in the settings UI (AC: 1)
  - [x] Subtask 2.1: Change `IsSavePathReadOnly` from `true` to `false` in `OutputSettingsProjection.ReadOnly()`
  - [x] Subtask 2.2: Add `Tapped` event handler to `SettingsSavePathValuePill` in XAML
  - [x] Subtask 2.3: Add visual feedback for interactive state (cursor, hover)

- [x] Task 3: Implement folder picker (AC: 2, 3)
  - [x] Subtask 3.1: Add `OnSettingsSavePathTapped` handler that opens `FolderPicker`
  - [x] Subtask 3.2: Initialize `FolderPicker` with window handle using `InitializeWithWindow.Initialize()`
  - [x] Subtask 3.3: On folder selection, call `ISavePathSettingsWriter.SetSavePath()` and refresh projection
  - [x] Subtask 3.4: Handle picker cancellation gracefully (no-op)
  - [x] Subtask 3.5: Add path validation feedback in helper text

- [x] Task 4: Wire up dependency injection (AC: 2)
  - [x] Subtask 4.1: Add `ISavePathSettingsWriter` field to `MainWindow`
  - [x] Subtask 4.2: Add constructor parameter and inject via `App.xaml.cs`

- [x] Task 5: Update tests and validate (AC: all)
  - [x] Subtask 5.1: Update `SettingsPanelProjectionTests` to verify `IsSavePathReadOnly` is `false`
  - [x] Subtask 5.2: Run full validation: restore, build, tests, format verification
  - [x] Subtask 5.3: Record validation level: Mac edit / Windows CI-pass

### File List

- `src/Lumiere.Settings/ISavePathSettingsWriter.cs` — NEW writer interface
- `src/Lumiere.Settings/DefaultSettingsProvider.cs` — implemented `ISavePathSettingsWriter`
- `src/Lumiere.App.Core/SettingsPanelProjection.cs` — changed `IsSavePathReadOnly` to `false`
- `src/Lumiere.App/MainWindow.xaml` — added `Tapped` event to save path pill
- `src/Lumiere.App/MainWindow.xaml.cs` — added folder picker handler, field, constructor parameter
- `src/Lumiere.App/App.xaml.cs` — passed `settingsProvider` as `ISavePathSettingsWriter`
- `tests/Lumiere.Graphics.Tests/App/SettingsPanelProjectionTests.cs` — updated assertions

## Dev Notes

### Validation Level

**Windows CI-pass** — Folder picker is Windows manual validation. UI wiring and persistence are CI-testable.

### Architecture Guardrails

- **New interface:** `ISavePathSettingsWriter` follows the same pattern as other writers.
- **Persistence:** `LocalSettingsSnapshot.SavePath` already persists to JSON.
- **Folder picker:** Uses `Windows.Storage.Pickers.FolderPicker` with `WinRT.Interop.InitializeWithWindow`. Requires `HWND` from `WindowNative.GetWindowHandle(this)`.
- **Module boundary:** Picker interaction is UI-level, stays in `Lumiere.App`. Writer interface in `Lumiere.Settings`.
- **Path validation:** Basic check for path existence and accessibility. Detailed permission checking is deferred.

### Files to Modify/Create

| File | Change |
|---|---|
| `src/Lumiere.Settings/ISavePathSettingsWriter.cs` | **NEW** — writer interface |
| `src/Lumiere.Settings/DefaultSettingsProvider.cs` | Implement `ISavePathSettingsWriter` |
| `src/Lumiere.App.Core/SettingsPanelProjection.cs` | Change `IsSavePathReadOnly` to `false` |
| `src/Lumiere.App/MainWindow.xaml` | Add `Tapped` event to save path pill |
| `src/Lumiere.App/MainWindow.xaml.cs` | Add folder picker handler, inject writer |
| `src/Lumiere.App/App.xaml.cs` | Pass `ISavePathSettingsWriter` to `MainWindow` |
| `tests/Lumiere.Graphics.Tests/App/SettingsPanelProjectionTests.cs` | Update assertions |

### References

- [Source: src/Lumiere.Settings/IHdrAlertSettingsWriter.cs] — pattern for writer interface
- [Source: src/Lumiere.App.Core/SettingsPanelProjection.cs:130] — current `IsSavePathReadOnly: true`
- [Source: src/Lumiere.App/MainWindow.xaml:605-625] — current save path UI

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
