# Deferred Work

Updated 2026-05-09.

This file tracks work that is intentionally deferred after implementation or review. Keep only items that still need future attention here; resolved review history belongs in the story or review artifacts.

## MVP Blockers or Active Defects

None currently known.

## Active Technical Debt

None currently known.

## Recently Closed

None currently known.

## Deferred from: code review of story-4-1 (2026-05-10)

- 文档故事 review 退出标准：sprint-status 定义 review 为 "Ready for code review"，但文档故事没有从 review 到 done 的定义路径。需要为非代码故事定义 review 流程。
- Deferred 跟踪机制：cutover-classification.md 列出 6 个 deferred 项，但未创建对应的 story/epic 跟踪。需要在 sprint planning 中创建跟踪项或明确标记为 "accepted tech debt"。

## Deferred from: code review of story-4-2 (2026-05-11)

- 拒绝原因逻辑重复 [CaptureService.cs:105-110]：ExecuteCommand 中的 ternary 重复了 CanAcceptCommand 的 switch，缺少编译器耦合。若 CanAcceptCommand 修改接受/拒绝状态，此处必须同步更新。
- CaptureCommand 允许 null target [CaptureCommand.cs:9]：当前所有调用都传 target，但工厂方法允许 null，将编译时保证推迟为运行时检查。
- CaptureCommandResult 是 class 而非 record [CaptureCommandResult.cs:9]：与 CaptureCommand 的 record 语义不一致，若有人比较两个 result 对象会得到引用相等而非值相等。
- default case 静默拒绝未来 enum 值 [CaptureService.cs:64]：若 CaptureSessionStatus 新增值会被静默拒绝而非触发编译器警告，属于防御性编程但可能掩盖设计意图。

## Deferred from: code review of story-4-3 (2026-05-11)

- ValidateCommand 拒绝原因 ternary 重复 CanAcceptCommand switch [CaptureService.cs:105-110]：预存在问题，已在 deferred-work.md 中标记。需要编译器耦合。
- Debug.Assert 在 Release 构建中无效 [MainWindow.xaml.cs:563]：未来防护问题，热键/托盘入口点需要 DispatcherQueue 保护。Release 构建中 Debug.Assert 被编译掉，线程安全违规在生产环境中不可检测。

## Deferred from: bug fix for screenshot state reset (2026-05-11)

- SetCaptureActionsEnabled(true) 在 overlay 完成路径中未调用：按钮在 ExecuteCaptureFromUiAsync 的 finally 块中重新启用，但 OnOverlayCaptureConfirmed 和 OnOverlayCloseRequested 未重新启用。如果未来代码路径在 overlay 创建前禁用按钮，overlay 完成将永久禁用它们。
- ApplySessionState 中的重入守卫静默丢弃状态更新：如果 applyingSessionState 为 true，调用是静默的 no-op。修复添加了依赖于不被丢弃的第二个 ApplySessionState(Idle) 调用，但守卫使其无法推理。
- CaptureSessionState.FromStartResult 包含死代码：两个分支都调用 FromReadiness(target, result.Readiness, treatReadyAsCapturing: false)。条件语句无意义，表明方法是投机性编写的，从未正确实现。
- 修复未解决底层状态机设计缺陷：真正问题是 Disposed 和 Idle 被视为单独状态，但代码需要在单个原子操作中通过两者转换。修复是 band-aid：同一线程上的两个顺序 ApplySessionState 调用。原子 DisposeAndReset 或单个 Idle 调用（当意图返回就绪时跳过 Disposed）将是正确的修复。
