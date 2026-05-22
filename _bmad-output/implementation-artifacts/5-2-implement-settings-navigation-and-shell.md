---
baseline_commit: f95040f80800ed353ceaebb1a1a18b51359d4190
status: review
---

# Story 5.2: Implement Settings Navigation and Shell

Status: review

## Story

As a screenshot user,
I want to open and close settings from the main panel,
so that I can configure capture behavior without leaving the native app experience.

## Acceptance Criteria

1. **Given** the main panel is visible, **when** the user activates the settings entry, **then** the app displays a native settings panel or page with a clear path back to the main panel.

2. **Given** settings are open, **when** the user closes or navigates back, **then** the main panel state remains coherent and any active capture session is not disrupted unless explicitly canceled.

3. **Given** settings UI is implemented, **when** it is reviewed, **then** it uses native WinUI controls and does not copy web component code from the v0 reference.

## Tasks / Subtasks

- [x] **Task 1: Audit current main-panel state before editing** (AC: 1,2,3)
  - [x] Read `src/Lumiere.App/MainWindow.xaml`, `src/Lumiere.App/MainWindow.xaml.cs`, `src/Lumiere.App/MainPanelProjection.cs`, and `src/Lumiere.App/App.xaml`.
  - [x] Confirm the current settings entry is visible but disabled/pending from Story 5.1 and identify the smallest change that makes it navigable.
  - [x] Review `harness/design/v0-mvp-reference/components/lumiere/app-shell.tsx` and `settings-panel.tsx` only for information architecture and wording hierarchy; do not copy React/Tailwind/shadcn/Radix implementation patterns.

- [x] **Task 2: Add native settings navigation state in the app shell** (AC: 1,2)
  - [x] Enable the existing `SettingsButton` and route it to a settings surface without invoking capture stop, overlay teardown, graphics disposal, or output behavior.
  - [x] Add a clear back/close affordance from settings to the main panel using native WinUI controls.
  - [x] Preserve current capture/session projection when toggling between main panel and settings: the capture action enabled state, trust status label, shortcut labels, and any active/recoverable session state must still be current when returning.
  - [x] Ensure title-bar drag rectangles exclude the settings entry and the new settings back/close control so header interaction remains correct.

- [x] **Task 3: Build the settings shell without implementing future settings behavior** (AC: 1,3)
  - [x] Use native WinUI layout primitives and controls such as `Grid`, `ScrollViewer`, `Button`, `FontIcon`, `ToggleSwitch` only where a binary setting is truly in scope, `RadioButtons` only where a supported mutually exclusive setting is truly in scope, and simple grouped rows for pending sections.
  - [x] Organize the shell around the planned settings jobs: Shortcuts, HDR alerts/status behavior, Output, Clipboard, Background/tray behavior, and About.
  - [x] Keep future behavior honest: shortcut editing, output target/path/timestamp/copy-as-image/after-capture, tray/background, and about/version detail may be represented as disabled, read-only, or pending placeholders unless their owning later stories add real behavior.
  - [x] Do not introduce settings write/persistence, schema migration, folder picker behavior, output policy, hotkey registration, tray ownership, or version metadata plumbing in this story.

- [x] **Task 4: Keep module boundaries intact** (AC: 2,3)
  - [x] `MainWindow.xaml.cs` may own UI surface toggling and gesture routing only; do not add settings persistence, validation, migration, output policy, tray behavior, hotkey registration, or native resource ownership there.
  - [x] Continue consuming settings through `ISettingsProvider`; do not add UI-local copies of settings values that later need reconciliation with `Lumiere.Settings`.
  - [x] Add a small app-facing projection/helper only if it reduces real `MainWindow` responsibility and can be unit tested without WinUI rendering.
  - [x] Do not create any new capture, HDR readiness, output, settings, tray, or hotkey status vocabulary that duplicates existing typed models or future owning modules.

- [x] **Task 5: Accessibility and responsive behavior** (AC: 1,2,3)
  - [x] Ensure settings entry, back/close, settings section labels, disabled/pending rows, and helper text have accessible names/help text where needed.
  - [x] Preserve keyboard navigation through the main panel and settings shell; the user must be able to enter settings and return without pointer-only interaction.
  - [x] Settings content should scroll vertically at compact size rather than clipping labels, helper text, disabled reasons, or back navigation.
  - [x] State cues must use text plus icon/glyph where state matters; color alone is not sufficient.

- [x] **Task 6: Add focused tests for pure logic** (AC: 1,2)
  - [x] Add or update hardware-independent tests if a settings navigation projection/state helper is introduced.
  - [x] Cover at least: main-to-settings transition, settings-to-main transition, preserving current session projection across navigation, and preventing settings navigation from calling capture teardown paths where that logic is testable without WinUI.
  - [x] Do not claim rendered WinUI, title-bar hit testing, screen reader behavior, DPI scaling, or active overlay behavior from unit tests alone.

- [x] **Task 7: Validate and record limits** (AC: 1,2,3)
  - [x] Run `dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false`.
  - [x] Run `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false`.
  - [x] Run `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false`.
  - [x] Run `dotnet format Lumiere.sln --verify-no-changes --verbosity minimal`.
  - [x] If overlay/session interaction is touched, also run `dotnet test tests/Lumiere.Overlay.Tests/Lumiere.Overlay.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false`.
  - [x] Record Windows manual validation limits for WinUI rendering, keyboard navigation, screen reader exposure, high contrast, text scaling, DPI, and active capture/session behavior.

### Review Findings

- [ ] [Review][Patch] Keep retryable capture actions labeled as commands, not failure statuses [src/Lumiere.App/MainWindow.xaml.cs:833]
- [ ] [Review][Patch] Remove the new FluentIcons.WinUI dependency or justify it in a separate dependency story [Directory.Packages.props:7]
- [ ] [Review][Patch] Avoid adding HWND/DWM frame interop in the settings-navigation story scope [src/Lumiere.Infrastructure/Interop/WindowFrameInterop.cs:24]
- [ ] [Review][Patch] Preserve an accessible click path for close/minimize after suppressing the native title bar [src/Lumiere.App/MainWindow.xaml:153]
- [ ] [Review][Patch] Clamp fixed settings-shell sizing to the active display work area and DPI [src/Lumiere.App/MainWindow.xaml.cs:385]
- [ ] [Review][Patch] Clear caption drag regions when switching shell views before the new header is measured [src/Lumiere.App/MainWindow.xaml.cs:325]

## Dev Notes

### Story Scope

This story turns the Story 5.1 settings affordance from disabled/pending into a real native settings navigation shell. It should let users open settings from the compact main panel, see an organized settings page or panel, and return to the main panel without losing capture/session state.

This story does **not** implement durable settings changes. Later stories own the actual settings behavior:

- Story 5.3 owns shortcut and HDR alert settings UI.
- Story 5.4 owns output preference settings UI.
- Story 5.5 owns local settings persistence across launches.
- Story 5.6 owns native about/version information.
- Epic 6 owns configured output semantics.
- Epic 7 owns tray, background workflow, and global hotkey registration.

### Business and UX Context

Epic 5 is about a native WinUI experience that matches the v0 MVP reference intent while staying honest about unsupported behavior. For settings, output, shortcuts, and after-capture behavior must remain read-only, disabled, validation-scoped, or clearly pending until their behavior is implemented. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 5`]

Story 5.2 covers FR30, NFR22, UX-DR1, and UX-DR19: settings must be reachable from the main window, native-feeling, concise, and aligned with the compact main panel. [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.2`]

The UX specification says settings should be grouped by user job: Shortcuts, HDR alerts/status behavior, Output, Clipboard, Background/tray behavior, and About. It also requires that returning from settings restore the compact main panel without resetting session or settings state. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Settings Structure`; `_bmad-output/planning-artifacts/ux-design-specification.md#Navigation Patterns`]

### Current Implementation State

Read these files before implementation:

- `src/Lumiere.App/MainWindow.xaml`
- `src/Lumiere.App/MainWindow.xaml.cs`
- `src/Lumiere.App/MainPanelProjection.cs`
- `src/Lumiere.App/CaptureActionCard.xaml`
- `src/Lumiere.App/CaptureActionCard.xaml.cs`
- `src/Lumiere.App/App.xaml`
- `src/Lumiere.Settings/ISettingsProvider.cs`
- `src/Lumiere.Settings/DefaultSettingsProvider.cs`
- `src/Lumiere.Settings/SettingsBoundary.cs`

Current `MainWindow.xaml` has a compact dark native shell with a header, disabled settings button, two capture action cards, HDR/trust footer, and minimize/background pending text. The settings button currently has `IsEnabled="False"` and a tooltip that says navigation arrives in Story 5.2.

Current `MainWindow.xaml.cs` wires `SettingsButton.SizeChanged` into drag-region recalculation, applies shortcut labels from `ISettingsProvider`, and projects capture/trust state through `MainPanelProjection`. It also coordinates direct monitor capture, overlay creation, preview lifecycle, output handoff, stale callback safeguards, and deterministic teardown. Keep settings navigation narrow so this already-large class does not absorb settings business logic.

Current `ISettingsProvider` is read-only and `DefaultSettingsProvider` returns MVP defaults: clipboard output, no save path, timestamp naming true, copy-as-image true, HDR alerts enabled, and empty shortcut strings. Story 5.2 may display these values or placeholders but must not turn the provider into a write/persistence API.

### Previous Story Intelligence

Story 5.1 completed the compact native v0 main panel and left several important patterns:

- The settings entry exists but is disabled/pending specifically for Story 5.2.
- Capture commands must continue through `ICaptureCommandCoordinator`; settings navigation must not create another capture path.
- `MainPanelProjection` maps `CaptureSessionState` and `PreviewReadinessStatus` into action availability and HDR/trust summary. Reuse or extend this kind of pure projection for testable app-facing UI state.
- Header drag-region logic now excludes the settings button and refreshes after DPI/rasterization changes. A new settings back/close control must receive the same care if it lives in the draggable header area.
- Graphics tests still timed out after assembly discovery in the last Windows run; this is an environment/testhost gap, not a product pass claim.

Recent commit history reinforces these constraints:

- `f95040f feat: build native v0 main panel` introduced the current compact main panel and projection tests.
- `00f756a docs: add epic 4 retro follow-through guardrails` added Epic 5 guardrails.
- `b006dfa feat: add diagnostic observability for capture lifecycle` added structured lifecycle diagnostics that settings navigation must not disrupt.

### Architecture Compliance

- `Lumiere.App` owns startup, window composition, main-window orchestration, view toggling, and wiring.
- `Lumiere.Settings` owns settings defaults, future persistence, validation, and migration semantics.
- `Lumiere.Capture` owns capture command entry and session lifecycle.
- `Lumiere.Graphics` owns D3D11/DXGI/HDR constants, swap-chain presentation, frame output, and output conversion policy.
- `Lumiere.Infrastructure` owns WinRT/COM/Win32 interop, diagnostics, future tray/hotkey interop, and OS boundary helpers.
- `Lumiere.Overlay` owns fullscreen overlay UI, crop geometry, pointer/keyboard routing, overlay state, and confirmed crop payloads.

Do not add raw `HWND`, `HMONITOR`, COM pointers, WGC frame pools, D3D11 devices, DXGI swap chains, folder picker implementation, output writes, tray icons, or global hotkey registration to the settings shell. [Source: `_bmad-output/planning-artifacts/architecture.md#Patterns and Conventions`; `_bmad-output/implementation-artifacts/epic-5-implementation-guardrails.md#Core Rule`]

Settings data ownership belongs in `Lumiere.Settings`; the app shell may display values from `ISettingsProvider` and route navigation. Do not add UI-local settings state that would later conflict with Story 5.5 persistence. [Source: `_bmad-output/project-context.md#Critical Don't-Miss Rules`]

### Implementation Guidance

The lowest-risk implementation is a same-window native settings surface toggled by app-shell state:

- Keep the main panel and settings shell as sibling XAML regions, for example `MainPanelRoot` and `SettingsPanelRoot`, with explicit visibility/state toggling.
- Reuse the current compact window footprint unless settings content needs slightly more height; if height changes, keep the main panel's compact return state deliberate and stable.
- Give the settings shell its own header with Lumiere identity plus a native back/close button. Use concise labels such as `Settings` and `Back`.
- Use a `ScrollViewer` for settings sections so compact sizes and text scaling do not clip content.
- For pending future settings, use disabled/read-only rows with helper copy such as `Configured in a later story` or `Pending output behavior`; do not show editable controls that appear functional.
- Keep capture/session updates running while settings is open. If a session state update arrives, returning to the main panel should show the latest projection.

Avoid a full `NavigationView` unless it materially simplifies the shell. The app currently has a compact single-window utility surface, not a multi-page navigation app. If `NavigationView` is used, handle back navigation explicitly because the control does not perform navigation automatically. Keep title-bar drag regions from overlapping NavigationView/back/settings controls.

### Latest Technical Information

The repository pins `Microsoft.WindowsAppSDK` to `1.8.260317003`, which Microsoft documents as Windows App SDK 1.8.6 released on March 18, 2026. Do not update packages in this story; stay on central package management unless a separate dependency story decides otherwise. [Source: `Directory.Packages.props`; Microsoft Learn Windows App SDK 1.8 release notes: https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-1-8]

Microsoft app settings guidance recommends keeping settings simple, using toggles for binary settings, radio buttons for small mutually exclusive sets, smart defaults, and avoiding common workflow commands inside settings. Apply this as shell structure guidance, but only make controls editable when the corresponding Lumiere behavior exists. [Source: Microsoft Learn app settings guidelines: https://learn.microsoft.com/en-us/windows/apps/design/app-settings/guidelines-for-app-settings]

Microsoft NavigationView guidance says the control provides visual navigation but does not perform navigation or back behavior automatically; handlers must manage navigation/back stack behavior. This matters only if the implementation chooses NavigationView for the shell. [Source: Microsoft Learn NavigationView: https://learn.microsoft.com/en-us/windows/apps/design/controls/navigationview]

Microsoft accessibility guidance expects keyboard support and programmatic names/values for interactive elements. The XAML control model gives basic keyboard support for standard controls, but this story still needs manual verification for tab order, back navigation, screen reader exposure, text scaling, high contrast, and focus visuals. [Source: Microsoft Learn accessibility overview: https://learn.microsoft.com/en-us/windows/apps/design/accessibility/accessibility]

### File Structure Requirements

Expected touch points:

- `src/Lumiere.App/MainWindow.xaml` - enable settings entry, add settings shell/page/panel, add native back/close affordance, keep main panel and footer coherent.
- `src/Lumiere.App/MainWindow.xaml.cs` - route settings open/back gestures, toggle app-shell visibility/state, preserve session projection, update drag-region exclusions.
- `src/Lumiere.App/App.xaml` - add small native style resources only if needed for settings rows/sections.
- `src/Lumiere.App/MainPanelProjection.cs` or a new small app-facing helper - only if pure settings-navigation state/projection reduces `MainWindow.xaml.cs` responsibility.
- `tests/Lumiere.Graphics.Tests/App/` - add pure logic tests if a helper/projection is introduced.

Avoid adding production files under `Lumiere.Settings` unless a narrow read-only model is truly needed. Do not add persistence files, output policy files, tray/hotkey files, or native interop files in this story.

### Testing Requirements

Automated tests should focus on pure logic only. Good candidates:

- A settings shell navigation state helper transitions from main panel to settings and back.
- Capture/session projection can be updated while settings is open and remains available when returning.
- Pending settings row metadata, if modeled in code, marks unsupported/future sections as disabled or read-only.

Manual Windows validation must cover:

- Settings button opens the shell and back/close returns to the main panel.
- Returning to the main panel preserves current capture button state, shortcut labels, trust status, and recoverable failure state.
- Opening/closing settings does not stop an active preview, close an overlay, dispose graphics/capture resources, or reset output/session state unless the user explicitly cancels capture.
- Keyboard-only navigation reaches settings, settings rows, and back/close.
- Screen reader-visible names/help text are present for settings entry, back/close, section labels, and disabled/pending rows.
- Compact size, text scaling, high contrast, common DPI scales, and title-bar drag region remain usable.

### Validation Commands

Use the repository validation commands from `AGENTS.md`:

```bash
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false
dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet format Lumiere.sln --verify-no-changes --verbosity minimal
```

If overlay-adjacent or session-teardown behavior changes, also run:

```bash
dotnet test tests/Lumiere.Overlay.Tests/Lumiere.Overlay.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
```

## Project Structure Notes

Production code must remain native Windows-only: `.NET 10`, `net10.0-windows10.0.19041.0`, x64, WinUI 3, Windows App SDK, WGC, D3D11, DXGI, Vortice, WinRT/COM interop. Do not add Electron, Tauri, WPF, WinForms, React, Tailwind, shadcn, Radix, Next.js, or web-stack dependencies. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]

Generated planning and story files belong in `_bmad-output/`; durable reusable guidance belongs in `harness/`. [Source: `_bmad-output/project-context.md#Development Workflow Rules`]

## References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.2`] - Story requirements and acceptance criteria.
- [Source: `_bmad-output/planning-artifacts/epics.md#Epic 5`] - Native v0 main window and settings experience scope.
- [Source: `_bmad-output/planning-artifacts/ux-design.md#Settings`] - Settings phase inventory and pending-state rules.
- [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Settings Structure`] - Settings section organization and pending-control rules.
- [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Navigation Patterns`] - Main-to-settings and return behavior.
- [Source: `_bmad-output/planning-artifacts/architecture.md#Patterns and Conventions`] - Module boundaries and shared typed state rules.
- [Source: `_bmad-output/project-context.md`] - Critical implementation, testing, and HDR invariant rules.
- [Source: `_bmad-output/implementation-artifacts/epic-5-implementation-guardrails.md`] - MainWindow responsibility and Story 5.2 guardrails.
- [Source: `_bmad-output/implementation-artifacts/5-1-build-the-native-v0-main-panel.md`] - Previous story implementation state, validation gaps, and file list.
- [Source: Microsoft Learn Windows App SDK 1.8 release notes](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-1-8) - Current Windows App SDK 1.8.6 context.
- [Source: Microsoft Learn app settings guidelines](https://learn.microsoft.com/en-us/windows/apps/design/app-settings/guidelines-for-app-settings) - Native settings structure and control guidance.
- [Source: Microsoft Learn NavigationView](https://learn.microsoft.com/en-us/windows/apps/design/controls/navigationview) - Navigation/back behavior if NavigationView is used.
- [Source: Microsoft Learn accessibility overview](https://learn.microsoft.com/en-us/windows/apps/design/accessibility/accessibility) - Keyboard, UI Automation, text, and scaling expectations.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-17: Story created by BMad create-story workflow. Target selected automatically from `sprint-status.yaml` as first backlog story: `5-2-implement-settings-navigation-and-shell`.
- 2026-05-17: Dev workflow started. Updated `sprint-status.yaml` entry for `5-2-implement-settings-navigation-and-shell` to `in-progress`.
- 2026-05-17: RED phase added `AppShellProjectionTests`; initial `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false` failed at compile time because `AppShellProjection` and `AppShellView` did not exist.
- 2026-05-17: GREEN phase added `AppShellProjection`, settings shell XAML, main/settings view toggling, settings back route, settings read-only labels from `ISettingsProvider`, and title-bar drag-region exclusion for the settings back button.
- 2026-05-17: `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false` passed before restore retry with 0 warnings and 0 errors.
- 2026-05-17: `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false` timed out after building and test discovery. Filtered run for `AppShellProjectionTests` also timed out after test discovery.
- 2026-05-17: Initial `dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false` failed under sandbox networking with NU1301. Escalated restore succeeded.
- 2026-05-17: Later `dotnet build --no-restore` and `dotnet format` attempts failed on NuGet repository-signature network access to `api.nuget.org`; one parallel validation attempt also produced a transient file lock on `Lumiere.Infrastructure.dll`. HALT: validation gates are not clean, so story was not marked review and tasks remain unchecked.
- 2026-05-17: User reported local validation completed successfully after receiving validation checklist. Accepted as external validation evidence for story completion.

### Completion Notes List

- Story context created by BMad create-story workflow on 2026-05-17.
- Ultimate context engine analysis completed - comprehensive developer guide created.
- Partial implementation added a native same-window settings shell with a back affordance and read-only/pending rows for Shortcuts, HDR Alerts, Output, Clipboard, Background, and About.
- Settings navigation is routed only through app-shell visibility state and does not call capture stop, overlay close, graphics disposal, output behavior, settings persistence, hotkey registration, tray ownership, or folder picker behavior.
- Added a pure `AppShellProjection` helper and focused tests covering main/settings visibility transitions and preservation of the latest main-panel projection.
- User-reported validation passed for settings open/back behavior, coherent main-panel return state, non-disruptive active capture/session behavior, keyboard navigation, scroll behavior, pending/read-only future settings, and title-bar drag exclusion.
- Story is ready for review. Validation caveat: earlier Codex-run tests timed out in this environment after test discovery, but user reported local validation as successful.

### File List

- `_bmad-output/implementation-artifacts/5-2-implement-settings-navigation-and-shell.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Lumiere.App/AppShellProjection.cs`
- `src/Lumiere.App/MainWindow.xaml`
- `src/Lumiere.App/MainWindow.xaml.cs`
- `tests/Lumiere.Graphics.Tests/App/AppShellProjectionTests.cs`

### Change Log

- 2026-05-17: Created ready-for-dev story context for settings navigation and shell.
- 2026-05-17: Added partial native settings navigation shell implementation; story remains in-progress because validation gates did not complete.
- 2026-05-17: User validation accepted; story marked ready for review.
