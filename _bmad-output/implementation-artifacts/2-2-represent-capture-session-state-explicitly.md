# Story 2.2: Represent Capture Session State Explicitly

Status: done

<!-- Ultimate context engine analysis completed - comprehensive developer guide created. -->

## Story

As a user,
I want the app to distinguish normal, degraded, unsupported, and failed capture states,
so that I am not misled about whether the preview can be trusted.

## Acceptance Criteria

1. Given capture is initialized, when platform capability checks pass, then the session state moves to normal capturing state.
2. Given screen capture is unavailable, when the app attempts to initialize capture, then the session enters `Unsupported` with a concise user-facing reason.
3. Given the app can capture but cannot prove HDR correctness, when validation fails, then the session enters `Degraded` and no silent SDR fallback is presented as valid.

## Tasks / Subtasks

- [x] Add an explicit capture session state model in `Lumiere.Capture`. (AC: 1, 2, 3)
  - [x] Introduce `CaptureSessionStatus` or equivalent with at least `Idle`, `SelectingTarget`, `Initializing`, `Capturing`, `Degraded`, `Unsupported`, `Failed`, and `Disposed`.
  - [x] Introduce an immutable `CaptureSessionState` snapshot that carries status, `CaptureTarget?`, `PreviewReadinessStatus`, optional user-facing reason, and optional technical detail.
  - [x] Keep session state separate from `CaptureStartResult.Started`; `Started` only means native session resources exist, not that the preview is HDR-trustworthy.
  - [x] Reuse `PreviewReadinessStatus`, `PreviewReadinessState`, and `PreviewReadinessStage` instead of creating a parallel diagnostics vocabulary.

- [x] Map target selection and capture startup outcomes into session state. (AC: 1, 2)
  - [x] Map picker cancellation to recoverable `Idle`, not `Failed`.
  - [x] Map `CaptureTargetSelectionResult.Unsupported` and `CaptureStartResult.NotStarted` with unsupported readiness to `Unsupported`.
  - [x] Map selection/start failures to `Failed` with preserved stage and technical detail.
  - [x] Map successful `StartCapture` initialization to `Initializing`, then move to `Capturing` only after frame presentation reports ready evidence.

- [x] Represent degraded preview trust explicitly. (AC: 3)
  - [x] When graphics, presentation, or HDR validation returns `PreviewReadinessState.Degraded`, reflect `CaptureSessionState.Status == Degraded`.
  - [x] Do not show `HDR-ready` or `Capturing` for degraded status.
  - [x] Preserve existing FP16/scRGB requirements; degraded state is a visible warning, not permission to use SDR bitmap/GDI fallback.

- [x] Update app wiring to consume typed session state. (AC: 1, 2, 3)
  - [x] Refactor `MainWindow.xaml.cs` so status labels are derived from `CaptureSessionState`, not scattered nullable resources or ad hoc strings.
  - [x] Keep `MainWindow` as high-level flow orchestration only; do not move WGC, D3D11, DXGI, or picker interop details into UI code.
  - [x] Preserve Story 1.5/2.1 race guards: generation checks, `StopPreview` rollback, free-threaded frame callback dispatching, and no resource allocation after picker cancellation.

- [x] Add focused tests for state transitions and trust semantics. (AC: 1, 2, 3)
  - [x] Test normal transition: selected target + capture start + ready presentation maps to `Capturing`.
  - [x] Test unsupported transition: support check or capture start unsupported maps to `Unsupported` and exposes concise user message.
  - [x] Test degraded transition: degraded readiness maps to `Degraded` and `RequiresUserAttention`.
  - [x] Test failure transition preserves `PreviewReadinessStage` and technical detail.
  - [x] Test cancellation returns `Idle` and does not imply native session resources exist.
  - [x] Run the standard Windows validation chain before review.

### Review Findings

- [x] [Review][Patch] Preserve degraded/failed presentation evidence instead of overwriting it with capture readiness [src/Lumiere.App/MainWindow.xaml.cs:114]
- [x] [Review][Patch] Guard post-await UI updates when target selection completes after the window has closed [src/Lumiere.App/MainWindow.xaml.cs:73]
- [x] [Review][Patch] Preserve the active target when unsupported readiness is reported during an active session [src/Lumiere.Capture/CaptureSessionState.cs:125]

## Dev Notes

### Story Scope

Story 2.2 creates the formal state contract for capture trust. It should turn the current "selection result + capture start result + preview readiness + UI labels" pieces into one explicit session snapshot that later stories can use for stop/restart/recreate lifecycle behavior, overlay status, diagnostics, and manual validation.

This story does not implement stop/restart/resource recreation, target resize handling, fullscreen crop overlay, monitor capability detection, settings, export, clipboard, hotkeys, tray, annotation, or capture history.

### Current Repository Context

Relevant current implementation:

- `src/Lumiere.Capture/CaptureTargetSelectionResult.cs` already distinguishes `Selected`, `Canceled`, `Unsupported`, and `Failed`, carries `CaptureTarget?`, and uses `PreviewReadinessStatus`.
- `src/Lumiere.Capture/CaptureStartResult.cs` distinguishes `Started` vs. not started and carries `CaptureSessionResources?` plus readiness, but it is not a full session state model.
- `src/Lumiere.Capture/CaptureService.cs` gates `GraphicsCaptureSession.IsSupported()`, creates `Direct3D11CaptureFramePool.CreateFreeThreaded`, maps unsupported/failed readiness, and reports frame failures from the free-threaded callback.
- `src/Lumiere.Graphics/Hdr/PreviewReadinessStatus.cs` already models `Initializing`, `Ready`, `Degraded`, `Unsupported`, and `Failed`; `RequiresUserAttention` is true for degraded/unsupported/failed.
- `src/Lumiere.App/MainWindow.xaml.cs` currently owns most active session facts through fields such as `captureSession`, `swapChainResources`, `previewFramePresenter`, `previewGeneration`, and user-facing label mapping in `ApplyReadiness`.
- Tests under `tests/Lumiere.Graphics.Tests/Capture/` and `tests/Lumiere.Graphics.Tests/Hdr/` already cover target selection, capture lifecycle result basics, and readiness semantics.

Likely new files:

```text
src/Lumiere.Capture/CaptureSessionStatus.cs
src/Lumiere.Capture/CaptureSessionState.cs
tests/Lumiere.Graphics.Tests/Capture/CaptureSessionStateTests.cs
```

Likely updated files:

```text
src/Lumiere.App/MainWindow.xaml.cs
src/Lumiere.Capture/CaptureStartResult.cs
src/Lumiere.Capture/CaptureTargetSelectionResult.cs
tests/Lumiere.Graphics.Tests/Capture/CaptureLifecycleTests.cs
tests/Lumiere.Graphics.Tests/Capture/CaptureTargetSelectionTests.cs
```

### Architecture Compliance

- `Lumiere.Capture` owns capture session state semantics and result mapping.
- `Lumiere.App` observes session state and renders user-facing status; it must not own low-level capture classification.
- `Lumiere.Graphics` remains the owner of HDR readiness evidence from presentation and color-space validation.
- `Lumiere.Infrastructure` remains the owner of WinRT/COM/WinUI interop helpers and diagnostic formatting.
- Do not introduce `Lumiere.Overlay` behavior in this story; Epic 3 consumes this state later.

### State Mapping Guidance

Use this mapping unless implementation discovers a stronger local pattern:

| Source condition | Session status | Readiness state |
| --- | --- | --- |
| App awaiting user action | `Idle` | `Initializing` with ready-to-capture copy |
| Picker open | `SelectingTarget` | `Initializing` |
| Target selected, native resources starting | `Initializing` | `Initializing` |
| Capture started and ready frame/presentation evidence exists | `Capturing` | `Ready` |
| Capability unsupported | `Unsupported` | `Unsupported` |
| HDR correctness cannot be proven | `Degraded` | `Degraded` |
| Native/interop/start/frame failure | `Failed` | `Failed` |
| Window/session disposed | `Disposed` | `Failed` or `Initializing` only if needed for internal tests; UI should not show active capture |

Do not equate WGC session start with HDR correctness. A WGC session can exist while preview trust is still `Initializing`, `Degraded`, or `Failed`.

### UX Requirements

- User-facing labels should stay direct: `Ready to capture`, `Initializing preview`, `HDR-ready`, `Degraded preview`, `Unsupported capture`, `Preview failed`.
- Degraded and unsupported states must be visible and must not use success language.
- Cancellation should feel ordinary and should return to a recoverable idle state.
- Technical detail belongs in diagnostics text; default status should be concise and non-technical.
- Do not resize or shift preview/crop surfaces as status changes; this matters for Epic 3 coordinate stability.

### Technical Requirements and Latest References

- Keep package versions currently locked in `Directory.Packages.props`: `Microsoft.WindowsAppSDK` `1.8.260317003`, `Vortice.Direct3D11` `3.8.3`, `Vortice.DXGI` `3.8.3`, xUnit `2.9.3`, and xUnit runner `3.1.5`.
- Microsoft's Windows App SDK downloads page currently lists stable `1.8.6 (1.8.260317003)` released 2026-03-18, matching this repository's lock.
- NuGet currently lists `Vortice.Direct3D11` `3.8.3` as the latest stable package and compatible with `net10.0`; keep it rather than upgrading or adding a new graphics wrapper.
- Microsoft Learn describes `GraphicsCaptureSession.IsSupported()` as the capability check for screen capture support and `StartCapture()` as starting an existing session.
- Microsoft Learn describes `Direct3D11CaptureFramePool.CreateFreeThreaded` as removing the `DispatcherQueue` dependency and raising `FrameArrived` on the frame pool's internal worker thread. Continue treating frame callbacks as non-UI-thread callbacks.
- Microsoft Learn lists `DirectXPixelFormat.R16G16B16A16Float` as the WinRT equivalent of `DXGI_FORMAT_R16G16B16A16_FLOAT`; do not weaken the FP16 capture path.

References:

- `_bmad-output/planning-artifacts/epics.md#Story-2.2-Represent-Capture-Session-State-Explicitly`
- `_bmad-output/planning-artifacts/prd.md#Functional-Requirements`
- `_bmad-output/planning-artifacts/architecture.md#Core-Architectural-Decisions`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Feedback-Patterns`
- `_bmad-output/project-context.md#Critical-Implementation-Rules`
- `_bmad-output/implementation-artifacts/2-1-start-capture-and-select-a-display-or-window-target.md#Completion-Notes-List`
- Microsoft Learn: https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads
- Microsoft Learn: https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.graphicscapturesession
- Microsoft Learn: https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.direct3d11captureframepool.createfreethreaded
- Microsoft Learn: https://learn.microsoft.com/en-us/uwp/api/windows.graphics.directx.directxpixelformat
- NuGet: https://www.nuget.org/packages/Vortice.Direct3D11/

### Previous Story Intelligence

Story 2.1 established the right boundary for target selection:

- `CaptureTargetSelectionService` owns picker outcome classification.
- `MainWindow.xaml.cs` should delegate high-level target selection rather than interpret picker null/exception semantics directly.
- Unsupported capture should be checked before picker/session work where possible.
- Cancellation is a normal no-session path.
- `GraphicsCaptureItem.CreateForTesting` was unavailable in the target SDK, so tests use `CaptureTarget.CreateForTest`.
- Review feedback already fixed broad catch ordering, COM continuation risk, and nullable target ergonomics. Preserve those improvements.

Deferred review notes relevant to Story 2.2:

- `GraphicsCaptureSession.IsSupported()` may have nuance beyond a simple Boolean; keep the result as a typed unsupported state with technical detail, not as a generic failure.
- Current UI copy can say `Initializing preview` before actual session work; session state should make "selecting target", "initializing capture", and "capturing" distinct.
- `CaptureTargetKind` remains `Unknown`; do not treat monitor/window HDR capability as known in this story.

### Git Intelligence

Recent commits show the implementation pattern to follow:

- `9ffea82 feat: complete implementation of target selection for display or window capture` created the current target-selection service/result pattern.
- `2f0e953 feat: implement minimal WGC FP16 capture to live preview` and `4a42b2c feat: record epic 1 HDR preview validation and harden frame handling` established the preview proof path.
- `0a36ff4 fix: resolve Epic 1 review findings - StopPreview deadlock, StartPreview race, and failed StartCapture rollback` fixed lifecycle hazards that this story must not regress.

### Testing Requirements

Run from repository root on Windows:

```bash
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false
dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet format Lumiere.sln --verify-no-changes --verbosity minimal
```

Automated tests may verify state mapping and resource-independent lifecycle behavior. Real WinUI, WGC, DXGI, D3D11, HDR display fidelity, and multi-monitor behavior still require Windows hardware/manual validation. Completion notes must label validation level accurately.

### Anti-Patterns to Avoid

- Do not create a new diagnostics/status system parallel to `PreviewReadinessStatus`.
- Do not report `Capturing` or `HDR-ready` only because `GraphicsCaptureSession.StartCapture()` returned.
- Do not hide degraded/unsupported state behind a generic exception or success label.
- Do not treat picker cancellation as failure.
- Do not introduce SDR fallback, CPU readback, `SoftwareBitmap`, `BitmapImage`, GDI, WIC, PNG bytes, or XAML `Image` preview paths.
- Do not make `MainWindow.xaml.cs` the owner of capture state transition rules.
- Do not implement Epic 2.3 stop/restart/recreate semantics in this story except for preserving existing cleanup behavior.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-04: Added capture session state model and state transition tests.
- 2026-05-04: Refactored `MainWindow.xaml.cs` status rendering to consume `CaptureSessionState`.
- 2026-05-04: Ran Windows validation chain; an initial parallel build/test attempt hit a transient WinAppSDK `priconfig.xml.intermediate` file lock, then sequential build passed.

### Completion Notes List

- Implemented `CaptureSessionStatus` and immutable `CaptureSessionState` in `Lumiere.Capture`.
- Mapped target selection, capture start, readiness, degraded, unsupported, failed, cancellation, and disposed semantics through typed session snapshots.
- Updated `MainWindow.xaml.cs` so UI status labels/messages are derived from `CaptureSessionState`, while preserving generation checks, cleanup rollback, and free-threaded frame dispatch.
- Added focused xUnit coverage for normal, unsupported, degraded, failed, canceled, and unsupported-start session transitions.
- Windows CI-pass: restore, build, test, and format verification passed on Windows. Windows manual-pass for real WGC/DXGI/HDR display behavior was not run.

### File List

- src/Lumiere.App/MainWindow.xaml.cs
- src/Lumiere.Capture/CaptureSessionState.cs
- src/Lumiere.Capture/CaptureSessionStatus.cs
- tests/Lumiere.Graphics.Tests/Capture/CaptureSessionStateTests.cs
- _bmad-output/implementation-artifacts/2-2-represent-capture-session-state-explicitly.md
- _bmad-output/implementation-artifacts/sprint-status.yaml

### Change Log

- 2026-05-04: Created Story 2.2 context and marked ready for development.
- 2026-05-04: Implemented explicit capture session state model and marked story ready for review.
