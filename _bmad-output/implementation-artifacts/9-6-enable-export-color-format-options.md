Status: done

# Story 9.6: Enable Export Color Format Options

## Story

As a screenshot user,
I want to see export color format options in settings with clear validation status,
so that I understand which formats are available and which are pending validation.

## Requirements Covered

FR29, NFR9, UX-DR11, UX-DR18

## Acceptance Criteria

1. **Given** settings are open, **when** the export section is displayed, **then** HDR10, P3, and sRGB segments are visible with clear validation-scoped labels.

2. **Given** the user views export options, **when** implementation semantics are incomplete for HDR10 or P3, **then** those options are disabled or explicitly labeled as pending encoder metadata, conversion policy, and Windows validation.

3. **Given** sRGB is selected as the default, **when** the user views the export section, **then** sRGB shows as active with a note that it reflects the current basic PNG output surface.

4. **Given** the user selects an enabled export option, **when** the preference is saved, **then** the change is persisted through `IExportColorSettingsWriter`.

## Tasks / Subtasks

- [x] Task 1: Create `IExportColorSettingsWriter` interface (AC: 4)
  - [x] Subtask 1.1: Create `IExportColorSettingsWriter.cs` in `Lumiere.Settings` with `void SetExportColorFormat(string format)` method
  - [x] Subtask 1.2: Implement `IExportColorSettingsWriter` in `DefaultSettingsProvider`

- [x] Task 2: Enable sRGB selection in the export UI (AC: 1, 2, 3)
  - [x] Subtask 2.1: Change sRGB `IsReadOnly` from `true` to `false` in `CreateExportColorOptions()`
  - [x] Subtask 2.2: Keep HDR10 and P3 as `IsReadOnly: true` (pending validation)
  - [x] Subtask 2.3: Add click handler for enabled export segments

- [x] Task 3: Wire up dependency injection (AC: 4)
  - [x] Subtask 3.1: Add `IExportColorSettingsWriter` field to `MainWindow`
  - [x] Subtask 3.2: Add constructor parameter and inject via `App.xaml.cs`

- [x] Task 4: Update tests and validate (AC: all)
  - [x] Subtask 4.1: Update `SettingsPanelProjectionTests` to verify sRGB is selectable
  - [x] Subtask 4.2: Run full validation: restore, build, tests, format verification

## Dev Notes

### Validation Level

**Windows CI-pass** — Export format selection is UI-only for MVP. Actual format conversion is not implemented.

### Architecture Guardrails

- **New interface:** `IExportColorSettingsWriter` follows the same pattern as other writers.
- **sRGB only:** Only sRGB is selectable for MVP. HDR10 and P3 remain disabled until encoder metadata, conversion policy, and Windows validation exist.
- **No HDR claims:** Export format selection does not imply HDR-preserving output. sRGB reflects basic PNG output.

### Files to Create/Modify

| File | Change |
|---|---|
| `src/Lumiere.Settings/IExportColorSettingsWriter.cs` | **NEW** — writer interface |
| `src/Lumiere.Settings/DefaultSettingsProvider.cs` | Implement `IExportColorSettingsWriter` |
| `src/Lumiere.App.Core/SettingsPanelProjection.cs` | Enable sRGB selection |
| `src/Lumiere.App/MainWindow.xaml.cs` | Add export segment click handler |
| `src/Lumiere.App/App.xaml.cs` | Pass writer to `MainWindow` |
| `tests/Lumiere.Graphics.Tests/App/SettingsPanelProjectionTests.cs` | Update assertions |

## Dev Agent Record

### File List
