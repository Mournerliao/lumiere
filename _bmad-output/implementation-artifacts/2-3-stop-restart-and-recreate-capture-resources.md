# Story 2.3: Stop, Restart, and Recreate Capture Resources

Status: done

<!-- Ultimate context engine analysis completed - comprehensive developer guide created. -->

## Story

As a user,
I want capture sessions to stop and restart without restarting the app,
so that I can recover from cancellation, target changes, or display size changes.

## Acceptance Criteria

1. Given an active capture session, when the user stops or cancels capture, then WGC session, frame pool, frames, and related resources are disposed deterministically.
2. Given a target size changes, when the frame size no longer matches the preview resources, then capture and preview resources are recreated safely.
3. Given capture restarts after teardown, when a new target is selected, then stale frames or invalid surfaces from the previous session are not reused.

## Tasks / Subtasks

- [x] Promote stop/restart behavior into explicit lifecycle code instead of leaving it as incidental UI cleanup. (AC: 1, 3)
  - [x] Keep `MainWindow` as flow orchestration, but make the stop path update `CaptureSessionState` to `Disposed` or recoverable `Idle` after resources are released.
  - [x] Preserve the existing `previewGeneration` guard so delayed picker continuations, free-threaded frame callbacks, and post-dispose UI updates cannot revive old resources.
  - [x] Ensure selecting a new target always tears down the previous capture session, frame presenter, and swap chain before creating replacements.
  - [x] Do not dispose `GraphicsDeviceResources` on ordinary stop/restart unless the window/app is closing or a device-level failure requires it.

- [x] Harden deterministic teardown ordering for capture and preview resources. (AC: 1)
  - [x] Preserve capture disposal order: unsubscribe `FrameArrived`, dispose/stop `GraphicsCaptureSession`, dispose `Direct3D11CaptureFramePool`, dispose the WinRT `IDirect3DDevice`.
  - [x] Preserve preview disposal order: detach `SwapChainPanel` through `SetSwapChain(null)` before releasing the DXGI swap chain.
  - [x] Treat `FrameArrived` as free-threaded/background; teardown races must not throw out of WGC callbacks or touch WinUI directly.
  - [x] Leave WGC frames and `CapturedFrameTexture` scoped to each callback/presentation attempt; never store them in UI state.

- [x] Detect frame-size mismatch and recreate capture/preview resources safely. (AC: 2)
  - [x] Compare each arrived frame's `ContentSize` / `CapturedFrameTexture` size against the active preview resource size.
  - [x] When size changes, stop presenting the mismatched frame, invalidate the current generation, dispose old session and swap chain resources, then recreate frame pool/session and preview swap chain using the new size.
  - [x] Reapply `ApplyPreviewPanelFit` or equivalent layout sizing for the new target/frame size without changing the FP16/scRGB format requirements.
  - [x] Represent recreation as an explicit `Initializing` or `Degraded/Failed` session state rather than continuing to show `HDR-ready` during rebuild.

- [x] Prevent stale frames and invalid surfaces after restart. (AC: 3)
  - [x] Ensure frames from older generations are disposed and ignored before presentation.
  - [x] Ensure `activeCaptureTarget`, `activePresentationEvidence`, `previewFramePresenter`, `swapChainResources`, and `captureSession` are cleared atomically under the existing preview lock before disposal happens outside the lock.
  - [x] Keep failure/readiness callbacks generation-scoped; old callbacks must not overwrite the active target's status.

- [x] Add focused lifecycle tests. (AC: 1, 2, 3)
  - [x] Extend capture lifecycle tests for idempotent stop/dispose and the current unsubscribe-before-resource-release contract.
  - [x] Add tests for restart state sequencing: active session -> stop -> idle/disposed -> new initialization without reusing old resources.
  - [x] Add a resource-independent test seam for size mismatch decisions so frame-size recreation can be validated without real WGC hardware.
  - [x] Preserve existing swap-chain lifecycle tests that require detach-before-release and retry after detach failure.
  - [x] Run the standard Windows validation chain before review.

### Review Findings

- [x] [Review][Patch] Delayed frame-size recreation can revive a stale capture target [src/Lumiere.App/MainWindow.xaml.cs:237]
- [x] [Review][Patch] Frame-size recreation invalidates the generation before old resources are cleared or dispatcher enqueue is proven [src/Lumiere.App/MainWindow.xaml.cs:222]
- [x] [Review][Patch] Restart/recreation sequencing is not covered by behavioral tests [tests/Lumiere.Graphics.Tests/Capture/CaptureLifecycleTests.cs:28]
- [x] [Review][Defer] CaptureSessionResources disposal is not concurrency-idempotent [src/Lumiere.Capture/CaptureSessionResources.cs:33] - deferred, pre-existing

## Dev Notes

### Story Scope

Story 2.3 turns the current minimal preview lifecycle into a reliable stop/restart/recreate path. The end state should let a user stop or cancel capture, select another target, or survive a target/frame size change without app restart, stale frames, invalid surfaces, or misleading `HDR-ready` status.

This story does not implement the fullscreen crop overlay, confirm/cancel overlay UX, manual lifecycle stability matrix, advanced monitor HDR/SDR capability detection, settings, export, clipboard, hotkeys, tray, annotation, or capture history. Story 2.4 will validate repeated lifecycle stability more broadly; this story builds the product code paths it will exercise.

### Current Repository Context

Relevant current implementation:

- `src/Lumiere.App/MainWindow.xaml.cs` owns the current lifecycle orchestration. `StartPreview` calls `StopPreview` before creating a new swap chain and WGC session, uses `previewGeneration` to ignore stale callbacks, clears active fields under `previewSync`, and disposes resources outside the lock.
- `src/Lumiere.App/MainWindow.xaml.cs` currently sets `previewFramePresenter = null`, clears `swapChainResources`, and disposes the swap chain when `CaptureStartResult` is not started, but should also make stop/restart status visible through `CaptureSessionState`.
- `src/Lumiere.Capture/CaptureService.cs` creates `Direct3D11CaptureFramePool.CreateFreeThreaded`, subscribes `FrameArrived`, starts `GraphicsCaptureSession`, disposes each `Direct3D11CaptureFrame` with `using var frame`, and wraps the frame surface in `CapturedFrameTexture` only for the presentation callback lifetime.
- `src/Lumiere.Capture/CaptureSessionResources.cs` owns WGC session resources and delegates disposal to `CaptureSessionDisposalCoordinator`.
- `src/Lumiere.Capture/CaptureSessionDisposalCoordinator.cs` currently enforces `unsubscribe -> stop-session -> dispose-frame-pool -> dispose-device`.
- `src/Lumiere.Graphics/Presentation/SwapChainResources.cs` owns swap-chain resources and delegates disposal to `SwapChainDisposalCoordinator`.
- `src/Lumiere.Graphics/Presentation/SwapChainDisposalCoordinator.cs` enforces `detachPreview -> releaseResources`; tests already require release not to run if detach fails.
- `src/Lumiere.Graphics/Presentation/SwapChainCreationOptions.cs` validates positive width/height, preserves `HdrConstants.DxgiSwapChainFormat`, and uses `HdrConstants.DxgiColorSpace`.
- `src/Lumiere.Capture/CaptureSessionState.cs` now models `Idle`, `SelectingTarget`, `Initializing`, `Capturing`, `Degraded`, `Unsupported`, `Failed`, and `Disposed`; reuse it rather than introducing another lifecycle state vocabulary.

Likely updated files:

```text
src/Lumiere.App/MainWindow.xaml.cs
src/Lumiere.Capture/CaptureService.cs
src/Lumiere.Capture/CaptureSessionResources.cs
src/Lumiere.Capture/CaptureSessionDisposalCoordinator.cs
tests/Lumiere.Graphics.Tests/Capture/CaptureLifecycleTests.cs
tests/Lumiere.Graphics.Tests/Capture/CaptureSessionStateTests.cs
```

Possible new files if a small decision seam reduces UI complexity:

```text
src/Lumiere.Capture/CaptureFrameSizeChange.cs
tests/Lumiere.Graphics.Tests/Capture/CaptureFrameSizeChangeTests.cs
```

### Architecture Compliance

- `Lumiere.Capture` owns WGC frame pool/session lifecycle, frame disposal, and capture lifecycle result semantics.
- `Lumiere.Graphics` owns swap-chain creation, presentation, detach-before-release behavior, and FP16/scRGB constants.
- `Lumiere.App` may coordinate high-level flow, but must not own WGC/D3D11/DXGI implementation details or invent alternate readiness semantics.
- `Lumiere.Infrastructure` remains the boundary for WinRT/COM/WinUI interop helpers.
- Keep the primary preview path GPU-resident. Do not add `SoftwareBitmap`, `BitmapImage`, GDI, WIC, PNG bytes, CPU readback, SDR, or 8-bit fallback paths.

### Lifecycle Requirements

Use this lifecycle model unless implementation discovers a stricter local pattern:

| Event | Required behavior |
| --- | --- |
| User stops/cancels active capture | Increment generation, clear active fields under lock, dispose capture session resources, detach/release swap chain, then report stopped/idle state. |
| User selects a new target | Stop existing preview first; no old frame, presenter, swap chain, or session may be reused for the new target. |
| Capture start fails after swap-chain creation | Dispose any partially created WGC resources and dispose/detach preview resources created for that attempt. |
| Free-threaded frame arrives after stop | Dispose/ignore it via generation check; it must not update UI or present to an old swap chain. |
| Frame size changes | Recreate frame pool/session and preview resources using the new size; do not keep presenting mismatched frames. |
| Window closes | Stop preview first, then dispose device-level resources. |

### Previous Story Intelligence

Story 2.2 established these patterns that must be preserved:

- `CaptureSessionState` is the state contract; do not create parallel lifecycle/status vocabulary.
- `Started` on `CaptureStartResult` only means native WGC session resources exist; it does not mean the preview is HDR-trustworthy.
- `MainWindow.xaml.cs` derives labels from `CaptureSessionState`, while `Lumiere.Capture` owns capture state semantics.
- Generation checks are required after target picker awaits, frame callbacks, frame readiness callbacks, and diagnostic callbacks.
- Degraded/failed presentation evidence must not be overwritten by later generic capture initialization messages.
- Unsupported readiness during an active session can retain target/native-session context.

Story 2.1 established these patterns that still apply:

- Picker cancellation is normal and must not be treated as failure.
- `CaptureTargetSelectionService` owns picker outcome classification.
- `GraphicsCaptureItem.CreateForTesting` was unavailable in the target SDK; tests should continue using `CaptureTarget.CreateForTest`.
- `CaptureTargetKind` remains limited; do not infer full monitor HDR capability in this story.

### Git Intelligence

Recent commits show the implementation pattern to follow:

- `3a964fb Record capture session state review fixes` added the final Story 2.2 state model, review fixes, and sprint/story updates.
- `9ffea82 feat: complete implementation of target selection for display or window capture` created the target selection service/result pattern that restart flow should keep using.
- `4a42b2c feat: record epic 1 HDR preview validation and harden frame handling` hardened frame handling and swap-chain interop; do not weaken those safeguards.
- `0a36ff4` referenced in Story 2.2 fixed `StopPreview` deadlock, `StartPreview` race, and failed-start rollback hazards. Preserve those lifecycle protections even if the local git history has moved on.

### Technical Requirements and Latest References

- Keep package versions currently locked in `Directory.Packages.props`: `Microsoft.WindowsAppSDK` `1.8.260317003`, `Vortice.Direct3D11` `3.8.3`, `Vortice.DXGI` `3.8.3`, xUnit `2.9.3`, and xUnit runner `3.1.5`.
- Microsoft Learn currently documents `Direct3D11CaptureFramePool` as `IClosable`/`IDisposable` and exposes `Close`, `Dispose`, `CreateFreeThreaded`, `Recreate`, and `TryGetNextFrame`.
- Microsoft Learn currently documents `CreateFreeThreaded` as removing the `DispatcherQueue` dependency and raising `FrameArrived` on the frame pool's internal worker thread. Continue treating callbacks as non-UI-thread.
- Microsoft Learn currently documents `Direct3D11CaptureFramePool.Recreate(IDirect3DDevice, DirectXPixelFormat, Int32, SizeInt32)` for recreating a frame pool from new inputs. Use it only if it preserves the required state and event semantics cleanly; otherwise dispose/recreate explicitly with the same FP16 format.
- Microsoft Learn currently documents `IDXGISwapChain::ResizeBuffers` as changing back buffer size/format/count for window resize. Any swap-chain resize/recreate path must preserve `DXGI_FORMAT_R16G16B16A16_FLOAT` and re-establish scRGB color-space evidence.

References:

- `_bmad-output/planning-artifacts/epics.md#Story-2.3-Stop-Restart-and-Recreate-Capture-Resources`
- `_bmad-output/planning-artifacts/prd.md#Reliability-and-Resource-Lifecycle`
- `_bmad-output/planning-artifacts/architecture.md#Resource-Lifecycle-and-Session-Management-FR27-FR31-NFR9-NFR13`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Error-Recovery`
- `_bmad-output/project-context.md#Critical-Dont-Miss-Rules`
- `_bmad-output/implementation-artifacts/2-2-represent-capture-session-state-explicitly.md#Completion-Notes-List`
- Microsoft Learn: https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.direct3d11captureframepool
- Microsoft Learn: https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.direct3d11captureframepool.framearrived
- Microsoft Learn: https://learn.microsoft.com/en-us/windows/win32/api/dxgi/nf-dxgi-idxgiswapchain-resizebuffers

### Testing Requirements

Run from repository root on Windows:

```bash
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false
dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet format Lumiere.sln --verify-no-changes --verbosity minimal
```

Automated tests should focus on lifecycle sequencing, state transitions, stale generation handling, and resize/recreate decision logic. Real WinUI, WGC, DXGI, D3D11, HDR display fidelity, and multi-monitor resize behavior still require Windows hardware/manual validation. Completion notes must label validation level accurately.

### Anti-Patterns to Avoid

- Do not keep presenting a frame whose size no longer matches the active preview/swap-chain resources.
- Do not reuse `Direct3D11CaptureFrame`, `CapturedFrameTexture`, old swap chains, old presenters, or old frame pools across restart/recreate boundaries.
- Do not update WinUI from `FrameArrived` directly.
- Do not hold `previewSync` while calling UI-thread detach or COM disposal if that can reintroduce deadlock risk.
- Do not report `HDR-ready` during teardown, resize recreation, or after degraded/failed evidence.
- Do not dispose shared device resources during ordinary target restart unless the window is closing or device recovery is explicitly required.
- Do not implement Story 2.4's full repeated lifecycle validation matrix here beyond the focused tests needed for this story.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-04: Started Story 2.3 implementation; loaded lifecycle context and identified frame-size recreation seam plus MainWindow stop/restart hardening path.
- 2026-05-04: Red phase confirmed with missing `CaptureFrameSizeChange` compile failure before implementation.
- 2026-05-04: Windows validation chain passed: restore, build, test (82 passed), and format verify.

### Completion Notes List

- Implemented explicit stop/restart orchestration in `MainWindow`: selecting a new target now tears down the previous preview first, `StopPreview` can report `Disposed`, and ordinary restart does not dispose shared `GraphicsDeviceResources`.
- Added frame-size mismatch detection before presentation. Mismatched frames are skipped, the current generation is invalidated, and UI-thread restart recreates the WGC session path plus FP16/scRGB swap chain resources for the replacement size.
- Preserved generation-scoped callbacks for frame presentation, readiness, and diagnostics so stale callbacks cannot overwrite the active target status.
- Added `CaptureFrameSizeChange` as a resource-independent decision seam with tests, plus lifecycle/state tests for idempotent disposal and `Disposed` state sequencing.
- Validation level: Windows CI-pass for restore/build/unit tests/format on this Windows environment. Windows manual-pass for real WGC/DXGI/HDR display behavior was not performed.

### File List

- `_bmad-output/implementation-artifacts/2-3-stop-restart-and-recreate-capture-resources.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Lumiere.App/MainWindow.xaml.cs`
- `src/Lumiere.Capture/CaptureFrameSizeChange.cs`
- `src/Lumiere.Capture/CaptureTarget.cs`
- `tests/Lumiere.Graphics.Tests/Capture/CaptureFrameSizeChangeTests.cs`
- `tests/Lumiere.Graphics.Tests/Capture/CaptureLifecycleTests.cs`
- `tests/Lumiere.Graphics.Tests/Capture/CaptureSessionStateTests.cs`

### Change Log

- 2026-05-04: Created Story 2.3 context and marked ready for development.
- 2026-05-04: Implemented stop/restart/recreate lifecycle hardening and marked ready for review.
