---
stepsCompleted: [1, 2, 3, 4]
inputDocuments:
  - '/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/prd.md'
  - '/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/architecture.md'
workflowType: 'epics-and-stories'
project_name: 'lumiere'
user_name: 'Asherliao'
date: '2026-04-20'
requirementsExtracted: true
lastStep: 4
status: 'complete'
completedAt: '2026-04-20'
---

# lumiere - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for lumiere, decomposing the requirements from the PRD and Architecture requirements into implementable stories.

## Requirements Inventory

### Functional Requirements

FR1: Users can initiate a new screen capture session from the desktop application.

FR2: Users can choose a display or window as the capture target.

FR3: Users can cancel capture target selection before a capture session begins.

FR4: The system can report when screen capture is unsupported on the current device or Windows configuration.

FR5: The system can distinguish between normal, degraded, and unsupported capture states.

FR6: Users can view a live preview of the selected capture target before confirming a crop.

FR7: The system can preserve HDR-oriented capture data in the primary preview workflow.

FR8: The system can validate that the primary preview path is using the required HDR-capable capture and presentation configuration.

FR9: The system can notify users when the preview cannot be trusted as HDR-correct.

FR10: Users can compare the app's preview state against a clear status indicator for HDR readiness.

FR11: Users can create a crop selection by dragging over the full-screen preview.

FR12: Users can adjust or recreate the crop selection before confirmation.

FR13: Users can confirm the selected capture region.

FR14: Users can cancel the capture overlay and return to the prior desktop state.

FR15: Users can see the active crop region and non-selected area clearly while selecting.

FR16: Users can complete the MVP crop workflow without configuring advanced settings.

FR17: Users can interact with a full-screen overlay that displays the capture preview and crop controls.

FR18: The system can keep preview rendering and interaction overlays visually layered in the correct order.

FR19: The system can handle transparent or borderless overlay behavior required for screenshot selection.

FR20: The system can manage overlay hit testing so crop selection remains possible.

FR21: The system can close or dismiss the overlay reliably after confirm, cancel, or failure.

FR22: Users can see concise diagnostic information when HDR capture or preview setup fails.

FR23: Advanced users can inspect whether the app is using the intended capture format, preview format, and color-space state.

FR24: The system can detect and report target display or monitor capability differences relevant to HDR preview correctness.

FR25: The system can report graphics initialization failures with enough context to support troubleshooting.

FR26: The system can surface degraded output warnings instead of silently presenting SDR fallback as valid.

FR27: The system can start, stop, and restart capture sessions without requiring app restart.

FR28: The system can release capture, preview, and graphics resources when a session ends.

FR29: The system can recreate capture and preview resources when target size or capture target changes.

FR30: The system can detach preview presentation resources before graphics teardown.

FR31: The system can prevent stale capture frames or invalid graphics surfaces from being reused after their valid lifetime.

FR32: Developers can run a minimal HDR pipeline spike independent of later product features.

FR33: Developers can verify the app's key HDR constants and capture/preview states.

FR34: Developers can repeat capture start/stop flows to check resource stability.

FR35: Developers can test capture behavior across HDR enabled, HDR disabled, SDR monitor, and multi-monitor scenarios.

FR36: Users can access minimal local preferences needed for capture behavior once those preferences exist.

FR37: Users can choose whether future capture sessions include cursor capture when that option is implemented.

FR38: Users can enable or disable advanced diagnostics when diagnostic UI is available.

FR39: Users can export or copy capture output after HDR/SDR output semantics are defined.

FR40: Users can choose between HDR-preserving output and SDR tone-mapped output when export support exists.

FR41: Users can use global hotkey or tray workflows when post-MVP desktop integration is implemented.

FR42: Users can add lightweight annotations when post-MVP annotation support is implemented.

### NonFunctional Requirements

NFR1: The primary preview pipeline must preserve FP16/scRGB capture data and must not silently downgrade to SDR.

NFR2: The system must expose a visible degraded or unsupported state when HDR preview correctness cannot be established.

NFR3: MVP validation must include side-by-side comparison against ordinary SDR screenshot output on real HDR hardware.

NFR4: HDR-related constants and configuration must be testable and centrally verifiable.

NFR5: Crop interaction must remain responsive during live preview under normal capture conditions.

NFR6: The live preview path must avoid CPU readback or bitmap conversion for routine frame presentation.

NFR7: Frame processing must release WGC frame objects promptly enough to avoid frame pool starvation during normal use.

NFR8: Overlay startup should feel immediate enough for screenshot use; any noticeable delay must be attributable to explicit target selection or graphics initialization.

NFR9: Repeated capture start, cancel, confirm, and restart flows must not produce unbounded GPU memory growth.

NFR10: All WGC, WinRT, COM, D3D11, DXGI, frame pool, session, texture, render target, and swap-chain resources must have deterministic teardown paths.

NFR11: The preview swap chain must be detached before graphics device teardown.

NFR12: Wrong-thread WinUI access must be prevented by design, not handled as a recoverable runtime error.

NFR13: Device/resource initialization failures must leave the application in a recoverable state.

NFR14: The MVP targets Windows desktop with `.NET 10 LTS` and `net10.0-windows10.0.19041.0`.

NFR15: The MVP targets `x64` first and must not rely on `Any CPU`.

NFR16: The application must run without network access for core capture workflows.

NFR17: The application must handle HDR and SDR monitor configurations without presenting misleading output.

NFR18: The application must use Windows capture consent and capability mechanisms.

NFR19: The MVP must not upload screenshots, telemetry, or display content to any remote service.

NFR20: Any future diagnostics must avoid capturing or exposing screenshot content unless explicitly user-approved.

NFR21: Borderless capture behavior must only be used with the required Windows capability and user consent.

NFR22: Core capture controls must be understandable without requiring graphics API knowledge.

NFR23: Error and degraded-state messages must be actionable for non-developer users while allowing advanced diagnostics for power users.

NFR24: Overlay controls should be keyboard-reachable where practical for MVP and must not trap users without a cancel path.

NFR25: Native interop code must be isolated behind narrow APIs.

NFR26: Diagnostics must identify capture stage, graphics initialization stage, and presentation stage failures separately.

NFR27: Package versions and target framework decisions must be recorded in project files once scaffolding begins.

NFR28: MVP code must preserve the module boundaries between capture, graphics rendering, and overlay UI.

### Additional Requirements

- Starter foundation must be a WinUI 3 Blank App with custom graphics/capture infrastructure, not Electron, Tauri, WPF bitmap-first, or web UI scaffolding.
- First implementation story must create or verify a WinUI 3 `.NET 10` solution using `net10.0-windows10.0.19041.0`, `x64` first, and Windows App SDK `1.8.260317003`.
- Project package decisions must include `Microsoft.WindowsAppSDK` `1.8.260317003`, `Vortice.Direct3D11` `3.8.3`, `Vortice.DXGI` `3.8.3`, and `Microsoft.Windows.CsWinRT` `2.2.0` only if concrete interop requires it.
- Core source boundaries must be organized around `Lumiere.App`, `Lumiere.Overlay`, `Lumiere.Capture`, `Lumiere.Graphics`, `Lumiere.Infrastructure`, and `Lumiere.Settings`.
- `CaptureService` must own WGC target selection, frame pool/session lifecycle, frame arrival, and frame disposal.
- `GraphicsEngine` must own D3D11 device/context, DXGI swap chain, render targets, shaders, HDR constants, resize handling, presentation, and WinUI swap-chain interop.
- `OverlayUI` must own the WinUI 3 fullscreen overlay, `SwapChainPanel`, crop canvas, toolbar, keyboard/mouse interaction, and user-facing state.
- Native interop, COM/DXGI/WinRT bridge code, and Win32 window style manipulation must stay behind narrow infrastructure APIs.
- The live HDR preview path must use WGC frame pool pixel format `DirectXPixelFormat.R16G16B16A16Float`.
- The live HDR preview path must use DXGI swap chain format `DXGI_FORMAT_R16G16B16A16_FLOAT`.
- The live HDR preview path must use DXGI color space `DXGI_COLOR_SPACE_RGB_FULL_G10_NONE_P709`.
- The preview surface must be a `SwapChainPanel` attached through `ISwapChainPanelNative.SetSwapChain`.
- `SetSwapChain` and WinUI state mutation must run on the UI thread through `DispatcherQueue`.
- Graphics teardown must call `SetSwapChain(null)` before releasing swap-chain/device resources.
- WGC frames must be disposed promptly and must not be retained after checkout lifetime.
- Direct HWND/HMONITOR capture item creation must account for Windows 10 1903/build 18362 or later behavior.
- MVP implementation must stay local/offline and must not introduce network or cloud dependencies.
- Internal communication must use typed service methods, immutable events/state snapshots, and typed result/status objects for degraded or unsupported states.
- Expected platform/capability failures must become visible `Unsupported` or `Degraded` states instead of silent fallback.
- `DiagnosticsService` or equivalent diagnostics infrastructure must identify capture, graphics, presentation, overlay, interop, and lifecycle stages.
- Manual validation docs must cover HDR enabled, HDR disabled, SDR monitor, multi-monitor, and repeated lifecycle teardown scenarios.
- Post-MVP export, clipboard, global hotkey, tray, annotation, and capture history work must remain deferred until separate stories define their semantics.

### UX Design Requirements

A standalone UX Design document exists at `_bmad-output/planning-artifacts/ux-design-specification.md` and must be used as an implementation input alongside the PRD and Architecture.

UX-related implementation work must honor the UX specification's requirements for the fullscreen overlay, HDR readiness/trust states, degraded/unsupported/failed recovery messages, crop interaction, keyboard cancellation, diagnostics disclosure, target context, accessibility, and layout stability.

### FR Coverage Map

FR1: Epic 2 - User can initiate a capture session from the desktop app.

FR2: Epic 2 - User can choose a display or window capture target.

FR3: Epic 2 - User can cancel target selection before capture starts.

FR4: Epic 2 - System reports unsupported screen capture.

FR5: Epic 2 - System distinguishes normal, degraded, and unsupported capture states.

FR6: Epic 1 - User can view a live preview of the selected capture target.

FR7: Epic 1 - System preserves HDR-oriented capture data in the primary preview workflow.

FR8: Epic 1 - System validates HDR-capable capture and presentation configuration.

FR9: Epic 1 - System notifies users when preview cannot be trusted as HDR-correct.

FR10: Epic 1 - User can compare preview state against a clear HDR readiness indicator.

FR11: Epic 3 - User can create a crop selection by dragging over the full-screen preview.

FR12: Epic 3 - User can adjust or recreate the crop selection before confirmation.

FR13: Epic 3 - User can confirm the selected capture region.

FR14: Epic 3 - User can cancel the capture overlay and return to the desktop state.

FR15: Epic 3 - User can see active crop region and non-selected area clearly.

FR16: Epic 3 - User can complete MVP crop workflow without advanced settings.

FR17: Epic 3 - User can interact with a full-screen overlay that displays preview and crop controls.

FR18: Epic 3 - System keeps preview rendering and interaction overlays layered correctly.

FR19: Epic 3 - System handles transparent/borderless overlay behavior for screenshot selection.

FR20: Epic 3 - System manages overlay hit testing so crop selection remains possible.

FR21: Epic 3 - System closes or dismisses overlay after confirm, cancel, or failure.

FR22: Epic 4 - User sees concise diagnostic information when HDR capture or preview setup fails.

FR23: Epic 4 - Advanced users can inspect capture format, preview format, and color-space state.

FR24: Epic 4 - System detects and reports display/monitor capability differences.

FR25: Epic 4 - System reports graphics initialization failures with troubleshooting context.

FR26: Epic 4 - System surfaces degraded output warnings instead of silent SDR fallback.

FR27: Epic 2 - System can start, stop, and restart capture sessions without app restart.

FR28: Epic 2 - System releases capture, preview, and graphics resources when a session ends.

FR29: Epic 2 - System recreates capture and preview resources when size or target changes.

FR30: Epic 2 - System detaches preview presentation resources before graphics teardown.

FR31: Epic 2 - System prevents stale frames or invalid graphics surfaces from reuse.

FR32: Epic 1 - Developers can run a minimal HDR pipeline spike independent of later product features.

FR33: Epic 1 - Developers can verify key HDR constants and capture/preview states.

FR34: Epic 2 - Developers can repeat capture start/stop flows to check resource stability.

FR35: Epic 4 - Developers can test capture behavior across HDR enabled, HDR disabled, SDR monitor, and multi-monitor scenarios.

FR36: Epic 5 - Users can access minimal local preferences once preferences exist.

FR37: Deferred/Post-MVP - Users can choose future cursor capture behavior when cursor capture semantics are implemented.

FR38: Epic 5 - Users can enable or disable advanced diagnostics when diagnostic UI exists.

FR39: Epic 6 - Users can export or copy capture output after output semantics are defined.

FR40: Epic 6 - Users can choose HDR-preserving output or SDR tone-mapped output.

FR41: Epic 6 - Users can use global hotkey or tray workflows post-MVP.

FR42: Epic 6 - Users can add lightweight annotations post-MVP.

## MVP Implementation Readiness Note

Epics 1-5 define the MVP implementation lane, with FR37 explicitly deferred until cursor capture behavior is specified. Epic 6 is roadmap/post-MVP only and must not be selected for MVP sprint planning.

## Epic List

### Epic 1: Trusted HDR Preview Foundation

Users and developers can prove that Lumiere's core HDR capture and preview promise works: a WGC FP16 frame reaches a Direct3D/DXGI scRGB swap-chain preview without SDR bitmap fallback, and the app can visibly state whether the preview is HDR-ready.

**FRs covered:** FR6, FR7, FR8, FR9, FR10, FR32, FR33

**Implementation notes:** This epic includes the WinUI 3/.NET 10 solution foundation, package version locking, HDR constants, D3D11 device creation, WinRT/DXGI interop, minimal WGC capture frame pool, `SwapChainPanel` presentation, and initial HDR validation. It must leave post-MVP export out of scope.

### Epic 2: Capture Target and Session Lifecycle

Users can start a capture session, choose or cancel a display/window target, and trust the app to start, stop, restart, resize, and tear down capture resources without stale frames or resource leaks.

**FRs covered:** FR1, FR2, FR3, FR4, FR5, FR27, FR28, FR29, FR30, FR31, FR34

**Implementation notes:** This epic productizes capture/session ownership around `CaptureService`, target selection, capture state transitions, resize/recreate behavior, prompt frame disposal, and swap-chain detachment before teardown.

### Epic 3: Fullscreen Overlay Crop Workflow

Users can interact with a full-screen capture overlay, create and adjust a crop selection over the HDR preview, confirm the selected region, or cancel and return safely to the desktop.

**FRs covered:** FR11, FR12, FR13, FR14, FR15, FR16, FR17, FR18, FR19, FR20, FR21

**Implementation notes:** This epic owns `OverlayUI`, `SwapChainPanel` layering, XAML crop canvas, crop geometry, coordinate mapping, hit testing, confirm/cancel behavior, and keyboard escape paths.

### Epic 4: Diagnostics and HDR Capability Trust

Users and developers can understand whether capture and preview are trustworthy, degraded, unsupported, or failed, with enough stage-specific information to troubleshoot HDR display and graphics issues.

**FRs covered:** FR22, FR23, FR24, FR25, FR26, FR35

**Implementation notes:** This epic adds diagnostic status surfaces, advanced technical details, capability checks for HDR/SDR and multi-monitor configurations, graphics initialization failure reporting, and the manual HDR validation matrix.

### Epic 5: Local Preferences and Diagnostic Controls

Users can access minimal local preferences that support the MVP workflow and choose whether advanced diagnostics are visible, while keeping the core capture experience local-only and simple.

**FRs covered:** FR36, FR38

**Deferred FRs referenced:** FR37

**Implementation notes:** This epic is intentionally thin for MVP. Advanced diagnostics visibility is the primary MVP preference. Cursor capture must not be treated as implemented MVP behavior until cursor capture semantics are defined in a separate story.

### Epic 6: Post-MVP Capture Output and Workflow Expansion

**Status:** Roadmap / Not ready for MVP implementation

Users can eventually export or copy capture output, choose HDR-preserving or SDR tone-mapped output, use hotkey/tray workflows, and add lightweight annotations after the HDR preview pipeline has been proven.

This epic must not be pulled into MVP sprint planning until separate research or design work defines HDR still export semantics, SDR tone mapping behavior, clipboard behavior, hotkey/tray architecture, and annotation rendering rules.

**FRs covered:** FR39, FR40, FR41, FR42

**Implementation notes:** This epic is a post-MVP holding epic. Stories here must not be pulled into MVP until HDR still export semantics, clipboard behavior, hotkey/tray architecture, and annotation rendering rules receive separate design/research.

## Epic 1: Trusted HDR Preview Foundation

Users and developers can prove that Lumiere's core HDR capture and preview promise works: a WGC FP16 frame reaches a Direct3D/DXGI scRGB swap-chain preview without SDR bitmap fallback, and the app can visibly state whether the preview is HDR-ready.

### Story 1.1: Scaffold the Native Windows App Foundation

As a developer,
I want the Lumiere solution scaffolded with the approved WinUI 3 and .NET foundation,
So that all future HDR capture work starts from the correct native Windows runtime and project boundaries.

**Requirements:** NFR14, NFR15, NFR27, NFR28, Architecture starter requirements, repository foundation workflow requirements

**Acceptance Criteria:**

**Given** a clean repository workspace
**When** repository foundation work begins
**Then** Git is initialized before WinUI scaffolding proceeds
**And** the repository contains `.gitignore`, `.editorconfig`, formatting configuration, README, and documented developer workflow conventions.

**Given** a clean repository workspace with repository foundation files in place
**When** the solution is created
**Then** it contains `Lumiere.sln`, `Directory.Build.props`, `Directory.Packages.props`, and the source projects defined by the architecture
**And** the app project targets `net10.0-windows10.0.19041.0` and `x64`.

**Given** the package configuration
**When** dependencies are restored
**Then** Windows App SDK, Vortice.Direct3D11, Vortice.DXGI, and any required CsWinRT package versions are pinned as architecture-approved versions.

**Given** a developer prepares a local change
**When** they read the repository workflow documentation
**Then** the expected formatting command, build/restore validation commands, and commit message convention are clear enough to follow before code review.

**Given** the solution is opened by a developer
**When** they inspect project references
**Then** UI, overlay, capture, graphics, infrastructure, and settings boundaries are represented as separate projects or modules.

### Story 1.2: Centralize HDR Constants and Preview Readiness Status

As a developer,
I want HDR formats and readiness states centralized,
So that implementation agents cannot accidentally weaken the capture or preview path.

**Requirements:** FR8, FR9, FR10, FR33, NFR1, NFR4

**Acceptance Criteria:**

**Given** the graphics project exists
**When** `HdrConstants` is implemented
**Then** it exposes `DirectXPixelFormat.R16G16B16A16Float`, `DXGI_FORMAT_R16G16B16A16_FLOAT`, and `DXGI_COLOR_SPACE_RGB_FULL_G10_NONE_P709`.

**Given** automated tests are run
**When** HDR constants are inspected
**Then** tests fail if any primary preview constant is changed to an 8-bit, SDR, bitmap, or GDI-oriented format.

**Given** the app initializes preview state
**When** HDR readiness cannot be established
**Then** a typed status reports `Degraded` or `Unsupported` instead of silently falling back.

### Story 1.3: Create D3D11 Device and WinRT/DXGI Interop Bridge

As a developer,
I want a narrow interop bridge for Direct3D, DXGI, WinRT, and COM objects,
So that capture and rendering code can share GPU resources without leaking native details into UI code.

**Requirements:** FR32, FR33, NFR25, Architecture interop requirements

**Acceptance Criteria:**

**Given** the graphics infrastructure is initialized
**When** the D3D11 device provider is created
**Then** it creates a device/context suitable for WGC and DXGI swap-chain rendering.

**Given** WGC requires a WinRT Direct3D device
**When** the interop bridge wraps the DXGI device
**Then** it returns a WinRT-compatible Direct3D device through a narrow infrastructure API.

**Given** interop calls fail
**When** HRESULT or COM failures occur
**Then** diagnostics include operation name, stage, and technical detail.

### Story 1.4: Attach an FP16 scRGB Swap Chain to SwapChainPanel

As an HDR screenshot user,
I want the preview surface to be hardware-rendered through an HDR-capable swap chain,
So that the app can preserve HDR appearance instead of showing a washed-out bitmap preview.

**Requirements:** FR6, FR7, FR8, FR9, FR10, FR30, NFR1, NFR6, NFR11

**Acceptance Criteria:**

**Given** a `SwapChainPanel` is available on the UI thread
**When** the graphics engine attaches a composition swap chain
**Then** the swap chain uses `DXGI_FORMAT_R16G16B16A16_FLOAT`.

**Given** the swap chain is created
**When** color space is configured
**Then** `DXGI_COLOR_SPACE_RGB_FULL_G10_NONE_P709` is set or a visible degraded/failed diagnostic is produced.

**Given** graphics teardown begins
**When** the preview is detached
**Then** `SetSwapChain(null)` is called on the UI thread before device-bound resources are released.

### Story 1.5: Prove Minimal WGC FP16 Capture to Live Preview

As an HDR display user,
I want a minimal live preview that preserves the source display appearance,
So that Lumiere's core product promise is proven before broader workflow features are built.

**Requirements:** FR6, FR7, FR8, FR9, FR10, FR32, FR33, NFR1, NFR3

**Acceptance Criteria:**

**Given** a capture target is selected for the spike
**When** WGC capture starts
**Then** the frame pool uses `DirectXPixelFormat.R16G16B16A16Float`.

**Given** a frame arrives
**When** the frame is rendered
**Then** the preview path remains GPU-resident and does not use `BitmapImage`, `SoftwareBitmap`, GDI, or CPU readback for routine presentation.

**Given** the preview is running on HDR hardware
**When** the app reports readiness
**Then** the user can see whether the preview is HDR-ready, degraded, unsupported, or failed.

## Epic 2: Capture Target and Session Lifecycle

Users can start a capture session, choose or cancel a display/window target, and trust the app to start, stop, restart, resize, and tear down capture resources without stale frames or resource leaks.

### Story 2.1: Start Capture and Select a Display or Window Target

As a screenshot user,
I want to start capture and choose a display or window,
So that I can decide exactly what Lumiere previews.

**Requirements:** FR1, FR2, FR3, NFR18

**Acceptance Criteria:**

**Given** the desktop app is running
**When** the user initiates capture
**Then** target selection begins through Windows-supported capture mechanisms.

**Given** target selection is open
**When** the user chooses a display or window
**Then** the app creates a typed capture target and proceeds toward session initialization.

**Given** the user cancels target selection
**When** cancellation is received
**Then** no capture session starts and the app returns to a recoverable idle state.

### Story 2.2: Represent Capture Session State Explicitly

As a user,
I want the app to distinguish normal, degraded, unsupported, and failed capture states,
So that I am not misled about whether the preview can be trusted.

**Requirements:** FR4, FR5, FR26, NFR2, NFR17

**Acceptance Criteria:**

**Given** capture is initialized
**When** platform capability checks pass
**Then** the session state moves to normal capturing state.

**Given** screen capture is unavailable
**When** the app attempts to initialize capture
**Then** the session enters `Unsupported` with a concise user-facing reason.

**Given** the app can capture but cannot prove HDR correctness
**When** validation fails
**Then** the session enters `Degraded` and no silent SDR fallback is presented as valid.

### Story 2.3: Stop, Restart, and Recreate Capture Resources

As a user,
I want capture sessions to stop and restart without restarting the app,
So that I can recover from cancellation, target changes, or display size changes.

**Requirements:** FR27, FR28, FR29, FR30, FR31, NFR7, NFR10, NFR11, NFR13

**Acceptance Criteria:**

**Given** an active capture session
**When** the user stops or cancels capture
**Then** WGC session, frame pool, frames, and related resources are disposed deterministically.

**Given** a target size changes
**When** the frame size no longer matches the preview resources
**Then** capture and preview resources are recreated safely.

**Given** capture restarts after teardown
**When** a new target is selected
**Then** stale frames or invalid surfaces from the previous session are not reused.

### Story 2.4: Validate Repeated Capture Lifecycle Stability

As a developer,
I want repeatable lifecycle validation for capture start, stop, cancel, and restart,
So that graphics and capture resources do not leak across sessions.

**Requirements:** FR34, NFR9, NFR10, NFR11

**Acceptance Criteria:**

**Given** lifecycle tests or manual diagnostics are run
**When** capture starts and stops repeatedly
**Then** session state returns to idle or recoverable failure without app restart.

**Given** graphics teardown occurs
**When** resources are released
**Then** preview presentation is detached before device-bound resources are disposed.

**Given** repeated sessions are exercised
**When** diagnostics are inspected
**Then** there is no evidence of unbounded frame pool, texture, render target, swap-chain, or device resource growth.

## Epic 3: Fullscreen Overlay Crop Workflow

Users can interact with a full-screen capture overlay, create and adjust a crop selection over the HDR preview, confirm the selected region, or cancel and return safely to the desktop.

### Story 3.1: Show a Fullscreen Overlay Above the HDR Preview

As a screenshot user,
I want a fullscreen overlay that contains the live preview and capture controls,
So that I can select a region without leaving the capture flow.

**Requirements:** FR17, FR18, FR21, NFR8

**Acceptance Criteria:**

**Given** capture preview is available
**When** the overlay opens
**Then** the `SwapChainPanel` fills the preview surface.

**Given** the overlay is visible
**When** UI controls render
**Then** the crop canvas and controls appear above the hardware preview.

**Given** overlay initialization fails
**When** the failure is detected
**Then** the overlay closes or reports failure without leaving an unusable topmost window.

### Story 3.2: Create a Crop Selection by Dragging

As a screenshot user,
I want to drag over the preview to create a crop selection,
So that I can choose the exact region I care about.

**Requirements:** FR11, FR15, FR16, NFR5

**Acceptance Criteria:**

**Given** the overlay is in selection mode
**When** the user presses and drags over the preview
**Then** a crop rectangle is created from the drag start and current pointer position.

**Given** the crop rectangle is active
**When** the user continues dragging
**Then** the active region and non-selected area remain visually clear.

**Given** drag coordinates leave expected bounds
**When** crop geometry is computed
**Then** the crop is clamped to the preview area.

### Story 3.3: Adjust or Recreate the Crop Selection

As a screenshot user,
I want to adjust or recreate my crop selection before confirming,
So that small selection mistakes do not force me to restart capture.

**Requirements:** FR12, FR15, FR16, NFR5

**Acceptance Criteria:**

**Given** a crop selection exists
**When** the user drags a handle or edge
**Then** the crop rectangle updates without shifting the preview layout.

**Given** a crop selection exists
**When** the user starts a new selection gesture according to the interaction rules
**Then** the previous crop is replaced by the new crop.

**Given** the crop changes
**When** coordinates are mapped
**Then** device-independent UI coordinates can be converted consistently for capture/rendering use.

### Story 3.4: Confirm or Cancel the Capture Overlay

As a screenshot user,
I want to confirm a crop or cancel the overlay,
So that I can complete or exit the MVP workflow predictably.

**Requirements:** FR13, FR14, FR21, NFR24

**Acceptance Criteria:**

**Given** a valid crop selection exists
**When** the user confirms
**Then** the selected crop region is captured as the confirmed MVP output state.

**Given** the overlay is open
**When** the user cancels
**Then** capture and preview resources begin teardown and the desktop state is restored.

**Given** confirm or cancel is invoked during a degraded state
**When** the operation completes
**Then** the user receives the appropriate status and the overlay does not remain stuck.

### Story 3.5: Manage Overlay Hit Testing and Keyboard Escape

As a screenshot user,
I want overlay input to work reliably,
So that transparency or topmost behavior does not prevent crop interaction or cancellation.

**Requirements:** FR19, FR20, NFR24

**Acceptance Criteria:**

**Given** the overlay uses transparent or borderless window behavior
**When** the user interacts with the crop canvas
**Then** hit testing routes input to crop controls instead of passing all input through the window.

**Given** the overlay is active
**When** the user presses the cancel key
**Then** the capture flow exits safely.

**Given** controls are visible
**When** keyboard navigation is used where practical for MVP
**Then** the user is not trapped without a cancel path.

## Epic 4: Diagnostics and HDR Capability Trust

Users and developers can understand whether capture and preview are trustworthy, degraded, unsupported, or failed, with enough stage-specific information to troubleshoot HDR display and graphics issues.

### Story 4.1: Show User-Facing Capture and Preview Status

As an HDR screenshot user,
I want concise status messages for capture and preview health,
So that I know whether I can trust what I see.

**Requirements:** FR22, FR26, NFR2, NFR23

**Acceptance Criteria:**

**Given** capture or preview initialization succeeds
**When** the overlay is displayed
**Then** the user can see a clear normal or HDR-ready status.

**Given** capture or preview is degraded
**When** the status is shown
**Then** the message explains that preview fidelity cannot be fully trusted.

**Given** capture is unsupported or failed
**When** the status is shown
**Then** the message is actionable and does not require graphics API knowledge.

### Story 4.2: Provide Advanced Technical Diagnostics

As a power user or developer,
I want advanced diagnostics for capture format, preview format, and color-space state,
So that I can troubleshoot HDR behavior and implementation issues.

**Requirements:** FR23, FR25, NFR26

**Acceptance Criteria:**

**Given** advanced diagnostics are enabled
**When** a capture session is active
**Then** diagnostics include capture pixel format, swap-chain format, color space, and current status.

**Given** graphics initialization fails
**When** diagnostics are inspected
**Then** the failure identifies the capture, graphics, presentation, overlay, interop, or lifecycle stage.

**Given** native interop returns an error
**When** diagnostics are captured
**Then** technical detail includes operation context and native error information where available.

### Story 4.3: Detect HDR, SDR, and Multi-Monitor Capability Differences

As a multi-monitor power user,
I want Lumiere to detect relevant HDR/SDR monitor differences,
So that I know when target display capabilities affect preview correctness.

**Requirements:** FR24, FR35, NFR17

**Acceptance Criteria:**

**Given** a capture target is selected
**When** display capability checks run
**Then** the app reports capability information relevant to HDR preview correctness.

**Given** the selected target is SDR or HDR cannot be proven
**When** preview starts
**Then** the app shows normal, degraded, or unsupported status as appropriate.

**Given** multiple monitors are present
**When** the capture target changes
**Then** diagnostics reflect the current target rather than stale monitor data.

### Story 4.4: Document Manual HDR Validation Scenarios

As a developer,
I want a manual HDR validation matrix,
So that hardware-dependent fidelity behavior can be checked consistently.

**Requirements:** FR35, NFR3

**Acceptance Criteria:**

**Given** validation docs are generated
**When** a developer opens the HDR manual test matrix
**Then** it covers HDR enabled, HDR disabled, SDR monitor, and multi-monitor scenarios.

**Given** lifecycle validation docs are generated
**When** repeated capture sessions are tested
**Then** the checklist covers start, stop, cancel, confirm, target change, resize, and teardown.

**Given** diagnostics guidance is generated
**When** an HDR issue is investigated
**Then** the guide maps user-visible status to capture, graphics, presentation, or interop causes.

## Epic 5: Local Preferences and Diagnostic Controls

Users can access minimal local preferences that support the MVP workflow and choose whether advanced diagnostics are visible, while keeping the core capture experience local-only and simple.

### Story 5.1: Add a Minimal Local Settings Store

As a user,
I want Lumiere to remember minimal local preferences,
So that simple capture behavior can persist without accounts or network services.

**Requirements:** FR36, NFR16, NFR19, NFR20

**Acceptance Criteria:**

**Given** the app starts
**When** settings are loaded
**Then** local preferences are read from a local settings store without network access.

**Given** a preference changes
**When** the app saves settings
**Then** only non-content preferences are persisted.

**Given** settings are unavailable or corrupt
**When** the app starts
**Then** safe defaults are used and core capture remains recoverable.

### Story 5.2: Control Advanced Diagnostics Visibility

As a user,
I want to enable or disable advanced diagnostics,
So that non-developer users can keep the capture UI simple while power users can inspect details.

**Requirements:** FR38, NFR22, NFR23

**Acceptance Criteria:**

**Given** diagnostics visibility is disabled
**When** the overlay shows status
**Then** only concise user-facing messages appear.

**Given** diagnostics visibility is enabled
**When** the overlay shows status
**Then** advanced capture, preview, and color-space details are available.

**Given** cursor capture semantics have not been defined
**When** settings are displayed for MVP
**Then** cursor capture is omitted from implemented preferences and not presented as a working option.

## Epic 6: Post-MVP Capture Output and Workflow Expansion

Users can eventually export or copy capture output, choose HDR-preserving or SDR tone-mapped output, use hotkey/tray workflows, and add lightweight annotations after the HDR preview pipeline has been proven.

### Story 6.1: Define HDR Export and Clipboard Semantics

**Status:** Post-MVP research/specification candidate; not part of MVP implementation.

As a product owner,
I want HDR export and clipboard semantics researched and specified,
So that future output features do not compromise the proven preview pipeline.

**Requirements:** FR39, FR40

**Acceptance Criteria:**

**Given** the MVP preview path is proven
**When** export planning begins
**Then** HDR-preserving and SDR tone-mapped output semantics are documented before implementation.

**Given** clipboard behavior is considered
**When** output options are specified
**Then** HDR and SDR clipboard expectations are explicit.

**Given** semantics are not yet approved
**When** implementation work is planned
**Then** export and clipboard code is not added to the MVP preview path.

### Story 6.2: Implement Explicit HDR or SDR Capture Output

**Status:** Post-MVP implementation candidate; blocked until Story 6.1 or equivalent output semantics are approved.

As a screenshot user,
I want to export or copy capture output with explicit HDR/SDR handling,
So that saved or copied captures do not misrepresent fidelity.

**Requirements:** FR39, FR40

**Acceptance Criteria:**

**Given** output semantics are approved
**When** the user exports or copies a capture
**Then** the app clearly identifies whether output is HDR-preserving or SDR tone-mapped.

**Given** SDR tone mapping is selected
**When** output is produced
**Then** the user is not told the result is HDR-preserving.

**Given** HDR-preserving output is unavailable
**When** the user attempts HDR output
**Then** the app reports unsupported or degraded output rather than silently producing SDR.

### Story 6.3: Add Global Hotkey and Tray Workflow

**Status:** Post-MVP implementation candidate; blocked until hotkey/tray architecture is specified.

As a frequent screenshot user,
I want global hotkey and tray access,
So that I can start capture quickly from normal desktop work.

**Requirements:** FR41

**Acceptance Criteria:**

**Given** hotkey support is implemented
**When** the user presses the configured hotkey
**Then** the capture target flow starts without opening unrelated UI.

**Given** tray support is implemented
**When** the user opens the tray menu
**Then** capture actions and basic status are available.

**Given** permissions or OS behavior prevent hotkey registration
**When** the app starts
**Then** the user receives a visible, recoverable diagnostic.

### Story 6.4: Add Lightweight Annotation over Confirmed Capture Output

**Status:** Post-MVP implementation candidate; blocked until output and annotation rendering semantics are specified.

As a screenshot user,
I want lightweight annotations after capture,
So that I can mark up a capture without weakening preview correctness.

**Requirements:** FR42

**Acceptance Criteria:**

**Given** a capture output path is defined
**When** annotation mode opens
**Then** annotation rendering is separated from the primary HDR live preview pipeline.

**Given** the user adds annotation marks
**When** output is saved or copied
**Then** output semantics still identify HDR-preserving or SDR tone-mapped behavior.

**Given** annotation support is not available in MVP
**When** MVP stories are planned
**Then** annotation work remains excluded from the core HDR preview proof.
