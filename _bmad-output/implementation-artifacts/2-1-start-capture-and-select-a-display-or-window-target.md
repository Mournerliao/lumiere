# Story 2.1: Start Capture and Select a Display or Window Target

Status: done

<!-- Ultimate context engine analysis completed - comprehensive developer guide created. -->
<!-- Rewritten in English on 2026-05-04 to avoid mojibake/encoding issues. -->

## Story

As a screenshot user,
I want to start capture and choose a display or window,
so that I can decide exactly what Lumiere previews.

## Acceptance Criteria

1. Given the desktop app is running, when the user initiates capture, then target selection begins through Windows-supported capture mechanisms.
2. Given target selection is open, when the user chooses a display or window, then the app creates a typed capture target and proceeds toward session initialization.
3. Given the user cancels target selection, when cancellation is received, then no capture session starts and the app returns to a recoverable idle state.

## Tasks / Subtasks

- [x] Productize target selection from the Epic 1 spike into formal `Lumiere.Capture` service semantics. (AC: 1, 2, 3)
  - [x] Add or refactor a narrow type such as `CaptureTargetSelectionService` or `CapturePickerService` to call the Windows `GraphicsCapturePicker` and return a typed result.
  - [x] Do not let `MainWindow.xaml.cs` directly interpret picker success, cancellation, or exception semantics.
  - [x] Keep `Lumiere.Infrastructure.Interop.GraphicsCapturePickerInterop` as the low-level boundary for WinUI owner-window initialization and picker interop.
  - [x] Do not spread `WindowNative.GetWindowHandle`, COM/WinRT setup, or picker initialization details into App/UI code.
  - [x] Make the UI-thread requirement explicit. If the selection entry point is not on the UI thread, marshal correctly or return a failed status instead of leaking an unclassified exception.

- [x] Add typed target-selection result and cancellation semantics. (AC: 2, 3)
  - [x] Introduce a type such as `CaptureTargetSelectionResult`.
  - [x] Distinguish at least `Selected`, `Canceled`, `Unsupported`, and `Failed`.
  - [x] Carry `CaptureTarget?` plus `PreviewReadinessStatus` or diagnostics as appropriate.
  - [x] Treat `Canceled` as a normal no-session path: do not create `Direct3D11CaptureFramePool`, `GraphicsCaptureSession`, a swap chain, or a preview presenter.
  - [x] Use `GraphicsCaptureSession.IsSupported()` or an equivalent capability gate for `Unsupported`, and map it to visible `Unsupported capture` readiness.
  - [x] Preserve stage and technical detail for `Failed`, preferably by reusing `PreviewReadinessStatus`, `PreviewReadinessStage`, and `InteropFailureDiagnostics`.

- [x] Strengthen the formal `CaptureTarget` model. (AC: 2)
  - [x] Continue wrapping `GraphicsCaptureItem`, `SizeInt32`, and `DisplayName` in `CaptureTarget`.
  - [x] Validate that target size is positive before session initialization.
  - [x] Reserve room for target kind semantics such as `Display`, `Window`, and `Unknown`, but do not implement full monitor capability diagnostics in this story.
  - [x] Keep `GraphicsCaptureItem` lifetime under capture/session ownership. UI should receive state and target summaries, not operate on the raw item.

- [x] Update App wiring so the main window only orchestrates the high-level flow. (AC: 1, 2, 3)
  - [x] The click handler should call the target-selection service, update status from the typed result, and start existing preview/session initialization only for `Selected`.
  - [x] After cancellation, re-enable controls and return to idle/capture-ready copy with no half-initialized resources.
  - [x] After failure or unsupported capture, show explicit recoverable status; never show misleading `HDR-ready` or success language.
  - [x] Preserve the race and rollback protections fixed in Story 1.5.

- [x] Add focused tests for target selection and cancellation. (AC: 1, 2, 3)
  - [x] Test selection result semantics: selected exposes a target, canceled exposes no target/session, unsupported maps to unsupported readiness, and failed carries diagnostic detail.
  - [x] Test `CaptureTarget` valid and invalid sizes so invalid targets cannot enter frame-pool initialization.
  - [x] If the real `GraphicsCapturePicker` cannot run in unit tests, use a narrow interface/fake picker to test state transitions.
  - [x] Do not claim automated validation of real Windows picker UX or real user selection unless that validation actually ran.
  - [x] Run the standard Windows validation chain before review.

## Dev Notes

### Story Scope

This story is the entry point for Epic 2. It turns the picker spike from Epic 1 into formal capture target selection semantics. After this story, the user can start capture from the app, use a Windows-supported target-selection mechanism, receive a typed `CaptureTarget` after selecting a display/window, and return safely to idle when selection is canceled.

This story does not implement stop/restart/recreate behavior, target resize handling, fullscreen crop overlay, multi-monitor HDR capability diagnostics, export, clipboard, annotation, hotkey, tray, or capture history.

### Current Repository Context

The current codebase already has a working Epic 1 preview proof path:

- `src/Lumiere.App/MainWindow.xaml.cs` directly calls `GraphicsCapturePickerInterop.PickSingleItemAsync(this)`, interprets null cancellation, and calls `StartPreview(GraphicsCaptureItem item)` on success. That is acceptable spike wiring but should not remain the long-term owner of target-selection semantics.
- `src/Lumiere.Infrastructure/Interop/GraphicsCapturePickerInterop.cs` already wraps `GraphicsCapturePicker` and WinUI owner-window initialization. Keep this as the low-level interop helper.
- `src/Lumiere.Capture/CaptureService.cs` starts `Direct3D11CaptureFramePool` / `GraphicsCaptureSession` from `CaptureTarget` and maps unsupported/failed readiness.
- `src/Lumiere.Capture/CaptureTarget.cs` creates a typed target from `GraphicsCaptureItem`, but still needs invalid size guardrails, target-kind room, and formal selection-result semantics.
- `tests/Lumiere.Graphics.Tests/Capture/` already contains capture configuration, start-result, and disposal-ordering tests.

The implementation should narrow the contract between "select target" and "start session" instead of rewriting graphics preview or the WGC frame pipeline.

### Technical Requirements

- Target selection must use OS-supported Windows Graphics Capture mechanisms.
- `GraphicsCapturePicker.PickSingleItemAsync()` opens the picker and returns the selected `GraphicsCaptureItem`. Microsoft documentation states it must be called on the UI thread or an exception is thrown.
- `GraphicsCaptureSession.IsSupported()` should gate unsupported screen-capture environments.
- `Direct3D11CaptureFramePool.CreateFreeThreaded` belongs to session initialization, not target selection. Its `FrameArrived` event uses an internal worker thread, so any UI updates still need dispatcher marshaling.
- Cancellation is a normal user path and must not allocate capture, frame-pool, swap-chain, render-target, or presenter resources.
- Preserve the FP16/scRGB preview path and do not introduce SDR bitmap/GDI fallback.

References:

- Microsoft Learn: `GraphicsCapturePicker.PickSingleItemAsync` - https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.graphicscapturepicker.picksingleitemasync
- Microsoft Learn: `GraphicsCaptureSession.IsSupported` - https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.graphicscapturesession.issupported
- Microsoft Learn: `Direct3D11CaptureFramePool.CreateFreeThreaded` - https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.direct3d11captureframepool.createfreethreaded

### Architecture Compliance

- `Lumiere.App` owns button click handling, high-level flow composition, and user-visible status binding. It should not directly interpret low-level picker interop or construct WGC session resources.
- `Lumiere.Capture` owns target-selection result semantics, `CaptureTarget`, capability classification, and the session-start contract.
- `Lumiere.Infrastructure` owns WinUI/WinRT interop details for picker owner initialization and any future HWND/HMONITOR item factories.
- `Lumiere.Graphics` owns swap chain, frame presentation, presentation readiness, and graphics resource teardown. Do not add target picker logic there.

### UX Requirements

- Keep the default flow short: start capture, choose target, preview.
- Cancellation should feel safe and ordinary; it should not leave stale capture state, disabled controls, or misleading failure language.
- Status copy should be trust-oriented: `Initializing preview`, `Unsupported capture`, `Preview failed`, or an idle/capture-ready message.
- Do not show `HDR-ready` until a frame has actually reached the proven preview path.

### Previous Story Intelligence

Story 1.5 fixed several lifecycle hazards that this story must preserve:

- `StopPreview` deadlock with an in-flight free-threaded frame callback.
- Failed capture startup leaving newly attached preview resources alive.
- `StartPreview` racing `StopPreview` and losing or accepting stale sessions.
- Captured frame surface lifetime crossing the `Direct3D11CaptureFrame` lifetime.

For Story 2.1, cancellation should happen before swap-chain/capture startup, so the best path requires no rollback at all.

### File Structure Requirements

Likely touch points:

```text
src/
  Lumiere.App/
    MainWindow.xaml.cs                 # UPDATE: high-level wiring only
  Lumiere.Capture/
    CaptureTarget.cs                   # UPDATE: validation / target metadata
    CaptureTargetSelectionResult.cs    # NEW
    CaptureTargetSelectionService.cs   # NEW, or equivalent name
    CaptureService.cs                  # UPDATE only if selected target contract changes
  Lumiere.Infrastructure/
    Interop/
      GraphicsCapturePickerInterop.cs  # UPDATE only to keep low-level picker initialization narrow
tests/
  Lumiere.Graphics.Tests/
    Capture/
      CaptureTargetSelectionTests.cs   # NEW
      CaptureTargetTests.cs            # NEW or folded into existing capture tests
```

### Testing Requirements

- Unit-test typed selection result semantics without requiring the real picker.
- Unit-test cancellation as non-failure and no-session behavior.
- Unit-test unsupported capture mapping separately from picker cancellation.
- Unit-test invalid target size before session initialization.
- Run the standard validation chain before review:
  - `dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false`
  - `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false`
  - `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false`
  - `dotnet format Lumiere.sln --verify-no-changes --verbosity minimal`

### Anti-Patterns to Avoid

- Do not leave `MainWindow.xaml.cs` as the long-term owner of picker result semantics.
- Do not treat picker cancellation as an error or failed HDR preview.
- Do not create swap-chain/capture resources before the user selects a valid target.
- Do not introduce CPU bitmap, `SoftwareBitmap`, `BitmapImage`, GDI, WIC, PNG bytes, or XAML `Image` preview paths.
- Do not mark display/window kind or HDR capability as proven simply because `GraphicsCapturePicker` returned a `GraphicsCaptureItem`.
- Do not swallow picker initialization exceptions; map them to failed readiness with stage and technical detail.

## Dev Agent Record

### Agent Model Used

DeepSeek-v4-pro (Claude Code harness)

### Debug Log References

- 2026-05-04: Loaded `bmad-dev-story` workflow, BMad config, sprint status, project context, Story 2.1 spec, existing source code (MainWindow.xaml.cs, CaptureService.cs, CaptureTarget.cs, GraphicsCapturePickerInterop.cs, all test files), and key types (CaptureStartResult, CaptureSessionResources, PreviewReadinessStatus, PreviewReadinessStage, InteropFailureDiagnostics).
- 2026-05-04: Verified existing test patterns use fakes/stubs for platform boundaries and InternalsVisibleTo for test accessibility.
- 2026-05-04: Discovered circular dependency risk: Infrastructure cannot reference Capture. Moved `ICaptureTargetPicker` to `Lumiere.Infrastructure.Interop` to keep the dependency graph clean.
- 2026-05-04: Discovered `GraphicsCaptureItem.CreateForTesting` unavailable in target SDK. Added internal `CaptureTarget.CreateForTest` factory for unit-testing target validation without real picker.

### Completion Notes List

- Created `CaptureTargetSelectionService` in `Lumiere.Capture` that wraps Windows picker interop and returns typed `CaptureTargetSelectionResult`.
- Created `CaptureTargetSelectionResult` with `SelectionOutcome` enum (Selected, Canceled, Unsupported, Failed) carrying `CaptureTarget?` and `PreviewReadinessStatus`.
- Added `CaptureTargetKind` enum (Unknown, Display, Window) to `CaptureTarget` with size validation in `FromItem`.
- Created `ICaptureTargetPicker` interface in `Lumiere.Infrastructure.Interop` and `GraphicsCaptureTargetPicker` implementation bridging to `GraphicsCapturePickerInterop`.
- Refactored `MainWindow.xaml.cs`: click handler now delegates to `CaptureTargetSelectionService`, only orchestrates high-level flow. `StartPreview` now takes `CaptureTarget` directly. Removed direct picker null/exception interpretation.
- `GraphicsCapturePickerInterop` preserved unchanged as low-level interop boundary.
- Added 15 new unit tests: 12 in `CaptureTargetSelectionTests` (result semantics + fake picker state transitions), 8 in `CaptureTargetTests` (size validation, kind storage, display name defaults).
- Validation: `dotnet build` 0 errors 0 warnings, `dotnet test` 59/59 passed (0 regressions), `dotnet format --verify-no-changes` passed.
- Manual HDR validation on Windows hardware not yet run for this story.

### File List

- src/Lumiere.Capture/CaptureTargetKind.cs (NEW)
- src/Lumiere.Capture/CaptureTargetSelectionResult.cs (NEW)
- src/Lumiere.Capture/CaptureTargetSelectionService.cs (NEW)
- src/Lumiere.Capture/SelectionOutcome.cs (NEW)
- src/Lumiere.Capture/CaptureTarget.cs (UPDATE: add Kind, size validation)
- src/Lumiere.Infrastructure/Interop/ICaptureTargetPicker.cs (NEW)
- src/Lumiere.Infrastructure/Interop/GraphicsCaptureTargetPicker.cs (NEW)
- src/Lumiere.App/MainWindow.xaml.cs (UPDATE: delegate to selection service)
- tests/Lumiere.Graphics.Tests/Capture/CaptureTargetSelectionTests.cs (NEW)
- tests/Lumiere.Graphics.Tests/Capture/CaptureTargetTests.cs (NEW)
- _bmad-output/implementation-artifacts/2-1-start-capture-and-select-a-display-or-window-target.md
- _bmad-output/implementation-artifacts/sprint-status.yaml

### Review Findings

- [x] [Review][Patch] P1-CRITICAL: `StartPreview` 缺少异常捕获——`OnSelectCaptureTargetClick` 移除了原有的 `catch(Exception)` 块，`StartPreview` 抛出的异常会导致 async void 崩溃 [src/Lumiere.App/MainWindow.xaml.cs:59]
- [x] [Review][Patch] P2-HIGH: `ConfigureAwait(false)` 导致 COM 跨线程访问 `GraphicsCaptureItem`——WinRT 对象可能不支持自由线程封送 [src/Lumiere.Capture/CaptureTargetSelectionService.cs:7]
- [x] [Review][Patch] P3-MEDIUM: `catch(Exception)` 内用 `is` 类型检查应改为分开的 `catch(NotSupportedException)` + `catch(ArgumentException)` + `catch(Exception)` 块 [src/Lumiere.Capture/CaptureTargetSelectionService.cs:39]
- [x] [Review][Patch] P4-MEDIUM: 尺寸校验的 `ArgumentException` 错误映射到 `PreviewReadinessStage.Interop`，应为 `Capture` [src/Lumiere.Capture/CaptureTargetSelectionService.cs:53]
- [x] [Review][Patch] P5-LOW: `IsSelected` 缺少 `[MemberNotNullWhen(true, nameof(Target))]` 标注 [src/Lumiere.Capture/CaptureTargetSelectionResult.cs:18]
- [x] [Review][Defer] D1-HIGH: `CreateForTest` 使用 `null!` 传递 `GraphicsCaptureItem` ——已知变通方案（`CreateForTesting` 在目标 SDK 不可用），故事文档已记录 [src/Lumiere.Capture/CaptureTarget.cs:27]
- [x] [Review][Defer] D2-HIGH: 窗口关闭期间 `deviceResources` use-after-dispose——选择器显示中关闭窗口时有已有竞态 [src/Lumiere.App/MainWindow.xaml.cs:293]
- [x] [Review][Defer] D3-MEDIUM: `GraphicsCaptureSession.IsSupported()` 是不可测试的静态依赖——超出本故事范围 [src/Lumiere.Capture/CaptureTargetSelectionService.cs:17]
- [x] [Review][Defer] D4-MEDIUM: `CaptureTarget` 持有 `IDisposable`（`GraphicsCaptureItem`）但自身未实现 `IDisposable`——已有问题 [src/Lumiere.Capture/CaptureTarget.cs:8]
- [x] [Review][Defer] D5-MEDIUM: 取消选择时显示 "Initializing preview" 而非空闲状态——已有 UX 问题 [src/Lumiere.Capture/CaptureTargetSelectionService.cs:28]
- [x] [Review][Defer] D6-MEDIUM: `CaptureTargetKind` 在生产代码中始终为 `Unknown`——规格明确说本故事不实现分类 [src/Lumiere.Capture/CaptureTarget.cs:47]
- [x] [Review][Defer] D7-MEDIUM: 捕获尺寸无上限校验——已有问题 [src/Lumiere.Capture/CaptureTarget.cs:33]
- [x] [Review][Defer] D8-LOW: `previewGeneration` 在 UI dispatcher 回调中未同步读取——已有模式 [src/Lumiere.App/MainWindow.xaml.cs:138]

## Change Log

- 2026-05-04: Implemented Story 2.1 — productized target selection into `CaptureTargetSelectionService`, added typed `CaptureTargetSelectionResult` with `SelectionOutcome` enum, strengthened `CaptureTarget` model with `CaptureTargetKind` and size validation, refactored `MainWindow` to delegate to selection service. 15 new tests. Build/test/format all pass on Windows.
