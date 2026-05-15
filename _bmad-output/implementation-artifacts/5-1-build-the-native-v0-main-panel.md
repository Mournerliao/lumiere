# Story 5.1: Build the Native v0 Main Panel

Status: in-progress

## Story

As a screenshot user,
I want the main window to match the compact v0 capture-first layout,
so that Lumiere feels like a focused screenshot utility rather than a dashboard prototype.

## Acceptance Criteria

1. **Given** the main window opens, **when** the v0-aligned panel is displayed, **then** it shows Lumiere identity, settings entry, fullscreen capture, region capture, shortcut labels, HDR status summary, and minimize/background intent in a compact native WinUI layout.

2. **Given** the user triggers fullscreen or region capture, **when** capture is active, **then** capture actions show lifecycle-driven active or disabled state and prevent duplicate triggers.

3. **Given** the UI is reviewed against `harness/design/v0-mvp-reference`, **when** differences are found, **then** production code follows WinUI/Fluent conventions while preserving layout intent, density, wording hierarchy, and information architecture.

## Tasks / Subtasks

- [ ] **Task 1: Audit the existing main panel and v0 reference before editing** (AC: 1,3)
  - [ ] Review `harness/design/v0-mvp-reference/` for layout intent, wording hierarchy, information architecture, and the intended main-panel/tray/settings vocabulary.
  - [ ] Review current `src/Lumiere.App/MainWindow.xaml` and `src/Lumiere.App/CaptureActionCard.xaml` before changing layout.
  - [ ] Keep web implementation details out of production code: no React, Tailwind, shadcn, Radix, OKLCH token copying, or web-specific component architecture.

- [ ] **Task 2: Reshape the native WinUI main panel** (AC: 1,3)
  - [ ] Preserve compact utility density with a header, primary capture action area, and status/footer area.
  - [ ] Show Lumiere identity and add a visible settings entry. Settings may be disabled or non-navigating in this story if Story 5.2 owns navigation, but it must be represented honestly.
  - [ ] Represent minimize/background intent without implementing tray/background behavior from Epic 7. If it is not functional yet, scope it as disabled, pending, or informational.
  - [ ] Present fullscreen and region capture as native WinUI controls, with Region treated as the defining flow while Fullscreen remains available.
  - [ ] Show shortcut labels from `ISettingsProvider.FullscreenShortcut` and `ISettingsProvider.RegionShortcut`; use explicit pending/unassigned copy when the provider returns empty strings.

- [ ] **Task 3: Project lifecycle-driven capture states into the panel** (AC: 2)
  - [ ] Continue routing capture button clicks through `ICaptureCommandCoordinator`; do not create a parallel capture path.
  - [ ] Keep `CaptureSessionState` as the shared lifecycle contract and `PreviewReadinessStatus` as the trust/readiness vocabulary.
  - [ ] Update button enabled/active/disabled visuals from lifecycle state rather than UI-local booleans where practical.
  - [ ] Prevent duplicate triggers while a capture is selecting target, initializing, capturing, degraded, unsupported, failed, or tearing down according to existing session state semantics.
  - [ ] Preserve current direct monitor capture behavior and overlay startup; this story changes the main panel surface, not the capture pipeline.

- [ ] **Task 4: Implement an HDR/trust status summary without overclaiming** (AC: 1,3)
  - [ ] Use text plus glyph/icon plus semantic color; color must not be the only discriminator.
  - [ ] Project existing readiness states into concise labels such as ready, enable HDR/degraded, unsupported, failed, and stopped without introducing a new parallel status enum.
  - [ ] Do not claim HDR-preserving clipboard/file output. Current clipboard behavior remains basic bitmap usability unless later output stories add policy and validation evidence.
  - [ ] Keep technical detail available only where it helps diagnostics; the default main panel should remain concise.

- [ ] **Task 5: Preserve architecture boundaries and avoid MainWindow growth** (AC: 1,2,3)
  - [ ] `MainWindow.xaml.cs` may project state, route gestures, and compose existing services only.
  - [ ] Do not add settings persistence, output policy, tray behavior, global hotkey registration, native resource ownership, or low-level Win32/COM/DXGI logic to `MainWindow.xaml.cs`.
  - [ ] If a reusable projection helper or view model is needed, keep it small and app-facing; only extract it if it removes real `MainWindow` responsibility.
  - [ ] Keep raw `HWND`, `HMONITOR`, COM pointers, WGC frame pools, D3D11 devices, and DXGI swap chains inside their owning boundaries.

- [ ] **Task 6: Add focused automated coverage** (AC: 1,2,3)
  - [ ] Add or update hardware-independent tests for any new projection logic, state-to-label mapping, shortcut display fallback, or capture action availability rules.
  - [ ] Keep pure capture/session tests in `tests/Lumiere.Graphics.Tests` while that is the established pattern.
  - [ ] Do not claim WinUI rendering, actual WGC/DXGI behavior, HDR display behavior, tray, hotkeys, DPI, or multi-monitor correctness from unit tests alone.

- [ ] **Task 7: Run validation and record limits** (AC: 1,2,3)
  - [ ] Run `dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false`.
  - [ ] Run `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false`.
  - [ ] Run `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false`.
  - [ ] Run `dotnet test tests/Lumiere.Overlay.Tests/Lumiere.Overlay.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false` if overlay-adjacent behavior is touched.
  - [ ] Run `dotnet format Lumiere.sln --verify-no-changes --verbosity minimal`.
  - [ ] Record any Mac-edit versus Windows CI-pass versus Windows manual-pass limits in completion notes.

## Dev Notes

### Story Scope

This is a **native WinUI main panel implementation story**. It updates the app's first visible surface so it matches the v0 MVP reference intent: compact Lumiere identity, fullscreen and region capture actions, shortcut labels, HDR status summary, settings entry, and minimize/background intent.

This story does **not** implement:

- Settings navigation or settings shell behavior beyond representing the settings entry honestly; Story 5.2 owns navigation.
- Shortcut editing, HDR alert settings, or global hotkey registration; Stories 5.3 and 7.3 own those.
- Output preference behavior, file output, after-capture behavior, or HDR-preserving output claims; Epic 6 owns output semantics.
- Tray icon/menu, background workflow, minimize-to-tray, or quit cleanup policy; Epic 7 owns tray and background behavior.
- Capture pipeline, overlay crop behavior, WGC frame pool semantics, FP16/scRGB swap-chain behavior, or output conversion policy.

### Business and UX Context

Epic 5 exists so users can operate Lumiere through a native WinUI experience that matches the v0 MVP reference intent while remaining honest about unsupported settings/output/tray/hotkey behavior. Story 5.1 is the first user-visible UI cutover for that epic. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 5`]

The PRD defines the MVP main window as compact and native, with Lumiere branding, fullscreen and region capture entry points, current shortcut display, HDR status summary, and a clear settings entry. The default MVP capture path must remain direct monitor capture without a picker-first interruption. [Source: `_bmad-output/planning-artifacts/prd.md#MVP - Minimum Viable Product`]

The selected UX direction is **Compact Reference Translation + Overlay-Centered Lens**. For this story, use the compact reference translation: dark-first utility posture, compact card-like surfaces, restrained indigo accent, large capture actions, bottom HDR status, and restrained settings access. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Chosen Direction`]

### Current Implementation State

Read these files before implementation:

- `src/Lumiere.App/MainWindow.xaml`
- `src/Lumiere.App/MainWindow.xaml.cs`
- `src/Lumiere.App/CaptureActionCard.xaml`
- `src/Lumiere.App/CaptureActionCard.xaml.cs`
- `src/Lumiere.App/App.xaml`
- `src/Lumiere.Settings/ISettingsProvider.cs`
- `src/Lumiere.Settings/DefaultSettingsProvider.cs`

Current `MainWindow.xaml` already has a dark shell, a Lumiere header, fullscreen button, region `CaptureActionCard`, capture status panel, bottom ready indicator, and custom title bar drag area. It does **not** yet show a settings entry, minimize/background intent, usable shortcut labels, or a richer non-color-only HDR/trust status summary aligned to the v0 reference.

Current `MainWindow.xaml.cs` routes fullscreen and region button clicks through `ExecuteCaptureFromUiAsync`, which calls `ICaptureCommandCoordinator` before starting direct monitor capture. It also owns preview/overlay orchestration, session-state projection, stale callback rejection, clipboard output handoff, and teardown logging. Preserve this behavior while reshaping the UI. Treat its size as a risk: add only projection/routing code required for this story.

Current `CaptureActionCard` is a reusable native XAML user control with title, description, glyph, icon brushes, shortcut text, and a click event. It is a good starting point for the v0 capture action buttons, but implementation should ensure disabled/active state and accessible names/reasons are clear.

Current `ISettingsProvider` exposes `FullscreenShortcut` and `RegionShortcut`; `DefaultSettingsProvider` returns empty strings because real shortcut configuration is not implemented yet. Story 5.1 should display these values through the provider and use honest fallback copy such as `Not assigned` or `Pending shortcut setup`, without implying global hotkeys work.

### Architecture Compliance

- `Lumiere.App` owns startup, composition, main-window orchestration, and wiring only.
- `Lumiere.Capture` owns capture command entry/session lifecycle through `ICaptureCommandCoordinator`, `CaptureService`, `CaptureSessionState`, and related typed results.
- `Lumiere.Graphics` owns D3D11/DXGI resources, HDR constants, swap-chain presentation, frame output, and output conversion policy.
- `Lumiere.Infrastructure` owns WinRT/COM/Win32 interop, native handle wrappers, diagnostics, tray/hotkey interop when added later, and OS boundary helpers.
- `Lumiere.Overlay` owns fullscreen overlay UI, crop geometry, pointer/keyboard routing, overlay state, and confirmed crop payloads.
- `Lumiere.Settings` owns local preferences, defaults, validation, future persistence, and migration semantics.

Do not create duplicate lifecycle/status vocabularies. Reuse `CaptureSessionState`, `CaptureSessionStatus`, `PreviewReadinessStatus`, and `PreviewReadinessState` for state projection. [Source: `_bmad-output/planning-artifacts/architecture.md#Patterns and Conventions`]

Do not introduce bitmap-first or SDR preview behavior. Preserve WGC `R16G16B16A16Float`, DXGI `R16G16B16A16_Float`, scRGB `RgbFullG10NoneP709`, and GPU-resident preview. [Source: `_bmad-output/project-context.md#Framework-Specific Rules`]

### Epic 5 Guardrails

`MainWindow.xaml.cs` may project state, route user gestures, and compose existing services. It must not become the owner of new product logic, native resource lifetimes, settings persistence, output policy, tray behavior, or hotkey registration. [Source: `_bmad-output/implementation-artifacts/epic-5-implementation-guardrails.md#Core Rule`]

For Story 5.1 specifically:

- Preserve capture command routing through `ICaptureCommandCoordinator`.
- Keep unsupported output/tray/hotkey controls hidden, disabled, read-only, or clearly scoped as pending.
- Use native WinUI/Fluent controls; do not import web patterns from the v0 reference.
- Do not add new settings or output behavior while reshaping the panel.
- Keep diagnostic detail out of the default user-facing path unless deliberately exposed by state.

### Previous Story and Git Intelligence

Epic 4 completed the MVP foundation cutover. The current architecture is usable but has one pressure point: `MainWindow.xaml.cs` still coordinates direct monitor target selection, preview/swap-chain lifecycle, overlay events, output request construction, session projection, UI-thread marshalling, and teardown. Epic 5 must avoid adding settings workflows, output option semantics, tray projection, or hotkey registration directly there. [Source: `_bmad-output/implementation-artifacts/epic-4-retro-2026-05-13.md#Recommended Epic 5 Guardrails`]

Recent commits reinforce the same pattern:

- `00f756a docs: add epic 4 retro follow-through guardrails` added `epic-5-implementation-guardrails.md`.
- `b006dfa feat: add diagnostic observability for capture lifecycle` added structured lifecycle logging in `MainWindow.xaml.cs`, `Lumiere.Capture`, and graphics disposal paths.
- `9bb4a58 feat: complete Story 4.5 validation and code review with disposal and InvalidCrop fixes` tightened disposal and overlay feedback behavior.

Implementation should preserve the Story 4.7 structured logging and stale callback safeguards while changing only the main panel UI/projection.

### Latest Technical Information

The repository currently pins `Microsoft.WindowsAppSDK` to `1.8.260317003`, which corresponds to Windows App SDK **1.8.6**, released March 18, 2026. No package update is required for this story; stay on the centralized package version unless a separate dependency story decides otherwise. [Source: `Directory.Packages.props`; Microsoft Learn Windows App SDK 1.8 release notes: https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-1-8]

WinUI and Windows App SDK controls documentation continues to point developers toward native controls and built-in control patterns; use `Button`, `FontIcon`/icon controls, theme resources, tooltips/automation properties, and standard focus behavior rather than custom web-style controls. [Source: Microsoft Learn controls overview: https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/; icons guidance: https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/icons]

Accessibility guidance requires text scaling support, high-contrast compatibility, keyboard navigation, and text that is programmatically available to assistive technologies. Do not rely on color-only HDR/status distinctions. [Source: Microsoft Learn accessibility overview: https://learn.microsoft.com/en-us/windows/apps/design/accessibility/accessibility; accessible text requirements: https://learn.microsoft.com/en-us/windows/apps/design/accessibility/accessible-text-requirements]

### File Structure Requirements

Expected touch points:

- `src/Lumiere.App/MainWindow.xaml` - reshape compact main panel layout, settings entry, shortcut labels, status summary, and minimize/background intent.
- `src/Lumiere.App/MainWindow.xaml.cs` - project existing typed state and settings-provider shortcut values into the panel; keep changes narrow.
- `src/Lumiere.App/CaptureActionCard.xaml` and `.xaml.cs` - update reusable capture action button behavior/properties only if needed for lifecycle state, accessible text, shortcut display, or disabled reason.
- `src/Lumiere.App/App.xaml` - add or refine WinUI resource brushes/styles only if they support this story's native visual system.
- `tests/Lumiere.Graphics.Tests/` - add hardware-independent tests if new pure projection or availability logic is extracted.

Avoid adding files in `Lumiere.Settings`, `Lumiere.Graphics.Output`, `Lumiere.Infrastructure`, or tray/hotkey-related locations unless implementation discovers a small boundary-owned projection helper is truly necessary. This story should not implement settings persistence, output target policy, tray, or hotkeys.

### Testing Requirements

Automated coverage should focus on pure logic, not rendered WinUI claims. Good candidates:

- Mapping `CaptureSessionState` / `PreviewReadinessStatus` into action availability and status summary labels.
- Shortcut display fallback when `ISettingsProvider` returns empty strings.
- Guard behavior ensuring capture actions cannot start duplicate sessions.

Manual Windows validation is still required for actual WinUI rendering, title bar behavior, focus visuals, text scaling, high contrast, WGC/DXGI/HDR behavior, DPI scaling, multi-monitor behavior, and overlay/capture behavior.

### Validation Commands

Use the repository validation commands from `AGENTS.md`:

```bash
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false
dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet format Lumiere.sln --verify-no-changes --verbosity minimal
```

If overlay-adjacent behavior changes, also run:

```bash
dotnet test tests/Lumiere.Overlay.Tests/Lumiere.Overlay.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
```

## Project Structure Notes

The production code must remain native Windows-only: `.NET 10`, `net10.0-windows10.0.19041.0`, x64, WinUI 3, Windows App SDK, WGC, D3D11, DXGI, Vortice, WinRT/COM interop. Do not add Electron, Tauri, WPF, WinForms, React, Tailwind, shadcn, Radix, Next.js, or web-stack dependencies. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]

Generated planning and story files belong in `_bmad-output/`; durable reusable guidance belongs in `harness/`. [Source: `_bmad-output/project-context.md#Development Workflow Rules`]

## References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.1`] - Story requirements and acceptance criteria.
- [Source: `_bmad-output/planning-artifacts/prd.md#MVP - Minimum Viable Product`] - Compact main window, capture entry points, shortcut display, HDR status, settings entry.
- [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Chosen Direction`] - Compact Reference Translation + Overlay-Centered Lens.
- [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Custom Components`] - Capture Action Button and Trust Status Badge requirements.
- [Source: `_bmad-output/planning-artifacts/architecture.md#Patterns and Conventions`] - Module boundaries and shared state vocabulary.
- [Source: `_bmad-output/project-context.md`] - Critical implementation, testing, and HDR invariant rules.
- [Source: `_bmad-output/implementation-artifacts/epic-5-implementation-guardrails.md`] - MainWindow responsibility guardrails.
- [Source: `_bmad-output/implementation-artifacts/epic-4-retro-2026-05-13.md`] - Previous epic learnings and Epic 5 risk guidance.
- [Source: Microsoft Learn Windows App SDK 1.8 release notes](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-1-8) - Current Windows App SDK 1.8.6 context.
- [Source: Microsoft Learn Windows controls overview](https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/) - Native WinUI control guidance.
- [Source: Microsoft Learn accessibility overview](https://learn.microsoft.com/en-us/windows/apps/design/accessibility/accessibility) - Keyboard, high contrast, UI Automation, and scaling expectations.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-15: Resolved workflow customization manually because local `python3` lacks stdlib `tomllib` required by `_bmad/scripts/resolve_customization.py`.
- 2026-05-15: Loaded `sprint-status.yaml`, project context, harness workflow guidance, v0 main-panel/tray reference files, and current WinUI main-panel files before editing.
- 2026-05-15: Attempted RED/GREEN validation with `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false`; blocked by local environment: `dotnet` command not found.
- 2026-05-15: Attempted repository validation commands (`dotnet restore`, `dotnet build`, graphics tests, overlay tests, `dotnet format`); all blocked by local environment: `dotnet` command not found.

### Completion Notes List

- Story context created by BMad create-story workflow on 2026-05-15.
- Ultimate context engine analysis completed - comprehensive developer guide created.
- Mac-edit implementation draft added for the compact native v0 main panel: settings entry is visible but disabled/pending, minimize/background intent is visible but disabled/pending, capture actions use the shared `CaptureActionCard`, and shortcut labels come from `ISettingsProvider` with `Not assigned` fallback.
- Capture action availability and footer trust status now project from `CaptureSessionState`/`PreviewReadinessStatus` through `MainPanelProjection`; duplicate capture triggers remain disabled for non-idle lifecycle states and the previous unconditional re-enable in `ExecuteCaptureFromUiAsync` was removed.
- Focused hardware-independent tests were added for shortcut fallback, lifecycle-driven capture availability, and readiness-to-trust-summary mapping, but they could not be executed in this macOS environment because the .NET SDK is unavailable.
- Validation level: Mac-edit only. Windows CI-pass and Windows manual-pass remain required before this story can be marked `review`.

### File List

- `_bmad-output/implementation-artifacts/5-1-build-the-native-v0-main-panel.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Lumiere.App/App.xaml`
- `src/Lumiere.App/CaptureActionCard.xaml`
- `src/Lumiere.App/MainPanelProjection.cs`
- `src/Lumiere.App/MainWindow.xaml`
- `src/Lumiere.App/MainWindow.xaml.cs`
- `tests/Lumiere.Graphics.Tests/App/MainPanelProjectionTests.cs`
- `tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj`

### Change Log

- 2026-05-15: Added Mac-edit implementation draft for Story 5.1 main panel and focused projection tests; story remains in-progress because required .NET/Windows validation could not run locally.
