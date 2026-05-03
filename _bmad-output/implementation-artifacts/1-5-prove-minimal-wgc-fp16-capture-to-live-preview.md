# Story 1.5: Prove Minimal WGC FP16 Capture to Live Preview

Status: done

<!-- Rewritten in English on 2026-05-04 to remove mojibake/encoding-corrupted text. -->

## Story

As an HDR display user,
I want a minimal live preview that preserves the source display appearance,
so that Lumiere's core product promise is proven before broader workflow features are built.

## Acceptance Criteria

1. Given a capture target is selected for the spike, when WGC capture starts, then the frame pool uses `DirectXPixelFormat.R16G16B16A16Float`.
2. Given a frame arrives, when the frame is rendered, then the preview path remains GPU-resident and does not use `BitmapImage`, `SoftwareBitmap`, GDI, or CPU readback for routine presentation.
3. Given the preview is running on HDR hardware, when the app reports readiness, then the user can see whether the preview is HDR-ready, degraded, unsupported, or failed.

## Tasks / Subtasks

- [x] Reuse Story 1.1-1.4 foundations instead of recreating graphics or interop infrastructure. (AC: 1, 2, 3)
  - [x] Reuse the D3D11 device provider and graphics resources.
  - [x] Reuse centralized HDR constants and readiness states.
  - [x] Reuse WinRT/DXGI and `SwapChainPanel` interop boundaries.
- [x] Add a minimal WGC capture session lifecycle inside `Lumiere.Capture`. (AC: 1, 2)
  - [x] Add `CaptureService`, `CaptureSessionResources`, `CaptureTarget`, `CaptureStartResult`, and related lifecycle helpers.
  - [x] Create a WinRT `IDirect3DDevice` from the existing DXGI device.
  - [x] Create `Direct3D11CaptureFramePool` with `DirectXPixelFormat.R16G16B16A16Float`.
  - [x] Keep `FrameArrived` as a non-UI-thread boundary.
  - [x] Dispose WGC session, frame pool, frames, and COM/WinRT resources deterministically.
- [x] Connect WGC frames to the existing FP16/scRGB swap-chain preview. (AC: 2)
  - [x] Add `CapturedFrameTexture`, `PreviewFramePresenter`, `PreviewRenderResult`, and `SwapChainFrameOutput`.
  - [x] Unwrap captured frame surfaces as `ID3D11Texture2D`.
  - [x] Present frames through the GPU-resident path without CPU bitmap fallback.
- [x] Wire minimal app preview and user-visible readiness. (AC: 3)
  - [x] Update `MainWindow.xaml` / `MainWindow.xaml.cs` to host a `SwapChainPanel`.
  - [x] Show explicit labels: `HDR-ready`, `Degraded preview`, `Unsupported capture`, `Preview failed`, and `Initializing preview`.
  - [x] Keep target picker and preview wiring minimal; formal target/session lifecycle continues in Epic 2.
- [x] Add tests and validation notes. (AC: 1, 2, 3)
  - [x] Add tests for capture configuration defaults.
  - [x] Add tests for capture lifecycle/disposal ordering.
  - [x] Add tests for preview frame presentation readiness mapping.
  - [x] Record Windows validation and manual HDR validation status separately.

## Review Findings

- [x] [Review][Decision] Story/sprint status overstates completion before Windows manual HDR validation. Resolved by keeping Story 1.5 implementation `done` while tracking manual HDR validation separately.
- [x] [Review][Patch] StopPreview can deadlock with an in-flight free-threaded frame callback [src/Lumiere.App/MainWindow.xaml.cs:138]
- [x] [Review][Patch] StartCapture failure leaves newly attached preview resources alive [src/Lumiere.App/MainWindow.xaml.cs:80]
- [x] [Review][Patch] StartPreview can race StopPreview and lose the newly started capture session [src/Lumiere.App/MainWindow.xaml.cs:94]
- [x] [Review][Patch] Add direct test coverage for `CaptureStartResult.StartSucceeded` [src/Lumiere.Capture/CaptureStartResult.cs:21]
- [x] [Review][Patch] Frame surface lifetime crosses the `Direct3D11CaptureFrame` lifetime [src/Lumiere.Capture/CaptureService.cs:103]

## Dev Notes

### Story Scope

This story proves the minimum technical chain: `WGC -> FP16 frame -> GPU-resident preview`. It intentionally does not implement the full product target-selection UX, fullscreen crop overlay, export, clipboard, annotation, history, tray, or hotkey features.

Epic 2 continues from this point by productizing capture target selection and session lifecycle. Epic 3 later adds overlay crop behavior.

### Current Repository Context

Story 1.5 builds directly on:

- `src/Lumiere.Graphics/Devices/GraphicsDeviceProvider.cs`
- `src/Lumiere.Graphics/Devices/GraphicsDeviceResources.cs`
- `src/Lumiere.Graphics/Hdr/HdrConstants.cs`
- `src/Lumiere.Graphics/Hdr/PreviewReadinessStatus.cs`
- `src/Lumiere.Graphics/Presentation/GraphicsEngine.cs`
- `src/Lumiere.Graphics/Presentation/SwapChainManager.cs`
- `src/Lumiere.Graphics/Presentation/SwapChainResources.cs`
- `src/Lumiere.Infrastructure/Interop/Direct3D11Interop.cs`
- `src/Lumiere.Infrastructure/Interop/SwapChainPanelPreviewSurface.cs`

### Technical Guardrails

- `Direct3D11CaptureFramePool` must use `DirectXPixelFormat.R16G16B16A16Float`.
- Routine frame presentation must remain GPU-resident.
- Do not use `BitmapImage`, `SoftwareBitmap`, WIC, GDI, CPU map/readback, byte arrays, PNG, or XAML `Image` as the main preview path.
- `FrameArrived` may run on a background worker thread; WinUI state and `SwapChainPanel` interactions must marshal to the UI thread.
- WGC frames must not be retained beyond their valid frame lifetime.
- Capture/presentation failures must map to visible readiness states and diagnostics.

### Previous Story Intelligence

Story 1.4 established a mandatory `ISwapChainPreviewSurface` attach path and detach-before-release semantics. Story 1.5 must preserve that order during capture startup, failure rollback, stop, and window close.

### Validation Notes

Windows validation was recorded as passed:

- `dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false`
- `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false`
- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false` with 37 passing tests
- `dotnet format Lumiere.sln --verify-no-changes --verbosity minimal`

Validation level recorded in the story:

- Mac edit: not used in the final Windows validation session.
- Windows local build/test/format: passed.
- Windows manual-pass: completed on 2026-05-04 for the Epic 1 minimal HDR preview proof path only. The app reached `HDR-ready` after target selection, and diagnostics reported that `Direct3D11CaptureFrame.Surface` reached the FP16 scRGB swap chain without CPU readback.
- Epic 2 lifecycle behavior remains out of scope and not started.

## Dev Agent Record

### Agent Model Used

GPT-5

### Completion Notes List

- Added minimal WGC capture session types under `Lumiere.Capture`.
- Added `Direct3D11SurfaceInterop` and `GraphicsCapturePickerInterop` under `Lumiere.Infrastructure`.
- Added `CapturedFrameTexture`, `PreviewFramePresenter`, `PreviewRenderResult`, and `SwapChainFrameOutput` under `Lumiere.Graphics.Presentation`.
- Updated `MainWindow` to host a `SwapChainPanel`, show explicit readiness labels, and start minimal live preview through the capture picker.
- Added guardrail tests for capture configuration defaults, capture disposal ordering, and preview frame presentation readiness mapping.
- Fixed `CaptureStartResult.Started(...)` naming conflict by renaming the static factory to `CaptureStartResult.StartSucceeded(...)`.
- Resolved captured frame lifetime issue by presenting frame textures synchronously inside the `Direct3D11CaptureFrame` lifetime and queuing only readiness updates to the UI thread.
- Resolved stop/start races, failed capture rollback, and success-factory coverage findings.

### File List

- _bmad-output/implementation-artifacts/1-5-prove-minimal-wgc-fp16-capture-to-live-preview.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- src/Lumiere.App/MainWindow.xaml
- src/Lumiere.App/MainWindow.xaml.cs
- src/Lumiere.Capture/CaptureService.cs
- src/Lumiere.Capture/CaptureSessionDisposalCoordinator.cs
- src/Lumiere.Capture/CaptureSessionOptions.cs
- src/Lumiere.Capture/CaptureSessionResources.cs
- src/Lumiere.Capture/CaptureStartResult.cs
- src/Lumiere.Capture/CaptureTarget.cs
- src/Lumiere.Capture/Properties/AssemblyInfo.cs
- src/Lumiere.Graphics/Presentation/CapturedFrameTexture.cs
- src/Lumiere.Graphics/Presentation/IPreviewFrameOutput.cs
- src/Lumiere.Graphics/Presentation/PreviewFramePresenter.cs
- src/Lumiere.Graphics/Presentation/PreviewRenderResult.cs
- src/Lumiere.Graphics/Presentation/SwapChainFrameOutput.cs
- src/Lumiere.Graphics/Properties/AssemblyInfo.cs
- src/Lumiere.Infrastructure/Interop/Direct3D11SurfaceInterop.cs
- src/Lumiere.Infrastructure/Interop/GraphicsCapturePickerInterop.cs
- tests/Lumiere.Graphics.Tests/Capture/CaptureLifecycleTests.cs
- tests/Lumiere.Graphics.Tests/Capture/CaptureSessionConfigurationTests.cs
- tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj
- tests/Lumiere.Graphics.Tests/Presentation/PreviewFramePresenterTests.cs

### Change Log

- 2026-04-24: Completed Story 1.5 implementation validation on Windows, fixed `CaptureStartResult` factory naming conflict, updated task checkboxes, and moved the story to review.
- 2026-04-24: Resolved code review finding for captured frame lifetime and moved the story to done.
- 2026-04-24: Resolved follow-up Edge Case Hunter findings for stop/start races, failed capture rollback, and success-factory coverage.
- 2026-05-04: Rewrote story document in English to remove mojibake text.
