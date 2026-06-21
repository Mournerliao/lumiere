---
stepsCompleted:
  - 1
  - 2
  - 3
  - 4
  - 5
  - 6
  - 7
  - 8
inputDocuments:
  - _bmad-output/planning-artifacts/prd.md
  - _bmad-output/planning-artifacts/research/technical-lumiere-mvp-v0-design-winui-wgc-hdr-research-2026-05-09.md
  - _bmad-output/planning-artifacts/validation-report-2026-05-09.md
  - docs/validation/lifecycle-validation.md
  - docs/validation/overlay-validation.md
  - harness/README.md
  - harness/design/index.md
  - harness/design/design-principles.md
  - harness/design/design-workflow.md
  - harness/design/ui-review-checklist.md
  - harness/design/external-references.md
  - harness/design/v0-mvp-reference/README.md
  - harness/planning/project-plan.md
  - harness/planning/mvp-feature-list.md
  - _bmad-output/implementation-artifacts/1-1-scaffold-the-native-windows-app-foundation.md
  - _bmad-output/implementation-artifacts/1-2-centralize-hdr-constants-and-preview-readiness-status.md
  - _bmad-output/implementation-artifacts/1-3-create-d3d11-device-and-winrt-dxgi-interop-bridge.md
  - _bmad-output/implementation-artifacts/1-4-attach-an-fp16-scrgb-swap-chain-to-swapchainpanel.md
  - _bmad-output/implementation-artifacts/1-5-prove-minimal-wgc-fp16-capture-to-live-preview.md
  - _bmad-output/implementation-artifacts/2-1-start-capture-and-select-a-display-or-window-target.md
  - _bmad-output/implementation-artifacts/2-2-represent-capture-session-state-explicitly.md
  - _bmad-output/implementation-artifacts/2-3-stop-restart-and-recreate-capture-resources.md
  - _bmad-output/implementation-artifacts/2-4-validate-repeated-capture-lifecycle-stability.md
  - _bmad-output/implementation-artifacts/2-5-create-monitor-capture-targets-without-picker.md
  - _bmad-output/implementation-artifacts/3-1-show-a-fullscreen-overlay-above-the-hdr-preview.md
  - _bmad-output/implementation-artifacts/3-2-create-a-crop-selection-by-dragging.md
  - _bmad-output/implementation-artifacts/3-3-adjust-or-recreate-the-crop-selection.md
  - _bmad-output/implementation-artifacts/3-4-confirm-or-cancel-the-capture-overlay.md
  - _bmad-output/implementation-artifacts/3-5-manage-overlay-hit-testing-and-keyboard-escape.md
  - _bmad-output/implementation-artifacts/3-6-release-to-capture-and-copy.md
  - _bmad-output/implementation-artifacts/epic-1-retro-2026-05-04.md
  - _bmad-output/implementation-artifacts/epic-2-retro-2026-05-07.md
  - _bmad-output/implementation-artifacts/deferred-work.md
workflowType: 'architecture'
lastStep: 8
status: 'complete'
completedAt: '2026-05-09'
project_name: 'lumiere'
user_name: 'lumiere'
date: '2026-05-09'
planningConstraints:
  - Preserve Epic 1-3 code implementation and validation documents as historical foundation work from the pre-MVP-rebaseline route.
  - When recreating epics for the updated MVP route, keep Epic 1-3 and begin rework or continued implementation from Epic 4.
---

# Architecture Decision Document

_This document builds collaboratively through step-by-step discovery. Sections are appended as we work through each architectural decision together._

## Project Context Analysis

### Requirements Overview

**Functional Requirements:**

Lumiere is a native Windows HDR screenshot utility centered on a low-interruption capture loop: users can start fullscreen or region capture from the main window, global shortcuts, or tray; enter direct monitor capture without a picker-first interruption; select a valid crop over a fullscreen overlay; and output to clipboard, folder, or both according to shared settings.

Architecturally, the requirements divide into six capability groups:

1. Capture entry and session control across main window, tray, and global hotkeys.
2. HDR readiness and trust feedback through typed ready, degraded, unsupported, failed, and completed states.
3. Overlay and region selection with release-to-capture, invalid crop handling, Escape/cancel, and stable preview geometry.
4. Output behavior for clipboard and file targets with honest fidelity semantics.
5. Settings and preferences shared across entry points.
6. Validation and diagnostics evidence for Windows-only capture, graphics, output, and lifecycle behavior.

Existing Epic 1-3 implementation records prove or partially implement the foundation: native project scaffold, FP16/scRGB constants, D3D11 and WinRT/DXGI interop, FP16 swap-chain preview, WGC FP16 live preview, direct monitor capture, explicit capture session state, lifecycle teardown/restart, fullscreen overlay, crop creation/adjustment, confirm/cancel foundation, hit testing, Escape handling, and release-to-capture with basic clipboard output.

These records are historical foundation from the pre-MVP-rebaseline route. Future MVP implementation planning must preserve Epic 1-3 and continue from Epic 4, using later epics to refactor or extend the foundation into the updated MVP product shape.

**Non-Functional Requirements:**

The dominant NFRs are architectural rather than cosmetic:

- Preserve HDR-first invariants: WGC `R16G16B16A16Float`, DXGI `R16G16B16A16_FLOAT`, scRGB color space, GPU-resident preview, and no silent SDR preview fallback.
- Keep Windows-native platform boundaries strict: WinUI 3, Windows App SDK, WGC, D3D11, DXGI, WinRT/COM, Win32 tray/hotkey/monitor/clipboard interop.
- Ensure deterministic disposal and teardown ordering for WGC sessions, frame pools, frames, WinRT Direct3D devices, swap chains, overlays, tray icons, hotkeys, and native resources.
- Use generation-scoped callbacks or equivalent session tokens so stale async work cannot update UI or capture state after restart.
- Keep all operation local and offline, with no cloud upload, account dependency, telemetry endpoint, or remote processing in MVP.
- Separate basic clipboard usability from HDR-preserving output claims until target-aware HDR evidence, encoder behavior, metadata, color conversion, target-app compatibility, and Windows manual validation exist.
- Treat Windows manual validation as required evidence for HDR displays, WGC timing, DXGI/scRGB presentation, tray, hotkeys, multi-monitor behavior, DPI scaling, clipboard/file output, and GPU/resource trends.

**Scale & Complexity:**

- Primary domain: native Windows desktop graphics/capture utility.
- Complexity level: high for a desktop utility, driven by HDR fidelity, GPU/native resource ownership, WinRT/COM interop, multi-monitor behavior, overlay input, and validation depth.
- Estimated architectural components: app shell, capture session controller, graphics/presentation pipeline, overlay/crop subsystem, output subsystem, settings persistence, native interop infrastructure, diagnostics/validation layer, tray integration, global hotkey integration.

### Technical Constraints & Dependencies

The architecture must stay Windows-only and aligned to `.NET 10`, `net10.0-windows10.0.19041.0`, `x64`, WinUI 3, Windows App SDK, WGC, D3D11, DXGI, Vortice, WinRT/COM, and Win32 interop where required.

The v0 MVP reference under `harness/design/v0-mvp-reference/` is a UX reference only. Its React, Tailwind, shadcn, Radix, and web implementation details must not enter production code. The native WinUI/Fluent implementation should translate layout intent, density, wording hierarchy, tray/menu structure, settings structure, and HDR status concepts without inheriting web dependencies.

The default MVP path must avoid picker-first interruption. Direct monitor capture through `IGraphicsCaptureItemInterop::CreateForMonitor` is the architectural default, while picker behavior may remain fallback/debug only.

Clipboard output currently exists as a narrow basic bitmap path. It must not be treated as a validated HDR-preserving output path until Epic 4+ defines output semantics, conversion policy, and validation evidence.

### Cross-Cutting Concerns Identified

- HDR fidelity and claim discipline across target-aware detection, capture, preview, output, UI copy, validation records, and release language.
- Native resource lifecycle and deterministic disposal across capture, graphics, overlay, output, tray, and hotkey boundaries.
- Single capture/session state shared across main window, overlay, tray, hotkeys, settings, and output.
- UI-thread marshalling for WinUI state, `SwapChainPanel` attachment/detachment, overlay updates, and native callbacks.
- Multi-monitor and DPI correctness for direct monitor targeting, overlay placement, crop geometry, and capture-pixel mapping.
- Output semantics and user trust, especially clipboard/file behavior versus HDR preservation claims.
- Settings consistency across main window, tray, shortcuts, output pipeline, and HDR alert behavior.
- Validation-level labeling: Mac edit, Windows CI-pass, and Windows manual-pass must remain distinct.
- Boundary enforcement: UI orchestration must not own WGC, D3D11, DXGI, COM pointers, HMONITOR/HWND handles, or low-level output conversion details directly.

## Starter Template Evaluation

### Primary Technology Domain

Native Windows desktop graphics application.

Lumiere is not a web, mobile, Electron, Tauri, or generic desktop CRUD application. The foundation must remain WinUI 3 + Windows App SDK + WGC + D3D11/DXGI + WinRT/COM interop on `.NET 10`, `net10.0-windows10.0.19041.0`, and x64.

### Starter Options Considered

**Option 1: Existing Brownfield WinUI 3 Solution Scaffold - Selected**

The repository already contains the appropriate native Windows solution foundation:

- `Lumiere.sln`
- `Directory.Build.props`
- `Directory.Packages.props`
- `src/Lumiere.App`
- `src/Lumiere.Capture`
- `src/Lumiere.Graphics`
- `src/Lumiere.Infrastructure`
- `src/Lumiere.Overlay`
- `src/Lumiere.Settings`
- `tests/Lumiere.Graphics.Tests`
- `tests/Lumiere.Overlay.Tests`

This foundation was implemented and validated through Epic 1-3 historical work. It already encodes the core architectural boundaries needed for the rebaselined MVP.

**Option 2: New WinUI Blank App Starter - Rejected for Current Work**

A new WinUI blank app starter would be appropriate for a greenfield project, but Lumiere is now brownfield. Re-running or replacing the scaffold would risk losing the existing FP16/scRGB preview proof, capture lifecycle state, overlay crop behavior, direct monitor capture work, and validation records.

Current web verification also indicates that Visual Studio WinUI templates remain the most reliable starter route, while `dotnet new winui` CLI support is not a stable architectural assumption to base this brownfield project on.

**Option 3: Electron, Tauri, WPF, WinForms, Web UI, or Generic Screenshot Starter - Rejected**

These starters conflict with the project constraints. They would either move the product away from native WinUI 3, weaken the HDR-first graphics path, introduce web UI dependencies, or encourage bitmap-first/SDR-first capture paths.

### Selected Starter: Existing Brownfield Native WinUI 3 Solution Scaffold

**Rationale for Selection:**

The existing scaffold is already the correct starter for the updated MVP route. It preserves the project's highest-risk technical assets:

- FP16 WGC capture path.
- FP16/scRGB DXGI swap-chain preview.
- D3D11 device and WinRT/DXGI interop bridge.
- Explicit capture session state.
- Deterministic teardown and lifecycle validation seams.
- Fullscreen overlay and crop interaction foundation.
- Direct monitor capture without picker-first interruption.
- Separate modules for app shell, capture, graphics, overlay, infrastructure, and settings.

The architecture should therefore document and harden this starter rather than replace it.

**Initialization Command:**

No new initialization command should be run for the current brownfield MVP route.

Historical greenfield initialization would be equivalent to creating a WinUI 3 blank app and then applying the project's custom module split, target framework, x64 runtime, package pins, and HDR/capture boundaries. For this repository, the correct action is:

```bash
# Do not re-run a starter template for the brownfield MVP route.
# Continue from the existing Lumiere.sln scaffold and preserve Epic 1-3 history.
```

**Architectural Decisions Provided by Starter:**

**Language & Runtime:**

- C# / .NET 10.
- `net10.0-windows10.0.19041.0`.
- x64 / `win-x64` only.
- Nullable reference types and implicit usings enabled.
- Central package management enabled.

**UI & Platform Foundation:**

- WinUI 3 and Windows App SDK for app shell, settings, overlay, and windowing.
- No React, Tailwind, shadcn, Radix, Electron, Tauri, WPF, WinForms, or web production stack.
- v0 MVP reference remains UX input only, not implementation source.

**Graphics & Capture Foundation:**

- WGC capture lifecycle isolated in `Lumiere.Capture`.
- D3D11/DXGI rendering and FP16/scRGB presentation isolated in `Lumiere.Graphics`.
- WinRT/COM/Win32 interop isolated in `Lumiere.Infrastructure`.
- Overlay and crop UI behavior isolated in `Lumiere.Overlay`.

**Build Tooling:**

- `Directory.Build.props` defines target framework, x64 platform, runtime identifier, nullable, deterministic build defaults, and central package management.
- `Directory.Packages.props` currently pins Windows App SDK `1.8.260317003`, Vortice `3.8.3`, and test/logging dependencies.
- Current version verification supports keeping these pins unless a concrete blocker requires a separate dependency-management story.

**Testing Framework:**

- xUnit-based tests are already present for graphics/capture and overlay boundaries.
- Hardware-independent tests cover constants, state mapping, lifecycle seams, crop geometry, overlay state, and release-to-capture logic.
- Windows manual validation remains separate from automated test success.

**Code Organization:**

- `Lumiere.App`: startup, composition, high-level orchestration.
- `Lumiere.Capture`: WGC targets, session lifecycle, capture state.
- `Lumiere.Graphics`: D3D11/DXGI resources, HDR constants, swap-chain presentation.
- `Lumiere.Infrastructure`: COM/WinRT/Win32 interop, diagnostics primitives.
- `Lumiere.Overlay`: fullscreen overlay, crop UI, pointer/keyboard interaction.
- `Lumiere.Settings`: local preferences.

**Development Experience:**

- Mac editing is allowed, but Windows validation is required for build, WinUI, WGC, DXGI, D3D11, tray, hotkey, overlay, clipboard/file output, and HDR claims.
- Validation levels must remain distinct: Mac edit, Windows CI-pass, Windows manual-pass.
- Future implementation stories should start from Epic 4 and preserve Epic 1-3 as historical foundation.

## Core Architectural Decisions

### Decision Priority Analysis

**Critical Decisions:**

- Preserve native WinUI 3 / Windows App SDK / .NET 10 / x64 foundation.
- Preserve FP16/scRGB WGC -> D3D11/DXGI preview invariants.
- Use direct monitor capture as the default MVP path; picker remains fallback/debug only.
- Keep one capture/session state contract shared across app, overlay, tray, hotkeys, settings, and output.
- Treat clipboard output as basic bitmap usability, not HDR-preserving output.
- Treat public perfect-HDR-fidelity release as blocked until target-aware HDR detection, output profile contracts, compatibility evidence, and hardware validation gates pass.

**Important Decisions:**

- Keep settings local and versionable; no database or cloud storage in MVP.
- Keep app fully offline with no account, upload, remote processing, or telemetry dependency.
- Route Win32/COM/WinRT details through `Lumiere.Infrastructure`.
- Keep output pipeline behind a narrow abstraction before expanding file/clipboard semantics.
- Use Windows manual validation as a release-readiness gate for platform behavior.

**Deferred Decisions:**

- Installer/signing/update channel.
- Additional HDR-preserving file formats beyond the first validated public-release profile.
- Additional tone mapping and color profile controls beyond the first supported public-release policy.
- Rich diagnostics UI.
- Startup/minimize policy beyond MVP tray behavior.
- Gallery, history, annotation, onboarding, and editor-like workflows.

### Data Architecture

Lumiere does not require a database for MVP.

Architecture decision: use local settings persistence owned by `Lumiere.Settings`, plus local validation records and diagnostics where needed. Output artifacts are files and clipboard payloads, not application-managed database entities.

Settings must cover shortcut preferences, output target, save path, timestamp naming, clipboard image option, HDR alert preference, and version/about metadata. The schema should be explicit and migration-friendly, but lightweight.

### Authentication & Security

No authentication or authorization system is required for MVP.

Security posture is local-first privacy:

- No account login.
- No cloud upload.
- No remote processing.
- No telemetry dependency.
- No screenshot pixel data, frame dumps, or raw screen content in logs.
- Clipboard behavior follows OS clipboard semantics and must not be described as private storage.

Native failure logs may include operation, stage, user-facing state, technical detail, and correlation/session identifiers, but must not include captured content.

### API & Communication Patterns

Lumiere is a single-process desktop app. It should not introduce REST, GraphQL, local HTTP APIs, IPC, or service-style communication for MVP.

Internal communication should use narrow typed interfaces and events:

- Capture results and session state from `Lumiere.Capture`.
- Readiness evidence from `Lumiere.Graphics`.
- Overlay events such as close/cancel and confirmed crop from `Lumiere.Overlay`.
- Interop failures mapped by `Lumiere.Infrastructure`.
- Settings changes through `Lumiere.Settings`.

Error handling should use typed results for expected degraded/unsupported/failure states and exceptions for invariant violations or unrecoverable native failures.

### Frontend Architecture

The UI is native WinUI 3, not web frontend architecture.

`Lumiere.App` owns startup, composition, main window orchestration, and wiring. It should not become the owner of WGC, D3D11, DXGI, COM pointer, or monitor handle semantics.

`Lumiere.Overlay` owns fullscreen overlay UI, crop geometry, pointer routing, keyboard Escape, confirmation payloads, and overlay visual states.

The v0 MVP reference informs layout hierarchy, density, settings organization, tray command shape, and HDR status intent only. React, Tailwind, shadcn, Radix, and web implementation patterns remain out of production code.

### Infrastructure & Deployment

MVP runtime is Windows-only:

- `.NET 10` / `net10.0-windows10.0.19041.0`
- x64 / `win-x64`
- WinUI 3 / Windows App SDK `1.8.260317003`
- Vortice Direct3D11/DXGI `3.8.3`
- CsWinRT `2.2.0` only where concrete interop requires it

CI/validation decisions:

- Automated gates: restore, build, graphics tests, overlay tests, format verification.
- Manual gates: WGC, D3D11/DXGI, HDR display behavior, direct monitor capture, overlay topmost/input behavior, tray, hotkeys, clipboard/file output, multi-monitor, DPI scaling, lifecycle/resource trends.
- Validation language must distinguish Mac edit, Windows CI-pass, and Windows manual-pass.

Packaging, signing, installer, update channel, and distribution remain post-MVP unless needed for early Windows validation.

### Decision Impact Analysis

**Implementation Sequence:**

1. Preserve Epic 1-3 as historical foundation.
2. Start updated MVP implementation from Epic 4.
3. Use Epic 4-9 to harden output usability, settings persistence, tray/hotkeys, and status language around the existing capture/overlay foundation.
4. Use Epic 10+ to complete target-aware HDR detection, output fidelity contracts, compatibility evidence, and public-release validation.
5. Run Windows manual validation before making HDR, direct monitor, output fidelity, or multi-monitor behavior claims.
6. Defer installer, diagnostics UI, gallery, history, and annotation until after the public HDR fidelity release path is coherent.

**Cross-Component Dependencies:**

- Output depends on capture frame/crop payloads, graphics conversion policy, settings target, output profile contracts, and validation language.
- Tray and hotkeys depend on shared session state and settings.
- HDR status depends on target-aware display capability evidence, graphics readiness, capture support, and output semantics.
- Overlay correctness depends on capture target geometry, graphics preview stability, input routing, and lifecycle teardown.
- Settings persistence affects main window, tray, hotkeys, output, and HDR alerts.

## Implementation Patterns & Consistency Rules

### Pattern Categories Defined

**Critical Conflict Points Identified:**

Nine conflict areas require explicit consistency rules: module ownership, naming, file placement, state/result shapes, event payloads, native interop ownership, diagnostics, validation level language, and output semantics.

### Naming Patterns

**Database Naming Conventions:**

No database is used in MVP. Agents must not introduce database table, migration, ORM, or repository naming conventions unless a future architecture update explicitly adds persistent application data beyond local settings and output files.

**API Naming Conventions:**

No REST, GraphQL, local HTTP, or service API is used in MVP. Agents must not add endpoint naming, route naming, JSON API wrappers, or HTTP status conventions for internal app communication.

**Code Naming Conventions:**

- Use C# PascalCase for public types, records, enums, methods, properties, and events.
- Use camelCase for private fields, locals, and parameters, following existing repository style.
- Use responsibility-specific names rather than generic helpers: prefer `CaptureSessionState`, `SwapChainDisposalEvidence`, `DirectMonitorCaptureTargetSelectionService`; avoid `Helper`, `Manager`, or `Utils` unless the surrounding code already establishes the pattern.
- Status enums and result types should describe product/capability states, not UI text: `Unsupported`, `Degraded`, `Failed`, `Disposed`, `CaptureConfirmed`.
- User-facing text belongs near UI/status projection code, not inside low-level graphics, capture, or interop types.

### Structure Patterns

**Project Organization:**

- `Lumiere.App` owns startup, composition, main-window orchestration, and wiring between modules.
- `Lumiere.Capture` owns WGC target selection semantics, capture target models, session lifecycle, capture state, and capture lifecycle evidence.
- `Lumiere.Graphics` owns D3D11/DXGI resources, HDR constants, swap-chain creation, presentation, frame output, graphics conversion policies, and graphics lifecycle evidence.
- `Lumiere.Infrastructure` owns WinRT/COM/Win32 interop, native handle wrappers, HRESULT/COM failure mapping, diagnostics primitives, and cross-cutting OS boundary helpers.
- `Lumiere.Overlay` owns fullscreen overlay UI, crop geometry, pointer routing, keyboard Escape, overlay state, and confirmed crop payloads.
- `Lumiere.Settings` owns local preference persistence, defaults, validation, and future migration semantics.

**File Structure Patterns:**

- Place files by boundary ownership first, not by convenience from the calling UI.
- Put hardware-independent tests under existing boundary test projects:
  - Capture/graphics lifecycle tests in `tests/Lumiere.Graphics.Tests` while that is the established pattern.
  - Overlay/crop tests in `tests/Lumiere.Overlay.Tests`.
- Validation checklists live under `docs/validation/`.
- BMad-generated story and planning output remains in `_bmad-output/`.
- Durable guidance belongs in `harness/`.

### Format Patterns

**API Response Formats:**

Not applicable for MVP because there is no internal HTTP/API surface.

For internal operations, use typed result objects instead of unstructured tuples, nullable booleans, magic strings, or exception-only flow. Examples:

- `CaptureTargetSelectionResult`
- `CaptureStartResult`
- `CaptureSessionState`
- `PreviewReadinessStatus`
- `ConfirmedCaptureSelection`
- `CaptureSessionDisposalEvidence`
- `SwapChainDisposalEvidence`

**Data Exchange Formats:**

- Internal state payloads should be immutable records or small value types where practical.
- Diagnostics should include operation, stage, user-facing state, technical detail, and optional session/correlation identity.
- Diagnostics must not include screenshot pixels, raw frame dumps, or captured screen content.
- Date/time values for filenames, validation records, or logs should use explicit invariant formatting; do not infer culture-sensitive formats for persisted names.

### Communication Patterns

**Event System Patterns:**

- UI/module events should be typed and purpose-specific.
- Do not merge confirm and cancel into a single untyped close event.
- Overlay confirmation uses a typed crop payload.
- Overlay cancellation/close remains separate from capture confirmation.
- Capture and graphics callbacks must be generation-scoped or session-token-scoped before they update app-visible state.

**State Management Patterns:**

- `CaptureSessionState` remains the shared capture lifecycle contract.
- Do not create a parallel status vocabulary in App, Overlay, Settings, Tray, Hotkeys, or Output.
- `PreviewReadinessStatus` remains the readiness/trust evidence vocabulary for graphics/presentation/capture capability.
- UI may project state into labels, but must not be the owner of state transition rules.
- Stale callbacks must not mutate UI or active session state after a newer generation starts.

### Process Patterns

**Error Handling Patterns:**

- Expected platform or capability outcomes should be typed states: unsupported, degraded, failed, canceled, disposed.
- Native interop failures should be mapped to structured diagnostics at the interop boundary.
- Exceptions are appropriate for invariant violations, programming errors, and unrecoverable native failures.
- User-facing errors must be concise and not overclaim success.
- Degraded and unsupported states must never use `HDR-ready`, `completed`, or success language.

**Loading State Patterns:**

- Use explicit states such as `SelectingTarget`, `Initializing`, `Capturing`, `Degraded`, `Unsupported`, `Failed`, and `Disposed`.
- Do not report `Capturing` or `HDR-ready` merely because a WGC session object exists.
- During teardown, restart, frame-size recreation, output write, or failure recovery, avoid stale success messages.
- Overlay status changes must not resize or shift the preview surface or invalidate crop coordinate mapping.

### Enforcement Guidelines

**All AI Agents MUST:**

- Preserve FP16/scRGB constants and never introduce SDR/bitmap preview fallback into the main preview path.
- Keep platform/native APIs inside their boundary module.
- Reuse existing typed state/result models before adding new ones.
- Add hardware-independent tests for new pure logic and update Windows manual validation docs for real WGC/DXGI/WinUI behavior.
- Label validation accurately as Mac edit, Windows CI-pass, or Windows manual-pass.
- Preserve Epic 1-3 as historical foundation and start rebaselined MVP planning from Epic 4.

**Pattern Enforcement:**

- Code review should flag boundary leaks, duplicated status vocabularies, untyped events, native handle exposure, and unsupported HDR claims.
- Story specs should list touched modules and explicitly state which module owns each new type.
- New output or interop code must include a resource ownership note.
- Pattern updates should be made in this architecture document or durable harness docs, not only in transient story notes.

### Pattern Examples

**Good Examples:**

- `Lumiere.Infrastructure.Interop.MonitorHandle` wraps native monitor handles instead of exposing raw `IntPtr` across modules.
- `Lumiere.Capture.CaptureSessionState` carries lifecycle status instead of `MainWindow` inventing ad hoc strings.
- `Lumiere.Overlay.Crop.CropCoordinateMapper` maps DIP crop regions to capture pixels without touching WGC frames or D3D textures.
- `SwapChainDisposalEvidence` records detach-before-release behavior without putting graphics ownership in UI code.

**Anti-Patterns:**

- Adding `BitmapImage`, `SoftwareBitmap`, GDI, WIC, or CPU bitmap readback as the primary live preview path.
- Letting `MainWindow.xaml.cs` directly own COM pointers, HMONITOR/HWND handles, WGC frame pools, D3D11 devices, or DXGI swap chains.
- Introducing a second capture status enum in Tray, Settings, or Output.
- Treating clipboard bitmap output as HDR-preserving without explicit conversion policy and Windows manual validation.
- Adding a web UI dependency because the v0 reference is React-based.
- Marking direct monitor capture, HDR fidelity, tray, hotkey, or multi-monitor behavior as complete from Mac edits or unit tests alone.

## Project Structure & Boundaries

### Complete Project Directory Structure

```text
lumiere/
├── Lumiere.sln
├── Directory.Build.props
├── Directory.Packages.props
├── README.md
├── AGENTS.md
├── docs/
│   └── validation/
│       ├── lifecycle-validation.md
│       └── overlay-validation.md
├── harness/
│   ├── README.md
│   ├── design/
│   │   ├── index.md
│   │   ├── design-principles.md
│   │   ├── design-workflow.md
│   │   ├── ui-review-checklist.md
│   │   ├── external-references.md
│   │   └── v0-mvp-reference/
│   ├── planning/
│   │   ├── project-plan.md
│   │   └── mvp-feature-list.md
│   └── workflows/
├── src/
│   ├── Lumiere.App/
│   │   ├── App.xaml
│   │   ├── App.xaml.cs
│   │   ├── MainWindow.xaml
│   │   ├── MainWindow.xaml.cs
│   │   └── CaptureActionCard.xaml/.cs
│   ├── Lumiere.Capture/
│   │   ├── CaptureService.cs
│   │   ├── CaptureTarget*.cs
│   │   ├── CaptureSession*.cs
│   │   ├── CaptureLifecycle*.cs
│   │   └── DirectMonitorCaptureTargetSelectionService.cs
│   ├── Lumiere.Graphics/
│   │   ├── Devices/
│   │   ├── Hdr/
│   │   ├── Presentation/
│   │   └── Clipboard/
│   ├── Lumiere.Infrastructure/
│   │   ├── Diagnostics/
│   │   └── Interop/
│   ├── Lumiere.Overlay/
│   │   ├── Crop/
│   │   ├── Input/
│   │   ├── Windowing/
│   │   ├── OverlayWindow.xaml
│   │   └── OverlayWindow.xaml.cs
│   └── Lumiere.Settings/
│       └── SettingsBoundary.cs
├── tests/
│   ├── Lumiere.Graphics.Tests/
│   │   ├── Capture/
│   │   ├── Devices/
│   │   ├── Hdr/
│   │   └── Presentation/
│   └── Lumiere.Overlay.Tests/
│       ├── Crop*.cs
│       ├── Overlay*.cs
│       └── ReleaseToCaptureTests.cs
└── _bmad-output/
    ├── planning-artifacts/
    └── implementation-artifacts/
```

### Architectural Boundaries

**API Boundaries:**

There are no HTTP/API boundaries in MVP. All communication is in-process through typed services, events, records, and result objects.

**Component Boundaries:**

- `Lumiere.App` composes modules and owns app/window orchestration only.
- `Lumiere.Capture` owns capture target selection semantics, WGC session lifecycle, session state, and lifecycle evidence.
- `Lumiere.Graphics` owns graphics devices, HDR constants, swap-chain presentation, frame presentation, and graphics/output conversion policies.
- `Lumiere.Infrastructure` owns WinRT/COM/Win32 interop, native handles, diagnostics primitives, and structured logging.
- `Lumiere.Overlay` owns overlay UI, crop geometry, pointer/keyboard input, overlay state, and confirmed crop payloads.
- `Lumiere.Settings` owns local settings persistence, validation, defaults, and migrations.

**Service Boundaries:**

Future Epic 4+ services should be added to the owning module, not to `MainWindow.xaml.cs`.

- Output target selection and output orchestration: narrow app-facing abstraction; concrete clipboard/file work belongs in graphics/infrastructure/settings as appropriate.
- Tray integration: shell/windowing integration through infrastructure, command projection through app orchestration.
- Global hotkeys: Win32 registration and message handling through infrastructure, command routing through app orchestration.
- Settings persistence: `Lumiere.Settings`, consumed by app/tray/hotkeys/output.

**Data Boundaries:**

- No database.
- Settings are local persisted data.
- Output files are user artifacts, not app-managed records.
- Clipboard payloads follow OS clipboard semantics.
- Validation docs are evidence records, not runtime product state.

### Requirements to Structure Mapping

**Feature/Epic Mapping:**

- Epic 1 historical foundation: `Lumiere.Graphics`, `Lumiere.Infrastructure`, `Lumiere.Capture`, `Lumiere.App`.
- Epic 2 historical lifecycle/direct capture: `Lumiere.Capture`, `Lumiere.Infrastructure.Interop`, `Lumiere.App`, lifecycle validation docs.
- Epic 3 historical overlay/crop/release-to-capture: `Lumiere.Overlay`, `Lumiere.Graphics.Clipboard`, `Lumiere.App`, overlay validation docs.
- Epic 4-9 MVP foundation: extend settings/output/tray/hotkey/status behavior without claiming HDR preservation prematurely.
- Epic 10+ public fidelity work: connect capture targets to display output identity, make HDR probing target-aware, define output profile contracts, validate compatibility, and record public-release evidence.
- Epic 4+ settings: implement under `Lumiere.Settings`; app, tray, hotkeys, output consume settings through interfaces.
- Epic 4+ tray/hotkeys: native Win32 details in `Lumiere.Infrastructure`; command/session routing in `Lumiere.App`.

**Cross-Cutting Concerns:**

- HDR constants: `src/Lumiere.Graphics/Hdr/`
- Capture/session state: `src/Lumiere.Capture/`
- Native interop: `src/Lumiere.Infrastructure/Interop/`
- Diagnostics/logging: `src/Lumiere.Infrastructure/Diagnostics/`
- Overlay/crop geometry: `src/Lumiere.Overlay/Crop/`
- Manual validation: `docs/validation/`

### Integration Points

**Internal Communication:**

- App starts capture through capture services.
- Capture produces typed targets/session state.
- Graphics presents frames and readiness evidence.
- Overlay emits typed close/cancel and confirmed crop events.
- Output consumes confirmed crop, active frame/texture access policy, settings, and validation-aware conversion semantics.
- Settings provide shared configuration to app, tray, hotkeys, and output.

**External Integrations:**

- Windows Graphics Capture.
- D3D11/DXGI.
- WinUI 3 / Windows App SDK windowing.
- WinRT/COM interop.
- Win32 monitor, tray, hotkey, z-order, and capture-exclusion APIs.
- Windows clipboard and file/folder picker APIs.

**Data Flow:**

Trigger -> shared session guard -> direct monitor target -> WGC FP16 frame pool -> D3D11 texture -> FP16/scRGB swap-chain preview -> overlay crop -> confirmed crop payload -> output policy -> clipboard/file result -> user-facing completion or recoverable failure.

### File Organization Patterns

**Configuration Files:**

- Build settings remain in `Directory.Build.props`.
- Package versions remain in `Directory.Packages.props`.
- Runtime/user preferences belong in `Lumiere.Settings`, not MSBuild props.
- BMad configuration stays under `_bmad/` and generated artifacts under `_bmad-output/`.

**Source Organization:**

Source files are organized by ownership boundary. New code must first answer: app orchestration, capture, graphics, infrastructure, overlay, or settings?

**Test Organization:**

Pure capture/graphics logic remains in `tests/Lumiere.Graphics.Tests` while established. Pure overlay/crop/input logic remains in `tests/Lumiere.Overlay.Tests`. Real WGC/DXGI/WinUI/HDR behavior requires Windows manual validation docs.

**Asset Organization:**

MVP production code should not depend on web prototype assets. `harness/design/v0-mvp-reference/` remains design reference only.

### Development Workflow Integration

**Development Server Structure:**

No dev server is required for production Lumiere. The v0 prototype may run separately as design reference only.

**Build Process Structure:**

The solution is built through `Lumiere.sln`, x64 only, with central package management and project references preserving module boundaries.

**Deployment Structure:**

Packaging/signing/installer/update structure is deferred. Do not introduce distribution-specific layout until a dedicated post-MVP or validation-distribution story defines it.

## Architecture Validation Results

### Coherence Validation ✅

**Decision Compatibility:**

The architectural decisions are compatible. The selected brownfield WinUI 3 foundation aligns with `.NET 10`, Windows App SDK, WGC, D3D11/DXGI, Vortice, WinRT/COM, x64-only targeting, and the existing Epic 1-3 implementation records.

No decision conflicts with the HDR-first invariant. Direct monitor capture, FP16/scRGB preview, typed capture state, overlay crop behavior, and local-only output/settings scope all reinforce the same MVP direction.

**Pattern Consistency:**

The implementation patterns support the decisions. Naming, state/result models, module ownership, diagnostics, validation language, and output-claim rules all point agents toward the same implementation style.

The patterns explicitly block the highest-risk divergences: duplicated lifecycle vocabularies, native handle leakage, bitmap-first preview fallback, web UI contamination from the v0 prototype, and unsupported HDR-preservation claims.

**Structure Alignment:**

The project structure supports the architecture. Existing modules map cleanly to the intended boundaries, and Epic 4+ work has clear target locations for settings, output semantics, tray, hotkeys, diagnostics, and validation.

### Requirements Coverage Validation ✅

**Epic/Feature Coverage:**

Epic 1-3 historical implementation is covered as foundation work and preserved as requested. Updated MVP planning begins from Epic 4, with later work expected to harden settings, output, tray, hotkeys, status language, and validation around the existing capture/overlay foundation.

**Functional Requirements Coverage:**

The functional requirement categories are architecturally supported:

- Capture entry and session control: `Lumiere.App`, `Lumiere.Capture`, future tray/hotkey infrastructure.
- HDR readiness and trust feedback: `Lumiere.Graphics`, `Lumiere.Capture`, shared typed state.
- Overlay and region selection: `Lumiere.Overlay`, existing crop/confirm/release-to-capture foundation.
- Output behavior: existing basic clipboard path plus future output abstraction and settings integration.
- Settings/preferences: `Lumiere.Settings` boundary reserved for local persistence.
- Validation/diagnostics: `docs/validation/`, infrastructure diagnostics, lifecycle evidence models.

**Non-Functional Requirements Coverage:**

The NFRs are architecturally addressed:

- HDR fidelity: FP16/scRGB invariants and no SDR preview fallback are explicit.
- Reliability/lifecycle: deterministic disposal, detach-before-release, generation-scoped callbacks.
- Privacy/local operation: no account, no cloud, no telemetry dependency, no captured pixels in logs.
- Windows compatibility: native Windows-only stack and manual validation gates.
- Maintainability: strict module boundaries and typed result/state patterns.
- Accessibility/usability: Escape/cancel, status language, native WinUI/Fluent direction, non-color-only state requirements.

### Implementation Readiness Validation ✅

**Decision Completeness:**

Critical decisions are documented with current version baselines where applicable. Deferred decisions are explicitly marked and do not block MVP architecture execution.

**Structure Completeness:**

The project structure is concrete and tied to existing files/modules. Future Epic 4+ locations are defined by boundary rather than forcing speculative files too early.

**Pattern Completeness:**

The main AI-agent conflict points are addressed: naming, ownership, state, events, interop, diagnostics, validation, output semantics, and file placement.

### Gap Analysis Results

**Critical Gaps:**

None.

**Important Gaps:**

- Target-aware HDR detection remains required before public HDR readiness claims can be made on mixed-monitor systems.
- Output profile contracts remain required before HDR-preserving clipboard/file claims can be made.
- Windows manual validation remains required for real WGC/DXGI/D3D11/HDR, direct monitor capture, multi-monitor, DPI, overlay input, tray, hotkeys, output compatibility, and resource trend claims.

**Nice-to-Have Gaps:**

- Production diagnostics could consume teardown evidence more visibly.
- COM pointer ownership rules could be promoted into a durable interop guideline.
- Future packaging/signing/update architecture should be added when distribution becomes in scope.

### Validation Issues Addressed

The main issue found during validation was scope ambiguity around Epic 1-3. This is now resolved in frontmatter, project context, starter evaluation, decisions, patterns, and structure: Epic 1-3 remain historical foundation, and updated MVP planning starts from Epic 4.

The second issue was risk of overclaiming clipboard/HDR output. The architecture now consistently states that current clipboard output is basic bitmap usability only, not HDR-preserving output.

### Architecture Completeness Checklist

**Requirements Analysis**

- [x] Project context thoroughly analyzed
- [x] Scale and complexity assessed
- [x] Technical constraints identified
- [x] Cross-cutting concerns mapped

**Architectural Decisions**

- [x] Critical decisions documented with versions
- [x] Technology stack fully specified
- [x] Integration patterns defined
- [x] Performance considerations addressed

**Implementation Patterns**

- [x] Naming conventions established
- [x] Structure patterns defined
- [x] Communication patterns specified
- [x] Process patterns documented

**Project Structure**

- [x] Complete directory structure defined
- [x] Component boundaries established
- [x] Integration points mapped
- [x] Requirements to structure mapping complete

### Architecture Readiness Assessment

**Overall Status:** READY FOR MVP FOUNDATION IMPLEMENTATION; PUBLIC HDR FIDELITY RELEASE REQUIRES EPIC 10+ WORK

**Confidence Level:** High

**Key Strengths:**

- Clear HDR-first technical invariants.
- Existing Epic 1-3 foundation is preserved rather than overwritten.
- Strong module boundaries for capture, graphics, infrastructure, overlay, app, and settings.
- Typed lifecycle/readiness patterns reduce agent drift.
- Manual validation requirements are explicit and not confused with automated tests.

**Areas for Future Enhancement:**

- Complete target-aware HDR detection for active capture targets.
- Define full output profile contracts for supported clipboard and file paths.
- Validate target-app compatibility and public release fidelity evidence.
- Promote COM/display identity ownership rules into durable interop guidance.
- Add packaging/signing/update architecture when distribution enters scope.

### Implementation Handoff

**AI Agent Guidelines:**

- Follow all architectural decisions exactly as documented.
- Use implementation patterns consistently across all components.
- Respect project structure and boundaries.
- Refer to this document for architectural questions.
- Do not re-plan Epic 1-3 as new MVP epics; preserve them and continue from Epic 4.

**First Implementation Priority:**

For the current public release target, start with Epic 10 target-aware HDR detection. Do not implement or enable advanced HDR output profiles until the HDR fidelity contract and output profile contract are approved. Epic 4-9 remain the MVP foundation and should not be rolled back.

Before UI-heavy implementation stories begin, use the MVP UX specification/state inventory as the source of truth for main panel, settings, tray, overlay, HDR status, completion/failure feedback, disabled controls, and non-color-only state discrimination.
