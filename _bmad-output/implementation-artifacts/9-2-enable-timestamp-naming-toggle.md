Status: done

# Story 9.2: Enable Timestamp Naming Toggle

## Story

As a screenshot user,
I want to enable or disable timestamp-based file naming in settings,
so that folder output uses consistent, safe filenames that avoid overwriting.

## Requirements Covered

FR26, FR38, UX-DR15, UX-DR18

## Acceptance Criteria

1. **Given** settings are open, **when** the timestamp naming section is displayed, **then** the timestamp toggle is enabled and reflects the current persisted preference.

2. **Given** the user toggles timestamp naming, **when** the preference is saved, **then** the change is persisted through `ITimestampSettingsWriter.SetTimestampNaming()`.

3. **Given** timestamp naming is enabled, **when** folder output creates a file, **then** the filename uses deterministic invariant formatting and avoids overwriting existing files.

4. **Given** timestamp naming is disabled, **when** folder output creates a file, **then** the filename uses the configured or default naming policy without timestamp.

## Tasks / Subtasks

- [x] Task 1: Create `ITimestampSettingsWriter` interface (AC: 2)
  - [x] Subtask 1.1: Create `ITimestampSettingsWriter.cs` in `Lumiere.Settings` with `void SetTimestampNaming(bool enabled)` method
  - [x] Subtask 1.2: Implement `ITimestampSettingsWriter` in `DefaultSettingsProvider` following the `SetHdrAlertsEnabled` pattern

- [x] Task 2: Enable the timestamp toggle in the settings UI (AC: 1)
  - [x] Subtask 2.1: Change `IsTimestampReadOnly` from `true` to `false` in `OutputSettingsProjection.ReadOnly()`
  - [x] Subtask 2.2: Add `SettingsTimestampButton.IsEnabled = !projection.Output.IsTimestampReadOnly;` in `ApplySettingsProjection()`
  - [x] Subtask 2.3: Pass `projection.Output.IsTimestampReadOnly` to `ApplySwitchState()` for the timestamp toggle

- [x] Task 3: Wire up the click handler (AC: 2)
  - [x] Subtask 3.1: Add `OnSettingsTimestampButtonClick` handler in `MainWindow.xaml.cs` that calls `ITimestampSettingsWriter.SetTimestampNaming()`
  - [x] Subtask 3.2: Wire the XAML `Click` event to the handler
  - [x] Subtask 3.3: Add `ITimestampSettingsWriter` field to `MainWindow` and inject via constructor

- [x] Task 4: Update tests and validate (AC: all)
  - [x] Subtask 4.1: Update `SettingsPanelProjectionTests` to verify `IsTimestampReadOnly` is `false`
  - [x] Subtask 4.2: Run full validation: restore, build, tests, format verification
  - [x] Subtask 4.3: Record validation level: Mac edit / Windows CI-pass

### File List

- `src/Lumiere.Settings/ITimestampSettingsWriter.cs` — NEW writer interface
- `src/Lumiere.Settings/DefaultSettingsProvider.cs` — implemented `ITimestampSettingsWriter`
- `src/Lumiere.App.Core/SettingsPanelProjection.cs` — changed `IsTimestampReadOnly` to `false`
- `src/Lumiere.App/MainWindow.xaml` — removed `IsEnabled="False"`, added `Click` event
- `src/Lumiere.App/MainWindow.xaml.cs` — added click handler, field, constructor parameter
- `src/Lumiere.App/App.xaml.cs` — passed `settingsProvider` as `ITimestampSettingsWriter`
- `tests/Lumiere.Graphics.Tests/App/SettingsPanelProjectionTests.cs` — updated assertion

## Dev Notes

### Validation Level

**Windows CI-pass** — Toggle enablement and writer wiring. Timestamp naming behavior already tested in output pipeline.

### Architecture Guardrails

- **New interface:** `ITimestampSettingsWriter` follows the same pattern as `IHdrAlertSettingsWriter` — single method, implemented in `DefaultSettingsProvider`.
- **Persistence:** `LocalSettingsSnapshot.TimestampNaming` already persists to JSON. The writer just needs to mutate the snapshot and save.
- **Module boundary:** Writer interface belongs in `Lumiere.Settings`. UI wiring belongs in `Lumiere.App`.
- **Projection pattern:** Follow `IsCopyAsImageReadOnly` / `IsHdrAlertsReadOnly` pattern.

### Files to Modify/Create

| File | Change |
|---|---|
| `src/Lumiere.Settings/ITimestampSettingsWriter.cs` | **NEW** — writer interface |
| `src/Lumiere.Settings/DefaultSettingsProvider.cs` | Implement `ITimestampSettingsWriter` |
| `src/Lumiere.App.Core/SettingsPanelProjection.cs` | Change `IsTimestampReadOnly: true` to `false` |
| `src/Lumiere.App/MainWindow.xaml` | Wire `Click` event for timestamp button |
| `src/Lumiere.App/MainWindow.xaml.cs` | Add click handler, inject writer, wire `IsEnabled` |
| `src/Lumiere.App/App.xaml.cs` | Pass `ITimestampSettingsWriter` to `MainWindow` |
| `tests/Lumiere.Graphics.Tests/App/SettingsPanelProjectionTests.cs` | Assert `IsTimestampReadOnly` is `false` |

### References

- [Source: src/Lumiere.Settings/IHdrAlertSettingsWriter.cs] — pattern for writer interface
- [Source: src/Lumiere.Settings/DefaultSettingsProvider.cs:49-53] — `SetHdrAlertsEnabled()` pattern
- [Source: src/Lumiere.App.Core/SettingsPanelProjection.cs:133] — current `IsTimestampReadOnly: true`

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
