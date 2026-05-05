# Story 3.4: Confirm or Cancel the Capture Overlay

Status: done

<!-- Ultimate context engine analysis completed - comprehensive developer guide created. -->

## Story

As a screenshot user,
I want to confirm a crop or cancel the overlay,
so that I can complete or exit the MVP workflow predictably.

## Acceptance Criteria

1. Given a valid crop selection exists, when the user confirms, then the selected crop region is captured as the confirmed MVP output state.
2. Given the overlay is open, when the user cancels, then capture and preview resources begin teardown and the desktop state is restored.
3. Given confirm or cancel is invoked during a degraded state, when the operation completes, then the user receives the appropriate status and the overlay does not remain stuck.

## Tasks / Subtasks

- [x] Add an explicit confirmed-crop state model owned by `Lumiere.Overlay`. (AC: 1)
  - [x] Reuse `CropController`, `CropSelection`, `CropCoordinateMapper`, `CropPixelRect`, and `CaptureFrameSize`; do not create a second crop geometry model.
  - [x] Add a typed confirmed result, such as `ConfirmedCaptureSelection`, that contains the DIP crop region, mapped capture pixel region, frame size, overlay status, and status text needed by `Lumiere.App`.
  - [x] Treat confirm as the MVP in-app output state only. Do not implement file export, clipboard output, annotation, history, tone mapping, HDR still image encoding, or post-MVP output semantics.
  - [x] Reject confirm when there is no valid active crop; the UI should keep the overlay open and not claim completion.

- [x] Add confirm UI and command wiring without destabilizing the preview surface. (AC: 1, 3)
  - [x] Add a `Confirm crop` action to `src/Lumiere.Overlay/OverlayWindow.xaml` near the existing `Cancel` action.
  - [x] Enable confirm only when the crop controller has a valid active crop and overlay status is usable.
  - [x] Keep `PreviewSwapChainPanel`, `CropCanvas`, status panel, mask, and crop coordinate mapping stable when the confirm button appears, disables, or transitions to confirming.
  - [x] Keep pointer handling in `OverlayWindow` thin; delegate crop validity and mapping to overlay crop-domain types.

- [x] Wire confirm into app-level capture flow through a narrow event or callback. (AC: 1, 2, 3)
  - [x] Add an overlay event such as `CaptureConfirmed` that emits the typed confirmed result.
  - [x] In `src/Lumiere.App/MainWindow.xaml.cs`, handle confirmation by recording the confirmed MVP state, applying a user-facing status, starting preview teardown, and closing the overlay safely.
  - [x] Preserve `CloseRequested` for cancel semantics; do not overload close/cancel and confirm into the same untyped event.
  - [x] Ensure repeated confirm/cancel clicks are idempotent and cannot double-dispose resources or update a replaced overlay.

- [x] Preserve teardown and native resource ownership rules. (AC: 2, 3)
  - [x] Reuse existing `StopPreview()` and `CloseOverlayWindow()` paths for cancel and post-confirm teardown where possible.
  - [x] Preserve `previewGeneration`, stale callback checks, stop-before-restart, frame-size recreation, and failed UI-dispatch swap-chain disposal behavior in `MainWindow`.
  - [x] Keep teardown order: capture session/frame pool first where currently owned, then preview detach through `SetSwapChain(null)`, then DXGI swap-chain resources.
  - [x] Do not dispose WGC, D3D11, DXGI, swap-chain, frame-pool, frame texture, render-target, or WinRT resources from overlay crop code.

- [x] Handle degraded, unsupported, failed, closing, and disposed states explicitly. (AC: 3)
  - [x] Allow confirm in `HdrReady` and `DegradedPreview` only when a valid crop exists; include the degraded status in the confirmed result/status message.
  - [x] Disable or reject confirm in `UnsupportedCapture`, `PreviewFailed`, `Closing`, and `Disposed` states.
  - [x] Keep cancel available for degraded and failure states wherever the overlay can safely close.
  - [x] Ensure confirm/cancel transitions apply visible `Closing` or final status text before teardown when practical, without blocking resource disposal.

- [x] Add automated tests and manual validation notes. (AC: 1, 2, 3)
  - [x] Extend `tests/Lumiere.Overlay.Tests/` for confirm availability, valid crop confirmation, no-crop rejection, DIP-to-pixel mapping in confirmed results, degraded-state confirmation, and unsupported/failed-state rejection.
  - [x] Extend app or lifecycle tests where practical for idempotent confirm/cancel teardown and preserved stale-callback behavior.
  - [x] Preserve existing overlay crop creation/adjustment, coordinate mapper, graphics, and capture lifecycle tests.
  - [x] Update `docs/validation/overlay-validation.md` with Story 3.4 checks for confirm, cancel, degraded confirm, repeated confirm/cancel, high-DPI, HDR/SDR, and multi-monitor overlay behavior.

### Review Findings

- [x] [Review][Patch] Refresh overlay capture frame size when reusing overlay after preview recreation [src/Lumiere.App/MainWindow.xaml.cs:394]

## Dev Notes

### Story Scope

Story 3.4 completes the MVP overlay decision point: a user can confirm a valid crop selection as an in-app MVP output state, or cancel and return to the desktop with capture/preview teardown underway. The confirmed state should prove the app knows exactly what crop region was selected in both overlay DIP coordinates and capture pixel coordinates.

This story does not implement saved screenshots, clipboard, HDR/SDR output semantics, export formats, annotation, global hotkeys, tray workflow, capture history, settings, or advanced diagnostics UI. Epic 6 owns output/export semantics after separate research. Story 3.5 still owns broader window transparency, hit-testing hardening, and keyboard escape coverage.

### Current Repository Context

Story 3.3 established the crop foundation that this story should consume:

- `src/Lumiere.Overlay/Crop/CropController.cs` owns crop creation, adjustment, outside-crop recreation, active pointer gesture state, and previous crop restoration.
- `src/Lumiere.Overlay/Crop/CropCoordinateMapper.cs` maps crop rectangles from preview DIP coordinates to capture pixel coordinates using `CaptureFrameSize` and `CropPixelRect`.
- `src/Lumiere.Overlay/OverlayWindow.xaml.cs` owns overlay pointer wiring, crop visuals, status application, cancel event routing, and crop availability by overlay state.
- `src/Lumiere.Overlay/OverlayWindow.xaml` contains `PreviewSwapChainPanel` as the base hardware preview layer, `CropCanvas` above it, crop mask/borders/handles, status text, and the current Cancel button.
- `src/Lumiere.App/MainWindow.xaml.cs` owns capture target selection, overlay lifetime, `StopPreview()`, `CloseOverlayWindow()`, `previewGeneration`, stale callback checks, frame-size recreation, and session status projection into the overlay.
- `docs/validation/overlay-validation.md` already covers Story 3.1-3.3 overlay, crop, adjustment, high-DPI, HDR/SDR, and multi-monitor manual checks.

Likely changed or new files:

```text
src/Lumiere.Overlay/OverlayWindow.xaml
src/Lumiere.Overlay/OverlayWindow.xaml.cs
src/Lumiere.Overlay/OverlayState.cs
src/Lumiere.Overlay/Crop/ConfirmedCaptureSelection.cs
src/Lumiere.App/MainWindow.xaml.cs
tests/Lumiere.Overlay.Tests/OverlayConfirmTests.cs
tests/Lumiere.Overlay.Tests/CropCoordinateMapperTests.cs
docs/validation/overlay-validation.md
```

Avoid changing package versions, HDR constants, WGC frame pool format, DXGI swap-chain format, color space, swap-chain attach/detach behavior, or project boundaries unless a compile-time integration seam truly requires it.

### Confirm Semantics

- Confirm requires a valid active crop. A creating, adjusting, empty, too-small, unsupported, failed, closing, or disposed state must not produce a confirmed result.
- The confirmed result should preserve both:
  - UI crop region in device-independent pixels relative to the overlay preview.
  - Capture crop region in source pixels produced by `CropCoordinateMapper`.
- Confirm should be valid in `HdrReady` and `DegradedPreview`. In degraded mode, the result/status must carry degraded context so the app does not imply HDR correctness was proven.
- Confirm should start the same kind of deterministic teardown that cancel uses after the result is recorded. It should not leave the overlay open unless the confirm is rejected.
- Confirm must not extract pixels, read back textures, encode images, copy to clipboard, or introduce SDR/HDR output claims.

### Cancel Semantics

- Cancel should remain available throughout normal and degraded overlay states and should restore desktop state by closing the overlay and beginning capture/preview teardown.
- Escape currently routes through `CloseRequested`; preserve that behavior unless Story 3.5 changes the broader keyboard model.
- Cancel must be idempotent. Multiple cancel/close paths should not double-dispose or resurrect stale state.
- If cancel occurs while a crop gesture is creating or adjusting, crop state can be discarded because the session is ending.

### Architecture Compliance

- `Lumiere.Overlay` owns overlay UI behavior, crop validity, crop confirmation payloads, and overlay-level status.
- `Lumiere.App` owns app composition, receives the confirmed crop result, updates app-visible state, and coordinates teardown.
- `Lumiere.Capture` remains the owner of Windows Graphics Capture target/session/frame lifecycle.
- `Lumiere.Graphics` remains the owner of D3D11/DXGI rendering, FP16/scRGB constants, swap-chain resources, and presentation.
- `Lumiere.Infrastructure` remains the owner of native interop, diagnostics/result primitives, and UI-thread helpers.
- The main live preview must remain GPU-resident: no `BitmapImage`, `SoftwareBitmap`, GDI, WIC, CPU readback, 8-bit texture, SDR fallback, WPF, WinForms, web UI, Electron, or Tauri path for live preview.

### UX Requirements

Use the UX specification as implementation input:

- `Confirm crop` is the primary action only after a valid crop exists.
- `Cancel` remains available and understandable while the fullscreen overlay is open.
- Degraded confirmation must be honest: users should know the preview fidelity cannot be fully trusted.
- Controls, status messages, and diagnostics must not resize the preview surface or alter crop coordinate mapping.
- The overlay should feel focused and native; do not turn this into an editor or output/export screen.
- Status cannot rely on color alone, and keyboard-safe cancellation remains required where practical.

### Previous Story Intelligence

Story 3.3 review and implementation created guardrails that 3.4 must preserve:

- Do not create a second crop controller or duplicate crop state model.
- Outside-crop recreation keeps the previous active crop until a valid replacement commits.
- Crop coordinate mapping uses floor for left/top and ceiling for right/bottom to avoid losing selected edge pixels.
- Pointer gestures are bound to one active pointer and conclude through `PointerReleased`, `PointerCanceled`, and `PointerCaptureLost`.
- Overlay target re-selection should close/recreate the overlay rather than keeping stale UI state.
- Failed UI-dispatch swap-chain release must still dispose resources through the existing fallback.
- Validation level for overlay pointer routing, fullscreen/topmost behavior, HDR/SDR visibility, high-DPI, and multi-monitor behavior still requires Windows manual validation.

Earlier lifecycle stories established:

- Preserve `previewGeneration` as the stale-callback defense after picker awaits, frame callbacks, readiness callbacks, diagnostics callbacks, and queued resize/recreate work.
- Stop/restart should not dispose shared `GraphicsDeviceResources` during ordinary session teardown.
- Capture teardown order remains unsubscribe `FrameArrived`, dispose/stop `GraphicsCaptureSession`, dispose `Direct3D11CaptureFramePool`, dispose WinRT `IDirect3DDevice`.
- Preview teardown order remains `SetSwapChain(null)` through the preview surface before releasing DXGI swap-chain resources.
- Disposal should happen outside `previewSync`; do not hold locks while performing UI-thread detach or COM disposal.

### Git Intelligence

Recent commits show the implementation lane this story must preserve:

- `ed589a7 feat: implement stop, restart, and recreate capture resources` added lifecycle/recreate behavior that confirm/cancel must reuse rather than bypass.
- `3a964fb Record capture session state review fixes` hardened session state vocabulary and status preservation.
- `9ffea82 feat: complete implementation of target selection for display or window capture` introduced target selection service/result patterns; canceling overlay should not reinterpret picker cancellation.
- `2f0e953 feat: implement minimal WGC FP16 capture to live preview` introduced the GPU-resident FP16 preview path; confirm/cancel must not add bitmap or CPU readback output.

### Latest Technical Information

- `Directory.Packages.props` currently locks `Microsoft.WindowsAppSDK` `1.8.260317003`, `Vortice.Direct3D11` `3.8.3`, `Vortice.DXGI` `3.8.3`, `Microsoft.NET.Test.Sdk` `18.4.0`, xUnit `2.9.3`, and xUnit runner `3.1.5`.
- Microsoft Learn lists Windows App SDK `1.8.6 (1.8.260317003)` as the current stable 1.8 release. Do not upgrade Windows App SDK in this story without a concrete blocker.
- NuGet lists `Vortice.Direct3D11` `3.8.3` as compatible with `net10.0`; no new graphics package is needed for confirm/cancel overlay work.
- Microsoft Learn pointer guidance says `CapturePointer` should be called from `PointerPressed`, captured pointers continue routing input to the capturing element, and `PointerPressed`/`PointerReleased` are not guaranteed to occur in pairs. Keep `PointerCanceled` and `PointerCaptureLost` as gesture-ending paths.

References:

- `_bmad-output/planning-artifacts/epics.md#Story-3.4-Confirm-or-Cancel-the-Capture-Overlay`
- `_bmad-output/planning-artifacts/prd.md#Crop-Interaction`
- `_bmad-output/planning-artifacts/prd.md#Overlay-and-Desktop-Window-Behavior`
- `_bmad-output/planning-artifacts/architecture.md#Crop-Interaction-FR11-FR16`
- `_bmad-output/planning-artifacts/architecture.md#Overlay-and-Desktop-Window-Behavior-FR17-FR21`
- `_bmad-output/planning-artifacts/ux-design-specification.md#OverlayActionToolbar`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Button-Hierarchy`
- `_bmad-output/project-context.md#Critical-Implementation-Rules`
- `_bmad-output/implementation-artifacts/3-3-adjust-or-recreate-the-crop-selection.md`
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

Automated tests should stay hardware-independent and focus on confirm availability, confirmed payload shape, DIP-to-pixel mapping, degraded status preservation, unsupported/failed confirm rejection, idempotent cancel/confirm state transitions, and preservation of existing crop and lifecycle tests.

Manual Windows validation is required for real WinUI button routing, Escape/Cancel behavior, fullscreen/topmost overlay teardown, HDR/SDR display visibility, high-DPI scaling, multi-monitor placement, WGC, DXGI, D3D11, and HDR fidelity. Completion notes must label validation as `Mac-pass`, `Windows CI-pass`, or `Windows manual-pass` accurately.

### Anti-Patterns to Avoid

- Do not implement export, clipboard, annotation, hotkeys, tray workflow, capture history, or HDR still-image semantics.
- Do not read back GPU textures or create bitmap screenshots as part of confirm.
- Do not silently treat degraded preview as HDR-ready in confirmed status.
- Do not create duplicate crop state, duplicate coordinate mapping, or a second overlay teardown path.
- Do not close over stale `overlayWindow`, `captureSession`, `swapChainResources`, or old `previewGeneration` values in a way that lets old callbacks update new state.
- Do not make confirm available before a valid active crop exists.
- Do not hide or remove the cancel path while confirming, degraded, failed, or closing unless the window is already disposed.
- Do not claim HDR/topmost/fullscreen/manual pointer behavior fully complete without Windows manual validation.

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- 2026-05-05: `dotnet restore tests/Lumiere.Overlay.Tests/Lumiere.Overlay.Tests.csproj --disable-parallel --verbosity detailed /nr:false` passed.
- 2026-05-05: `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false` passed.
- 2026-05-05: `dotnet test tests/Lumiere.Overlay.Tests/Lumiere.Overlay.Tests.csproj -p:Platform=x64 --no-build --verbosity minimal /nr:false` passed: 43 tests.
- 2026-05-05: `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-build --verbosity minimal /nr:false` passed: 98 tests.
- 2026-05-05: `dotnet format Lumiere.sln --verify-no-changes --verbosity minimal` passed after line-ending normalization.

### Completion Notes List

- Added `ConfirmedCaptureSelection` as the overlay-owned MVP confirmation payload. It reuses the existing crop selection and coordinate mapper, preserves DIP and capture-pixel crop regions, carries frame size and overlay status, and rejects missing/invalid crops plus unsupported, failed, closing, and disposed states.
- Added `Confirm crop` UI next to `Cancel`. Confirm enables only for active valid crops in `HdrReady` or `DegradedPreview`, disables during active gestures/closing, and routes mapping/validity through the crop-domain model rather than duplicating geometry in the window.
- Added a typed `CaptureConfirmed` overlay event and app-level handler that records the confirmed MVP state, applies a user-facing confirmed status, then reuses `StopPreview()` and `CloseOverlayWindow()` for deterministic teardown. `CloseRequested` remains the cancel path.
- Confirm/cancel transitions are guarded with an overlay closing flag to prevent repeated clicks from double-emitting or double-disposing. Overlay crop code does not dispose WGC, D3D11, DXGI, swap-chain, frame-pool, frame texture, render-target, or WinRT resources.
- Added automated overlay confirmation tests and Story 3.4 manual validation notes. Validation level: Windows CI-pass for restore/build/tests/format on this Windows machine; Windows manual-pass for real fullscreen/topmost, HDR/SDR display behavior, high-DPI, multi-monitor, and WGC/DXGI/HDR fidelity is still required.

### File List

- `_bmad-output/implementation-artifacts/3-4-confirm-or-cancel-the-capture-overlay.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/validation/overlay-validation.md`
- `src/Lumiere.App/MainWindow.xaml.cs`
- `src/Lumiere.Overlay/Crop/ConfirmedCaptureSelection.cs`
- `src/Lumiere.Overlay/OverlayWindow.xaml`
- `src/Lumiere.Overlay/OverlayWindow.xaml.cs`
- `tests/Lumiere.Overlay.Tests/OverlayConfirmTests.cs`

### Change Log

- 2026-05-05: Created Story 3.4 context and marked ready for development.
- 2026-05-05: Implemented confirm/cancel overlay workflow and marked ready for review.
