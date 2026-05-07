---
stepsCompleted: [1, 2, 3, 4, 5, 6, 7, 8]
inputDocuments:
  - '/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/prd.md'
  - '/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/research/technical-lumiere-hdr-capture-research-2026-04-20.md'
  - '/Users/asherliao/Projects/lumiere/_bmad-output/project-context.md'
workflowType: 'architecture'
project_name: 'lumiere'
user_name: 'Asherliao'
date: '2026-04-20'
lastStep: 8
status: 'complete'
completedAt: '2026-04-20'
---

# Architecture Decision Document - Lumiere

**Author:** Asherliao
**Date:** 2026-04-20
**Status:** Complete

## Approved MVP-to-1.0 Rebaseline (2026-05-07)

The active implementation plan is now the six-epic MVP-to-1.0 route documented in `_bmad-output/planning-artifacts/epics.md`.

Architecturally, this means:

- The default MVP capture path is direct monitor capture and must not require `GraphicsCapturePicker` before the user can draw a region.
- `GraphicsCapturePicker` remains a fallback/debug or later explicit target-selection path, not the default screenshot path.
- MVP output includes only a narrow clipboard result with explicit semantics. It must not weaken or redefine the FP16/scRGB live preview path.
- Settings, tray, global hotkey, annotation, advanced export, and capture history are post-1.0 roadmap items unless separately promoted.
- MVP completion is not claimed when feature stories alone are done; it is claimed only after the MVP completion gate epic validates Windows manual scenarios and deferred-work triage.
- Installer and 1.0 release work is isolated in the installer/release epic and must preserve WinUI 3, Windows App SDK, `.NET 10`, `net10.0-windows10.0.19041.0`, x64, WGC, D3D11, DXGI, and Vortice constraints.

## Executive Architecture Summary

Lumiere is a native Windows desktop application whose architecture is shaped by one non-negotiable product promise: HDR capture preview must preserve FP16/scRGB fidelity instead of silently falling back to SDR bitmap paths. The system is therefore a modular Windows graphics application, not a conventional screenshot utility with a UI wrapper.

The architecture is a modular monolith built around three strict ownership boundaries:

- `CaptureService` owns Windows.Graphics.Capture target selection, frame pool/session lifecycle, frame arrival, and prompt frame disposal.
- `GraphicsEngine` owns Direct3D 11 device/context, DXGI swap chain, render targets, shaders, HDR constants, resize handling, presentation, and WinUI swap-chain interop.
- `OverlayUI` owns the WinUI 3 fullscreen overlay, `SwapChainPanel`, crop canvas, toolbar, keyboard/mouse interaction, and user-facing state.

All native interop, COM/DXGI/WinRT bridge code, and Win32 window style manipulation must stay behind narrow infrastructure APIs. No UI component may create capture sessions or Direct3D devices directly.

## Project Context Analysis

### Requirements Overview

**Functional Requirements:**

The PRD defines 42 functional requirements across capture target selection, HDR preview fidelity, crop interaction, overlay behavior, diagnostics, resource lifecycle, MVP validation, settings, and post-MVP export/workflow capabilities. Architecturally, these requirements map to four MVP capability groups:

- Capture orchestration: initiate capture, choose display/window target, cancel selection, detect unsupported/degraded states, and restart sessions safely.
- HDR preview pipeline: preserve FP16/scRGB data from WGC frames through D3D11/DXGI presentation, validate HDR readiness, and surface degraded states visibly.
- Overlay crop workflow: render fullscreen preview, layer XAML crop controls above the swap chain, support drag/adjust/confirm/cancel, and dismiss reliably.
- Lifecycle and diagnostics: dispose frame pools, sessions, textures, swap chains, and device resources deterministically; expose diagnostic state for capture, graphics, presentation, and HDR capability.

Post-MVP requirements for export, clipboard, hotkey, tray, annotation, and capture history are intentionally deferred so the MVP does not dilute the HDR proof path.

**Non-Functional Requirements:**

The 28 NFRs are architecture-driving. The most important are:

- HDR fidelity: primary preview must use FP16/scRGB and must not silently downgrade to SDR.
- Performance: live preview must avoid CPU readback/bitmap conversion, and crop interaction must remain responsive.
- Reliability: repeated capture start/stop/cancel/confirm flows must not leak WGC, WinRT, COM, D3D11, DXGI, frame, texture, render-target, or swap-chain resources.
- Threading: WinUI and `SwapChainPanel` operations must run on the UI thread; capture callbacks must not mutate UI state directly.
- Platform: target `.NET 10 LTS`, `net10.0-windows10.0.19041.0`, Windows App SDK 1.8 stable, WinUI 3, WGC, Direct3D 11, DXGI, Vortice, and explicit `x64` first.
- Privacy/security: no network dependency, no screenshot upload, and no capture permission bypass.

**Scale & Complexity:**

- Primary domain: native Windows desktop graphics / HDR capture.
- Complexity level: high.
- Estimated architectural components: 9 core components: app shell, overlay window, crop interaction, capture service, graphics engine, interop bridge, diagnostics, settings, tests/validation harness.

### Technical Constraints & Dependencies

- WGC frame pool pixel format must be `DirectXPixelFormat.R16G16B16A16Float`.
- DXGI swap chain format must be `DXGI_FORMAT_R16G16B16A16_FLOAT`.
- DXGI color space must be `DXGI_COLOR_SPACE_RGB_FULL_G10_NONE_P709`.
- Preview must use `SwapChainPanel` with `ISwapChainPanelNative.SetSwapChain`.
- `SetSwapChain` and WinUI state mutation must be marshaled to the UI thread.
- `SetSwapChain(null)` must occur before graphics teardown.
- WGC frames must be disposed promptly and not retained after checkout lifetime.
- Direct HWND/HMONITOR capture item creation requires Windows 10 1903/build 18362 or later; the target TFM is still `net10.0-windows10.0.19041.0`.
- The MVP must be offline and local-only.

### Cross-Cutting Concerns Identified

- HDR constants and validation.
- Native resource ownership and deterministic disposal.
- UI-thread/capture-thread boundaries.
- Degraded/unsupported state reporting.
- Multi-monitor HDR/SDR capability variance.
- Resize and target change handling.
- Diagnostics and testability.
- Packaging/runtime dependency strategy.

## Starter Template Evaluation

### Primary Technology Domain

The primary technology domain is a native Windows desktop application using C#/.NET, WinUI 3, Windows App SDK, Windows.Graphics.Capture, Direct3D 11, DXGI, and Vortice.

This is not a good fit for Electron, Tauri, WPF bitmap-first templates, web UI starters, cross-platform screenshot libraries, or generic desktop boilerplates because the core value is GPU-resident HDR preview fidelity.

### Current Version Verification

Versions were checked on 2026-04-20:

- .NET support policy lists `.NET 10` as LTS, active, latest patch `10.0.6`, with support through 2028-11-14.
- Windows App SDK stable channel lists `1.8.6`, package/runtime version `1.8.260317003`, released 2026-03-18.
- NuGet lists `Vortice.Direct3D11` `3.8.3`, last updated 2026-03-04, compatible with `net10.0`.
- NuGet lists `Microsoft.Windows.CsWinRT` stable `2.2.0`; a newer `3.0.0-preview.260319.2` exists, but it is prerelease and should not be the default MVP choice.

Sources:

- [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy)
- [Windows App SDK downloads](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads)
- [Microsoft.WindowsAppSDK NuGet](https://www.nuget.org/packages/Microsoft.WindowsAppSdk/)
- [Vortice.Direct3D11 NuGet](https://www.nuget.org/packages/Vortice.Direct3D11/)
- [Microsoft.Windows.CsWinRT NuGet](https://www.nuget.org/packages/Microsoft.Windows.CsWinRT/)

### Starter Options Considered

**WinUI 3 Blank App, Packaged or Unpackaged**

This is the recommended foundation. It keeps the app in the supported Windows App SDK path while leaving enough room to create custom D3D11/DXGI infrastructure. It does not impose web-style routing, browser rendering, or UI patterns that would conflict with `SwapChainPanel` and native capture.

**WinUI 3 Gallery/sample-derived structure**

Useful as reference material, but not as the project starter. Samples tend to optimize for demonstrating APIs, not for strict resource lifetime ownership and HDR pipeline validation.

**WPF/WinForms screenshot tool starter**

Rejected for MVP. These approaches invite bitmap/GDI or SDR-oriented preview paths and would fight the core HDR requirement.

**Electron/Tauri desktop starter**

Rejected for MVP. Browser/webview surfaces are not the right primary presentation layer for an FP16/scRGB DirectX preview pipeline.

### Selected Starter: WinUI 3 Blank App with Custom Graphics/Capture Infrastructure

**Rationale for Selection:**

Use the official WinUI 3/Windows App SDK application foundation, then immediately add dedicated `Capture`, `Graphics`, `Overlay`, `Interop`, and `Diagnostics` modules. This gives the project stable Windows desktop scaffolding without hiding the hard parts behind unsuitable abstractions.

**Initialization Command:**

Use Visual Studio 2022 WinUI 3 template or equivalent `dotnet new` template once Windows App SDK templates are installed. The first implementation story must create or verify:

```bash
dotnet new winui --name Lumiere --framework net10.0-windows10.0.19041.0
```

If the installed template does not support `--framework` directly, create the WinUI 3 blank app and then edit the project file to set:

```xml
<TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
<Platforms>x64</Platforms>
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
```

**Package References:**

```xml
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.260317003" />
<PackageReference Include="Vortice.Direct3D11" Version="3.8.3" />
<PackageReference Include="Vortice.DXGI" Version="3.8.3" />
<PackageReference Include="Microsoft.Windows.CsWinRT" Version="2.2.0" />
```

Use `Microsoft.Windows.CsWinRT` only if the concrete interop implementation needs it. Do not move to `3.0.0-preview.260319.2` unless a documented blocker requires it.

**Architectural Decisions Provided by Starter:**

**Language & Runtime:** C# on `.NET 10 LTS`, Windows-specific TFM, `x64` first.

**Styling Solution:** WinUI 3/XAML resources for overlay controls only. The HDR preview is DirectX-backed, not XAML image-backed.

**Build Tooling:** SDK-style `.csproj`, Windows App SDK package reference, Visual Studio 2022 tooling.

**Testing Framework:** Not provided by starter. Add .NET test projects deliberately after scaffolding.

**Code Organization:** Starter provides app entry/window shell; Lumiere adds explicit modules for capture, graphics, overlay, interop, diagnostics, configuration, and tests.

**Development Experience:** Use Visual Studio debugging for WinUI/DirectX interop, with local manual HDR validation on real HDR hardware.

## Core Architectural Decisions

### Decision Priority Analysis

**Critical Decisions (Block Implementation):**

- Native Windows desktop implementation with WinUI 3 and Windows App SDK 1.8 stable.
- `.NET 10 LTS`, `net10.0-windows10.0.19041.0`, `x64` first.
- WGC + D3D11 + DXGI + Vortice for capture/rendering.
- FP16/scRGB preview path with no SDR/GDI/bitmap fallback in MVP live preview.
- Strict module ownership: `CaptureService`, `GraphicsEngine`, `OverlayUI`, `Interop`, `Diagnostics`.
- Deterministic disposal and UI-thread marshaling as first-class architecture rules.

**Important Decisions (Shape Architecture):**

- Modular monolith rather than plugin/service decomposition.
- `SwapChainPanel` as the only main live preview surface.
- Centralized HDR constants and validation.
- Degraded/unsupported state model instead of exceptions-only failure behavior.
- Manual HDR hardware validation remains part of MVP quality gates.

**Deferred Decisions (Post-MVP):**

- HDR still export format.
- SDR tone-mapping presets.
- Clipboard format semantics.
- Global hotkey/tray architecture.
- Annotation rendering model.
- Capture history/storage.
- Installer/update channel.

### Data Architecture

No database is required for MVP.

**Decision:** Use in-memory session state plus simple local settings when preferences are introduced.

**Rationale:** The MVP is a live capture/preview/crop workflow. Persistent data would add architecture weight without supporting the Phase 0/Phase 1 proof.

**Data Owners:**

- `CaptureSessionState`: current target, capture status, frame size, diagnostics.
- `OverlayState`: crop rectangle, drag handles, confirm/cancel state.
- `GraphicsState`: device readiness, swap-chain status, color-space status, resize state.
- `AppSettings`: future local preferences such as diagnostics visibility or cursor capture.

### Authentication & Security

**Decision:** No application authentication in MVP. Use Windows.Graphics.Capture consent and capability mechanisms.

**Security Rules:**

- Do not bypass OS capture permission or border behavior.
- Do not upload screenshots, telemetry, display content, or diagnostics to any remote service.
- Do not capture in background without explicit user action and OS-supported permission.
- If borderless capture is added later, request the appropriate Windows capability and handle denial visibly.

### API & Communication Patterns

No network API is required for MVP.

**Internal Communication Pattern:**

- UI commands call application services through typed methods.
- `CaptureService` emits typed frame/capture events.
- `GraphicsEngine` accepts valid frame texture handles and render commands.
- `DiagnosticsService` aggregates status from capture, graphics, presentation, and HDR validation.
- UI observes state snapshots; background capture callbacks never mutate XAML directly.

**Error Handling Standard:**

Use typed result/status objects for expected degraded/unsupported states and exceptions for programmer errors or unrecoverable native failures.

Example status categories:

- `Ready`
- `Initializing`
- `Capturing`
- `Degraded`
- `Unsupported`
- `Failed`
- `Disposed`

### Frontend Architecture

**Decision:** WinUI 3 overlay UI with `SwapChainPanel` plus XAML overlay canvas.

**Rules:**

- `SwapChainPanel` is the hardware preview layer.
- XAML `Canvas` owns crop rectangle, mask, handles, toolbar, and status text.
- UI must not know D3D11/DXGI implementation details.
- Crop state is device-independent-pixel based in UI, then converted through a single coordinate mapping service for capture/render/export use.
- All WinUI operations must use `DispatcherQueue` when originating from capture or render callbacks.

### Infrastructure & Deployment

**Decision:** MVP targets local Windows development first; packaging decision is deferred until after Phase 0 spike succeeds.

**Default Runtime/Package Direction:**

- Windows App SDK `1.8.260317003`.
- `x64` first.
- Packaged deployment is preferred for later distribution if capture capabilities and runtime installation flow align.
- Unpackaged development is acceptable for early spike if it reduces setup friction, but runtime dependency handling must be documented.

### Decision Impact Analysis

**Implementation Sequence:**

1. Scaffold WinUI 3 app with fixed TFM/package versions.
2. Add shared constants, diagnostics, and disposal infrastructure.
3. Implement D3D11 device provider and WinRT/DXGI interop bridge.
4. Implement minimal `GraphicsEngine` swap-chain attach/detach and color-space validation.
5. Implement `CaptureService` with FP16 WGC frame pool and frame disposal.
6. Prove Phase 0 live HDR preview on hardware.
7. Add overlay crop interaction and state mapping.
8. Add teardown/restart validation and manual HDR test matrix.

**Cross-Component Dependencies:**

- `OverlayUI` depends on `GraphicsEngine` only through preview host/attach abstractions.
- `CaptureService` depends on D3D device interop but must not depend on overlay controls.
- `GraphicsEngine` depends on frame texture inputs but must not own WGC sessions.
- `DiagnosticsService` reads component state without controlling lifecycle.

## Implementation Patterns & Consistency Rules

### Pattern Categories Defined

Critical conflict areas for AI agents:

- Native resource ownership.
- HDR constants and validation.
- Thread marshaling.
- Project/module structure.
- Error/degraded-state formats.
- Test placement and naming.
- Interop isolation.
- Crop coordinate mapping.

### Naming Patterns

**Database Naming Conventions:**

No database in MVP. If future storage is added, use lowercase snake_case table/column names and keep persistence under `src/Lumiere.Infrastructure/Storage`.

**API Naming Conventions:**

No network API in MVP. Internal service APIs use C# PascalCase methods and strongly typed records/classes.

**Code Naming Conventions:**

- Types: `PascalCase`, responsibility-based names, e.g. `GraphicsEngine`, `CaptureService`, `OverlayWindow`, `HdrConstants`.
- Interfaces: `I` prefix only for true abstraction boundaries, e.g. `IGraphicsEngine`, `ICaptureService`.
- Async methods: suffix `Async`.
- Private fields: `_camelCase`.
- Constants: `PascalCase` under a central static class, e.g. `HdrConstants.SwapChainFormat`.
- Events: past-tense or state-oriented names, e.g. `FrameArrived`, `CaptureFailed`, `StatusChanged`.

### Structure Patterns

**Project Organization:**

- Production source lives under `src/`.
- Tests live under `tests/`.
- Manual validation docs live under `docs/validation/`.
- Native interop lives only in `Lumiere.Infrastructure/Interop`.
- Direct3D/DXGI code lives only in `Lumiere.Graphics`.
- WGC session/frame pool code lives only in `Lumiere.Capture`.
- WinUI/XAML overlay code lives only in `Lumiere.App` and `Lumiere.Overlay`.

**File Structure Patterns:**

- One primary type per file.
- Avoid generic `Helpers.cs`; use specific names like `DxgiInterop.cs`, `WindowStyleService.cs`, `FramePoolFactory.cs`.
- Keep generated/package/build artifacts out of source directories.

### Format Patterns

**Internal Result Format:**

Use typed results for expected platform states:

```csharp
public sealed record OperationResult<T>(
    bool IsSuccess,
    T? Value,
    LumiereStatus Status,
    DiagnosticInfo? Diagnostic);
```

Use exceptions for invariant violations, invalid object lifetime use, or unexpected native failures that cannot be represented as degraded/unsupported.

**Diagnostic Format:**

Diagnostics must identify:

- Stage: capture, graphics, presentation, overlay, interop, lifecycle.
- Severity: info, warning, error.
- User message: concise and non-technical when shown in UI.
- Technical detail: HRESULT/API/stage-specific detail for advanced diagnostics.

### Communication Patterns

**Event System Patterns:**

- `CaptureService` may raise events from background/capture context.
- Subscribers must treat capture events as non-UI-thread events unless explicitly documented otherwise.
- UI subscribers must marshal via `DispatcherQueue`.
- Event payloads must be immutable records.

**State Management Patterns:**

- Keep state local to owning module.
- Expose read-only snapshots across boundaries.
- Do not share mutable Direct3D/WinRT objects beyond their lifetime owner.
- Do not retain WGC `Direct3D11CaptureFrame` after frame processing scope.

### Process Patterns

**Error Handling Patterns:**

- Expected capability failures become `Unsupported` or `Degraded` status.
- Unexpected HRESULT failures include operation name and native code in diagnostics.
- UI displays friendly summary plus optional advanced details.
- No silent fallback from HDR preview to SDR preview.

**Loading State Patterns:**

- Overlay state moves through explicit states: `Idle`, `SelectingTarget`, `InitializingGraphics`, `Capturing`, `Degraded`, `Failed`, `Confirming`, `Closing`.
- Loading/degraded UI must not resize the preview surface or change crop coordinate mapping.

### Enforcement Guidelines

**All AI Agents MUST:**

- Preserve FP16/scRGB constants in the live preview path.
- Keep module ownership boundaries strict.
- Implement `IDisposable` for any owner of WGC, WinRT, COM, D3D11, DXGI, swap-chain, texture, render-target, or frame-pool resources.
- Marshal WinUI access through `DispatcherQueue`.
- Detach `SwapChainPanel` with `SetSwapChain(null)` before graphics teardown.
- Surface degraded/unsupported states visibly.
- Add or update validation whenever HDR constants or lifecycle semantics change.

**Pattern Enforcement:**

- Review any changes to HDR constants, frame formats, swap-chain formats, disposal order, interop helpers, or thread dispatch explicitly.
- Add tests for lifecycle/state logic when implementation introduces test infrastructure.
- Keep manual HDR validation checklist current in `docs/validation/`.

### Pattern Examples

**Good Examples:**

- `CaptureService.StartAsync(CaptureTarget target, CancellationToken cancellationToken)`
- `GraphicsEngine.AttachToSwapChainPanelAsync(SwapChainPanel panel)`
- `HdrConstants.CapturePixelFormat = DirectXPixelFormat.R16G16B16A16Float`
- `using var frame = _framePool.TryGetNextFrame();`
- `dispatcherQueue.TryEnqueue(() => overlayState.Apply(status));`

**Anti-Patterns:**

- Rendering the main preview with `BitmapImage`, `SoftwareBitmap`, GDI, or 8-bit textures.
- Calling `SetSwapChain` from a capture callback thread.
- Letting `OverlayWindow` create a D3D11 device directly.
- Holding WGC frames in UI state.
- Falling back to SDR without setting visible degraded status.

## Project Structure & Boundaries

### Complete Project Directory Structure

```text
lumiere/
├── README.md
├── AGENTS.md
├── Lumiere.sln
├── Directory.Build.props
├── Directory.Packages.props
├── .editorconfig
├── .gitignore
├── docs/
│   ├── architecture/
│   │   └── architecture.md
│   ├── validation/
│   │   ├── hdr-manual-test-matrix.md
│   │   ├── lifecycle-validation.md
│   │   └── diagnostics-guide.md
│   └── decisions/
│       └── adr-0001-hdr-first-preview-pipeline.md
├── src/
│   ├── Lumiere.App/
│   │   ├── Lumiere.App.csproj
│   │   ├── App.xaml
│   │   ├── App.xaml.cs
│   │   ├── MainWindow.xaml
│   │   ├── MainWindow.xaml.cs
│   │   ├── app.manifest
│   │   └── Assets/
│   ├── Lumiere.Overlay/
│   │   ├── Lumiere.Overlay.csproj
│   │   ├── OverlayWindow.xaml
│   │   ├── OverlayWindow.xaml.cs
│   │   ├── OverlayViewModel.cs
│   │   ├── OverlayState.cs
│   │   ├── Crop/
│   │   │   ├── CropController.cs
│   │   │   ├── CropGeometry.cs
│   │   │   ├── CropHandle.cs
│   │   │   └── CoordinateMapper.cs
│   │   └── Windowing/
│   │       ├── OverlayWindowPresenter.cs
│   │       ├── WindowHitTestMode.cs
│   │       └── WindowStyleService.cs
│   ├── Lumiere.Capture/
│   │   ├── Lumiere.Capture.csproj
│   │   ├── CaptureService.cs
│   │   ├── CaptureTarget.cs
│   │   ├── CaptureSessionState.cs
│   │   ├── CaptureStatus.cs
│   │   ├── FrameArrivedEventArgs.cs
│   │   ├── FramePoolFactory.cs
│   │   └── TargetSelection/
│   │       ├── CapturePickerService.cs
│   │       ├── HwndCaptureTargetFactory.cs
│   │       └── MonitorCaptureTargetFactory.cs
│   ├── Lumiere.Graphics/
│   │   ├── Lumiere.Graphics.csproj
│   │   ├── GraphicsEngine.cs
│   │   ├── GraphicsDeviceProvider.cs
│   │   ├── HdrConstants.cs
│   │   ├── HdrValidationResult.cs
│   │   ├── RenderFrame.cs
│   │   ├── RenderTargetManager.cs
│   │   ├── SwapChainManager.cs
│   │   ├── SwapChainPanelPresenter.cs
│   │   ├── TextureRenderer.cs
│   │   └── Shaders/
│   │       ├── FullscreenQuad.hlsl
│   │       └── ShaderCompiler.cs
│   ├── Lumiere.Infrastructure/
│   │   ├── Lumiere.Infrastructure.csproj
│   │   ├── Diagnostics/
│   │   │   ├── DiagnosticInfo.cs
│   │   │   ├── DiagnosticSeverity.cs
│   │   │   ├── DiagnosticStage.cs
│   │   │   └── DiagnosticsService.cs
│   │   ├── Interop/
│   │   │   ├── ComObjectLifetime.cs
│   │   │   ├── Direct3D11Interop.cs
│   │   │   ├── DxgiInterop.cs
│   │   │   ├── GraphicsCaptureInterop.cs
│   │   │   ├── SwapChainPanelNativeInterop.cs
│   │   │   └── Win32WindowInterop.cs
│   │   ├── Results/
│   │   │   ├── LumiereStatus.cs
│   │   │   └── OperationResult.cs
│   │   └── Threading/
│   │       ├── DispatcherQueueExtensions.cs
│   │       └── UiThreadGuard.cs
│   └── Lumiere.Settings/
│       ├── Lumiere.Settings.csproj
│       ├── AppSettings.cs
│       └── SettingsStore.cs
├── tests/
│   ├── Lumiere.Capture.Tests/
│   │   ├── CaptureServiceLifecycleTests.cs
│   │   └── FramePoolConfigurationTests.cs
│   ├── Lumiere.Graphics.Tests/
│   │   ├── HdrConstantsTests.cs
│   │   ├── GraphicsEngineLifecycleTests.cs
│   │   └── SwapChainConfigurationTests.cs
│   ├── Lumiere.Overlay.Tests/
│   │   ├── CropControllerTests.cs
│   │   └── CoordinateMapperTests.cs
│   └── Lumiere.Infrastructure.Tests/
│       ├── OperationResultTests.cs
│       └── DiagnosticsServiceTests.cs
└── _bmad-output/
    └── planning-artifacts/
        └── architecture.md
```

### Architectural Boundaries

**API Boundaries:**

No external API boundary in MVP. Internal service boundaries are typed C# interfaces/classes.

**Component Boundaries:**

- `Lumiere.App`: application composition and startup only.
- `Lumiere.Overlay`: UI/crop/window behavior only.
- `Lumiere.Capture`: WGC capture lifecycle only.
- `Lumiere.Graphics`: D3D11/DXGI rendering and presentation only.
- `Lumiere.Infrastructure`: interop, diagnostics, result types, UI-thread helpers.
- `Lumiere.Settings`: local preferences only.

**Service Boundaries:**

- `CaptureService` does not render.
- `GraphicsEngine` does not select capture targets.
- `OverlayWindow` does not own native graphics resources.
- `DiagnosticsService` observes and reports; it does not control lifecycle.

**Data Boundaries:**

- GPU resources remain with their owning graphics/capture services.
- UI state uses simple immutable values and coordinate structs.
- Settings are local-only and must not store screenshot content in MVP.

### Requirements to Structure Mapping

**Capture Target Selection (FR1-FR5):**

- `src/Lumiere.Capture/CaptureService.cs`
- `src/Lumiere.Capture/TargetSelection/`
- `src/Lumiere.Infrastructure/Interop/GraphicsCaptureInterop.cs`

**HDR Preview Fidelity (FR6-FR10, NFR1-NFR4):**

- `src/Lumiere.Graphics/HdrConstants.cs`
- `src/Lumiere.Graphics/GraphicsEngine.cs`
- `src/Lumiere.Graphics/SwapChainManager.cs`
- `src/Lumiere.Graphics/SwapChainPanelPresenter.cs`
- `tests/Lumiere.Graphics.Tests/HdrConstantsTests.cs`

**Crop Interaction (FR11-FR16):**

- `src/Lumiere.Overlay/Crop/`
- `src/Lumiere.Overlay/OverlayWindow.xaml`
- `tests/Lumiere.Overlay.Tests/`

**Overlay and Desktop Window Behavior (FR17-FR21):**

- `src/Lumiere.Overlay/Windowing/`
- `src/Lumiere.Infrastructure/Interop/Win32WindowInterop.cs`
- `src/Lumiere.Graphics/SwapChainPanelPresenter.cs`

**Capability Detection and Diagnostics (FR22-FR26, NFR23-NFR26):**

- `src/Lumiere.Infrastructure/Diagnostics/`
- `src/Lumiere.Graphics/HdrValidationResult.cs`
- `docs/validation/diagnostics-guide.md`

**Resource Lifecycle and Session Management (FR27-FR31, NFR9-NFR13):**

- `src/Lumiere.Capture/CaptureService.cs`
- `src/Lumiere.Graphics/GraphicsEngine.cs`
- `src/Lumiere.Infrastructure/Interop/ComObjectLifetime.cs`
- lifecycle tests under `tests/`

**MVP Validation and Testing Support (FR32-FR35):**

- `docs/validation/hdr-manual-test-matrix.md`
- `docs/validation/lifecycle-validation.md`
- tests under `tests/`

**Settings and Preferences (FR36-FR38):**

- `src/Lumiere.Settings/`

**Post-MVP Output and Workflow (FR39-FR42):**

- Deferred. Do not create export/hotkey/annotation modules until separate stories define semantics.

### Integration Points

**Internal Communication:**

`OverlayUI` issues capture commands through `CaptureService`. `CaptureService` provides frames/status to `GraphicsEngine` and diagnostics. `GraphicsEngine` attaches/detaches the swap chain to `SwapChainPanel` only through a UI-thread-aware presenter.

**External Integrations:**

- Windows.Graphics.Capture.
- Direct3D 11.
- DXGI.
- WinUI 3 / Windows App SDK.
- Win32 HWND/HMONITOR/window style interop.

**Data Flow:**

1. User starts capture in WinUI.
2. Target selection returns a `CaptureTarget`.
3. `CaptureService` creates FP16 WGC frame pool/session.
4. WGC frame arrives.
5. Frame surface is accessed as D3D11 texture through interop.
6. `GraphicsEngine` renders/copies to FP16 swap-chain render target.
7. Swap chain presents through `SwapChainPanel`.
8. Overlay canvas maps crop rectangle over preview.
9. Confirm/cancel tears down or advances to future output pipeline.

### File Organization Patterns

**Configuration Files:**

- `Directory.Packages.props` centralizes package versions.
- `Directory.Build.props` centralizes TFM/platform defaults.
- App manifest stays under `Lumiere.App`.

**Source Organization:**

Source is organized by ownership boundary, not by generic technical layer.

**Test Organization:**

Tests mirror source project names and focus on lifecycle, constants, state, crop geometry, diagnostics, and boundaries.

**Asset Organization:**

Only UI assets live under `Lumiere.App/Assets`. Shader assets live under `Lumiere.Graphics/Shaders`.

### Development Workflow Integration

**Development Server Structure:**

Not applicable. This is a native desktop app.

**Build Process Structure:**

Build through Visual Studio/MSBuild using the solution and SDK-style project files.

**Deployment Structure:**

Packaging is deferred until after Phase 0. The structure supports either packaged or unpackaged Windows App SDK deployment.

## Architecture Validation Results

### Coherence Validation

**Decision Compatibility:**

The stack is coherent: WinUI 3 provides the app shell and overlay, WGC provides secure capture, D3D11/DXGI provide GPU/HDR rendering, Vortice provides C# bindings, and Windows App SDK supplies the desktop app platform. The selected package versions align with `.NET 10` and stable Windows App SDK guidance.

**Pattern Consistency:**

The implementation patterns reinforce the decisions: strict ownership prevents lifecycle ambiguity, centralized HDR constants prevent format drift, and dispatcher rules protect WinUI thread affinity.

**Structure Alignment:**

The project structure maps directly to the three core modules plus infrastructure and tests. It avoids generic folders where interop or graphics code could spread.

### Requirements Coverage Validation

**Feature Coverage:**

All MVP feature groups have architectural support: capture selection, HDR preview, crop overlay, diagnostics, lifecycle, and validation.

**Functional Requirements Coverage:**

FR1-FR38 are covered by the defined modules and boundaries. FR39-FR42 are explicitly deferred as post-MVP output/workflow capabilities.

**Non-Functional Requirements Coverage:**

NFR1-NFR28 are addressed through HDR constants, GPU-resident preview, deterministic disposal, UI-thread marshaling, local-only operation, explicit degraded states, test structure, and validation docs.

### Implementation Readiness Validation

**Decision Completeness:**

Critical implementation decisions are documented with versions, rationale, and enforcement rules.

**Structure Completeness:**

The directory structure is concrete enough for implementation stories to create projects, files, and tests without inventing ownership boundaries.

**Pattern Completeness:**

The major AI-agent conflict points are covered: naming, structure, result formats, diagnostics, lifecycle, threading, and anti-patterns.

### Gap Analysis Results

**Critical Gaps:** None remaining for MVP architecture.

**Important Gaps:**

- Exact test framework is not selected. This is acceptable until project scaffolding confirms repository conventions.
- Packaging mode is deferred until after Phase 0. This is intentional because HDR pipeline proof should gate distribution decisions.

**Nice-to-Have Gaps:**

- Export architecture needs separate research.
- Hotkey/tray architecture needs a post-MVP story.
- Device-lost handling can be expanded after initial graphics spike.

### Validation Issues Addressed

- Risk of SDR fallback is addressed by banning SDR/GDI/bitmap paths from the main live preview.
- Risk of wrong-thread UI access is addressed by dispatcher and presenter boundaries.
- Risk of resource leaks is addressed by explicit ownership, disposal rules, and teardown ordering.
- Risk of hardware variance is addressed by manual HDR validation and visible degraded states.

### Architecture Completeness Checklist

**Requirements Analysis**

- [x] Project context thoroughly analyzed.
- [x] Scale and complexity assessed.
- [x] Technical constraints identified.
- [x] Cross-cutting concerns mapped.

**Architectural Decisions**

- [x] Critical decisions documented with versions.
- [x] Technology stack fully specified.
- [x] Integration patterns defined.
- [x] Performance considerations addressed.

**Implementation Patterns**

- [x] Naming conventions established.
- [x] Structure patterns defined.
- [x] Communication patterns specified.
- [x] Process patterns documented.

**Project Structure**

- [x] Complete directory structure defined.
- [x] Component boundaries established.
- [x] Integration points mapped.
- [x] Requirements-to-structure mapping complete.

### Architecture Readiness Assessment

**Overall Status:** READY FOR IMPLEMENTATION

**Confidence Level:** High for architecture readiness; medium for HDR visual correctness until Phase 0 is verified on real HDR hardware.

**Key Strengths:**

- The architecture is aligned with the product's unique HDR value proposition.
- High-risk native resources have explicit owners.
- The live preview path avoids SDR-oriented convenience APIs.
- AI agents have concrete module boundaries and anti-patterns.
- Validation gates match the known risks from the PRD and technical research.

**Areas for Future Enhancement:**

- HDR export and SDR tone mapping.
- Device-lost recovery depth.
- Packaging/update strategy.
- Hotkey/tray workflow.
- Annotation and history modules.

### Implementation Handoff

**AI Agent Guidelines:**

- Follow this architecture document as the source of truth.
- Use the project context rules in `_bmad-output/project-context.md`.
- Preserve HDR constants and fail visibly when HDR correctness cannot be established.
- Respect module ownership boundaries.
- Prefer a working Phase 0 HDR spike before broad product UI work.

**First Implementation Priority:**

Create the WinUI 3 `.NET 10` solution, central package files, `Lumiere.App`, `Lumiere.Graphics`, `Lumiere.Capture`, `Lumiere.Overlay`, `Lumiere.Infrastructure`, and the first validation tests for HDR constants and lifecycle scaffolding.

## Workflow Completion Summary

The architecture workflow is complete. This document now captures the core technical direction, starter choice, major decisions, consistency rules, project structure, requirement mapping, and validation results for Lumiere.

The implementation phase should begin with the Phase 0 HDR pipeline spike:

1. Scaffold the WinUI 3 solution and projects.
2. Add locked package versions and platform settings.
3. Implement D3D11 device creation and WinRT interop.
4. Attach an FP16 DXGI composition swap chain to `SwapChainPanel`.
5. Create a WGC FP16 frame pool.
6. Render captured frames into the scRGB preview path.
7. Validate on real HDR hardware before expanding product scope.
