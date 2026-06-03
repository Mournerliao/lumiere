# Deferred Work

Updated: 2026-05-26

This file tracks work intentionally deferred after implementation, review, or retrospective. It is not a graveyard: every unresolved item should either have a target epic/story hint, an accepted-tech-debt label, or a clear reason it remains parked.

Resolved review history belongs in story files or review artifacts. Historical mojibake-heavy review text should not be copied forward unless the meaning is recoverable.

## MVP Blockers or Active Defects

None currently known.

## Active Technical Debt

### Capture command rejection logic is duplicated

- Source: Story 4.2 / 4.3 code review.
- Status: **Resolved in Story 7.6.** `ClassifyRejection()` method provides single authoritative mapping from session status to rejection outcome.
- Evidence: `CaptureService.ValidateCommand()` and `TryReserveCommand()` both consume `ClassifyRejection()`.

### `ApplySessionState` reentrancy silently drops updates

- Source: screenshot state reset follow-up.
- Status: **Resolved in Story 7.6.** Reentrant calls now queue pending state and apply after current projection completes.
- Evidence: `MainWindow.ApplySessionState()` uses `pendingSessionState` field and deferred application loop with diagnostic logging.

### Capture action re-enable path depends on current overlay completion ordering

- Source: Story 4.3 review.
- Status: **Resolved in Story 7.6.** Capture action re-enable is driven by authoritative session-state projection, with diagnostic logging in `OnOverlayClosed()`.
- Evidence: `MainPanelProjection.Project(state)` drives `CanStartCapture` based on session state, not overlay completion ordering.

### Disposed-to-idle transition remains awkward

- Source: screenshot state reset follow-up.
- Status: **Resolved in Story 7.6.** `StopPreviewAndResetToIdle()` consolidates teardown and reset into single atomic flow.
- Evidence: `OnOverlayCloseRequested`, `OnOverlayCaptureConfirmed`, and `CompleteFullscreenCaptureAsync` all use `StopPreviewAndResetToIdle()`.

### `CaptureSessionState.FromStartResult` contains a dead conditional

- Source: screenshot state reset follow-up.
- Current shape: both branches call `FromReadiness(target, result.Readiness, treatReadyAsCapturing: false)`.
- Risk: hides intended semantics and weakens future maintenance confidence.
- Target: low-risk capture-state cleanup.
- Suggested acceptance criterion: Given `CaptureStartResult` is converted to session state, when start succeeds or does not start, then the method has distinct, tested branches or the dead conditional is removed.

## Future Story Candidates

### BMad workflow: non-code story review exit criteria

- Source: Epic 4 retrospective.
- Current shape: non-code/documentation stories can reach review without a crisp checklist that proves intended artifacts, links, and follow-through were completed.
- Risk: workflow-only work may appear done while losing review findings, status updates, or traceability.
- Target: before the next documentation/planning-heavy story.
- Suggested acceptance criterion: Given a story changes only BMad artifacts, when it enters review, then the story identifies expected artifact changes, preserved decisions, verification method, and review disposition.

### Epic 5: Main window and settings guardrails

- Source: Epic 4 retrospective.
- Status: guardrail document created in `_bmad-output/implementation-artifacts/epic-5-implementation-guardrails.md`.
- Target: include as context in Story 5.1 and later Epic 5 stories.
- Follow-up: if the guardrail proves durable, promote stable rules into `harness/` or `_bmad-output/project-context.md`.

### Epic 5.2 / 5.5: Settings write and persistence plan

- Source: Epic 4 retrospective and Story 4.4 review.
- Current shape: `ISettingsProvider` and `DefaultSettingsProvider` exist, but settings are read-only and only partially consumed.
- Risk: settings UI could create local duplicated state before persistence exists.
- Target: Story 5.2 or Story 5.5.
- Suggested acceptance criterion: Given editable settings are introduced, when the user changes a supported value, then the write path and persisted source of truth are owned by `Lumiere.Settings` and consumed through a shared abstraction.

### Epic 6.1: Output policy type ownership decision

- Source: Epic 4 retrospective.
- Current shape: `CropPixelRect`, `OutputTarget`, and output request types live in `Lumiere.Graphics.Output`, and `Lumiere.Settings` references `OutputTarget`.
- Risk: output policy vocabulary may become too graphics-owned if settings and UI semantics expand.
- Target: Story 6.1.
- Suggested acceptance criterion: Given output target policy is formalized, when ownership is reviewed, then shared output vocabulary has an explicit owning module and no circular or convenience-only dependencies.

### Epic 7: Release-build UI-thread protection for non-main entry points

- Source: Story 4.3 review.
- Status: **Resolved in Epic 7.** All tray and hotkey commands dispatch through `DispatcherQueue` before mutating app state.
- Evidence: Stories 7.1-7.5 all use `DispatcherQueue.TryEnqueue()` for tray/hotkey command handling.

### Future overlay story: InvalidCrop integration tests

- Source: Story 4.6 review and Epic 4 retrospective.
- Missing coverage:
  - InvalidCrop save/apply/timer/restore round trip in `OverlayWindow`.
  - Escape or close during active InvalidCrop feedback.
  - rapid successive invalid crop gestures.
  - confirm click while InvalidCrop feedback is active.
- Target: next overlay-related story or focused test-hardening task.
- Suggested acceptance criterion: Given InvalidCrop feedback is active, when close, retry, timer, or confirm paths run, then prior valid selection and terminal states are preserved correctly.

### Documentation cleanup: mojibake-heavy story review sections

- Source: Epic 4 retrospective.
- Current shape: several story review sections contain encoded/mojibake text.
- Risk: future automation or human review may misread historical findings.
- Target: documentation maintenance only if those records become active source material.
- Suggested acceptance criterion: Given a mojibake-heavy story record is used as planning input, when it is cleaned, then the corrected summary preserves original review meaning and does not rewrite historical conclusions.

## Validation Gaps Carried Forward

### Epic 8.4 / 8.5: Hardware validation gaps from Epic 4

- Source: Story 4.5 and Epic 4 retrospective.
- Gaps:
  - Escape cancel with and without active crop was not fully validated in Story 4.5.
  - Multi-monitor behavior was not validated beyond a single-monitor environment.
  - DPI scales 100%, 125%, and 200% were not validated; 150% was tested.
  - SDR display behavior was not separately validated.
  - Clipboard lock recovery/failure injection was not tested.
- Target: Story 8.4 and Story 8.5.
- Suggested acceptance criterion: Given release validation is executed, when these scenarios are not run or fail, then the release matrix records them as explicit gaps, limitations, or blockers rather than implied support.

## Accepted Decisions / No Current Action

### Sprint-status timestamp formats remain mixed for now

- Source: Epic 4 retrospective follow-through.
- Current shape: `sprint-status.yaml` may contain both date-only and timestamp-with-offset values.
- Decision: no current action; normalize only if a future tooling story needs machine-validated timestamps.
- Rationale: changing historical status metadata for cosmetic consistency adds noise without improving implementation safety.

### `MainWindow` retains a direct `CaptureService` field for now

- Source: Story 4.4 review.
- Decision: accepted as-is for the current foundation. `ICaptureCommandCoordinator` wraps command reservation, while `MainWindow` still needs `CaptureService` for current session projection and existing preview orchestration.
- Revisit trigger: if a future app-state coordinator removes the remaining projection dependency cleanly.

### Constructor-injected graphics resources fail through caller path

- Source: Story 4.5 review.
- Decision: accepted as-is. `GraphicsEngine` construction failure propagates through app startup; this is not a current active defect.
- Revisit trigger: if device-loss recovery or startup diagnostics become an explicit story.

### `ISettingsProvider` injected before full consumption

- Source: Story 4.4 review.
- Decision: accepted temporary seam. It exists so Epic 5 can consume settings through a shared abstraction.
- Revisit trigger: Story 5.2 / 5.5 should replace the stub-only usefulness with real settings shell/persistence behavior.

### `CaptureCommand` permits a null target

- Source: Story 4.2 review.
- Decision: accepted for current command shape because fullscreen/region commands may reserve target resolution for a later step.
- Revisit trigger: if command payload semantics are tightened after tray/hotkey and direct target selection settle.

### `CaptureCommandResult` is a class rather than a record

- Source: Story 4.2 review.
- Decision: accepted technical style debt. No current behavioral defect is known.
- Revisit trigger: if equality semantics become important in tests or command-result caching.

### Default switch rejects future `CaptureSessionStatus` values

- Source: Story 4.2 review.
- Decision: accepted defensive behavior for now.
- Revisit trigger: when adding a new session status, review command acceptance explicitly.

## Deferred from: code review of story 7.6 (2026-05-26)

- ClassifyRejection completeness — implicit default for new enum values [CaptureService.cs:105]. Future extensibility concern; current behavior is correct.
- Deferred loop blocks UI thread with many pending states [MainWindow.xaml.cs:1213]. Low risk in practice; at most 1-2 states accumulate.
- RequiresFailureTeardown behavior in deferred loop [MainWindow.xaml.cs:1228]. Correct but confusing; StopPreview calls during applyingSessionState will queue to pendingSessionState which the loop picks up.

## Recently Closed

- Story 7.6 resolved 4 technical debt items: capture command rejection unification, ApplySessionState reentrancy, capture action re-enable diagnostics, Disposed-to-idle consolidation.
- Epic 7 retrospective created `_bmad-output/implementation-artifacts/epic-7-retro-2026-05-26.md`.
- Epic 7 UI-thread protection resolved: all tray/hotkey commands dispatch through `DispatcherQueue`.
- Epic 4 retrospective created `_bmad-output/implementation-artifacts/epic-4-retro-2026-05-13.md`.
- Epic 5 guardrail follow-through created `_bmad-output/implementation-artifacts/epic-5-implementation-guardrails.md`.

## Deferred from: code review of 5-1-build-the-native-v0-main-panel.md (2026-05-17)

- Native close/minimize command affordance is still pending a later shell/tray story. Story 5.1 intentionally represents minimize/background intent without implementing tray/background behavior; revisit when Story 5.2 or Epic 7 owns shell commands.
- Full text scaling, high contrast, mixed-DPI, SDR, and multi-monitor manual validation remains future release-matrix coverage. Story 5.1 validated a single HDR 4K display at 150% DPI only.
- Add deeper automated coverage for HWND/DWM frame suppression helpers. Current Story 5.1 relies on build/manual validation for the native interop path; future coverage can extract pure style-bit planning logic or add boundary tests.

## Deferred from: code review of spec-restore-export-format-segmented-control.md (2026-05-25)

- CreateExportColorOptions allocates new list on every call — Could be static readonly field since data is static.
- "validation-scoped" jargon in accessibility text — Screen reader users won't understand this means "not functional".
- ExportColorDisplayValue hardcoded to "sRGB" — Panel-level automation name always says "Export profile: sRGB" regardless of actual selection.
- No test validates sRGB is default/active segment — Design Notes say "prefer sRGB" but no test explicitly asserts the policy.

## Deferred from: code review of story 8-1 (2026-06-01)

- AC1: 7 个可区分状态 vs 规格要求 8 个 — 基于 UX 分析，7 个标签已足够区分，8 状态需求留作未来优化
- Gap Analysis #4: Degraded preview 与 Enable HDR 共享标签 — stage 区分在 UX 层面意义有限
- Gap Analysis #5: Unsupported capture 与 HDR unavailable 共享标签 — 同上
- `SettingsPanelProjection.cs` 有未使用的 `using Lumiere.Graphics.Output` 引入 — 可能为后续集成预留
- "Output error" 与 Gap Analysis 术语 "Output failed" 不一致 — 术语统一留作后续

## Deferred from: overlay info panel user-friendly optimization (2026-06-03)

- 测试失败: `DefaultSettingsProviderTests.HdrAlertsEnabled_ReturnsTrue` 和 `AllProperties_ReturnConsistentValues` 失败 — 测试期望 `HdrAlertsEnabled` 默认为 `true`，但实际为 `false`。这是预先存在的问题，与本次更改无关。可能是设置文件中的值被设置为 `false`，需要检查测试环境或设置文件。
- 审查发现: 缺少无障碍支持（AutomationProperties）、硬编码中文文本、缺少动画效果、缺少测试覆盖、缺少悬停状态等。

## Deferred from: code review of story 8-3 (2026-06-03)

- Double-dispose of `swapChain3` in `SwapChainManager` catch+finally [SwapChainManager.cs:70,83] — pre-existing, already tracked from Story 8-2 review
- `InteropFailureDiagnostics.Write` uses unbounded `exception.ToString()` [InteropFailureDiagnostics.cs:14] — pre-existing pattern, may produce multi-KB log entries
- `TryReportFrameFailure` bare catch swallows callback exceptions [CaptureService.cs:387] — pre-existing, diagnostic logging executes before the callback so diagnostic is not lost

## Deferred from: code review of story 8-4 (2026-06-03)

- `SessionDiagnosticScope.Dispose()` 缺乏线程安全保障 — `disposed` 字段非 volatile 也无 Interlocked 保护 [SessionDiagnosticScope.cs:43-52]，当前单线程使用低风险
- `DiagnosticContext` 8 个工厂方法大量重复样板代码 [DiagnosticContext.cs]，风格偏好非功能问题
- `InteropFailureDiagnostics.LogAndFormat` 返回值含完整调用栈，传播到 PreviewReadinessStatus [InteropFailureDiagnostics.cs:14]，pre-existing pattern
- `DiagnosticRecord.Create` 不验证空/空白字符串 [DiagnosticRecord.cs:27-30]，防御性编码当前无实际风险
- `SessionDiagnosticScope` 8 字符十六进制 ID 碰撞风险 [SessionDiagnosticScope.cs:27-32]，概率极低
- `CaptureService` 日志格式与 `MapFailureToReadiness` 格式重复维护 [CaptureService.cs:276 vs 309]，DRY 违反
- `MapFailureToReadiness` 行为从"记录+格式化"变为"仅格式化" [CaptureService.cs:309-325]，当前调用点已正确记录但方法语义变更
- `DiagnosticRecord.Exception` 可变引用类型 [DiagnosticRecord.cs:15]，微小风险当前无实际影响

## Deferred from: code review of story 8-2 (2026-06-03)

- Multi-monitor probe hardcodes output index 0 — `HdrDisplayCapability.cs:92` always queries first output. On multi-monitor setups with different HDR states per display, probe result may not match capture target.
- Duplicated alert-message mapping logic — `MainPanelProjection.MapAlertMessage` and `TrayMenuProjection.MapTrayAlertMessage` share identical guard logic and switch structure.
- Tray alert label always uses Warning color — `TrayMenuWindow.xaml:46` hardcodes WarningBrush for all severities. Unsupported/Failed show yellow in tray but red in main panel.
- Fixed window height may clip InfoBar content — `MainPanelHeightDips = 310` with Auto-sized InfoBar row.
- No test coverage for Probe() COM paths — `HdrDisplayCapabilityTests.cs` tests only constructed records, not actual COM interop probing.
- swapChain3 double-disposed on error path — `SwapChainManager.cs` catch and finally blocks both dispose swapChain3.
- Probe(IDXGIDevice) overload is dead code — zero callers and zero test coverage.
- SwapChainManager probes HDR capability without caching — allocates COM objects on each Configure call.
