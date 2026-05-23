---
status: done
---

# Story 5.4: Add Output Preference Settings UI

Status: done

## Story

As a screenshot user,
I want settings for output target, save path, timestamp naming, copy-as-image, and supported after-capture behavior,
so that I can configure capture output once before using any entry point.

## Acceptance Criteria

1. **Given** configured output behavior is not yet implemented, **when** the output section is displayed, **then** output target, save path, timestamp naming, copy-as-image, and after-capture controls are hidden, disabled, read-only, or explicitly scoped as pending Epic 6 behavior.

2. **Given** Epic 6 implements configured output behavior, **when** output controls are enabled, **then** each enabled setting is consumed by the output pipeline and reflected in per-target completion or recoverable failure feedback.

3. **Given** Epic 6 has enabled output preference controls and clipboard output is part of the selected target, **when** clipboard settings are displayed, **then** copy-as-image is visible and does not imply HDR-preserving clipboard semantics.

4. **Given** after-capture and timestamp preferences are visible after Epic 6 enables output controls, **when** unsupported behavior lacks implementation semantics, **then** it is hidden, disabled, or clearly scoped until Epic 6 implements it.

## Tasks / Subtasks

- [x] **Task 1: Audit existing settings and output surfaces before editing** (AC: 1,2,3,4)
  - [x] Read `src/Lumiere.App/MainWindow.xaml`, `src/Lumiere.App/MainWindow.xaml.cs`, `src/Lumiere.App/SettingsPanelProjection.cs`, `src/Lumiere.Settings/ISettingsProvider.cs`, `src/Lumiere.Settings/DefaultSettingsProvider.cs`, and `src/Lumiere.Graphics/Output/*`.
  - [x] Confirm the current settings shell already has output, clipboard, timestamp, copy-as-image, and open-after-capture placeholders.
  - [x] Confirm current output behavior is still clipboard-only/basic usability through `IOutputService` and does not consume configurable output target, save path, timestamp naming, or after-capture behavior yet.

- [x] **Task 2: Strengthen output settings projection without enabling behavior** (AC: 1,2,3,4)
  - [x] Extend `SettingsPanelProjection` only if needed so output target, save path display, timestamp naming, copy-as-image, and after-capture affordance state are explicit, testable, and read-only/pending until Epic 6.
  - [x] Continue reading values from `ISettingsProvider`: `OutputTarget`, `SavePath`, `TimestampNaming`, and `CopyAsImage`.
  - [x] Do not add output settings writers, file persistence, folder validation, output policy, filename generation, or after-capture execution in this story.
  - [x] Use existing `OutputTarget` values (`Clipboard`, `Folder`, `Both`) instead of creating a parallel enum or UI-local destination vocabulary.

- [x] **Task 3: Upgrade native output and clipboard settings UI honestly** (AC: 1,3,4)
  - [x] Keep output target controls visible only as disabled/read-only/pending UI until Epic 6 owns configured output behavior.
  - [x] Show or add a save path row that is disabled/read-only/pending; if no path is configured, display an honest fallback such as `Not configured`.
  - [x] Keep timestamp naming and copy-as-image controls disabled/read-only or explicitly pending; their visual state may reflect current provider defaults but must not imply active persisted behavior.
  - [x] Keep open/reveal after capture disabled/read-only/pending until Epic 6 defines supported artifact behavior.
  - [x] Remove or relabel any export/color-format UI that implies HDR10, P3, sRGB, or HDR-preserving output support before semantics and validation exist.

- [x] **Task 4: Preserve shell behavior and accessibility** (AC: 1,3,4)
  - [x] Settings open/back must not stop capture, close overlay, dispose graphics resources, reset output/session state, or lose the latest main-panel projection.
  - [x] Settings content must scroll vertically at compact height without clipping labels, helper text, disabled reasons, or controls.
  - [x] Output target, save path, timestamp, copy-as-image, after-capture, and export/color-scope rows must have accessible names/help text that reveal pending/read-only state.
  - [x] State cues must use text plus control state or glyph; do not rely on color alone.

- [x] **Task 5: Keep architecture boundaries intact** (AC: 1,2,3,4)
  - [x] `MainWindow.xaml.cs` may apply projection to native controls and route existing events only; do not add product output policy there.
  - [x] `Lumiere.Settings` remains the settings source of truth; no UI-local copies that later need reconciliation.
  - [x] `Lumiere.Graphics.Output` remains the output model/service boundary; do not add folder output, filename policy, or per-target feedback behavior in this story.
  - [x] Do not add raw `HWND`, folder picker interop, Win32 shell launch/reveal behavior, WIC/bitmap conversion, COM pointers, WGC frame pool logic, D3D11 devices, or DXGI resources to settings UI.

- [x] **Task 6: Add focused hardware-independent tests** (AC: 1,2,3,4)
  - [x] Cover output target projection for `Clipboard`, `Folder`, and `Both`.
  - [x] Cover save path display for configured value and `Not configured` fallback.
  - [x] Cover timestamp naming and copy-as-image projection from provider defaults while remaining read-only/pending.
  - [x] Cover after-capture/export/color controls as disabled/read-only/pending metadata.
  - [x] Cover that copy-as-image helper text does not claim HDR-preserving clipboard output.

- [x] **Task 7: Validate and record limits** (AC: 1,2,3,4)
  - [x] Run `dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false`.
  - [x] Run `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false`.
  - [x] Run `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false`.
  - [x] Run `dotnet format Lumiere.sln --verify-no-changes --verbosity minimal`.
  - [x] If overlay/session or output execution behavior is touched unexpectedly, also run `dotnet test tests/Lumiere.Overlay.Tests/Lumiere.Overlay.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false`.
  - [x] Record Windows manual validation limits for rendered WinUI, keyboard navigation, screen reader exposure, high contrast, text scaling, DPI, and future clipboard/file output behavior.

### Review Findings

- [x] [Review][Patch] Add a non-color selected cue to the output destination display [src/Lumiere.App/MainWindow.xaml.cs:954]
- [x] [Review][Patch] Keep copy-as-image accessibility text honest about pending/read-only behavior [src/Lumiere.App.Core/SettingsPanelProjection.cs:95]
- [x] [Review][Patch] Prevent caption-region cleanup failures from interrupting settings view switches [src/Lumiere.App/MainWindow.xaml.cs:388]
- [x] [Review][Patch] Expose the full configured save path when the visible pill is truncated [src/Lumiere.App/MainWindow.xaml.cs:891]
- [x] [Review][Patch] Move or regroup color output metadata out of the HDR alerts section [src/Lumiere.App/MainWindow.xaml:316]

## Dev Notes

### Story Scope

This story upgrades the existing settings output and clipboard placeholders into a complete, honest settings UI surface for planned output preferences. It does **not** implement configured output behavior. Epic 6 owns output target policy, folder writes, timestamp filename policy, both-target completion, copy-as-image execution semantics, after-capture behavior, and per-target success/failure feedback.

In scope:

- Output target display for clipboard, folder, and both.
- Save path row with configured value or `Not configured` fallback.
- Timestamp naming, copy-as-image, and after-capture controls shown as disabled/read-only/pending.
- Export/color-format options hidden, disabled, or explicitly scoped as unsupported/unvalidated.
- Pure projection/test coverage for the above.

Out of scope:

- Folder picker UI, save-path validation, file permissions, folder writes, timestamp filename generation, opening/revealing output artifacts, output result model expansion, clipboard behavior changes, durable settings persistence, and HDR-preserving output claims.
- Any change to WGC, D3D11, DXGI, overlay, capture lifecycle, tray, or global hotkey behavior.

### Business and UX Context

Story 5.4 covers FR34, FR35, FR36, NFR24, UX-DR11, UX-DR12, UX-DR13, UX-DR14, UX-DR15, UX-DR16, and UX-DR18. Settings must let users understand future output preferences while preventing unsupported controls from appearing functional. [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.4`]

The UX design explicitly says output target, path, timestamp, copy-as-image, and after-capture controls must be hidden, disabled, read-only, or scoped pending Epic 6. It also says output controls must not imply clipboard HDR preservation. [Source: `_bmad-output/planning-artifacts/ux-design.md#Settings`]

The UX specification says settings should be grouped by user job, output target should be mutually exclusive between clipboard, folder, and both, folder/both requires save-path visibility and validation, and clipboard success must mean usability rather than HDR preservation unless validated. For Story 5.4, these are UI shape and honesty requirements only; validation and behavior come later. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Form Patterns`; `_bmad-output/planning-artifacts/ux-design-specification.md#Content Guidelines`]

### Current Implementation State

Read these files before implementation:

- `src/Lumiere.App/MainWindow.xaml`
- `src/Lumiere.App/MainWindow.xaml.cs`
- `src/Lumiere.App/SettingsPanelProjection.cs`
- `src/Lumiere.App/SettingsSectionHeader.xaml`
- `src/Lumiere.App/SettingsSectionHeader.xaml.cs`
- `src/Lumiere.Settings/ISettingsProvider.cs`
- `src/Lumiere.Settings/DefaultSettingsProvider.cs`
- `src/Lumiere.Settings/SettingsBoundary.cs`
- `src/Lumiere.Graphics/Output/OutputTarget.cs`
- `src/Lumiere.Graphics/Output/IOutputService.cs`
- `src/Lumiere.Graphics/Output/OutputResult.cs`
- `tests/Lumiere.Graphics.Tests/App/SettingsPanelProjectionTests.cs`
- `tests/Lumiere.Graphics.Tests/Settings/DefaultSettingsProviderTests.cs`

Current `MainWindow.xaml` already has a same-window settings shell and sections for `SHORTCUTS`, `HDR`, `OUTPUT`, `CLIPBOARD`, and `ABOUT`. The output area currently displays destination segments, disabled open-after-capture and timestamp toggles, and a disabled copy-as-image toggle. It also contains an `Export` segmented display with `HDR10`, `P3`, and `sRGB`; Story 5.4 must ensure this cannot be read as supported HDR/color output.

Current `MainWindow.xaml.cs` applies settings through `ApplySettingsProjection`. It already updates output destination segments from `SettingsPanelProjection.Output`, sets timestamp/copy-as-image toggle state from provider values, and keeps the settings shell separate from capture teardown. Preserve this pattern; avoid moving policy into event handlers.

Current `SettingsPanelProjection` includes:

- `OutputSettingsProjection.ReadOnly(OutputTarget outputTarget)`
- `TimestampNaming`
- `CopyAsImage`
- shortcut and HDR alert projection from Story 5.3

Story 5.4 should extend this projection if the UI needs explicit save path, pending reasons, after-capture, or export/color scope metadata. Keep this pure and hardware-independent.

Current `ISettingsProvider` includes output-related read properties:

- `OutputTarget`
- `SavePath`
- `TimestampNaming`
- `CopyAsImage`

Current `DefaultSettingsProvider` defaults are clipboard output, no save path, timestamp naming enabled, copy-as-image enabled, HDR alerts enabled, and no configured shortcuts. These are in-session/default values only; durable persistence is Story 5.5.

Current `IOutputService` and `OutputResult` already mention clipboard/folder/both concepts, but the active implementation path is still basic clipboard usability through `ClipboardOutputService`. Do not treat these types as proof that configured folder output is implemented.

### Previous Story Intelligence

Story 5.3 completed the shortcut and HDR alert settings UI. Important patterns to reuse:

- `SettingsPanelProjection` is the app-facing pure projection layer for settings display rules.
- `DefaultSettingsProvider` can provide in-session state only for explicitly scoped story behavior; Story 5.4 should not add a write seam because output behavior and persistence belong later.
- Settings UI should use native WinUI controls, accessible names/help text, and explicit pending copy.
- `MainWindow.xaml.cs` should stay projection/application glue, not a settings store or behavior owner.

Story 5.3 review findings matter:

- Monolithic repeated settings markup was called out. Prefer reusing `SettingsSectionHeader` and, if worthwhile, add small native settings-row components rather than duplicating large row structures.
- Icon approach should stay consistent; avoid mixing new icon systems.
- Compact `ToggleSwitch` styling must preserve native toggle semantics, focus, and theme behavior.
- Pending copy must be visible enough that disabled controls cannot be mistaken for implemented behavior.

Recent commits:

- `9349743 Mark story 5.3 done and record review findings`
- `0062a07 Polish settings UI and add startup failure logging`
- `c9dbaa5 feat: add native settings shell`
- `f95040f feat: build native v0 main panel`
- `00f756a docs: add epic 4 retro follow-through guardrails`

These reinforce the current approach: small native WinUI changes, pure projection tests, honest pending states, and no new dependency/version churn.

### Architecture Compliance

- `Lumiere.App` may own WinUI surface toggling, projection application, and gesture routing only.
- `Lumiere.Settings` owns settings defaults, future writes, persistence, validation, and migration.
- `Lumiere.Graphics.Output` owns output target/result models and future output behavior.
- `Lumiere.Infrastructure` owns future folder picker/shell interop if native platform APIs are needed.
- `Lumiere.Capture`, `Lumiere.Graphics`, and `Lumiere.Overlay` must not be changed for settings-only UI unless a read-only projection needs existing state.

Do not add output policy to `MainWindow.xaml.cs`. Do not add a second output-target enum. Do not make UI state the source of truth for output settings. Do not use words such as `HDR-preserving`, `HDR saved`, `HDR10 output`, `P3 export`, or `validated output` unless the implementation and Windows manual validation evidence exist.

### Implementation Guidance

Recommended low-risk shape:

- Extend `OutputSettingsProjection` with explicit fields such as `SavePathDisplayValue`, `SavePathHelpText`, `TimestampHelpText`, `CopyAsImageHelpText`, `AfterCaptureHelpText`, `ExportColorHelpText`, `IsReadOnly`, and `PendingReason` if that keeps XAML/code-behind simple and testable.
- Keep output target UI as a disabled segmented display or native `RadioButtons`/radio-style rows with `IsEnabled=False`. If using `RadioButtons`, keep it disabled/read-only and ensure accessible help text states that Epic 6 enables behavior.
- Add a disabled/read-only save path row. Do not open a folder picker or call Windows storage APIs.
- Keep disabled toggles for timestamp/copy-as-image/open-after-capture if they are visually useful, but their help text must say they are pending output behavior or persistence.
- Replace or scope the current export display. A safer label is `Color output` with `Not available`/`Pending validation`, or hide the `HDR10/P3/sRGB` segmented display entirely until Epic 6.5.
- Use short copy. Examples:
  - `Destination`
  - `Clipboard`
  - `Folder`
  - `Both`
  - `Save path`
  - `Not configured`
  - `Output behavior arrives in Epic 6`
  - `Clipboard image output is basic usability, not validated HDR preservation`
  - `Color/export options need encoder policy and Windows validation`

Avoid copy that suggests behavior is active:

- `Choose folder`
- `Browse`
- `Saved to`
- `HDR10`
- `P3`
- `sRGB`
- `Open file after capture`
- `Active`
- `Applied`
- `Will save`

### Latest Technical Information

The repository pins `Microsoft.WindowsAppSDK` to `1.8.260317003`. This story should not update package versions or add UI libraries. [Source: `Directory.Packages.props`; Microsoft Learn Windows App SDK 1.8 release notes: https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-1-8]

Microsoft's WinUI RadioButtons guidance treats radio buttons as a mutually exclusive selection control. If used for output target, keep them disabled/read-only because configured output behavior is not implemented until Epic 6. [Source: Microsoft Learn Radio buttons: https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/radio-button]

Microsoft's ToggleSwitch guidance says toggles are for binary settings that take effect when changed. Because timestamp, copy-as-image, and after-capture behavior do **not** take effect yet, disabled/read-only toggles must include pending help text, or a read-only row may be clearer. [Source: Microsoft Learn Toggle switches: https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/toggles]

Microsoft's app settings guidance favors simple settings, smart defaults, and shallow hierarchy. Keep the existing grouped settings shell rather than creating a multi-page settings experience. [Source: Microsoft Learn Guidelines for app settings: https://learn.microsoft.com/en-us/windows/apps/design/app-settings/guidelines-for-app-settings]

Windows folder picking and file output belong to later behavior stories. If Epic 6 uses a folder picker, it must be owned through the appropriate platform boundary and initialized for the WinUI window; do not add it in this UI-only story. [Source: Microsoft Learn File and folder pickers: https://learn.microsoft.com/en-us/windows/apps/develop/files/using-file-folder-pickers]

### File Structure Requirements

Expected touch points:

- `src/Lumiere.App/MainWindow.xaml` - upgrade output/clipboard/export rows in the existing settings shell.
- `src/Lumiere.App/MainWindow.xaml.cs` - apply any new projection fields to existing controls; no output policy or folder picker behavior.
- `src/Lumiere.App/SettingsPanelProjection.cs` - preferred place for pure output settings display, fallback, pending, and accessibility metadata.
- `tests/Lumiere.Graphics.Tests/App/SettingsPanelProjectionTests.cs` - pure tests for output settings projection.
- `src/Lumiere.App/SettingsSectionHeader.xaml(.cs)` or a new small app-facing settings row component - only if it reduces repeated settings markup without introducing broad abstraction.

Possible read-only references:

- `src/Lumiere.Settings/ISettingsProvider.cs`
- `src/Lumiere.Settings/DefaultSettingsProvider.cs`
- `src/Lumiere.Graphics/Output/OutputTarget.cs`
- `src/Lumiere.Graphics/Output/IOutputService.cs`
- `src/Lumiere.Graphics/Output/OutputResult.cs`

Avoid touching:

- `src/Lumiere.Graphics/Clipboard/ClipboardOutputService.cs`
- capture, overlay, D3D/DXGI, WGC, tray, hotkey, and infrastructure interop code
- `Directory.Packages.props`

### Testing Requirements

Automated tests should be hardware-independent and avoid rendered WinUI assertions.

Useful tests:

- `OutputSettingsProjection` maps `OutputTarget.Clipboard` to clipboard selected/read-only.
- `OutputSettingsProjection` maps `OutputTarget.Folder` to folder selected/read-only.
- `OutputSettingsProjection` maps `OutputTarget.Both` to both selected/read-only.
- Empty or whitespace `SavePath` projects as `Not configured`.
- Configured `SavePath` projects without trimming away meaningful path text except surrounding whitespace.
- Timestamp naming and copy-as-image reflect provider defaults but remain pending/read-only in metadata.
- After-capture and export/color controls remain disabled/read-only/pending.
- Copy-as-image help text explicitly avoids HDR-preserving claims.

Manual Windows validation must cover:

- Settings opens and returns to main panel with current capture/session state preserved.
- Output, save path, timestamp, copy-as-image, after-capture, and export/color rows are visible/readable and clearly pending/read-only.
- Keyboard focus skips or handles disabled controls predictably; no pointer-only path is required to understand pending state.
- Screen reader-visible names/help text communicate current value and pending reason.
- Text scaling, high contrast, compact height, and DPI scaling do not clip labels, values, helper text, or disabled reasons.

### Validation Commands

Use the repository validation commands from `AGENTS.md`:

```bash
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false
dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet format Lumiere.sln --verify-no-changes --verbosity minimal
```

If overlay/session interaction is touched unexpectedly, also run:

```bash
dotnet test tests/Lumiere.Overlay.Tests/Lumiere.Overlay.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
```

## Project Structure Notes

Production code must remain native Windows-only: `.NET 10`, `net10.0-windows10.0.19041.0`, x64, WinUI 3, Windows App SDK, WGC, D3D11, DXGI, Vortice, WinRT/COM interop. Do not add Electron, Tauri, WPF, WinForms, React, Tailwind, shadcn, Radix, Next.js, or web-stack dependencies. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]

Generated planning and story files belong in `_bmad-output/`; durable reusable guidance belongs in `harness/`. [Source: `_bmad-output/project-context.md#Development Workflow Rules`]

## References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.4`] - Story requirements and acceptance criteria.
- [Source: `_bmad-output/planning-artifacts/epics.md#Epic 5`] - Native settings scope and honest pending controls.
- [Source: `_bmad-output/planning-artifacts/ux-design.md#Settings`] - Output controls pending Epic 6 and no clipboard HDR-preservation implication.
- [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Form Patterns`] - Output target, save path, settings structure, and shared source of truth.
- [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Content Guidelines`] - Completion/output copy must avoid HDR claims without validation.
- [Source: `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`] - Module ownership boundaries.
- [Source: `_bmad-output/project-context.md`] - HDR, settings, output, testing, and validation guardrails.
- [Source: `_bmad-output/implementation-artifacts/epic-5-implementation-guardrails.md#Story 5.4: Output Preference Settings UI`] - Story-specific guardrails.
- [Source: `_bmad-output/implementation-artifacts/5-3-add-shortcut-and-hdr-alert-settings-ui.md`] - Previous story state, patterns, review findings, and validation notes.
- [Source: Microsoft Learn Windows App SDK 1.8 release notes](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-1-8) - Current pinned Windows App SDK release context.
- [Source: Microsoft Learn Radio buttons](https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/radio-button) - Native mutually exclusive option control guidance.
- [Source: Microsoft Learn Toggle switches](https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/toggles) - Native binary setting guidance.
- [Source: Microsoft Learn Guidelines for app settings](https://learn.microsoft.com/en-us/windows/apps/design/app-settings/guidelines-for-app-settings) - Native settings structure guidance.
- [Source: Microsoft Learn File and folder pickers](https://learn.microsoft.com/en-us/windows/apps/develop/files/using-file-folder-pickers) - Folder picker behavior belongs to later implementation scope.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-23: Story created by BMad create-story workflow. Target selected automatically from `sprint-status.yaml` as first backlog story: `5-4-add-output-preference-settings-ui`.
- 2026-05-23: Existing settings/output projection, Story 5.3 completion notes, Epic 5 guardrails, UX specs, architecture, project context, and recent commit history analyzed before writing story context.
- 2026-05-23: Dev workflow started; story and sprint status moved to in-progress.
- 2026-05-23: Task 1 audit confirmed current settings UI placeholders and clipboard-only output behavior before implementation edits.
- 2026-05-23: Added output projection metadata for save path fallback, read-only/pending state, timestamp, copy-as-image, after-capture, and color output.
- 2026-05-23: Upgraded output settings UI to display destination, save path, timestamp, after-capture, copy-as-image, and color output as explicit pending/read-only controls.
- 2026-05-23: Validation restore/build/format passed; `SettingsPanelProjectionTests` test execution hangs in the WinUI app-referencing test host and was captured with blame hang diagnostics.
- 2026-05-23: Projection test-host hang fixed by moving pure App projections into `Lumiere.App.Core`; full `Lumiere.Graphics.Tests` now completes successfully.
- 2026-05-23: Code review patch findings fixed for destination selected cue, copy-as-image pending/read-only accessibility text, caption-region cleanup resilience, full save-path tooltip, and color-output grouping.
- 2026-05-23: Post-review restore was refreshed from the user NuGet cache with local audit disabled; `dotnet build`, `dotnet test`, and `dotnet format --verify-no-changes --no-restore` pass.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story is ready for dev; implementation has not started.
- Task 1 audit completed: existing output UI is placeholder/read-only, and active output behavior remains basic clipboard usability through `IOutputService`/`ClipboardOutputService`.
- Implemented the planned output preference projection and native settings UI changes while keeping output behavior pending/read-only for Epic 6.
- Resolved the App projection test-host hang by extracting pure projections to `Lumiere.App.Core`; validation now completes without hang.
- Fixed all code review patch findings and marked Story 5.4 done.
- Post-review validation confirmed full build, graphics tests, and format verification after refreshing restore assets from the user NuGet cache.
- Initial validation before review patches completed: `dotnet restore`, `dotnet build`, full `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj`, and `dotnet format --verify-no-changes` passed. Manual rendered WinUI/accessibility validation remains a Windows review activity.

### File List

- `_bmad-output/implementation-artifacts/5-4-add-output-preference-settings-ui.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Lumiere.App/MainWindow.xaml`
- `src/Lumiere.App/MainWindow.xaml.cs`
- `src/Lumiere.App.Core/Lumiere.App.Core.csproj`
- `src/Lumiere.App.Core/AppShellProjection.cs`
- `src/Lumiere.App.Core/MainPanelProjection.cs`
- `src/Lumiere.App.Core/SettingsPanelProjection.cs`
- `src/Lumiere.App/Lumiere.App.csproj`
- `src/Lumiere.App/AppShellProjection.cs`
- `src/Lumiere.App/MainPanelProjection.cs`
- `src/Lumiere.App/SettingsPanelProjection.cs`
- `tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj`
- `tests/Lumiere.Graphics.Tests/App/AppShellProjectionTests.cs`
- `tests/Lumiere.Graphics.Tests/App/MainPanelProjectionTests.cs`
- `tests/Lumiere.Graphics.Tests/App/SettingsPanelProjectionTests.cs`

### Change Log

- 2026-05-23: Created ready-for-dev story context for output preference settings UI.
- 2026-05-23: Implemented output preference pending/read-only projection and UI surface; validation was initially blocked by the App projection test-host hang.
- 2026-05-23: Moved story to review after fixing App projection test-host hang and completing full graphics test validation.
- 2026-05-23: Addressed code review findings and marked story done.
