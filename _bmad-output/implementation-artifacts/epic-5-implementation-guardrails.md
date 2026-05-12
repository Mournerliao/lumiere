# Epic 5 Implementation Guardrails

Date: 2026-05-13
Source: Epic 4 retrospective and architecture review
Scope: Epic 5 main window and settings work

## Purpose

Epic 4 left Lumiere with a sound MVP foundation and one visible pressure point: `MainWindow.xaml.cs` still coordinates too much workflow knowledge. Epic 5 can proceed, but UI work must not turn `MainWindow` into the application layer for settings, output, tray, or hotkeys.

Use this document before creating or implementing Story 5.1+.

## Core Rule

`MainWindow.xaml.cs` may project state, route user gestures, and compose existing services. It must not become the owner of new product logic, native resource lifetimes, settings persistence, output policy, tray behavior, or hotkey registration.

When in doubt, add the behavior to the owning module behind a narrow interface, then let `MainWindow` consume that interface.

## What May Stay In `MainWindow.xaml.cs`

- WinUI window setup, title bar, sizing, backdrop, and top-level event handlers.
- Binding/projection from typed state to visible labels, button enabled states, and native UI controls.
- Routing main-window capture button clicks through `ICaptureCommandCoordinator`.
- Opening or closing UI surfaces by calling owning-module services or coordinators.
- UI-thread dispatch for app-visible state updates when the code is already in the app shell.
- Temporary MVP composition glue when no owning-module behavior exists yet, provided the story records why it is temporary.

## What Must Not Be Added To `MainWindow.xaml.cs`

- New settings persistence, validation, migration, or defaulting rules.
- New output target policy, per-target success/failure semantics, file naming, folder writes, or HDR output claims.
- Tray icon ownership, Win32 notification icon details, menu command state, or quit cleanup policy.
- Global hotkey registration, conflict detection, message pump handling, or shortcut recovery behavior.
- Raw `HWND`, `HMONITOR`, COM pointer, WGC frame pool, D3D11 device, DXGI swap-chain, or low-level clipboard/file conversion ownership.
- A parallel status vocabulary for capture, HDR readiness, output, settings, tray, or hotkeys.
- UI-local copies of settings that later need reconciliation with `ISettingsProvider` or persisted settings.

## Owning Module Guidance

| Concern | Preferred Owner | App Shell Role |
|---------|-----------------|----------------|
| Capture command entry | `Lumiere.Capture` via `ICaptureCommandCoordinator` | Call coordinator and project result |
| Capture session state | `Lumiere.Capture` | Read/project state; do not duplicate state machine |
| FP16/scRGB preview resources | `Lumiere.Graphics` | Attach surface and request presentation through graphics abstractions |
| Overlay crop interaction | `Lumiere.Overlay` | Subscribe to typed overlay events |
| Settings defaults/persistence | `Lumiere.Settings` | Open settings UI and bind to provider/store abstractions |
| Output target policy | `Lumiere.Graphics.Output` or Epic 6 decision | Pass confirmed crop/frame context to output abstraction |
| Tray shell integration | `Lumiere.Infrastructure` for Win32 details, app command projection above it | Wire commands to existing capture/settings services |
| Global hotkeys | `Lumiere.Infrastructure` for registration/message handling, capture routing through coordinator | Wire configured shortcuts to command execution |
| Diagnostics/logging primitives | `Lumiere.Infrastructure.Diagnostics` | Emit app-level events with structured context |

## Epic 5 Story Guardrails

### Story 5.1: Native v0 Main Panel

- Preserve capture command routing through `ICaptureCommandCoordinator`.
- Keep unsupported output/tray/hotkey controls hidden, disabled, read-only, or clearly scoped as pending.
- Use native WinUI/Fluent controls; do not import web patterns from the v0 reference.
- Do not add new settings or output behavior while reshaping the panel.
- Keep diagnostic detail out of the default user-facing path unless deliberately exposed by state.

### Story 5.2: Settings Navigation and Shell

- Build a settings shell without inventing persistence rules in `MainWindow`.
- Introduce a settings coordinator or settings view model only if it removes real `MainWindow` responsibility.
- Settings open/close state may live in app UI; settings data ownership belongs in `Lumiere.Settings`.

### Story 5.3: Shortcut and HDR Alert Settings UI

- Shortcut editing UI must not imply global hotkeys work until Epic 7 registration exists.
- HDR alert preferences must use the shared settings path, not UI-local fields. If Story 5.3 precedes the durable write/persistence work, the control must be read-only, pending, or backed by an explicit temporary write seam owned by `Lumiere.Settings`; do not quietly save alert state in `MainWindow`.
- Invalid/conflicting shortcut recovery belongs to settings/hotkey services, not `MainWindow`.

### Story 5.4: Output Preference Settings UI

- Output controls must remain hidden, disabled, read-only, or explicitly scoped until Epic 6 behavior exists.
- UI text must not imply HDR-preserving clipboard or file output without validation evidence.
- Do not implement output policy in the settings UI.

### Story 5.5: Persist Local Settings Across Launches

- Persistence belongs in `Lumiere.Settings`.
- Add a write/persistence counterpart to `ISettingsProvider` deliberately; do not mutate the read-only provider into an unclear grab bag.
- Settings consumers should read one shared source, including future tray, hotkeys, output, and HDR alerts.

### Story 5.6: About and Version

- Version/about data should come from a single authoritative source where practical.
- HDR-first description must keep output fidelity claims honest.

## Code Review Checklist

- Does this change add product logic to `MainWindow.xaml.cs` that belongs to Capture, Graphics, Overlay, Infrastructure, Settings, or Output?
- Does every new app-facing behavior use an existing typed state/result model before adding new vocabulary?
- Are unsupported controls disabled/read-only/scoped instead of pretending to work?
- Are native resources owned by their boundary module?
- Are settings read from a shared provider/store rather than duplicated in UI state?
- Does the change preserve the FP16/scRGB preview path and avoid bitmap/SDR preview fallback?
- Does the story record the validation level honestly?

## Follow-Up

When Story 5.1 is created, include this document in the story context. If Epic 5 discovers a stable new implementation rule, promote it into `harness/` or `_bmad-output/project-context.md` only after it is proven reusable beyond one story.
