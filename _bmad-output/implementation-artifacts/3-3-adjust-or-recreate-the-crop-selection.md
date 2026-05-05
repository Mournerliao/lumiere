# Story 3.3: Adjust or Recreate the Crop Selection

Status: done

<!-- Ultimate context engine analysis completed - comprehensive developer guide created. -->

## Story

As a screenshot user,
I want to adjust or recreate my crop selection before confirming,
so that small selection mistakes do not force me to restart capture.

## Acceptance Criteria

1. Given a crop selection exists, when the user drags a handle or edge, then the crop rectangle updates without shifting the preview layout.
2. Given a crop selection exists, when the user starts a new selection gesture according to the interaction rules, then the previous crop is replaced by the new crop.
3. Given the crop changes, when coordinates are mapped, then device-independent UI coordinates can be converted consistently for capture/rendering use.

## Tasks / Subtasks

- [x] Extend the existing crop model/controller for adjustment gestures. (AC: 1, 2)
  - [x] Reuse `src/Lumiere.Overlay/Crop/CropController.cs`, `CropGeometry.cs`, `CropSelection.cs`, and `CropSelectionPhase.cs`; do not create a parallel crop state machine.
  - [x] Add explicit adjustment phases or gesture types for edge/handle drag behavior while preserving existing empty, creating, active, cancel, and clear behavior.
  - [x] Add hit-test logic for corners and edges with stable DIP hit areas that remain usable at common Windows scaling values.
  - [x] Keep all adjusted geometry normalized, clamped to `OverlayPreviewLayout.PreviewBounds`, and subject to the existing minimum-size rule.
  - [x] Preserve Story 3.2 behavior where cancel/capture loss during creation restores the previous active crop.

- [x] Render adjustment affordances above the existing crop visuals. (AC: 1)
  - [x] Add edge/corner handle visuals to `src/Lumiere.Overlay/OverlayWindow.xaml` or focused overlay crop visual types if the code needs separation.
  - [x] Keep the current non-selected mask and dual-stroke crop boundary visible over bright, dark, and high-contrast HDR content.
  - [x] Avoid moving or resizing `PreviewSwapChainPanel`, `CropCanvas`, status controls, or cancel controls when handles appear, disappear, or move.
  - [x] Do not make the full overlay click-through; the crop layer must continue to receive pointer input above the `SwapChainPanel`.

- [x] Wire pointer interactions for handle/edge adjustment and crop recreation. (AC: 1, 2)
  - [x] Detect whether `PointerPressed` starts handle adjustment, edge adjustment, or a new crop recreation gesture.
  - [x] Capture the pointer from `PointerPressed` and conclude the gesture through `PointerReleased`, `PointerCanceled`, and `PointerCaptureLost`.
  - [x] Define and implement the recreation rule explicitly: pointer down outside the active crop starts a new selection and replaces the previous crop only when the new gesture commits a valid crop.
  - [x] Keep pointer handling in `OverlayWindow` thin; delegate geometry transitions and hit-test decisions to crop-domain types.
  - [x] Continue allowing crop interaction in `HdrReady` and `DegradedPreview` states, but clear/disable it in unsupported, failed, closing, or disposed states.

- [x] Add a single coordinate mapping model for crop output geometry. (AC: 3)
  - [x] Add a small overlay-owned mapping type under `src/Lumiere.Overlay/Crop/`, such as `CropCoordinateMapper`, unless an equivalent already exists.
  - [x] Convert crop rectangles from device-independent preview coordinates to target/capture pixel coordinates using the current preview bounds and capture frame size.
  - [x] Keep the mapping pure and hardware-independent; it must not inspect WGC frames, D3D textures, swap chains, or monitor HDR state.
  - [x] Handle non-zero preview origins, different preview/capture aspect ratios if represented by future layout data, and rounding/clamping at pixel boundaries.
  - [x] Return typed values suitable for later confirm/output stories without implementing export, clipboard, or final capture output.

- [x] Preserve capture, graphics, overlay lifecycle, and HDR preview boundaries. (AC: 1, 2, 3)
  - [x] Do not change FP16/scRGB constants, WGC frame pool format, DXGI swap-chain format, color space, or swap-chain attach/detach behavior.
  - [x] Do not create or dispose WGC sessions, frame pools, D3D11 devices, DXGI swap chains, frame textures, or render targets from crop code.
  - [x] Preserve `MainWindow` lifecycle safeguards: `previewGeneration`, stale callback checks, stop-before-restart, frame-size recreation, Escape/Cancel teardown, and `SetSwapChain(null)` before swap-chain disposal.
  - [x] Keep WinUI state mutation on the UI thread and keep capture/frame callbacks away from crop UI state.

- [x] Add and update hardware-independent tests and manual validation notes. (AC: 1, 2, 3)
  - [x] Extend `tests/Lumiere.Overlay.Tests/` for corner/edge hit-testing, each resize direction, negative/inside-out adjustment normalization, bounds clamping, minimum-size rejection, cancel/capture-lost restoration, and outside-crop recreation semantics.
  - [x] Add coordinate mapper tests for DIP-to-pixel conversion, non-zero preview bounds, rounding, clamping, and mismatched preview/capture dimensions where supported.
  - [x] Preserve existing crop creation, overlay state, preview layout, graphics, and capture lifecycle tests.
  - [x] Update `docs/validation/overlay-validation.md` with Story 3.3 manual checks for resizing handles/edges, recreating the crop, high-DPI scaling, HDR/SDR displays, and multi-monitor placement.

### Review Findings

- [x] [Review][Patch] Existing overlay remains open and reusable while selecting a new target [`src/Lumiere.App/MainWindow.xaml.cs:53`]
- [x] [Review][Patch] Swap-chain resources are dropped if preview recreation cannot enqueue UI teardown [`src/Lumiere.App/MainWindow.xaml.cs:272`]
- [x] [Review][Patch] Crop coordinate mapping can round inward and lose selected edge pixels [`src/Lumiere.Overlay/Crop/CropCoordinateMapper.cs:30`]
- [x] [Review][Patch] Crop pointer gestures are not bound to a single primary pointer [`src/Lumiere.Overlay/OverlayWindow.xaml.cs:94`]
- [x] [Review][Patch] Pending outside-crop recreation replaces the public active selection before a valid commit [`src/Lumiere.Overlay/Crop/CropController.cs:101`]

## Dev Notes

### Story Scope

Story 3.3 makes the existing crop selection correctable. The output is still an in-app crop selection over the live HDR preview, not a saved screenshot. Users should be able to drag visible handles or edges to adjust an active crop, or start a new valid crop outside the active crop to replace it, without restarting capture.

This story does not implement final confirm behavior, capture output state, export, clipboard, annotation, hotkeys, tray workflow, advanced diagnostics, settings, or new capture/graphics lifecycle ownership. Story 3.4 owns confirm/cancel workflow semantics. Story 3.5 owns broader transparent/window hit-testing and keyboard escape hardening.

### Current Repository Context

Story 3.2 already introduced the crop foundation:

- `src/Lumiere.Overlay/Crop/CropController.cs` owns Begin/Update/Commit/Cancel/Clear and preserves a previous active crop while a new creation gesture is in progress.
- `src/Lumiere.Overlay/Crop/CropGeometry.cs` creates normalized, clamped DIP rectangles from drag start/current points and rejects crops smaller than `DefaultMinimumSize`.
- `src/Lumiere.Overlay/Crop/CropSelection.cs` exposes `Empty`, `Creating`, and `Active` selection phases plus `IsVisible`.
- `src/Lumiere.Overlay/OverlayWindow.xaml.cs` wires `CropCanvas` pointer events, captures/release pointers, updates crop visuals, and clears selection when overlay state becomes unsupported/failed/closing/disposed.
- `src/Lumiere.Overlay/OverlayWindow.xaml` has `PreviewSwapChainPanel` as the base preview layer, `CropCanvas` above it, mask rectangles, and dual black/white crop border rectangles.
- `src/Lumiere.Overlay/OverlayPreviewLayout.cs` currently models a fill-surface preview with `PreviewBounds = Rect(0, 0, width, height)`.
- `docs/validation/overlay-validation.md` already documents Story 3.1 and 3.2 manual overlay/crop checks.
- `tests/Lumiere.Overlay.Tests/CropControllerTests.cs` and `CropGeometryTests.cs` cover creation, clamping, too-small rejection, and creation cancel restoration.

Likely changed or new files:

```text
src/Lumiere.Overlay/Crop/CropController.cs
src/Lumiere.Overlay/Crop/CropGeometry.cs
src/Lumiere.Overlay/Crop/CropSelection.cs
src/Lumiere.Overlay/Crop/CropSelectionPhase.cs
src/Lumiere.Overlay/Crop/CropAdjustmentHandle.cs
src/Lumiere.Overlay/Crop/CropHitTestResult.cs
src/Lumiere.Overlay/Crop/CropCoordinateMapper.cs
src/Lumiere.Overlay/OverlayWindow.xaml
src/Lumiere.Overlay/OverlayWindow.xaml.cs
tests/Lumiere.Overlay.Tests/CropControllerTests.cs
tests/Lumiere.Overlay.Tests/CropGeometryTests.cs
tests/Lumiere.Overlay.Tests/CropCoordinateMapperTests.cs
docs/validation/overlay-validation.md
```

Avoid changing `Lumiere.Capture`, `Lumiere.Graphics`, `Lumiere.Infrastructure/Interop`, package versions, or project boundaries unless a compile-time integration seam truly requires it.

### Interaction Rules

- Dragging an active crop's corner handle adjusts both adjacent edges.
- Dragging an active crop's edge adjusts only that edge.
- If an adjustment crosses the opposite edge, the geometry must normalize without producing negative width/height or jumping the preview layout.
- If adjusted geometry becomes smaller than the minimum valid crop, the controller should either keep the last valid active crop or mark the in-progress adjustment invalid while leaving the committed crop recoverable. Pick one behavior and test it explicitly.
- Pointer down outside the active crop begins a new crop recreation gesture. The previous active crop remains recoverable during the gesture and is replaced only after the new gesture commits a valid crop.
- Pointer down inside the active crop but not on a handle/edge should not accidentally destroy the crop. If moving the whole crop is not implemented in this story, leave inside-crop drag as no-op or document it as deferred in code/tests.
- `PointerCanceled` and `PointerCaptureLost` must never leave the controller stuck in creating/adjusting state.
- Handle hit areas should be larger than the visual handle stroke/dot so scaling and HDR contrast do not make resizing fragile.

### Coordinate Mapping Requirements

The crop state is stored in device-independent pixels relative to the overlay preview. Story 3.3 must introduce the conversion seam that later confirm/output work can consume.

Required behavior:

- Input: crop rectangle in preview DIP coordinates, preview bounds in DIP coordinates, capture/frame size in pixels.
- Output: crop rectangle in capture pixel coordinates, clamped to the capture/frame extent.
- Mapping must be deterministic and pure. It should be testable without WinUI runtime, WGC, DXGI, D3D11, or HDR hardware.
- If the current layout fills the whole overlay, the scale is capture width / preview width and capture height / preview height.
- If future letterboxing or target-aspect layout is introduced, mapping must subtract preview bounds origin before scaling.
- Use explicit rounding behavior and cover it in tests; avoid hidden truncation that loses the right/bottom edge unexpectedly.

Do not implement final pixel extraction or output. The mapper exists so Story 3.4 can confirm a crop without inventing coordinate math under pressure.

### Architecture Compliance

- `Lumiere.Overlay` owns crop UI behavior, crop geometry, handle hit testing, pointer gesture state, coordinate mapping, and overlay visual updates.
- `Lumiere.App` may receive later confirmed crop results, but should not own handle geometry, crop adjustment rules, or pointer state internals.
- `Lumiere.Capture` remains the owner of Windows Graphics Capture target/session/frame lifecycle.
- `Lumiere.Graphics` remains the owner of D3D11/DXGI rendering, FP16/scRGB constants, swap-chain resources, and presentation.
- `Lumiere.Infrastructure` remains the owner of native interop, diagnostics/result primitives, and UI-thread helpers.
- Main preview must remain GPU-resident: no `BitmapImage`, `SoftwareBitmap`, GDI, WIC, CPU readback, 8-bit texture, SDR fallback, WPF, WinForms, web UI, Electron, or Tauri path for live preview.
- Any owner of native graphics/capture resources must preserve explicit disposal semantics. Crop adjustment code should not own such resources at all.

### UX Requirements

Use the UX specification as an implementation input:

- Crop adjustment must feel immediate, stable, and visually clear over a live HDR preview.
- Handles and crop boundary must remain visible over bright, dark, and high-contrast HDR content.
- The selected crop, non-selected mask, status, and cancel controls must not cause preview layout shifts.
- Status must remain visible and must not rely on color alone.
- Cancel and Escape remain available where practical during the fullscreen overlay flow.
- The MVP is mouse/keyboard-first. Touch-specific gestures are not required.
- Avoid editor-like complexity. No annotations, history, export controls, or advanced settings belong in this crop moment.

### Previous Story Intelligence

Story 3.2 established these patterns to preserve:

- Use `CropCanvas` for pointer input and XAML crop visuals above the `SwapChainPanel`; do not route crop gestures through the preview panel.
- Keep `OverlayWindow` pointer handlers thin and move geometry/state transitions into crop-domain types.
- Use DIP-based crop geometry relative to `OverlayPreviewLayout.PreviewBounds`.
- Restore the previous active crop if a replacement creation gesture is canceled.
- Clear/hide crop visuals when crop interaction becomes unavailable.
- Real pointer routing, fullscreen/topmost behavior, HDR display behavior, high-DPI scaling, and multi-monitor placement still require Windows manual validation.

Earlier lifecycle stories established:

- Preserve `previewGeneration` as the stale-callback defense after picker awaits, frame callbacks, readiness callbacks, diagnostics callbacks, and queued resize/recreate work.
- Stop/restart should not dispose shared `GraphicsDeviceResources` during ordinary session teardown.
- Capture teardown order remains unsubscribe `FrameArrived`, dispose/stop `GraphicsCaptureSession`, dispose `Direct3D11CaptureFramePool`, dispose WinRT `IDirect3DDevice`.
- Preview teardown order remains `SetSwapChain(null)` through the preview surface before releasing DXGI swap-chain resources.
- Disposal should happen outside `previewSync`; do not hold locks while performing UI-thread detach or COM disposal.

### Git Intelligence

Recent commits show the implementation lane that crop adjustment must not disturb:

- `ed589a7 feat: implement stop, restart, and recreate capture resources` added lifecycle/recreate behavior that crop work must preserve.
- `3a964fb Record capture session state review fixes` hardened session state vocabulary and status preservation.
- `9ffea82 feat: complete implementation of target selection for display or window capture` introduced target selection service/result patterns; crop work should not reinterpret picker cancellation.
- `2f0e953 feat: implement minimal WGC FP16 capture to live preview` introduced the GPU-resident FP16 preview path; crop visuals must stay in the XAML overlay layer above it.

### Latest Technical Information

- `Directory.Packages.props` currently locks `Microsoft.WindowsAppSDK` `1.8.260317003`, `Vortice.Direct3D11` `3.8.3`, `Vortice.DXGI` `3.8.3`, `Microsoft.NET.Test.Sdk` `18.4.0`, xUnit `2.9.3`, and xUnit runner `3.1.5`.
- Microsoft Learn lists Windows App SDK `1.8.6 (1.8.260317003)` as the current stable 1.8 release. Do not upgrade Windows App SDK in this story without a concrete blocker.
- NuGet lists `Vortice.Direct3D11` `3.8.3` as compatible with `net10.0`; no new graphics package is needed for crop adjustment.
- Microsoft Learn pointer guidance says pointer capture is normally acquired in `PointerPressed`, captured pointers keep routing input to the capturing element, and `PointerPressed`/`PointerReleased` must not be assumed to occur in pairs. Handle `PointerCanceled` and `PointerCaptureLost` as gesture-ending paths.

References:

- `_bmad-output/planning-artifacts/epics.md#Story-3.3-Adjust-or-Recreate-the-Crop-Selection`
- `_bmad-output/planning-artifacts/prd.md#Crop-Interaction`
- `_bmad-output/planning-artifacts/architecture.md#Crop-Interaction-FR11-FR16`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Overlay-Behavior`
- `_bmad-output/project-context.md#Critical-Implementation-Rules`
- `Directory.Packages.props`
- Microsoft Learn: https://learn.microsoft.com/en-us/windows/apps/develop/input/handle-pointer-input
- Microsoft Learn: https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads
- NuGet: https://www.nuget.org/packages/Vortice.Direct3D11/

### Testing Requirements

Run from repository root on Windows:

```bash
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false
dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Overlay.Tests/Lumiere.Overlay.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet format Lumiere.sln --verify-no-changes --verbosity minimal
```

Automated tests should stay hardware-independent and focus on crop controller, hit testing, geometry normalization/clamping, coordinate mapping, preview bounds stability, and preservation of existing overlay state behavior.

Manual Windows validation is required for real WinUI pointer routing, handle hit areas, fullscreen/topmost overlay behavior, HDR/SDR display visibility, high-DPI scaling, multi-monitor placement, WGC, DXGI, D3D11, and HDR fidelity. Completion notes must label validation as `Mac-pass`, `Windows CI-pass`, or `Windows manual-pass` accurately.

### Anti-Patterns to Avoid

- Do not create a second crop controller or duplicate crop state model.
- Do not destroy a committed active crop until a replacement crop commits successfully.
- Do not let resize handles or labels resize the preview surface or alter coordinate mapping.
- Do not let crop code own WGC, D3D11, DXGI, swap-chain, frame-pool, frame texture, render-target, or monitor capability state.
- Do not use bitmap screenshots, CPU readback, PNG bytes, `SoftwareBitmap`, `BitmapImage`, GDI, WPF/WinForms, web UI, or SDR textures for the live preview.
- Do not update WinUI from capture/frame callbacks.
- Do not make the whole overlay click-through.
- Do not implement confirm output, export, clipboard, annotations, diagnostics settings, hotkeys, tray workflow, or capture history in this story.
- Do not claim HDR/topmost/fullscreen/manual pointer behavior fully complete without Windows manual validation.

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- 2026-05-05: `dotnet test tests/Lumiere.Overlay.Tests/Lumiere.Overlay.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false` initially failed as expected during red phase because Story 3.3 crop types/APIs were not implemented.
- 2026-05-05: `dotnet test tests/Lumiere.Overlay.Tests/Lumiere.Overlay.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false` passed after controller, mapper, and overlay integration.
- 2026-05-05: `dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false` passed.
- 2026-05-05: `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false` passed with 0 warnings and 0 errors.
- 2026-05-05: `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false` passed.
- 2026-05-05: `dotnet test tests/Lumiere.Overlay.Tests/Lumiere.Overlay.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false` passed.
- 2026-05-05: `dotnet format Lumiere.sln --verify-no-changes --verbosity minimal` passed after mechanical CRLF normalization for Overlay source/tests.
- 2026-05-05: Code review fixes passed `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false` and `dotnet test tests/Lumiere.Overlay.Tests/Lumiere.Overlay.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false`.

### Completion Notes List

- Implemented crop hit-testing, explicit adjustment gestures, normalized/clamped handle and edge resizing, inside-crop no-op behavior, and outside-crop recreation that preserves the previous active crop until a valid replacement commits.
- Added visible edge/corner handle affordances above the existing dim mask and dual crop border without moving the preview surface or controls.
- Added pure DIP-to-capture-pixel coordinate mapping with typed frame and pixel rectangle values for future confirm/output stories.
- Added hardware-independent coverage for adjustment, recreation, cancellation/restoration, bounds/min-size behavior, and coordinate mapping.
- Updated Story 3.3 manual overlay validation notes for Windows high-DPI, HDR/SDR, and multi-monitor checks.
- Addressed code review findings for stale overlay teardown on target re-selection, failed UI-dispatch swap-chain release, outward crop pixel rounding, active pointer ownership, and pending replacement crop state.
- Validation level: Windows CI-pass for restore/build/automated tests/format on this Windows workspace; Windows manual-pass remains required for real pointer routing, fullscreen/topmost, HDR/SDR visibility, high-DPI, and multi-monitor behavior.

### File List

- `src/Lumiere.Overlay/Crop/CaptureFrameSize.cs`
- `src/Lumiere.Overlay/Crop/CropAdjustmentHandle.cs`
- `src/Lumiere.Overlay/Crop/CropController.cs`
- `src/Lumiere.Overlay/Crop/CropCoordinateMapper.cs`
- `src/Lumiere.Overlay/Crop/CropGeometry.cs`
- `src/Lumiere.Overlay/Crop/CropHitTestKind.cs`
- `src/Lumiere.Overlay/Crop/CropHitTestResult.cs`
- `src/Lumiere.Overlay/Crop/CropPixelRect.cs`
- `src/Lumiere.Overlay/Crop/CropSelection.cs`
- `src/Lumiere.Overlay/Crop/CropSelectionPhase.cs`
- `src/Lumiere.Overlay/OverlayWindow.xaml`
- `src/Lumiere.Overlay/OverlayWindow.xaml.cs`
- `tests/Lumiere.Overlay.Tests/CropControllerTests.cs`
- `tests/Lumiere.Overlay.Tests/CropCoordinateMapperTests.cs`
- `docs/validation/overlay-validation.md`
- `_bmad-output/implementation-artifacts/3-3-adjust-or-recreate-the-crop-selection.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-05-05: Created Story 3.3 context and marked ready for development.
- 2026-05-05: Implemented crop adjustment/recreation, coordinate mapping, tests, validation notes, and marked ready for review.
