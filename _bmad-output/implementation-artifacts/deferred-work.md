## Deferred from: code review of 2-1-start-capture-and-select-a-display-or-window-target (2026-05-04)

### Handled on 2026-05-04

- D1-HIGH: Removed the `null!` handoff from `CaptureTarget.CreateForTest`. Test targets now explicitly report `HasCaptureItem == false`, and production capture startup rejects them with a clear readiness failure instead of reaching WGC with a hidden null item.
- D2-HIGH: Guarded capture selection/preview startup after window close and cleared capture/graphics service references when device resources are disposed.
- D3-MEDIUM: Made capture support probing injectable for tests and kept `NotSupportedException` mapped to an unsupported readiness result.
- D5-MEDIUM: Changed the idle target-selection UI label from "Initializing preview" to "Ready to capture" while preserving the existing readiness state model.
- D7-MEDIUM: Added an upper-bound validation for capture target dimensions using the D3D11 2D texture limit of 16,384 pixels per dimension.
- D8-LOW: Read `previewGeneration` through `Volatile.Read` in async/UI dispatcher callbacks and use `Interlocked.Increment` for generation bumps.

### Closed by design on 2026-05-04

- D4-MEDIUM: Closed as by design. `CaptureTarget` remains a typed target descriptor and does not implement `IDisposable` because `GraphicsCaptureItem` has no documented disposal contract in the API surface this project targets. WGC teardown stays with session/resource owners such as `CaptureSessionResources`.

### Future story candidate

- D6-MEDIUM: Add typed capture target creation for display/window paths. Picker-created production targets should continue to use `CaptureTargetKind.Unknown` because `GraphicsCapturePicker` returns only a `GraphicsCaptureItem`, not whether the user chose a display or a window. A future story should introduce explicit creation paths such as `TryCreateFromDisplayId(...) => Display` and `TryCreateFromWindowId(...) => Window`, likely behind a narrow infrastructure factory.

## Deferred from: code review of 2-3-stop-restart-and-recreate-capture-resources (2026-05-04)

- CaptureSessionResources disposal is not concurrency-idempotent. `Dispose()` checks a plain `bool disposed` before running the teardown action, so two racing callers could both dispose native WGC resources. This was pre-existing; the current story only added sequential double-dispose coverage.

## Deferred from: code review of 3-5-manage-overlay-hit-testing-and-keyboard-escape (2026-05-05)

- SwapChainResources 内部构造函数使用 `null!` 抑制编译器警告（`SwapChain = null!`），仅用于测试场景的内部构造函数，非生产路径。
- overlayWindow 字段跨线程非同步访问：`TryEnqueueUi` 在 frame 回调线程读取，`EnsureOverlayWindow`/`CloseOverlayWindow` 在 UI 线程写入，无显式同步。延续已有模式。
- CaptureSessionResources Action 适配器总是返回完整成功证据：接受 `Action` 的内部构造函数无条件返回四字段全 true 的证据，仅用于向后兼容的测试路径。
- DisposeAfterFailedUiDetach 缺少诊断日志：UI 线程不可用时静默释放资源，生产排障困难。
- TryEnqueueUi 静默回退 DispatcherQueue：overlay 的 DispatcherQueue 拒绝时静默路由到 RootGrid.DispatcherQueue。
- DisposalEvidence 生产环境无消费者：所有证据记录仅在测试中断言，存储但未读取。作为未来诊断基础设施。
- OnOverlayCaptureConfirmed 过早设置 Disposed 状态：StopPreview 异步清理后立即设置 Disposed 状态，与已有异步清理模式一致。

## Deferred from: code review of 2-5-create-monitor-capture-targets-without-picker (2026-05-07)

- `GetMonitorDisplayName` returns raw `DeviceName` (`\\.\DISPLAY1`) instead of a user-friendly name. Pre-existing UX concern, not caused by this change.
- `GetMonitorFromWindow` is public but unused in this changeset. Future use for window-handle fallback path.
- `MonitorFromPoint` with `MONITOR_DEFAULTTONEAREST` never returns null — the `IntPtr.Zero` check is dead code. Harmless but could be cleaned up.

## Deferred from: code review of 3-6-release-to-capture-and-copy (2026-05-07)

- 无 HDR→SDR 色调映射 — 故事规格明确说明"basic usable bitmap without claiming HDR-preserving semantics"，Story 4.2 定义完整语义。
- 测试文件重复 — `ReleaseToCaptureTests.cs` 与 `CropControllerTests.cs` 测试用例重复。代码清理，不影响功能。
- `CropCommitResult.InvalidGeometry` 路径创建多余 `CropSelection` — 对象身份变化但区域不变，下游引用相等性检查可能受影响。低风险。
- 架构边界违规 — `Lumiere.Infrastructure` 直接创建 D3D11 纹理，绕过 `Lumiere.Graphics` 边界。应移至 `Lumiere.Graphics` 或通过窄接口委托。需要更深入的重构，MVP 阶段可接受。
