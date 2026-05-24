---
title: 'Fix enable output settings controls'
type: 'bugfix'
created: '2026-05-24'
status: 'done'
baseline_commit: '444a91bb23a92f2559037a1d8602d03773f57be9'
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent - do not modify unless human renegotiates">

## Intent

**Problem:** Epic 6 output behavior can consume persisted `OutputTarget` and `CopyAsImage`, but the settings UI still leaves destination and copy-as-image controls read-only, so users cannot change the preferences that the output pipeline already supports.

**Approach:** Add a narrow output-settings writer path, enable only the supported destination and copy-as-image controls, persist changes through the existing local settings store, and refresh the settings projection after each user action.

## Boundaries & Constraints

**Always:** Keep settings as the shared source of truth consumed by the output request; preserve existing HDR/scRGB capture and output boundaries; use deterministic local-settings persistence and structured logging through existing logger patterns; keep unsupported controls explicitly read-only.

**Ask First:** If implementing this requires changing the settings schema version, introducing a new UI framework/control family, adding a save-path picker, or changing output pipeline semantics.

**Never:** Do not implement Epic 7 tray/hotkeys/background behavior; do not claim clipboard output is HDR-preserving; do not enable color/export options or after-capture choices beyond existing projection behavior; do not create parallel in-memory settings state in `Lumiere.App`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Destination changed | User selects Clipboard, Folder, or Both in settings | `DefaultSettingsProvider.OutputTarget` updates, persists to local settings, and the visible segment selection refreshes | Invalid enum values are not exposed by the UI; store validation remains the fallback for corrupted files |
| Copy-as-image toggled | User toggles Copy as image on or off | `DefaultSettingsProvider.CopyAsImage` updates, persists to local settings, and next output request consumes the new value | Save failure logs through `LocalSettingsStore`; in-memory preference remains active as existing save behavior defines |
| Read-only controls remain unsupported | Save path, timestamp, after-capture, and color/export controls are shown | Unsupported controls remain disabled/read-only unless already supported elsewhere | No new handlers are added for unsupported controls |

</frozen-after-approval>

## Code Map

- `src/Lumiere.Settings/ISettingsProvider.cs` -- read-only settings contract already consumed by UI and output request construction.
- `src/Lumiere.Settings/DefaultSettingsProvider.cs` -- existing local-settings provider and HDR alert writer; add output preference writer methods here.
- `src/Lumiere.App.Core/SettingsPanelProjection.cs` -- marks output destination and copy-as-image as read-only today; update projection flags/help text for supported controls only.
- `src/Lumiere.App/MainWindow.xaml` -- destination segments are static borders and copy-as-image is disabled; add click/toggle hooks and accessibility text for enabled controls.
- `src/Lumiere.App/MainWindow.xaml.cs` -- existing HDR alert handler is the pattern for settings writes and projection refresh; add destination/copy-as-image handlers.
- `tests/Lumiere.Graphics.Tests/Settings/DefaultSettingsProviderTests.cs` -- provider persistence coverage for writer methods.
- `tests/Lumiere.Graphics.Tests/App/SettingsPanelProjectionTests.cs` -- projection coverage for enabled supported controls and disabled unsupported controls.

## Tasks & Acceptance

**Execution:**
- [x] `src/Lumiere.Settings/IOutputSettingsWriter.cs` -- add a focused writer contract for supported output preferences -- avoids widening read-only provider semantics.
- [x] `src/Lumiere.Settings/DefaultSettingsProvider.cs` -- implement output target and copy-as-image writer methods that update the snapshot and call `LocalSettingsStore.Save`.
- [x] `src/Lumiere.App.Core/SettingsPanelProjection.cs` -- mark destination and copy-as-image as enabled/supported while keeping save path, timestamp, after-capture, and color/export read-only.
- [x] `src/Lumiere.App/MainWindow.xaml` and `src/Lumiere.App/MainWindow.xaml.cs` -- wire destination selection and copy-as-image toggle to the writer, refresh projection, and log concise setting updates.
- [x] `src/Lumiere.App/App.xaml.cs` -- pass the shared settings provider as the output settings writer to `MainWindow`.
- [x] `tests/Lumiere.Graphics.Tests/Settings/DefaultSettingsProviderTests.cs` and `tests/Lumiere.Graphics.Tests/App/SettingsPanelProjectionTests.cs` -- cover persistence and projection behavior in the matrix.

**Acceptance Criteria:**
- Given the settings panel is open, when the user chooses Clipboard, Folder, or Both, then the selected destination updates visually and the next output request reads the persisted `OutputTarget`.
- Given the settings panel is open, when the user toggles Copy as image off, then the toggle stays off after projection refresh and clipboard output is skipped by the existing output policy when applicable.
- Given unsupported output preferences are visible, when the settings panel is projected, then save path editing, timestamp, after-capture, and color/export options remain read-only or disabled.
- Given Lumiere is restarted after changing destination or copy-as-image, when settings load, then the same values are restored from local settings.

## Spec Change Log

## Verification

**Commands:**
- `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false` -- expected: succeeds after restore has already been performed.
- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false` -- expected: all tests pass.
- `dotnet format Lumiere.sln --verify-no-changes --verbosity minimal` -- expected: no formatting changes required.

## Suggested Review Order

**UI Interaction**

- Start with user entry.
  [`MainWindow.xaml:410`](../../src/Lumiere.App/MainWindow.xaml#L410)

- Follow destination writes.
  [`MainWindow.xaml.cs:165`](../../src/Lumiere.App/MainWindow.xaml.cs#L165)

- Verify clipboard toggle.
  [`MainWindow.xaml:693`](../../src/Lumiere.App/MainWindow.xaml#L693)

- Confirm keyboard styling.
  [`MainWindow.xaml.cs:1021`](../../src/Lumiere.App/MainWindow.xaml.cs#L1021)

**Settings Boundary**

- Review writer contract.
  [`IOutputSettingsWriter.cs:8`](../../src/Lumiere.Settings/IOutputSettingsWriter.cs#L8)

- Check persisted writes.
  [`DefaultSettingsProvider.cs:56`](../../src/Lumiere.Settings/DefaultSettingsProvider.cs#L56)

- Confirm app wiring.
  [`App.xaml.cs:48`](../../src/Lumiere.App/App.xaml.cs#L48)

**Projection And Tests**

- Inspect enabled projection.
  [`SettingsPanelProjection.cs:118`](../../src/Lumiere.App.Core/SettingsPanelProjection.cs#L118)

- Check projection coverage.
  [`SettingsPanelProjectionTests.cs:152`](../../tests/Lumiere.Graphics.Tests/App/SettingsPanelProjectionTests.cs#L152)

- Check persistence coverage.
  [`DefaultSettingsProviderTests.cs:72`](../../tests/Lumiere.Graphics.Tests/Settings/DefaultSettingsProviderTests.cs#L72)
