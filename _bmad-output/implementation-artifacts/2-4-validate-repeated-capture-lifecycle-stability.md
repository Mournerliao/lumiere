# Story 2.4: Validate Repeated Capture Lifecycle Stability

Status: done

<!-- Ultimate context engine analysis completed - comprehensive developer guide created. -->

## Story

As a developer,
I want repeatable lifecycle validation for capture start, stop, cancel, and restart,
so that graphics and capture resources do not leak across sessions.

## Acceptance Criteria

1. Given lifecycle tests or manual diagnostics are run, when capture starts and stops repeatedly, then session state returns to idle or recoverable failure without app restart.
2. Given graphics teardown occurs, when resources are released, then preview presentation is detached before device-bound resources are disposed.
3. Given repeated sessions are exercised, when diagnostics are inspected, then there is no evidence of unbounded frame pool, texture, render target, swap-chain, or device resource growth.

## Tasks / Subtasks

- [x] Add repeatable lifecycle validation around existing stop/restart behavior without rewriting the capture pipeline. (AC: 1, 3)
  - [x] Add a resource-independent lifecycle validation seam that can record repeated start, stop, cancel, restart, resize/recreate, failure, and close attempts.
  - [x] Use the existing `CaptureSessionState` vocabulary: `Idle`, `SelectingTarget`, `Initializing`, `Capturing`, `Degraded`, `Unsupported`, `Failed`, and `Disposed`.
  - [x] Verify each repeated flow ends in `Idle`, `Disposed`, `Unsupported`, `Degraded`, or `Failed`; it must not remain stuck in `SelectingTarget`, `Initializing`, or stale `Capturing`.
  - [x] Preserve the `previewGeneration` stale-callback guard in `MainWindow`; do not introduce callbacks that can revive old targets or old status.

- [x] Strengthen deterministic teardown evidence for capture and presentation resources. (AC: 2, 3)
  - [x] Preserve capture teardown order: unsubscribe `FrameArrived`, dispose/stop `GraphicsCaptureSession`, dispose `Direct3D11CaptureFramePool`, dispose WinRT `IDirect3DDevice`.
  - [x] Preserve preview teardown order: call `SetSwapChain(null)` through the preview surface before releasing DXGI swap-chain resources.
  - [x] Add tests proving teardown evidence is recorded even when flows are repeated, cancelled, or fail during initialization.
  - [x] Do not dispose shared `GraphicsDeviceResources` during ordinary capture restart; device-level disposal remains window/app close or explicit device recovery scope.

- [x] Add diagnostics or validation records that make repeated lifecycle stability inspectable. (AC: 1, 3)
  - [x] Record lifecycle attempt count, final state, teardown completion, detach-before-release evidence, and any degraded/failed readiness detail.
  - [x] Keep diagnostics local-only and content-free; do not record screenshot pixels, frame contents, or target image data.
  - [x] If diagnostics are surfaced in code, place models under the owning boundary (`Lumiere.Capture` for capture lifecycle state, `Lumiere.Graphics` for presentation lifecycle evidence, or `Lumiere.Infrastructure` only for cross-cutting diagnostic primitives).
  - [x] Avoid adding user-facing advanced diagnostics UI in this story; Epic 4 owns broader diagnostics surfaces.

- [x] Cover repeated lifecycle paths with focused automated tests. (AC: 1, 2, 3)
  - [x] Extend `tests/Lumiere.Graphics.Tests/Capture/CaptureLifecycleTests.cs` or add narrowly named test files for repeated flow validation.
  - [x] Preserve existing tests for `CaptureSessionResources` idempotent disposal, `CaptureSessionDisposalCoordinator` order, `SwapChainDisposalCoordinator` detach-before-release, and resize/recreate request generation matching.
  - [x] Add tests for multiple validation iterations and for recoverable failure states.
  - [x] Keep tests hardware-independent; real WGC, WinUI, DXGI, D3D11, HDR display, and GPU memory behavior remain Windows manual validation.

- [x] Document the manual lifecycle stability checklist needed before claiming Windows manual-pass. (AC: 3)
  - [x] Create or update `docs/validation/lifecycle-validation.md` if the repository does not already contain it.
  - [x] Include start, stop, cancel target selection, restart with same target, restart with different target, frame-size recreate, failed initialization, window close, and repeated sequence loops.
  - [x] Include what to inspect: final state, stuck overlay/window behavior, frame arrival after stop, detach-before-release logs/evidence, and GPU memory trend.
  - [x] State that GPU memory stability and real frame pool/swap-chain behavior require Windows hardware/manual validation.

### Review Findings

- [x] [Review][Patch] Capture teardown evidence is returned but discarded by the production resource owner [`src/Lumiere.Capture/CaptureSessionResources.cs:18`]
- [x] [Review][Patch] Swap-chain detach/release evidence is returned but discarded by the production resource owner [`src/Lumiere.Graphics/Presentation/SwapChainResources.cs:32`]
- [x] [Review][Patch] Empty or unmeasured lifecycle summaries can report a clean validation result [`src/Lumiere.Capture/CaptureLifecycleValidationSummary.cs:16`]

## Dev Notes

### Story Scope

Story 2.4 validates that the lifecycle behavior implemented through Stories 2.1-2.3 is repeatable and inspectable. The expected output is validation code/tests and a manual lifecycle checklist, not a new capture architecture.

This story does not implement fullscreen crop overlay UI, crop confirm/cancel product workflow, HDR/SDR monitor capability detection, advanced diagnostics UI, settings, export, clipboard, hotkeys, tray, annotation, or capture history. It may create diagnostic/validation data structures only when they directly support repeated lifecycle stability.

### Current Repository Context

Relevant current implementation:

- `src/Lumiere.App/MainWindow.xaml.cs` orchestrates the current preview lifecycle. It stops any active preview before target selection/start, uses `previewGeneration` to ignore stale callbacks, clears active resources under `previewSync`, disposes capture and swap-chain resources outside the lock, and reports `Disposed` on ordinary stop.
- `src/Lumiere.App/MainWindow.xaml.cs` handles frame-size mismatch by invalidating the generation, clearing old target/session/presenter/swap-chain references, disposing old capture resources, then queueing UI-thread swap-chain disposal and restart with a replacement target size.
- `src/Lumiere.Capture/CaptureService.cs` creates `Direct3D11CaptureFramePool.CreateFreeThreaded`, treats `FrameArrived` as a background-thread callback, scopes each WGC frame with `using var frame`, wraps the frame surface in `CapturedFrameTexture` only for callback lifetime, and reports first frame failure through a gate.
- `src/Lumiere.Capture/CaptureSessionResources.cs` is idempotent only for sequential calls. Story 2.3 review deferred concurrency-idempotence as pre-existing; do not claim concurrency safety unless this story actually implements and tests it.
- `src/Lumiere.Capture/CaptureSessionDisposalCoordinator.cs` enforces capture disposal order.
- `src/Lumiere.Graphics/Presentation/SwapChainResources.cs` delegates detach/release to `SwapChainDisposalCoordinator`.
- `src/Lumiere.Graphics/Presentation/SwapChainDisposalCoordinator.cs` currently lets detach failure prevent resource release; existing tests require retry after detach failure.
- `src/Lumiere.Capture/CaptureFrameSizeChange.cs` and `CapturePreviewRecreationRequest.cs` are hardware-independent seams created by Story 2.3 for resize/recreate validation.

Likely updated files:

```text
src/Lumiere.Capture/CaptureSessionResources.cs
src/Lumiere.Capture/CaptureSessionDisposalCoordinator.cs
src/Lumiere.Graphics/Presentation/SwapChainDisposalCoordinator.cs
tests/Lumiere.Graphics.Tests/Capture/CaptureLifecycleTests.cs
tests/Lumiere.Graphics.Tests/Presentation/SwapChainLifecycleTests.cs
docs/validation/lifecycle-validation.md
```

Possible new files if they reduce test ambiguity without leaking platform APIs:

```text
src/Lumiere.Capture/CaptureLifecycleValidationRecord.cs
src/Lumiere.Capture/CaptureLifecycleValidationSummary.cs
tests/Lumiere.Graphics.Tests/Capture/CaptureLifecycleValidationTests.cs
```

Avoid placing capture lifecycle concepts in `Lumiere.App` unless they are strictly UI orchestration. Avoid putting graphics-specific detach/release state in `Lumiere.Capture`.

### Architecture Compliance

- `Lumiere.Capture` owns WGC frame pool/session lifecycle, capture status, and capture lifecycle validation semantics.
- `Lumiere.Graphics` owns swap-chain presentation lifecycle, detach-before-release behavior, FP16/scRGB constants, and graphics validation evidence.
- `Lumiere.App` may coordinate UI commands and display status, but it must not own WGC/D3D11/DXGI resource semantics.
- `Lumiere.Infrastructure` remains the boundary for native interop, result/diagnostic primitives, and UI-thread helpers.
- The primary preview path must remain GPU-resident and HDR-oriented: no `BitmapImage`, `SoftwareBitmap`, GDI, WIC, PNG bytes, CPU readback, SDR, or 8-bit fallback paths for routine presentation.

### Lifecycle Validation Requirements

Use these flow expectations:

| Flow | Required validation |
| --- | --- |
| User cancels target picker | No WGC session starts; state returns to `Idle`/recoverable selection result. |
| User stops active capture | Generation increments; active session/presenter/swap-chain references clear; capture resources dispose; swap chain detaches then releases; state becomes `Disposed` or `Idle`. |
| User restarts same target | Old resources are not reused; old callbacks cannot update active state; new attempt gets a new generation. |
| User selects a different target | Previous preview stops first; old frame pool, frame, presenter, and swap-chain evidence do not leak into new target status. |
| Frame size changes | Mismatched frame is skipped; old generation invalidates; capture and preview resources recreate for replacement size. |
| Capture start fails after partial setup | Partial WGC resources and any created preview resources dispose; final state is recoverable `Failed`, `Unsupported`, or `Degraded`. |
| Detach fails during swap-chain disposal | Release is not called until detach succeeds on retry; diagnostics/evidence must not claim successful release. |
| Window closes | Stop preview before disposing device-level resources; no frame callback may touch WinUI after close. |

### Previous Story Intelligence

Story 2.3 established these patterns that must be preserved:

- `MainWindow` currently coordinates lifecycle but should not accumulate more native ownership. Prefer small validation seams under source-boundary projects.
- `previewGeneration` is the core stale-callback defense after picker awaits, free-threaded frame callbacks, readiness callbacks, diagnostics callbacks, and queued resize/recreate work.
- Ordinary stop/restart must not dispose shared `GraphicsDeviceResources`.
- Frame-size mismatch must not present the mismatched frame and must not report `HDR-ready` while rebuilding.
- Disposal should happen outside `previewSync`; do not reintroduce lock-held UI-thread detach or COM disposal deadlock risk.
- WGC frames and `CapturedFrameTexture` remain scoped to callback/presentation attempts and must not be stored in UI state.

Story 2.2 established these patterns:

- `CaptureSessionState` is the state contract. Do not create a parallel status vocabulary.
- `CaptureStartResult.Started` only proves native WGC session resources exist; it does not prove preview trust.
- Degraded/failed presentation evidence must not be overwritten by generic capture initialization text.
- Picker cancellation is normal and should not become a failure state.

Story 2.1 established these patterns:

- `CaptureTargetSelectionService` owns picker result classification.
- Tests should use `CaptureTarget.CreateForTest`; do not rely on unavailable SDK test construction helpers.
- Do not infer full monitor HDR capability in Epic 2.

### Git Intelligence

Recent commits show the implementation pattern to follow:

- `3a964fb Record capture session state review fixes` finalized Story 2.2 state model and review fixes. Preserve that state contract.
- `9ffea82 feat: complete implementation of target selection for display or window capture` introduced target selection service/result patterns. Keep picker cancellation separate from capture failure.
- `2f0e953 feat: implement minimal WGC FP16 capture to live preview` introduced the live WGC/FP16 preview path. Do not weaken it with CPU/bitmap shortcuts for validation.
- `4a42b2c feat: record epic 1 HDR preview validation and harden frame handling` hardened frame handling and swap-chain interop. Keep those safeguards intact.

### Technical Requirements and Latest References

- Keep package/platform settings currently locked in `Directory.Build.props` and `Directory.Packages.props`: `net10.0-windows10.0.19041.0`, `x64`, `win-x64`, `Microsoft.WindowsAppSDK` `1.8.260317003`, `Vortice.Direct3D11` `3.8.3`, `Vortice.DXGI` `3.8.3`, `Microsoft.NET.Test.Sdk` `18.4.0`, xUnit `2.9.3`, and xUnit runner `3.1.5`.
- Microsoft Learn documents `Direct3D11CaptureFramePool` as `IClosable`/`IDisposable` with `Close`, `Dispose`, `CreateFreeThreaded`, `Recreate`, and `TryGetNextFrame`. `CreateFreeThreaded` removes the `DispatcherQueue` dependency and raises `FrameArrived` on the frame pool's worker thread; keep all WinUI mutation marshaled to the UI thread.
- Microsoft Learn documents `IDXGISwapChain::ResizeBuffers` as requiring applications to release all direct and indirect references to back buffers before resize can succeed. Lumiere's validation should continue favoring explicit detach/release evidence over hidden reuse.
- Any resize/recreate or swap-chain validation must preserve `DXGI_FORMAT_R16G16B16A16_FLOAT` and re-establish scRGB color-space evidence; do not accept an 8-bit format as a "stable" lifecycle result.

References:

- `_bmad-output/planning-artifacts/epics.md#Story-2.4-Validate-Repeated-Capture-Lifecycle-Stability`
- `_bmad-output/planning-artifacts/prd.md#Reliability-and-Resource-Lifecycle`
- `_bmad-output/planning-artifacts/prd.md#Journey-4-Developer-Verifies-Pipeline-Stability-During-Repeated-Captures`
- `_bmad-output/planning-artifacts/architecture.md#Resource-Lifecycle-and-Session-Management-FR27-FR31-NFR9-NFR13`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Error-Recovery`
- `_bmad-output/project-context.md#Critical-Dont-Miss-Rules`
- `_bmad-output/implementation-artifacts/2-3-stop-restart-and-recreate-capture-resources.md#Completion-Notes-List`
- Microsoft Learn: https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.direct3d11captureframepool
- Microsoft Learn: https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.direct3d11captureframepool.createfreethreaded
- Microsoft Learn: https://learn.microsoft.com/en-us/windows/win32/api/dxgi/nf-dxgi-idxgiswapchain-resizebuffers

### Testing Requirements

Run from repository root on Windows:

```bash
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false
dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet format Lumiere.sln --verify-no-changes --verbosity minimal
```

Automated tests should cover lifecycle sequencing, repeated validation iterations, state transitions, disposal ordering, stale generation decisions, resize/recreate validation records, and recoverable failure outcomes. Real WinUI, WGC, DXGI, D3D11, HDR display fidelity, multi-monitor behavior, and GPU memory trends require Windows hardware/manual validation. Completion notes must label validation level accurately as `Mac-pass`, `Windows CI-pass`, or `Windows manual-pass`.

### Anti-Patterns to Avoid

- Do not create a new capture service or graphics engine just for validation.
- Do not validate stability by converting frames to CPU bitmaps or SDR formats.
- Do not store `Direct3D11CaptureFrame`, `CapturedFrameTexture`, swap chains, frame pools, or render targets in diagnostic records.
- Do not update WinUI directly from `FrameArrived`.
- Do not hold `previewSync` while performing UI-thread detach or COM disposal.
- Do not claim GPU memory stability from unit tests alone.
- Do not mark real WGC/DXGI/HDR lifecycle behavior complete without Windows manual validation.
- Do not expand into Epic 4 advanced diagnostics UI; keep this story focused on lifecycle stability evidence.

## Dev Agent Record

### Agent Model Used

GPT-5.5

### Debug Log References

- 2026-05-04: Red phase confirmed missing capture lifecycle validation seam with `CaptureLifecycleValidationTests` compile failures.
- 2026-05-04: Red phase confirmed teardown evidence expectations with coordinator return-type compile failures.
- 2026-05-04: Focused lifecycle validation tests passed after adding capture validation records and summary.
- 2026-05-04: Focused capture/presentation lifecycle tests passed after adding teardown evidence return values.
- 2026-05-04: Full Windows CI-pass validation succeeded: `dotnet restore`, `dotnet build`, `dotnet test`, and `dotnet format --verify-no-changes`.
- 2026-05-04: Code review fixes retained production disposal evidence on capture and swap-chain resources, tightened lifecycle summary aggregate validation, and passed `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /m:1 /nr:false`.

### Completion Notes List

- Added capture-owned lifecycle validation models for repeated attempts, final recoverable state inspection, teardown completion, detach-before-release evidence, resource growth evidence, and failed/degraded readiness technical detail.
- Added capture and graphics teardown evidence records returned by existing disposal coordinators while preserving disposal order and retry behavior.
- Added hardware-independent tests for repeated recoverable lifecycle summaries, stuck final states, resource growth evidence, capture teardown evidence, and swap-chain detach-before-release evidence.
- Added `docs/validation/lifecycle-validation.md` manual Windows checklist covering start, stop, cancel, same/different-target restart, frame-size recreate, failed initialization, window close, repeated loops, and GPU memory inspection.
- Validation level: Windows CI-pass completed; Windows manual-pass remains gated on running the new checklist with real WGC/DXGI/D3D11/HDR hardware conditions.

### File List

- docs/validation/lifecycle-validation.md
- src/Lumiere.Capture/CaptureLifecycleAttemptKind.cs
- src/Lumiere.Capture/CaptureLifecycleValidationRecord.cs
- src/Lumiere.Capture/CaptureLifecycleValidationSummary.cs
- src/Lumiere.Capture/CaptureResourceGrowthEvidence.cs
- src/Lumiere.Capture/CaptureSessionDisposalCoordinator.cs
- src/Lumiere.Capture/CaptureSessionDisposalEvidence.cs
- src/Lumiere.Graphics/Presentation/SwapChainDisposalCoordinator.cs
- src/Lumiere.Graphics/Presentation/SwapChainDisposalEvidence.cs
- tests/Lumiere.Graphics.Tests/Capture/CaptureLifecycleTests.cs
- tests/Lumiere.Graphics.Tests/Capture/CaptureLifecycleValidationTests.cs
- tests/Lumiere.Graphics.Tests/Presentation/SwapChainLifecycleTests.cs

### Change Log

- 2026-05-04: Created Story 2.4 context and marked ready for development.
- 2026-05-04: Implemented repeated lifecycle validation records, teardown evidence, focused tests, and manual validation checklist.
