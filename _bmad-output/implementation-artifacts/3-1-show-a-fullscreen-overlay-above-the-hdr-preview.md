# Story 3.1: Show a Fullscreen Overlay Above the HDR Preview

Status: done

<!-- Ultimate context engine analysis completed - comprehensive developer guide created. -->

## Story

As a screenshot user,
I want a fullscreen overlay that contains the live preview and capture controls,
so that I can select a region without leaving the capture flow.

## Acceptance Criteria

1. Given capture preview is available, when the overlay opens, then the `SwapChainPanel` fills the preview surface.
2. Given the overlay is visible, when UI controls render, then the crop canvas and controls appear above the hardware preview.
3. Given overlay initialization fails, when the failure is detected, then the overlay closes or reports failure without leaving an unusable topmost window.

## Tasks / Subtasks

- [x] Create the initial overlay window surface inside the `Lumiere.Overlay` boundary. (AC: 1, 2)
  - [x] Replace the placeholder-only overlay boundary with an `OverlayWindow` XAML surface or equivalent WinUI overlay host under `src/Lumiere.Overlay/`.
  - [x] Make the base layer a full-surface `SwapChainPanel`; it must not be wrapped in a card, styled image, `BitmapImage`, `SoftwareBitmap`, GDI, WIC, CPU-readback, SDR, or 8-bit preview path.
  - [x] Add a XAML overlay layer above the preview for status, future crop canvas, and compact controls; this story may create the empty canvas/control layer but must not implement crop drag/adjust semantics.
  - [x] Keep preview bounds stable when status or controls appear; overlay UI must not resize or shift the `SwapChainPanel`.

- [x] Introduce a narrow overlay presentation/orchestration API instead of letting app shell code own overlay internals. (AC: 1, 3)
  - [x] Keep `Lumiere.App` responsible for startup/composition only; it may create/show the overlay and wire existing capture services, but overlay layout/state belongs in `Lumiere.Overlay`.
  - [x] Reuse existing `ISwapChainPreviewSurface` / `SwapChainPanelPreviewSurface` for attaching the FP16 scRGB swap chain to the overlay `SwapChainPanel`.
  - [x] Do not let `OverlayWindow` create D3D11 devices, WGC sessions, DXGI swap chains, frame pools, or frame textures directly.
  - [x] Preserve the existing `MainWindow` capture lifecycle safeguards if refactoring orchestration: `previewGeneration`, stale callback checks, frame-size recreate handling, and stop-before-restart behavior.

- [x] Add overlay state and failure handling that cannot strand a fullscreen/topmost window. (AC: 3)
  - [x] Model overlay-visible states using the existing readiness/session vocabulary: `Initializing`, `HDR-ready`, `Degraded preview`, `Unsupported capture`, `Preview failed`, and closing/disposed.
  - [x] If overlay creation, presenter setup, `SetSwapChain`, or capture/preview initialization fails, show an actionable failure state or close safely after teardown.
  - [x] Ensure close/failed paths call existing preview stop/teardown logic so WGC session, frame pool, swap chain, and device-bound presentation resources are not left running.
  - [x] Provide an Escape/cancel path where practical even if later confirm/cancel workflow details are owned by Story 3.4/3.5.

- [x] Configure fullscreen/borderless behavior through WinUI/Windows App SDK windowing APIs with interop isolated. (AC: 1, 3)
  - [x] Prefer `AppWindow`/presenter APIs available in Windows App SDK for fullscreen or borderless presentation; keep HWND/AppWindow lookup or Win32 style code behind overlay/windowing infrastructure.
  - [x] If `OverlappedPresenter` is used for MVP borderless/topmost behavior, set border/title-bar and topmost behavior deliberately and document why it is used instead of `AppWindowPresenterKind.FullScreen`.
  - [x] Do not enable whole-window click-through as part of this story; later crop hit-testing must remain possible.
  - [x] Keep any `SetWindowLong`, layered/transparent styles, or hit-test code in `src/Lumiere.Overlay/Windowing/` or `src/Lumiere.Infrastructure/Interop/`, not in app shell code.

- [x] Add focused tests for overlay state/layout seams that are hardware-independent. (AC: 1, 2, 3)
  - [x] Add or prepare `tests/Lumiere.Overlay.Tests/` if needed for pure state/layout models such as overlay state transitions, preview bounds stability, and failure close decisions.
  - [x] Keep automated tests free of real WinUI/WGC/DXGI/D3D11 hardware requirements unless the existing test project can run them on Windows CI.
  - [x] Preserve existing graphics/capture tests, especially HDR constants, swap-chain readiness, detach-before-release, capture lifecycle, and frame-size recreation tests.
  - [x] Document manual Windows validation for fullscreen overlay open/close, layering, Escape/cancel, failure recovery, HDR/SDR display behavior, and Windows scaling.

### Review Findings

- [x] [Review][Patch] Overlay is maximized on the default window instead of being placed for the selected capture target [src/Lumiere.Overlay/Windowing/OverlayWindowPresenter.cs:16]
- [x] [Review][Patch] Swap-chain disposal can fall back to the capture callback thread when UI dispatch fails during preview recreation [src/Lumiere.App/MainWindow.xaml.cs:294]
- [x] [Review][Patch] Preview failure teardown decision is modeled but not enforced [src/Lumiere.App/MainWindow.xaml.cs:447]
- [x] [Review][Patch] Failure, degraded, and unsupported overlay states are not visually distinct from HDR-ready [src/Lumiere.Overlay/OverlayWindow.xaml:24]
- [x] [Review][Patch] OverlayBoundary still describes the overlay boundary as future-only after OverlayWindow was implemented [src/Lumiere.Overlay/OverlayBoundary.cs:3]

## Dev Notes

### Story Scope

Story 3.1 starts Epic 3 by creating the fullscreen overlay shell above the proven HDR preview path. The expected output is a usable overlay host that can show the existing live preview as its base layer and stable XAML controls above it.

This story does not implement crop rectangle creation, handle adjustment, confirm/cancel output semantics, overlay hit-test edge cases, advanced diagnostics controls, settings, export, clipboard, global hotkeys, tray workflow, annotation, or capture history. It may create placeholder/state seams that later Epic 3 stories extend.

### Current Repository Context

Relevant current implementation:

- `src/Lumiere.Overlay/OverlayBoundary.cs` is only a marker type. This story should replace or supplement it with actual overlay window/view/state types.
- `src/Lumiere.App/MainWindow.xaml` currently hosts `PreviewSwapChainPanel` and a status card directly in the app shell. Moving preview display into an overlay must preserve the working capture/preview path.
- `src/Lumiere.App/MainWindow.xaml.cs` currently owns target selection, graphics service creation, preview swap-chain creation, capture start, stale-generation guarding, frame-size recreate, teardown, and status labels.
- `src/Lumiere.Infrastructure/Interop/SwapChainPanelPreviewSurface.cs` already adapts a WinUI `SwapChainPanel` to `ISwapChainPreviewSurface`; reuse it for the overlay panel rather than inventing a second attach abstraction.
- `src/Lumiere.Graphics/Presentation/GraphicsEngine.cs` creates preview swap-chain resources through `SwapChainManager`; overlay code should consume this through composition/orchestration, not duplicate graphics creation.
- `src/Lumiere.Capture/CaptureSessionState.cs` is the current status contract for user-facing readiness and technical detail.

Likely updated files:

```text
src/Lumiere.Overlay/Lumiere.Overlay.csproj
src/Lumiere.Overlay/OverlayBoundary.cs
src/Lumiere.Overlay/OverlayWindow.xaml
src/Lumiere.Overlay/OverlayWindow.xaml.cs
src/Lumiere.Overlay/OverlayState.cs
src/Lumiere.Overlay/Windowing/OverlayWindowPresenter.cs
src/Lumiere.App/MainWindow.xaml
src/Lumiere.App/MainWindow.xaml.cs
```

Possible new files if they keep responsibilities narrow:

```text
src/Lumiere.Overlay/OverlayViewModel.cs
src/Lumiere.Overlay/OverlayPreviewHost.cs
src/Lumiere.Overlay/OverlayFailureAction.cs
tests/Lumiere.Overlay.Tests/OverlayStateTests.cs
tests/Lumiere.Overlay.Tests/OverlayPreviewLayoutTests.cs
docs/validation/overlay-validation.md
```

### Architecture Compliance

- `Lumiere.Overlay` owns overlay/window/crop-control UI behavior.
- `Lumiere.App` wires startup and composition only.
- `Lumiere.Capture` owns WGC target/session/frame lifecycle.
- `Lumiere.Graphics` owns D3D11/DXGI rendering, swap-chain creation, FP16/scRGB constants, and presentation resources.
- `Lumiere.Infrastructure` owns interop, diagnostics/result primitives, and UI-thread helpers.
- The primary preview path must preserve FP16/scRGB: WGC `DirectXPixelFormat.R16G16B16A16Float`, swap-chain `DXGI_FORMAT_R16G16B16A16_FLOAT`, color space `DXGI_COLOR_SPACE_RGB_FULL_G10_NONE_P709`, and `ISwapChainPanelNative.SetSwapChain`.
- `SetSwapChain` and all WinUI state mutation must run on the UI thread.
- Teardown must detach the swap chain before releasing device-bound resources.

### UX Requirements

Use the UX specification as an implementation input:

- The full-screen overlay opens after target selection and contains the preview plus XAML overlay layer.
- The `SwapChainPanel` fills the preview surface as the base layer.
- Status and controls appear above the preview and remain stable as state changes.
- Plain labels must be used: `Initializing preview`, `HDR-ready`, `Degraded preview`, `Unsupported capture`, `Preview failed`.
- Status must not rely on color alone, and degraded/unsupported/failed states must be visually distinct from HDR-ready.
- Avoid decorative chrome, heavy panels, editor-like complexity, and long explanatory paragraphs during capture.
- Advanced diagnostics should remain a disclosure, not an always-visible rail; broader diagnostics are Epic 4.
- Overlay UI must avoid trapping the user in fullscreen; Escape/cancel should be available where practical.
- Preview bounds and crop coordinate mapping must not change when status, warnings, toolbar, or diagnostics appear.

### Fullscreen and Windowing Guidance

Microsoft Learn documents `AppWindow` as the Windows App SDK abstraction for top-level HWND management in WinUI 3. It also documents `OverlappedPresenter` properties such as `HasBorder`, `HasTitleBar`, and `IsAlwaysOnTop`, plus `SetBorderAndTitleBar(bool, bool)`. `AppWindowPresenterKind.FullScreen` exists for full-screen presenter behavior, while the default presenter is overlapped.

Implementation guidance:

- Use `Window.AppWindow` where available on the current Windows App SDK version, or isolate `WindowNative.GetWindowHandle` / `Win32Interop.GetWindowIdFromWindow` fallback in a small windowing service.
- Choose fullscreen presenter or borderless/topmost overlapped presenter deliberately based on MVP overlay needs and document the tradeoff in code or dev notes.
- Keep transparent or click-through Win32 styles out of this story unless absolutely required for showing the window; Story 3.5 owns hit testing.

References:

- Microsoft Learn: https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/windowing/windowing-overview
- Microsoft Learn: https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.windowing.overlappedpresenter
- Microsoft Learn: https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.windowing.appwindowpresenterkind

### Previous Story Intelligence

Story 2.4 established these patterns:

- Do not rewrite the capture pipeline for validation or UI work.
- Preserve `previewGeneration` as the stale-callback defense after picker awaits, free-threaded frame callbacks, readiness callbacks, diagnostics callbacks, and queued resize/recreate work.
- Ordinary stop/restart must not dispose shared `GraphicsDeviceResources`.
- Capture teardown order remains unsubscribe `FrameArrived`, dispose/stop `GraphicsCaptureSession`, dispose `Direct3D11CaptureFramePool`, dispose WinRT `IDirect3DDevice`.
- Preview teardown order remains `SetSwapChain(null)` through the preview surface before releasing DXGI swap-chain resources.
- Real GPU memory stability and real WGC/DXGI/D3D11/HDR behavior require Windows hardware/manual validation.

Story 2.3 established:

- Disposal should happen outside `previewSync`; do not hold locks while performing UI-thread detach or COM disposal.
- Frame-size mismatch must not present mismatched frames and must not report HDR-ready while rebuilding.
- `CaptureFrameSizeChange` and `CapturePreviewRecreationRequest` are existing hardware-independent seams for resize/recreate behavior.

Story 2.2 established:

- `CaptureSessionState` is the state contract; do not introduce a parallel readiness vocabulary.
- Picker cancellation is normal and should not become a failure state.
- Degraded/failed presentation evidence must not be overwritten by generic capture initialization text.

### Git Intelligence

Recent commits show the implementation pattern to follow:

- `ed589a7 feat: implement stop, restart, and recreate capture resources` added lifecycle/recreate behavior that overlay work must preserve.
- `3a964fb Record capture session state review fixes` hardened the current session state model.
- `9ffea82 feat: complete implementation of target selection for display or window capture` introduced target selection service/result patterns; keep picker cancellation separate from capture failure.
- `2f0e953 feat: implement minimal WGC FP16 capture to live preview` introduced the GPU-resident FP16 preview path; do not weaken it with bitmap shortcuts for overlay display.

### Latest Technical Information

- Repository package versions are centrally locked: `Microsoft.WindowsAppSDK` `1.8.260317003`, `Vortice.Direct3D11` `3.8.3`, `Vortice.DXGI` `3.8.3`, `Microsoft.NET.Test.Sdk` `18.4.0`, xUnit `2.9.3`, and xUnit runner `3.1.5`.
- NuGet currently shows newer `Microsoft.WindowsAppSDK` versions, including `1.8.260416003` and `2.0.1`, after the architecture document was written. Do not upgrade in this story unless a concrete blocker requires it; if a blocker is found, document the reason and rerun Windows validation.
- NuGet currently shows `Vortice.Direct3D11` `3.8.3` as the latest listed stable version and compatible with `net10.0`.
- Treat package upgrades as separate dependency-management work because overlay behavior depends on WinUI/windowing runtime details and must be validated on Windows.

References:

- NuGet: https://www.nuget.org/packages/Microsoft.WindowsAppSDK/
- NuGet: https://www.nuget.org/packages/Vortice.Direct3D11/
- `_bmad-output/planning-artifacts/architecture.md#Starter-Template-Evaluation`
- `Directory.Packages.props`

### Testing Requirements

Run from repository root on Windows:

```bash
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false
dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet format Lumiere.sln --verify-no-changes --verbosity minimal
```

Automated tests should cover pure overlay state, failure-close decisions, preview bounds/layout calculations if extracted into testable models, and preservation of existing capture/graphics lifecycle behavior. Real WinUI overlay rendering, AppWindow presenter behavior, WGC, DXGI, D3D11, HDR display fidelity, topmost/fullscreen behavior, multi-monitor placement, Windows scaling, and keyboard focus require Windows manual validation. Completion notes must label validation as `Mac-pass`, `Windows CI-pass`, or `Windows manual-pass` accurately.

### Anti-Patterns to Avoid

- Do not create a second capture service or graphics engine for overlay display.
- Do not let overlay code own WGC, D3D11, DXGI, swap-chain, frame-pool, or frame texture lifetime.
- Do not replace `SwapChainPanel` with a XAML image, software bitmap, WPF/WinForms/GDI surface, web UI, or SDR screenshot-library preview.
- Do not use CPU readback, PNG bytes, or 8-bit textures to make the overlay easier to render.
- Do not update WinUI directly from `FrameArrived`.
- Do not store native frame/swap-chain objects in overlay state or diagnostics.
- Do not make the whole overlay click-through in a way that will block later crop interaction.
- Do not implement crop drag/adjust/confirm/export semantics in this story.
- Do not claim fullscreen/topmost/HDR behavior complete without Windows manual validation.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-04: `dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false` passed.
- 2026-05-04: `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false` passed with 0 warnings and 0 errors.
- 2026-05-04: `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false` passed: 97 tests.
- 2026-05-04: `dotnet test tests/Lumiere.Overlay.Tests/Lumiere.Overlay.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false` passed: 9 tests.
- 2026-05-04: `dotnet format Lumiere.sln --verify-no-changes --verbosity minimal` passed.

### Completion Notes List

- Implemented a WinUI `OverlayWindow` inside `Lumiere.Overlay` with a full-surface `SwapChainPanel` base layer, XAML crop/status/control layer above it, and stable fill-surface preview layout.
- Added overlay state modeling that maps existing `CaptureSessionState` values to the required plain labels: `Initializing preview`, `HDR-ready`, `Degraded preview`, `Unsupported capture`, `Preview failed`, and disposed/closing states.
- Refactored `MainWindow` orchestration so it opens the overlay, reuses the overlay-provided `ISwapChainPreviewSurface`, and preserves existing `previewGeneration`, stale callback, frame-size recreate, and stop-before-restart safeguards.
- Added Cancel/Escape teardown paths that call existing preview stop logic before closing the overlay so capture/session/swap-chain resources are not left running.
- Configured borderless topmost MVP presentation through isolated `OverlayWindowPresenter` using `OverlappedPresenter`, with maximization instead of whole-window click-through or direct app-shell Win32 style code.
- Added hardware-independent overlay tests for state mapping, failure teardown decisions, and full-surface preview layout.
- Added manual Windows validation guidance for fullscreen/topmost overlay behavior, layering, Escape/cancel, failure recovery, HDR/SDR display behavior, and scaling.
- Validation level: Windows CI-pass for restore/build/tests/format on this machine. Windows manual-pass for real topmost/fullscreen, WGC/DXGI/D3D11/HDR display behavior is still required.

### File List

- Lumiere.sln
- src/Lumiere.App/MainWindow.xaml
- src/Lumiere.App/MainWindow.xaml.cs
- src/Lumiere.Capture/Properties/AssemblyInfo.cs
- src/Lumiere.Overlay/Lumiere.Overlay.csproj
- src/Lumiere.Overlay/OverlayDisplayStatus.cs
- src/Lumiere.Overlay/OverlayFailureAction.cs
- src/Lumiere.Overlay/OverlayPreviewLayout.cs
- src/Lumiere.Overlay/OverlayState.cs
- src/Lumiere.Overlay/OverlayStatusStyle.cs
- src/Lumiere.Overlay/OverlayWindow.xaml
- src/Lumiere.Overlay/OverlayWindow.xaml.cs
- src/Lumiere.Overlay/Windowing/OverlayPlacementRequest.cs
- src/Lumiere.Overlay/Windowing/OverlayWindowPresenter.cs
- tests/Lumiere.Overlay.Tests/Lumiere.Overlay.Tests.csproj
- tests/Lumiere.Overlay.Tests/OverlayPlacementRequestTests.cs
- tests/Lumiere.Overlay.Tests/OverlayPreviewLayoutTests.cs
- tests/Lumiere.Overlay.Tests/OverlayStateTests.cs
- docs/validation/overlay-validation.md

### Change Log

- 2026-05-04: Created Story 3.1 context and marked ready for development.
- 2026-05-04: Implemented fullscreen overlay host above the HDR preview path and marked ready for review.
- 2026-05-04: Addressed code review findings and marked story done.
