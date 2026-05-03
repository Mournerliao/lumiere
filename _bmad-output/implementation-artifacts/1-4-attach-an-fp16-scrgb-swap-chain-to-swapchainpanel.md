# Story 1.4: Attach an FP16 scRGB Swap Chain to SwapChainPanel

Status: done

<!-- Rewritten in English on 2026-05-04 to remove mojibake/encoding-corrupted text. -->

## Story

As an HDR screenshot user,
I want the preview surface to be hardware-rendered through an HDR-capable swap chain,
so that the app can preserve HDR appearance instead of showing a washed-out bitmap preview.

## Acceptance Criteria

1. Given a `SwapChainPanel` is available on the UI thread, when the graphics engine attaches a composition swap chain, then the swap chain uses `DXGI_FORMAT_R16G16B16A16_FLOAT`.
2. Given the swap chain is created, when color space is configured, then `DXGI_COLOR_SPACE_RGB_FULL_G10_NONE_P709` is set or a visible degraded/failed diagnostic is produced.
3. Given graphics teardown begins, when the preview is detached, then `SetSwapChain(null)` is called on the UI thread before device-bound resources are released.

## Tasks / Subtasks

- [x] Reuse the Story 1.3 graphics device and interop foundation. (AC: 1, 2, 3)
  - [x] Reuse `GraphicsDeviceResources`; do not create a second D3D11 device.
  - [x] Reuse centralized HDR constants; do not duplicate format/color-space literals.
- [x] Implement FP16/scRGB swap-chain creation. (AC: 1)
  - [x] Add `SwapChainCreationOptions`.
  - [x] Add `SwapChainManager`.
  - [x] Use FP16 `HdrConstants.DxgiSwapChainFormat`, flip sequential presentation, stretch scaling, two buffers, and no MSAA.
- [x] Implement color-space validation and readiness mapping. (AC: 2)
  - [x] Add color-space configurator/controller abstractions.
  - [x] Set or report inability to set `HdrConstants.DxgiColorSpace`.
  - [x] Keep presentation evidence non-ready until real live preview exists.
- [x] Attach the swap chain through a narrow `SwapChainPanel` preview surface. (AC: 1, 3)
  - [x] Add `SwapChainPanelNativeInterop`.
  - [x] Add `ISwapChainPreviewSurface` and `SwapChainPanelPreviewSurface`.
  - [x] Attach through `GraphicsEngine.CreatePreviewSwapChain(..., previewSurface)`.
- [x] Make detach-before-release mandatory and retryable. (AC: 3)
  - [x] Add `SwapChainResources` and `SwapChainDisposalCoordinator`.
  - [x] Require a detach action before releasing swap-chain resources.
  - [x] Mark resources disposed only after detach and release complete.
- [x] Add focused tests. (AC: 1, 2, 3)
  - [x] Test descriptor/configuration values.
  - [x] Test readiness mapping.
  - [x] Test attach failure diagnostic mapping.
  - [x] Test detach-before-release ordering.

## Dev Notes

### Story Scope

This story establishes presentation infrastructure only: an FP16/scRGB DXGI composition swap chain, `SwapChainPanel` attach/detach, color-space validation, presentation readiness diagnostics, and deterministic teardown.

It does not create a WGC frame pool, receive live frames, write shader copy/rendering, implement crop overlay behavior, or produce screenshot output. Story 1.5 proves the full WGC FP16 capture-to-live-preview chain.

### Current Repository Context

Story 1.4 builds on:

- `src/Lumiere.Graphics/Devices/GraphicsDeviceProvider.cs`
- `src/Lumiere.Graphics/Devices/GraphicsDeviceResources.cs`
- `src/Lumiere.Graphics/Hdr/HdrConstants.cs`
- `src/Lumiere.Graphics/Hdr/PreviewReadinessStatus.cs`
- `src/Lumiere.Infrastructure/Interop/Direct3D11Interop.cs`
- `src/Lumiere.Infrastructure/Interop/NativeInteropException.cs`
- existing xUnit tests under `tests/Lumiere.Graphics.Tests/`

### Architecture Compliance

- `Lumiere.Graphics` owns swap-chain creation, render target/back-buffer ownership, presentation validation, readiness evidence, and resource teardown.
- `Lumiere.Infrastructure/Interop` owns `ISwapChainPanelNative`, COM/HRESULT handling, and WinUI swap-chain attach/detach interop.
- `Lumiere.App` and `Lumiere.Overlay` may host a `SwapChainPanel`, but must not create DXGI swap chains or call raw COM interop directly.
- `Lumiere.Capture` must not own presentation resources.

### Technical Guardrails

- The swap chain must use `DXGI_FORMAT_R16G16B16A16_FLOAT`.
- The color space must be `DXGI_COLOR_SPACE_RGB_FULL_G10_NONE_P709`, or the app must report degraded/failed readiness.
- `SetSwapChain` and `SetSwapChain(null)` are UI-thread operations.
- `SetSwapChain(null)` must happen before device-bound swap-chain resources are released.
- Do not introduce `BitmapImage`, `SoftwareBitmap`, GDI, CPU readback, XAML `Image`, or 8-bit SDR preview paths.
- Do not mark preview `Ready` merely because presentation resources exist; live frame proof belongs to Story 1.5.

### Previous Story Intelligence

Story 1.3 already completed D3D11 device creation, deterministic disposal, and WinRT/DXGI bridge work. Story 1.4 must reuse those resources and continue the established pattern: small ownership-focused types, typed diagnostics, and guardrail tests.

### Validation Notes

Windows validation was recorded as passed:

- `dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false`
- `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false`
- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false`
- `dotnet format Lumiere.sln --verify-no-changes --verbosity minimal`

Real `SwapChainPanel` UI-thread attachment was encapsulated but not fully exercised by automated tests.

## Review Findings

- [x] [Review][Decision] Decide whether Story 1.4 requires a real `SwapChainPanel` attach path or only attachable APIs. Resolved by adding mandatory preview surface attach through `ISwapChainPreviewSurface` / `SwapChainPanelPreviewSurface` and `GraphicsEngine.CreatePreviewSwapChain(..., previewSurface)`.
- [x] [Review][Patch] `SwapChainResources` can release an attached swap chain without a mandatory detach action [src/Lumiere.Graphics/Presentation/SwapChainResources.cs:11]
- [x] [Review][Patch] `SwapChainResources.Dispose` marks the object disposed before detach succeeds, preventing retry after UI-thread or COM detach failure [src/Lumiere.Graphics/Presentation/SwapChainResources.cs:25]

## Dev Agent Record

### Agent Model Used

GPT-5

### Completion Notes List

- Implemented `SwapChainCreationOptions` for FP16 composition swap-chain descriptors.
- Implemented `SwapChainManager` and `GraphicsEngine` presentation entry points that reuse existing `GraphicsDeviceResources.Device`.
- Implemented `SwapChainColorSpaceConfigurator` and `SwapChainColorSpaceController`.
- Added `SwapChainResources` and `SwapChainDisposalCoordinator` so presentation detach occurs before swap-chain release.
- Added `SwapChainPanelNativeInterop` to encapsulate `ISwapChainPanelNative.SetSwapChain` and `SetSwapChain(null)`.
- Added `ISwapChainPreviewSurface` and `SwapChainPanelPreviewSurface`.
- Added `PreviewReadinessStatus.Initializing(PreviewReadinessStage, ...)` overload.
- Did not implement WGC frame pool, live frame rendering, shader path, crop UI, export, clipboard, hotkey, tray, annotation, history, cloud, telemetry, or SDR fallback behavior.

### File List

- src/Lumiere.Graphics/Hdr/PreviewReadinessStatus.cs
- src/Lumiere.Graphics/Presentation/GraphicsEngine.cs
- src/Lumiere.Graphics/Presentation/ISwapChainColorSpaceController.cs
- src/Lumiere.Graphics/Presentation/SwapChainColorSpaceConfigurator.cs
- src/Lumiere.Graphics/Presentation/SwapChainColorSpaceController.cs
- src/Lumiere.Graphics/Presentation/SwapChainCreationOptions.cs
- src/Lumiere.Graphics/Presentation/SwapChainDisposalCoordinator.cs
- src/Lumiere.Graphics/Presentation/SwapChainManager.cs
- src/Lumiere.Graphics/Presentation/SwapChainPresentationException.cs
- src/Lumiere.Graphics/Presentation/SwapChainResources.cs
- src/Lumiere.Infrastructure/Interop/SwapChainPanelNativeInterop.cs
- src/Lumiere.Infrastructure/Lumiere.Infrastructure.csproj
- tests/Lumiere.Graphics.Tests/Presentation/SwapChainConfigurationTests.cs
- tests/Lumiere.Graphics.Tests/Presentation/SwapChainLifecycleTests.cs
- tests/Lumiere.Graphics.Tests/Presentation/SwapChainReadinessTests.cs
- _bmad-output/implementation-artifacts/1-4-attach-an-fp16-scrgb-swap-chain-to-swapchainpanel.md
- _bmad-output/implementation-artifacts/sprint-status.yaml

### Change Log

- 2026-04-23: Implemented Story 1.4 FP16/scRGB composition swap-chain presentation foundation and marked story ready for review.
- 2026-04-23: Addressed code review findings by adding a mandatory preview surface attach path and making detach required/retryable before swap-chain release.
- 2026-05-04: Rewrote story document in English to remove mojibake text.
