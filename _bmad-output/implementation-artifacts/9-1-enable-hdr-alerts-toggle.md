Status: done

# Story 9.1: Enable HDR Alerts Toggle

## Story

As a screenshot user,
I want to enable or disable HDR alerts in settings,
so that I can control whether Lumiere shows warnings when HDR is unavailable, degraded, unsupported, or failed.

## Requirements Covered

FR13, FR38, UX-DR10, UX-DR18

## Acceptance Criteria

1. **Given** settings are open, **when** the HDR alerts section is displayed, **then** the HDR alerts toggle is enabled and reflects the current persisted preference.

2. **Given** the user toggles HDR alerts, **when** the preference is saved, **then** the change is persisted through `IHdrAlertSettingsWriter.SetHdrAlertsEnabled()` and reflected in subsequent HDR alert behavior.

3. **Given** HDR alerts are disabled, **when** a non-critical HDR warning occurs, **then** Lumiere suppresses optional alert chrome while preserving status and diagnostics.

4. **Given** the toggle state is projected, **when** the settings panel refreshes, **then** the toggle visual state matches the persisted `HdrAlertsEnabled` value.

## Tasks / Subtasks

- [x] Task 1: Enable the HDR alerts toggle in the settings UI (AC: 1, 4)
  - [x] Subtask 1.1: Remove `IsEnabled="False"` from `SettingsHdrAlertsButton` in `MainWindow.xaml` (line 387)
  - [x] Subtask 1.2: Add dynamic `SettingsHdrAlertsButton.IsEnabled` assignment in `ApplySettingsProjection()` using a new `IsHdrAlertsReadOnly` projection property
  - [x] Subtask 1.3: Pass `isReadOnly: false` (or projection-driven value) to `ApplySwitchState()` for the HDR alerts toggle (currently hardcoded `true` at line 1376)

- [x] Task 2: Add `IsHdrAlertsReadOnly` to `SettingsPanelProjection` (AC: 1, 4)
  - [x] Subtask 2.1: Add `bool IsHdrAlertsReadOnly` property to `SettingsPanelProjection` record
  - [x] Subtask 2.2: Set `IsHdrAlertsReadOnly: false` in `Project()` method — HDR alerts writer is fully implemented
  - [x] Subtask 2.3: Update `SettingsPanelProjectionTests` to verify `IsHdrAlertsReadOnly` is `false`

- [x] Task 3: Verify existing HDR alert suppression behavior (AC: 3)
  - [x] Subtask 3.1: Verify `MainPanelProjection.Project()` already suppresses alert chrome when `hdrAlertsEnabled: false`
  - [x] Subtask 3.2: Verify `hdrAlertDismissed` flag in `MainWindow.xaml.cs` is reset on toggle change (already done at line 251)
  - [x] Subtask 3.3: Add test coverage for HDR alert suppression when disabled

- [x] Task 4: Validate and record (AC: all)
  - [x] Subtask 4.1: Run full validation: restore, build, tests, format verification
  - [x] Subtask 4.2: Verify all existing tests continue to pass
  - [x] Subtask 4.3: Record validation level: Mac edit / Windows CI-pass

## Dev Notes

### Validation Level

**Windows CI-pass** — Toggle enablement is UI wiring. HDR alert suppression already tested.

### Architecture Guardrails

- **No new interfaces needed:** `IHdrAlertSettingsWriter` already exists and is fully implemented in `DefaultSettingsProvider`.
- **No new persistence needed:** `LocalSettingsSnapshot.HdrAlertsEnabled` already persists to JSON.
- **Projection pattern:** Follow existing `IsCopyAsImageReadOnly` pattern — add `IsHdrAlertsReadOnly` to `SettingsPanelProjection` and wire it to `SettingsHdrAlertsButton.IsEnabled`.
- **`ApplySwitchState` pattern:** The `isReadOnly` parameter controls the visual appearance of the toggle switch. Pass `false` to make it look interactive.

### Files to Modify

| File | Change |
|---|---|
| `src/Lumiere.App/MainWindow.xaml` | Remove `IsEnabled="False"` from `SettingsHdrAlertsButton` (line 387) |
| `src/Lumiere.App/MainWindow.xaml.cs` | Add `SettingsHdrAlertsButton.IsEnabled = !projection.IsHdrAlertsReadOnly;` and pass `projection.IsHdrAlertsReadOnly` to `ApplySwitchState` |
| `src/Lumiere.App.Core/SettingsPanelProjection.cs` | Add `bool IsHdrAlertsReadOnly` parameter, set to `false` in `Project()` |
| `tests/Lumiere.Graphics.Tests/App/SettingsPanelProjectionTests.cs` | Assert `IsHdrAlertsReadOnly` is `false` |

### References

- [Source: src/Lumiere.Settings/IHdrAlertSettingsWriter.cs] — existing writer interface
- [Source: src/Lumiere.Settings/DefaultSettingsProvider.cs:49-53] — existing `SetHdrAlertsEnabled()` implementation
- [Source: src/Lumiere.App/MainWindow.xaml.cs:243-259] — existing click handler
- [Source: src/Lumiere.App/MainWindow.xaml.cs:1370-1380] — current projection application with hardcoded `isReadOnly: true`
- [Source: src/Lumiere.App.Core/SettingsPanelProjection.cs:18-45] — projection factory method

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List

- `src/Lumiere.App/MainWindow.xaml` — removed `IsEnabled="False"` from HDR alerts button
- `src/Lumiere.App/MainWindow.xaml.cs` — added dynamic `IsEnabled` and `isReadOnly` for HDR alerts toggle
- `src/Lumiere.App.Core/SettingsPanelProjection.cs` — added `bool IsHdrAlertsReadOnly` parameter
- `tests/Lumiere.Graphics.Tests/App/SettingsPanelProjectionTests.cs` — added `IsHdrAlertsReadOnly` assertion
