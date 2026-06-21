---
title: 'Harden Native Settings And Accessibility Semantics'
type: 'feature'
created: '2026-06-22'
status: 'in-progress'
route: 'vertical-slice'
story: '13-2'
---

# Harden Native Settings And Accessibility Semantics

## Intent

`Public perfect-HDR-fidelity` still had a mismatch between the native-settings design intent and the actual WinUI semantics in code. The settings surface looked close to the prototype, but several controls were still custom button-shaped toggles, segmented buttons, or `Tapped`-only activators.

This slice restores native semantics where WinUI already has a better fit, while preserving the current layout density and fidelity copy:

- `ToggleSwitch` for immediate binary settings.
- Native single-choice selection for output destination.
- Native single-choice semantics for export profile selection, while keeping validation-scoped profile status visible.
- Standard `Button` click activation for shortcut capture and folder browsing rows.

## Delivered In This Slice

1. Replaced custom button-drawn switches with native `ToggleSwitch` controls for:
   - HDR alerts
   - Open after capture
   - Timestamp naming
   - Copy as image
2. Replaced the output destination custom segmented buttons with a native single-choice control path.
3. Replaced the export profile custom segmented buttons with native radio-button semantics and restored the supported `sRGB` write path so users can return from validation-scoped profiles to the compatibility profile.
   - Validation-scoped profiles that remain the persisted selection now stay keyboard-focusable and screen-reader-readable for the current session, while still blocking unsupported runtime switching.
4. Replaced `Tapped`-only shortcut and save-path activators with standard `Button.Click` activation.
5. Removed the obsolete handcrafted switch and segmented-button visual resources and code-behind state painter that had been compensating for the non-native controls.
6. Kept existing fidelity wording and validation-scoped copy intact so the accessibility pass does not re-open the fidelity-contract language.

## Suggested Review Order

1. [Settings shell markup](../../src/Lumiere.App/MainWindow.xaml) - native control substitutions and retained layout structure.
2. [Settings shell orchestration](../../src/Lumiere.App/MainWindow.xaml.cs) - `ToggleSwitch`, destination selection, export profile selection, automation text, and button activation wiring.
3. [Settings projection](../../src/Lumiere.App.Core/SettingsPanelProjection.cs) - export profile selection is editable only where the profile contract allows it, while selected validation-scoped profiles retain explicit accessibility state.
4. [Settings resources](../../src/Lumiere.App/App.xaml) - removal of now-unused handcrafted switch/segment styling.
5. [Design extension](../../harness/design/perfect-hdr-fidelity-extension.md) - native-fit guidance now aligned with the shipped control choices.

## Validation

- `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false`
- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-build --verbosity minimal /nr:false`
- `dotnet test tests/Lumiere.Overlay.Tests/Lumiere.Overlay.Tests.csproj -p:Platform=x64 --no-build --verbosity minimal /nr:false`

## Remaining Work

Story 13-2 is still `in-progress`, not `done`.

Remaining follow-up that still belongs to this story:

- Run Windows manual accessibility checks for keyboard, Narrator/screen reader, high contrast, text scaling, and DPI.
- Run manual checks on the native export profile radio-button path, especially selected locked-for-session states for validation-scoped profiles.
- Record explicit manual validation evidence instead of relying on code inspection and CI alone.
