# Story 3.5: Manage Overlay Hit Testing and Keyboard Escape

Status: done

<!-- Ultimate context engine analysis completed - comprehensive developer guide created. -->

## Story

As a screenshot user,
I want overlay input to work reliably,
so that transparency or topmost behavior does not prevent crop interaction or cancellation.

## Acceptance Criteria

1. Given the overlay uses transparent or borderless window behavior, when the user interacts with the crop canvas, then hit testing routes input to crop controls instead of passing all input through the window.
2. Given the overlay is active, when the user presses the cancel key, then the capture flow exits safely.
3. Given controls are visible, when keyboard navigation is used where practical for MVP, then the user is not trapped without a cancel path.

## Tasks / Subtasks

- [x] Add an explicit overlay hit-test mode boundary owned by `Lumiere.Overlay.Windowing`. (AC: 1)
  - [x] Add a small type such as `OverlayHitTestMode` or equivalent under `src/Lumiere.Overlay/Windowing/`.
  - [x] Keep the default MVP mode interactive for crop input; do not apply whole-window click-through behavior while crop selection is possible.
  - [x] If Win32 extended styles are introduced, keep P/Invoke and style mutation behind a narrow infrastructure/windowing API, not inside crop code.
  - [x] Surface a technical detail string from presenter application that says whether interactive hit testing or pass-through behavior is active.

- [x] Harden `OverlayWindowPresenter` without weakening existing borderless/topmost behavior. (AC: 1)
  - [x] Preserve `OverlappedPresenter.Create()`, `SetBorderAndTitleBar(false, false)`, `IsAlwaysOnTop = true`, and `AppWindow.MoveAndResize(...)`.
  - [x] Keep display placement through `OverlayPlacementRequest` and `SelectOverlayBounds`; do not replace the multi-monitor placement seam.
  - [x] Add automated tests for hit-test mode selection and presenter diagnostics where hardware-independent.
  - [x] Do not change capture target selection, WGC session ownership, or DXGI swap-chain creation.

- [x] Make keyboard Escape cancellation robust even when focus moves to overlay controls. (AC: 2, 3)
  - [x] Preserve the existing `CloseRequested` event as the cancel path and keep `CaptureConfirmed` separate.
  - [x] Add handling that catches Escape from `RootGrid`, `ConfirmButton`, `CancelButton`, status/details controls, and any future focused child where practical.
  - [x] Consider `KeyboardAccelerator` or a shared preview/key handler so Escape does not depend only on `RootGrid` focus.
  - [x] Ensure repeated Escape presses are idempotent through the existing `isClosingRequested` guard.

- [x] Preserve crop pointer routing and gesture-ending semantics. (AC: 1)
  - [x] Keep pointer handling in `OverlayWindow.xaml.cs` thin and continue delegating crop state to `CropController`.
  - [x] Keep `CropCanvas.Background="Transparent"` and `IsHitTestVisible="True"` or an equivalent explicit hit-test surface so the canvas receives pointer input over the `SwapChainPanel`.
  - [x] Keep pointer capture from `PointerPressed` and continue ending gestures through `PointerReleased`, `PointerCanceled`, and `PointerCaptureLost`.
  - [x] Do not let status panel, toolbar, or future diagnostics controls consume pointer events intended for crop canvas outside their visible bounds.

- [x] Maintain accessibility and visible cancel affordances. (AC: 2, 3)
  - [x] Keep `Cancel` keyboard reachable whenever the overlay can safely close.
  - [x] Keep `Confirm crop` enabled only for valid confirmable crop/status combinations; this story does not change confirm semantics.
  - [x] Ensure focus is restored or moved predictably when the overlay opens and when diagnostics/failure UI is present.
  - [x] Status must not rely on color alone; no change should make degraded/unsupported/failure states appear successful.

- [x] Add tests and manual validation notes. (AC: 1, 2, 3)
  - [x] Extend `tests/Lumiere.Overlay.Tests/` for hit-test mode defaults, presenter diagnostics, Escape command routing seams, and idempotent cancel state where unit-testable.
  - [x] Preserve existing overlay crop creation, adjustment, confirm, layout, placement, and state tests.
  - [x] Update `docs/validation/overlay-validation.md` with Story 3.5 checks for transparent/borderless hit testing, Escape from focused buttons/status controls, repeated Escape, high-DPI, HDR/SDR, and multi-monitor overlays.

### Review Findings

- [ ] [Review][Patch] Real display selections never use display-specific overlay placement [src/Lumiere.Overlay/Windowing/OverlayWindowPresenter.cs:64]

#### Code review 2026-05-05

- [x] [Review][Decision] **Preview surface moved from MainWindow to overlay window** — 已接受。overlay window 承载预览是其天然职责。 [`src/Lumiere.App/MainWindow.xaml`]
- [x] [Review][Decision] **Lumiere.Overlay 新增对 Lumiere.Capture 的依赖** — 已解耦。移除 project reference，将 `FromCaptureSession` 和 `FromTarget` 上移到 App 层，`OverlayPlacementRequest` 改用 `bool IsDisplayTarget` 替代 `CaptureTargetKind`。
- [x] [Review][Patch] **ApplySessionState 重入风险** — 已修复。添加 `applyingSessionState` 守卫旗标，防止 `RequiresFailureTeardown` 触发重入。 [`src/Lumiere.App/MainWindow.xaml.cs:~440`]
- [x] [Review][Patch] **OnOverlayCloseRequested 缺少 isClosed 守卫** — 已修复。添加 `if (isClosed) return;` 提前返回。 [`src/Lumiere.App/MainWindow.xaml.cs:OnOverlayCloseRequested`]
- [x] [Review][Patch] **EnsureOverlayWindow 未校验 target 尺寸** — 已修复。添加 `<= 0` 校验提前返回。 [`src/Lumiere.App/MainWindow.xaml.cs:EnsureOverlayWindow`]
- [x] [Review][Patch] **OnOverlayCaptureConfirmed 中 StopPreview 异常会泄漏 overlay** — 已修复。`StopPreview` 移入 `try/finally`，确保 always close。 [`src/Lumiere.App/MainWindow.xaml.cs:OnOverlayCaptureConfirmed`]
- [x] [Review][Patch] **DisposeAfterFailedUiDetach 异常导致脏状态** — 已修复。`releaseResources()` 移入 `try/finally`，确保 `disposed` 和 `DisposalEvidence` 始终设置。 [`src/Lumiere.Graphics/Presentation/SwapChainResources.cs:DisposeAfterFailedUiDetach`]
- [x] [Review][Defer] **SwapChainResources 内部构造函数使用 null!** — `SwapChain = null!` 仅用于测试场景的内部构造函数，非生产路径。 — deferred, test-only internal constructor pattern
- [x] [Review][Defer] **overlayWindow 字段跨线程非同步访问** — `TryEnqueueUi` 在 frame 回调线程读取，`EnsureOverlayWindow`/`CloseOverlayWindow` 在 UI 线程写入，无显式同步。此模式延续了已有的 `captureSessionResources` 等字段的模式。 — deferred, extends pre-existing pattern
- [x] [Review][Defer] **CaptureSessionResources Action 适配器总是返回完整成功证据** — 接受 `Action` 的内部构造函数无条件返回四字段全 true 的证据，与内部委托的实际行为无关。此路径仅用于向后兼容的测试。 — deferred, internal backward-compat path
- [x] [Review][Defer] **DisposeAfterFailedUiDetach 缺少诊断日志** — UI 线程不可用时静默释放资源，无日志记录失败的 detach，生产排障困难。 — deferred, future diagnostic enhancement
- [x] [Review][Defer] **TryEnqueueUi 静默回退 DispatcherQueue** — overlay 的 DispatcherQueue 拒绝排队时静默路由到 RootGrid.DispatcherQueue，无日志或线程亲和性提示。 — deferred, design choice
- [x] [Review][Defer] **DisposalEvidence 生产环境无消费者** — 所有 Capture/Graphics 的 evidence 记录仅在测试中断言，生产路径中仅存储未读取。 — deferred, infrastructure for future diagnostics
- [x] [Review][Defer] **OnOverlayCaptureConfirmed 过早设置 Disposed 状态** — `StopPreview(reportStopped: false)` 触发异步资源清理后立即设置 `Disposed` 状态，此时资源可能仍在清理中。这与已有模式一致。 — deferred, consistent with existing async disposal pattern

## Dev Notes

### Story Scope

Story 3.5 finishes the Epic 3 fullscreen overlay safety layer: the overlay must remain interactive even when it is borderless, topmost, transparent-looking, or layered over a GPU-backed `SwapChainPanel`, and the user must always have a reliable keyboard escape path.

This story does not implement export, clipboard output, annotation, global hotkeys, tray workflow, capture history, HDR still-image encoding, SDR tone mapping, diagnostics preferences, or advanced diagnostics UI. It also should not rewrite crop geometry, confirm payloads, capture lifecycle, graphics lifecycle, or HDR constants.

### Current Repository Context

The current implementation already has the main overlay pieces this story should harden rather than replace:

- `src/Lumiere.Overlay/OverlayWindow.xaml` has `PreviewSwapChainPanel` as the base layer, `CropCanvas` above it with `Background="Transparent"` and pointer handlers, and status controls with `Confirm crop` and `Cancel`.
- `src/Lumiere.Overlay/OverlayWindow.xaml.cs` owns overlay state application, Escape handling through `RootGrid.KeyDown`, `CloseRequested`, `CaptureConfirmed`, crop pointer capture, pointer cancel/capture-loss handling, and idempotent close guards.
- `src/Lumiere.Overlay/Windowing/OverlayWindowPresenter.cs` applies a borderless always-on-top `OverlappedPresenter`, selects display bounds, and moves/resizes the overlay.
- `src/Lumiere.App/MainWindow.xaml.cs` owns overlay lifetime, handles `CloseRequested` by stopping preview and closing the overlay, and handles `CaptureConfirmed` separately.
- `tests/Lumiere.Overlay.Tests/` already covers crop controller behavior, confirm payload creation, placement selection, preview layout, and overlay state mapping.

Likely changed or new files:

```text
src/Lumiere.Overlay/Windowing/OverlayHitTestMode.cs
src/Lumiere.Overlay/Windowing/OverlayWindowPresenter.cs
src/Lumiere.Overlay/OverlayWindow.xaml
src/Lumiere.Overlay/OverlayWindow.xaml.cs
tests/Lumiere.Overlay.Tests/OverlayPlacementRequestTests.cs
tests/Lumiere.Overlay.Tests/OverlayInputTests.cs
docs/validation/overlay-validation.md
```

Avoid changing:

```text
src/Lumiere.Capture/*
src/Lumiere.Graphics/Hdr/HdrConstants.cs
src/Lumiere.Graphics/Presentation/SwapChainManager.cs
src/Lumiere.Graphics/Presentation/GraphicsEngine.cs
src/Lumiere.Infrastructure/Interop/SwapChainPanelNativeInterop.cs
Directory.Packages.props
```

### Hit-Testing Requirements

- The MVP overlay should be interactive by default. A full-window `WS_EX_TRANSPARENT`-style pass-through mode would break crop creation unless it is narrowly scoped and disabled during crop selection.
- The crop canvas must remain above `SwapChainPanel` in XAML z-order and must have an explicit hit-testable surface.
- Standard controls can receive input over their visible bounds, but transparent empty areas should continue to allow crop gestures on the crop canvas.
- If a future pass-through mode is introduced for non-interactive overlay states, it must be explicit in state/model names and must not be active during `HdrReady` or `DegradedPreview` crop interaction.
- Presenter/windowing code may know about AppWindow, HWND, topmost, borderless, and native style details; crop controller code must not.

### Keyboard Escape Requirements

- Escape should route to the same cancel semantics as `Cancel`, which means `CloseRequested` -> app-level `StopPreview()` -> `CloseOverlayWindow()`.
- Escape should work when focus is on the root, `Confirm crop`, `Cancel`, status text/details, or any added overlay control where practical.
- Repeated Escape must be safe and should not double-dispose WGC, swap-chain, frame-pool, WinRT, COM, D3D11, or DXGI resources.
- Escape during crop creation or adjustment should cancel the overlay flow safely; it does not need to preserve a partial in-flight crop.
- Do not merge confirm and cancel into one untyped close event.

### Architecture Compliance

- `Lumiere.Overlay` owns overlay UI behavior, crop input routing, keyboard escape behavior, and overlay windowing coordination.
- `Lumiere.App` owns app composition, preview start/stop, overlay lifetime, and app-visible status updates.
- `Lumiere.Capture` remains the only owner of WGC target/session/frame lifecycle.
- `Lumiere.Graphics` remains the only owner of D3D11/DXGI rendering, FP16/scRGB swap-chain resources, presentation, and HDR constants.
- `Lumiere.Infrastructure` owns native interop helpers if this story needs HWND/style APIs.
- No UI code should create capture sessions, D3D11 devices, DXGI swap chains, or WGC frame pools.

### UX Requirements

Use `_bmad-output/planning-artifacts/ux-design-specification.md` as implementation input:

- A fullscreen overlay must always provide safe cancel and keyboard escape behavior.
- The captured content stays central; controls support crop interaction without becoming an editor.
- Crop handles, mask, and status must remain visible over bright, dark, and high-contrast HDR content.
- Toolbar/status/diagnostics changes must not resize `SwapChainPanel` or alter crop coordinate mapping.
- Confirm, cancel, retry, and details controls should use standard WinUI focus behavior.
- Status text must identify `HDR-ready`, `Degraded preview`, `Unsupported capture`, or `Preview failed` without relying only on color.

### Previous Story Intelligence

Story 3.4 established:

- Confirm is an in-app MVP output state only; no export/clipboard/annotation/output semantics.
- `ConfirmedCaptureSelection` reuses existing crop state and coordinate mapping.
- Confirm is allowed only for valid active crops in `HdrReady` or `DegradedPreview`.
- `CloseRequested` remains the cancel path; `CaptureConfirmed` remains the confirm path.
- `isClosingRequested` guards repeated confirm/cancel.
- Overlay crop code must not dispose native capture or graphics resources.

Story 3.3 established:

- Do not create a second crop controller or duplicate crop geometry model.
- Pointer gestures are owned by one active pointer and must conclude through release, cancellation, or capture loss.
- Outside-crop recreation preserves the previous active crop until a valid replacement commits.
- Coordinate mapping uses the existing `CropCoordinateMapper`; do not move mapping into windowing or app code.

Earlier lifecycle stories established:

- Preserve `previewGeneration` stale-callback checks.
- Capture teardown disposes WGC session/frame pool/resources deterministically.
- Preview teardown detaches `SwapChainPanel` before DXGI swap-chain resources are released.
- Do not hold lifecycle locks while doing UI-thread detach or COM disposal.

### Git Intelligence

Recent commits show the implementation lane this story must preserve:

- `ed589a7 feat: implement stop, restart, and recreate capture resources` hardened teardown/recreate behavior that Escape/cancel must reuse.
- `3a964fb Record capture session state review fixes` tightened status vocabulary and state projection.
- `9ffea82 feat: complete implementation of target selection for display or window capture` established target selection boundaries; overlay cancel must not reinterpret picker cancellation.
- `2f0e953 feat: implement minimal WGC FP16 capture to live preview` established the GPU-resident FP16 preview path; hit-test changes must not add bitmap or CPU readback paths.

### Latest Technical Information

- `Directory.Packages.props` currently locks `Microsoft.WindowsAppSDK` `1.8.260317003`, `Vortice.Direct3D11` `3.8.3`, `Vortice.DXGI` `3.8.3`, `Microsoft.NET.Test.Sdk` `18.4.0`, xUnit `2.9.3`, and xUnit runner `3.1.5`. Do not upgrade packages for this story without a concrete blocker.
- Microsoft Learn for WinUI/Windows App SDK windowing states that WinUI `Window` and `AppWindow` are based on the HWND model, and `AppWindow` manages top-level window size, placement, presenter, visibility, and z-order. Keep overlay window behavior in the presenter/windowing boundary.
- Microsoft Learn pointer guidance says `CapturePointer` should be called from pointer input while the pointer is pressed, captured pointers continue routing input to the capturing element, and pointer press/release are not guaranteed to occur in pairs. Keep `PointerCanceled` and `PointerCaptureLost` as gesture-ending paths.
- Microsoft Learn `UIElement.CapturePointer` notes capture only succeeds for a pressed pointer and only the capturing element receives pointer events while capture is held. Do not attempt to synthesize pointer captures outside pointer event handlers.

References:

- `_bmad-output/planning-artifacts/epics.md#Story-3.5-Manage-Overlay-Hit-Testing-and-Keyboard-Escape`
- `_bmad-output/planning-artifacts/prd.md#Overlay-and-Desktop-Window-Behavior`
- `_bmad-output/planning-artifacts/prd.md#Accessibility-and-Usability`
- `_bmad-output/planning-artifacts/architecture.md#Overlay-and-Desktop-Window-Behavior-FR17-FR21`
- `_bmad-output/planning-artifacts/architecture.md#Frontend-Architecture`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Overlay-Behavior`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Accessibility-Strategy`
- `_bmad-output/project-context.md#Critical-Implementation-Rules`
- `_bmad-output/implementation-artifacts/3-4-confirm-or-cancel-the-capture-overlay.md`
- `src/Lumiere.Overlay/OverlayWindow.xaml`
- `src/Lumiere.Overlay/OverlayWindow.xaml.cs`
- `src/Lumiere.Overlay/Windowing/OverlayWindowPresenter.cs`
- `src/Lumiere.App/MainWindow.xaml.cs`
- Microsoft Learn: https://learn.microsoft.com/en-us/windows/apps/develop/input/handle-pointer-input
- Microsoft Learn: https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.uielement.capturepointer?view=windows-app-sdk-1.8
- Microsoft Learn: https://learn.microsoft.com/en-us/windows/apps/develop/ui-input/windowing-overview
- Microsoft Learn: https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/windowing/windowing-overview

### Testing Requirements

Run from repository root on Windows:

```bash
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false
dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Overlay.Tests/Lumiere.Overlay.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet format Lumiere.sln --verify-no-changes --verbosity minimal
```

Automated tests should stay hardware-independent and focus on presenter/hit-test mode seams, Escape command routing where testable, idempotent cancel state, and preserving existing crop/confirm/layout tests.

Manual Windows validation is required for real fullscreen/topmost behavior, transparent/borderless hit testing, Escape while focus is on each visible control, crop pointer routing over `SwapChainPanel`, HDR/SDR display visibility, high-DPI scaling, multi-monitor placement, WGC, DXGI, D3D11, and HDR fidelity. Completion notes must label validation as `Mac-pass`, `Windows CI-pass`, or `Windows manual-pass` accurately.

### Anti-Patterns to Avoid

- Do not apply full-window click-through while crop selection is expected to work.
- Do not remove `CropCanvas.Background="Transparent"` or otherwise make empty overlay space non-hit-testable without an equivalent tested replacement.
- Do not rely only on root focus for Escape.
- Do not create duplicate crop state, duplicate confirm state, or duplicate teardown paths.
- Do not let overlay code dispose WGC, WinRT, COM, D3D11, DXGI, swap-chain, frame-pool, texture, or render-target resources.
- Do not introduce WPF, WinForms, Electron, Tauri, web UI, GDI, `BitmapImage`, `SoftwareBitmap`, CPU readback, SDR fallback, export, clipboard, annotation, hotkey, or tray behavior.
- Do not claim fullscreen/topmost/transparency/HDR behavior is fully complete without Windows manual validation.

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- 2026-05-05: Added failing overlay input/windowing tests for hit-test mode defaults, presenter diagnostics, Escape routing, and idempotent cancel gating.
- 2026-05-05: Implemented interactive hit-test mode defaults, presenter application diagnostics, Escape `KeyboardAccelerator` routing, and shared cancel request gate.
- 2026-05-05: Ran validation: restore, build, overlay tests, graphics tests, and format verification all passed on Windows.

### Completion Notes List

- Added an explicit `Lumiere.Overlay.Windowing` hit-test mode boundary with interactive MVP defaults and presenter diagnostic strings for interactive/pass-through modes.
- Preserved borderless topmost presenter behavior and display placement while surfacing the presenter technical detail in the overlay status details.
- Added Escape routing through both root `KeyDown` and a `KeyboardAccelerator`, backed by a shared idempotent cancel gate; `CloseRequested` and `CaptureConfirmed` remain separate paths.
- Preserved crop canvas pointer routing, transparent hit-test surface, pointer capture, and gesture-ending semantics.
- Added Story 3.5 manual validation checks for transparent/borderless hit testing, Escape focus paths, repeated Escape, high-DPI, HDR/SDR, and multi-monitor overlays.
- Validation level: Windows CI-pass for restore/build/tests/format; Windows manual-pass still required for real fullscreen/topmost transparency, WGC/DXGI/D3D11, HDR, high-DPI, and multi-monitor behavior.

### File List

- `src/Lumiere.Overlay/Windowing/OverlayHitTestMode.cs`
- `src/Lumiere.Overlay/Windowing/OverlayHitTestModeDefaults.cs`
- `src/Lumiere.Overlay/Windowing/OverlayPresenterApplication.cs`
- `src/Lumiere.Overlay/Windowing/OverlayWindowPresenter.cs`
- `src/Lumiere.Overlay/Input/OverlayKeyboardInputRouter.cs`
- `src/Lumiere.Overlay/Input/OverlayCancelRequestGate.cs`
- `src/Lumiere.Overlay/OverlayWindow.xaml`
- `src/Lumiere.Overlay/OverlayWindow.xaml.cs`
- `tests/Lumiere.Overlay.Tests/OverlayHitTestModeTests.cs`
- `tests/Lumiere.Overlay.Tests/OverlayInputTests.cs`
- `docs/validation/overlay-validation.md`
- `_bmad-output/implementation-artifacts/3-5-manage-overlay-hit-testing-and-keyboard-escape.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-05-05: Created Story 3.5 context and marked ready for development.
- 2026-05-05: Implemented overlay hit-test mode boundary, robust Escape cancellation routing, presenter diagnostics, tests, and manual validation notes.
