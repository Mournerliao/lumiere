# Story 3.2: Create a Crop Selection by Dragging

Status: done

<!-- Ultimate context engine analysis completed - comprehensive developer guide created. -->

## Story

As a screenshot user,
I want to drag over the preview to create a crop selection,
so that I can choose the exact region I care about.

## Acceptance Criteria

1. Given the overlay is in selection mode, when the user presses and drags over the preview, then a crop rectangle is created from the drag start and current pointer position.
2. Given the crop rectangle is active, when the user continues dragging, then the active region and non-selected area remain visually clear.
3. Given drag coordinates leave expected bounds, when crop geometry is computed, then the crop is clamped to the preview area.

## Tasks / Subtasks

- [x] Add a crop geometry model and controller inside `Lumiere.Overlay`. (AC: 1, 3)
  - [x] Create focused types under `src/Lumiere.Overlay/Crop/` such as `CropGeometry`, `CropController`, and any small drag-state/value objects needed.
  - [x] Store crop geometry in device-independent pixels relative to the overlay preview bounds, not relative to WGC frame objects or native textures.
  - [x] Support empty, creating, and active crop states; handle negative drags by normalizing rectangle origin/size.
  - [x] Clamp all computed geometry to `OverlayPreviewLayout.PreviewBounds` and reject or mark invalid any zero-size/too-small crop.

- [x] Wire pointer drag creation through the existing overlay XAML layer. (AC: 1, 2)
  - [x] Make `CropCanvas` hit-testable for selection mode and attach `PointerPressed`, `PointerMoved`, `PointerReleased`, `PointerCanceled`, and `PointerCaptureLost` handlers.
  - [x] Call `CapturePointer` from `PointerPressed`; finish or cancel the creating state on release, cancel, or capture loss.
  - [x] Keep pointer handling in `OverlayWindow` thin; delegate geometry/state transitions to the crop controller.
  - [x] Do not route crop pointer events through `SwapChainPanel`; the XAML crop layer owns interaction above the hardware preview.

- [x] Render immediate crop visuals above the HDR preview. (AC: 2)
  - [x] Draw or update a selected-region rectangle and non-selected mask on `CropCanvas`.
  - [x] Ensure the crop boundary remains visible over bright, dark, and high-contrast HDR content.
  - [x] Keep status/cancel controls available while dragging and avoid shifting or resizing the `SwapChainPanel`.
  - [x] This story may show a simple active rectangle/mask; resize handles and adjustment semantics belong to Story 3.3.

- [x] Preserve existing capture, graphics, and overlay lifecycle behavior. (AC: 1, 2, 3)
  - [x] Reuse the overlay window and `ISwapChainPreviewSurface` introduced by Story 3.1.
  - [x] Do not create new WGC sessions, D3D11 devices, DXGI swap chains, frame pools, render targets, or frame textures from crop code.
  - [x] Preserve `MainWindow` safeguards: `previewGeneration`, stale callback checks, frame-size recreation, stop-before-restart, Escape/Cancel teardown, and `SetSwapChain(null)` detach before swap-chain disposal.
  - [x] Disable crop creation only when the overlay/preview state is unsupported, failed, closing, or disposed; degraded preview may still allow crop if the UI clearly preserves the degraded status.

- [x] Add hardware-independent tests for crop creation. (AC: 1, 2, 3)
  - [x] Add or extend `tests/Lumiere.Overlay.Tests/` for drag start/move/release transitions, negative drag normalization, bounds clamping, zero-size or minimum-size invalid crops, and preview-bounds stability.
  - [x] Keep tests free of real WinUI/WGC/DXGI/D3D11 requirements; test controller and geometry models directly.
  - [x] Preserve existing overlay state/layout tests and existing graphics/capture lifecycle tests.
  - [x] Document required manual Windows validation for pointer drag over live preview, visual mask contrast, high-DPI scaling, Escape/cancel during drag, HDR/SDR displays, and multi-monitor placement.

### Review Findings

- [x] [Review][Patch] Clear or hide active crop visuals when crop selection becomes unavailable [`src/Lumiere.Overlay/OverlayWindow.xaml.cs`:156]
- [x] [Review][Patch] Position the overlay from the captured target rather than the new overlay window's nearest display [`src/Lumiere.Overlay/Windowing/OverlayWindowPresenter.cs`:17]

## Dev Notes

### Story Scope

Story 3.2 adds the first real crop interaction on top of the fullscreen overlay from Story 3.1. The expected output is a pointer-driven crop rectangle that appears immediately while dragging, shows the selected and non-selected regions clearly, and clamps to the current preview area.

This story does not implement crop resizing handles, edge/handle adjustment, replacing an existing crop by interaction rule, confirm output semantics, export, clipboard, annotation, hotkeys, tray workflow, settings, advanced diagnostics, or new capture/graphics lifecycle behavior. Story 3.3 owns adjustment/recreation; Story 3.4 owns confirm/cancel workflow semantics; Story 3.5 owns broader hit-testing and keyboard escape hardening.

### Current Repository Context

Relevant current implementation:

- `src/Lumiere.Overlay/OverlayWindow.xaml` already contains a full-surface `PreviewSwapChainPanel`, an empty `CropCanvas` above it, and a status/control layer above the canvas.
- `src/Lumiere.Overlay/OverlayWindow.xaml.cs` already exposes `PreviewSurface`, focuses `RootGrid`, handles Escape/Cancel through `CloseRequested`, applies `OverlayState`, and sizes the preview through `OverlayPreviewLayout.FillSurface`.
- `src/Lumiere.Overlay/OverlayPreviewLayout.cs` currently models fill-surface preview bounds and should be reused as the crop bounds source.
- `src/Lumiere.Overlay/OverlayState.cs` maps `CaptureSessionState` into `Initializing preview`, `HDR-ready`, `Degraded preview`, `Unsupported capture`, `Preview failed`, closing, and disposed states.
- `src/Lumiere.App/MainWindow.xaml.cs` owns target selection, capture startup, FP16 scRGB preview creation, frame callbacks, frame-size recreation, overlay open/close, and teardown.
- `tests/Lumiere.Overlay.Tests/` already has pure tests for overlay state mapping, placement request, and preview fill-surface layout.

Likely changed or new files:

```text
src/Lumiere.Overlay/OverlayWindow.xaml
src/Lumiere.Overlay/OverlayWindow.xaml.cs
src/Lumiere.Overlay/Crop/CropController.cs
src/Lumiere.Overlay/Crop/CropGeometry.cs
src/Lumiere.Overlay/Crop/CropSelectionState.cs
tests/Lumiere.Overlay.Tests/CropControllerTests.cs
tests/Lumiere.Overlay.Tests/CropGeometryTests.cs
docs/validation/overlay-validation.md
```

Avoid changing `Lumiere.Capture`, `Lumiere.Graphics`, or native interop unless a compile-time integration seam truly requires it.

### Architecture Compliance

- `Lumiere.Overlay` owns crop UI behavior, pointer state, crop geometry, mask visuals, and overlay commands.
- `Lumiere.App` may wire overlay lifecycle and receive future confirmed crop outputs, but it must not own crop geometry internals.
- `Lumiere.Capture` remains the owner of WGC target/session/frame lifecycle.
- `Lumiere.Graphics` remains the owner of D3D11/DXGI rendering, FP16/scRGB constants, swap-chain resources, and presentation.
- `Lumiere.Infrastructure` remains the owner of native interop, diagnostics/result primitives, and UI-thread helpers.
- The primary preview path remains GPU-resident: no `BitmapImage`, `SoftwareBitmap`, GDI, WIC, CPU readback, 8-bit texture, SDR fallback, web UI, WPF, WinForms, Electron, or Tauri path for live preview.
- WinUI state changes and `SwapChainPanel` operations must remain on the UI thread.

### Crop Interaction Requirements

- Pointer down starts a new creation gesture in selection mode.
- Pointer move updates crop geometry immediately while the pointer is captured.
- Pointer release commits an active crop if geometry is valid; otherwise return to empty state.
- Pointer cancel or capture loss must leave a coherent empty or previous active state, not a stuck creating state.
- Crop geometry must normalize negative drags and clamp to preview bounds.
- The selected region and non-selected mask must remain legible over bright and dark content.
- The preview surface size and coordinate mapping must not change when status, warnings, toolbar, mask, or crop rectangle appear.

Microsoft Learn pointer guidance notes that `CapturePointer` is called from `PointerPressed`, and `PointerPressed` / `PointerReleased` should not be assumed to occur in pairs; handle `PointerCanceled` and `PointerCaptureLost` as gesture-ending paths.

### UX Requirements

Use the UX specification as an implementation input:

- The user should be able to drag immediately over the preview without layout shifts or UI lag.
- Crop creation must use the familiar Windows screenshot pattern: press, drag, release.
- The crop boundary must remain visible over bright, dark, and high-contrast HDR content.
- The non-selected mask should clarify what is outside the crop without hiding context.
- Status should remain visible and must not rely on color alone.
- Cancel and Escape remain available where practical during the fullscreen overlay flow.
- Confirm may remain absent or disabled until Story 3.4 defines confirm behavior; do not imply export/clipboard output exists.
- Avoid editor-like complexity, annotations, history, or advanced settings in the crop moment.

### Previous Story Intelligence

Story 3.1 established these implementation patterns:

- Reuse the existing overlay `SwapChainPanel` and `SwapChainPanelPreviewSurface`; do not invent a second preview attach abstraction.
- `OverlayWindow` is the XAML surface; `MainWindow` orchestrates capture/preview lifecycle.
- The overlay already has failure teardown decisions, distinct status styles, and Escape/Cancel teardown.
- Whole-window click-through is intentionally not enabled because later crop selection must receive input.
- Real topmost/fullscreen, WGC/DXGI/D3D11/HDR display behavior still requires Windows manual validation.

Earlier lifecycle stories established:

- Preserve `previewGeneration` as the stale-callback defense after picker awaits, frame callbacks, readiness callbacks, diagnostics callbacks, and queued resize/recreate work.
- Stop/restart should not dispose shared `GraphicsDeviceResources` during ordinary session teardown.
- Capture teardown order remains unsubscribe `FrameArrived`, dispose/stop `GraphicsCaptureSession`, dispose `Direct3D11CaptureFramePool`, dispose WinRT `IDirect3DDevice`.
- Preview teardown order remains `SetSwapChain(null)` through the preview surface before releasing DXGI swap-chain resources.
- Disposal should happen outside `previewSync`; do not hold locks while performing UI-thread detach or COM disposal.

### Git Intelligence

Recent commits show the current implementation lane:

- `ed589a7 feat: implement stop, restart, and recreate capture resources` added lifecycle/recreate behavior that crop work must preserve.
- `3a964fb Record capture session state review fixes` hardened session state vocabulary and status preservation.
- `9ffea82 feat: complete implementation of target selection for display or window capture` introduced target selection service/result patterns; crop work should not reinterpret picker cancellation.
- `2f0e953 feat: implement minimal WGC FP16 capture to live preview` introduced the GPU-resident FP16 preview path; crop visuals must stay in the XAML overlay layer above it.

### Latest Technical Information

- Repository package versions are centrally locked in `Directory.Packages.props`: `Microsoft.WindowsAppSDK` `1.8.260317003`, `Vortice.Direct3D11` `3.8.3`, `Vortice.DXGI` `3.8.3`, `Microsoft.NET.Test.Sdk` `18.4.0`, xUnit `2.9.3`, and xUnit runner `3.1.5`.
- NuGet currently lists `Microsoft.WindowsAppSDK` `1.8.260317003` as the latest stable 1.8 package and shows 2.0 packages as preview/experimental. Do not upgrade Windows App SDK in this story unless a concrete blocker requires it.
- NuGet currently lists `Vortice.Direct3D11` `3.8.3` as the latest stable package and compatible with `net10.0`.
- Crop interaction should use WinUI pointer events and XAML shapes/brushes; no new graphics library is needed.

References:

- NuGet: https://www.nuget.org/packages/Microsoft.WindowsAppSdk/
- NuGet: https://www.nuget.org/packages/Vortice.Direct3D11/
- Microsoft Learn: https://learn.microsoft.com/en-us/windows/apps/design/input/handle-pointer-input
- `_bmad-output/planning-artifacts/architecture.md#Starter-Template-Evaluation`
- `Directory.Packages.props`

### Testing Requirements

Run from repository root on Windows:

```bash
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false
dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Overlay.Tests/Lumiere.Overlay.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet format Lumiere.sln --verify-no-changes --verbosity minimal
```

Automated tests should focus on crop geometry/controller behavior, preview bounds stability, and preservation of existing overlay state behavior. Real WinUI pointer routing, fullscreen/topmost overlay behavior, WGC, DXGI, D3D11, HDR fidelity, multi-monitor placement, and Windows scaling require Windows manual validation. Completion notes must label validation as `Mac-pass`, `Windows CI-pass`, or `Windows manual-pass` accurately.

### Anti-Patterns to Avoid

- Do not create a second capture service, graphics engine, preview surface abstraction, or swap-chain path for crop selection.
- Do not let crop code own WGC, D3D11, DXGI, swap-chain, frame-pool, frame texture, or render-target lifetime.
- Do not use bitmap screenshots, CPU readback, PNG bytes, `SoftwareBitmap`, `BitmapImage`, GDI, WPF/WinForms, web UI, or SDR textures for the live preview.
- Do not update WinUI from capture/frame callbacks.
- Do not store native frames, swap-chain resources, or D3D textures in crop state.
- Do not make the whole overlay click-through.
- Do not implement resize handles, final confirm output, export, clipboard, annotations, diagnostics settings, hotkeys, tray workflow, or capture history in this story.
- Do not claim HDR/topmost/fullscreen/manual pointer behavior fully complete without Windows manual validation.

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- 2026-05-04: `dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false` passed on Windows.
- 2026-05-04: `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false` passed on Windows.
- 2026-05-04: `dotnet test tests/Lumiere.Overlay.Tests/Lumiere.Overlay.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false` passed: 19 tests.
- 2026-05-04: `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false` passed: 97 tests.
- 2026-05-04: `dotnet format Lumiere.sln --verify-no-changes --verbosity minimal` passed after CRLF line-ending normalization in existing App/Overlay files.

### Completion Notes List

- Implemented `Lumiere.Overlay.Crop` geometry/controller types for DIP-based crop creation, negative drag normalization, preview-bounds clamping, minimum-size rejection, and empty/creating/active state transitions.
- Wired `CropCanvas` pointer press/move/release/cancel/capture-lost events to the controller while keeping the `SwapChainPanel` preview path untouched.
- Added XAML mask and dual-stroke crop boundary visuals above the HDR preview so the selected region remains legible without shifting the preview or status/cancel controls.
- Preserved capture and graphics ownership boundaries; crop code creates no WGC, D3D11, DXGI, swap-chain, frame-pool, render-target, or frame texture resources.
- Added hardware-independent crop tests and documented manual Windows validation for live pointer drag, visual contrast, high-DPI scaling, Escape/cancel during drag, HDR/SDR displays, and multi-monitor placement.
- Validation level: Windows CI-pass for restore/build/automated tests/format. Windows manual-pass was not run; live WinUI pointer routing, HDR display behavior, high-DPI, and multi-monitor placement still require manual validation.

### File List

- docs/validation/overlay-validation.md
- src/Lumiere.App/MainWindow.xaml.cs
- src/Lumiere.Overlay/Crop/CropController.cs
- src/Lumiere.Overlay/Crop/CropGeometry.cs
- src/Lumiere.Overlay/Crop/CropSelection.cs
- src/Lumiere.Overlay/Crop/CropSelectionPhase.cs
- src/Lumiere.Overlay/OverlayBoundary.cs
- src/Lumiere.Overlay/OverlayStatusStyle.cs
- src/Lumiere.Overlay/OverlayWindow.xaml
- src/Lumiere.Overlay/OverlayWindow.xaml.cs
- src/Lumiere.Overlay/Windowing/OverlayPlacementRequest.cs
- src/Lumiere.Overlay/Windowing/OverlayWindowPresenter.cs
- tests/Lumiere.Overlay.Tests/CropControllerTests.cs
- tests/Lumiere.Overlay.Tests/CropGeometryTests.cs
- tests/Lumiere.Overlay.Tests/OverlayPlacementRequestTests.cs
- tests/Lumiere.Overlay.Tests/OverlayStateTests.cs

### Change Log

- 2026-05-04: Created Story 3.2 context and marked ready for development.
- 2026-05-04: Implemented crop drag creation, crop visuals, tests, and validation notes; marked ready for review.
