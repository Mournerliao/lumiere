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

This slice restores native semantics where WinUI already has a better fit, while preserving the current layout density and keeping fidelity wording honest without forcing screen-reader users through internal release-process jargon:

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
6. Rewrote export-profile helper copy and automation text so unsupported HDR profiles are described in user language such as "currently unavailable", "shown for planning", and "kept as the current choice for this session" instead of internal wording like `validation-scoped`.
7. Extracted shell layout sizing into a typed projection seam so main/settings surfaces do not rely on scattered window-height constants.
8. Main-window sizing now reacts to main-panel alert visibility and shell switches, giving long HDR status text and `InfoBar` states more vertical room before compact utility content gets squeezed.
9. Reworked the main-panel body into a scrollable content region while keeping header, alert row, and footer fixed. This keeps capture actions primary and reachable under compact utility sizing instead of letting long state text compress the main workflow.
10. Added `CaptureTargetScopeProjection` so main-panel and overlay trust/detail text now prefix the active target explicitly instead of forcing users to infer which display or window the current HDR state belongs to.
11. Added a dedicated overlay fidelity projection seam so overlay status copy now uses the same selected-profile gate model as the main panel while keeping compact, native-feeling wording.
12. Normalized overlay fidelity labels to the same `Profile · Gate` cadence used elsewhere in the app, removing an inconsistent separator artifact from the compact capture surface.

## Suggested Review Order

1. [Settings shell markup](../../src/Lumiere.App/MainWindow.xaml) - native control substitutions and retained layout structure.
2. [Settings shell orchestration](../../src/Lumiere.App/MainWindow.xaml.cs) - `ToggleSwitch`, destination selection, export profile selection, automation text, button activation wiring, and state-driven shell resizing.
3. [Shell layout projection](../../src/Lumiere.App.Core/AppShellLayoutProjection.cs) - compact/alert/settings sizing policy expressed as a typed seam instead of hardcoded view logic.
4. [Main panel shell markup](../../src/Lumiere.App/MainWindow.xaml) - scroll boundary added so compact main-shell content adapts structurally before primary capture actions are squeezed.
5. [Settings projection](../../src/Lumiere.App.Core/SettingsPanelProjection.cs) - export profile selection is editable only where the profile contract allows it, while selected blocked profiles retain explicit accessibility state in user-facing wording.
6. [Layout tests](../../tests/Lumiere.Graphics.Tests/App/AppShellLayoutProjectionTests.cs) - alert-aware compact shell sizing coverage.
7. [Settings resources](../../src/Lumiere.App/App.xaml) - removal of now-unused handcrafted switch/segment styling.
8. [Design extension](../../harness/design/perfect-hdr-fidelity-extension.md) - native-fit guidance now aligned with the shipped control choices.

## Validation

- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --filter "FullyQualifiedName~AppShellLayoutProjectionTests|FullyQualifiedName~AppShellProjectionTests|FullyQualifiedName~MainPanelProjectionTests" --verbosity minimal /nr:false`
- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --filter "FullyQualifiedName~AppShellLayoutProjectionTests|FullyQualifiedName~MainPanelProjectionTests|FullyQualifiedName~OutputResultProjectionTests|FullyQualifiedName~AppShellProjectionTests" --verbosity minimal /nr:false`
- `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false`
- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-build --verbosity minimal /nr:false`
- `dotnet test tests/Lumiere.Overlay.Tests/Lumiere.Overlay.Tests.csproj -p:Platform=x64 --no-build --verbosity minimal /nr:false`

## Remaining Work

Story 13-2 is still `in-progress`, not `done`.

Remaining follow-up that still belongs to this story:

- Run Windows manual accessibility checks for keyboard, Narrator/screen reader, high contrast, text scaling, and DPI.
- Run Windows manual checks that main-panel alert text, compact utility sizing, and settings-shell layout remain usable at 100%, 125%, 150%, and 200% DPI / text scaling.
- Run manual checks on the native export profile radio-button path, especially selected current-session states for blocked HDR profiles.
- Record explicit manual validation evidence instead of relying on code inspection and CI alone.
