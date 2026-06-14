Status: done

# Story 9.5: Enable Shortcut Editing

## Story

As a screenshot user,
I want to configure fullscreen and region capture shortcuts in settings,
so that Lumiere matches my workflow and key preferences.

## Requirements Covered

FR32, FR33, FR38, UX-DR8, UX-DR9, UX-DR18

## Acceptance Criteria

1. **Given** settings are open, **when** the shortcuts section is displayed, **then** fullscreen and region shortcut controls are editable with current configured values.

2. **Given** the user activates a shortcut control, **when** the control enters edit mode, **then** the user can press a key combination to set the new shortcut.

3. **Given** the user presses an invalid key combination, **when** validation occurs, **then** the control shows inline feedback and preserves the previous valid shortcut.

4. **Given** the shortcut is changed, **when** the preference is saved, **then** the change is persisted through `IShortcutSettingsWriter` and the hotkey is re-registered.

## Tasks / Subtasks

- [x] Task 1: Create `IShortcutSettingsWriter` interface (AC: 4)
  - [x] Subtask 1.1: Create `IShortcutSettingsWriter.cs` in `Lumiere.Settings` with `void SetFullscreenShortcut(string? shortcut)` and `void SetRegionShortcut(string? shortcut)` methods
  - [x] Subtask 1.2: Implement `IShortcutSettingsWriter` in `DefaultSettingsProvider`

- [x] Task 2: Enable shortcut editing in the settings UI (AC: 1)
  - [x] Subtask 2.1: Change `IsReadOnly` from `true` to `false` in `ShortcutSettingProjection.FromHotkeyBinding()`
  - [x] Subtask 2.2: Add `Tapped` event handlers to shortcut pill controls in XAML
  - [x] Subtask 2.3: Implement key capture dialog or inline editing mode

- [x] Task 3: Wire up dependency injection (AC: 4)
  - [x] Subtask 3.1: Add `IShortcutSettingsWriter` field to `MainWindow`
  - [x] Subtask 3.2: Add constructor parameter and inject via `App.xaml.cs`

- [x] Task 4: Update tests and validate (AC: all)
  - [x] Subtask 4.1: Update `SettingsPanelProjectionTests` to verify shortcuts are editable
  - [x] Subtask 4.2: Run full validation: restore, build, tests, format verification

## Dev Notes

### Validation Level

**Windows CI-pass** — Shortcut editing UI and persistence. Key registration is Windows manual validation.

### Architecture Guardrails

- **New interface:** `IShortcutSettingsWriter` follows the same pattern as other writers.
- **Persistence:** `LocalSettingsSnapshot.FullscreenShortcut` and `RegionShortcut` already persist to JSON.
- **Key capture:** Use a simple `ContentDialog` with key event capture for MVP. Full key binding editor is deferred.
- **Validation:** Minimum modifier key (Ctrl, Alt, Shift) required. Single-key shortcuts are rejected.

### Files to Create/Modify

| File | Change |
|---|---|
| `src/Lumiere.Settings/IShortcutSettingsWriter.cs` | **NEW** — writer interface |
| `src/Lumiere.Settings/DefaultSettingsProvider.cs` | Implement `IShortcutSettingsWriter` |
| `src/Lumiere.App.Core/SettingsPanelProjection.cs` | Change shortcut `IsReadOnly` to `false` |
| `src/Lumiere.App/MainWindow.xaml` | Add `Tapped` events to shortcut pills |
| `src/Lumiere.App/MainWindow.xaml.cs` | Add key capture handlers, inject writer |
| `src/Lumiere.App/App.xaml.cs` | Pass writer to `MainWindow` |
| `tests/Lumiere.Graphics.Tests/App/SettingsPanelProjectionTests.cs` | Update assertions |

## Dev Agent Record

### File List
